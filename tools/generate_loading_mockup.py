# -*- coding: utf-8 -*-
"""Redesigned post-New-Game LOADING screen — "OPENING THE OFFICE".
PM 2026-06-13: replaces an ugly/blank loading page. Diegetic work-order boot instead of a
spinner: the office powers up, the relay links, a room code is generated. System-chrome
language (dark panel + tungsten amber + 3270), matching the menu + lobby. No CRT green
(that's for in-world terminals only), no spinner.
Output: design/ux/mockups/ui-kit/02b_opening_office.svg
"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import generate_ui_mockups as k

OUT = k.OUT
W, H = k.W, k.H

b = k.office_bg()

# ── header ──
b += k.t(96, 92, "BLACK COMMISSION", 38, k.PAPER, sp=3, weight="bold")
b += k.ln(98, 110, 470, 110, k.AMBER, 2)
b += k.t(98, 140, "OPENING THE OFFICE  ·  STAND BY", 17, k.PAPER_D, sp=2)

# ── centre system panel ──
px, py, pw, ph = 540, 300, 840, 432
b += k.r(px - 2, py - 2, pw + 4, ph + 4, k.PANEL_L, op=0.55)
b += k.r(px, py, pw, ph, k.PANEL, op=0.96)
# amber header strip
b += k.r(px, py, pw, 52, "#241A0E", op=0.96)
b += k.r(px, py, 6, 52, k.AMBER)
b += k.t(px + 28, py + 35, "OPENING THE OFFICE", 24, k.AMBER_L, sp=2, weight="bold")
b += k.t(px + pw - 26, py + 34, "DISPATCH BOOT", 16, k.PAPER_DD, anchor="end", font=k.MONO)

# ── boot status lines (work-order style) ──
lines = [
    ("Powering the dispatch desk", "DONE", "done"),
    ("Establishing relay link",    "DONE", "done"),
    ("Generating room code",       "K7F2Q", "code"),
    ("Posting the crew manifest",  "",      "prog"),
]
ly = py + 98
for label, val, kind in lines:
    b += k.t(px + 30, ly, ">", 20, k.AMBER_D, font=k.MONO)
    b += k.t(px + 58, ly, label, 21, k.PAPER, font=k.MONO)
    b += k.t(px + 452, ly, "." * 26, 21, k.PAPER_DD, font=k.MONO)
    if kind == "done":
        b += k.t(px + pw - 30, ly, "DONE", 20, k.AMBER_L, anchor="end", font=k.MONO, weight="bold")
    elif kind == "code":
        b += k.t(px + pw - 30, ly, val, 26, k.AMBER_L, anchor="end", font=k.MONO, weight="bold", sp=4)
    else:
        b += k.r(px + pw - 156, ly - 15, 126, 13, "#16140F")
        b += k.r(px + pw - 156, ly - 15, 74, 13, k.AMBER)
    ly += 48

# ── wide progress bar ──
by = py + ph - 92
b += k.t(px + 30, by - 12, "OPENING…", 18, k.PAPER_D, font=k.MONO)
b += k.t(px + pw - 30, by - 12, "72%", 18, k.AMBER_L, anchor="end", font=k.MONO, weight="bold")
b += k.r(px + 30, by, pw - 60, 14, "#16140F")
b += k.r(px + 30, by, int((pw - 60) * 0.72), 14, k.AMBER)
b += k.t(px + 30, py + ph - 24,
         "Share room code K7F2Q with your crew — they can join the moment the office opens.",
         16, k.PAPER_DD)

b += k.grain(0.05) + k.vignette()

os.makedirs(OUT, exist_ok=True)
path = os.path.join(OUT, "02b_opening_office.svg")
open(path, "w", encoding="utf-8").write(k.svg(b))
print("wrote", path)
