"""Remap Quaternius palette atlas -> Black Commission municipal-debt-noir palette.
Keeps luminance (shading cells survive), collapses hue into: dead-rubber-black ->
civic teal -> aged paper ramp; red-dominant cells -> stamp red. White bg kept.

Modes (optional 3rd arg, default "bc"):
  bc     — creature skin: teal/paper ramp + red-family cells -> stamp red.
  statue — weathered municipal bronze: dark bronze -> verdigris -> chalky oxide.
           No red branch (a statue must not read as flesh); everything oxidises.
"""
from PIL import Image
import sys

SRC, DST = sys.argv[1], sys.argv[2]
MODE = sys.argv[3] if len(sys.argv) > 3 else "bc"

# sRGB anchors (0-255)
RAMPS = {
    "bc": [
        (0.00, (14, 17, 18)),      # dead rubber black
        (0.30, (30, 50, 52)),      # civic teal dark
        (0.55, (58, 84, 82)),      # civic teal
        (0.78, (150, 140, 115)),   # aged paper shadow
        (1.00, (201, 190, 164)),   # aged paper
    ],
    "statue": [
        (0.00, (16, 15, 12)),      # near-black bronze crevice
        (0.22, (56, 50, 34)),      # dark aged bronze (horns/halo band)
        (0.42, (66, 112, 94)),     # verdigris (demon body-cell luminance lands here)
        (0.65, (122, 162, 138)),   # pale verdigris
        (1.00, (192, 208, 188)),   # chalky oxide highlight
    ],
}
RAMP = RAMPS[MODE]
STAMP_RED = (168, 51, 42)

def ramp(t):
    for (t0, c0), (t1, c1) in zip(RAMP, RAMP[1:]):
        if t <= t1:
            k = 0 if t1 == t0 else (t - t0) / (t1 - t0)
            return tuple(round(a + (b - a) * k) for a, b in zip(c0, c1))
    return RAMP[-1][1]

img = Image.open(SRC).convert("RGBA")
px = img.load()
w, h = img.size
changed = 0
for y in range(h):
    for x in range(w):
        r, g, b, a = px[x, y]
        if a == 0 or (r > 235 and g > 235 and b > 235):
            continue  # unused/background
        lum = (0.2126 * r + 0.7152 * g + 0.0722 * b) / 255.0
        red_dom = (r - max(g, b)) / 255.0
        if MODE == "statue" and red_dom > 0.12:
            # Flesh cells become the statue's verdigris body: Quaternius reds are dark
            # (lum ~0.2), so lift them into the oxide band instead of the crevice band.
            lum = min(0.72, max(0.38, 0.40 + (lum - 0.18) * 0.9))
            px[x, y] = (*ramp(lum), a)
            changed += 1
            continue
        if MODE == "bc" and red_dom > 0.12 and r > 90:  # red-family cell -> stamp red, luminance-scaled
            k = 0.55 + 0.7 * lum
            px[x, y] = (min(255, round(STAMP_RED[0] * k)),
                        min(255, round(STAMP_RED[1] * k)),
                        min(255, round(STAMP_RED[2] * k)), a)
        else:
            px[x, y] = (*ramp(lum), a)
        changed += 1
img.save(DST)
print(f"recolored {changed} px -> {DST}")
