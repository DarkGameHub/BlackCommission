#!/usr/bin/env python3
"""
HQ LONGITUDINAL SECTION A-A' renderer (Municipal Debt Noir).

WHY a section and not another floor plan:
  design/art/spatial-language-spec.md (PM-locked 2026-06-10) principle #1 —
  "玩家看不到平面图——他们读的是截面：宽度、天花高度、顶部轮廓、门洞比例、光的种类。
   几何语法先于道具语言。" The player reads the SECTION, never the plan. Three prior
  top-down site-plans were rejected as "没有设计感" precisely because a plan cannot show
  ceiling compression (压顶), volume rhythm (体积韵律), threshold proportion, or the
  light-and-dark pattern that ARE the spatial identity. This draws the HQ in section.

SENSE OF PLACE (adaptive reuse): the office squats in a FORECLOSED MUNICIPAL VEHICLE-
  INSPECTION / WEIGH SUBSTATION. Native fixtures earn the dispatch-and-return ritual for
  free: an inspection lane, an overhead services bundle, a low-wide roll-up, an apron
  canopy, a vehicle gate with a height-clearance bar, an old pylon road-sign. The broke
  office is the tenant; the civic bones are the landlord. Wear tells the foreclosure story.

Section cut: vertical plane down the dispatch axis (x = 4.5), looking EAST (+X).
  +Z renders to the RIGHT (south office -> north gate). All heights in FINAL metres
  (post HqScale 1.2). Run:  python tools/hq_section_render.py
  Out: design/hq/HQ_Section_AA_v1.png
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw  # noqa: E402
from hq_floorplan_render import (  # noqa: E402
    font, text_size, INK, PAPER, GREEN, RED, AMBER, VAN, VAN_F,
)

# ---- section-specific palette ---------------------------------------------
POCHE   = (66, 69, 64)     # cut solid (concrete / block / steel)  — the poché
POCHE_O = (24, 26, 24)     # poché outline
SLAB    = (54, 57, 52)     # floor / apron slab (slightly darker)
SERVICE = (52, 54, 58)     # tenant's hung services bundle (the 压顶 element)
CANOPY  = (74, 70, 60)     # apron canopy slab + posts
BEYOND  = (120, 132, 128)  # projected "beyond the cut" linework (desk/gear/board)
BEYOND2 = (150, 160, 156)
GLOWW   = (243, 214, 150)  # warm tungsten pool (desk / muster)
GLOWS   = (240, 200, 132)  # sodium pool (threshold / van bay / gate)
GLOWG   = (168, 214, 156)  # CRT phosphor green
DARKB   = (214, 208, 191)  # subtle "dark zone" band (darker than paper)
FOGSIL  = (132, 138, 128)  # fog-dissolved backdrop silhouette
FOGSIL2 = (112, 119, 110)
DIMCOL  = (60, 62, 58)
EARTH   = (150, 134, 104)

# ---- view transform --------------------------------------------------------
PXM = 40
Z0, Z1 = -2.0, 46.0
YMIN, YMAX = -0.7, 6.7
ML, MT, MRG, MB = 162, 176, 84, 286
W = int(ML + (Z1 - Z0) * PXM + MRG)
H = int(MT + (YMAX - YMIN) * PXM + MB)


def tz(z):
    return ML + (z - Z0) * PXM


def ty(y):
    return MT + (YMAX - y) * PXM


def box(d, z0, z1, y0, y1, fill, outline=POCHE_O, w=2):
    x0, x1 = tz(z0), tz(z1)
    py0, py1 = ty(y1), ty(y0)   # y1 is top
    d.rectangle([min(x0, x1), py0, max(x0, x1), py1], fill=fill, outline=outline, width=w)


def line(d, z0, y0, z1, y1, col, w=2):
    d.line([tz(z0), ty(y0), tz(z1), ty(y1)], fill=col, width=w)


def dash(d, z0, y0, z1, y1, col, w=2, dl=10, gp=7):
    a = (tz(z0), ty(y0))
    b = (tz(z1), ty(y1))
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
    """vertical dimension line with ticks + label, drawn at depth z (+off px to the right)."""
    x = tz(z) + off
    d.line([(x, ty(y0)), (x, ty(y1))], fill=DIMCOL, width=1)
    for yy in (y0, y1):
        d.line([(x - 5, ty(yy)), (x + 5, ty(yy))], fill=DIMCOL, width=1)
    f = font(13)
    tw, _ = text_size(d, s, f)
    d.rectangle([x - tw / 2 - 3, (ty(y0) + ty(y1)) / 2 - 9, x + tw / 2 + 3, (ty(y0) + ty(y1)) / 2 + 9], fill=PAPER)
    d.text((x - tw / 2, (ty(y0) + ty(y1)) / 2 - 8), s, font=f, fill=DIMCOL)


def hdim(d, z0, z1, y, s):
    d.line([(tz(z0), ty(y)), (tz(z1), ty(y))], fill=DIMCOL, width=1)
    for zz in (z0, z1):
        d.line([(tz(zz), ty(y) - 5), (tz(zz), ty(y) + 5)], fill=DIMCOL, width=1)
    f = font(13)
    tw, _ = text_size(d, s, f)
    cx = (tz(z0) + tz(z1)) / 2
    d.rectangle([cx - tw / 2 - 3, ty(y) - 9, cx + tw / 2 + 3, ty(y) + 9], fill=PAPER)
    d.text((cx - tw / 2, ty(y) - 8), s, font=f, fill=DIMCOL)


def light_pool(d, apex, z_l, z_r, y_floor, col):
    """soft light wedge from a fixture apex down to a floor pool + floor glow ellipse."""
    d.polygon([apex, (tz(z_l), ty(y_floor)), (tz(z_r), ty(y_floor))], fill=col)
    cx = (tz(z_l) + tz(z_r)) / 2
    d.ellipse([tz(z_l), ty(y_floor) - 5, tz(z_r), ty(y_floor) + 7], fill=col)


def figure(d, z, y_floor, h=1.8, col=(40, 42, 40)):
    """1.8 m human scale figure (head + body + legs)."""
    x = tz(z)
    head_r = 0.13 * PXM
    hy = ty(y_floor + h)
    d.ellipse([x - head_r, hy, x + head_r, hy + 2 * head_r], fill=col)
    d.line([(x, hy + 2 * head_r), (x, ty(y_floor + 0.62 * h))], fill=col, width=4)        # torso
    d.line([(x, ty(y_floor + 0.78 * h)), (x - 0.18 * PXM, ty(y_floor + 0.58 * h))], fill=col, width=3)
    d.line([(x, ty(y_floor + 0.78 * h)), (x + 0.18 * PXM, ty(y_floor + 0.6 * h))], fill=col, width=3)
    d.line([(x, ty(y_floor + 0.62 * h)), (x - 0.15 * PXM, ty(y_floor))], fill=col, width=3)  # legs
    d.line([(x, ty(y_floor + 0.62 * h)), (x + 0.15 * PXM, ty(y_floor))], fill=col, width=3)


def tag(d, z, y, n, to=None):
    """circled keynote number with an optional short leader to its feature."""
    x, py = tz(z), ty(y)
    if to:
        d.line([(x, py), (tz(to[0]), ty(to[1]))], fill=(120, 122, 118), width=1)
    r = 11
    d.ellipse([x - r, py - r, x + r, py + r], fill=PAPER, outline=INK, width=2)
    f = font(14)
    tw, th = text_size(d, str(n), f)
    d.text((x - tw / 2, py - th / 2 - 1), str(n), font=f, fill=INK)


def render(out_path):
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)

    # ---- datum + faint height grid every 1 m -------------------------------
    for gy in range(0, 7):
        d.line([(tz(Z0), ty(gy)), (tz(Z1), ty(gy))], fill=(216, 209, 189), width=1)
    # ground hatch below grade
    for gx in range(int(tz(Z0)), int(tz(Z1)), 16):
        d.line([(gx, ty(-0.05)), (gx - 9, ty(-0.55))], fill=(196, 180, 150), width=1)

    # ======== LIGHT POOLS (under everything — the noir pattern) =============
    # warm desk-lamp pool over the CRT
    light_pool(d, (tz(5.6), ty(2.25)), 4.2, 7.4, 0.02, GLOWW)
    # CRT phosphor green (small, at screen height)
    d.ellipse([tz(5.2), ty(1.45), tz(6.4), ty(0.7)], fill=GLOWG)
    # the ONE live fluorescent over the muster pad
    light_pool(d, (tz(15.6), ty(2.36)), 13.6, 17.8, 0.02, (236, 224, 188))
    # sodium spills UNDER the rolled-up door (cold amber threshold)
    d.polygon([(tz(17.4), ty(3.0)), (tz(19.6), ty(-0.13)), (tz(16.0), ty(-0.13))], fill=GLOWS)
    # sodium pool under the canopy, on the van
    light_pool(d, (tz(22.0), ty(3.18)), 19.6, 24.6, -0.13, GLOWS)
    # sodium at the gate
    light_pool(d, (tz(43.0), ty(3.5)), 40.5, 45.0, -0.2, GLOWS)

    # ======== subtle DARK ZONES (the doctrine's required 暗区) ==============
    for (za, zb) in ((7.6, 13.4), (25.0, 39.5)):
        box(d, za, zb, -0.13, 0.0, DARKB, outline=None, w=0)
        d.text((tz((za + zb) / 2) - 6, ty(0.0) + 8), "暗", font=font(13), fill=(150, 140, 120))

    # ======== BACKDROP beyond the gate (fog silhouettes) ====================
    # old municipal pylon road-sign (the orienting landmark on return)
    box(d, 45.0, 45.5, 0, 6.3, FOGSIL2, outline=None, w=0)
    box(d, 44.2, 46.0, 5.4, 6.3, FOGSIL2, outline=None, w=0)
    # dead treeline silhouettes
    for (zc, hh) in ((44.8, 4.4), (45.6, 5.2)):
        d.polygon([(tz(zc), ty(0)), (tz(zc - 0.5), ty(hh * 0.45)), (tz(zc), ty(hh)),
                   (tz(zc + 0.5), ty(hh * 0.45))], fill=FOGSIL)

    # ======== POCHE: the cut civic shell ====================================
    # floor / lane slab (datum 0) with the chained inspection-pit recess
    box(d, -0.0, 10.0, -0.35, 0.0, SLAB)
    box(d, 10.0, 13.0, -0.78, -0.45, SLAB)          # inspection pit (recessed)
    box(d, 13.0, 18.0, -0.35, 0.0, SLAB)
    # exterior apron (steps down 0.15 over the door sill), gentle fall to a drain, then road ramp
    box(d, 18.0, 44.0, -0.5, -0.15, SLAB)
    d.polygon([(tz(44.0), ty(-0.15)), (tz(46.0), ty(-0.5)), (tz(46.0), ty(-0.95)), (tz(44.0), ty(-0.85))], fill=SLAB, outline=POCHE_O)

    # south (back) wall + the office structural soffit (3.42 m bays)
    box(d, -0.4, 0.0, 0.0, 3.62, POCHE)
    box(d, 0.0, 18.2, 3.42, 3.62, POCHE)            # roof/soffit slab
    # tenant's hung services bundle = the 压顶 over the central walkway (cable tray + duct + dead tubes)
    box(d, 2.2, 17.2, 2.40, 2.62, SERVICE)
    for zt in (4.5, 8.0, 11.5, 15.0):               # dead fluorescent tubes hung from it
        cross = RED if zt != 15.0 else None
        d.rectangle([tz(zt - 0.45), ty(2.40), tz(zt + 0.45), ty(2.34)], fill=(60, 62, 60))
        if cross:                                    # dead = crossed
            d.line([(tz(zt - 0.4), ty(2.34)), (tz(zt + 0.4), ty(2.40))], fill=(150, 60, 50), width=1)

    # the roll-up door: lintel + coiled drum + jambs (opening 3.12 m, low & WIDE)
    box(d, 17.9, 18.3, 0.0, 3.12, POCHE)            # far jamb (north wall pier, beyond)
    box(d, 17.9, 18.3, 3.12, 3.62, POCHE)           # lintel over opening
    d.ellipse([tz(17.4), ty(3.42), tz(18.1), ty(2.9)], outline=(40, 42, 40), width=3)  # coiled door drum
    d.arc([tz(17.4), ty(3.42), tz(18.1), ty(2.9)], 0, 360, fill=(70, 72, 70), width=2)

    # apron canopy (the outdoor-room ceiling over the van bay) — soffit 3.30, on posts
    box(d, 18.6, 26.0, 3.30, 3.52, CANOPY)
    box(d, 19.0, 19.3, -0.15, 3.30, CANOPY)
    box(d, 25.4, 25.7, -0.15, 3.30, CANOPY)

    # the VAN — the one cared-for asset — parked nose-in under the canopy
    box(d, 20.2, 23.8, -0.15, 2.10, VAN_F, outline=INK, w=2)
    box(d, 23.2, 23.8, 2.10, 2.68, VAN_F, outline=INK, w=2)   # cab/roof box -> 2.83 overall
    d.ellipse([tz(20.5), ty(0.05), tz(21.1), ty(-0.5)], fill=(30, 32, 32))    # wheel
    d.ellipse([tz(22.7), ty(0.05), tz(23.3), ty(-0.5)], fill=(30, 32, 32))
    d.ellipse([tz(20.0), ty(1.5), tz(20.4), ty(1.1)], fill=(255, 238, 190))   # warm headlight

    # gate clearance bar (vehicle height limit) + closed gate hint at z 44
    box(d, 43.6, 44.4, 2.9, 3.1, POCHE)             # clearance bar (cut)
    dash(d, 44.0, 0.0, 44.0, 2.4, (60, 62, 58), w=2)   # closed gate leaf (beyond)
    box(d, 43.9, 44.1, 3.1, 3.4, POCHE)             # gate post stub

    # ======== BEYOND-THE-CUT projected linework (elevation behind the plane) =
    # debt / takeover board on the west wall
    box(d, 3.0, 4.0, 1.2, 2.25, None, outline=BEYOND, w=2)
    d.line([(tz(3.0), ty(2.25)), (tz(4.0), ty(1.2))], fill=BEYOND2, width=1)
    # CRT job desk + monitor
    box(d, 5.0, 6.6, 0.0, 0.78, None, outline=BEYOND, w=2)
    box(d, 5.5, 6.3, 0.78, 1.4, None, outline=BEYOND, w=2)
    # gear wall (half-empty rack)
    box(d, 11.3, 12.4, 0.0, 2.4, None, outline=BEYOND, w=2)
    for yy in (0.6, 1.2, 1.8):
        d.line([(tz(11.3), ty(yy)), (tz(12.4), ty(yy))], fill=BEYOND2, width=1)
    # muster pad outline on the floor
    d.line([(tz(14.6), ty(0.0)), (tz(17.4), ty(0.0))], fill=BEYOND, width=3)

    # ======== ENFILADE sightline: spawn eye -> through door -> van ===========
    eye = (tz(1.8), ty(1.68))
    d.ellipse([eye[0] - 5, eye[1] - 5, eye[0] + 5, eye[1] + 5], outline=GREEN, width=2)
    dash(d, 1.8, 1.68, 22.0, 1.25, GREEN, w=2, dl=12, gp=8)
    arrow(d, (tz(20.0), ty(1.3)), (tz(22.0), ty(1.25)), GREEN, w=2, head=9)

    # ======== human scale figures ===========================================
    figure(d, 16.0, 0.0)                 # at the muster (clear scale read)
    figure(d, 19.4, -0.13, col=(70, 72, 70))   # stepping out under the door (faint)

    # ======== DIMENSIONS ====================================================
    vdim(d, 0.7, 0.0, 3.42, "层高 3.42")
    vdim(d, 9.4, 0.0, 2.40, "压顶 2.40")
    vdim(d, 18.95, -0.15, 3.12, "卷帘 3.12")
    vdim(d, 26.6, -0.15, 3.30, "雨棚 3.30")
    vdim(d, 24.0, -0.15, 2.83, "货车 2.83", off=0)
    vdim(d, 44.9, -0.2, 3.0, "限高 3.0")
    hdim(d, 0.0, 9.0, -0.62, "办公室 9.0 m")
    hdim(d, 18.0, 44.0, -0.62, "调度院 26 m(门→院门)")

    # ======== KEYNOTE TAGS (circled numbers + short leaders) ================
    tags = [
        (1, 2.7, 1.95, (3.7, 2.35)), (2, 5.9, 1.78, (5.85, 1.42)),
        (3, 11.85, 2.78, (11.85, 2.4)), (4, 16.0, 2.86, (16.0, 2.36)),
        (5, 18.1, 3.42, (18.1, 3.12)), (6, 22.0, 3.74, (22.0, 3.52)),
        (7, 23.0, 2.30, (23.2, 2.1)), (8, 44.0, 2.45, (44.0, 2.9)),
        (9, 9.2, 2.96, (9.2, 2.62)),
    ]
    for n, z, y, to in tags:
        tag(d, z, y, n, to)
    # small un-numbered scene notes
    label(d, 7.2, -0.92, "旧验车坑(锁链围挡)=「曾验车」历史·无需动线", (120, 95, 60), 12, leader=(tz(11.4), ty(-0.62)))
    label(d, 28.5, 5.5, "开阔院面=释放(暗)·返程减压步道", (120, 110, 80), 13)
    label(d, 40.0, 6.15, "死树线+雾+旧站牌=封闭地平线/返程地标", (90, 96, 86), 12, leader=(tz(45.0), ty(5.4)))
    label(d, 1.7, 1.12, "可视轴线: 出生即见货车(穿门框)", GREEN, 12)

    # ======== TITLE =========================================================
    d.text((ML, 22), "HQ 纵剖面 A–A′ — 「弃用的市政验车站」适应性改造", font=font(30), fill=INK)
    d.text((ML, 60), "Municipal Debt Noir · 办公室=房客, 市政骨架=房东 · 体积韵律(窄→宽)·压顶(2.40)·光锚·磨损叙事 · 依 spatial-language-spec 第1原则「玩家读截面」", font=font(15), fill=(96, 90, 70))

    # ======== KEYNOTES (bottom margin, two columns) =========================
    keynotes = [
        "① 出生区 — 暖台灯角落; 低·暗·亲密的破办公室一角",
        "② 电脑/CRT — 选任务·结算; 磷光绿=全场唯一「活信号」色",
        "③ 装备墙 — 起手半空, 花钱才慢慢填满(经济幻想写进空间)",
        "④ 集结台 — 唯一一盏活日光灯下=「集合」节拍",
        "⑤ 卷帘门[E] — 低而宽; 手揭幕=压缩→释放的门槛戏",
        "⑥ 雨棚 — 院子的「天花」; 货车停进有顶的室外房间(非露天草地)",
        "⑦ 货车 — 全场唯一保养良好之物(暖前照灯)",
        "⑧ 出院门6m — 常闭·无[E]·限高杆=车门; 玩家跟不出去",
        "⑨ 压顶2.40 — 房客私接线槽/风管/坏灯管=走廊压迫(非原结构)",
    ]
    ky = H - MB + 22
    d.text((ML, ky - 26), "关键注释", font=font(16), fill=INK)
    colw = (W - ML - MRG) / 2
    for i, s in enumerate(keynotes):
        col = 0 if i < 5 else 1
        row = i if i < 5 else i - 5
        d.text((ML + col * colw, ky + row * 25), s, font=font(14), fill=(48, 50, 46))

    # ======== LEGEND + SCALE (very bottom) ==================================
    ly = H - 64
    d.text((ML, ly - 26), "图例", font=font(15), fill=INK)
    items = [
        (POCHE, "剖切实体(混凝土/砌块/钢)"), (BEYOND, "剖面后方投影(电脑/装备/债务板)"),
        (GLOWS, "钠灯光锚"), (GLOWW, "暖钨光锚"), (GLOWG, "CRT磷光绿"),
        (GREEN, "可视轴线"), (DARKB, "暗区(必留)"), (FOGSIL, "雾中背景剪影"),
    ]
    cx = ML
    for col, lab in items:
        d.rectangle([cx, ly, cx + 20, ly + 16], fill=col, outline=INK)
        d.text((cx + 26, ly), lab, font=font(13), fill=INK)
        cx += 26 + text_size(d, lab, font(13))[0] + 28
        if cx > W - 340:
            cx = ML
            ly += 26
    sbx, sby = W - MRG - 5 * PXM - 10, H - 58
    d.line([(sbx, sby), (sbx + 5 * PXM, sby)], fill=INK, width=4)
    for k in range(6):
        d.line([(sbx + k * PXM, sby - 5), (sbx + k * PXM, sby + 5)], fill=INK, width=2)
    d.text((sbx + 5 * PXM + 8, sby - 9), "5 m", font=font(14), fill=INK)

    img.save(out_path)
    return W, H


if __name__ == "__main__":
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "design", "hq")
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "HQ_Section_AA_v1.png")
    w, h = render(path)
    print(f"OK  {path}  ({w}x{h})")
