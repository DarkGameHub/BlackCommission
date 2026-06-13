# -*- coding: utf-8 -*-
"""Crew picker v3 — "choose your own agent". Backlit candidates (possible versions of
you) trudge past a cold window; you sit in an opulent warm room and pick which is you.

PM fixes on v2: figures must read as real people (→ backlit rim-light + anatomy),
interior must look richer (→ chandelier/art/decanter/gilt), exterior must feel
bleaker (→ colder desaturated sky, ruined skyline, heavy driving dust). Semantics:
this is a self-appearance picker, framed by the luxury-vs-grim contrast PM wants.

NOTE: in-engine these are the actual 3D character models walking the window — the
mockup figures only stage the composition.
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "ui-kit")
W, H = 1920, 1080
AMBER="#FF9820"; AMBER_L="#FFAB40"; AMBER_D="#9A6418"
PAPER="#D6CCAE"; PAPER_D="#9A917A"; INK="#26201A"; INK_D="#4A4136"
RED="#C23A2B"; TEAL="#3F5F5C"; GOLD="#C8A24A"; GOLD_L="#E4C77A"
PAPER_BG="#CFC4A4"; PAPER_BG2="#B8AD8E"
RIM="#C2C8C0"   # cold window rim-light on backlit figures
VESTS=["#9A6038","#5E6A4E","#7C624A","#41615E","#A8842C"]
MONO="'Consolas','Courier New',monospace"; SANS="'Liberation Sans','Arial',sans-serif"


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
def path(d,fill,op=None,stroke=None,sw=None):
    o=f' opacity="{op}"' if op is not None else "";s=f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    return f'<path d="{d}" fill="{fill}"{o}{s}/>'


def figure(cx, ground, sc, vest, headgear="helmet", tool="torch", sharp=True):
    """Backlit 'cheap commission worker' per art-bible §5: real-scale temp hire,
    oversized hi-vis VEST (the one silhouette widener) + reflective tape, distinct
    headgear (helmet/beanie/bumpcap/hood), rectangular daypack, a carried tool, heavy
    work boots, tired-competence stance (weight on one hip, slight forward lean).
    NOT a hiker — no ski poles, no sculpted gear."""
    op = 1.0 if sharp else 0.46
    body = "#0B0C0D" if sharp else "#16171B"     # jacket / coverall (dark)
    rim = RIM
    hh = 300*sc                                   # real proportions (~1.75m, not heroic)
    head_r = 19*sc
    hx = cx                                        # head leans slightly forward
    lean = 8*sc
    hy = ground-hh
    sh = hy+head_r*1.9                             # shoulder line
    hip = ground-hh*0.46
    vestW = 58*sc                                  # vest is OVERSIZED — wider than torso
    g=f'<g opacity="{op}">'
    # ── legs: weight on left hip, right leg relaxed (tired stance, slight stride) ──
    g+=path(f"M {cx-14*sc},{hip} L {cx-16*sc},{ground-12*sc} L {cx-2*sc},{ground-12*sc} L {cx+2*sc},{hip} Z", body)  # weight leg
    g+=path(f"M {cx+4*sc},{hip} L {cx+16*sc},{ground-12*sc} L {cx+28*sc},{ground-12*sc} L {cx+16*sc},{hip} Z", body)  # trailing leg
    # ── heavy work boots: wide toe + sole ledge ──
    for bxoff in (-20*sc, 12*sc):
        g+=path(f"M {cx+bxoff},{ground-14*sc} L {cx+bxoff+26*sc},{ground-14*sc} L {cx+bxoff+30*sc},{ground-4*sc} "
                f"L {cx+bxoff+30*sc},{ground} L {cx+bxoff-2*sc},{ground} Z", body)
    # ── rectangular daypack (zipper + strap, no sculpt) behind torso ──
    g+=r(cx-34*sc,sh+6*sc,18*sc,hh*0.34,body)
    g+=ln(cx-25*sc,sh+8*sc,cx-25*sc,sh+6*sc+hh*0.34,"#17181B",1.5*sc,op=0.7)   # zipper
    # ── jacket torso (real width, slight forward lean) ──
    g+=path(f"M {cx-18*sc+lean*0.3},{sh} L {cx-20*sc},{hip} L {cx+20*sc},{hip} L {cx+18*sc+lean*0.3},{sh} "
            f"C {cx+8*sc},{sh-8*sc} {cx-8*sc},{sh-8*sc} {cx-18*sc+lean*0.3},{sh} Z", body)
    # ── OVERSIZED hi-vis vest: the deliberate widener, sits over the jacket ──
    vx = cx-vestW/2+lean*0.2
    g+=path(f"M {vx},{sh+4*sc} L {vx-4*sc},{hip-6*sc} L {vx+vestW+4*sc},{hip-6*sc} L {vx+vestW},{sh+4*sc} "
            f"L {vx+vestW*0.62},{sh-2*sc} L {vx+vestW*0.5},{sh+18*sc} L {vx+vestW*0.38},{sh-2*sc} Z",
            vest if sharp else "#2E2A22")
    if sharp:
        # reflective safety tape — physical material, reads bright (two horizontal bands)
        g+=r(vx-2*sc,sh+22*sc,vestW+4*sc,5*sc,"#E8E4D4",op=0.9)
        g+=r(vx-2*sc,hip-20*sc,vestW+4*sc,5*sc,"#E8E4D4",op=0.85)
        # tiny company badge, clipped left chest
        g+=r(vx+8*sc,sh+10*sc,9*sc,6*sc,"#C9C2A8",op=0.8)
    # ── near arm holding a carried tool, low/forward ──
    g+=path(f"M {cx+16*sc},{sh+6*sc} L {cx+26*sc},{hip-6*sc} L {cx+18*sc},{hip-2*sc} L {cx+8*sc},{sh+12*sc} Z", body)
    if tool=="torch":
        g+=r(cx+18*sc,hip-10*sc,8*sc,18*sc,body)                       # cylindrical torch
        if sharp:  # short warm beam cone aimed down-forward
            g+=path(f"M {cx+26*sc},{hip-2*sc} L {cx+70*sc},{hip+30*sc} L {cx+64*sc},{hip+44*sc} L {cx+22*sc},{hip+6*sc} Z",
                    "#FFD27A",op=0.22)
    elif tool=="case":
        g+=r(cx+14*sc,hip-4*sc,30*sc,22*sc,body)+ln(cx+22*sc,hip-4*sc,cx+22*sc,hip-12*sc,body,3*sc)  # equipment case
    elif tool=="clipboard":
        g+=r(cx+18*sc,hip-12*sc,18*sc,24*sc,body)+r(cx+21*sc,hip-9*sc,12*sc,12*sc,"#9A917A",op=0.5)   # clipboard + form
    elif tool=="cable":
        g+=f'<circle cx="{cx+24*sc}" cy="{hip}" r="{12*sc}" fill="none" stroke="{body}" stroke-width="{5*sc}"/>'  # coiled cable
    # ── headgear (distinct head-break per variant) ──
    if headgear=="helmet":
        g+=f'<path d="M {hx-head_r},{hy+head_r*1.2} A {head_r} {head_r} 0 0 1 {hx+head_r},{hy+head_r*1.2} Z" fill="{body}"/>'
        g+=r(hx-head_r-5*sc,hy+head_r*1.1,2*head_r+10*sc,5*sc,body)       # brim
        if sharp: g+=r(hx-head_r,hy+head_r*0.5,2*head_r,4*sc,"#E8E4D4",op=0.85)  # reflective band
    elif headgear=="beanie":
        g+=f'<circle cx="{hx}" cy="{hy+head_r}" r="{head_r}" fill="{body}"/>'
        g+=r(hx-head_r,hy+head_r*0.6,2*head_r,head_r*0.8,body)
    elif headgear=="bumpcap":
        g+=f'<circle cx="{hx}" cy="{hy+head_r}" r="{head_r}" fill="{body}"/>'
        g+=r(hx-head_r,hy+head_r*0.7,2*head_r,4*sc,body)
        g+=r(hx+head_r-3*sc,hy+head_r*0.8,head_r*0.9,5*sc,body)           # forward peak
    else:  # hood
        g+=f'<circle cx="{hx}" cy="{hy+head_r}" r="{head_r*0.85}" fill="{body}"/>'
        g+=path(f"M {hx-head_r-4*sc},{hy+head_r*1.7} Q {hx},{hy-head_r*0.5} {hx+head_r+4*sc},{hy+head_r*1.7} "
                f"L {hx+head_r},{hy+head_r*2.1} Q {hx},{hy+head_r*0.6} {hx-head_r},{hy+head_r*2.1} Z", body)
    # neck/jaw shadow
    g+=r(hx-6*sc,hy+head_r*1.7,12*sc,8*sc,body)
    # ── COLD RIM LIGHT (window backlight) on the window-facing edge ──
    if sharp:
        g+=path(f"M {hx-2*sc},{hy+3*sc} Q {hx+head_r-2*sc},{hy+head_r*0.5} {hx+head_r-3*sc},{hy+head_r*1.4}",
                "none",stroke=rim,sw=3*sc)
        g+=ln(vx+vestW,sh+6*sc,vx+vestW+2*sc,hip-6*sc,rim,3*sc,op=0.8)     # vest edge
        g+=ln(cx+16*sc,sh+6*sc,cx+26*sc,hip-6*sc,rim,2.5*sc,op=0.6)        # arm edge
    else:
        g+=ln(vx+vestW,sh+6*sc,vx+vestW,hip,rim,2,op=0.35)
    g+="</g>"
    return g


DEFS=f'''<defs>
 <radialGradient id="vig" cx="0.5" cy="0.5" r="0.82"><stop offset="0.32" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="0.82"/></radialGradient>
 <linearGradient id="sky" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#5A636A"/><stop offset="0.5" stop-color="#727A7C"/><stop offset="0.78" stop-color="#5C6360"/><stop offset="1" stop-color="#3E443F"/></linearGradient>
 <linearGradient id="fog" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#838A86" stop-opacity="0"/><stop offset="1" stop-color="#9098920" stop-opacity="0.7"/></linearGradient>
 <linearGradient id="dust" x1="0" y1="0" x2="1" y2="0.3"><stop offset="0" stop-color="#C6C9C0" stop-opacity="0.6"/><stop offset="0.5" stop-color="#C6C9C0" stop-opacity="0.14"/><stop offset="1" stop-color="#C6C9C0" stop-opacity="0.55"/></linearGradient>
 <radialGradient id="warm" cx="0.42" cy="0.92" r="0.95"><stop offset="0" stop-color="#FFC062" stop-opacity="0.42"/><stop offset="0.5" stop-color="{AMBER_D}" stop-opacity="0.16"/><stop offset="1" stop-color="#FFC062" stop-opacity="0"/></radialGradient>
 <radialGradient id="chand" cx="0.5" cy="0.3" r="0.7"><stop offset="0" stop-color="#FFE0A0" stop-opacity="0.5"/><stop offset="1" stop-color="#FFE0A0" stop-opacity="0"/></radialGradient>
 <linearGradient id="shaft" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="#FFE6B0" stop-opacity="0.16"/><stop offset="1" stop-color="#FFE6B0" stop-opacity="0"/></linearGradient>
 <linearGradient id="paperg" x1="0" y1="0" x2="0" y2="1"><stop offset="0" stop-color="{PAPER_BG}"/><stop offset="1" stop-color="{PAPER_BG2}"/></linearGradient>
 <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" stitchTiles="stitch"/><feColorMatrix type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.5 0"/></filter></defs>'''


def build():
    # ── opulent warm Mars lounge ──
    g=r(0,0,W,H,"#26190F")
    g+=r(0,0,W,H,"url(#warm)")
    # crown molding + gilt
    g+=r(0,0,W,40,"#1A1009")+ln(0,40,W,40,GOLD,3,op=0.7)
    # damask wall panels
    for x in range(60,W,300):
        g+=r(x,70,210,560,"#3A2616",op=0.35)+r(x,70,210,560,None,stroke=GOLD,sw=1.5,)
    # wainscot
    g+=r(0,H-170,W,170,"#3E2A18")+ln(0,H-170,W,H-170,GOLD,3,op=0.6)
    for x in range(80,W,240): g+=ln(x,H-170,x,H,"#241608",2)
    # warm rug glow
    g+=f'<ellipse cx="960" cy="1010" rx="900" ry="150" fill="#4A2E16" opacity="0.6"/>'
    # chandelier (gold, glowing) top-center
    g+=f'<ellipse cx="960" cy="150" rx="260" ry="150" fill="url(#chand)"/>'
    g+=ln(960,0,960,70,GOLD,3,op=0.7)
    for dx in (-90,-45,0,45,90):
        g+=f'<circle cx="{960+dx}" cy="98" r="7" fill="{GOLD_L}"/>'+ln(960,70,960+dx,98,GOLD,2,op=0.6)
    g+=path("M 860,96 Q 960,150 1060,96","none",stroke=GOLD,sw=3,op=0.6)
    # framed painting, left wall
    g+=r(90,200,150,200,"#1C120A",stroke=GOLD,sw=6)+r(108,218,114,164,"#3A2A1C")
    g+=path("M 108,360 L 150,300 L 180,340 L 222,290 L 222,382 L 108,382 Z","#241A12")

    # ── the tall cold window (dominant) ──
    wx,wy,ww,wh=300,140,1320,610
    g+=r(wx-30,wy-30,ww+60,wh+60,"#160E07",stroke=GOLD,sw=5)   # gilt frame
    g+=r(wx,wy,ww,wh,"url(#sky)")
    # ruined industrial skyline (cranes, broken towers)
    sky=[(360,70,300),(450,130,180),(620,60,360),(700,150,230),(900,90,300),
         (1040,120,200),(1180,70,340),(1320,140,210),(1470,80,290)]
    for bx,bw,bh in sky:
        g+=r(bx,wy+wh-bh-70,bw,bh,"#4A504C",op=0.6)
    g+=ln(640,wy+wh-430,640,wy+60,"#3E443F",6)+ln(640,wy+80,720,wy+120,"#3E443F",5)  # crane arm
    # heavy ground fog + driving dust
    g+=r(wx,wy+wh-150,ww,150,"url(#fog)")
    g+=r(wx,wy,ww,wh,"url(#dust)")
    for x in range(wx-40,wx+ww,70):
        g+=ln(x,wy,x-90,wy+wh,"#D6D8CE",1.5,op=0.10)
    # ── backlit candidates: the 6 worker variants (art-bible §5 head/tool table) ──
    ground=wy+wh-26
    g+=figure(470,ground,0.62,VESTS[1],headgear="beanie",tool="case",sharp=False)
    g+=figure(665,ground,0.76,VESTS[2],headgear="bumpcap",tool="clipboard",sharp=False)
    g+=figure(930,ground,1.04,VESTS[0],headgear="helmet",tool="torch",sharp=True)  # featured
    g+=figure(1235,ground,0.76,VESTS[3],headgear="hood",tool="cable",sharp=False)
    g+=figure(1425,ground,0.62,VESTS[4],headgear="helmet",tool="torch",sharp=False)
    g+=r(wx,wy,ww,wh,"url(#dust)",op=0.5)               # dust haze over figures
    # mullions
    g+=ln(wx+ww/3,wy,wx+ww/3,wy+wh,"#160E07",10)+ln(wx+2*ww/3,wy,wx+2*ww/3,wy+wh,"#160E07",10)
    g+=ln(wx,wy+wh/2,wx+ww,wy+wh/2,"#160E07",8)
    # warm shaft falling into the room
    g+=path(f"M {wx+140},{wy+wh} L {wx+ww-140},{wy+wh} L {wx+ww+140},{H-170} L {wx-140},{H-170} Z","url(#shaft)")

    # ── opulent foreground silhouettes ──
    g+=path("M 110,1080 L 110,800 Q 110,720 210,720 L 340,720 Q 380,720 380,800 L 380,1080 Z","#1A0F07")  # wing chair
    g+=r(120,870,260,210,"#22140A")+path("M 130,870 Q 250,830 370,870 L 370,910 L 130,910 Z","#2A1A0E")
    g+=r(1560,820,170,200,"#1C1109")                       # side table
    g+=path("M 1600,820 L 1690,820 L 1700,760 L 1590,760 Z","#241509")
    g+=r(1612,720,46,60,"#3A2614")+f'<ellipse cx="1635" cy="720" rx="23" ry="7" fill="{GOLD_L}" opacity="0.5"/>'  # decanter
    g+=path("M 1700,800 L 1712,760 L 1730,760 L 1742,800 Z","#2A1A0E")+f'<ellipse cx="1721" cy="762" rx="15" ry="5" fill="{GOLD_L}" opacity="0.4"/>'  # glass

    # ── brass header plate ──
    g+=r(300,56,1320,62,"#1B120A")+ln(300,118,1620,118,GOLD,2,op=0.55)
    g+=t(330,98,"SELECT YOUR AGENT",30,GOLD_L,font=MONO,sp=6,weight="bold")
    g+=t(1590,96,"1 / 5",24,AMBER_L,font=MONO,anchor="end")
    # amber selection bracket on the current figure
    bx,bw2,bty,bbh=838,184,wy+34,wh-110
    for ax,ay in [(bx,bty),(bx+bw2,bty),(bx,bty+bbh),(bx+bw2,bty+bbh)]:
        sx=1 if ax==bx else -1; sy=1 if ay<wy+wh/2 else -1
        g+=ln(ax,ay,ax+sx*44,ay,AMBER,4)+ln(ax,ay,ax,ay+sy*44,AMBER,4)

    # ── self-dossier chit (paper) ──
    cx0=590
    g+=r(cx0,800,740,152,"url(#paperg)")
    g+=r(cx0,800,740,44,TEAL)
    g+=t(cx0+24,830,"AGENT DOSSIER — THIS IS ME",20,PAPER,font=MONO,sp=4,weight="bold")
    g+=t(cx0+716,829,"FORM BC-04",15,"#A9C0B8",font=MONO,anchor="end")
    g+=r(cx0+28,868,58,58,VESTS[0])+r(cx0+28,868,58,58,None,stroke="#8A7F63",sw=2)
    g+=t(cx0+108,896,"REPAIRMAN — ORANGE HELMET, HI-VIS VEST",22,INK,weight="bold")
    g+=t(cx0+108,928,"Temp hire · supply-closet kit · carries a hand torch",17,INK_D)
    g+=f'<g transform="translate({cx0+582},864) rotate(-9)" opacity="0.82">'+r(0,0,130,54,None,stroke=RED,sw=4)
    g+=t(65,35,"ASSIGNED",20,RED,anchor="middle",weight="bold")+"</g>"
    g+=t(960,1004,"‹ ›  CYCLE LOOK     ·     [E]  THIS IS ME     ·     [ESC]  KEEP CURRENT",
         20,PAPER_D,anchor="middle",sp=2)

    g+=r(0,0,W,H,"url(#vig)")+f'<rect x="0" y="0" width="{W}" height="{H}" filter="url(#grain)" opacity="0.06"/>'
    return g


svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'+DEFS+build()+"</svg>"
os.makedirs(OUT,exist_ok=True)
open(os.path.join(OUT,"14_crew_picker.svg"),"w",encoding="utf-8").write(svg)
print("wrote 14_crew_picker v3")
