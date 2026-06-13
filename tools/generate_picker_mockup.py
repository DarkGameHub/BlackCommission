# -*- coding: utf-8 -*-
"""Crew picker v4 — the REAL low-poly worker, not a flat silhouette.

PM: figures read as flat anime paper cutouts; must have real-human proportions and
can be low-pixel LC style. So the figure is rebuilt from the ACTUAL in-game character
geometry (PlayerFirstPersonRig.BuildThirdPersonVisual): box torso 0.48x0.78x0.28,
sphere head ~0.33, box arms 0.13x0.62, box legs, flat helmet 0.36x0.10, thin hi-vis
vest 0.50x0.36 (the widener) + reflective tape, rectangular daypack, red debt badge —
~1.83 m, real proportions. Drawn as shaded 3D boxes (lit front / dark side / mid top)
so it has VOLUME. The selected worker stands on a lit review dais in the warm room;
the others queue beyond the cold window. In-engine this IS the real model.
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "ui-kit")
W, H = 1920, 1080
AMBER="#FF9820"; AMBER_L="#FFAB40"; AMBER_D="#9A6418"
PAPER="#D6CCAE"; PAPER_D="#9A917A"; INK="#26201A"; INK_D="#4A4136"
RED="#C23A2B"; TEAL="#3F5F5C"; GOLD="#C8A24A"; GOLD_L="#E4C77A"
PAPER_BG="#CFC4A4"; PAPER_BG2="#B8AD8E"
MONO="'Consolas','Courier New',monospace"; SANS="'Liberation Sans','Arial',sans-serif"

# Real model material colours (PlayerCharacterPalette), variant 0 = Repairman.
UNIFORM="#2B332B"; SKIN="#8F6650"; BOOTS="#121413"
# featured = copper vest + bone helmet
VEST="#D99930"; HELMET="#C9C2AB"; BADGE=RED; PACK="#1A1C1A"


def esc(s): return s.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;")
def t(x,y,s,size,fill,font=SANS,anchor="start",sp=None,weight=None,op=None,italic=False):
    a=f' letter-spacing="{sp}"' if sp else "";w=f' font-weight="{weight}"' if weight else ""
    o=f' opacity="{op}"' if op else "";i=' font-style="italic"' if italic else ""
    return f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" fill="{fill}" text-anchor="{anchor}"{a}{w}{o}{i}>{esc(s)}</text>'
def r(x,y,w,h,fill,op=None,stroke=None,sw=None,rx=None):
    o=f' opacity="{op}"' if op is not None else "";st=f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    rr=f' rx="{rx}"' if rx else "";f=f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{st}{rr}/>'
def ln(x1,y1,x2,y2,st,sw=2,op=None):
    o=f' opacity="{op}"' if op is not None else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{st}" stroke-width="{sw}"{o}/>'
def poly(pts,fill,op=None):
    o=f' opacity="{op}"' if op is not None else ""
    return f'<polygon points="{pts}" fill="{fill}"{o}/>'

def shade(hexc, f):
    c=hexc.lstrip("#"); rr,gg,bb=int(c[0:2],16),int(c[2:4],16),int(c[4:6],16)
    rr=max(0,min(255,int(rr*f))); gg=max(0,min(255,int(gg*f))); bb=max(0,min(255,int(bb*f)))
    return f"#{rr:02x}{gg:02x}{bb:02x}"

# isometric depth vector (a box's depth maps to this screen offset)
DPX, DPY = 0.46, -0.42

def box3d(x, y, w, h, depth, base, lit=1.0):
    """Front-left lit 3/4 box. x,y = top-left of the front face. depth in px."""
    dx, dy = depth*DPX, depth*DPY
    front = shade(base, 1.0*lit)
    top   = shade(base, 1.22*lit)
    side  = shade(base, 0.6*lit)
    g = poly(f"{x+w},{y} {x+w+dx},{y+dy} {x+w+dx},{y+h+dy} {x+w},{y+h}", side)   # right side
    g += poly(f"{x},{y} {x+w},{y} {x+w+dx},{y+dy} {x+dx},{y+dy}", top)            # top
    g += r(x, y, w, h, front)                                                     # front
    return g


def worker3d(cx, base_y, k, lit=1.0):
    """Low-poly worker from the real model geometry, scaled by k (px per metre),
    standing with feet at base_y, centred on cx. ~1.83 m tall, real proportions."""
    def X(m): return cx + m*k          # metre x → screen (centre origin)
    def Y(m): return base_y - m*k       # metre y (height) → screen
    g = ""
    # legs (work trousers) 0.16 w each, from 0 to 0.66 m
    legw = 0.17*k
    g += box3d(X(-0.20), Y(0.70), legw, 0.62*k, 0.20*k, UNIFORM, lit)
    g += box3d(X(0.04),  Y(0.70), legw, 0.62*k, 0.20*k, UNIFORM, lit)
    # boots (wide toe)
    g += box3d(X(-0.22), Y(0.10), 0.21*k, 0.10*k, 0.26*k, BOOTS, lit)
    g += box3d(X(0.02),  Y(0.10), 0.21*k, 0.10*k, 0.26*k, BOOTS, lit)
    # backpack (behind torso → drawn first, peeking)
    g += box3d(X(-0.19)+0.18*k, Y(1.30), 0.38*k, 0.48*k, 0.12*k, PACK, lit*0.85)
    # torso 0.48 x 0.78, centre y=1.05 → top 1.44, bottom 0.66
    g += box3d(X(-0.24), Y(1.44), 0.48*k, 0.78*k, 0.28*k, UNIFORM, lit)
    # arms 0.13 x 0.62 at x=±0.36, slight outward
    g += box3d(X(-0.42), Y(1.40), 0.14*k, 0.62*k, 0.16*k, UNIFORM, lit)
    g += box3d(X(0.30),  Y(1.40), 0.14*k, 0.62*k, 0.16*k, UNIFORM, lit)
    # hi-vis VEST 0.50 x 0.36 on the chest front (the widener), y centre 1.18
    vx, vy, vw, vh = X(-0.25), Y(1.36), 0.50*k, 0.36*k
    g += r(vx, vy, vw, vh, shade(VEST, lit))
    # reflective tape (two bands) — bright physical material
    g += r(vx, vy+0.10*k, vw, 0.045*k, shade("#EDEAD9", lit), op=0.95)
    g += r(vx, vy+0.26*k, vw, 0.045*k, shade("#EDEAD9", lit), op=0.9)
    # red debt badge, left chest
    g += r(X(0.05), Y(1.30), 0.12*k, 0.07*k, shade(BADGE, lit))
    # head sphere ~0.33, centre 1.58
    hx, hy, hr = cx, Y(1.58), 0.165*k
    g += f'<circle cx="{hx+0.06*k:.0f}" cy="{hy:.0f}" r="{hr:.0f}" fill="{shade(SKIN,0.7*lit)}"/>'  # back/shade
    g += f'<circle cx="{hx:.0f}" cy="{hy:.0f}" r="{hr:.0f}" fill="{shade(SKIN,lit)}"/>'
    # flat helmet 0.36 x 0.10 on top, y centre 1.78
    g += box3d(X(-0.18), Y(1.83), 0.36*k, 0.11*k, 0.34*k, HELMET, lit)
    # reflective helmet band
    g += r(X(-0.18), Y(1.79), 0.36*k, 0.03*k, shade("#EDEAD9", lit), op=0.85)
    # held hand torch (right hand, low) — cylindrical
    g += box3d(X(0.33), Y(1.00), 0.07*k, 0.20*k, 0.07*k, "#3A3A36", lit)
    return g


def worker_silhouette(cx, base_y, k, vest, rim="#AEB4AC"):
    """Dim backlit version of the same proportions for the queue beyond the window."""
    body="#1A1C20"
    def X(m): return cx + m*k
    def Y(m): return base_y - m*k
    g=f'<g opacity="0.55">'
    g+=r(X(-0.20),Y(0.70),0.17*k,0.60*k,body)+r(X(0.04),Y(0.70),0.17*k,0.60*k,body)   # legs
    g+=r(X(-0.24),Y(1.44),0.48*k,0.78*k,body)                                          # torso
    g+=r(X(-0.42),Y(1.40),0.14*k,0.60*k,body)+r(X(0.30),Y(1.40),0.14*k,0.60*k,body)    # arms
    g+=r(X(-0.25),Y(1.36),0.50*k,0.34*k,shade(vest,0.5))                               # vest (dim)
    g+=f'<circle cx="{cx:.0f}" cy="{Y(1.58):.0f}" r="{0.165*k:.0f}" fill="{body}"/>'    # head
    g+=r(X(-0.18),Y(1.83),0.36*k,0.11*k,body)                                          # helmet
    g+=ln(X(0.24),Y(1.44),X(0.24),Y(0.70),rim,2,op=0.4)                                # rim
    g+="</g>"
    return g


DEFS=f'''<defs>
 <radialGradient id="vig" cx="0.5" cy="0.52" r="0.82"><stop offset="0.34" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="0.8"/></radialGradient>
 <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#5A636A"/><stop offset="0.5" stop-color="#727A7C"/><stop offset="0.8" stop-color="#5A615D"/><stop offset="1" stop-color="#3C423D"/></linearGradient>
 <linearGradient id="fog" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#838A86" stop-opacity="0"/><stop offset="1" stop-color="#8E9690" stop-opacity="0.7"/></linearGradient>
 <linearGradient id="dust" x1="0" y1="0" x2="1" y2="0.3"><stop offset="0" stop-color="#C6C9C0" stop-opacity="0.55"/><stop offset="0.5" stop-color="#C6C9C0" stop-opacity="0.12"/><stop offset="1" stop-color="#C6C9C0" stop-opacity="0.5"/></linearGradient>
 <radialGradient id="warm" cx="0.42" cy="0.92" r="0.95"><stop offset="0" stop-color="#FFC062" stop-opacity="0.40"/><stop offset="0.5" stop-color="{AMBER_D}" stop-opacity="0.15"/><stop offset="1" stop-color="#FFC062" stop-opacity="0"/></radialGradient>
 <radialGradient id="dais" cx="0.5" cy="0.5" r="0.5"><stop offset="0" stop-color="#FFCE78" stop-opacity="0.45"/><stop offset="1" stop-color="#FFCE78" stop-opacity="0"/></radialGradient>
 <radialGradient id="chand" cx="0.5" cy="0.3" r="0.7"><stop offset="0" stop-color="#FFE0A0" stop-opacity="0.5"/><stop offset="1" stop-color="#FFE0A0" stop-opacity="0"/></radialGradient>
 <linearGradient id="paperg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{PAPER_BG}"/><stop offset="1" stop-color="{PAPER_BG2}"/></linearGradient>
 <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" stitchTiles="stitch"/><feColorMatrix type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.5 0"/></filter></defs>'''


def build():
    # ── opulent warm Mars lounge ──
    g=r(0,0,W,H,"#26190F")+r(0,0,W,H,"url(#warm)")
    g+=r(0,0,W,40,"#1A1009")+ln(0,40,W,40,GOLD,3,op=0.7)
    for x in range(60,W,300):
        g+=r(x,70,210,500,"#3A2616",op=0.32)+r(x,70,210,500,None,stroke=GOLD,sw=1.5)
    g+=r(0,H-150,W,150,"#3E2A18")+ln(0,H-150,W,H-150,GOLD,3,op=0.6)
    g+=f'<ellipse cx="960" cy="120" rx="240" ry="130" fill="url(#chand)"/>'+ln(960,0,960,70,GOLD,3,op=0.6)
    for dx in (-80,-40,0,40,80): g+=f'<circle cx="{960+dx}" cy="92" r="6" fill="{GOLD_L}"/>'
    g+=r(90,200,140,180,"#1C120A",stroke=GOLD,sw=5)+r(106,216,108,148,"#3A2A1C")

    # ── tall cold window with the queue beyond ──
    wx,wy,ww,wh=300,120,1320,560
    g+=r(wx-28,wy-28,ww+56,wh+56,"#160E07",stroke=GOLD,sw=5)
    g+=r(wx,wy,ww,wh,"url(#sky)")
    for bx,bw,bh in [(360,70,260),(450,120,160),(620,60,320),(720,140,200),(900,90,260),
                     (1050,110,180),(1190,70,300),(1330,130,190),(1470,80,250)]:
        g+=r(bx,wy+wh-bh-60,bw,bh,"#4A504C",op=0.55)
    g+=ln(640,wy+wh-380,640,wy+50,"#3E443F",6)+ln(640,wy+70,710,wy+110,"#3E443F",5)
    g+=r(wx,wy+wh-130,ww,130,"url(#fog)")
    # queue of candidates beyond the glass (same proportions, dim)
    gq=wy+wh-24
    g+=worker_silhouette(470,gq,150*0.62,"#5E6A4E")
    g+=worker_silhouette(690,gq,150*0.72,"#7C624A")
    g+=worker_silhouette(1180,gq,150*0.72,"#41615E")
    g+=worker_silhouette(1410,gq,150*0.62,"#A8842C")
    g+=r(wx,wy,ww,wh,"url(#dust)")
    for x in range(wx-40,wx+ww,72): g+=ln(x,wy,x-86,wy+wh,"#D6D8CE",1.4,op=0.09)
    g+=ln(wx+ww/3,wy,wx+ww/3,wy+wh,"#160E07",10)+ln(wx+2*ww/3,wy,wx+2*ww/3,wy+wh,"#160E07",10)
    g+=ln(wx,wy+wh/2,wx+ww,wy+wh/2,"#160E07",8)

    # ── lit review dais + the SELECTED worker as a real lit 3D low-poly model ──
    daisY=952
    g+=f'<ellipse cx="960" cy="{daisY+8}" rx="360" ry="84" fill="url(#dais)"/>'
    g+=f'<ellipse cx="960" cy="{daisY+10}" rx="250" ry="46" fill="#1A0F07" opacity="0.6"/>'   # platform
    g+=f'<ellipse cx="960" cy="{daisY+10}" rx="250" ry="46" fill="none" stroke="{GOLD}" stroke-width="2" opacity="0.5"/>'
    g+=worker3d(960, daisY, 232, lit=1.0)     # ~1.83 m * 232 px/m ≈ 425 px tall, real proportions

    # ── opulent foreground props ──
    g+=r(1560,820,160,200,"#1C1109")+path_decanter(1600,760)

    # ── brass header ──
    g+=r(300,52,1320,60,"#1B120A")+ln(300,112,1620,112,GOLD,2,op=0.55)
    g+=t(330,93,"SELECT YOUR AGENT",30,GOLD_L,font=MONO,sp=6,weight="bold")
    g+=t(1590,91,"1 / 6",24,AMBER_L,font=MONO,anchor="end")
    # amber selection bracket around the lit model
    bx,bw2,bty,bbh=812,296,560,400
    for ax,ay in [(bx,bty),(bx+bw2,bty),(bx,bty+bbh),(bx+bw2,bty+bbh)]:
        sx=1 if ax==bx else -1; sy=1 if ay<bty+bbh/2 else -1
        g+=ln(ax,ay,ax+sx*46,ay,AMBER,4)+ln(ax,ay,ax,ay+sy*46,AMBER,4)

    # ── dossier chit ──
    cx0=120; cy0=470
    g+=r(cx0,cy0,460,210,"url(#paperg)")
    g+=r(cx0,cy0,460,44,TEAL)
    g+=t(cx0+22,cy0+30,"AGENT DOSSIER",20,PAPER,font=MONO,sp=4,weight="bold")
    g+=t(cx0+438,cy0+29,"BC-04",15,"#A9C0B8",font=MONO,anchor="end")
    g+=r(cx0+24,cy0+66,52,52,VEST)+r(cx0+24,cy0+66,52,52,None,stroke="#8A7F63",sw=2)
    g+=t(cx0+96,cy0+92,"REPAIRMAN",24,INK,weight="bold")
    g+=t(cx0+96,cy0+120,"Orange helmet · hi-vis vest",17,INK_D)
    g+=t(cx0+96,cy0+144,"Temp hire · supply-closet kit",17,INK_D)
    g+=t(cx0+24,cy0+184,"Tool: hand torch",17,INK)
    g+=f'<g transform="translate({cx0+300},{cy0+150}) rotate(-9)" opacity="0.82">'+r(0,0,128,52,None,stroke=RED,sw=4)
    g+=t(64,34,"ASSIGNED",19,RED,anchor="middle",weight="bold")+"</g>"

    g+=t(960,1044,"‹ ›  CYCLE LOOK     ·     [E]  THIS IS ME     ·     [ESC]  KEEP CURRENT",
         20,PAPER_D,anchor="middle",sp=2)

    g+=r(0,0,W,H,"url(#vig)")+f'<rect x="0" y="0" width="{W}" height="{H}" filter="url(#grain)" opacity="0.055"/>'
    return g

def path_decanter(x,y):
    return r(x,y,44,58,"#3A2614")+f'<ellipse cx="{x+22}" cy="{y}" rx="22" ry="7" fill="{GOLD_L}" opacity="0.5"/>'


svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'+DEFS+build()+"</svg>"
os.makedirs(OUT,exist_ok=True)
open(os.path.join(OUT,"14_crew_picker.svg"),"w",encoding="utf-8").write(svg)
print("wrote 14_crew_picker v4 (real low-poly model)")
