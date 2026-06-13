# -*- coding: utf-8 -*-
"""Anti-cartoon demo: the main menu with grit (PM: 'why does it feel so cartoonish').
Flat vector = cartoon. Real game has 256px worn textures + film grain + LC outline +
vignette post. This pushes the MOCKUP toward that: heavy grain, dust blotches, scratches,
scanlines, desaturated sodium-amber, stronger vignette, worn button edges. 3270 font.
Left half = clean (current), right half = gritted, so PM sees the difference."""
import os, base64
OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "palette-options")
W, H = 1920, 1080
_TTF = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project", "Art", "UI", "Fonts", "3270-Regular.ttf")
B64 = base64.b64encode(open(_TTF, "rb").read()).decode(); F = "BC3270"
BLACK="#161613"; AMBER="#E08A2A"; AMBER_L="#E8B24A"; AMBER_D="#7A4E18"
PAPER="#C7BD9E"; PAPER_D="#8A8270"; CRT="#5BD158"; CRT_D="#2A5C30"; RED="#A83A2E"

def esc(s): return s.replace("&","&amp;").replace("<","&lt;").replace(">","&gt;")
def t(x,y,s,size,fill,anchor="start",sp=None,op=None):
    a=f' letter-spacing="{sp}"' if sp else "";o=f' opacity="{op}"' if op else ""
    return f'<text x="{x}" y="{y}" font-family="{F}" font-size="{size}" fill="{fill}" text-anchor="{anchor}"{a}{o}>{esc(s)}</text>'
def r(x,y,w,h,fill,op=None,stroke=None,sw=None):
    o=f' opacity="{op}"' if op is not None else "";st=f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    f=f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{st}/>'
def ln(x1,y1,x2,y2,st,sw=2,op=None):
    o=f' opacity="{op}"' if op is not None else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{st}" stroke-width="{sw}"{o}/>'

DEFS=f'''<defs>
<style>@font-face{{font-family:'{F}';src:url('data:font/ttf;base64,{B64}') format('truetype');}}</style>
<radialGradient id="vig" cx="0.5" cy="0.46" r="0.7"><stop offset="0.32" stop-color="#000" stop-opacity="0"/><stop offset="1" stop-color="#000" stop-opacity="0.88"/></radialGradient>
<radialGradient id="lamp" cx="0.5" cy="0.5" r="0.5"><stop offset="0" stop-color="{AMBER}" stop-opacity="0.26"/><stop offset="1" stop-color="{AMBER}" stop-opacity="0"/></radialGradient>
<filter id="fine"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" stitchTiles="stitch"/><feColorMatrix type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.6 0"/></filter>
<filter id="blotch"><feTurbulence type="fractalNoise" baseFrequency="0.012" numOctaves="3" stitchTiles="stitch"/><feColorMatrix type="matrix" values="0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0.5 0"/></filter></defs>'''

def menu(x0, gritty, label):
    """Render a half-width main menu starting at x0 (panel 860 wide)."""
    g=""
    # dark base
    g+=r(x0,0,860,H,BLACK)
    g+=f'<ellipse cx="{x0+700}" cy="720" rx="360" ry="380" fill="url(#lamp)"/>'
    # office hint
    g+=r(x0+560,600,120,100,"#13150E")+ln(x0+580,648,x0+660,648,CRT_D,3,op=0.6)
    g+=r(x0+720,560,90,300,"#16140F")
    # dirt blotches (gritty only)
    if gritty:
        g+=f'<rect x="{x0}" y="0" width="860" height="{H}" filter="url(#blotch)" opacity="0.16"/>'
        # scratches
        import random; random.seed(7)
        for _ in range(22):
            sx=x0+random.randint(20,820); sy=random.randint(40,1040); a=random.uniform(-0.5,0.5)
            g+=ln(sx,sy,sx+random.randint(40,160),sy+int(random.randint(40,160)*a),"#2A2A24",1,op=random.uniform(0.1,0.3))
    # title
    g+=t(x0+60,150,"BLACK COMMISSION",44,PAPER,sp=1)
    g+=r(x0+62,168,360,4,AMBER)
    g+=t(x0+62,200,"OUTSOURCED COMMISSION OFFICE",15,PAPER_D,sp=4)
    # buttons
    rows=[("CONTINUE SHIFT",True),("NEW OFFICE",False),("JOIN OFFICE",False),("SETTINGS",False),("SHUT DOWN",False)]
    y=280
    for lbl,sel in rows:
        fill = "#241E12" if sel else "#1A1A16"
        bd = AMBER if sel else "#33332B"
        g+=r(x0+60,y,440,66,fill,stroke=bd,sw=2)
        if sel: g+=r(x0+60,y,5,66,AMBER)+t(x0+80,y+44,">",24,AMBER)
        g+=t(x0+110,y+42,lbl,26,AMBER_L if sel else PAPER,sp=1)
        if gritty:  # worn paint flecks on the button edge
            g+=ln(x0+60,y,x0+500,y,"#000",1,op=0.4)+ln(x0+60,y+66,x0+500,y+66,"#000",1,op=0.3)
        y+=80
    # vignette + grain
    g+=r(x0,0,860,H,"url(#vig)")
    if gritty:
        g+=f'<rect x="{x0}" y="0" width="860" height="{H}" filter="url(#fine)" opacity="0.14"/>'
        # scanlines
        g+='<g opacity="0.07">'+"".join(ln(x0,yy,x0+860,yy,"#000",1) for yy in range(0,H,4))+"</g>"
    # label tag
    g+=r(x0+20,20,300,34,"#000",op=0.6)+t(x0+34,44,label,18,AMBER_L if gritty else PAPER_D)
    return g

body=menu(0,False,"CLEAN  (current — cartoonish)")
body+=menu(960,True,"GRITTED  (grain+dust+scanlines)")
body+=ln(960,0,960,H,"#000",4)
svg=f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'+DEFS+body+"</svg>"
os.makedirs(OUT,exist_ok=True)
open(os.path.join(OUT,"grit_compare.svg"),"w",encoding="utf-8").write(svg)
print("wrote grit_compare")
