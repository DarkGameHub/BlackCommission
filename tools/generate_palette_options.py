# -*- coding: utf-8 -*-
"""Palette alternatives for the UI's cool/structural colour (PM: the olive-green
breaks immersion). Terminal CRT green stays. This swaps the document-card header /
UI structural colour (currently civic teal #3F5F5C). Shows the settlement card + a
menu accent strip in each option so PM can pick a direction. Font = 3270 (embedded)."""
import os, base64

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "palette-options")
W, H = 1920, 1080
_TTF = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project", "Art", "UI", "Fonts", "3270-Regular.ttf")
B64 = base64.b64encode(open(_TTF, "rb").read()).decode()
F = "BC3270"

BLACK="#1A1A17"; AMBER="#FF9820"; AMBER_L="#FFAB40"; PAPER="#D6CCAE"; PAPER_D="#9A917A"
INK="#26201A"; INK_D="#4A4136"; RED="#C23A2B"; PAPER_BG="#CFC4A4"; PAPER_BG2="#B8AD8E"

# (name, desc, header/cool colour, header-text colour, accent colour for menu)
OPTIONS = [
    ("A · CIVIC TEAL (current)", "the green you find distracting", "#3F5F5C", "#D6CCAE", AMBER),
    ("B · CIVIC BLUE", "municipal blueprint slate — institutional, no green", "#3A4E66", "#D7DCE2", AMBER),
    ("C · AMBER MONO", "no cool colour at all — bronze + amber, warmest / most LC", "#6E5226", "#E8D9B4", AMBER),
    ("D · OXBLOOD", "deep官印 red-brown ledger — bureaucratic, debt-heavy", "#5A2E2A", "#E2C9B8", AMBER),
]


def esc(s): return s.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;")
def t(x,y,s,size,fill,anchor="start",sp=None,op=None,italic=False):
    a=f' letter-spacing="{sp}"' if sp else "";o=f' opacity="{op}"' if op else ""
    i=' font-style="italic"' if italic else ""
    return f'<text x="{x}" y="{y}" font-family="{F}" font-size="{size}" fill="{fill}" text-anchor="{anchor}"{a}{o}{i}>{esc(s)}</text>'
def r(x,y,w,h,fill,op=None,stroke=None,sw=None):
    o=f' opacity="{op}"' if op is not None else "";st=f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    f=f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{st}/>'
def ln(x1,y1,x2,y2,st,sw=2,op=None):
    o=f' opacity="{op}"' if op is not None else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{st}" stroke-width="{sw}"{o}/>'

DEFS=f'''<defs><style>@font-face{{font-family:'{F}';src:url('data:font/ttf;base64,{B64}') format('truetype');}}</style>
<linearGradient id="paperg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{PAPER_BG}"/><stop offset="1" stop-color="{PAPER_BG2}"/></linearGradient></defs>'''


def panel(x, y, name, desc, cool, htext, accent):
    g=""
    # label
    g+=t(x, y-18, name, 26, accent, sp=2)
    g+=t(x, y+8, desc, 16, PAPER_D)
    # menu accent strip sample
    g+=r(x, y+24, 360, 54, "#1F1F1B", stroke="#39392F", sw=2)
    g+=r(x, y+24, 6, 54, accent)
    g+=t(x+22, y+58, "> CONTINUE SHIFT", 22, AMBER_L)
    # document card sample
    cy=y+104
    g+=r(x, cy, 600, 300, "url(#paperg)")
    g+=r(x, cy, 600, 46, cool)
    g+=t(x+20, cy+31, "COMMISSION SETTLEMENT", 20, htext, sp=3)
    g+=t(x+580, cy+30, "BC-2098-0007", 14, htext, anchor="end", op=0.7)
    g+=t(x+22, cy+86, "\"REAL COAST\" ECO-COLUMN", 20, INK)
    g+=ln(x+20, cy+104, x+580, cy+104, "#8A7F63", 2)
    g+=t(x+22, cy+140, "Commission pay", 18, INK)+t(x+580, cy+140, "300G", 18, INK, anchor="end")
    g+=ln(x+20, cy+156, x+580, cy+156, "#A2967A", 1.5)
    g+=t(x+22, cy+186, "Clause C-7 · carry impact x3", 18, INK)+t(x+580, cy+186, "-27G", 18, "#9A4A40", anchor="end")
    g+=ln(x+20, cy+204, x+580, cy+204, "#A2967A", 1.5)
    g+=t(x+22, cy+244, "NET PAID", 24, INK)+t(x+580, cy+244, "273G", 30, INK, anchor="end")
    # stamp uses cool colour as a sub-accent on B/C/D, red stays red
    g+=f'<g transform="translate({x+430},{cy+250}) rotate(-9)" opacity="0.8">'+r(0,0,140,46,None,stroke=RED,sw=4)
    g+=t(70,31,"SETTLED",18,RED,anchor="middle")+"</g>"
    # swatch hexes
    g+=r(x, cy+320, 40, 28, cool)+t(x+50, cy+340, cool.upper(), 16, PAPER_D)
    g+=r(x+220, cy+320, 40, 28, accent)+t(x+270, cy+340, "AMBER ACCENT (unchanged)", 16, PAPER_D)
    return g


body=r(0,0,W,H,BLACK)
body+=t(80, 60, "UI COOL-COLOUR OPTIONS  ·  terminal CRT green stays  ·  pick one", 30, PAPER, sp=2)
body+=ln(80, 78, 1200, 78, "#39392F", 2)
positions=[(120,200),(1020,200),(120,720),(1020,720)]
for (px,py),(name,desc,cool,htext,accent) in zip(positions, OPTIONS):
    body+=panel(px,py,name,desc,cool,htext,accent)

svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'+DEFS+body+"</svg>"
os.makedirs(OUT,exist_ok=True)
open(os.path.join(OUT,"cool_colour_options.svg"),"w",encoding="utf-8").write(svg)
print("wrote cool_colour_options")
