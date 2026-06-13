# -*- coding: utf-8 -*-
"""Crew picker v2 — opulent Mars interior looking out at the bleak Earth labor line.

PM fixes on v1: figures looked like ugly blocks · no atmosphere · figures too short ·
the luxury-interior vs grim-exterior contrast was missing. v2: a warm, plush Mars
buyer's lounge (the satire — wealth choosing labor) framing a tall cold window where
properly-proportioned candidate workers trudge past in dust. Same palette + grain.
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "ui-kit")
W, H = 1920, 1080
BLACK="#1A1A17"; AMBER="#FF9820"; AMBER_L="#FFAB40"; AMBER_D="#9A6418"
PAPER="#D6CCAE"; PAPER_D="#9A917A"; PAPER_DD="#6A6456"; INK="#26201A"; INK_D="#4A4136"
CRT="#6CFF5F"; CRT_D="#2E7A33"; RED="#C23A2B"; TEAL="#3F5F5C"; MILGRN="#55624A"
PAPER_BG="#CFC4A4"; PAPER_BG2="#B8AD8E"
GOLD="#C8A24A"; WARMWOOD="#5A4028"
VESTS=["#8C5937","#55624A","#7C624A","#3F5F5C","#A8842C"]
MONO="'Consolas','Courier New',monospace"; SANS="'Liberation Sans','Arial',sans-serif"


def esc(s): return s.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;")
def t(x,y,s,size,fill,font=SANS,anchor="start",sp=None,weight=None,op=None,italic=False):
    a=f' letter-spacing="{sp}"' if sp else ""; w=f' font-weight="{weight}"' if weight else ""
    o=f' opacity="{op}"' if op else ""; i=' font-style="italic"' if italic else ""
    return f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" fill="{fill}" text-anchor="{anchor}"{a}{w}{o}{i}>{esc(s)}</text>'
def r(x,y,w,h,fill,op=None,stroke=None,sw=None):
    o=f' opacity="{op}"' if op is not None else ""; st=f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    f=f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{st}/>'
def ln(x1,y1,x2,y2,st,sw=2,op=None):
    o=f' opacity="{op}"' if op is not None else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{st}" stroke-width="{sw}"{o}/>'


def worker(cx, ground, scale, vest, dim=False):
    """Properly-proportioned trudging laborer silhouette: head, hunched shoulders,
    tapered coat, mid-stride legs, heavy backpack, walking pole. ~8 heads tall feel."""
    op = 0.42 if dim else 1.0
    H_ = 360*scale
    body = "#0C0D0E" if dim else "#070809"
    head_r = 17*scale
    hx, hy = cx, ground-H_
    sh_y = hy+head_r*1.7
    hip_y = ground-H_*0.46
    g=f'<g opacity="{op}">'
    # walking pole (only sharp figure)
    if not dim:
        g+=ln(cx+30*scale, ground, cx+18*scale, hy+head_r, "#15140F", 4*scale)
    # back leg
    g+=f'<path d="M {cx-4*scale},{hip_y} L {cx-26*scale},{ground} L {cx-12*scale},{ground} L {cx+4*scale},{hip_y} Z" fill="{body}"/>'
    # front leg (stride)
    g+=f'<path d="M {cx+4*scale},{hip_y} L {cx+22*scale},{ground-6*scale} L {cx+36*scale},{ground} L {cx+10*scale},{hip_y} Z" fill="{body}"/>'
    # coat torso, tapered, slightly hunched forward
    g+=(f'<path d="M {cx-22*scale},{sh_y} '
        f'C {cx-30*scale},{(sh_y+hip_y)/2} {cx-24*scale},{hip_y} {cx-16*scale},{hip_y+8*scale} '
        f'L {cx+18*scale},{hip_y+8*scale} '
        f'C {cx+24*scale},{hip_y} {cx+26*scale},{(sh_y+hip_y)/2} {cx+18*scale},{sh_y} '
        f'C {cx+8*scale},{sh_y-10*scale} {cx-12*scale},{sh_y-10*scale} {cx-22*scale},{sh_y} Z" fill="{body}"/>')
    # vest patch on the chest (the only color — identity)
    if not dim:
        g+=r(cx-13*scale, sh_y+6*scale, 26*scale, 30*scale, vest)
    else:
        g+=r(cx-10*scale, sh_y+6*scale, 20*scale, 24*scale, "#2E2A22")
    # heavy backpack
    g+=f'<rect x="{cx-34*scale}" y="{sh_y+2*scale}" width="{16*scale}" height="{H_*0.34}" rx="{5*scale}" fill="{ "#101010" if dim else "#0A0B0C"}"/>'
    g+=f'<rect x="{cx-36*scale}" y="{sh_y+H_*0.20}" width="{20*scale}" height="{8*scale}" fill="{"#141414" if dim else "#0E0F10"}"/>'
    # hunched head + hood
    g+=f'<circle cx="{hx}" cy="{hy+head_r}" r="{head_r}" fill="{body}"/>'
    g+=f'<path d="M {hx-head_r-3*scale},{hy+head_r*1.4} Q {hx},{hy-head_r*0.5} {hx+head_r+3*scale},{hy+head_r*1.4}" fill="{body}"/>'
    g+="</g>"
    return g


DEFS=f'''<defs>
 <radialGradient id="vig" cx="0.5" cy="0.5" r="0.85"><stop offset="0.35" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="0.8"/></radialGradient>
 <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#8A9098"/><stop offset="0.45" stop-color="#A6ABA8"/><stop offset="0.72" stop-color="#7E837E"/><stop offset="1" stop-color="#565C56"/></linearGradient>
 <linearGradient id="dust" x1="0" y1="0" x2="1" y2="0"><stop offset="0" stop-color="#CDCABA" stop-opacity="0.55"/><stop offset="0.5" stop-color="#CDCABA" stop-opacity="0.10"/><stop offset="1" stop-color="#CDCABA" stop-opacity="0.5"/></linearGradient>
 <radialGradient id="warm" cx="0.5" cy="0.85" r="0.8"><stop offset="0" stop-color="#FFB45A" stop-opacity="0.36"/><stop offset="0.5" stop-color="{AMBER_D}" stop-opacity="0.14"/><stop offset="1" stop-color="#FFB45A" stop-opacity="0"/></radialGradient>
 <linearGradient id="shaft" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFE0A0" stop-opacity="0.18"/><stop offset="1" stop-color="#FFE0A0" stop-opacity="0"/></linearGradient>
 <linearGradient id="paperg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{PAPER_BG}"/><stop offset="1" stop-color="{PAPER_BG2}"/></linearGradient>
 <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" stitchTiles="stitch"/><feColorMatrix type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.5 0"/></filter></defs>'''


def build():
    # ── warm luxurious Mars interior base ──
    g=r(0,0,W,H,"#241814")                                   # deep warm wall
    g+=r(0,0,W,H,"url(#warm)")
    # rich panelled wainscot + gold trim
    g+=r(0,H-150,W,150,WARMWOOD)
    g+=ln(0,H-150,W,H-150,GOLD,3,op=0.6)
    for x in range(120,W,260):
        g+=r(x,H-150,180,150,"#4A3422",op=0.5)+ln(x,H-150,x,H,"#2E2014",2)
    # plush rug glow on the floor, warm key from the right
    g+=f'<ellipse cx="1300" cy="980" rx="760" ry="180" fill="#3A2616" opacity="0.7"/>'

    # ── the big cold window (tall, dominant) ──
    wx,wy,ww,wh = 300, 150, 1320, 620
    g+=r(wx-26,wy-26,ww+52,wh+52,"#1A120C")                  # deep frame recess
    g+=r(wx-26,wy-26,ww+52,wh+52,None,stroke=GOLD,sw=4)      # gilt frame edge
    g+=r(wx,wy,ww,wh,"url(#sky)")                            # bleak grey Earth sky
    # distant bleak skyline + ground haze
    for i,(bx,bw,bh) in enumerate([(360,90,150),(470,60,210),(560,120,120),(760,70,260),
                                   (880,140,160),(1080,80,220),(1200,110,140),(1360,90,200),(1480,70,170)]):
        g+=r(bx,wy+wh-bh-90,bw,bh,"#6E736E",op=0.5)
    g+=r(wx,wy+wh-110,ww,110,"#6A6F68",op=0.6)               # ground band
    # candidate labor line trudging past, properly tall
    ground=wy+wh-30
    worker_dim=lambda cx,sc,v: worker(cx,ground,sc,v,dim=True)
    g+=worker_dim(470,0.62,VESTS[1])
    g+=worker_dim(660,0.74,VESTS[2])
    g+=worker(920,ground,1.04,VESTS[0])                      # current — tall & sharp
    g+=worker_dim(1230,0.74,VESTS[3])
    g+=worker_dim(1420,0.62,VESTS[4])
    # blowing dust + faint reflection streaks over the glass
    g+=r(wx,wy,ww,wh,"url(#dust)")
    for x in range(wx+40,wx+ww,110):
        g+=ln(x,wy,x-46,wy+wh,"#E0DCCE",1,op=0.05)
    # mullions
    g+=ln(wx+ww/3,wy,wx+ww/3,wy+wh,"#1A120C",10)
    g+=ln(wx+2*ww/3,wy,wx+2*ww/3,wy+wh,"#1A120C",10)
    g+=ln(wx,wy+wh/2,wx+ww,wy+wh/2,"#1A120C",8)
    # warm light shaft falling into the room from the window's lower edge
    g+=f'<polygon points="{wx+120},{wy+wh} {wx+ww-120},{wy+wh} {wx+ww+120},{H} {wx-120},{H}" fill="url(#shaft)"/>'

    # ── luxury foreground cues (silhouette): wing chair + side table + glass ──
    g+=f'<path d="M 120,1080 L 120,820 Q 120,760 200,760 L 320,760 Q 360,760 360,820 L 360,1080 Z" fill="#160E08"/>'   # wing chair back
    g+=r(120,900,250,180,"#1A100A")                                                    # chair seat block
    g+=r(1560,840,150,180,"#170F09")                                                   # side table
    g+=r(1600,790,70,60,"#1F140C")+f'<ellipse cx="1635" cy="790" rx="22" ry="8" fill="{GOLD}" opacity="0.4"/>'  # decanter
    g+=f'<circle cx="1700" cy="880" r="6" fill="{GOLD}" opacity="0.5"/>'

    # ── header label (engraved brass plate) ──
    g+=r(300,60,1320,66,"#1B130C")+ln(300,126,1620,126,GOLD,2,op=0.5)
    g+=t(330,104,"DISPATCH ELIGIBILITY — SELECT YOUR CREW",30,GOLD,sp=4,weight="bold")
    g+=t(1590,100,"CAND. 1 / 5",22,AMBER_L,font=MONO,anchor="end")
    # selection bracket on the current candidate (amber)
    bx,bw2,bty,bbh=830,180,wy+40,wh-130
    for (ax,ay) in [(bx,bty),(bx+bw2,bty),(bx,bty+bbh),(bx+bw2,bty+bbh)]:
        sx=1 if ax==bx else -1; sy=1 if ay<wy+wh/2 else -1
        g+=ln(ax,ay,ax+sx*42,ay,AMBER,4)+ln(ax,ay,ax,ay+sy*42,AMBER,4)

    # ── assignment chit (paper) bottom-center ──
    cx0=590
    g+=r(cx0,800,740,150,"url(#paperg)")
    g+=r(cx0,800,740,44,TEAL)
    g+=t(cx0+24,830,"ASSIGNMENT CHIT",20,PAPER,sp=6,weight="bold")
    g+=t(cx0+716,829,"FORM BC-04",15,"#A9C0B8",font=MONO,anchor="end")
    g+=r(cx0+28,868,56,56,VESTS[0])+r(cx0+28,868,56,56,None,stroke="#8A7F63",sw=2)
    g+=t(cx0+104,894,"CANDIDATE 01  ·  \"COPPER\" VEST",24,INK,weight="bold")
    g+=t(cx0+104,926,"Cleared for surface work · no incidents on file",17,INK_D)
    g+=f'<g transform="translate({cx0+580},864) rotate(-9)" opacity="0.82">'+r(0,0,130,54,None,stroke=RED,sw=4)
    g+=t(65,35,"ELIGIBLE",22,RED,anchor="middle",weight="bold")+"</g>"
    g+=t(960,1004,"‹ ›  CYCLE     ·     [E]  STAMP & ASSIGN     ·     [ESC]  KEEP CURRENT",
         20,PAPER_D,anchor="middle",sp=2)

    g+=r(0,0,W,H,"url(#vig)")
    g+=f'<rect x="0" y="0" width="{W}" height="{H}" filter="url(#grain)" opacity="0.06"/>'
    return g


svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'+DEFS+build()+"</svg>"
os.makedirs(OUT,exist_ok=True)
open(os.path.join(OUT,"14_crew_picker.svg"),"w",encoding="utf-8").write(svg)
print("wrote 14_crew_picker v2")
