#!/usr/bin/env python3
"""
HQ floor-plan renderer for Black Commission (Municipal Debt Noir).

Data-driven top-down floor-plan drawer built on Pillow (no matplotlib needed).
Feed it a `plan` dict (shell polygon, zones, openings, player path, spawns,
sightlines, dims, legend) and it emits a clean PNG for PM review.

Style: aged-paper ground, dead-rubber ink, civic-teal zones, dispatch-green
path, stamp-red locked door, sodium-amber departure threshold. Labels are CJK
(Microsoft YaHei) so the PM can read them directly.

Coordinate convention (matches the design agents):
  origin (0,0) at an interior corner; +X = width (right); +Z = depth (into the
  unit). The roll-up departure door is on the far +Z side, so +Z renders UP.

Usage:  python tools/hq_floorplan_render.py [out.png]
Edit build_demo_plan (or import render_floorplan) to draw a real layout.
"""

import math
import sys
from PIL import Image, ImageDraw, ImageFont

# --- Municipal Debt Noir palette -------------------------------------------
PAPER  = (228, 221, 201)   # aged paper ground (outside the unit)
FLOOR  = (214, 206, 183)   # interior floor (slightly darker than paper)
INK    = (26, 28, 26)      # dead-rubber black (walls, text)
GRID   = (203, 193, 170)
GRID5  = (178, 166, 138)
TEAL   = (46, 110, 115)    # civic teal (outlines for work zones)
TEAL_F = (150, 184, 186)   # civic teal fill
GREEN  = (54, 120, 74)     # restrained dispatch green (player path)
RED    = (158, 43, 37)     # stamp red (locked door / takeover pressure)
AMBER  = (214, 156, 58)    # sodium amber (departure threshold / reveal)
VAN    = (60, 62, 64)      # van body
VAN_F  = (96, 98, 100)
SIL    = (88, 92, 100)     # distant city silhouette
PAPER_F = (205, 196, 174)  # neutral prop fill (paper/board)

PPM = 48                   # pixels per metre
PAD_M = 1.4                # world padding (m) around all geometry
MARG = dict(l=96, t=86, r=96, b=250)  # px margins (b leaves room for legend)


# --- font helpers ----------------------------------------------------------
_FONT_CANDIDATES = [
    "C:/Windows/Fonts/msyh.ttc",     # Microsoft YaHei (CJK + Latin)
    "C:/Windows/Fonts/simhei.ttf",
    "C:/Windows/Fonts/simsun.ttc",
    "C:/Windows/Fonts/arial.ttf",
]
_font_cache = {}


def font(size):
    if size in _font_cache:
        return _font_cache[size]
    for path in _FONT_CANDIDATES:
        try:
            f = ImageFont.truetype(path, size)
            _font_cache[size] = f
            return f
        except OSError:
            continue
    f = ImageFont.load_default()
    _font_cache[size] = f
    return f


def text_size(draw, s, fnt):
    b = draw.textbbox((0, 0), s, font=fnt)
    return b[2] - b[0], b[3] - b[1]


def text_center(draw, cx, cy, s, fnt, fill):
    w, h = text_size(draw, s, fnt)
    draw.text((cx - w / 2, cy - h / 2), s, font=fnt, fill=fill, align="center")


# --- geometry helpers ------------------------------------------------------
def rect_corners(cx, cz, w, d, angle=0.0):
    a = math.radians(angle)
    ca, sa = math.cos(a), math.sin(a)
    out = []
    for dx, dz in ((-w / 2, -d / 2), (w / 2, -d / 2), (w / 2, d / 2), (-w / 2, d / 2)):
        out.append((cx + dx * ca - dz * sa, cz + dx * sa + dz * ca))
    return out


def bounds_of(plan):
    xs, zs = [], []

    def add(x, z):
        xs.append(x)
        zs.append(z)

    for v in plan.get("shell", []):
        add(*v)
    for z in plan.get("zones", []):
        for c in rect_corners(z["cx"], z["cz"], z["w"], z["d"], z.get("angle", 0)):
            add(*c)
    for o in plan.get("openings", []):
        add(*o["p1"])
        add(*o["p2"])
    for p in plan.get("path", []):
        add(*p)
    for s in plan.get("spawns", []):
        add(s["x"], s["z"])
    for sl in plan.get("sightlines", []):
        add(*sl["from"])
        add(*sl["to"])
    return min(xs) - PAD_M, max(xs) + PAD_M, min(zs) - PAD_M, max(zs) + PAD_M


def make_tx(xmin, zmax):
    def tx(x, z):
        return (MARG["l"] + (x - xmin) * PPM, MARG["t"] + (zmax - z) * PPM)
    return tx


# --- primitives ------------------------------------------------------------
def arrow(draw, a, b, color, width, head=13):
    draw.line([a, b], fill=color, width=width)
    ang = math.atan2(b[1] - a[1], b[0] - a[0])
    for s in (math.radians(152), math.radians(-152)):
        hx = b[0] + head * math.cos(ang + s)
        hy = b[1] + head * math.sin(ang + s)
        draw.line([b, (hx, hy)], fill=color, width=width)


def dashed(draw, a, b, color, width, dash=11, gap=8):
    x1, y1 = a
    x2, y2 = b
    dist = math.hypot(x2 - x1, y2 - y1)
    if dist == 0:
        return
    ux, uy = (x2 - x1) / dist, (y2 - y1) / dist
    pos = 0.0
    while pos < dist:
        p, q = pos, min(pos + dash, dist)
        draw.line([(x1 + ux * p, y1 + uy * p), (x1 + ux * q, y1 + uy * q)], fill=color, width=width)
        pos += dash + gap


# --- main render -----------------------------------------------------------
def render_floorplan(plan, out_path):
    xmin, xmax, zmin, zmax = plan.get("bounds") or bounds_of(plan)
    W = int(MARG["l"] + (xmax - xmin) * PPM + MARG["r"])
    H = int(MARG["t"] + (zmax - zmin) * PPM + MARG["b"])
    tx = make_tx(xmin, zmax)

    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)

    # grid
    x0 = math.ceil(xmin)
    x1 = math.floor(xmax)
    z0 = math.ceil(zmin)
    z1 = math.floor(zmax)
    for gx in range(x0, x1 + 1):
        col = GRID5 if gx % 5 == 0 else GRID
        d.line([tx(gx, zmin), tx(gx, zmax)], fill=col, width=1)
    for gz in range(z0, z1 + 1):
        col = GRID5 if gz % 5 == 0 else GRID
        d.line([tx(xmin, gz), tx(xmax, gz)], fill=col, width=1)

    # interior floor fill + shell outline
    shell = plan.get("shell")
    if shell:
        poly = [tx(*v) for v in shell]
        d.polygon(poly, fill=FLOOR)
        d.line(poly + [poly[0]], fill=INK, width=6)

    # openings (roll-up door etc.) drawn over the wall
    for o in plan.get("openings", []):
        d.line([tx(*o["p1"]), tx(*o["p2"])], fill=o.get("color", AMBER), width=9)
        if o.get("label"):
            mx = (o["p1"][0] + o["p2"][0]) / 2
            mz = (o["p1"][1] + o["p2"][1]) / 2
            px, py = tx(mx, mz)
            text_center(d, px, py - 16, o["label"], font(15), o.get("color", AMBER))

    # zones / props
    for z in plan.get("zones", []):
        corners = [tx(*c) for c in rect_corners(z["cx"], z["cz"], z["w"], z["d"], z.get("angle", 0))]
        d.polygon(corners, fill=z.get("fill", TEAL_F), outline=z.get("outline", TEAL))
        d.line(corners + [corners[0]], fill=z.get("outline", TEAL), width=2)
        if z.get("label"):
            px, py = tx(z["cx"], z["cz"])
            text_center(d, px, py, z["label"], font(z.get("font", 15)), z.get("text", INK))

    # player path
    path = plan.get("path", [])
    for i in range(len(path) - 1):
        a, b = tx(*path[i]), tx(*path[i + 1])
        arrow(d, a, b, GREEN, 4)

    # sightlines
    for sl in plan.get("sightlines", []):
        dashed(d, tx(*sl["from"]), tx(*sl["to"]), sl.get("color", (120, 120, 120)), 2)

    # spawns
    for s in plan.get("spawns", []):
        px, py = tx(s["x"], s["z"])
        r = 13
        d.ellipse([px - r, py - r, px + r, py + r], fill=(238, 232, 214), outline=INK, width=2)
        text_center(d, px, py, s["label"], font(13), INK)

    # dimension lines
    for dm in plan.get("dims", []):
        a, b = tx(*dm["p1"]), tx(*dm["p2"])
        d.line([a, b], fill=INK, width=1)
        for end in (a, b):
            d.ellipse([end[0] - 3, end[1] - 3, end[0] + 3, end[1] + 3], fill=INK)
        mx, my = (a[0] + b[0]) / 2, (a[1] + b[1]) / 2
        text_center(d, mx, my - 10, dm["label"], font(14), INK)

    # free labels / callouts (drawn on top)
    for lb in plan.get("labels", []):
        px, py = tx(lb["x"], lb["z"])
        f = font(lb.get("size", 14))
        col = lb.get("color", INK)
        if lb.get("anchor", "center") == "left":
            d.text((px, py), lb["text"], font=f, fill=col)
        else:
            text_center(d, px, py, lb["text"], f, col)

    # scale bar (5 m)
    sbx = MARG["l"]
    sby = H - MARG["b"] + 56
    d.line([(sbx, sby), (sbx + 5 * PPM, sby)], fill=INK, width=4)
    for k in range(6):
        d.line([(sbx + k * PPM, sby - 5), (sbx + k * PPM, sby + 5)], fill=INK, width=2)
    d.text((sbx + 5 * PPM + 10, sby - 9), "5 m", font=font(15), fill=INK)

    # orientation note
    d.text((W - MARG["r"] - 230, H - MARG["b"] + 48),
           "+X 右/宽    +Z 里/卷帘门在顶端",
           font=font(14), fill=INK)

    # title + subtitle
    d.text((MARG["l"], 24), plan.get("title", "HQ 平面图"), font=font(30), fill=INK)
    if plan.get("subtitle"):
        d.text((MARG["l"], 60), plan["subtitle"], font=font(16), fill=(90, 84, 66))

    # legend
    lx = MARG["l"]
    ly = H - MARG["b"] + 96
    d.text((lx, ly - 26), "图例", font=font(16), fill=INK)
    cx = lx
    for color, label in plan.get("legend", []):
        d.rectangle([cx, ly, cx + 22, ly + 18], fill=color, outline=INK)
        d.text((cx + 30, ly), label, font=font(14), fill=INK)
        cx += 34 + text_size(d, label, font(14))[0] + 28
        if cx > W - 320:
            cx = lx
            ly += 30

    img.save(out_path)
    return W, H


# --- demo plan (smoke test only; replaced with the real layout) ------------
def build_demo_plan():
    return {
        "title": "渲染器自检 (DEMO)",
        "subtitle": "smoke test -- not a real layout",
        "shell": [(0, 0), (9, 0), (9, 11), (0, 11)],
        "openings": [{"name": "rollup", "p1": (2.5, 11), "p2": (6.5, 11),
                      "color": AMBER, "label": "卷帘门"}],
        "zones": [
            {"cx": 1.6, "cz": 1.5, "w": 1.7, "d": 0.85, "label": "电脑", "fill": TEAL_F, "outline": TEAL},
            {"cx": 8.0, "cz": 5.5, "w": 0.9, "d": 4.0, "label": "装备", "fill": TEAL_F, "outline": TEAL},
        ],
        "path": [(2.9, 2.3), (7.6, 5.5), (4.5, 9.5)],
        "spawns": [{"label": "1", "x": 2.9, "z": 2.3}, {"label": "2", "x": 7.0, "z": 5.5}],
        "sightlines": [{"from": (2.9, 2.3), "to": (4.5, 10.8)}],
        "dims": [{"p1": (0, -0.7), "p2": (9, -0.7), "label": "9.0 m"}],
        "legend": [(TEAL_F, "工作区"), (GREEN, "动线"), (RED, "上锁门"), (AMBER, "卷帘门/门槛")],
    }


if __name__ == "__main__":
    out = sys.argv[1] if len(sys.argv) > 1 else "hq_floorplan_demo.png"
    w, h = render_floorplan(build_demo_plan(), out)
    print(f"OK wrote {out} ({w}x{h})")
