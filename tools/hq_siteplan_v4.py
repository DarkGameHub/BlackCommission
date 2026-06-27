#!/usr/bin/env python3
"""
HQ WHOLE-SITE plan v4 (2026-06-24) — architecture-led recompose.

Supersedes the v3 zoning diagram (HQ_SitePlan_v3.png), which the PM rejected as
"没有设计感": it was adjacency boxes + grass bands. v4 is the PLAN companion to the
longitudinal SECTION (design/hq/HQ_Section_AA_v1.png) and is driven by the same idea:

  SENSE OF PLACE — the office squats in a FORECLOSED MUNICIPAL VEHICLE-INSPECTION /
  WEIGH SUBSTATION. The civic bones (inspection lane, weigh-plate apron, roll-up,
  canopy, vehicle gate + clearance bar, pylon road-sign) earn the dispatch ritual for
  free. The plan now carries architectural information a zoning diagram cannot:
    * the A–A′ section cut line (cross-referenced to the section drawing)
    * the apron CANOPY projected overhead (the yard's "ceiling" — a roofed outdoor room)
    * the embedded WEIGH-PLATE as the diegetic van bay (not a painted rectangle)
    * the sunken inspection LANE + chained PIT down the spine (level change + history)
    * a composed LIGHT-ANCHOR layout (asymmetric: west = dim/shadow, east = clear/access)
    * the old PYLON road-sign as the return landmark on a closed, fogged horizon.

Run:  python tools/hq_siteplan_v4.py     Out: design/hq/HQ_SitePlan_v4.png
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hq_floorplan_render import (  # noqa: E402
    render_floorplan, INK, TEAL, TEAL_F, GREEN, RED, AMBER, VAN, VAN_F, PAPER_F,
)

ASPHALT  = (120, 115, 103)   # dispatch yard apron — PLAYABLE
ASPHALT_O = (150, 144, 130)
WALL     = (40, 42, 40)       # raised perimeter wall (2.4 m post-scale)
BACKDROP = (50, 54, 42)       # NON-PLAYABLE: dead woods + earth berm hugging the compound
BACKDROP_O = (36, 40, 30)
TREE     = (60, 66, 50)       # far dead-treeline / fogged warehouse silhouettes
TREE_O   = (46, 50, 38)
DIRT     = (170, 150, 112)    # the one opening north — van's dirt road out
DIRT_O   = (140, 120, 90)
SHED     = (96, 98, 100)      # equipment shed (east flank) / west service booth
CANOPY   = (181, 172, 150)    # apron canopy projected OVERHEAD (the yard ceiling)
CANOPY_O = (150, 140, 116)
LANE     = (150, 143, 126)    # sunken inspection lane (interior spine, −0.35 m)
PLATE    = (132, 134, 136)    # old steel weigh-plate = the van bay
PLATE_O  = (170, 172, 174)
PIT      = (44, 44, 42)       # chained-off defunct inspection pit
CURB     = (224, 214, 188)    # raised office curb (the +0 datum, desks live here)
LW       = (236, 196, 120)    # warm tungsten light anchor
LS       = (224, 176, 92)     # sodium light anchor
PYLON    = (70, 72, 66)
MUSTER   = (231, 217, 191)
MUSTER_O = (198, 178, 148)
DIM      = (150, 120, 70)
NOTE     = (210, 205, 186)
RNOTE    = (224, 150, 150)
CUT      = (96, 74, 120)      # section cut line A–A'


def build_site_plan():
    return {
        "title": "HQ 站点平面图 v4 ·「弃用市政验车站」适应性改造 — 含 A–A′ 剖切位 · 光锚 · 顶盖投影",
        "subtitle": "可进入=办公室+围合调度院 · 平面与剖面(HQ_Section_AA_v1)互为参照 · 市政骨架(验车道/地磅/限高杆/站牌)白嫖出发⇄返程仪式",
        "bounds": (-10.8, 19.8, -3.8, 50.8),
        "shell": [(0, 0), (9, 0), (9, 17), (0, 19.5)],
        "openings": [
            {"p1": (2.5, 18.806), "p2": (6.5, 17.694), "color": AMBER, "label": ""},  # roll-up door
            {"p1": (1.5, 44.0), "p2": (7.5, 44.0), "color": AMBER, "label": ""},        # yard exit gate (6 m)
        ],
        "zones": [
            # ===== NON-PLAYABLE backdrop hugging all four sides (believable hard edge) =====
            {"cx": -9.0, "cz": 23.0, "w": 3.2, "d": 50.0, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "死林\n土坡\n背景\n不可\n进入", "font": 12, "text": NOTE},
            {"cx": 18.0, "cz": 23.0, "w": 3.2, "d": 50.0, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "死林\n土坡\n背景\n不可\n进入", "font": 12, "text": NOTE},
            {"cx": 4.5, "cz": -2.3, "w": 27.0, "d": 2.6, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "杂物后场 → 死林 (背景·不可进入)", "font": 12, "text": NOTE},
            {"cx": 4.5, "cz": 48.6, "w": 27.0, "d": 3.4, "fill": TREE, "outline": TREE_O,
             "label": "死树线 + 雾中旧仓库剪影 (背景)", "font": 12, "text": (225, 228, 210)},
            # ===== PLAYABLE: dispatch yard apron =====
            {"cx": 4.5, "cz": 31.0, "w": 24.0, "d": 25.0, "fill": ASPHALT, "outline": ASPHALT_O, "label": ""},
            # apron CANOPY projected overhead — the yard's ceiling (roofed outdoor room)
            {"cx": 4.5, "cz": 22.0, "w": 6.8, "d": 7.2, "fill": CANOPY, "outline": CANOPY_O,
             "label": "雨棚顶盖(投影)", "font": 11, "text": (90, 84, 66)},
            # raised perimeter walls (2.4 m — above eye height)
            {"cx": -7.5, "cz": 31.0, "w": 0.5, "d": 26.0, "fill": WALL, "outline": INK, "label": ""},
            {"cx": 16.5, "cz": 31.0, "w": 0.5, "d": 26.0, "fill": WALL, "outline": INK, "label": ""},
            {"cx": -3.0, "cz": 44.0, "w": 9.0, "d": 0.5, "fill": WALL, "outline": INK, "label": ""},
            {"cx": 12.0, "cz": 44.0, "w": 9.0, "d": 0.5, "fill": WALL, "outline": INK, "label": ""},
            # the one opening north — van's dirt road out + old pylon road-sign landmark
            {"cx": 4.5, "cz": 48.5, "w": 4.0, "d": 9.0, "fill": DIRT, "outline": DIRT_O,
             "label": "土路·北出", "font": 12, "text": (120, 100, 70)},
            {"cx": 8.4, "cz": 46.5, "w": 0.8, "d": 0.8, "fill": PYLON, "outline": INK, "label": ""},
            # equipment shed (east flank) + west service booth — depth on the walls, asymmetry
            {"cx": 14.0, "cz": 28.0, "w": 4.0, "d": 6.0, "fill": SHED, "outline": INK,
             "label": "器材棚", "font": 12, "text": (235, 235, 235)},
            {"cx": -6.4, "cz": 24.0, "w": 2.2, "d": 3.0, "fill": SHED, "outline": INK,
             "label": "旧缴费亭\n/电箱", "font": 10, "text": (235, 235, 235)},
            # dead floodlight poles (vertical interest) + drain + west-flank detritus (asymmetry rule)
            {"cx": -6.3, "cz": 30.0, "w": 0.4, "d": 0.4, "fill": (66, 68, 66), "outline": INK, "label": ""},
            {"cx": 15.0, "cz": 36.5, "w": 0.4, "d": 0.4, "fill": (66, 68, 66), "outline": INK, "label": ""},
            {"cx": 3.3, "cz": 26.0, "w": 0.7, "d": 0.7, "fill": (70, 72, 72), "outline": (110, 112, 112), "label": ""},
            {"cx": -5.6, "cz": 39.0, "w": 1.3, "d": 1.0, "fill": (78, 76, 68), "outline": INK, "label": ""},
            {"cx": -6.0, "cz": 27.0, "w": 0.9, "d": 1.4, "fill": (78, 76, 68), "outline": INK, "label": ""},
            # old steel WEIGH-PLATE = the van bay (diegetic marker) + the van nose-in
            {"cx": 4.5, "cz": 22.0, "w": 4.2, "d": 6.8, "fill": PLATE, "outline": PLATE_O,
             "label": "", "font": 11},
            {"cx": 4.5, "cz": 22.0, "w": 2.2, "d": 5.0, "fill": VAN_F, "outline": VAN,
             "label": "货车", "font": 13, "text": (240, 240, 240)},
            # ===== PLAYABLE interior (Option A spine, locked) — now with lane + curbs + pit =====
            {"cx": 4.5, "cz": 11.5, "w": 3.0, "d": 13.0, "fill": LANE, "outline": (120, 114, 98),
             "label": "", "font": 11},                                        # sunken inspection lane
            {"cx": 4.5, "cz": 11.5, "w": 1.6, "d": 2.2, "fill": PIT, "outline": (90, 90, 86),
             "label": "旧验车坑", "font": 10, "text": (200, 200, 196)},        # chained pit
            {"cx": 1.6, "cz": 4.5, "w": 3.0, "d": 8.0, "fill": CURB, "outline": (198, 186, 156),
             "label": "", "font": 11},                                        # west office curb (+0)
            {"cx": 7.4, "cz": 8.5, "w": 2.6, "d": 9.0, "fill": CURB, "outline": (198, 186, 156),
             "label": "", "font": 11},                                        # east office curb (+0)
            {"cx": 4.5, "cz": 16.3, "w": 3.0, "d": 2.2, "fill": MUSTER, "outline": MUSTER_O,
             "label": "集结台", "font": 12, "text": (110, 100, 80)},
            {"cx": 1.0, "cz": 6.0, "w": 1.6, "d": 0.9, "fill": TEAL_F, "outline": TEAL,
             "label": "电脑/CRT\n选任务·结算", "font": 12},
            {"cx": 8.1, "cz": 12.3, "w": 1.0, "d": 2.6, "fill": TEAL_F, "outline": TEAL,
             "label": "装备墙\n半空", "font": 12},
            {"cx": 0.3, "cz": 3.6, "w": 0.35, "d": 1.8, "fill": RED, "outline": INK, "label": ""},
            {"cx": 2.0, "cz": 9.6, "w": 1.0, "d": 1.0, "fill": PAPER_F, "outline": (150, 140, 115),
             "label": "折叠桌", "font": 10, "text": (110, 100, 80)},
            {"cx": 6.9, "cz": 1.2, "w": 1.8, "d": 0.8, "fill": PAPER_F, "outline": (150, 140, 115),
             "label": "沙发", "font": 10, "text": (110, 100, 80)},
            # ===== LIGHT ANCHORS (composed pools; asymmetric — west dim, east clear) =====
            {"cx": 1.4, "cz": 6.0, "w": 0.5, "d": 0.5, "fill": LW, "outline": INK, "label": ""},   # desk lamp (warm)
            {"cx": 4.5, "cz": 16.3, "w": 0.5, "d": 0.5, "fill": LW, "outline": INK, "label": ""},  # muster fluoro
            {"cx": 4.5, "cz": 22.0, "w": 0.6, "d": 0.6, "fill": LS, "outline": INK, "label": ""},  # van-bay sodium
            {"cx": 4.5, "cz": 43.0, "w": 0.6, "d": 0.6, "fill": LS, "outline": INK, "label": ""},  # gate sodium
            {"cx": -5.5, "cz": 33.0, "w": 0.45, "d": 0.45, "fill": (180, 150, 96), "outline": INK, "label": ""},  # dim west flank
        ],
        "labels": [
            {"x": 0.8, "z": 3.2, "text": "债务/接管板", "color": (198, 78, 66), "size": 12, "anchor": "left"},
            {"x": 2.6, "z": 18.5, "text": "卷帘门(手动[E]揭幕)", "color": (150, 95, 30), "size": 13, "anchor": "left"},
            {"x": 7.9, "z": 44.0, "text": "出院门 6m → (仅货车·限高杆)", "color": (235, 200, 130), "size": 12, "anchor": "left"},
            {"x": 6.0, "z": 22.0, "text": "旧地磅板=装车位", "color": (235, 230, 220), "size": 11, "anchor": "left"},
            {"x": 9.0, "z": 46.5, "text": "旧站牌(返程地标)", "color": (205, 205, 195), "size": 11, "anchor": "left"},
            {"x": 8.7, "z": 38.0, "text": "调度院 24×25m", "color": (210, 202, 178), "size": 13, "anchor": "left"},
            {"x": -7.0, "z": 36.5, "text": "院墙 2.4m\n过眼高=真围合", "color": (215, 222, 200), "size": 11, "anchor": "left"},
            {"x": 9.6, "z": 32.0, "text": "返程: 回库→步行\n回 CRT 结算", "color": RNOTE, "size": 12, "anchor": "left"},
            {"x": 5.4, "z": 11.5, "text": "下沉验车道", "color": (60, 56, 48), "size": 11, "anchor": "left"},
            {"x": 9.4, "z": 29.5, "text": "光锚: 西暗/东亮·不对称\n(详见剖面 A–A′)", "color": (214, 190, 130), "size": 12, "anchor": "left"},
            {"x": 4.9, "z": -1.4, "text": "A", "color": (214, 196, 240), "size": 20, "anchor": "left"},
            {"x": 4.9, "z": 47.8, "text": "A′", "color": (214, 196, 240), "size": 20, "anchor": "left"},
        ],
        # depart route (green) zig-zags spawn→CRT→gear→muster→door→bay→gate
        "path": [(4.5, 2.4), (1.5, 6.0), (8.1, 12.3), (4.5, 16.0), (4.5, 18.1), (4.5, 21.0), (4.5, 43.5)],
        "sightlines": [
            # section cut line A–A' down the spine
            {"from": (4.5, -1.2), "to": (4.5, 46.8), "color": CUT},
            # return route (stamp-red) — gate → bay → door → past debt board → CRT
            {"from": (3.6, 43.5), "to": (3.6, 24.6), "color": RED},
            {"from": (3.6, 24.6), "to": (3.6, 18.6), "color": RED},
            {"from": (3.6, 18.6), "to": (1.2, 4.2), "color": RED},
        ],
        "spawns": [
            {"label": "1", "x": 2.0, "z": 2.2}, {"label": "2", "x": 3.4, "z": 2.2},
            {"label": "3", "x": 5.6, "z": 2.2}, {"label": "4", "x": 7.0, "z": 2.2},
        ],
        "dims": [
            {"p1": (0.3, 0.5), "p2": (8.7, 0.5), "label": "9.0 m"},
            {"p1": (15.8, 18.6), "p2": (15.8, 43.8), "label": "院深 25 m"},
        ],
        "legend": [
            (TEAL_F, "工位(电脑/装备)"), (GREEN, "出发动线"), (RED, "返程动线"),
            (CUT, "剖切线 A–A′"), (AMBER, "卷帘门/院门"), (CANOPY, "雨棚顶盖(投影)"),
            (PLATE, "旧地磅=装车位"), (LS, "钠灯锚"), (LW, "暖钨锚"),
            (ASPHALT, "可进入:调度院"), (BACKDROP, "背景·不可进入"),
        ],
    }


if __name__ == "__main__":
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "design", "hq")
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "HQ_SitePlan_v4.png")
    w, h = render_floorplan(build_site_plan(), path)
    print(f"OK  {path}  ({w}x{h})")
