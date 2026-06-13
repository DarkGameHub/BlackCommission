# -*- coding: utf-8 -*-
"""Pixel / dot-matrix menu typography (PM: low-pixel, old-computer / printer font).

Windows ships no bitmap font, so we draw a real 5x7 dot-matrix font as rectangles
(square pixels = old CRT/DOS) or circles (pin printer). Authentically pixelated,
matches the lo-fi 256px map. Terminal CRT-green layer stays locked.
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "font-options")
W, H = 1920, 1080
BLACK = "#1A1A17"; AMBER = "#FF9820"; AMBER_L = "#FFAB40"; AMBER_D = "#9A6418"
PAPER = "#D6CCAE"; PAPER_D = "#9A917A"; PAPER_DD = "#6A6456"
CRT = "#6CFF5F"; CRT_D = "#2E7A33"; RED = "#C23A2B"; MILGRN = "#55624A"

F = {
 "A": ["01110","10001","10001","11111","10001","10001","10001"],
 "B": ["11110","10001","10001","11110","10001","10001","11110"],
 "C": ["01110","10001","10000","10000","10000","10001","01110"],
 "D": ["11110","10001","10001","10001","10001","10001","11110"],
 "E": ["11111","10000","10000","11110","10000","10000","11111"],
 "F": ["11111","10000","10000","11110","10000","10000","10000"],
 "G": ["01110","10001","10000","10111","10001","10001","01110"],
 "H": ["10001","10001","10001","11111","10001","10001","10001"],
 "I": ["11111","00100","00100","00100","00100","00100","11111"],
 "J": ["00111","00010","00010","00010","00010","10010","01100"],
 "K": ["10001","10010","10100","11000","10100","10010","10001"],
 "L": ["10000","10000","10000","10000","10000","10000","11111"],
 "M": ["10001","11011","10101","10101","10001","10001","10001"],
 "N": ["10001","10001","11001","10101","10011","10001","10001"],
 "O": ["01110","10001","10001","10001","10001","10001","01110"],
 "P": ["11110","10001","10001","11110","10000","10000","10000"],
 "Q": ["01110","10001","10001","10001","10101","10010","01101"],
 "R": ["11110","10001","10001","11110","10100","10010","10001"],
 "S": ["01111","10000","10000","01110","00001","00001","11110"],
 "T": ["11111","00100","00100","00100","00100","00100","00100"],
 "U": ["10001","10001","10001","10001","10001","10001","01110"],
 "V": ["10001","10001","10001","10001","10001","01010","00100"],
 "W": ["10001","10001","10001","10101","10101","11011","10001"],
 "X": ["10001","10001","01010","00100","01010","10001","10001"],
 "Y": ["10001","10001","01010","00100","00100","00100","00100"],
 "Z": ["11111","00001","00010","00100","01000","10000","11111"],
 "0": ["01110","10001","10011","10101","11001","10001","01110"],
 "1": ["00100","01100","00100","00100","00100","00100","01110"],
 "2": ["01110","10001","00001","00110","01000","10000","11111"],
 "3": ["11110","00001","00001","01110","00001","00001","11110"],
 "4": ["00010","00110","01010","10010","11111","00010","00010"],
 "5": ["11111","10000","11110","00001","00001","10001","01110"],
 "6": ["01110","10000","10000","11110","10001","10001","01110"],
 "7": ["11111","00001","00010","00100","01000","01000","01000"],
 "8": ["01110","10001","10001","01110","10001","10001","01110"],
 "9": ["01110","10001","10001","01111","00001","00001","01110"],
 " ": ["00000"]*7,
 ".": ["00000","00000","00000","00000","00000","01100","01100"],
 ",": ["00000","00000","00000","00000","01100","01100","01000"],
 "-": ["00000","00000","00000","11111","00000","00000","00000"],
 ":": ["00000","01100","01100","00000","01100","01100","00000"],
 "/": ["00001","00001","00010","00100","01000","10000","10000"],
 "(": ["00110","01000","10000","10000","10000","01000","00110"],
 ")": ["01100","00010","00001","00001","00001","00010","01100"],
 "%": ["11001","11010","00100","01000","01011","10011","00011"],
 "\"":["01010","01010","01010","00000","00000","00000","00000"],
 "'": ["01100","01100","01000","00000","00000","00000","00000"],
 "*": ["00000","01010","00100","11111","00100","01010","00000"],
 ">": ["10000","01000","00100","00010","00100","01000","10000"],
 "G2":["01110","10000","10000","10111","10001","10001","01110"],
}


def pix(x, y, s, px, color, shape="square", gap=1):
    out = []
    cx = x
    for ch in s:
        g = F.get(ch.upper() if ch != "·" else "*", F.get(ch, F[" "]))
        if ch == "·": g = F["*"]
        for ry, row in enumerate(g):
            for rxi, c in enumerate(row):
                if c == "1":
                    px0 = cx + rxi * px
                    py0 = y + ry * px
                    if shape == "dot":
                        out.append(f'<circle cx="{px0+px/2:.1f}" cy="{py0+px/2:.1f}" r="{px*0.42:.1f}" fill="{color}"/>')
                    else:
                        out.append(f'<rect x="{px0}" y="{py0}" width="{px-0.5}" height="{px-0.5}" fill="{color}"/>')
        cx += 6 * px + gap * px
    return "".join(out)


def pix_w(s, px, gap=1):
    return len(s) * (6 * px + gap * px)


def r(x, y, w, h, fill, op=None, stroke=None, sw=None):
    o = f' opacity="{op}"' if op is not None else ""
    st = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    f = f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{st}/>'


def ln(x1, y1, x2, y2, st, sw=2, op=None):
    o = f' opacity="{op}"' if op is not None else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{st}" stroke-width="{sw}"{o}/>'


DEFS = f'''<defs>
  <radialGradient id="vig" cx="0.5" cy="0.46" r="0.78"><stop offset="0.42" stop-color="#000" stop-opacity="0"/>
    <stop offset="1" stop-color="#000" stop-opacity="0.74"/></radialGradient>
  <radialGradient id="lamp" cx="0.5" cy="0.5" r="0.5"><stop offset="0" stop-color="{AMBER}" stop-opacity="0.30"/>
    <stop offset="0.55" stop-color="{AMBER_D}" stop-opacity="0.08"/><stop offset="1" stop-color="{AMBER}" stop-opacity="0"/></radialGradient>
  <radialGradient id="crtglow" cx="0.5" cy="0.5" r="0.6"><stop offset="0" stop-color="{CRT}" stop-opacity="0.16"/>
    <stop offset="1" stop-color="{CRT}" stop-opacity="0"/></radialGradient>
  <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" stitchTiles="stitch"/>
    <feColorMatrix type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.5 0"/></filter></defs>'''


def office_bg():
    g = r(0, 0, W, H, BLACK)
    g += r(1060, 0, 860, H, "#1E1E1B") + ln(1060, 0, 1060, H, "#000", 3, op=0.5)
    g += f'<ellipse cx="1520" cy="720" rx="520" ry="430" fill="url(#lamp)"/>'
    g += r(1300, 760, 520, 220, "#241F18")
    g += f'<ellipse cx="1430" cy="660" rx="160" ry="130" fill="url(#crtglow)"/>'
    g += r(1360, 600, 150, 120, "#15170F") + r(1372, 612, 126, 84, "#0E140C")
    g += ln(1384, 648, 1486, 648, CRT_D, 3, op=0.7) + ln(1384, 664, 1460, 664, CRT_D, 3, op=0.5)
    g += r(1650, 560, 130, 420, "#201E18")
    for i in range(3): g += ln(1650, 660 + i * 110, 1780, 660 + i * 110, "#15140F", 4)
    g += r(0, 0, W, H, "url(#vig)")
    g += f'<rect x="0" y="0" width="{W}" height="{H}" filter="url(#grain)" opacity="0.05"/>'
    return g


def menu(shape, tag):
    g = office_bg()
    g += r(0, 0, 520, 38, "#000", op=0.7) + pix(14, 10, tag, 3, AMBER_L, shape)
    # logo (big pixels)
    g += pix(120, 120, "BLACK", 9, PAPER, shape)
    g += pix(120, 200, "COMMISSION", 9, PAPER, shape)
    g += r(124, 278, 540, 6, AMBER)
    g += pix(126, 300, "OUTSOURCED COMMISSION OFFICE", 3, PAPER_D, shape)
    # overdue stamp (kept as outline + pixel text)
    g += f'<g transform="translate(720,108) rotate(-9)" opacity="0.85">' + r(0, 0, 170, 70, None, stroke=RED, sw=5)
    g += pix(16, 14, "OVER", 4, RED, shape) + pix(16, 42, "DUE", 4, RED, shape) + "</g>"
    items = [("CONTINUE SHIFT", "RESUME THE LAST LEDGER", True),
             ("NEW OFFICE", "OPEN A FRESH OFFICE", False),
             ("JOIN OFFICE", "ENTER A ROOM CODE", False),
             ("SETTINGS", "NAME LANGUAGE VOLUME", False),
             ("SHUT DOWN", "CLOCK OUT POWER DOWN", False)]
    y = 400
    for lbl, sub, sel in items:
        if sel:
            fill, bd, tx, sx = "#2A2418", AMBER, AMBER_L, "#C9A86A"
        else:
            fill, bd, tx, sx = "#1F1F1B", "#39392F", PAPER, PAPER_D
        g += r(120, y, 620, 84, fill, stroke=bd, sw=2)
        if sel:
            g += r(120, y, 6, 84, AMBER) + pix(140, y + 30, ">", 4, AMBER, shape)
        g += pix(184, y + 22, lbl, 5, tx, shape)
        g += pix(184, y + 58, sub, 3, sx, shape)
        y += 100
    g += pix(120, 1018, "VER 0.1   LAN DIRECT", 3, PAPER_DD, shape)
    g += pix(1380, 1018, "QUARTERLY DEBT 1200G OVERDUE", 3, RED, shape)
    return g


def svg(b):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">' + DEFS + b + "</svg>"


V = {"P1_crt_blocks": menu("square", "P1 - CRT BLOCK PIXELS  (DOS/OLD COMPUTER)"),
     "P2_dot_matrix": menu("dot", "P2 - DOT-MATRIX PRINTER  (PIN DOTS)")}
os.makedirs(OUT, exist_ok=True)
for name, b in V.items():
    with open(os.path.join(OUT, name + ".svg"), "w", encoding="utf-8") as f:
        f.write(svg(b))
    print("wrote", name)
