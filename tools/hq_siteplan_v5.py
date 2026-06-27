#!/usr/bin/env python3
"""
HQ WHOLE-SITE plan (2026-06-24) — the PLAN companion to the finalized freight-hall SECTION.

Concept locked: the broke agency SQUATS in a derelict, contaminated MARS ORBITAL-FREIGHT
DEPOT (「货运堂」). ZAHA = MARS — the freight-hall envelope is a swept, fluid Martian shell
(here: a faceted curved EAST wall + a slanted north loading wall, runtime-buildable, no NURBS);
the agency's NEST is a small ORTHOGONAL warm box bolted into the SW corner (anti-Zaha, human).

OPTION B (interior-only, LOCKED 2026-06-24): the WHOLE ritual happens UNDER the shell — muster,
BOARD the van, and settle are all INSIDE. The freight roll-up door is a THRESHOLD / framed
picture, not a path. The old playable dispatch yard (apron + perimeter walls + vehicle gate)
is DELETED; beyond the door is now a NON-WALKABLE Mars-scrapyard BACKDROP, seen through the
open door, never entered. Companion to SECTION design/hq/HQ_Section_AA_v3.png (apex 13.6 m,
hall width 11 m, depth ~27 m, door 4.8 m — sizes finalized after an enlargement trial was
pulled back). The nest stays tiny and unchanged; a vast shell over a small nest IS the thesis.

The PLAN carries what the section cannot — the X/Z footprint that drives the builder + NavMesh:
  * the swept Mars envelope + the orthogonal nest in its corner (figure-ground = the thesis)
  * the interior dispatch spine: nest → CRT → gear → muster → loading-bay VAN (board here, end of walk)
  * the dead conveyor + broken cantilever + skylight crack as OVERHEAD PROJECTIONS (the curves, in plan)
  * the loading door as the only threshold; the Mars-scrapyard backdrop beyond it (incl. the ruin landmark)
  * the A–A′ cut line cross-referenced to the section drawing.

Run:  python tools/hq_siteplan_v5.py     Out: design/hq/HQ_SitePlan_v5.png
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hq_floorplan_render import (  # noqa: E402
    render_floorplan, INK, TEAL, TEAL_F, GREEN, RED, AMBER, VAN, VAN_F, PAPER_F,
)

NEST     = (206, 188, 150)   # the agency nest — warm orthogonal box floor (anti-Zaha)
NEST_O   = (170, 150, 112)
WALL     = (40, 42, 40)       # cut walls (nest front, perimeter)
MARSW    = (118, 124, 134)    # Mars shell column / pier (cool cut solid)
ASPHALT  = (120, 115, 103)    # loading apron — PLAYABLE
ASPHALT_O = (150, 144, 130)
BACKDROP = (50, 54, 42)       # NON-PLAYABLE dead woods + berm hugging the compound
BACKDROP_O = (36, 40, 30)
TREE     = (60, 66, 50)
TREE_O   = (46, 50, 38)
DIRT     = (170, 150, 112)    # the one opening north — van's dirt road out
DIRT_O   = (140, 120, 90)
PLATE    = (132, 134, 136)    # loading-bay deck (diegetic van bay marker)
PLATE_O  = (170, 172, 174)
SKYLT    = (198, 210, 200)    # contaminated daylight from the shell crack (floor landing)
SKYLT_O  = (168, 184, 172)
CONTAINER = (84, 76, 62)      # abandoned cargo container (Mars walked away)
CONTAINER_O = (44, 40, 32)
MUSTER   = (231, 217, 191)
MUSTER_O = (198, 178, 148)
CONV     = (96, 100, 110)     # dead conveyor — overhead projection
CANT     = (120, 110, 86)     # broken cantilever — overhead projection
CRACK    = (150, 168, 170)    # shell crack / skylight slot — overhead projection
MARSFOG  = (150, 160, 170)    # distant Mars-ruin landmark beyond the gate
LW       = (236, 196, 120)    # warm tungsten anchor (nest)
LG       = (150, 226, 140)    # CRT phosphor anchor
LS       = (224, 176, 92)     # sodium anchor (bay / apron / gate)
PYLONP   = (70, 72, 66)
NOTE     = (210, 205, 186)
RNOTE    = (224, 150, 150)
CUT      = (96, 74, 120)       # section cut line A–A'
BPACK    = (150, 132, 96)      # backpack rack (PROVISIONAL — pending PM go on the backpack system)


def build_site_plan():
    return {
        "title": "HQ 站点平面图 ·「废弃火星轨道货运堂」(纯室内 / Option B) — 剖面 A–A′(HQ_Section_AA_v3) 的平面companion",
        "subtitle": "可进入=暖窝+货堂(全室内·集结/登车/结算皆在壳内) · 卷帘门=框景, 门外火星废料场=不可进入背景 · 火星扫掠壳(分面曲东墙+斜北墙) 对峙 角落正交暖窝 · 死货运廊/断裂悬挑/天窗裂缝=顶部投影",
        "bounds": (-10.6, 19.6, -4.0, 38.0),
        # ===== Mars freight-shell envelope: swept faceted EAST wall + slanted NORTH loading wall =====
        "shell": [
            (-1.0, -0.6), (10.2, -0.6),
            (11.7, 4.5), (12.7, 10.0), (12.9, 15.5), (12.0, 20.5), (10.3, 24.6),   # east wall sweeps out & back
            (9.0, 26.8), (1.2, 28.4), (-1.0, 27.2),                                  # slanted north (loading) wall
        ],
        "openings": [
            {"p1": (2.7, 28.16), "p2": (6.7, 27.36), "color": AMBER, "label": ""},   # freight loading door = the ONLY threshold
        ],
        "zones": [
            # ===== NON-PLAYABLE backdrop hugging the building sides (dead woods / berm) =====
            {"cx": -8.6, "cz": 13.0, "w": 3.4, "d": 30.0, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "死林\n土坡\n背景\n不可\n进入", "font": 12, "text": NOTE},
            {"cx": 17.6, "cz": 13.0, "w": 3.4, "d": 30.0, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "死林\n土坡\n背景\n不可\n进入", "font": 12, "text": NOTE},
            {"cx": 4.5, "cz": -2.6, "w": 28.0, "d": 2.6, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "杂物后场 → 死林 (背景·不可进入)", "font": 12, "text": NOTE},
            # ===== 门外: the Mars scrapyard SEEN THROUGH THE DOOR — one dead grey-brown ground, NON-WALKABLE =====
            {"cx": 4.5, "cz": 33.2, "w": 30.0, "d": 9.6, "fill": (64, 62, 58), "outline": (44, 42, 40),
             "label": "门外 · 火星废料场 (透过卷帘门看得到 · 不可进入)", "font": 14, "text": NOTE},
            {"cx": 9.6, "cz": 36.2, "w": 3.2, "d": 2.6, "fill": MARSFOG, "outline": (120, 132, 144),
             "label": "火星废墟(地标)", "font": 11, "text": (40, 46, 54)},
            # abandoned cargo container just outside the door (backdrop prop, under the torn cantilever)
            {"cx": 0.8, "cz": 31.2, "w": 3.0, "d": 2.0, "angle": 12, "fill": CONTAINER, "outline": CONTAINER_O,
             "label": "废弃货柜", "font": 11, "text": (220, 214, 200)},
            # a dead sodium pole, far in the yard — silhouette only, NOT a playable light anchor
            {"cx": 7.6, "cz": 35.4, "w": 0.5, "d": 0.5, "fill": (96, 84, 56), "outline": INK,
             "label": "", "font": 10},
            # ===== INTERIOR loading bay: deck + the van (parked & BOARDED INSIDE; departs the door via overlay) =====
            {"cx": 4.5, "cz": 21.4, "w": 4.4, "d": 7.4, "fill": PLATE, "outline": PLATE_O, "label": ""},
            {"cx": 4.5, "cz": 21.4, "w": 2.3, "d": 5.2, "fill": VAN, "outline": (210, 210, 210),
             "label": "货车", "font": 13, "text": (240, 240, 240)},
            # ===== PLAYABLE interior — the agency NEST (orthogonal box, SW corner) =====
            {"cx": 2.5, "cz": 3.1, "w": 6.6, "d": 7.0, "fill": NEST, "outline": NEST_O, "label": ""},   # warm nest floor
            {"cx": 1.6, "cz": 6.6, "w": 4.8, "d": 0.4, "fill": WALL, "outline": INK, "label": ""},      # nest front wall (x-0.8→4.0);
            # the only way out of the nest = the 1.8 m door gap at x4.0–5.8 (nest ends at 5.8) — both flows pass through it
            {"cx": 1.4, "cz": 0.5, "w": 1.7, "d": 0.85, "fill": TEAL_F, "outline": TEAL,
             "label": "电脑/CRT\n选任务·结算", "font": 12},
            {"cx": 0.2, "cz": 2.4, "w": 0.35, "d": 1.8, "fill": RED, "outline": INK, "label": ""},      # debt/takeover board
            {"cx": 4.6, "cz": 3.0, "w": 1.1, "d": 0.9, "fill": PAPER_F, "outline": (150, 140, 115),
             "label": "折叠桌", "font": 10, "text": (110, 100, 80)},
            # ===== gear wall + (provisional) backpack rack, on the hall floor just outside the nest =====
            {"cx": 0.0, "cz": 10.2, "w": 0.9, "d": 3.2, "fill": TEAL_F, "outline": TEAL,
             "label": "装备墙\n半空", "font": 12},
            {"cx": 1.7, "cz": 10.2, "w": 1.0, "d": 2.0, "fill": BPACK, "outline": (110, 96, 68),
             "label": "背包架\n待定", "font": 11, "text": (60, 52, 36)},
            # ===== muster pad under the skylight crack + the leaning Y tree-column =====
            {"cx": 4.5, "cz": 15.4, "w": 5.0, "d": 5.4, "fill": SKYLT, "outline": SKYLT_O,
             "label": "", "font": 11},                                                 # contaminated daylight landing
            {"cx": 4.5, "cz": 15.6, "w": 3.2, "d": 2.4, "fill": MUSTER, "outline": MUSTER_O,
             "label": "集结台", "font": 12, "text": (110, 100, 80)},
            {"cx": 5.5, "cz": 15.4, "w": 0.7, "d": 0.7, "angle": 45, "fill": MARSW, "outline": INK,
             "label": "", "font": 10},                                                 # leaning column base
            # ===== LIGHT ANCHORS (interior only now — backdrop lights are silhouettes, not anchors) =====
            {"cx": 1.4, "cz": 1.4, "w": 0.5, "d": 0.5, "fill": LW, "outline": INK, "label": ""},   # desk lamp (warm)
            {"cx": 1.0, "cz": 0.6, "w": 0.4, "d": 0.4, "fill": LG, "outline": INK, "label": ""},   # CRT phosphor
            {"cx": 4.5, "cz": 15.6, "w": 0.55, "d": 0.55, "fill": SKYLT_O, "outline": INK, "label": ""},  # crack daylight
            {"cx": 7.2, "cz": 22.0, "w": 0.6, "d": 0.6, "fill": LS, "outline": INK, "label": ""},  # van-bay sodium (interior)
        ],
        "labels": [
            {"x": -0.6, "z": 5.6, "text": "暖窝(正交·暖)", "color": (120, 96, 50), "size": 12, "anchor": "left"},
            {"x": 7.0, "z": 27.0, "text": "货运卷帘门[E]·高4.8m·门=框景", "color": (150, 95, 30), "size": 12, "anchor": "left"},
            {"x": 6.4, "z": 19.4, "text": "装卸湾·钠灯(室内)", "color": (235, 230, 220), "size": 11, "anchor": "left"},
            {"x": 13.1, "z": 12.0, "text": "火星扫掠壳\n(曲东墙)", "color": (70, 80, 92), "size": 12, "anchor": "left"},
            {"x": 6.6, "z": 16.4, "text": "集结台·天光裂缝落点", "color": (110, 110, 96), "size": 11, "anchor": "left"},
            {"x": 5.9, "z": 14.2, "text": "倾斜树状柱", "color": (60, 64, 72), "size": 10, "anchor": "left"},
            {"x": 8.4, "z": 24.2, "text": "返程: 车回装卸湾→\n步行回 CRT 结算", "color": RNOTE, "size": 12, "anchor": "left"},
            {"x": 8.2, "z": 31.0, "text": "断裂悬挑投影(顶)", "color": (120, 110, 86), "size": 11, "anchor": "left"},
            {"x": 8.4, "z": 9.5, "text": "死货运廊(顶部投影)", "color": (96, 100, 110), "size": 11, "anchor": "left"},
            {"x": 3.0, "z": -2.0, "text": "A", "color": (150, 120, 180), "size": 20, "anchor": "left"},
            {"x": 2.2, "z": 34.6, "text": "A′", "color": (150, 120, 180), "size": 20, "anchor": "left"},
        ],
        # depart route (green): spawn → CRT → gear → muster → BOARD the van INSIDE (walk ends here; van departs via overlay)
        "path": [(1.4, 1.2), (4.6, 6.6), (1.4, 10.4), (4.0, 15.4), (4.0, 20.6)],
        "sightlines": [
            # section cut line A–A' — two short stubs at the ends (convention); the section cuts through the door
            # into the backdrop, so the north stub sits just past the threshold
            {"from": (4.5, -2.2), "to": (4.5, 0.4), "color": CUT},
            {"from": (4.5, 28.8), "to": (4.5, 33.2), "color": CUT},
            # dead conveyor — overhead sweep projection (the curve, in plan)
            {"from": (2.8, 5.0), "to": (5.6, 10.2), "color": CONV},
            {"from": (5.6, 10.2), "to": (7.6, 15.2), "color": CONV},
            {"from": (7.6, 15.2), "to": (6.6, 20.2), "color": CONV},
            {"from": (6.6, 20.2), "to": (4.9, 23.6), "color": CONV},
            # broken cantilever — overhead footprint, torn out OVER the door / threshold
            {"from": (1.9, 27.4), "to": (2.2, 32.4), "color": CANT},
            {"from": (7.0, 26.8), "to": (7.1, 32.2), "color": CANT},
            {"from": (2.2, 32.4), "to": (7.1, 32.2), "color": CANT},
            # shell crack / skylight slot — overhead, lands on the muster
            {"from": (3.0, 11.6), "to": (6.2, 11.2), "color": CRACK},
            # return route (stamp-red): van re-enters the door → parks the bay → walk back → past debt board → CRT (settle)
            {"from": (4.5, 27.6), "to": (5.3, 21.4), "color": RED},
            {"from": (5.3, 21.4), "to": (5.3, 7.6), "color": RED},
            {"from": (5.3, 7.6), "to": (0.5, 2.6), "color": RED},
            {"from": (0.5, 2.6), "to": (1.4, 1.2), "color": RED},
        ],
        "spawns": [
            {"label": "1", "x": 1.6, "z": 2.4}, {"label": "2", "x": 3.0, "z": 2.4},
            {"label": "3", "x": 1.6, "z": 4.2}, {"label": "4", "x": 3.0, "z": 4.2},
        ],
        "dims": [
            {"p1": (-1.0, -1.6), "p2": (10.2, -1.6), "label": "宽 ~11 m"},
            {"p1": (13.6, -0.6), "p2": (13.6, 26.8), "label": "货堂深 ~27 m (纯室内)"},
        ],
        "legend": [
            (NEST, "暖窝(正交)"), (TEAL_F, "工位(CRT/装备)"), (BPACK, "背包架(待定)"),
            (GREEN, "出发动线(止于登车)"), (RED, "返程动线(回CRT结算)"), (CUT, "剖切线 A–A′"),
            (CONV, "死货运廊(投影)"), (CANT, "断裂悬挑(投影)"), (CRACK, "壳裂缝(投影)"),
            (PLATE, "室内装卸湾=装车位"), (AMBER, "卷帘门(框景)"), (SKYLT, "天光裂缝落点"),
            (LS, "钠灯锚(室内)"), (LW, "暖钨锚"), (MARSFOG, "火星废墟地标"), ((64, 62, 58), "门外背景·不可进入"),
        ],
    }


if __name__ == "__main__":
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "design", "hq")
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "HQ_SitePlan_v5.png")
    w, h = render_floorplan(build_site_plan(), path)
    print(f"OK  {path}  ({w}x{h})")
