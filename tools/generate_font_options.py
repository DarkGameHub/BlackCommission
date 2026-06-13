# -*- coding: utf-8 -*-
"""Typography direction comparison for the BC menu/system layer.

The terminal (CRT green monospace) is LOCKED per PM and not shown here. This
sheet explores the font for the menu chrome + document cards, because the clean
sans in mockup-kit v2 reads too modern against the lo-fi industrial map.

Palette unchanged (art-bible §4). Only the typeface + its weight/spacing changes.
Output: design/ux/mockups/font-options/A..D.png
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "font-options")
W, H = 1920, 1080

BLACK = "#1A1A17"; AMBER = "#FF9820"; AMBER_L = "#FFAB40"; AMBER_D = "#9A6418"
PAPER = "#D6CCAE"; PAPER_D = "#9A917A"; PAPER_DD = "#6A6456"
CRT = "#6CFF5F"; CRT_D = "#2E7A33"; RED = "#C23A2B"; MILGRN = "#55624A"; INK_D = "#4A4136"

DEFS = f'''<defs>
  <radialGradient id="vig" cx="0.5" cy="0.46" r="0.75">
    <stop offset="0.45" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="0.72"/></radialGradient>
  <radialGradient id="lamp" cx="0.5" cy="0.5" r="0.5">
    <stop offset="0" stop-color="{AMBER}" stop-opacity="0.34"/><stop offset="0.55" stop-color="{AMBER_D}" stop-opacity="0.10"/>
    <stop offset="1" stop-color="{AMBER}" stop-opacity="0"/></radialGradient>
  <radialGradient id="crtglow" cx="0.5" cy="0.5" r="0.6">
    <stop offset="0" stop-color="{CRT}" stop-opacity="0.16"/><stop offset="1" stop-color="{CRT}" stop-opacity="0"/></radialGradient>
  <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" stitchTiles="stitch"/>
    <feColorMatrix type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  0 0 0 0.5 0"/></filter>
</defs>'''


def esc(s): return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def t(x, y, s, size, fill, font, anchor="start", sp=None, weight=None, op=None):
    a = f' letter-spacing="{sp}"' if sp else ""
    w = f' font-weight="{weight}"' if weight else ""
    o = f' opacity="{op}"' if op else ""
    return (f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" fill="{fill}"'
            f' text-anchor="{anchor}"{a}{w}{o}>{esc(s)}</text>')


def r(x, y, w, h, fill, op=None, stroke=None, sw=None):
    o = f' opacity="{op}"' if op is not None else ""
    s = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    f = f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{s}/>'


def ln(x1, y1, x2, y2, st, sw=2, op=None):
    o = f' opacity="{op}"' if op is not None else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{st}" stroke-width="{sw}"{o}/>'


def office_bg():
    g = r(0, 0, W, H, BLACK)
    g += r(1060, 0, 860, H, "#1E1E1B") + ln(1060, 0, 1060, H, "#000", 3, op=0.5)
    g += f'<ellipse cx="1520" cy="720" rx="520" ry="430" fill="url(#lamp)"/>'
    g += r(1300, 760, 520, 220, "#241F18") + ln(1300, 760, 1820, 760, AMBER_D, 2, op=0.4)
    g += f'<ellipse cx="1430" cy="660" rx="160" ry="130" fill="url(#crtglow)"/>'
    g += r(1360, 600, 150, 120, "#15170F") + r(1372, 612, 126, 84, "#0E140C")
    g += ln(1384, 648, 1486, 648, CRT_D, 3, op=0.7) + ln(1384, 664, 1460, 664, CRT_D, 3, op=0.5)
    g += r(1650, 560, 130, 420, "#201E18") + ln(1650, 560, 1780, 560, AMBER_D, 2, op=0.3)
    for i in range(3): g += ln(1650, 660 + i * 110, 1780, 660 + i * 110, "#15140F", 4)
    g += r(1250, 600, 7, 380, "#1A1610")
    g += r(0, 0, W, H, "url(#vig)")
    g += f'<rect x="0" y="0" width="{W}" height="{H}" filter="url(#grain)" opacity="0.055"/>'
    return g


def menu(font_head, font_body, head_sp, btn_sp, weight, variant_name, variant_desc, head_text="BLACK COMMISSION"):
    g = office_bg()
    # corner tag naming the direction
    g += r(0, 0, 560, 40, "#000", op=0.7)
    g += t(16, 28, variant_name + "  —  " + variant_desc, 18, AMBER_L, font_body, weight="bold")
    # logo
    g += t(120, 178, head_text, 62, PAPER, font_head, sp=head_sp, weight=weight)
    g += r(124, 200, 540, 5, AMBER)
    g += t(126, 244, "OUTSOURCED COMMISSION OFFICE", 20, PAPER_D, font_body, sp=8)
    g += f'<g transform="translate(700,90) rotate(-9)" opacity="0.85">' + r(0, 0, 150, 70, None, stroke=RED, sw=5)
    g += t(75, 30, "MUNICIPAL", 16, RED, font_body, anchor="middle", weight="bold")
    g += t(75, 54, "OVERDUE", 20, RED, font_head, anchor="middle", weight="bold") + "</g>"
    # buttons
    items = [("CONTINUE SHIFT", "Resume the last ledger", True),
             ("NEW OFFICE", "Open a fresh commission office", False),
             ("JOIN OFFICE", "Enter a teammate's room code", False),
             ("SETTINGS", "Name · language · volume · sensitivity", False),
             ("SHUT DOWN", "Clock out and power down", False)]
    y = 360
    for lbl, sub, sel in items:
        if sel:
            fill, bd, tx, sx = "#2A2418", AMBER, AMBER_L, "#C9A86A"
        else:
            fill, bd, tx, sx = "#1F1F1B", "#39392F", PAPER, PAPER_D
        g += r(120, y, 560, 84, fill, stroke=bd, sw=2)
        if sel:
            g += r(120, y, 6, 84, AMBER) + t(146, y + 50, "▸", 26, AMBER, font_body)
        g += t(184, y + 44, lbl, 32, tx, font_head, sp=btn_sp, weight=weight)
        g += t(184, y + 74, sub, 17, sx, font_body, sp=1)
        y += 100
    g += t(120, 1028, "ver 0.1", 18, PAPER_DD, font_body, sp=2)
    g += t(1800, 1028, "QUARTERLY DEBT: 1,200G  ·  OVERDUE", 18, RED, font_body, anchor="end", op=0.85)
    return g


def svg(body):
    return f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">' + DEFS + body + "</svg>"


# Windows system fonts chosen to match the lo-fi industrial map.
V = {
    "A_stencil": menu(
        "'Stencil','Stencil Std','Arial Black',sans-serif", "'Bahnschrift','Arial Narrow',sans-serif",
        4, 3, "bold", "A · MUNICIPAL STENCIL", "spray-stencil, depot crates"),
    "B_din_signage": menu(
        "'Bahnschrift SemiBold','Bahnschrift','DIN',sans-serif", "'Bahnschrift','Arial',sans-serif",
        6, 4, "bold", "B · ROAD-SIGN DIN", "municipal signage, best lo-fi fit"),
    "C_carbon_copy": menu(
        "'Courier New',Courier,monospace", "'Courier New',Courier,monospace",
        2, 2, "bold", "C · CARBON COPY", "typewriter form, pairs w/ document cards"),
    "D_stamped_poster": menu(
        "'Franklin Gothic Heavy','Arial Black',Impact,sans-serif", "'Bahnschrift','Arial',sans-serif",
        2, 2, "bold", "D · STAMPED POSTER", "heavy condensed, bold notice"),
}

os.makedirs(OUT, exist_ok=True)
for name, body in V.items():
    with open(os.path.join(OUT, name + ".svg"), "w", encoding="utf-8") as f:
        f.write(svg(body))
    print("wrote", name)
