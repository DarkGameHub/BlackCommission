#!/usr/bin/env python3
"""
HQ WHOLE-SITE plan v3 (2026-06-24) — tight bounded hub + believable hard edge.

Revised after PM feedback ("院子怪怪的 / 左右后面室外为什么没东西 / 玩家可进入吗,标明清楚")
and a spawn-design研调 of comparable co-op hubs:
  * Lethal Company — small ENCLOSED ship; terminal one end, exit door the other; the
    "outside" is a tight railed platform / the mission, never empty lawn.
  * Deep Rock Galactic — spawn in a cabin -> a DENSE, fully-enclosed hangar of purposeful
    stations; the "outside" is space, a hard backdrop you cannot enter.
  * Level-design boundary theory — prefer a BELIEVABLE HARD edge (walls/terrain/treeline)
    over invisible walls or an open void.

So: the PLAYABLE area = office interior + the enclosed dispatch yard ONLY. The dead woods
+ earth berm press CLOSE on all four sides as non-playable backdrop (the believable hard
edge) — the office is nestled in the wild, not marooned on an open green disk. One controlled
opening north (the van's gate + dirt road). Everything is explicitly labelled可进入/背景.

Run:  python tools/hq_siteplan_v2.py     Out: design/hq/HQ_SitePlan_v3.png
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hq_floorplan_render import (  # noqa: E402
    render_floorplan, INK, TEAL, TEAL_F, GREEN, RED, AMBER, VAN, VAN_F, PAPER_F,
)

ASPHALT  = (120, 115, 103)   # dispatch yard (asphalt) — PLAYABLE, clearly lighter than the dark backdrop
ASPHALT_O = (152, 146, 132)
WALL     = (40, 42, 40)      # raised perimeter wall (2.0 m, above eye height)
BACKDROP = (50, 54, 42)      # NON-PLAYABLE backdrop: dead woods + earth berm hugging the compound
BACKDROP_O = (36, 40, 30)
JUNK     = (78, 76, 68)      # broke-office discards in the back lot
TREE     = (62, 66, 50)      # far dead-treeline + fog
TREE_O   = (46, 50, 38)
DIRT     = (170, 150, 112)   # the one opening north — van's dirt road out
DIRT_O   = (140, 120, 90)
SHED     = (96, 98, 100)     # equipment shed (east flank)
MUSTER   = (231, 217, 191)
MUSTER_O = (198, 178, 148)
DIM      = (150, 120, 70)
NOTE     = (210, 205, 186)   # light note (reads on the dark backdrop)
RNOTE    = (224, 150, 150)   # light stamp-red note
PLAYTXT  = (235, 224, 150)   # playable-boundary callout


def build_site_plan():
    return {
        "title": "HQ 站点平面图 · v3「围合调度院 + 野林贴边收口」(2026-06-24)",
        "subtitle": "可进入 = 办公室 + 调度院(硬围合) · 四周死林/土坡贴边 = 背景(不可进入) · 参考 LC船舱 / DRG机库式紧凑出生区",
        "bounds": (-17.0, 23.5, -8.0, 51.5),
        "shell": [(0, 0), (9, 0), (9, 17), (0, 19.5)],
        "openings": [
            {"p1": (2.5, 18.806), "p2": (6.5, 17.694), "color": AMBER, "label": ""},  # roll-up door
            {"p1": (1.5, 44.0), "p2": (7.5, 44.0), "color": AMBER, "label": ""},       # yard exit gate (6 m)
        ],
        "zones": [
            # ===== NON-PLAYABLE backdrop wrapping the compound CLOSE on all sides (believable hard edge) =====
            {"cx": -11.0, "cz": 21.5, "w": 8.0, "d": 59.0, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "死林 + 土坡\n背景·不可进入", "font": 13, "text": NOTE},
            {"cx": 20.5, "cz": 21.5, "w": 9.0, "d": 59.0, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "死林 + 土坡\n背景·不可进入", "font": 13, "text": NOTE},
            {"cx": 4.5, "cz": -3.6, "w": 31.0, "d": 5.6, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "杂物后场 → 死林   背景·不可进入", "font": 12, "text": NOTE},
            {"cx": 4.5, "cz": 49.2, "w": 31.0, "d": 4.6, "fill": TREE, "outline": TREE_O,
             "label": "远死树林 + 雾收边 (背景)", "font": 13, "text": (225, 228, 210)},
            # broke-office discards in the back lot (silhouettes only)
            {"cx": -2.0, "cz": -3.6, "w": 1.6, "d": 1.6, "fill": JUNK, "outline": INK, "label": ""},
            {"cx": 1.2, "cz": -3.8, "w": 2.2, "d": 1.2, "fill": JUNK, "outline": INK, "label": ""},
            {"cx": 8.5, "cz": -3.6, "w": 1.8, "d": 1.5, "fill": JUNK, "outline": INK, "label": ""},
            # ===== PLAYABLE: dispatch yard apron (asphalt) =====
            {"cx": 4.5, "cz": 31.25, "w": 24.0, "d": 24.5, "fill": ASPHALT, "outline": ASPHALT_O, "label": ""},
            # raised perimeter walls (2.0 m — above 1.7 m eye height)
            {"cx": -7.5, "cz": 31.0, "w": 0.5, "d": 26.0, "fill": WALL, "outline": INK, "label": ""},
            {"cx": 16.5, "cz": 31.0, "w": 0.5, "d": 26.0, "fill": WALL, "outline": INK, "label": ""},
            {"cx": -3.0, "cz": 44.0, "w": 9.0, "d": 0.5, "fill": WALL, "outline": INK, "label": ""},
            {"cx": 12.0, "cz": 44.0, "w": 9.0, "d": 0.5, "fill": WALL, "outline": INK, "label": ""},
            # the one opening north — van's dirt road out (over the far treeline band)
            {"cx": 4.5, "cz": 47.9, "w": 4.0, "d": 9.0, "fill": DIRT, "outline": DIRT_O,
             "label": "土路·北出", "font": 12, "text": (120, 100, 70)},
            # equipment shed (east flank — breaks the wall, gives the yard asymmetry)
            {"cx": 14.0, "cz": 28.0, "w": 4.0, "d": 6.0, "fill": SHED, "outline": INK,
             "label": "器材棚", "font": 12, "text": (235, 235, 235)},
            # van bay marking + van
            {"cx": 4.5, "cz": 22.0, "w": 3.6, "d": 6.4, "fill": ASPHALT, "outline": AMBER, "label": ""},
            {"cx": 4.5, "cz": 22.0, "w": 2.2, "d": 5.0, "fill": VAN_F, "outline": VAN,
             "label": "货车", "font": 13, "text": (240, 240, 240)},
            # ===== PLAYABLE: interior work zones (Option A, locked) =====
            {"cx": 4.5, "cz": 14.6, "w": 5.6, "d": 5.2, "fill": (236, 224, 193), "outline": (206, 182, 140),
             "label": "发车通道·留空", "font": 12, "text": DIM},
            {"cx": 4.5, "cz": 16.3, "w": 3.0, "d": 2.2, "fill": MUSTER, "outline": MUSTER_O,
             "label": "集结台", "font": 12, "text": (110, 100, 80)},
            {"cx": 1.0, "cz": 6.0, "w": 1.6, "d": 0.9, "fill": TEAL_F, "outline": TEAL,
             "label": "电脑/CRT\n选任务·结算", "font": 12},
            {"cx": 8.1, "cz": 12.3, "w": 1.0, "d": 2.6, "fill": TEAL_F, "outline": TEAL,
             "label": "装备站\n配装", "font": 13},
            {"cx": 0.3, "cz": 3.6, "w": 0.35, "d": 1.8, "fill": RED, "outline": INK, "label": ""},
            {"cx": 0.3, "cz": 15.9, "w": 0.35, "d": 1.1, "fill": RED, "outline": INK, "label": ""},
            {"cx": 2.0, "cz": 10.6, "w": 1.2, "d": 1.2, "fill": PAPER_F, "outline": (150, 140, 115),
             "label": "折叠桌", "font": 11, "text": (110, 100, 80)},
            {"cx": 6.9, "cz": 1.2, "w": 1.8, "d": 0.8, "fill": PAPER_F, "outline": (150, 140, 115),
             "label": "沙发", "font": 11, "text": (110, 100, 80)},
        ],
        "labels": [
            {"x": -6.4, "z": 3.0, "text": "债务/接管板 →", "color": (198, 78, 66), "size": 13, "anchor": "left"},
            {"x": 0.7, "z": 15.9, "text": "S1 上锁侧门", "color": RED, "size": 12, "anchor": "left"},
            {"x": 2.6, "z": 18.4, "text": "卷帘门(手动[E]揭幕)", "color": (150, 95, 30), "size": 13, "anchor": "left"},
            {"x": 7.9, "z": 44.0, "text": "出院门 6m → (仅货车)", "color": (235, 200, 130), "size": 13, "anchor": "left"},
            {"x": 6.0, "z": 22.0, "text": "装车位", "color": (235, 220, 180), "size": 12, "anchor": "left"},
            {"x": 8.3, "z": 39.0, "text": "调度院 24×26m", "color": (210, 202, 178), "size": 13, "anchor": "left"},
            {"x": -7.0, "z": 35.0, "text": "院墙升至 2.0m\n(过眼高=真围合)", "color": (215, 222, 200), "size": 12, "anchor": "left"},
            {"x": 9.6, "z": 33.0, "text": "返程: 回库→步行\n回 CRT 结算", "color": RNOTE, "size": 12, "anchor": "left"},
            {"x": -14.6, "z": 26.5, "text": "结算 @ 办公室 CRT\n(待 PM 确认)", "color": RNOTE, "size": 12, "anchor": "left"},
            {"x": -14.6, "z": 9.5, "text": "层高 3.42m\n(原 4.32m 偏高)", "color": NOTE, "size": 12, "anchor": "left"},
        ],
        # depart route (green)
        "path": [(4.5, 2.4), (1.5, 6.0), (8.1, 12.3), (4.5, 16.0), (4.5, 18.1), (4.5, 21.0), (4.5, 43.5)],
        # return route (stamp-red dashed)
        "sightlines": [
            {"from": (4.5, 43.5), "to": (4.5, 24.6), "color": RED},
            {"from": (4.5, 24.6), "to": (4.5, 18.6), "color": RED},
            {"from": (4.5, 18.6), "to": (1.2, 4.0), "color": RED},
            {"from": (1.2, 4.0), "to": (1.4, 5.6), "color": RED},
        ],
        "spawns": [
            {"label": "1", "x": 2.2, "z": 2.2}, {"label": "2", "x": 4.0, "z": 2.2},
            {"label": "3", "x": 5.6, "z": 2.2}, {"label": "4", "x": 7.0, "z": 2.2},
        ],
        "dims": [
            {"p1": (0, -1.6), "p2": (9, -1.6), "label": "9.0 m"},
            {"p1": (17.4, 18.5), "p2": (17.4, 44), "label": "院深 26 m"},
        ],
        "legend": [
            (TEAL_F, "工位(电脑/装备)"), (GREEN, "出发动线"), (RED, "返程/压力·上锁门"),
            (AMBER, "卷帘门/出院门"), (ASPHALT, "可进入:调度院"), (BACKDROP, "背景·不可进入(死林/土坡)"),
            (TREE, "远树+雾"), (DIRT, "北土路"),
        ],
    }


if __name__ == "__main__":
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "design", "hq")
    os.makedirs(out_dir, exist_ok=True)
    path = os.path.join(out_dir, "HQ_SitePlan_v3.png")
    w, h = render_floorplan(build_site_plan(), path)
    print(f"OK  {path}  ({w}x{h})")
