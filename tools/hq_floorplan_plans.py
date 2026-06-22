#!/usr/bin/env python3
"""
Black Commission — HQ floor-plan OPTIONS for PM review.

Synthesis of the level-designer (spatial) + game-designer (loop) specs:
both plans use the AGREED loop logic — linear +Z spine (spawn -> debt board ->
computer -> gear -> threshold -> van), opposite-wall station stagger (the
diagonal that kills "呆/蠢"), a framed reveal aperture, a clear empty run-up to
the roll-up, and the S1 padlock door peripheral-at-rest off the spine.

They differ on ONE real creative fork: SHELL SHAPE / reveal style.
  Option A "Long-Axis Z-Spine" (recommended): gentle wedge, door visible from
    spawn -> tension builds staring at the closed door the whole prep.
  Option B "L-Dogleg Procession": the loading bay is around a corner -> the
    roll-up reveal happens when the team rounds the bend (door NOT visible from
    spawn; trades the goal-in-sight read for a turn-into-the-reveal beat).

Run:  python tools/hq_floorplan_plans.py
Outputs PNGs into design/hq/.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hq_floorplan_render import (  # noqa: E402
    render_floorplan, INK, TEAL, TEAL_F, GREEN, RED, AMBER, VAN, VAN_F, SIL, PAPER_F,
)

NEG = (236, 224, 193)        # intentional negative space (reveal run-up)
NEG_O = (206, 182, 140)
MUSTER = (231, 217, 191)     # muster pad
MUSTER_O = (198, 178, 148)
DIM = (150, 120, 70)
PROP_O = (150, 140, 115)


def silhouette(cx_list, z, color=SIL):
    return [{"cx": cx, "cz": z, "w": w, "d": 0.6, "fill": color, "outline": color, "label": ""}
            for (cx, w) in cx_list]


# =====================================================================
# Option A — "Long-Axis Z-Spine" (RECOMMENDED): gentle wedge, axial reveal
# =====================================================================
def build_option_a():
    return {
        "title": "HQ 平面图 · 方案A「长轴 Z 字动线」(推荐)",
        "subtitle": "缓楔形单元 9×19.5m · 工位对墙错位 → 强制对角动线 · 卷帘门正对出生点(全程可见目标)",
        "shell": [(0, 0), (9, 0), (9, 17), (0, 19.5)],
        "openings": [
            {"p1": (2.5, 18.806), "p2": (6.5, 17.694), "color": AMBER, "label": ""},
        ],
        "zones": [
            # intentional empty run-up + muster (drawn first, under everything)
            {"cx": 4.5, "cz": 14.6, "w": 5.6, "d": 5.2, "fill": NEG, "outline": NEG_O,
             "label": "发车通道 · 留空", "font": 13, "text": DIM},
            {"cx": 4.5, "cz": 16.3, "w": 3.0, "d": 2.2, "fill": MUSTER, "outline": MUSTER_O,
             "label": "集结台", "font": 12, "text": (110, 100, 80)},
            # work stations — opposite walls (the diagonal)
            {"cx": 1.0, "cz": 6.0, "w": 1.6, "d": 0.9, "fill": TEAL_F, "outline": TEAL,
             "label": "电脑/CRT\n选任务", "font": 13},
            {"cx": 8.1, "cz": 12.3, "w": 1.0, "d": 2.6, "fill": TEAL_F, "outline": TEAL,
             "label": "装备站\n配装", "font": 13},
            # pressure board + locked door (red-coded), on -X wall
            {"cx": 0.3, "cz": 3.6, "w": 0.35, "d": 1.8, "fill": RED, "outline": INK, "label": ""},
            {"cx": 0.3, "cz": 15.9, "w": 0.35, "d": 1.1, "fill": RED, "outline": INK, "label": ""},
            # two signature broke pieces only (small, secondary)
            {"cx": 2.0, "cz": 10.6, "w": 1.2, "d": 1.2, "fill": PAPER_F, "outline": PROP_O,
             "label": "折叠桌", "font": 12, "text": (110, 100, 80)},
            {"cx": 1.0, "cz": 5.0, "w": 0.7, "d": 0.7, "fill": PAPER_F, "outline": PROP_O, "label": ""},
            # van + city silhouette outside (revealed when the roll-up rises)
            {"cx": 4.5, "cz": 22.2, "w": 2.2, "d": 5.0, "fill": VAN_F, "outline": VAN,
             "label": "货车", "font": 13, "text": (240, 240, 240)},
        ] + silhouette([(1.0, 2.6), (3.4, 3.2), (5.6, 2.4), (7.8, 3.6), (10.0, 2.8)], 27.2),
        "labels": [
            {"x": 0.7, "z": 3.6, "text": "债务/接管板", "color": RED, "size": 13, "anchor": "left"},
            {"x": 0.7, "z": 15.9, "text": "S1 上锁侧门\n(执照解锁)", "color": RED, "size": 13, "anchor": "left"},
            {"x": 2.6, "z": 18.25, "text": "卷帘门(关 → 升起揭幕)", "color": (150, 95, 30), "size": 14, "anchor": "left"},
            {"x": 1.6, "z": 24.6, "text": "门外:停车区 + 雾城剪影", "color": (110, 100, 80), "size": 13, "anchor": "left"},
        ],
        "path": [(4.5, 2.4), (1.5, 3.7), (1.5, 6.3), (8.1, 12.3), (4.5, 15.6), (4.5, 18.1), (4.5, 21.0)],
        "spawns": [
            {"label": "1", "x": 2.2, "z": 2.2}, {"label": "2", "x": 4.0, "z": 2.2},
            {"label": "3", "x": 5.6, "z": 2.2}, {"label": "4", "x": 7.0, "z": 2.2},
        ],
        "sightlines": [{"from": (4.5, 2.4), "to": (4.5, 17.6), "color": (150, 150, 150)}],
        "dims": [
            {"p1": (0, -1.1), "p2": (9, -1.1), "label": "9.0 m"},
            {"p1": (-1.1, 0), "p2": (-1.1, 19.5), "label": "19.5 m"},
        ],
        "legend": [
            (TEAL_F, "工位(电脑/装备)"), (GREEN, "动线(Z字)"), (RED, "压力板/上锁门"),
            (AMBER, "卷帘门揭幕"), (VAN_F, "货车(门外)"), (NEG, "刻意留空"), (PAPER_F, "破败小物"),
        ],
    }


# =====================================================================
# Option B — "L-Dogleg Procession": loading bay around the corner
# =====================================================================
def build_option_b():
    return {
        "title": "HQ 平面图 · 方案B「L 折角 · 转角揭幕」",
        "subtitle": "L 形单元 · 装载湾在转角之后 · 出生点看不到卷帘门 → 队伍转过弯门升起才揭幕",
        "shell": [(0, 0), (9, 0), (9, 7), (16, 7), (16, 14), (0, 14)],
        "openings": [
            {"p1": (10.5, 14), "p2": (14.5, 14), "color": AMBER, "label": ""},
        ],
        "zones": [
            # the wing run-up + muster (around the corner)
            {"cx": 12.5, "cz": 11.6, "w": 6.2, "d": 4.0, "fill": NEG, "outline": NEG_O,
             "label": "发车通道 · 留空", "font": 13, "text": DIM},
            {"cx": 12.5, "cz": 11.6, "w": 3.0, "d": 2.2, "fill": MUSTER, "outline": MUSTER_O,
             "label": "集结台", "font": 12, "text": (110, 100, 80)},
            # work stations — opposite walls of the bar (diagonal), gear sets up the right turn
            {"cx": 1.0, "cz": 5.0, "w": 1.6, "d": 0.9, "fill": TEAL_F, "outline": TEAL,
             "label": "电脑/CRT\n选任务", "font": 13},
            {"cx": 7.6, "cz": 9.8, "w": 2.4, "d": 1.0, "fill": TEAL_F, "outline": TEAL,
             "label": "装备站 配装", "font": 13},
            # pressure board + locked door, -X wall
            {"cx": 0.3, "cz": 3.0, "w": 0.35, "d": 1.8, "fill": RED, "outline": INK, "label": ""},
            {"cx": 0.3, "cz": 11.0, "w": 0.35, "d": 1.1, "fill": RED, "outline": INK, "label": ""},
            # two signature broke pieces
            {"cx": 1.7, "cz": 9.4, "w": 1.2, "d": 1.2, "fill": PAPER_F, "outline": PROP_O,
             "label": "折叠桌", "font": 12, "text": (110, 100, 80)},
            {"cx": 1.0, "cz": 4.2, "w": 0.7, "d": 0.7, "fill": PAPER_F, "outline": PROP_O, "label": ""},
            # van + silhouette outside the wing
            {"cx": 12.5, "cz": 16.6, "w": 2.2, "d": 5.0, "fill": VAN_F, "outline": VAN,
             "label": "货车", "font": 13, "text": (240, 240, 240)},
        ] + silhouette([(9.5, 2.6), (11.8, 3.2), (14.0, 2.6), (16.0, 3.0)], 21.4),
        "labels": [
            {"x": 0.7, "z": 3.0, "text": "债务/接管板", "color": RED, "size": 13, "anchor": "left"},
            {"x": 0.7, "z": 11.0, "text": "S1 上锁侧门", "color": RED, "size": 13, "anchor": "left"},
            {"x": 10.6, "z": 14.0, "text": "卷帘门(转角后)", "color": (150, 95, 30), "size": 14, "anchor": "left"},
            {"x": 2.4, "z": 12.4, "text": "出生点看不到门 →\n转过弯才揭幕", "color": (130, 90, 90), "size": 14, "anchor": "left"},
            {"x": 9.6, "z": 19.0, "text": "门外:停车区 + 雾城剪影", "color": (110, 100, 80), "size": 13, "anchor": "left"},
        ],
        "path": [(4.5, 2.0), (1.5, 3.2), (1.5, 5.2), (7.6, 9.8), (11.8, 11.0), (12.5, 13.6), (12.5, 16.0)],
        "spawns": [
            {"label": "1", "x": 2.0, "z": 1.8}, {"label": "2", "x": 4.0, "z": 1.8},
            {"label": "3", "x": 5.5, "z": 1.8}, {"label": "4", "x": 7.0, "z": 1.8},
        ],
        "sightlines": [
            {"from": (4.5, 2.0), "to": (4.5, 13.5), "color": (170, 150, 150)},
            {"from": (11.8, 11.0), "to": (12.5, 13.6), "color": (150, 150, 150)},
        ],
        "dims": [
            {"p1": (0, -1.1), "p2": (9, -1.1), "label": "9.0 m"},
            {"p1": (-1.1, 0), "p2": (-1.1, 14), "label": "14 m"},
            {"p1": (9, 6.0), "p2": (16, 6.0), "label": "装载湾 +7 m"},
        ],
        "legend": [
            (TEAL_F, "工位(电脑/装备)"), (GREEN, "动线"), (RED, "压力板/上锁门"),
            (AMBER, "卷帘门揭幕"), (VAN_F, "货车(门外)"), (NEG, "刻意留空"), (PAPER_F, "破败小物"),
        ],
    }


if __name__ == "__main__":
    out_dir = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "design", "hq")
    os.makedirs(out_dir, exist_ok=True)
    for name, builder in (("A_LongAxis", build_option_a), ("B_LDogleg", build_option_b)):
        path = os.path.join(out_dir, f"HQ_Option_{name}.png")
        w, h = render_floorplan(builder(), path)
        print(f"OK  {path}  ({w}x{h})")
