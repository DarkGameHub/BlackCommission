"""Remap Quaternius palette atlas -> Black Commission municipal-debt-noir palette.
Keeps luminance (shading cells survive), collapses hue into: dead-rubber-black ->
civic teal -> aged paper ramp; red-dominant cells -> stamp red. White bg kept.
"""
from PIL import Image
import sys

SRC, DST = sys.argv[1], sys.argv[2]

# sRGB anchors (0-255)
RAMP = [
    (0.00, (14, 17, 18)),      # dead rubber black
    (0.30, (30, 50, 52)),      # civic teal dark
    (0.55, (58, 84, 82)),      # civic teal
    (0.78, (150, 140, 115)),   # aged paper shadow
    (1.00, (201, 190, 164)),   # aged paper
]
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
        if red_dom > 0.12 and r > 90:  # red-family cell -> stamp red, luminance-scaled
            k = 0.55 + 0.7 * lum
            px[x, y] = (min(255, round(STAMP_RED[0] * k)),
                        min(255, round(STAMP_RED[1] * k)),
                        min(255, round(STAMP_RED[2] * k)), a)
        else:
            px[x, y] = (*ramp(lum), a)
        changed += 1
img.save(DST)
print(f"recolored {changed} px -> {DST}")
