# -*- coding: utf-8 -*-
"""P0-1 大厅 · E2 版 mockup —— BC-DOS ROSTER 屏 (2026-07-04)

依据:
  - design/ux/e2-style-concept.md  (LOCKED: 色token/分配律/键盘语法/静态扫描线)
  - design/ux/lobby.md             (信息结构原样保留: 4工号槽/房间码/换色/静音/按住除名/Enter到岗/各自进入)

两张 1:1 (1920x1080) 游戏内画面:
  1. lobby_e2_roster  — 等待态 (会变的·住屏幕)
  2. lobby_e2_print   — Enter 后打印过场 (定格·上纸: 屏幕回显 + 派工单从打印机吐出)
"""
import os
import base64 as _b64

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups")
W, H = 1920, 1080
_TTF = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project", "Art", "UI", "Fonts", "3270-Regular.ttf")
FONT_B64 = _b64.b64encode(open(_TTF, "rb").read()).decode()

TERM = "'BC3270','Menlo',monospace"
HAND = "'Bradley Hand','Hannotate SC',cursive"

BLACK  = "#0B0B09"
PHOS   = "#C8B830"
PHOS_D = "#7E7420"
SCREEN = "#121307"
PAPER  = "#E2DCC9"
INKF   = "#3E3A2E"
RED    = "#B5372A"


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def t(x, y, s, size, fill, anchor="start", weight=None, sp=None, op=None, font=TERM, rot=None, rx=None, ry=None):
    a = f' letter-spacing="{sp}"' if sp else ""
    w = f' font-weight="{weight}"' if weight else ""
    o = f' opacity="{op}"' if op else ""
    tr = f' transform="rotate({rot} {rx} {ry})"' if rot is not None else ""
    return (f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" fill="{fill}"'
            f' text-anchor="{anchor}"{a}{w}{o}{tr}>{esc(s)}</text>')


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
  <filter id="fiber" x="0" y="0" width="100%" height="100%">
    <feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="3" seed="4" result="n"/>
    <feDiffuseLighting in="n" lighting-color="#ffffff" surfaceScale="1.2" result="l">
      <feDistantLight azimuth="45" elevation="58"/></feDiffuseLighting>
    <feComposite in="l" in2="SourceGraphic" operator="in"/>
  </filter>
  <filter id="rough"><feTurbulence type="fractalNoise" baseFrequency="0.014" numOctaves="4" seed="9" result="t"/>
    <feDisplacementMap in="SourceGraphic" in2="t" scale="7"/></filter>
  <filter id="crtnoise"><feTurbulence type="fractalNoise" baseFrequency="0.4 0.9" numOctaves="2" seed="3"/>
    <feColorMatrix type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  0 0 0 0.5 0"/></filter>
  <filter id="glow"><feGaussianBlur stdDeviation="2.2" result="b"/>
    <feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter>
  <radialGradient id="vig"><stop offset="0.5" stop-color="#000" stop-opacity="0"/>
    <stop offset="1" stop-color="#000" stop-opacity="0.6"/></radialGradient>
  <radialGradient id="tube"><stop offset="0" stop-color="{PHOS}" stop-opacity="0.07"/>
    <stop offset="1" stop-color="#000" stop-opacity="0"/></radialGradient>
</defs>'''

# 马甲色 (palette dots) — 与现有工装色板一致的抽象位: 磷光屏上以形状区分而非彩色
# (单色屏虚构: 屏幕本身显示不了彩色 → 用 A/B/C/D 位 + 实心/空心表达; 真实颜色在3D工装上)
SLOT_DOTS = "<A> B  C  D"   # own row: current pick bracketed


def crt_overlay(body):
    """static scanlines + tube glow + noise (a11y: 静态纹理, 不滚动)"""
    o = f'<ellipse cx="{W/2}" cy="{H/2}" rx="{W*0.72}" ry="{H*0.68}" fill="url(#tube)"/>'
    lines = "".join(ln(0, yy, W, yy, "#000", 1, op=0.22) for yy in range(2, H, 4))
    noise = r(0, 0, W, H, PHOS, filt="crtnoise", op=0.03, style="mix-blend-mode:screen")
    vig = r(0, 0, W, H, "url(#vig)")
    grain = r(0, 0, W, H, "#fff", filt="grain", op=0.04, style="mix-blend-mode:overlay")
    return o + body + lines + noise + vig + grain


def header_footer(status_right):
    b = ""
    b += t(72, 64, "BC-DOS v2.2", 22, PHOS_D, sp=1)
    b += t(W / 2, 64, "BLACK COMMISSION DISPATCH SYSTEM", 22, PHOS_D, anchor="middle", sp=4)
    b += t(W - 72, 64, "MEM 384KB OK", 22, PHOS_D, anchor="end")
    b += ln(72, 88, W - 72, 88, PHOS_D, 2, op=0.7)
    b += ln(72, H - 88, W - 72, H - 88, PHOS_D, 2, op=0.7)
    b += t(72, H - 52, "LINK OK · HOST: WANG", 20, PHOS_D)
    b += t(W - 72, H - 52, status_right, 20, PHOS_D, anchor="end")
    return b


# ═══════════════════════════════ 1. ROSTER 等待态 ═══════════════════════════════
b = r(0, 0, W, H, SCREEN)
b += header_footer("NO FUNDS. NO EXCUSES.")

b += t(72, 150, "> roster", 26, PHOS)

lx = 120                      # left column: roster
b += f'<g filter="url(#glow)">' + t(lx, 226, "DISPATCH ROSTER", 34, PHOS, weight="bold", sp=6) + "</g>"
b += t(lx, 262, "─" * 42, 24, PHOS_D)

rows = [
    ("01", "WANG (YOU)", True,  "[LEAD]", True, True),
    ("02", "CHEN",       False, "[MUTE]  [HOLD K - REMOVE]", True, False),
    ("03", "(VACANT) " + "." * 18, False, "", False, False),
    ("04", "(VACANT) " + "." * 18, False, "", False, False),
]
ry_ = 330
for num, name, dots, tail, filled, you in rows:
    col = PHOS if filled else PHOS_D
    op = None if filled else 0.75
    if you:
        b += r(lx - 20, ry_ - 34, 950, 50, PHOS, op=0.07)
    b += t(lx, ry_, num, 28, col, op=op)
    b += t(lx + 70, ry_, name, 28, col, weight="bold" if you else None, op=op)
    if dots:
        # 撞色置灰: C 已被占 → dim (单色屏用 A/B/C/D 位表达马甲色, 真彩在3D工装上)
        b += t(lx + 520, ry_, "<A>", 28, PHOS)
        b += t(lx + 592, ry_, "B", 28, PHOS)
        b += t(lx + 648, ry_, "C", 28, PHOS_D, op=0.5)
        b += t(lx + 704, ry_, "D", 28, PHOS)
    if tail:
        b += t(lx + 950, ry_, tail, 22, PHOS if you else PHOS_D, anchor="end")
    ry_ += 74

# hold-to-remove ink bar (0.8s) — 紧贴 02 行尾提示的正下方, 演示按住中态
b += r(lx + 700, 418, 250, 10, None, stroke=PHOS_D, sw=1)
b += r(lx + 700, 418, 155, 10, PHOS, op=0.8)

b += t(lx, ry_ + 10, "─" * 42, 24, PHOS_D)
b += t(lx, ry_ + 62, "[<] [>] SUIT COLOR      [V] CHANGE WORKWEAR", 24, PHOS)
b += t(lx, ry_ + 100, "(TAKEN COLOR SHOWS DIM — PICK ANOTHER)", 18, PHOS_D)

# right column: ROOM CODE (host 被问最多的一件事 → 全屏第二大字)
cx = 1310
b += t(cx, 226, "ROOM CODE", 26, PHOS_D, sp=4)
b += r(cx - 6, 250, 480, 130, None, stroke=PHOS_D, sw=2)
b += f'<g filter="url(#glow)">' + t(cx + 234, 342, "K7F2Q", 84, PHOS, anchor="middle", weight="bold", sp=16) + "</g>"
b += t(cx, 430, "[C] COPY CODE", 24, PHOS)
b += t(cx, 474, "[F1] INVITE FRIEND", 24, PHOS)
b += t(cx, 512, "(READ IT OVER VOICE — IT WORKS TOO)", 16, PHOS_D)

# bottom action: report-in
b += t(72, 830, "> report-in", 26, PHOS)
b += r(238, 806, 14, 30, PHOS)   # block cursor
b += f'<g filter="url(#glow)">' + t(120, 896, "ENTER = REPORT IN", 32, PHOS, weight="bold", sp=4) + "</g>"
b += t(120, 936, "AGENTS REPORT IN INDIVIDUALLY — NOBODY WAITS FOR NOBODY.", 20, PHOS_D)

svg1 = crt_overlay(b)

# ═══════════════════════════════ 2. 打印过场态 ═══════════════════════════════
b = r(0, 0, W, H, SCREEN)
b += header_footer("2/4 REPORTED IN")

# screen side: frozen echo (会变的定格了 → 打印)
b += t(72, 150, "> report-in", 26, PHOS_D)
b += t(120, 226, "WANG … REPORTED IN", 28, PHOS_D)
b += t(120, 300, "PRINTING DISPATCH SLIP", 34, PHOS, weight="bold", sp=4)
# progress w/ jam beat
b += r(120, 336, 560, 22, None, stroke=PHOS_D, sw=2)
b += r(124, 340, 350, 14, PHOS, op=0.9)
b += t(700, 354, "[JAM] … RETRY", 24, PHOS, op=0.9)
b += t(120, 420, "TEAR THE SLIP AT THE PRINTER TO PROCEED.", 22, PHOS_D)
b += t(120, 500, "> _", 26, PHOS)
b += r(158, 476, 14, 30, PHOS)

# world side (right half): printer spitting the dispatch slip — 屏幕之外的世界一角
px, pw = 1120, 520
panel_cx = px - 60 + (pw + 440) / 2
b += r(px - 60, 170, pw + 440, 760, "#07080A")            # dark office corner
b += t(panel_cx, 208, "— WORLD SIDE: THE PRINTER, 3m LEFT OF THE CRT —", 16, "#5E5A50", anchor="middle")
b += r(px, 300, pw, 150, "#2A2823", rx=10)
b += r(px + 100, 326, pw - 200, 22, "#0A0A08", rx=4)       # slot (narrower than body → flanks stay visible)
b += t(px + pw - 20, 434, "BC-PRN 09", 13, "#6E6A60", anchor="end")
b += f'<circle cx="{px + 34}" cy="{420}" r="7" fill="{PHOS}" opacity="0.95"/>'
b += t(px + 20, 444, "PRINTING…", 13, PHOS_D)
# slip emerging from the slot, draping down the printer front
spw = pw - 220
spx = px + 110
b += f'<g transform="rotate(-1.5 {spx + spw/2} 640)">'
b += r(spx, 338, spw, 430, PAPER, filt="rough")
b += r(spx, 338, spw, 430, "#fff", filt="fiber", op=0.3, style="mix-blend-mode:multiply")
for hy_ in range(366, 750, 36):
    b += f'<circle cx="{spx + 13}" cy="{hy_}" r="5" fill="{BLACK}" opacity="0.8"/>'
    b += f'<circle cx="{spx + spw - 13}" cy="{hy_}" r="5" fill="{BLACK}" opacity="0.8"/>'
qx = spx + 34
b += t(qx, 392, "BLACK COMMISSION * DISPATCH", 13, INKF)
b += t(qx, 428, "SLIP 0119 · CREW OF 2", 15, INKF, weight="bold")
b += t(qx, 458, "WANG — LEAD", 14, INKF)
b += t(qx, 486, "CHEN", 14, INKF)
b += t(qx, 516, "-" * 24, 13, INKF, op=0.7)
b += t(qx, 548, "REPORT TO: THE OFFICE", 14, INKF)
b += t(qx, 578, "VAN KEY: HOOK BY DOOR", 14, INKF)
b += t(qx - 2, 640, "back door sticks. push.", 19, "#2C4A8A", font=HAND, rot=-3, rx=qx, ry=640)
b += (f'<g transform="rotate(-8 {spx + spw - 90} 716)" opacity="0.82">'
      + r(spx + spw - 156, 692, 132, 44, None, stroke=RED, sw=3, style="filter:url(#rough)")
      + t(spx + spw - 90, 722, "ON DUTY", 15, RED, anchor="middle", weight="bold", sp=3)
      + "</g>")
b += "</g>"
b += ln(px + 110, 338, px + pw - 110, 338, "#000", 3, op=0.4)

svg2 = crt_overlay(b)

os.makedirs(OUT, exist_ok=True)
for name, body in [("lobby_e2_roster", svg1), ("lobby_e2_print", svg2)]:
    svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'
           + DEFS + body + "</svg>")
    p = os.path.join(OUT, name + ".svg")
    open(p, "w", encoding="utf-8").write(svg)
    # HTML wrapper: 消除 Chrome 直开 SVG 的适配白边 (底部白条 bug)
    html = f'<!DOCTYPE html><html><head><meta charset="utf-8"></head><body style="margin:0;overflow:hidden">{svg}</body></html>'
    open(os.path.join(OUT, name + ".html"), "w", encoding="utf-8").write(html)
    print("wrote", p)
