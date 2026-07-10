# -*- coding: utf-8 -*-
"""风格概念板 E —「全终端生态」(PM 2026-07-04: "可以都用 terminal 风格吗?")

One fiction, one machine: EVERY UI surface comes out of the office's dying computer.
  - Screens (terminal/menus/settings) = BC-DOS: sulfur-yellow phosphor (NOT LC's
    amber-orange), ASCII box-drawing, command line, scanlines, 3270 face.
  - Documents (roster/settlement/dossiers) = DOT-MATRIX PRINTOUTS from its printer:
    tractor-feed holes, green-bar continuous paper, ribbon-faded mono ink, red chop.
  - HUD = same face, bare sulfur text on world.

Differentiation vs Lethal Company (whose signature IS the terminal): sulfur yellow
(not orange), CJK-first, and the printout layer (LC has no paper at all).

Output: design/ux/mockups/style-concepts/concept_e_terminal.svg → PNG via Chrome.
"""
import os
import base64 as _b64

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "style-concepts")
W, H = 1920, 1080
_TTF = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project", "Art", "UI", "Fonts", "3270-Regular.ttf")
FONT_B64 = _b64.b64encode(open(_TTF, "rb").read()).decode()
SANS = "'Helvetica Neue','PingFang SC',sans-serif"
TERM = "'BC3270','Menlo','PingFang SC',monospace"

BLACK = "#0B0B09"
SULF  = "#D9B03F"   # sulfur yellow — SIGNATURE
SULF_D= "#8E7326"
SCREEN= "#161409"
PAPER = "#E2DCC9"   # printout paper
BAND  = "#C8D6BC"   # green-bar band
INKF  = "#3E3A2E"   # ribbon-faded ink
RED   = "#B5372A"


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def t(x, y, s, size, fill, anchor="start", weight=None, sp=None, op=None, font=TERM):
    a = f' letter-spacing="{sp}"' if sp else ""
    w = f' font-weight="{weight}"' if weight else ""
    o = f' opacity="{op}"' if op else ""
    return (f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" fill="{fill}"'
            f' text-anchor="{anchor}"{a}{w}{o}>{esc(s)}</text>')


def r(x, y, w, h, fill, op=None, stroke=None, sw=None, rx=None, style=None, filt=None):
    o = f' opacity="{op}"' if op is not None else ""
    s = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    rr = f' rx="{rx}"' if rx else ""
    st = f' style="{style}"' if style else ""
    fl = f' filter="url(#{filt})"' if filt else ""
    f = f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{s}{rr}{st}{fl}/>'


def ln(x1, y1, x2, y2, stroke, sw=1, op=None, dash=None):
    o = f' opacity="{op}"' if op is not None else ""
    d = f' stroke-dasharray="{dash}"' if dash else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{stroke}" stroke-width="{sw}"{o}{d}/>'


DEFS = f'''<defs>
  <style>@font-face{{font-family:'BC3270';src:url('data:font/ttf;base64,{FONT_B64}') format('truetype');}}</style>
  <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency="0.75" numOctaves="2" seed="11" stitchTiles="stitch"/>
    <feColorMatrix type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  0 0 0 0.6 0"/></filter>
  <filter id="crtnoise"><feTurbulence type="fractalNoise" baseFrequency="0.4 0.9" numOctaves="2" seed="3"/>
    <feColorMatrix type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  0 0 0 0.5 0"/></filter>
  <filter id="fiber" x="0" y="0" width="100%" height="100%">
    <feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="3" seed="4" result="n"/>
    <feDiffuseLighting in="n" lighting-color="#ffffff" surfaceScale="1.2" result="l">
      <feDistantLight azimuth="45" elevation="58"/></feDiffuseLighting>
    <feComposite in="l" in2="SourceGraphic" operator="in"/>
  </filter>
  <filter id="rough"><feTurbulence type="fractalNoise" baseFrequency="0.012" numOctaves="4" seed="9" result="t"/>
    <feDisplacementMap in="SourceGraphic" in2="t" scale="7"/></filter>
  <radialGradient id="vig"><stop offset="0.42" stop-color="#000" stop-opacity="0"/>
    <stop offset="1" stop-color="#000" stop-opacity="0.8"/></radialGradient>
  <radialGradient id="tube"><stop offset="0" stop-color="{SULF}" stop-opacity="0.10"/>
    <stop offset="0.7" stop-color="{SULF}" stop-opacity="0.03"/><stop offset="1" stop-color="#000" stop-opacity="0"/></radialGradient>
</defs>'''


b = r(0, 0, W, H, BLACK)

# ═══ left rail ═══
b += r(0, 0, 500, H, "#0C0C0D")
b += ln(500, 0, 500, H, "#1E1E20", 2)
b += t(48, 88, "方向 E", 18, "#6E6A60", sp=4, font=SANS)
b += t(48, 146, "全终端生态", 42, "#E6DFCE", weight="bold", sp=4, font=SANS)
b += t(48, 182, "一台破电脑 + 它的针式打印机 = 全部 UI", 15, "#8A857A", font=SANS)
b += t(48, 234, "屏幕 → BC-DOS 硫磺黄荧光终端", 15, "#B9B2A2", font=SANS)
b += t(48, 262, "文书 → 打印机吐出的连页打印件", 15, "#B9B2A2", font=SANS)
b += t(48, 290, "HUD  → 同字体裸字, 世界上直接压字", 15, "#B9B2A2", font=SANS)

b += t(48, 356, "色板 · 签名色打星", 13, "#6E6A60", sp=3, font=SANS)
for i, (hexv, name, star) in enumerate([
        (BLACK, "沥青黑", False), (SULF, "硫磺黄", True), (SCREEN, "屏底", False),
        (PAPER, "打印纸", False), (RED, "章红", False)]):
    x = 48 + i * 86
    b += r(x, 376, 72, 72, hexv, stroke="#2A2A2C", sw=1)
    if star:
        b += t(x + 58, 396, "★", 14, "#111", font=SANS)
    b += t(x, 468, name, 12, "#B9B2A2", font=SANS)
    b += t(x, 486, hexv, 10, "#6E6A60")

b += t(48, 546, "字体制度 (全局唯一字体 = 3270)", 13, "#6E6A60", sp=3, font=SANS)
b += t(48, 592, "DISPATCH ROSTER 0123", 26, "#E6DFCE")
b += t(48, 624, "> boot bc-dos --room K7F2Q_", 17, SULF)
b += t(48, 654, "派工名单 · 黑色委托（中文回退: 苹方）", 13, "#5E5A50", font=SANS)

b += t(48, 712, "vs Lethal Company 的三处切割", 13, "#6E6A60", sp=3, font=SANS)
b += t(48, 744, "① 硫磺黄 ≠ 它的琥珀橙", 14, "#B9B2A2", font=SANS)
b += t(48, 770, "② 中文优先终端 (赛道无人做)", 14, "#B9B2A2", font=SANS)
b += t(48, 796, "③ 打印件层 = 它完全没有的'纸'", 14, "#B9B2A2", font=SANS)

b += t(48, 856, "强: 单一虚构·全局天然统一·字体零成本", 14, "#B9B2A2", font=SANS)
b += t(48, 882, "弱: 终端=LC 招牌资产, 靠上面三刀切割", 14, "#B9B2A2", font=SANS)
b += t(48, H - 40, "Black Commission · UI 风格概念板 E · 2026-07-04", 12, "#4E4B44", sp=2, font=SANS)

# ═══ panel 1: CRT terminal (the screen surface) ═══
mx, my, mw, mh = 560, 96, 660, 560
# bezel
b += r(mx - 26, my - 26, mw + 52, mh + 82, "#26241F", rx=18)
b += r(mx - 26, my - 26, mw + 52, mh + 82, "#000", op=0.35, rx=18, filt="crtnoise")
b += r(mx - 8, my - 8, mw + 16, mh + 16, "#0A0A08", rx=8)
b += r(mx, my, mw, mh, SCREEN, rx=4)
b += f'<ellipse cx="{mx + mw/2}" cy="{my + mh/2}" rx="{mw * 0.72}" ry="{mh * 0.72}" fill="url(#tube)"/>'
# terminal content
tx, ty = mx + 34, my + 46
b += t(tx, ty, "BC-DOS v2.2 — 黑色委托事务所", 16, SULF_D)
b += t(tx, ty + 28, "> roster --room K7F2Q", 16, SULF)
box_w = 74
by = ty + 62
b += t(tx, by,      "┌──────────────────────────────────────────┐", 15, SULF_D)
b += t(tx, by + 26, "│  派工名单 DISPATCH ROSTER      K7F2Q     │", 15, SULF)
b += t(tx, by + 52, "├──────────────────────────────────────────┤", 15, SULF_D)
b += t(tx, by + 78, "│  01  WANG (YOU)   ■□◌□   [LEAD]          │", 15, SULF)
b += t(tx, by + 104,"│  02  AGENT 2         [MUTE] [HOLD:KICK]  │", 15, SULF)
b += t(tx, by + 130,"│  03  (VACANT) ..........................  │", 15, SULF_D)
b += t(tx, by + 156,"│  04  (VACANT) ..........................  │", 15, SULF_D)
b += t(tx, by + 182,"└──────────────────────────────────────────┘", 15, SULF_D)
b += t(tx, by + 224, "> report-in                            ", 16, SULF)
b += r(tx + 148, by + 210, 11, 18, SULF)          # block cursor
b += t(tx, by + 260, "ENTER = 确认到岗 · 打印派工单副本…", 14, SULF_D, font=SANS)
# scanlines + screen glass
for yy in range(int(my) + 2, int(my + mh), 4):
    b += ln(mx, yy, mx + mw, yy, "#000", 1, op=0.28)
b += r(mx, my, mw, mh, SULF, filt="crtnoise", op=0.05, rx=4, style="mix-blend-mode:screen")
b += t(mx + mw / 2, my + mh + 44, "表面 1 · 屏幕 = BC-DOS 终端 (菜单/办公电脑/设置)", 15, "#8A857A", anchor="middle", font=SANS)

# ═══ panel 2: dot-matrix printout (the document surface) ═══
px_, py_, pw_, ph_ = 1300, 76, 540, 700
b += f'<g transform="rotate(1.1 {px_ + pw_/2} {py_ + ph_/2})">'
b += r(px_ + 8, py_ + 10, pw_, ph_, "#000", op=0.5)
b += r(px_, py_, pw_, ph_, PAPER, filt="rough")
b += r(px_, py_, pw_, ph_, "#fff", filt="fiber", op=0.35, style="mix-blend-mode:multiply")
# green-bar bands
band_h = 46
i = 0
yy = py_ + 58
while yy + band_h < py_ + ph_ - 30:
    if i % 2 == 0:
        b += r(px_ + 34, yy, pw_ - 68, band_h, BAND, op=0.4)
    yy += band_h
    i += 1
# tractor-feed holes
for hy_ in range(int(py_) + 24, int(py_ + ph_) - 12, 40):
    b += f'<circle cx="{px_ + 17}" cy="{hy_}" r="6" fill="{BLACK}" opacity="0.85"/>'
    b += f'<circle cx="{px_ + pw_ - 17}" cy="{hy_}" r="6" fill="{BLACK}" opacity="0.85"/>'
b += ln(px_ + 34, py_, px_ + 34, py_ + ph_, "#8A8064", 1, op=0.5, dash="3 5")
b += ln(px_ + pw_ - 34, py_, px_ + pw_ - 34, py_ + ph_, "#8A8064", 1, op=0.5, dash="3 5")
# perforated top/bottom edges
b += ln(px_, py_, px_ + pw_, py_, "#8A8064", 1.5, dash="6 6")
b += ln(px_, py_ + ph_, px_ + pw_, py_ + ph_, "#8A8064", 1.5, dash="6 6")
# printed content (ribbon-faded mono)
cx_ = px_ + 56
b += t(cx_, py_ + 46, "BLACK COMMISSION * DISPATCH COPY", 14, INKF, op=0.92)
b += t(cx_, py_ + 96, "DISPATCH ROSTER", 30, INKF, weight="bold", sp=2, op=0.95)
b += t(cx_, py_ + 124, "ROOM CODE: K7F2Q      2026-07-04 21:14", 14, INKF, op=0.85)
b += t(cx_, py_ + 168, "----------------------------------------", 14, INKF, op=0.7)
b += t(cx_, py_ + 204, "01  WANG (YOU)              [LEAD]", 16, INKF, op=0.95)
b += t(cx_, py_ + 244, "02  AGENT 2                 joined", 16, INKF, op=0.88)
b += t(cx_, py_ + 284, "03  (VACANT)", 16, INKF, op=0.55)
b += t(cx_, py_ + 324, "04  (VACANT)", 16, INKF, op=0.55)
b += t(cx_, py_ + 364, "----------------------------------------", 14, INKF, op=0.7)
b += t(cx_, py_ + 402, "REPORT IN ANY ORDER.", 15, INKF, op=0.85)
b += t(cx_, py_ + 428, "THE OFFICE IS THE MUSTER POINT.", 15, INKF, op=0.85)
b += t(cx_, py_ + 490, "SETTLEMENT PREVIEW ... PENDING", 14, INKF, op=0.6)
b += t(cx_, py_ + 516, "CARGO MANIFEST ....... PENDING", 14, INKF, op=0.6)
b += t(cx_, py_ + 570, "*** END OF PAGE 01 ***", 13, INKF, op=0.5)
# red chop over the printout
b += (f'<g transform="rotate(-9 {px_ + pw_ - 130} {py_ + 620})" opacity="0.82">'
      + r(px_ + pw_ - 200, py_ + 596, 140, 46, None, stroke=RED, sw=3, style="filter:url(#rough)")
      + t(px_ + pw_ - 130, py_ + 626, "已 受 理", 20, RED, anchor="middle", weight="bold", sp=6, font=SANS)
      + "</g>")
b += "</g>"
b += t(px_ + pw_ / 2, py_ + ph_ + 52, "表面 2 · 文书 = 针式打印件 (派工单/结算单/卷宗)", 15, "#8A857A", anchor="middle", font=SANS)

# ═══ panel 3: HUD strip (bare sulfur text on world) ═══
hx, hy, hw, hh = 560, 760, 660, 240
b += r(hx, hy, hw, hh, "#101008")
b += f'<ellipse cx="{hx + hw*0.6}" cy="{hy + hh*0.4}" rx="{hw*0.5}" ry="{hh*0.8}" fill="url(#tube)"/>'
b += r(hx, hy, hw, hh, "#000", op=0.25, filt="crtnoise")
b += t(hx + 28, hy + 44, "货舱 45/120kg", 17, SULF)
b += t(hx + 28, hy + 74, "危险度 ▮▮▮▯▯", 17, SULF_D)
b += t(hx + hw - 28, hy + 44, "[E] 检视", 17, SULF, anchor="end")
b += t(hx + hw - 28, hy + 74, "队友: 2/2 在场", 15, SULF_D, anchor="end")
b += t(hx + hw / 2, hy + hh - 62, "撤 离 倒 计 时  04:32", 22, SULF, anchor="middle", sp=4)
b += ln(hx + 28, hy + hh - 40, hx + hw - 28, hy + hh - 40, SULF_D, 2, op=0.6)
b += t(hx + hw / 2, hy + hh + 36, "表面 3 · HUD = 同字体裸字压在世界上", 15, "#8A857A", anchor="middle", font=SANS)

b += r(0, 0, W, H, "url(#vig)")
b += r(0, 0, W, H, "#fff", filt="grain", op=0.07, style="mix-blend-mode:overlay")

svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'
       + DEFS + b + "</svg>")
os.makedirs(OUT, exist_ok=True)
path = os.path.join(OUT, "concept_e_terminal.svg")
open(path, "w", encoding="utf-8").write(svg)
print("wrote", path)
