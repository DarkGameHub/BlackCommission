#!/usr/bin/env python3
"""
HQ plan v6 (2026-07-01) — 「破旧事务所 + 车库湾」一体小平房 · 纯室内.

MARS DIRECTION RETIRED (PM 2026-07-01: "不能用火星白模,他应该就是一个破旧事务所").
The HQ is a modest, run-down SINGLE-STOREY commission office with an attached cheap
garage bay — human scale, dense with broke-office life. Spectacle belongs to the
MISSION maps; the HQ is the small warm ritual space (LC ship-vs-moons structure).

Kept from the Mars saga: PURE INTERIOR (no terrain/vegetation), the roll-up door as a
FRAMED backdrop threshold (never entered), the light-pool/atmosphere tech (tungsten
nest / CRT phosphor / sputtering fluorescent / sodium bay / cold window light).

Storytelling: the street FRONT DOOR is court-sealed (查封封条, stamp red) — the crew
comes and goes ONLY through the garage; the garage bay is a cheap later ADDITION
(taller tin volume stuck on the office's east side). Hostile-takeover pressure, in plan.

Ritual (outbound): spawn/rest corner SW → CRT desk (accept, west wall) → gear wall
(north) → strip curtain → garage → board the van REAR.  Return: curtain → the DEBT
BOARD + CRT face you across the room (settle beat) — one straight sightline.

Run:  python tools/hq_shabby_office_plan_v1.py
Out:  design/hq/HQ_ShabbyOffice_Plan_v1.png
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from hq_floorplan_render import (  # noqa: E402
    render_floorplan, INK, TEAL, TEAL_F, GREEN, RED, AMBER, VAN, VAN_F, PAPER_F,
)

# palette
WALLK   = (40, 42, 40)
PAPERK  = (206, 188, 150)     # aged-paper office floor tone
GARAGE  = (132, 134, 136)     # garage slab
GARAGE_O = (104, 106, 108)
BACKDROP = (50, 54, 42)       # non-walkable street backdrop
BACKDROP_O = (36, 40, 30)
PROP    = (150, 132, 96)      # furniture fill
PROP_O  = (110, 94, 62)
MUSTER  = (231, 217, 191)
MUSTER_O = (198, 178, 148)
CURTAIN = (150, 168, 170)
LW      = (236, 196, 120)     # tungsten anchor
LG      = (150, 226, 140)     # CRT phosphor anchor
LS      = (224, 176, 92)      # sodium anchor
LC      = (198, 210, 200)     # cold window light anchor
NOTE    = (210, 205, 186)
RNOTE   = (224, 150, 150)


def build_plan():
    return {
        "title": "HQ 平面图 v6 rev2 ·「破旧事务所 + 车库湾」(UX+游戏设计审后修订) — 火星方向废止 (PM 2026-07-01)",
        "subtitle": "办公厅 8×7m·顶2.9m ‖ 车库加建 7.2×8.2m·顶4.3m(铁皮) · 正门被法院查封=只能走车库 · 卷帘门4.0m=框景不可出 · 出发: 休息角→CRT接单→装备墙→门帘→登车 · 返程: 门帘→迎面债务板+CRT结算",
        "bounds": (-3.2, 19.4, -4.4, 13.6),

        # ── building envelope: office (W) + taller garage addition (E, pokes 1.2 m north) ──
        "shell": [
            (0.0, 0.0), (15.2, 0.0), (15.2, 8.2), (8.0, 8.2), (8.0, 7.0), (0.0, 7.0),
        ],

        "openings": [
            # court-sealed street door (south office wall) — NOT usable, pure storytelling
            {"p1": (2.0, 0.0), "p2": (3.1, 0.0), "color": RED, "label": "正门·查封"},
            # dirty windows, south office wall (cold daylight in)
            {"p1": (4.6, 0.0), "p2": (5.8, 0.0), "color": LC, "label": "脏窗"},
            {"p1": (6.2, 0.0), "p2": (7.4, 0.0), "color": LC, "label": "脏窗"},
            # padlocked side door S1 (west wall by the rest corner, license-as-leash, locked)
            {"p1": (0.0, 0.8), "p2": (0.0, 1.8), "color": RED, "label": "S1挂锁"},
            # office ↔ garage strip curtain (the ONLY interior threshold)
            {"p1": (8.0, 2.6), "p2": (8.0, 5.0), "color": CURTAIN, "label": "胶条门帘"},
            # the garage roll-up: 4.0 w × 3.4 h — frames the dead street, never entered
            {"p1": (9.6, 8.2), "p2": (13.6, 8.2), "color": AMBER, "label": "卷帘门 4.0m·框景"},
        ],

        "zones": [
            # ── office hall (aged-paper floor) ──
            {"cx": 4.0, "cz": 3.5, "w": 7.9, "d": 6.9, "fill": PAPERK, "outline": (170, 150, 112)},
            # rest corner (SW): sofa + folding table + desk lamp — the crew LIVES here (地铺感)
            {"cx": 1.5, "cz": 0.9, "w": 2.4, "d": 0.9, "fill": PROP, "outline": PROP_O, "label": "破沙发"},
            # v2 (UX+GD review): east 0.4 m — widens the west-wall aisle to ~1.4 m (two capsules pass)
            # and clears P3/P4 spawn clearance; the bestiary notebook lives ON this table (idle read).
            {"cx": 2.0, "cz": 2.5, "w": 1.2, "d": 0.8, "fill": PROP, "outline": PROP_O, "label": "折叠桌·图鉴"},
            # CRT settlement/commission desk against the WEST wall, screen facing EAST into the room
            {"cx": 1.0, "cz": 4.4, "w": 1.5, "d": 0.8, "fill": TEAL_F, "outline": TEAL, "label": "CRT 终端桌"},
            # debt board beside it (west wall) — the first thing you SEE coming back from a run.
            # v2: 0.3 m south, tightens the curtain→board/CRT sightline spread to <6°.
            {"cx": 0.25, "cz": 5.4, "w": 0.25, "d": 1.6, "fill": RNOTE, "outline": RED, "label": "债务板", "font": 13},
            # v2 (GD review): the CIVIC-PAPERWORK pillar gets a physical anchor — a stamped-notice
            # pinboard (逾期通知/盖章回执) beside the court-sealed door, stamp-red language.
            {"cx": 3.9, "cz": 0.25, "w": 1.3, "d": 0.25, "fill": RNOTE, "outline": RED, "label": "公文钉板", "font": 12},
            # gas-mask sentinel guards the SEALED front door (the office's grim joke)
            {"cx": 2.7, "cz": 0.7, "w": 0.7, "d": 0.7, "fill": PROP, "outline": PROP_O, "label": "岗哨", "font": 12},
            # gear wall along the NORTH office wall: supply cabinet + filing + tool rack + safety board
            {"cx": 3.4, "cz": 6.6, "w": 1.3, "d": 0.6, "fill": TEAL_F, "outline": TEAL, "label": "补给柜"},
            {"cx": 4.9, "cz": 6.6, "w": 1.1, "d": 0.6, "fill": TEAL_F, "outline": TEAL, "label": "档案柜"},
            {"cx": 6.3, "cz": 6.65, "w": 1.2, "d": 0.45, "fill": TEAL_F, "outline": TEAL, "label": "工具架"},
            # clutter: file-box stacks pinching the walkways (破产的拥挤)
            {"cx": 6.9, "cz": 1.1, "w": 0.9, "d": 0.9, "fill": PROP, "outline": PROP_O, "label": "纸箱", "font": 12},
            {"cx": 3.3, "cz": 5.3, "w": 0.8, "d": 0.8, "fill": PROP, "outline": PROP_O, "label": "纸箱", "font": 12},
            {"cx": 7.3, "cz": 5.9, "w": 0.8, "d": 1.4, "fill": PROP, "outline": PROP_O, "label": "杂物", "font": 12},

            # ── garage bay (concrete slab, oil stains) ──
            {"cx": 11.6, "cz": 4.1, "w": 7.1, "d": 8.1, "fill": GARAGE, "outline": GARAGE_O},
            # the van: nose to the roll-up (north), REAR opens toward the curtain — board at the rear
            {"cx": 11.6, "cz": 4.3, "w": 2.4, "d": 5.0, "fill": VAN_F, "outline": VAN, "label": "厢式货车\n(车头朝门)"},
            # v2 (UX review): depth 0.9→1.5 m (south to the wall) — roomier final-muster beat.
            {"cx": 11.6, "cz": 1.05, "w": 2.6, "d": 1.5, "fill": MUSTER, "outline": MUSTER_O, "label": "登车区(车尾)", "font": 13},
            # muster pad between curtain and van rear
            {"cx": 9.1, "cz": 3.8, "w": 1.6, "d": 2.0, "fill": MUSTER, "outline": MUSTER_O, "label": "集结", "font": 13},
            # garage clutter: workbench (E wall), barrels (SE), tires, powerbox on the party wall
            {"cx": 14.6, "cz": 5.6, "w": 0.9, "d": 2.2, "fill": PROP, "outline": PROP_O, "label": "工作台", "font": 12},
            {"cx": 14.5, "cz": 0.9, "w": 1.2, "d": 1.2, "fill": PROP, "outline": PROP_O, "label": "油桶", "font": 12},
            {"cx": 8.5, "cz": 6.9, "w": 0.7, "d": 1.0, "fill": PROP, "outline": PROP_O, "label": "配电箱", "font": 11},
            {"cx": 14.6, "cz": 3.0, "w": 0.8, "d": 0.8, "fill": PROP, "outline": PROP_O, "label": "轮胎", "font": 12},

            # ── light-language anchors pinned (UX review: LW/LG/LS were declared but never placed) ──
            {"cx": 1.5, "cz": 1.2, "w": 0.35, "d": 0.35, "fill": LW, "outline": (180, 140, 70)},
            {"cx": 0.6, "cz": 4.9, "w": 0.35, "d": 0.35, "fill": LG, "outline": (90, 160, 85)},
            {"cx": 10.0, "cz": 2.6, "w": 0.35, "d": 0.35, "fill": LS, "outline": (170, 128, 58)},

            # ── non-walkable street backdrop beyond the roll-up (seen, never entered) ──
            {"cx": 11.6, "cz": 10.6, "w": 15.0, "d": 4.0, "fill": BACKDROP, "outline": BACKDROP_O,
             "label": "死街背景(不可进入): 链网栅栏 · 垃圾箱 · 死路灯 · 对面废楼剪影", "text": NOTE, "font": 14},
        ],

        # outbound ritual: rest corner → CRT → gear wall → curtain → muster → van rear
        # v2: routes EAST of the (moved) folding table — no furniture crossings.
        "path": [
            (1.9, 1.6), (3.1, 2.4), (2.3, 4.3),   # wake at the rest corner → around the table → CRT desk
            (3.4, 5.9), (5.0, 6.1),               # → gear wall
            (8.0, 3.8), (9.1, 3.4),               # → through the curtain → muster
            (11.0, 1.4),                          # → board at the van rear
        ],

        # the return-settle beat: through the curtain, the debt board + CRT face you dead-on
        "sightlines": [
            {"from": (8.0, 3.8), "to": (1.0, 4.6), "color": (120, 150, 120)},
            {"from": (11.6, 8.2), "to": (11.6, 6.8), "color": (150, 150, 150)},
        ],

        "spawns": [
            {"x": 1.0, "z": 1.9, "label": "P1"},
            {"x": 2.4, "z": 1.9, "label": "P2"},
            {"x": 1.0, "z": 3.1, "label": "P3"},
            {"x": 2.4, "z": 3.1, "label": "P4"},
        ],

        "dims": [
            {"p1": (0.0, -1.2), "p2": (8.0, -1.2), "label": "办公厅 8.0 m"},
            {"p1": (8.0, -1.2), "p2": (15.2, -1.2), "label": "车库 7.2 m"},
            {"p1": (-1.0, 0.0), "p2": (-1.0, 7.0), "label": "7.0 m"},
            {"p1": (16.2, 0.0), "p2": (16.2, 8.2), "label": "8.2 m"},
            {"p1": (9.6, 9.0), "p2": (13.6, 9.0), "label": "卷帘 4.0 m"},
        ],

        "labels": [
            {"x": 4.0, "z": 7.35, "text": "办公厅 顶2.9m · 钨丝暖光 + 濒死日光灯管(sputter)", "size": 14, "color": INK},
            {"x": 11.6, "z": -0.6, "text": "车库加建 顶4.3m · 铁皮 · 钠灯 · 地面油渍", "size": 14, "color": INK},
            {"x": 2.55, "z": -0.75, "text": "法院封条(印章红)·不可用", "size": 13, "color": RED},
            {"x": 5.9, "z": -1.9, "text": "脏窗=冷天光入口(南)", "size": 13, "color": (110, 120, 112)},
            {"x": -2.4, "z": 1.3, "text": "执照锁门", "size": 12, "color": RED},
            {"x": 1.7, "z": 3.55, "text": "四人地铺/出生", "size": 12, "color": (120, 104, 70)},
            {"x": 0.5, "z": 0.5, "text": "钨丝池", "size": 11, "color": (180, 140, 70)},
            {"x": -1.3, "z": 4.9, "text": "CRT绿池", "size": 11, "color": (90, 160, 85)},
            {"x": 9.6, "z": 3.2, "text": "钠灯池", "size": 11, "color": (170, 128, 58)},
        ],

        "legend": [
            (PAPERK, "办公厅(纸色地·2.9m)"),
            (GARAGE, "车库湾(混凝土·4.3m)"),
            (BACKDROP, "死街背景·不可进入"),
            (TEAL_F, "功能家具(CRT/装备墙)"),
            (PROP, "生活杂物(纸箱/油桶/沙发)"),
            (MUSTER, "集结/登车区"),
            (RED, "查封/挂锁(印章红)"),
            (AMBER, "卷帘门=框景阈值"),
            (GREEN, "出发动线"),
        ],
    }


if __name__ == "__main__":
    out = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))),
                       "design", "hq", "HQ_ShabbyOffice_Plan_v2.png")
    render_floorplan(build_plan(), out)
    print(f"wrote {out}")
