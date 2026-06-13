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


def figure(cx, ground, sc, vest, sharp=True):
    """Backlit trudging laborer, ~7.5 heads. Dark body + cold rim light on the
    window-facing (upper/right) edges so it reads as a 3D person, not a blob."""
    op = 1.0 if sharp else 0.5
    body = "#0A0B0C" if sharp else "#15161A"
    coat = "#0E0F10" if sharp else "#181A1E"
    hh = 360*sc                       # total height
    head_r = 20*sc
    hx = cx; hy = ground-hh           # head top
    neck = hy+head_r*1.7
    sh = neck+8*sc                    # shoulder line
    hip = ground-hh*0.45
    rim = RIM
    g=f'<g opacity="{op}">'
    # ── back leg (planted) ──
    g+=path(f"M {cx-6*sc},{hip} L {cx-30*sc},{ground-6*sc} L {cx-30*sc},{ground} L {cx-12*sc},{ground} "
            f"L {cx+2*sc},{hip} Z", body)
    # ── front leg (stride, knee bent) ──
    g+=path(f"M {cx+6*sc},{hip} L {cx+26*sc},{hip+70*sc} L {cx+44*sc},{ground} L {cx+24*sc},{ground} "
            f"L {cx+10*sc},{hip+72*sc} L {cx-2*sc},{hip} Z", body)
    # boots
    g+=r(cx-32*sc,ground-10*sc,24*sc,12*sc,body)+r(cx+22*sc,ground-10*sc,26*sc,12*sc,body)
    # ── heavy backpack behind the torso ──
    g+=r(cx-40*sc,sh+4*sc,20*sc,hh*0.36,coat,rx=6*sc)
    g+=r(cx-44*sc,sh+hh*0.18,10*sc,10*sc,coat)        # side pocket
    # ── coat torso (tapered, slight forward hunch) ──
    g+=path(f"M {cx-26*sc},{sh} C {cx-34*sc},{(sh+hip)/2} {cx-28*sc},{hip} {cx-18*sc},{hip+10*sc} "
            f"L {cx+22*sc},{hip+10*sc} C {cx+30*sc},{hip} {cx+30*sc},{(sh+hip)/2} {cx+22*sc},{sh} "
            f"C {cx+10*sc},{sh-12*sc} {cx-14*sc},{sh-12*sc} {cx-26*sc},{sh} Z", coat)
    # coat fold lines (a touch lighter for form)
    g+=ln(cx-8*sc,sh+10*sc,cx-2*sc,hip,"#16171A",2*sc,op=0.8)
    g+=ln(cx+8*sc,sh+8*sc,cx+12*sc,hip,"#16171A",2*sc,op=0.6)
    # ── forward arm holding a strap / swinging ──
    g+=path(f"M {cx+18*sc},{sh+6*sc} C {cx+34*sc},{sh+40*sc} {cx+30*sc},{hip-10*sc} {cx+20*sc},{hip} "
            f"L {cx+10*sc},{hip-4*sc} C {cx+18*sc},{(sh+hip)/2} {cx+14*sc},{sh+30*sc} {cx+8*sc},{sh+10*sc} Z", coat)
    # chest strap across (pack strap)
    g+=ln(cx-18*sc,sh+8*sc,cx+16*sc,hip-30*sc,"#0A0B0C",4*sc)
    # ── vest patch (the only colour = your identity) ──
    if sharp:
        g+=r(cx-12*sc,sh+14*sc,24*sc,30*sc,vest)
        g+=ln(cx-12*sc,sh+14*sc,cx+12*sc,sh+14*sc,RIM,1.5,op=0.4)
    else:
        g+=r(cx-9*sc,sh+12*sc,18*sc,22*sc,"#2C2A26")
    # ── hooded head, face in shadow ──
    g+=f'<circle cx="{hx}" cy="{hy+head_r}" r="{head_r}" fill="{body}"/>'
    g+=path(f"M {hx-head_r-4*sc},{hy+head_r*1.5} Q {hx},{hy-head_r*0.6} {hx+head_r+4*sc},{hy+head_r*1.5} "
            f"L {hx+head_r+1*sc},{hy+head_r*1.9} Q {hx},{hy+head_r*0.5} {hx-head_r-1*sc},{hy+head_r*1.9} Z", coat)
    # ── COLD RIM LIGHT from the bright window: thin highlight on upper/right edges ──
    if sharp:
        g+=path(f"M {hx-2*sc},{hy+2*sc} Q {hx+head_r-2*sc},{hy+head_r*0.4} {hx+head_r-3*sc},{hy+head_r*1.3}",
                "none",stroke=rim,sw=3*sc)                      # head rim
        g+=ln(cx+20*sc,sh-2*sc,cx+24*sc,hip,rim,3*sc,op=0.85)   # shoulder/arm rim
        g+=ln(cx+8*sc,sh-9*sc,cx+20*sc,sh-2*sc,rim,3*sc,op=0.7) # shoulder top rim
        g+=ln(cx+30*sc,hip+30*sc,cx+42*sc,ground-6*sc,rim,2.5*sc,op=0.6)  # front shin rim
    else:
        g+=ln(cx+14*sc,sh,cx+16*sc,hip,rim,2,op=0.4)
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
    # ── backlit candidates (possible versions of you) ──
    ground=wy+wh-26
    g+=figure(470,ground,0.60,VESTS[1],sharp=False)
    g+=figure(660,ground,0.74,VESTS[2],sharp=False)
    g+=figure(930,ground,1.06,VESTS[0],sharp=True)     # current — tall, detailed, rim-lit
    g+=figure(1240,ground,0.74,VESTS[3],sharp=False)
    g+=figure(1430,ground,0.60,VESTS[4],sharp=False)
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
    g+=t(cx0+108,896,"\"COPPER\" — HEAVY COAT, FULL PACK",24,INK,weight="bold")
    g+=t(cx0+108,928,"Surface-rated · the build you'll wear on the job",17,INK_D)
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
