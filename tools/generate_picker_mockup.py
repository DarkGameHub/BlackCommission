# -*- coding: utf-8 -*-
"""Mars surveillance-room crew picker (PM concept, ref: hut window w/ figures in storm).

Framed as a labor-dispatch eligibility window: you sit in a dark monitoring booth,
candidates walk past a lit, dust-blown window one by one, you pick one. Entered from
the dispatch-roster card's "CHANGE GEAR" line (lobby.md amendment). Same visual
system as the v2 kit (art-bible palette, grain, vignette, amber accent, paper panel).
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "ui-kit")
W, H = 1920, 1080

BLACK = "#1A1A17"; AMBER = "#FF9820"; AMBER_L = "#FFAB40"; AMBER_D = "#9A6418"
PAPER = "#D6CCAE"; PAPER_D = "#9A917A"; PAPER_DD = "#6A6456"; INK = "#26201A"; INK_D = "#4A4136"
CRT = "#6CFF5F"; CRT_D = "#2E7A33"; RED = "#C23A2B"; TEAL = "#3F5F5C"
PAPER_BG = "#CFC4A4"; PAPER_BG2 = "#B8AD8E"
# character vest palette (matches PlayerCharacterPalette intent)
VESTS = ["#8C5937", "#55624A", "#7C624A", "#3F5F5C", "#A8842C"]

SANS = "'Liberation Sans','Arial',sans-serif"
MONO = "'Consolas','Courier New',monospace"


def esc(s): return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def t(x, y, s, size, fill, font=SANS, anchor="start", sp=None, weight=None, op=None, italic=False):
    a = f' letter-spacing="{sp}"' if sp else ""
    w = f' font-weight="{weight}"' if weight else ""
    o = f' opacity="{op}"' if op else ""
    i = ' font-style="italic"' if italic else ""
    return (f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" fill="{fill}"'
            f' text-anchor="{anchor}"{a}{w}{o}{i}>{esc(s)}</text>')


def r(x, y, w, h, fill, op=None, stroke=None, sw=None):
    o = f' opacity="{op}"' if op is not None else ""
    s = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    f = f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{s}/>'


def ln(x1, y1, x2, y2, st, sw=2, op=None):
    o = f' opacity="{op}"' if op is not None else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{st}" stroke-width="{sw}"{o}/>'


def walker(cx, ground, scale, vest, dim=False):
    """A simple silhouette: head + torso (vest color) + legs, mid-stride."""
    op = 0.5 if dim else 1.0
    h = 150 * scale
    bw = 46 * scale
    body_c = "#0E0E0C" if dim else "#0A0A08"
    g = f'<g opacity="{op}">'
    # legs (stride)
    g += f'<polygon points="{cx-10*scale},{ground} {cx-2*scale},{ground-h*0.42} {cx+6*scale},{ground}" fill="{body_c}"/>'
    g += f'<polygon points="{cx+14*scale},{ground} {cx+4*scale},{ground-h*0.42} {cx-2*scale},{ground}" fill="{body_c}"/>'
    # torso with vest
    g += r(cx - bw / 2, ground - h * 0.78, bw, h * 0.40, vest if not dim else "#3A352C")
    g += r(cx - bw / 2, ground - h * 0.78, bw, h * 0.10, body_c)   # shoulders shadow
    # backpack hint
    g += r(cx - bw / 2 - 8 * scale, ground - h * 0.74, 12 * scale, h * 0.30, "#15140F")
    # head
    g += f'<circle cx="{cx}" cy="{ground-h*0.86}" r="{15*scale}" fill="{body_c}"/>'
    g += "</g>"
    return g


DEFS = f'''<defs>
  <radialGradient id="vig" cx="0.5" cy="0.46" r="0.78">
    <stop offset="0.4" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="0.78"/></radialGradient>
  <linearGradient id="window" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="#6E7A82"/><stop offset="0.5" stop-color="#9AA4A2"/><stop offset="1" stop-color="#5A625E"/></linearGradient>
  <linearGradient id="dust" x1="0" y1="0" x2="1" y2="0">
    <stop offset="0" stop-color="#C7C2B0" stop-opacity="0.5"/><stop offset="0.5" stop-color="#C7C2B0" stop-opacity="0.12"/>
    <stop offset="1" stop-color="#C7C2B0" stop-opacity="0.42"/></linearGradient>
  <linearGradient id="paperg" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="{PAPER_BG}"/><stop offset="1" stop-color="{PAPER_BG2}"/></linearGradient>
  <radialGradient id="amberp" cx="0.5" cy="0.5" r="0.5">
    <stop offset="0" stop-color="{AMBER}" stop-opacity="0.22"/><stop offset="1" stop-color="{AMBER}" stop-opacity="0"/></radialGradient>
  <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" stitchTiles="stitch"/>
    <feColorMatrix type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.5 0"/></filter>
</defs>'''


def build():
    g = r(0, 0, W, H, BLACK)
    # ── the window band (lit, dusty) framed by the dark booth wall ──
    wy, wh = 250, 470
    g += r(220, wy, 1480, wh, "url(#window)")
    # cold haze + walking candidates behind the glass
    ground = wy + wh - 40
    # dim candidates further down the line
    g += walker(560, ground, 0.78, VESTS[1], dim=True)
    g += walker(760, ground, 0.86, VESTS[2], dim=True)
    # current candidate, centered + larger + sharp
    g += walker(980, ground, 1.18, VESTS[0])
    g += walker(1240, ground, 0.86, VESTS[3], dim=True)
    g += walker(1430, ground, 0.78, VESTS[4], dim=True)
    # dust sweep over the glass + reflection streaks
    g += r(220, wy, 1480, wh, "url(#dust)")
    for x in range(260, 1700, 90):
        g += ln(x, wy, x - 40, wy + wh, "#D8D2C2", 1, op=0.06)
    # window mullions / frame
    g += r(220, wy, 1480, wh, None, stroke="#0C0C0A", sw=14)
    g += ln(960, wy, 960, wy + wh, "#0C0C0A", 10)
    g += r(206, wy - 14, 1508, 16, "#15140F")     # top sill
    g += r(206, wy + wh - 2, 1508, 20, "#15140F")  # bottom sill
    # ── booth interior: dark wall above/below, amber console glow at bottom ──
    g += r(0, 0, W, wy - 14, "#161512")
    g += r(0, wy + wh + 18, W, H - (wy + wh + 18), "#121110")
    g += f'<ellipse cx="960" cy="1000" rx="720" ry="220" fill="url(#amberp)"/>'
    # selection bracket on the current candidate
    bx, bw2, bty, bbh = 880, 200, wy + 30, wh - 90
    g += ln(bx, bty, bx, bty + 40, AMBER, 4) + ln(bx, bty, bx + 40, bty, AMBER, 4)
    g += ln(bx + bw2, bty, bx + bw2, bty + 40, AMBER, 4) + ln(bx + bw2, bty, bx + bw2 - 40, bty, AMBER, 4)
    g += ln(bx, bty + bbh, bx, bty + bbh - 40, AMBER, 4) + ln(bx, bty + bbh, bx + 40, bty + bbh, AMBER, 4)
    g += ln(bx + bw2, bty + bbh, bx + bw2, bty + bbh - 40, AMBER, 4) + ln(bx + bw2, bty + bbh, bx + bw2 - 40, bty + bbh, AMBER, 4)
    # ── top title strip (paper label riveted to the booth wall) ──
    g += r(220, 70, 1480, 96, "#1C1B16")
    g += ln(220, 166, 1700, 166, AMBER_D, 2, op=0.4)
    g += t(252, 118, "DISPATCH ELIGIBILITY WINDOW", 34, PAPER, sp=6, weight="bold")
    g += t(254, 150, "LABOR REVIEW BOOTH · MARS RELAY · YOU PICK ONE, THEY ALSO PICK YOU",
           18, PAPER_D, sp=4)
    g += t(1668, 124, "CAND. 1 / 5", 24, AMBER_L, font=MONO, anchor="end")
    # ── bottom console: selected candidate readout (paper chit) + controls ──
    cx0 = 600
    g += r(cx0, 820, 720, 150, "url(#paperg)")
    g += r(cx0, 820, 720, 44, TEAL)
    g += t(cx0 + 24, 850, "ASSIGNMENT CHIT", 20, PAPER, sp=6, weight="bold")
    g += t(cx0 + 696, 849, "FORM BC-04", 15, "#A9C0B8", font=MONO, anchor="end")
    g += r(cx0 + 28, 888, 56, 56, VESTS[0])       # vest swatch
    g += r(cx0 + 28, 888, 56, 56, None, stroke="#8A7F63", sw=2)
    g += t(cx0 + 104, 912, "CANDIDATE 01 · \"COPPER\" VEST", 24, INK, weight="bold")
    g += t(cx0 + 104, 944, "Cleared for surface work · no prior incidents on file", 17, INK_D)
    g += stamp_eligible(cx0 + 560, 884)
    # controls
    g += t(960, 1024, "‹ ›  CYCLE CANDIDATE     ·     [E]  STAMP & ASSIGN     ·     [ESC]  KEEP CURRENT",
           20, PAPER_D, anchor="middle", sp=2)
    # frame vignette + grain
    g += r(0, 0, W, H, "url(#vig)")
    g += f'<rect x="0" y="0" width="{W}" height="{H}" filter="url(#grain)" opacity="0.06"/>'
    return g


def stamp_eligible(x, y):
    return (f'<g transform="translate({x},{y}) rotate(-9)" opacity="0.8">'
            + r(0, 0, 130, 54, None, stroke=RED, sw=4)
            + t(65, 35, "ELIGIBLE", 22, RED, anchor="middle", weight="bold") + "</g>")


svg = f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">' + DEFS + build() + "</svg>"
os.makedirs(OUT, exist_ok=True)
with open(os.path.join(OUT, "14_crew_picker.svg"), "w", encoding="utf-8") as f:
    f.write(svg)
print("wrote 14_crew_picker")
