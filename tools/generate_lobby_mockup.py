# -*- coding: utf-8 -*-
"""Redesigned 4-player WAITING / dispatch-roster lobby screen.
PM 2026-06-13: the in-game New-Game loading + 4-player waiting pages read as too ugly.
This is a fresh PNG concept for the waiting screen, in the locked UI kit language
(dark concrete + tungsten amber + oxblood paper card + 3270). Render to PNG via Edge.
Output: design/ux/mockups/ui-kit/03b_lobby_waiting.svg
"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import generate_ui_mockups as k   # reuse the kit palette + helpers (office_bg, paper_card, stamp, t/r/ln, svg, grain, vignette)

OUT = k.OUT
W, H = k.W, k.H
OX = "#5A2E2A"   # oxblood (document headers / READY badge) — NO green in UI per art-bible

b = k.office_bg()

# ── header ──
b += k.t(96, 92, "BLACK COMMISSION", 38, k.PAPER, sp=3, weight="bold")
b += k.ln(98, 110, 470, 110, k.AMBER, 2)
b += k.t(98, 140, "ASSEMBLING CREW  ·  DISPATCH PENDING", 17, k.PAPER_D, sp=2)

# ── room code block (top-right): big, copyable, the thing crews need ──
rx, ry = 1296, 60
b += k.r(rx, ry, 528, 108, k.PANEL, op=0.94)
b += k.r(rx, ry, 6, 108, k.AMBER)
b += k.t(rx + 30, ry + 36, "ROOM CODE", 19, k.PAPER_D, sp=3)
b += k.t(rx + 28, ry + 90, "K7F2Q", 54, k.AMBER_L, font=k.MONO, weight="bold", sp=10)
b += k.t(rx + 498, ry + 46, "[C] COPY", 17, k.PAPER_D, anchor="end")
b += k.t(rx + 498, ry + 92, "share to invite", 15, k.PAPER_DD, anchor="end")

# ── centre card: dispatch roster ──
cx, cy, cw, ch = 536, 224, 848, 660
b += k.paper_card(cx, cy, cw, ch, "DISPATCH ROSTER", "FORM BC-02")

# status row: WAITING + N/4 + four pips
sx, sy = cx + 40, cy + 102
b += k.t(sx, sy, "WAITING FOR CREW", 27, k.INK, weight="bold")
b += k.t(cx + cw - 40, sy, "2 / 4 REPORTED IN", 24, OX, anchor="end", weight="bold")
for i in range(4):
    b += k.r(sx + i * 42, sy + 16, 30, 10, (OX if i < 2 else "#B8AD8E"))
b += k.ln(cx + 40, sy + 46, cx + cw - 40, sy + 46, "#8A7F63", 2)

# four crew rows
rows = [
    ("01", "WANG",    "YOU · HOST", "READY",      "ready"),
    ("02", "AGENT 2", "",               "READY",      "ready"),
    ("03", "AGENT 3", "",               "joining…", "wait"),
    ("04", "",        "",               "open slot",  "open"),
]
rowy = sy + 92
for n, name, role, state, kind in rows:
    b += k.t(cx + 46, rowy, n, 22, k.INK_D, font=k.MONO)
    if kind == "open":
        b += k.t(cx + 98, rowy, "(open slot)", 25, "#9A8F73")
        b += k.t(cx + cw - 46, rowy, "waiting for a teammate…", 18, "#9A8F73", anchor="end")
    else:
        b += k.t(cx + 98, rowy, name, 28, k.INK, weight="bold")
        if role:
            b += k.r(cx + 300, rowy - 26, 158, 34, OX)
            b += k.t(cx + 379, rowy - 2, role, 16, k.PAPER, anchor="middle", weight="bold")
        if kind == "ready":
            b += k.t(cx + cw - 46, rowy, "■ " + state, 21, OX, anchor="end", weight="bold")
        else:
            b += k.t(cx + cw - 46, rowy, state, 21, "#9A6418", anchor="end")
    b += k.ln(cx + 40, rowy + 24, cx + cw - 40, rowy + 24, "#A2967A", 1.5)
    rowy += 84

# footer action
fy = cy + ch - 66
b += k.t(cx + 40, fy, "[ ENTER ]  DISPATCH — BEGIN THE SHIFT", 26, k.INK, weight="bold")
b += k.t(cx + 42, fy + 34, "Host dispatches when ready. Crew can still join with the room code.", 17, k.INK_D)
b += k.stamp(cx + cw - 116, fy - 12, "MUSTER", 150, 56, -8, 24)

b += k.grain(0.05) + k.vignette()

os.makedirs(OUT, exist_ok=True)
path = os.path.join(OUT, "03b_lobby_waiting.svg")
open(path, "w", encoding="utf-8").write(k.svg(b))
print("wrote", path)
