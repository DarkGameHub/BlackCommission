#!/usr/bin/env python3
"""
HQ WHOLE-SITE plan v-REDIRECT — the PLAN companion to HQ_Section_AA_v4_redirect.

Same redirect the art-director delivered after the PM rejected the shell as「太丑」: the plan
can't show ridge HEIGHT, but it CAN show the two things that make the new form read as a swept,
abandoned Mars machine rather than a centered lump:
  1. the EAST wall bellies OUT then tightens to a narrow prow at the north door (a swept figure,
     not a symmetric tube);
  2. the RIDGE-CREST TRACK (overhead, dashed) DRIFTS EAST as it climbs NORTH — the parametric
     sweep, seen from above (Section P2 lateral lerp 0.30->0.52 by z). The crest lands over the
     loading door.
And all DECAY is clustered at the NORTH end (torn cantilever footprint over the door + missing
panels + exposed ribs + crack origin at the tear), while the SOUTH nest corner stays intact.

Concept locked: broke agency SQUATS in a derelict MARS ORBITAL-FREIGHT DEPOT. ZAHA = MARS. Option
B (interior-only): muster, BOARD, settle all happen UNDER the shell; the roll-up door is a framed
threshold, never walked through; beyond it = NON-WALKABLE Mars-scrapyard backdrop.

Run:  python tools/hq_siteplan_redirect.py     Out: design/hq/HQ_Plan_redirect.png
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hq_floorplan_render import (  # noqa: E402
    render_floorplan, INK, TEAL, TEAL_F, GREEN, RED, AMBER, VAN, PAPER_F,
)

NEST     = (206, 188, 150)
NEST_O   = (170, 150, 112)
WALL     = (40, 42, 40)
BACKDROP = (50, 54, 42)
BACKDROP_O = (36, 40, 30)
PLATE    = (132, 134, 136)
PLATE_O  = (170, 172, 174)
SKYLT    = (198, 210, 200)
SKYLT_O  = (168, 184, 172)
CONTAINER = (84, 76, 62)
CONTAINER_O = (44, 40, 32)
MUSTER   = (231, 217, 191)
MUSTER_O = (198, 178, 148)
CONV     = (96, 100, 110)     # dead conveyor — overhead projection
RIDGE    = (74, 104, 150)     # NEW: ridge-crest track (overhead) — the swept apex line, drifts east
CANT     = (150, 96, 70)      # broken cantilever footprint — overhead, torn out over the door (north)
CRACK    = (150, 168, 170)    # shell crack / skylight — overhead, origin at the north tear
DECAY    = (120, 92, 70)      # north decay cluster hatch
MARSFOG  = (150, 160, 170)
LW       = (236, 196, 120)
LG       = (150, 226, 140)
LS       = (224, 176, 92)
NOTE     = (210, 205, 186)
RNOTE    = (224, 150, 150)
CUT      = (96, 74, 120)
BPACK    = (150, 132, 96)


def build_site_plan():
    return {
        "title": "HQ 站点平面图 v-REDIRECT ·「废弃火星轨道货运堂」(纯室内 / Option B) — 剖面 A–A′(v4 redirect) 的平面companion",
        "subtitle": "修正「太丑」: 东墙鼓腹外扩→北端收窄成尖prow(扫掠图形·非对称管) · 脊线(顶·虚线)随升高东偏=参数化扫掠从上看 · 衰败集中北端·南窝完好 · 卷帘门=框景·门外不可进入",
        "bounds": (-10.6, 19.6, -4.0, 38.0),
        # ===== Mars freight-shell: east wall BELLIES OUT (peak z19) then tightens to the north prow =====
        "shell": [
            (-1.0, -0.6), (10.0, -0.6),
            (10.8, 5.0), (11.8, 10.0), (12.6, 15.0), (12.9, 19.0), (12.2, 22.0), (10.2, 25.0),  # east bulge -> tighten
            (9.0, 27.0), (1.0, 28.2), (-1.0, 27.0),                                              # slanted north loading wall
        ],
        "openings": [
            {"p1": (2.7, 27.95), "p2": (6.7, 27.45), "color": AMBER, "label": ""},   # the ONLY threshold
        ],
        "zones": [
            # ===== NON-PLAYABLE backdrop hugging the building =====
            {"cx": -8.6, "cz": 13.0, "w": 3.4, "d": 30.0, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "死林\n土坡\n背景\n不可\n进入", "font": 12, "text": NOTE},
            {"cx": 17.6, "cz": 13.0, "w": 3.4, "d": 30.0, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "死林\n土坡\n背景\n不可\n进入", "font": 12, "text": NOTE},
            {"cx": 4.5, "cz": -2.6, "w": 28.0, "d": 2.6, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "杂物后场 → 死林 (背景·不可进入)", "font": 12, "text": NOTE},
            # ===== 门外: Mars scrapyard backdrop =====
            {"cx": 4.5, "cz": 33.2, "w": 30.0, "d": 9.6, "fill": (64, 62, 58), "outline": (44, 42, 40),
             "label": "门外 · 火星废料场 (透过卷帘门看得到 · 不可进入)", "font": 14, "text": NOTE},
            {"cx": 9.6, "cz": 36.2, "w": 3.2, "d": 2.6, "fill": MARSFOG, "outline": (120, 132, 144),
             "label": "火星废墟(地标)", "font": 11, "text": (40, 46, 54)},
            {"cx": 0.8, "cz": 31.2, "w": 3.0, "d": 2.0, "angle": 12, "fill": CONTAINER, "outline": CONTAINER_O,
             "label": "废弃货柜", "font": 11, "text": (220, 214, 200)},
            # ===== NORTH DECAY CLUSTER — the abandoned end (torn cantilever / missing panels / exposed ribs) =====
            {"cx": 4.5, "cz": 26.0, "w": 11.6, "d": 3.2, "fill": DECAY, "outline": (90, 68, 50),
             "label": "北端 = 废弃集中区\n断裂悬挑·缺板·外露肋·裂缝源", "font": 12, "text": (236, 222, 206)},
            # ===== INTERIOR loading bay + van (boarded INSIDE) =====
            {"cx": 4.5, "cz": 21.0, "w": 4.4, "d": 7.4, "fill": PLATE, "outline": PLATE_O, "label": ""},
            {"cx": 4.5, "cz": 21.0, "w": 2.3, "d": 5.2, "fill": VAN, "outline": (210, 210, 210),
             "label": "货车", "font": 13, "text": (240, 240, 240)},
            # ===== PLAYABLE interior — agency NEST (orthogonal, SW corner, INTACT) =====
            {"cx": 2.5, "cz": 3.1, "w": 6.6, "d": 7.0, "fill": NEST, "outline": NEST_O, "label": ""},
            {"cx": 1.6, "cz": 6.6, "w": 4.8, "d": 0.4, "fill": WALL, "outline": INK, "label": ""},
            {"cx": 1.4, "cz": 0.5, "w": 1.7, "d": 0.85, "fill": TEAL_F, "outline": TEAL,
             "label": "电脑/CRT\n选任务·结算", "font": 12},
            {"cx": 0.2, "cz": 2.4, "w": 0.35, "d": 1.8, "fill": RED, "outline": INK, "label": ""},
            {"cx": 4.6, "cz": 3.0, "w": 1.1, "d": 0.9, "fill": PAPER_F, "outline": (150, 140, 115),
             "label": "折叠桌", "font": 10, "text": (110, 100, 80)},
            # ===== gear wall + (provisional) backpack rack =====
            {"cx": 0.0, "cz": 10.2, "w": 0.9, "d": 3.2, "fill": TEAL_F, "outline": TEAL,
             "label": "装备墙\n半空", "font": 12},
            {"cx": 1.7, "cz": 10.2, "w": 1.0, "d": 2.0, "fill": BPACK, "outline": (110, 96, 68),
             "label": "背包架\n待定", "font": 11, "text": (60, 52, 36)},
            # ===== muster pad — now lit by daylight from the NORTH tear (crack origin moved north) =====
            {"cx": 4.5, "cz": 16.0, "w": 5.0, "d": 5.0, "fill": SKYLT, "outline": SKYLT_O,
             "label": "", "font": 11},
            {"cx": 4.5, "cz": 16.0, "w": 3.2, "d": 2.4, "fill": MUSTER, "outline": MUSTER_O,
             "label": "集结台", "font": 12, "text": (110, 100, 80)},
            {"cx": 5.5, "cz": 15.4, "w": 0.7, "d": 0.7, "angle": 45, "fill": (118, 124, 134), "outline": INK,
             "label": "", "font": 10},
            # ===== LIGHT ANCHORS (interior only) =====
            {"cx": 1.4, "cz": 1.4, "w": 0.5, "d": 0.5, "fill": LW, "outline": INK, "label": ""},
            {"cx": 1.0, "cz": 0.6, "w": 0.4, "d": 0.4, "fill": LG, "outline": INK, "label": ""},
            {"cx": 5.4, "cz": 23.6, "w": 0.55, "d": 0.55, "fill": SKYLT_O, "outline": INK, "label": ""},  # north tear daylight
            {"cx": 7.2, "cz": 21.4, "w": 0.6, "d": 0.6, "fill": LS, "outline": INK, "label": ""},
        ],
        "labels": [
            {"x": -0.6, "z": 5.6, "text": "暖窝(正交·暖·南端完好)", "color": (120, 96, 50), "size": 12, "anchor": "left"},
            {"x": 7.0, "z": 27.4, "text": "货运卷帘门[E]·高4.8m·门=框景", "color": (150, 95, 30), "size": 12, "anchor": "left"},
            {"x": 6.4, "z": 19.0, "text": "装卸湾·钠灯(室内)", "color": (235, 230, 220), "size": 11, "anchor": "left"},
            {"x": 13.1, "z": 18.8, "text": "火星扫掠壳\n东墙鼓腹(峰 z≈19)", "color": (70, 80, 92), "size": 12, "anchor": "left"},
            {"x": 5.0, "z": 12.6, "text": "脊线(顶·虚线): 升高时东偏 = 扫掠", "color": (60, 88, 138), "size": 12, "anchor": "left"},
            {"x": 6.8, "z": 16.8, "text": "集结台·北裂天光落点", "color": (110, 110, 96), "size": 11, "anchor": "left"},
            {"x": 8.4, "z": 23.6, "text": "返程: 车回装卸湾→\n步行回 CRT 结算", "color": RNOTE, "size": 12, "anchor": "left"},
            {"x": 3.0, "z": -2.0, "text": "A", "color": (150, 120, 180), "size": 20, "anchor": "left"},
            {"x": 2.2, "z": 34.6, "text": "A′", "color": (150, 120, 180), "size": 20, "anchor": "left"},
        ],
        "path": [(1.4, 1.2), (4.6, 6.6), (1.4, 10.4), (4.0, 16.0), (4.0, 20.2)],
        "sightlines": [
            # section cut line A–A'
            {"from": (4.5, -2.2), "to": (4.5, 0.4), "color": CUT},
            {"from": (4.5, 28.6), "to": (4.5, 33.2), "color": CUT},
            # ===== RIDGE-CREST TRACK (overhead) — drifts EAST as it climbs NORTH = the sweep =====
            {"from": (2.3, 0.0), "to": (2.7, 6.0), "color": RIDGE},
            {"from": (2.7, 6.0), "to": (3.2, 12.0), "color": RIDGE},
            {"from": (3.2, 12.0), "to": (3.7, 18.0), "color": RIDGE},
            {"from": (3.7, 18.0), "to": (4.1, 23.0), "color": RIDGE},
            {"from": (4.1, 23.0), "to": (4.3, 27.4), "color": RIDGE},   # crest lands over the door
            # dead conveyor — overhead sweep projection
            {"from": (2.8, 5.0), "to": (5.6, 10.2), "color": CONV},
            {"from": (5.6, 10.2), "to": (7.6, 15.2), "color": CONV},
            {"from": (7.6, 15.2), "to": (6.6, 20.2), "color": CONV},
            {"from": (6.6, 20.2), "to": (4.9, 23.6), "color": CONV},
            # broken cantilever — overhead footprint, torn out OVER the door (north)
            {"from": (1.6, 27.2), "to": (1.9, 32.6), "color": CANT},
            {"from": (7.2, 26.8), "to": (7.4, 32.4), "color": CANT},
            {"from": (1.9, 32.6), "to": (7.4, 32.4), "color": CANT},
            # shell crack / skylight — origin at the NORTH tear, rakes SW down onto the muster
            {"from": (5.6, 25.4), "to": (4.4, 16.4), "color": CRACK},
            # return route (stamp-red): door → bay → walk back → debt board → CRT (settle)
            {"from": (4.5, 27.4), "to": (5.3, 21.0), "color": RED},
            {"from": (5.3, 21.0), "to": (5.3, 7.6), "color": RED},
            {"from": (5.3, 7.6), "to": (0.5, 2.6), "color": RED},
            {"from": (0.5, 2.6), "to": (1.4, 1.2), "color": RED},
        ],
        "spawns": [
            {"label": "1", "x": 1.6, "z": 2.4}, {"label": "2", "x": 3.0, "z": 2.4},
            {"label": "3", "x": 1.6, "z": 4.2}, {"label": "4", "x": 3.0, "z": 4.2},
        ],
        "dims": [
            {"p1": (-1.0, -1.6), "p2": (10.0, -1.6), "label": "南端宽 ~11 m"},
            {"p1": (-1.0, 19.0), "p2": (12.9, 19.0), "label": "鼓腹最宽 ~13.9 m"},
            {"p1": (15.0, -0.6), "p2": (15.0, 27.0), "label": "货堂深 ~27 m (纯室内)"},
        ],
        "legend": [
            (NEST, "暖窝(正交·南·完好)"), (TEAL_F, "工位(CRT/装备)"), (BPACK, "背包架(待定)"),
            (GREEN, "出发动线(止于登车)"), (RED, "返程动线(回CRT结算)"), (CUT, "剖切线 A–A′"),
            (RIDGE, "脊线(顶·东偏=扫掠)"), (CONV, "死货运廊(投影)"), (CANT, "断裂悬挑(投影·北)"),
            (CRACK, "壳裂缝(北端·投影)"), (DECAY, "北端废弃集中区"), (PLATE, "室内装卸湾=装车位"),
            (AMBER, "卷帘门(框景)"), (SKYLT, "天光落点"), (MARSFOG, "火星废墟地标"), ((64, 62, 58), "门外背景·不可进入"),
        ],
    }


if __name__ == "__main__":
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "design", "hq")
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "HQ_Plan_redirect.png")
    w, h = render_floorplan(build_site_plan(), path)
    print(f"OK  {path}  ({w}x{h})")
