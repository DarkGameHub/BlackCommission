# -*- coding: utf-8 -*-
"""Menu in the terminal's retro-mono font (PM: reference Lethal Company; terminal is good).

LC's UI is a smooth retro-terminal monospace, NOT bitmap pixels. The terminal mockup
(which PM approved) already uses it. This extends that same monospace to the whole menu
in amber/paper on dark, so the entire UI reads as one retro-terminal voice. Palette
unchanged (art-bible §4).
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "font-options")
W, H = 1920, 1080
BLACK="#1A1A17"; AMBER="#FF9820"; AMBER_L="#FFAB40"; AMBER_D="#9A6418"
PAPER="#D6CCAE"; PAPER_D="#9A917A"; PAPER_DD="#6A6456"
CRT="#6CFF5F"; CRT_D="#2E7A33"; RED="#C23A2B"; MILGRN="#55624A"
# LC-style retro terminal mono. Consolas (approved via the terminal) first; the exact
# LC face is "3270"/"VCR OSD Mono" — embeddable later if PM wants a pixel-perfect match.
MONO = "'Consolas','Lucida Console','Courier New',monospace"


def esc(s): return s.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;")
def t(x,y,s,size,fill,anchor="start",sp=None,weight=None,op=None):
    a=f' letter-spacing="{sp}"' if sp else ""; w=f' font-weight="{weight}"' if weight else ""
    o=f' opacity="{op}"' if op else ""
    return f'<text x="{x}" y="{y}" font-family="{MONO}" font-size="{size}" fill="{fill}" text-anchor="{anchor}"{a}{w}{o}>{esc(s)}</text>'
def r(x,y,w,h,fill,op=None,stroke=None,sw=None):
    o=f' opacity="{op}"' if op is not None else ""; st=f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    f=f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{st}/>'
def ln(x1,y1,x2,y2,st,sw=2,op=None):
    o=f' opacity="{op}"' if op is not None else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{st}" stroke-width="{sw}"{o}/>'

DEFS=f'''<defs>
 <radialGradient id="vig" cx="0.5" cy="0.46" r="0.78"><stop offset="0.42" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="0.74"/></radialGradient>
 <radialGradient id="lamp" cx="0.5" cy="0.5" r="0.5"><stop offset="0" stop-color="{AMBER}" stop-opacity="0.30"/><stop offset="0.55" stop-color="{AMBER_D}" stop-opacity="0.08"/><stop offset="1" stop-color="{AMBER}" stop-opacity="0"/></radialGradient>
 <radialGradient id="crtglow" cx="0.5" cy="0.5" r="0.6"><stop offset="0" stop-color="{CRT}" stop-opacity="0.16"/><stop offset="1" stop-color="{CRT}" stop-opacity="0"/></radialGradient>
 <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" stitchTiles="stitch"/><feColorMatrix type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.5 0"/></filter>
 <filter id="soft"><feGaussianBlur stdDeviation="0.4"/></filter></defs>'''

def office_bg():
    g=r(0,0,W,H,BLACK)
    g+=r(1060,0,860,H,"#1E1E1B")+ln(1060,0,1060,H,"#000",3,op=0.5)
    g+=f'<ellipse cx="1520" cy="720" rx="520" ry="430" fill="url(#lamp)"/>'
    g+=r(1300,760,520,220,"#241F18")
    g+=f'<ellipse cx="1430" cy="660" rx="160" ry="130" fill="url(#crtglow)"/>'
    g+=r(1360,600,150,120,"#15170F")+r(1372,612,126,84,"#0E140C")
    g+=ln(1384,648,1486,648,CRT_D,3,op=0.7)+ln(1384,664,1460,664,CRT_D,3,op=0.5)
    g+=r(1650,560,130,420,"#201E18")
    for i in range(3): g+=ln(1650,660+i*110,1780,660+i*110,"#15140F",4)
    g+=r(0,0,W,H,"url(#vig)")
    g+=f'<rect x="0" y="0" width="{W}" height="{H}" filter="url(#grain)" opacity="0.05"/>'
    return g

def build():
    g=office_bg()
    g+=r(0,0,560,38,"#000",op=0.7)+t(14,27,"LC RETRO-MONO (terminal font, menu-wide)",18,AMBER_L,weight="bold")
    # logo — same mono, big, amber underline
    g+=t(120,168,"BLACK COMMISSION",70,PAPER,sp=2,weight="bold")
    g+=r(124,196,612,5,AMBER)
    g+=t(126,238,"> OUTSOURCED COMMISSION OFFICE",22,PAPER_D,sp=2)
    g+=f'<g transform="translate(760,84) rotate(-9)" opacity="0.85">'+r(0,0,156,70,None,stroke=RED,sw=5)
    g+=t(78,32,"MUNICIPAL",16,RED,anchor="middle",weight="bold")+t(78,56,"OVERDUE",20,RED,anchor="middle",weight="bold")+"</g>"
    items=[("CONTINUE SHIFT","resume the last ledger",True),
           ("NEW OFFICE","open a fresh commission office",False),
           ("JOIN OFFICE","enter a teammate's room code",False),
           ("SETTINGS","name / language / volume / sensitivity",False),
           ("SHUT DOWN","clock out and power down",False)]
    y=360
    for lbl,sub,sel in items:
        if sel: fill,bd,tx,sx="#2A2418",AMBER,AMBER_L,"#C9A86A"
        else: fill,bd,tx,sx="#1F1F1B","#39392F",PAPER,PAPER_D
        g+=r(120,y,620,84,fill,stroke=bd,sw=2)
        if sel: g+=r(120,y,6,84,AMBER)+t(150,y+54,">",30,AMBER,weight="bold")
        g+=t(186,y+46,lbl,34,tx,sp=2,weight="bold")
        g+=t(188,y+74,sub,18,sx,sp=1)
        y+=100
    g+=t(120,1026,"ver 0.1   ·   LAN DIRECT",18,PAPER_DD,sp=1)
    g+=t(1800,1026,"QUARTERLY DEBT: 1,200G  ·  OVERDUE",18,RED,anchor="end",op=0.85)
    return g

svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'+DEFS+build()+"</svg>"
os.makedirs(OUT,exist_ok=True)
open(os.path.join(OUT,"LC_mono_menu.svg"),"w",encoding="utf-8").write(svg)
print("wrote LC_mono_menu")
