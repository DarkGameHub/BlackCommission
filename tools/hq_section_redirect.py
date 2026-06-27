#!/usr/bin/env python3
"""
HQ LONGITUDINAL SECTION A-A' v4 — REDIRECT (「废弃火星轨道货运堂」纯室内 / Option B).

WHY THIS EXISTS: the PM built the Mars shell (HqMarsFreightWhitebox.cs) and rejected it —
「这个建筑整体长得有点太丑了」+「我们是不是要突出这是曾经的火星建筑被遗弃的?」. The
art-director (2026-06-24) diagnosed the look + delivered a buildable redirect. THIS drawing
visualizes the redirected FORM so the PM can judge the silhouette BEFORE we rebuild.

THE FIX (vs the rejected build):
  rejected = symmetric apex PLATEAU mid-span (Ridge z6..10.5 both = 13.6) decaying equally to
             both z-ends -> a centered gray-brown LUMP, no direction, no "abandoned" read.
  redirect = a single DIRECTIONAL SWEEP: low south/west haunch (~6 m) rising to ONE crest 13.6 m
             over the NORTH loading door (z~20), then a SHORT sharp fall to a TORN CANTILEVER
             that bursts PAST the door over the backdrop and breaks off (exposed ribs). ALL decay
             (torn prow, missing panels, exposed ribs, crack origin, hanging conduit, regolith
             dust) is CLUSTERED NORTH; the south nest corner stays intact (squatters chose the
             soundest corner). Material = cold pale BLUE-grey, deliberately OUTSIDE the Earth
             concrete/green/wood/rust palette (Mars = alien), in 3 value tiers.

The torn prow cresting over the lit door IS the "former Mars architecture, now abandoned" hero
the PM asked for: the silhouette climax lands on the ritual climax (boarding / departure).

Cut: vertical plane down the dispatch axis (x=4.5), looking EAST (+X); +Z renders RIGHT
(south nest -> north loading door -> backdrop). Heights in FINAL metres (post HqScale 1.2).
Run:  python tools/hq_section_redirect.py   Out: design/hq/HQ_Section_AA_v4_redirect.png
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from PIL import Image, ImageDraw  # noqa: E402

# reuse the v3 section toolkit (transform + primitives) verbatim so the canvas matches
from hq_section_v3_interior import (  # noqa: E402
    W, H, PXM, Z0, Z1, YMAX, ML, MT, MB,
    tz, ty, box, line, dash, arrow, bez, swept_ribbon, blob, light_pool,
    figure, vdim, hdim,
    NESTP, NEST_O, STEEL, STEEL_O, SLAB, SLAB_O, OUTSLAB, STAIN, MOLD, MOLD_D,
    GLOWW, GLOWS, GLOWG, SKYLT, SKYTINT, DARKB, FOGSIL, MARSFOG, DIMSOD,
)
from hq_floorplan_render import font, text_size, INK, PAPER, GREEN, RED, VAN_F  # noqa: E402

# ---- NEW Mars-shell palette: cold pale BLUE-grey, alien, OUTSIDE the Earth palette ----
# three value tiers per the art-director redirect (T1 intact / T2 weathered / T3 exposed structure)
T1 = (158, 172, 194)   # intact cold pale blue-grey (the last Martian gleam)        val ~0.66
T2 = (110, 123, 144)   # weathered shell — the cut solid                            val ~0.47
T3 = (70, 82, 102)     # exposed inner ribs / missing-panel recess — dark blue-steel val ~0.31
T_O = (40, 48, 62)     # shell outline
HAZE = (150, 162, 178)


# ---- before/after silhouette inset (its own mini pixel transform) ----------
def mini_silho(d, x0, y0, x1, y1, top, fill, edge, title_s, tcol, crest_note=None):
    zmn, zmx, ymx = -1.6, 33.6, 14.4
    def mx(z):
        return x0 + (z - zmn) / (zmx - zmn) * (x1 - x0)
    def my(y):
        return y1 - (y / ymx) * (y1 - y0)
    # panel ground + frame
    d.rectangle([x0 - 6, y0 - 22, x1 + 6, y1 + 8], outline=(150, 144, 124), width=1)
    d.line([(mx(zmn), my(0)), (mx(zmx), my(0))], fill=(150, 144, 124), width=1)
    poly = [(mx(z), my(y)) for (z, y) in top] + [(mx(top[-1][0]), my(0)), (mx(top[0][0]), my(0))]
    d.polygon(poly, fill=fill)
    d.line([(mx(z), my(y)) for (z, y) in top], fill=edge, width=2, joint="curve")
    # the tiny warm nest, same in both, to read the dwarf ratio
    d.rectangle([mx(-0.4), my(2.8), mx(6.0), my(0)], fill=(150, 120, 70), outline=(90, 70, 40))
    d.text((x0 - 4, y0 - 20), title_s, font=font(15), fill=tcol)
    if crest_note:
        cz, cy, s = crest_note
        d.text((mx(cz) - text_size(d, s, font(12))[0] / 2, my(cy) - 16), s, font=font(12), fill=tcol)


# ---- the redirected shell geometry (world z,y) — ONE directional sweep -----
# low south haunch -> single north crest 13.6 (over the door) -> short sharp fall to door head
SWEEP = [
    [(-0.6, 5.8), (5.5, 6.7), (11.0, 10.3), (16.0, 12.5)],     # south haunch rises
    [(16.0, 12.5), (18.3, 13.45), (20.0, 13.78), (22.2, 13.5)],  # CREST ~13.6 just inboard of the door
    [(22.2, 13.5), (24.2, 13.0), (25.7, 10.7), (27.0, 8.6)],   # sharp fall to the loading-door head
]
# the TORN CANTILEVER: bursts PAST the north wall over the backdrop, then breaks off
PROW = [
    [(27.0, 8.6), (29.6, 7.7), (31.5, 5.9), (33.3, 4.6)],
]
# top-edge sample for the after-silhouette inset
AFTER_SIL = [(-0.6, 5.8), (5, 6.6), (10, 9.5), (14, 11.8), (18, 13.4), (20, 13.78),
             (22.2, 13.5), (25, 11.0), (27, 8.6), (30, 7.0), (33.3, 4.6)]
# the rejected build: symmetric apex plateau mid-span, decays both ends (the LUMP)
BEFORE_SIL = [(0, 5.6), (3, 11.8), (6, 13.6), (10.5, 13.6), (13, 13.4),
              (17.5, 13.0), (22, 8.4), (27, 5.7)]
# dead cargo conveyor (kept — broken, but pushed to read as wreckage feeding the north tear)
CONV_1 = [[(5.4, 3.0), (10.5, 8.6), (15.5, 9.1), (19.0, 7.6)]]
CONV_2 = [[(20.8, 6.6), (22.4, 5.9), (23.4, 5.3), (24.4, 4.9)]]


def render(out_path):
    img = Image.new("RGB", (W, H), PAPER)
    d = ImageDraw.Draw(img)

    # datum + faint 2 m height grid
    for gy in range(0, 15, 2):
        d.line([(tz(Z0), ty(gy)), (tz(Z1), ty(gy))], fill=(217, 210, 190), width=1)
    for gx in range(int(tz(Z0)), int(tz(Z1)), 16):
        d.line([(gx, ty(-0.05)), (gx - 9, ty(-0.62))], fill=(196, 180, 150), width=1)

    # faint contaminated haze beyond the door
    for i in range(9):
        yb = 0.2 + i * 0.45
        f = max(0.0, 1 - i / 8.0)
        c = tuple(int(PAPER[k] + (SKYTINT[k] - PAPER[k]) * 0.55 * f) for k in range(3))
        d.line([(tz(27.6), ty(yb)), (tz(46.5), ty(yb))], fill=c, width=int(0.55 * PXM) + 1)

    # ======== LIGHT (under everything) ======================================
    light_pool(d, (tz(4.4), ty(2.55)), 2.6, 6.0, 0.02, GLOWW)            # warm nest pool (SOUTH, intact)
    d.ellipse([tz(3.8), ty(1.5), tz(5.0), ty(0.75)], fill=GLOWG)         # CRT phosphor
    # cold daylight now rakes through the NORTH tear / missing panels onto the muster + van
    d.polygon([(tz(23.4), ty(13.0)), (tz(26.6), ty(11.4)), (tz(19.2), ty(0.02)), (tz(15.6), ty(0.02))], fill=SKYLT)
    light_pool(d, (tz(21.4), ty(5.0)), 18.6, 24.2, -0.14, GLOWS)        # interior sodium on the van

    # required dark zones (the doctrine's 暗区) — the south flank stays cave-dark
    for (za, zb) in ((6.6, 9.6), (28.8, 39.8)):
        box(d, za, zb, -0.14, 0.0, DARKB, outline=None, w=0)
        d.text((tz((za + zb) / 2) - 6, ty(0.0) + 8), "暗", font=font(13), fill=(150, 140, 120))

    # ======== BACKDROP beyond the door (fog) — SEEN, NEVER ENTERED ==========
    far = bez((38.5, 0), (38.0, 7.0), (40.5, 8.6), (42.2, 4.6)) + bez((42.2, 4.6), (43.0, 3.0), (44.0, 1.4), (44.6, 0))
    d.polygon(far + [(tz(44.6), ty(0)), (tz(38.5), ty(0))], fill=(176, 184, 192))
    sil = bez((44.0, 0), (43.6, 9.8), (46.0, 11.8), (46.5, 5.6)) + bez((46.5, 5.6), (46.4, 3.0), (46.3, 1.2), (46.5, 0))
    d.polygon(sil + [(tz(46.5), ty(0))], fill=MARSFOG)
    for (zc, hh) in ((44.9, 4.4), (45.7, 5.4), (44.0, 3.4)):
        d.polygon([(tz(zc), ty(0)), (tz(zc - 0.45), ty(hh * 0.45)), (tz(zc), ty(hh)), (tz(zc + 0.45), ty(hh * 0.45))], fill=FOGSIL)

    # ======== FLOOR + GROUND ================================================
    box(d, -0.2, 26.8, -0.18, 0.0, SLAB)
    box(d, 26.8, 43.4, -0.5, -0.18, OUTSLAB)
    d.polygon([(tz(43.4), ty(-0.18)), (tz(46.5), ty(-0.5)), (tz(46.5), ty(-1.0)), (tz(43.4), ty(-0.86))], fill=OUTSLAB, outline=SLAB_O)
    line(d, 26.8, 0.0, 26.8, -0.18, (22, 24, 24), 4)
    box(d, 26.8, 27.2, -0.22, -0.14, (20, 22, 22), outline=None, w=0)
    # abandoned cargo container under the torn cantilever (backdrop)
    cont = [(tz(28.7), ty(-0.12)), (tz(31.6), ty(-0.2)), (tz(31.7), ty(1.72)), (tz(28.8), ty(1.8))]
    d.polygon(cont, fill=(74, 70, 66), outline=(34, 32, 30))
    for rz in (29.3, 29.9, 30.5, 31.1):
        line(d, rz, -0.14, rz, 1.74, (50, 48, 44), 1)
    blob(d, 28.9, 1.74, 0.34, MOLD_D)

    # ======== ENVELOPE END WALLS ============================================
    box(d, -0.7, -0.2, 0.0, 5.8, T2, outline=T_O)                       # south wall — the shell SPRINGS low here
    box(d, 26.6, 27.1, 0.0, 5.7, T2, outline=T_O)                       # north loading-door pier
    box(d, 26.6, 27.1, 4.8, 5.7, T2, outline=T_O)                       # door lintel (opening 4.8 m)

    # ======== THE REDIRECTED MARS SHELL — one swept gesture, crest over the door ====
    swept_ribbon(d, SWEEP, 0.95, T2, hi=T1, out=T_O)
    topP, undP = swept_ribbon(d, PROW, 0.95, T2, hi=T1, out=T_O)

    # --- decay CLUSTERED NORTH (z>21): missing panels, exposed ribs, torn tip ---
    # missing-panel recesses punched into the north flank (dark T3 voids in the skin)
    for (z0p, z1p, ya, yb) in ((22.6, 23.7, 13.1, 11.4), (24.0, 25.0, 12.0, 10.0), (25.4, 26.3, 10.4, 8.4)):
        d.polygon([(tz(z0p), ty(ya)), (tz(z1p), ty(ya - 0.4)), (tz(z1p), ty(yb)), (tz(z0p), ty(yb + 0.4))], fill=T3, outline=T_O)
        for rr in range(3):                                            # exposed inner ribs inside the hole
            zr = z0p + (z1p - z0p) * (0.25 + rr * 0.25)
            line(d, zr, ya - 0.5, zr, yb + 0.5, (52, 62, 80), 2)
    # torn cantilever tip: exposed rebar jutting from the broken edge
    for k in range(6):
        yy = 3.2 + k * 0.24
        line(d, 33.3, yy, 34.2 + 0.13 * k, yy + 0.18, (58, 70, 88), 2)
    # a hanging conduit drooping out of the tear
    hang = bez((30.4, 6.6), (30.8, 4.4), (31.6, 3.6), (32.4, 2.0))
    d.line(hang, fill=(46, 50, 56), width=3, joint="curve")
    d.ellipse([tz(32.3), ty(2.1), tz(32.6), ty(1.8)], fill=(40, 44, 50))
    # Mars regolith / dust drift sifting down through the tear onto the floor
    for k in range(7):
        zx = 24.6 + k * 0.5
        dash(d, zx, 9.6 - 0.3 * k, zx - 0.4, 0.1, (150, 150, 142), w=1, dl=4, gp=9)
    # contamination = VERTICAL gravity streaks (per redirect), clustered north
    for (zs, y0v) in ((22.4, 12.6), (23.6, 11.4), (25.2, 9.6), (26.4, 8.0)):
        dash(d, zs, y0v, zs, y0v - 2.6, STAIN, w=2, dl=5, gp=8)
    blob(d, 24.5, 5.0, 0.5, MOLD_D)
    blob(d, 25.0, 5.2, 0.34, MOLD)
    # SOUTH stays INTACT: clean intact gleam on the south haunch, no streaks
    d.line([bez(SWEEP[0][0], SWEEP[0][1], SWEEP[0][2], SWEEP[0][3])[i] for i in range(0, 91, 3)],
           fill=T1, width=2, joint="curve")

    # ======== leaning Y tree-column (Zaha splayed support) ==================
    def strut(z_base, z_top, y_top, wbase=0.34, wtop=0.16):
        d.polygon([(tz(z_base - wbase), ty(0)), (tz(z_base + wbase), ty(0)),
                   (tz(z_top + wtop), ty(y_top)), (tz(z_top - wtop), ty(y_top))], fill=T2, outline=T_O)
    strut(15.4, 13.8, 12.4)
    strut(15.4, 17.4, 12.2)
    d.line([(tz(15.4), ty(0)), (tz(15.4), ty(3.0))], fill=T_O, width=1)

    # ======== dead cargo conveyor (broken) ==================================
    swept_ribbon(d, CONV_1, 0.55, STEEL, hi=None, out=STEEL_O)
    swept_ribbon(d, CONV_2, 0.55, STEEL, hi=None, out=STEEL_O)
    line(d, 19.6, 7.2, 20.2, 5.4, STEEL, 5)

    # ======== THE AGENCY NEST (warm orthogonal box — SOUTH, intact, NOT enlarged) ===
    box(d, -0.2, 6.0, 2.7, 2.92, NESTP, outline=NEST_O)
    box(d, 5.8, 6.0, 0.0, 2.92, NESTP, outline=NEST_O)
    d.line([(tz(5.9), ty(2.5)), (tz(5.9), ty(0.0))], fill=(150, 120, 70), width=1)
    box(d, 3.6, 5.0, 0.0, 0.74, NESTP, outline=NEST_O)                   # CRT desk
    box(d, 4.0, 4.8, 0.74, 1.42, (40, 44, 42), outline=NEST_O)
    d.rectangle([tz(4.08), ty(1.34), tz(4.72), ty(0.86)], fill=GLOWG)
    box(d, 0.2, 1.2, 1.3, 2.4, None, outline=RED, w=2)                   # debt/takeover board
    d.text((tz(0.0), ty(2.66)), "债务/接管板", font=font(12), fill=RED)

    # ======== gear rack + muster pad ========================================
    box(d, 7.6, 8.7, 0.0, 2.3, None, outline=(120, 122, 118), w=2)
    d.line([(tz(13.4), ty(0.0)), (tz(17.6), ty(0.0))], fill=(120, 122, 118), width=3)

    # ======== loading door (roll-up 4.8) + the VAN (inside) =================
    d.ellipse([tz(25.9), ty(5.5), tz(26.7), ty(4.85)], outline=(40, 42, 40), width=3)
    box(d, 20.0, 23.6, -0.14, 2.0, VAN_F, outline=INK, w=2)
    box(d, 22.9, 23.6, 2.0, 2.62, VAN_F, outline=INK, w=2)
    d.ellipse([tz(20.4), ty(0.06), tz(21.0), ty(-0.5)], fill=(30, 32, 32))
    d.ellipse([tz(22.6), ty(0.06), tz(23.2), ty(-0.5)], fill=(30, 32, 32))
    d.ellipse([tz(23.4), ty(1.5), tz(23.8), ty(1.1)], fill=(255, 238, 190))

    # backdrop sodium lamps (dim props behind the door)
    def sodium_pole(z, h=4.4):
        line(d, z, 0.0, z, h, (44, 46, 44), 3)
        d.line([(tz(z), ty(h)), (tz(z + 0.8), ty(h - 0.12))], fill=(44, 46, 44), width=3)
        d.ellipse([tz(z + 0.55), ty(h - 0.02), tz(z + 1.05), ty(h - 0.5)], fill=DIMSOD, outline=(60, 56, 44))
    sodium_pole(35.0)
    sodium_pole(40.6)

    # ======== human scale ===================================================
    figure(d, 4.0, 0.0, col=(60, 50, 40))      # in the warm nest (south)
    figure(d, 12.8, 0.0)                        # muster, dwarfed
    figure(d, 16.6, 0.0)                        # gathering at the column base
    figure(d, 19.2, -0.14, col=(70, 72, 70))   # boarding the van, under the crest

    # ======== DIMENSIONS (the volume rhythm — the whole point) ==============
    vdim(d, 2.4, 0.0, 2.8, "暖窝 2.8")
    vdim(d, 20.0, 0.0, 13.6, "火星壳脊 13.6", off=0)
    vdim(d, 26.9, 0.0, 4.8, "装卸门 4.8", off=14)
    hdim(d, -0.2, 6.0, -0.66, "暖窝 6 m (南·完好)")
    hdim(d, 6.0, 26.8, -0.66, "货运堂 ~21 m (宽 11 m·见平面)")
    hdim(d, 26.8, 43.0, -0.66, "门外·火星废料场 (背景·不可进入)")

    # crest + tear callouts (the two readings the PM must judge)
    d.line([(tz(20.0), ty(13.78)), (tz(18.6), ty(14.7))], fill=(70, 82, 102), width=1)
    d.text((tz(11.6), ty(14.95)), "单脊 13.6 — 北偏·压在装卸门正上方(扫掠climax=出发climax)", font=font(15), fill=(54, 66, 86))
    d.line([(tz(31.0), ty(5.9)), (tz(31.8), ty(7.2))], fill=(70, 82, 102), width=1)
    d.text((tz(28.4), ty(8.0)), "断裂悬挑·钢筋外露\n=「曾经的火星·被遗弃」", font=font(13), fill=(60, 70, 86))
    d.text((tz(22.2), ty(11.6)), "缺板/外露肋\n污染(北端集中)", font=font(12), fill=(70, 82, 102))

    # ======== TITLE =========================================================
    d.text((ML, 22), "HQ 纵剖面 A–A′ v4 · REDIRECT — 「废弃火星轨道货运堂」(纯室内 / Option B)", font=font(29), fill=INK)
    d.text((ML, 60),
           "修正「太丑」: 对称团块 → 单一方向扫掠(南矮檐6m → 北单脊13.6m压门 → 短促断裂悬挑) · 衰败全集中北端·南窝完好 · 壳=冷蓝灰(刻意脱离地球色板=火星异质·三明度)",
           font=font(15), fill=(96, 90, 70))
    d.text((ML, 84),
           "「突出这是曾经的火星建筑被遗弃的」= 断裂悬挑的钢筋外露端压在被点亮的卷帘门上方; 剪影的高潮 = 仪式的高潮(集结·登车·出发). 曲线=真曲线·builder 分面近似可 runtime 搭.",
           font=font(14), fill=(120, 112, 88))

    # ======== before / after silhouette inset (the case, at a glance) =======
    bx0 = W - MB - 470
    by0 = H - MB + 64
    mini_silho(d, bx0, by0, bx0 + 200, by0 + 96, BEFORE_SIL, (150, 150, 150), (90, 92, 96),
               "拒 · 旧(已建): 对称团块·中部平台·两端等衰", (150, 60, 50),
               crest_note=(8.2, 13.6, "中部平台=团块"))
    mini_silho(d, bx0 + 250, by0, bx0 + 250 + 200, by0 + 96, AFTER_SIL, T2, T_O,
               "荐 · 新(本案): 北向单脊+断裂悬挑·衰败集中北", (54, 100, 70),
               crest_note=(20, 13.78, "脊→断头"))

    # ======== KEYNOTES (left) ===============================================
    keynotes = [
        "① 暖窝(南·完好) — 蹲占者选了最稳的角; 正交·暖·小; 永不放大 = 论点",
        "② 单脊 13.6 — 不再居中; 北偏压在装卸门正上方, 一道方向性手势(非团块)",
        "③ 断裂悬挑 — 壳体扫掠出挑·钢筋外露·折断 = 「曾经的火星·被遗弃」的剪影主角",
        "④ 缺板·外露肋·垂吊管·风积尘 — 衰败全部集中北端(裂缝源=断口)",
        "⑤ 冷蓝灰三明度壳 — 刻意脱离地球混凝土/绿/木/锈色板(火星=异质·非地球水泥)",
        "⑥ 北裂天光 — 污染冷昼光自断口斜泻, 打在集结台与货车上(车=英雄镜头)",
    ]
    ky = H - MB + 168
    d.text((ML, ky - 26), "关键注释 (修正要点)", font=font(16), fill=INK)
    for i, s in enumerate(keynotes):
        d.text((ML, ky + i * 25), s, font=font(14), fill=(48, 50, 46))

    # ======== LEGEND ========================================================
    ly = H - 56
    d.text((ML, ly - 26), "图例", font=font(15), fill=INK)
    items = [
        (T1, "火星壳·完好(T1)"), (T2, "火星壳·风化(T2·剖切)"), (T3, "外露结构/缺板(T3)"),
        (NESTP, "人类暖窝(剖切)"), (GLOWS, "室内钠灯"), (GLOWW, "暖钨灯"), (GLOWG, "CRT磷光"),
        (SKYLT, "北裂污染天光"), (MOLD, "霉斑"), (STAIN, "污染垂流"),
    ]
    cx = ML
    for col, lab in items:
        d.rectangle([cx, ly, cx + 20, ly + 16], fill=col, outline=INK)
        d.text((cx + 26, ly), lab, font=font(13), fill=INK)
        cx += 26 + text_size(d, lab, font(13))[0] + 24
        if cx > W - 380:
            cx = ML
            ly += 26
    sbx, sby = W - 96 - 5 * PXM - 10, H - 50
    d.line([(sbx, sby), (sbx + 5 * PXM, sby)], fill=INK, width=4)
    for k in range(6):
        d.line([(sbx + k * PXM, sby - 5), (sbx + k * PXM, sby + 5)], fill=INK, width=2)
    d.text((sbx + 5 * PXM + 8, sby - 9), "5 m", font=font(14), fill=INK)

    img.save(out_path)
    return W, H


if __name__ == "__main__":
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "design", "hq")
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "HQ_Section_AA_v4_redirect.png")
    w, h = render(path)
    print(f"OK  {path}  ({w}x{h})")
