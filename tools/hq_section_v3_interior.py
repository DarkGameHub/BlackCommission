#!/usr/bin/env python3
"""
HQ LONGITUDINAL SECTION A-A' v3 — 「废弃火星轨道货运堂」(纯室内 / Option B, enlarged).

CONCEPT (PM-approved 2026-06-24): the broke surface-retrieval agency squats inside the
derelict, contaminated shell of an ABANDONED MARS ORBITAL-FREIGHT DEPOT. World logic
(docs/world-background-2098.md): Mars = clean / expensive / parametric techno-luxury;
Earth = its ruins. The CRT is "discarded from Mars" — so is the building. ZAHA = MARS:
the soaring, swept, fluid parametric SHELL is the dead Martian machine; the agency's
warm orthogonal NEST bolted into the bottom is the anti-Zaha human counterpoint. The
contrast IS the design and the thesis: Earth's poor living in the bones of Mars's wealth.

v3 — INTERIOR-ONLY (Option B, locked 2026-06-24, unanimous 4-lead review): the whole
ritual happens UNDER the shell. The van parks, musters and BOARDS inside the loading
bay; the roll-up door is the THRESHOLD (a framed picture, not a path). The old apron /
sodium yard / Mars ruins beyond the door are now a NON-WALKABLE BACKDROP seen THROUGH
the open door — never entered.
SIZE (finalized): an enlargement trial (apex 15.5 / width 13) was reviewed by the art +
level leads and PULLED BACK — the PM judged it "不需要这么大". Locked at the APPROVED v2
grade: apex 13.6 m, hall width 11 m, depth ~27 m. The dwarfing already reads at 1:4.86
(2.8 m nest : 13.6 m shell ≈ Zaha DDP); bigger was scale-for-its-own-sake and made the
silhouette read squatter. The ONLY size change kept is the door 4.6 -> 4.8 m (so the
rising roll-up reveals the whole van head + a sliver of sky — ritual, zero build cost).
The NEST is deliberately NOT scaled — a vast shell over a tiny unchanged nest IS the thesis.

WHY a section (spatial-language-spec.md #1): the player reads the 截面 — width, ceiling
height, top profile, light. A plan cannot show the volume rhythm that fixes "太平了":
nest 2.8 m  ->  freight cathedral 15.5 m.

Curves are REAL (the PM required 曲线): shell, conveyor, torn cantilever = cubic-Bezier
swept ribbons. For the runtime builder they become a FACETED low-poly approximation
(chained angled segments) — buildable from primitives, no NURBS.

Cut: vertical plane down the dispatch axis (x=4.5), looking EAST (+X); +Z renders RIGHT
(south nest -> north loading door -> backdrop). Heights in FINAL metres (post HqScale 1.2).
Run:  python tools/hq_section_v3_interior.py   Out: design/hq/HQ_Section_AA_v3.png
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw  # noqa: E402
from hq_floorplan_render import (  # noqa: E402
    font, text_size, INK, PAPER, GREEN, RED, AMBER, VAN, VAN_F,
)

# ---- palette: Mars ghost (cool, pale-memory) vs human nest (warm, crude) ----
MARS    = (94, 100, 110)   # dead parametric shell — cut solid (cool, lighter than civic black)
MARS_O  = (38, 42, 50)
MARS_HI = (184, 190, 198)  # the last gleam of Mars white on the curved top edge
NESTP   = (60, 57, 52)     # agency nest — crude warm cut solid (anti-Zaha box)
NEST_O  = (22, 22, 20)
STEEL   = (58, 62, 70)     # dead cargo conveyor ribbon
STEEL_O = (28, 30, 34)
SLAB    = (52, 55, 52)     # interior floor slab
SLAB_O  = (26, 28, 26)
OUTSLAB = (44, 48, 54)     # ground BEYOND the door — cooler/bluer = "outside, not yours"
STAIN   = (110, 86, 62)    # contamination / rust streaks down the shell
MOLD    = (98, 130, 86)    # fungal bloom in the curves (the infection)
MOLD_D  = (70, 100, 64)
GLOWW   = (243, 214, 150)  # warm tungsten (nest)
GLOWS   = (240, 200, 132)  # sodium (interior van bay)
GLOWG   = (150, 226, 140)  # CRT phosphor green
SKYLT   = (208, 216, 208)  # pale contaminated daylight raking through the shell crack
SKYTINT = (214, 215, 201)  # faint open contaminated sky beyond the torn cantilever
DARKB   = (214, 208, 191)  # the doctrine's required 暗区
DIMCOL  = (60, 62, 58)
BEYOND  = (120, 132, 128)  # projected behind-cut linework
BEYOND2 = (150, 160, 156)
FOGSIL  = (132, 138, 128)  # dead treeline in fog
MARSFOG = (150, 160, 170)  # distant Mars-structure silhouette (the orienting landmark)
DIMSOD  = (150, 140, 110)  # dimmed backdrop sodium lamp (a prop, not a lit floor pool)

# ---- view transform --------------------------------------------------------
PXM = 33
Z0, Z1 = -2.5, 46.5
YMIN, YMAX = -1.1, 15.6
ML, MT, MRG, MB = 150, 190, 96, 300
W = int(ML + (Z1 - Z0) * PXM + MRG)
H = int(MT + (YMAX - YMIN) * PXM + MB)


def tz(z):
    return ML + (z - Z0) * PXM


def ty(y):
    return MT + (YMAX - y) * PXM


# ---- primitives ------------------------------------------------------------
def box(d, z0, z1, y0, y1, fill, outline=SLAB_O, w=2):
    x0, x1 = tz(z0), tz(z1)
    d.rectangle([min(x0, x1), ty(y1), max(x0, x1), ty(y0)], fill=fill, outline=outline, width=w)


def line(d, z0, y0, z1, y1, col, w=2):
    d.line([tz(z0), ty(y0), tz(z1), ty(y1)], fill=col, width=w)


def dash(d, z0, y0, z1, y1, col, w=2, dl=10, gp=7):
    a, b = (tz(z0), ty(y0)), (tz(z1), ty(y1))
    dist = math.hypot(b[0] - a[0], b[1] - a[1])
    if dist == 0:
        return
    ux, uy = (b[0] - a[0]) / dist, (b[1] - a[1]) / dist
    p = 0.0
    while p < dist:
        q = min(p + dl, dist)
        d.line([(a[0] + ux * p, a[1] + uy * p), (a[0] + ux * q, a[1] + uy * q)], fill=col, width=w)
        p += dl + gp


def arrow(d, a, b, col, w=3, head=12):
    d.line([a, b], fill=col, width=w)
    ang = math.atan2(b[1] - a[1], b[0] - a[0])
    for s in (math.radians(150), math.radians(-150)):
        d.line([b, (b[0] + head * math.cos(ang + s), b[1] + head * math.sin(ang + s))], fill=col, width=w)


def bez(p0, p1, p2, p3, steps=90):
    """cubic Bezier in WORLD (z,y) -> list of SCREEN (x,y)."""
    out = []
    for i in range(steps + 1):
        t = i / steps
        mt = 1 - t
        z = mt**3 * p0[0] + 3 * mt**2 * t * p1[0] + 3 * mt * t**2 * p2[0] + t**3 * p3[0]
        y = mt**3 * p0[1] + 3 * mt**2 * t * p1[1] + 3 * mt * t**2 * p2[1] + t**3 * p3[1]
        out.append((tz(z), ty(y)))
    return out


def swept_ribbon(d, segs, thick, fill, hi=None, out=None, sw=2):
    """a curved constant-thickness band from chained cubic segments (the Zaha sweep).
    segs: list of [(z,y)x4]; thick: band depth in world-m (offset DOWN for the under edge)."""
    top, und = [], []
    for s in segs:
        top += bez(s[0], s[1], s[2], s[3])
        s2 = [(p[0], p[1] - thick) for p in s]
        und += bez(s2[0], s2[1], s2[2], s2[3])
    d.polygon(top + und[::-1], fill=fill)
    if out:
        d.line(und, fill=out, width=sw, joint="curve")
    if hi:
        d.line(top, fill=hi, width=3, joint="curve")
    return top, und


def blob(d, z, y, r, fill, n=11):
    """irregular organic bloom (fungal contamination) at world (z,y), radius ~r m."""
    pts = []
    for k in range(n):
        a = 2 * math.pi * k / n
        rr = r * (0.62 + 0.38 * ((k * 7) % 5) / 4)
        pts.append((tz(z) + rr * PXM * math.cos(a), ty(y) - rr * PXM * math.sin(a)))
    d.polygon(pts, fill=fill)


def light_pool(d, apex, z_l, z_r, y_floor, col):
    d.polygon([apex, (tz(z_l), ty(y_floor)), (tz(z_r), ty(y_floor))], fill=col)
    d.ellipse([tz(z_l), ty(y_floor) - 5, tz(z_r), ty(y_floor) + 7], fill=col)


def figure(d, z, y_floor, h=1.8, col=(34, 36, 36)):
    x = tz(z)
    hr = 0.13 * PXM
    hy = ty(y_floor + h)
    d.ellipse([x - hr, hy, x + hr, hy + 2 * hr], fill=col)
    d.line([(x, hy + 2 * hr), (x, ty(y_floor + 0.62 * h))], fill=col, width=4)
    d.line([(x, ty(y_floor + 0.78 * h)), (x - 0.18 * PXM, ty(y_floor + 0.58 * h))], fill=col, width=3)
    d.line([(x, ty(y_floor + 0.78 * h)), (x + 0.18 * PXM, ty(y_floor + 0.6 * h))], fill=col, width=3)
    d.line([(x, ty(y_floor + 0.62 * h)), (x - 0.15 * PXM, ty(y_floor))], fill=col, width=3)
    d.line([(x, ty(y_floor + 0.62 * h)), (x + 0.15 * PXM, ty(y_floor))], fill=col, width=3)


def label(d, z, y, s, col=INK, size=15, anchor="lt", leader=None):
    f = font(size)
    px, py = tz(z), ty(y)
    tw, th = text_size(d, s.split("\n")[0], f)
    if "r" in anchor:
        px -= tw
    if "c" in anchor:
        px -= tw / 2
    if "m" in anchor:
        py -= th / 2
    if "b" in anchor:
        py -= th
    if leader:
        d.line([leader, (tz(z), ty(y))], fill=col, width=1)
    d.multiline_text((px, py), s, font=f, fill=col, spacing=3)


def vdim(d, z, y0, y1, s, off=0):
    x = tz(z) + off
    d.line([(x, ty(y0)), (x, ty(y1))], fill=DIMCOL, width=1)
    for yy in (y0, y1):
        d.line([(x - 5, ty(yy)), (x + 5, ty(yy))], fill=DIMCOL, width=1)
    f = font(13)
    tw, _ = text_size(d, s, f)
    cy = (ty(y0) + ty(y1)) / 2
    d.rectangle([x - tw / 2 - 3, cy - 9, x + tw / 2 + 3, cy + 9], fill=PAPER)
    d.text((x - tw / 2, cy - 8), s, font=f, fill=DIMCOL)


def hdim(d, z0, z1, y, s):
    d.line([(tz(z0), ty(y)), (tz(z1), ty(y))], fill=DIMCOL, width=1)
    for zz in (z0, z1):
        d.line([(tz(zz), ty(y) - 5), (tz(zz), ty(y) + 5)], fill=DIMCOL, width=1)
    f = font(13)
    tw, _ = text_size(d, s, f)
    cx = (tz(z0) + tz(z1)) / 2
    d.rectangle([cx - tw / 2 - 3, ty(y) - 9, cx + tw / 2 + 3, ty(y) + 9], fill=PAPER)
    d.text((cx - tw / 2, ty(y) - 8), s, font=f, fill=DIMCOL)


def tag(d, z, y, n, to=None):
    x, py = tz(z), ty(y)
    if to:
        d.line([(x, py), (tz(to[0]), ty(to[1]))], fill=(120, 122, 118), width=1)
    r = 11
    d.ellipse([x - r, py - r, x + r, py + r], fill=PAPER, outline=INK, width=2)
    f = font(14)
    tw, th = text_size(d, str(n), f)
    d.text((x - tw / 2, py - th / 2 - 1), str(n), font=f, fill=INK)


# ---- shell geometry (world z,y) — the dead Mars parametric sweep -----------
# piece A: springs from the south envelope wall, soars to the apex (13.6), stops at the crack
SHELL_A = [
    [(-0.6, 5.6), (2.5, 11.8), (6.0, 14.0), (10.4, 13.9)],
]
# piece B: resumes past the crack, sweeps DOWN to the loading-door head, then a TORN cantilever over the backdrop
SHELL_B = [
    [(12.6, 13.7), (17.5, 13.1), (22.0, 8.4), (26.8, 5.7)],
    [(26.8, 5.7), (29.6, 5.2), (31.6, 4.6), (33.4, 4.05)],
]
# dead cargo conveyor — the fluid ribbon that once fed containers to the orbital lift, now broken
CONV_1 = [[(5.4, 3.1), (10.5, 9.0), (15.5, 9.3), (19.0, 7.6)]]
CONV_2 = [[(20.8, 6.6), (22.4, 5.9), (23.4, 5.3), (24.4, 4.9)]]


def render(out_path):
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)

    # datum + faint 2 m height grid (so the soaring height reads)
    for gy in range(0, 15, 2):
        d.line([(tz(Z0), ty(gy)), (tz(Z1), ty(gy))], fill=(217, 210, 190), width=1)
    for gx in range(int(tz(Z0)), int(tz(Z1)), 16):
        d.line([(gx, ty(-0.05)), (gx - 9, ty(-0.62))], fill=(196, 180, 150), width=1)

    # faint contaminated haze low on the horizon (soft fade up — NOT a hard panel) — the backdrop air
    for i in range(9):
        yb = 0.2 + i * 0.45
        f = max(0.0, 1 - i / 8.0)
        c = tuple(int(PAPER[k] + (SKYTINT[k] - PAPER[k]) * 0.55 * f) for k in range(3))
        d.line([(tz(27.6), ty(yb)), (tz(46.5), ty(yb))], fill=c, width=int(0.55 * PXM) + 1)

    # ======== LIGHT (under everything — the noir pattern) ===================
    light_pool(d, (tz(4.4), ty(2.55)), 2.6, 6.0, 0.02, GLOWW)             # warm nest pool
    d.ellipse([tz(3.8), ty(1.5), tz(5.0), ty(0.75)], fill=GLOWG)          # CRT phosphor
    # the contaminated daylight shaft raking through the shell crack onto the muster
    d.polygon([(tz(10.5), ty(13.6)), (tz(12.6), ty(13.6)), (tz(17.6), ty(0.02)), (tz(13.4), ty(0.02))], fill=SKYLT)
    light_pool(d, (tz(21.4), ty(5.0)), 19.0, 24.2, -0.14, GLOWS)         # sodium on the van — the INTERIOR hero pool
    # NOTE (v3): the two apron sodium floor-pools are GONE — the lit staging is now INSIDE; the
    # ground beyond the door is dim backdrop (haze + silhouettes only), not a walkable lit yard.

    # required dark zones (留暗区)
    for (za, zb) in ((6.6, 9.6), (28.5, 39.8)):
        box(d, za, zb, -0.14, 0.0, DARKB, outline=None, w=0)
        d.text((tz((za + zb) / 2) - 6, ty(0.0) + 8), "暗", font=font(13), fill=(150, 140, 120))

    # ======== BACKDROP beyond the door (fog) — SEEN, NEVER ENTERED ==========
    # distant Mars-ruin silhouette — another dead parametric sweep = orienting landmark
    far = bez((38.5, 0), (38.0, 7.0), (40.5, 8.6), (42.2, 4.6)) + bez((42.2, 4.6), (43.0, 3.0), (44.0, 1.4), (44.6, 0))
    d.polygon(far + [(tz(44.6), ty(0)), (tz(38.5), ty(0))], fill=(176, 184, 192))   # faint, deep haze
    sil = bez((44.0, 0), (43.6, 9.8), (46.0, 11.8), (46.5, 5.6)) + bez((46.5, 5.6), (46.4, 3.0), (46.3, 1.2), (46.5, 0))
    d.polygon(sil + [(tz(46.5), ty(0))], fill=MARSFOG)                               # nearer ruin, the landmark
    for (zc, hh) in ((44.9, 4.4), (45.7, 5.4), (44.0, 3.4)):
        d.polygon([(tz(zc), ty(0)), (tz(zc - 0.45), ty(hh * 0.45)), (tz(zc), ty(hh)), (tz(zc + 0.45), ty(hh * 0.45))], fill=FOGSIL)

    # ======== FLOOR (interior) + GROUND (backdrop, cooler) ==================
    box(d, -0.2, 26.8, -0.18, 0.0, SLAB)                                  # interior freight floor (yours)
    box(d, 26.8, 43.4, -0.5, -0.18, OUTSLAB)                              # backdrop ground (steps down past the sill)
    d.polygon([(tz(43.4), ty(-0.18)), (tz(46.5), ty(-0.5)), (tz(46.5), ty(-1.0)), (tz(43.4), ty(-0.86))], fill=OUTSLAB, outline=SLAB_O)
    line(d, 26.8, 0.0, 26.8, -0.18, (22, 24, 24), 4)                      # door SILL — the hard threshold edge
    box(d, 26.8, 27.2, -0.22, -0.14, (20, 22, 22), outline=None, w=0)     # cast shadow just outside the sill
    line(d, 34.0, -0.34, 34.6, -0.34, (30, 32, 30), 3)                    # drain slot (backdrop)
    # abandoned cargo container under the cantilever — the freight yard Mars walked away from (backdrop)
    cont = [(tz(28.7), ty(-0.12)), (tz(31.6), ty(-0.2)), (tz(31.7), ty(1.72)), (tz(28.8), ty(1.8))]
    d.polygon(cont, fill=(74, 70, 66), outline=(34, 32, 30))
    for rz in (29.3, 29.9, 30.5, 31.1):
        line(d, rz, -0.14, rz, 1.74, (50, 48, 44), 1)                     # corrugation ribs
    blob(d, 28.9, 1.74, 0.34, MOLD_D)
    blob(d, 29.2, 1.78, 0.2, MOLD)

    # ======== ENVELOPE END WALLS (where the shell lands) ====================
    box(d, -0.7, -0.2, 0.0, 5.7, MARS, outline=MARS_O)                    # south envelope wall (shell springs here)
    box(d, 26.6, 27.1, 0.0, 5.7, MARS, outline=MARS_O)                    # north wall / loading-door pier
    box(d, 26.6, 27.1, 4.8, 5.7, MARS, outline=MARS_O)                    # door lintel (opening 4.8 m)

    # ======== THE MARS SHELL (curved, swept — soaring to 13.6) ==============
    swept_ribbon(d, SHELL_A, 0.85, MARS, hi=MARS_HI, out=MARS_O)
    topB, undB = swept_ribbon(d, SHELL_B, 0.85, MARS, hi=MARS_HI, out=MARS_O)
    # torn cantilever end: exposed rebar jutting from the broken edge
    for k in range(5):
        yy = 3.25 + k * 0.2
        line(d, 33.4, yy, 34.1 + 0.12 * k, yy + 0.16, STAIN, 2)
    # contamination streaks running DOWN the inside of the shell + fungal blooms in the curves
    for (zs, y0, y1) in ((4.0, 12.6, 9.2), (8.2, 13.4, 10.0), (16.5, 12.4, 8.8), (21.0, 9.6, 6.4)):
        dash(d, zs, y0, zs + 0.25, y1, STAIN, w=2, dl=6, gp=10)
    blob(d, 1.6, 9.2, 0.6, MOLD_D)
    blob(d, 2.0, 9.4, 0.42, MOLD)
    blob(d, 24.5, 5.0, 0.5, MOLD_D)
    blob(d, 25.0, 5.2, 0.34, MOLD)
    blob(d, 9.0, 0.0, 0.5, MOLD_D)
    blob(d, 9.3, 0.05, 0.32, MOLD)

    # ======== leaning Y tree-column (Zaha splayed support, now taller) ======
    def strut(z_base, z_top, y_top, wbase=0.34, wtop=0.16):
        d.polygon([(tz(z_base - wbase), ty(0)), (tz(z_base + wbase), ty(0)),
                   (tz(z_top + wtop), ty(y_top)), (tz(z_top - wtop), ty(y_top))], fill=MARS, outline=MARS_O)
    strut(15.4, 13.4, 12.6)
    strut(15.4, 17.2, 12.7)
    d.line([(tz(15.4), ty(0)), (tz(15.4), ty(3.0))], fill=MARS_O, width=1)  # shared stem hint

    # ======== dead cargo conveyor ribbon (broken) ==========================
    swept_ribbon(d, CONV_1, 0.55, STEEL, hi=None, out=STEEL_O)
    swept_ribbon(d, CONV_2, 0.55, STEEL, hi=None, out=STEEL_O)
    for hz in (8.0, 13.0, 17.5):                                          # hangers from shell to conveyor
        dash(d, hz, 12.6, hz, 9.2, (70, 72, 76), w=1, dl=5, gp=5)
    line(d, 19.6, 7.2, 20.2, 5.4, STEEL, 5)                              # a collapsed dangling section at the break

    # ======== THE AGENCY NEST (warm orthogonal box — deliberately NOT enlarged) ===
    box(d, -0.2, 6.0, 2.7, 2.92, NESTP, outline=NEST_O)                  # crude patched roof (压顶 = 2.8)
    box(d, 5.8, 6.0, 0.0, 2.92, NESTP, outline=NEST_O)                   # nest front post (toward the hall)
    d.line([(tz(5.9), ty(2.5)), (tz(5.9), ty(0.0))], fill=(150, 120, 70), width=1)  # warm spill at the threshold
    # CRT job desk + monitor (the secondhand Mars terminal)
    box(d, 3.6, 5.0, 0.0, 0.74, NESTP, outline=NEST_O)
    box(d, 4.0, 4.8, 0.74, 1.42, (40, 44, 42), outline=NEST_O)
    d.rectangle([tz(4.08), ty(1.34), tz(4.72), ty(0.86)], fill=GLOWG)    # green screen
    # debt / takeover board on the south wall
    box(d, 0.2, 1.2, 1.3, 2.4, None, outline=RED, w=2)
    line(d, 0.2, 2.4, 1.2, 1.3, (170, 70, 60), 1)
    label(d, 0.0, 2.66, "债务/接管板", RED, 12)

    # ======== gear rack (hall, half-empty) + muster pad =====================
    box(d, 7.6, 8.7, 0.0, 2.3, None, outline=BEYOND, w=2)
    for yy in (0.7, 1.4, 2.0):
        line(d, 7.6, yy, 8.7, yy, BEYOND2, 1)
    line(d, 8.0, 0.7, 8.5, 0.7, BEYOND, 4)                               # one lonely item left on the rack
    d.line([(tz(13.4), ty(0.0)), (tz(17.6), ty(0.0))], fill=BEYOND, width=3)  # muster pad outline

    # ======== loading door (freight roll-up, 5.0 m) + the VAN (inside) ======
    d.ellipse([tz(25.9), ty(5.5), tz(26.7), ty(4.85)], outline=(40, 42, 40), width=3)  # coiled drum (above the 4.8 head)
    d.arc([tz(25.9), ty(5.5), tz(26.7), ty(4.85)], 0, 360, fill=(70, 72, 70), width=2)
    box(d, 20.0, 23.6, -0.14, 2.0, VAN_F, outline=INK, w=2)              # van body
    box(d, 22.9, 23.6, 2.0, 2.62, VAN_F, outline=INK, w=2)              # cab (nose north, toward the door)
    d.ellipse([tz(20.4), ty(0.06), tz(21.0), ty(-0.5)], fill=(30, 32, 32))
    d.ellipse([tz(22.6), ty(0.06), tz(23.2), ty(-0.5)], fill=(30, 32, 32))
    d.ellipse([tz(23.4), ty(1.5), tz(23.8), ty(1.1)], fill=(255, 238, 190))  # warm headlight toward the door

    # backdrop sodium lamp standards (now BEHIND the door — dim props, no floor pool)
    def sodium_pole(z, h=4.4):
        line(d, z, 0.0, z, h, (44, 46, 44), 3)
        d.line([(tz(z), ty(h)), (tz(z + 0.8), ty(h - 0.12))], fill=(44, 46, 44), width=3)
        d.ellipse([tz(z + 0.55), ty(h - 0.02), tz(z + 1.05), ty(h - 0.5)], fill=DIMSOD, outline=(60, 56, 44))
    sodium_pole(33.9)
    sodium_pole(40.0)

    # ======== gate + clearance bar (freight height) — backdrop =============
    box(d, 42.4, 43.4, 4.2, 4.42, MARS, outline=MARS_O)                  # clearance bar
    dash(d, 42.9, 0.0, 42.9, 3.4, (60, 62, 58), w=2)                     # closed gate leaf (beyond)
    box(d, 42.8, 43.0, 4.42, 4.8, MARS, outline=MARS_O)

    # ======== the door is a FRAME: faint sightline spawn -> van -> landmark ==
    eye = (tz(2.4), ty(1.68))
    d.ellipse([eye[0] - 5, eye[1] - 5, eye[0] + 5, eye[1] + 5], outline=GREEN, width=2)
    dash(d, 2.4, 1.68, 21.0, 1.25, GREEN, w=2, dl=12, gp=8)              # spawn eye -> the van (the goal)
    arrow(d, (tz(19.2), ty(1.3)), (tz(21.0), ty(1.25)), GREEN, w=2, head=9)
    dash(d, 24.2, 1.7, 44.6, 4.4, (158, 158, 158), w=1, dl=7, gp=9)      # ...and on THROUGH the door to the ruin = framed view

    # ======== human scale (a person dwarfed by the 15.5 m cathedral) ========
    figure(d, 4.0, 0.0, col=(60, 50, 40))      # in the warm nest
    figure(d, 12.8, 0.0)                        # muster — under the crack-light, tiny in the 15.5 m void
    figure(d, 16.6, 0.0)                        # second of the team gathering at the column base
    figure(d, 19.2, -0.14, col=(70, 72, 70))   # boarding the van — INSIDE (the commitment point)

    # ======== DIMENSIONS (the volume rhythm — the whole point) ==============
    vdim(d, 2.4, 0.0, 2.8, "暖窝 2.8")
    vdim(d, 7.0, 0.0, 13.6, "火星货堂 13.6", off=0)
    vdim(d, 26.9, 0.0, 4.8, "装卸门 4.8", off=14)
    vdim(d, 23.6, -0.14, 2.83, "货车 2.83", off=12)
    vdim(d, 43.4, -0.2, 4.3, "限高 4.3", off=10)
    hdim(d, -0.2, 6.0, -0.66, "暖窝 6 m")
    hdim(d, 6.0, 26.8, -0.66, "货运堂 ~21 m (宽 11 m·见平面)")
    hdim(d, 26.8, 43.0, -0.66, "门外·火星废料场 (背景·不可进入)")

    # ======== KEYNOTE TAGS ==================================================
    for n, z, y, to in [
        (1, 3.0, 2.6, (4.0, 2.92)), (2, 5.5, 1.5, (4.7, 1.3)),
        (3, 12.4, 3.0, (12.8, 0.15)), (4, 17.8, 7.0, (16.7, 6.4)),
        (5, 8.2, 8.7, (9.3, 8.6)), (6, 11.6, 14.3, (11.6, 13.7)),
        (7, 23.2, 11.2, (22.2, 8.9)), (8, 21.2, 3.2, (20.8, 2.0)),
        (9, 27.9, 3.7, (26.9, 4.4)), (10, 34.4, 4.9, (33.6, 4.05)),
    ]:
        tag(d, z, y, n, to)
    label(d, 36.4, 6.6, "雾中另一座火星废墟=地标", (96, 102, 92), 12, leader=(tz(45.2), ty(5.6)))
    label(d, 30.2, 2.7, "门=框景", (110, 104, 84), 13)

    # ======== TITLE =========================================================
    d.text((ML, 24), "HQ 纵剖面 A–A′ — 「废弃火星轨道货运堂」(纯室内 / Option B)", font=font(30), fill=INK)
    d.text((ML, 62),
           "破产地表回收事务所蹲占火星废墟 · 扎哈＝火星(死参数化曲面壳) 对峙 人类暖窝(正交·破败) · 体积韵律 暖窝2.8m→货堂13.6m(压缩→释放·比1:4.86) · 门=框景·门外不可进入",
           font=font(15), fill=(96, 90, 70))
    d.text((ML, 86),
           "Option B 锁定: 全流程在壳内 (集结·登车·结算皆室内); 卷帘门是门槛不是路。曲线＝真曲线(builder 分面近似可 runtime 搭) · 依 spatial-language-spec 第1原则「玩家读截面」",
           font=font(14), fill=(120, 112, 88))

    # ======== KEYNOTES (bottom, two columns) ================================
    keynotes = [
        "① 暖窝 — 事务所私接的暖箱(低·正交·杂乱·尺寸不放大); 反扎哈的人类尺度·出生区",
        "② CRT — 火星淘汰的二手终端; 选任务·结算; 磷光绿=全场唯一「活信号」",
        "③ 集结台 — 站在污染天光裂缝下集合; 人被13.6m巨腔吞没(尺度对比1:4.86·壳大窝小=论点)",
        "④ 倾斜树状柱 — 扎哈式splayed支撑; 从货堂地面斜伸·撑起巨壳",
        "⑤ 死货运廊 — 曾把地球之物送上轨道的传送带; 已断裂坍塌(扫掠曲线)",
        "⑥ 火星壳裂缝 — 壳体破口; 苍白污染天光斜泻而下=carved light",
        "⑦ 火星壳 — 死掉的参数化曲面顶(顶13.6m); 昔日洁白被地球污染浸透·裂·长霉",
        "⑧ 货车 — 全场唯一活物; 停在室内装卸湾的钠灯里(登车=承诺点·英雄镜头)",
        "⑨ 装卸大门 — 货运卷帘(高4.8m); 门=望见门外火星废料场的框景; 车在门内登车→门升→驶出(过场)",
        "⑩ 断裂悬挑 — 壳体扫掠出挑·钢筋外露; 废墟的硬边界(无需围墙)",
    ]
    ky = H - MB + 24
    d.text((ML, ky - 26), "关键注释", font=font(16), fill=INK)
    colw = (W - ML - MRG) / 2
    for i, s in enumerate(keynotes):
        col = 0 if i < 5 else 1
        row = i if i < 5 else i - 5
        d.text((ML + col * colw, ky + row * 25), s, font=font(14), fill=(48, 50, 46))

    # ======== LEGEND + SCALE ================================================
    ly = H - 60
    d.text((ML, ly - 26), "图例", font=font(15), fill=INK)
    items = [
        (MARS, "火星壳(剖切·死参数化曲面)"), (NESTP, "人类暖窝(剖切)"), (STEEL, "死货运廊"),
        (GLOWS, "室内钠灯(装卸湾)"), (GLOWW, "暖钨灯"), (GLOWG, "CRT磷光"), (SKYLT, "污染天光"),
        (MOLD, "霉斑(感染)"), (GREEN, "可视轴线"), (OUTSLAB, "门外背景(不可进入)"),
    ]
    cx = ML
    for col, lab in items:
        d.rectangle([cx, ly, cx + 20, ly + 16], fill=col, outline=INK)
        d.text((cx + 26, ly), lab, font=font(13), fill=INK)
        cx += 26 + text_size(d, lab, font(13))[0] + 26
        if cx > W - 360:
            cx = ML
            ly += 26
    sbx, sby = W - MRG - 5 * PXM - 10, H - 54
    d.line([(sbx, sby), (sbx + 5 * PXM, sby)], fill=INK, width=4)
    for k in range(6):
        d.line([(sbx + k * PXM, sby - 5), (sbx + k * PXM, sby + 5)], fill=INK, width=2)
    d.text((sbx + 5 * PXM + 8, sby - 9), "5 m", font=font(14), fill=INK)

    img.save(out_path)
    return W, H


if __name__ == "__main__":
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "design", "hq")
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "HQ_Section_AA_v3.png")
    w, h = render(path)
    print(f"OK  {path}  ({w}x{h})")
