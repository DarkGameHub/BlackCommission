# -*- coding: utf-8 -*-
"""风格概念板 E2 —「一台机器 · 一条时间线」PM 终审稿 (2026-07-04 "出发吧")

Fixes from the four-discipline review + PM's own extensions:
  - signature colour: sulfur → GREEN PHOSPHOR #C8B830 band (art-director: +10-15° hue
    from LC's amber-orange, reads as dying municipal-surplus tube, not LC's brand).
  - font system: Source Han Mono (思源等宽) unifies CJK+Latin with 3270 (mockup
    approximates w/ system fallback; production imports the OFL font into Unity).
  - layout: a TIMELINE, not parallel panels (PM: "user 到底要关注哪里" — attention is
    choreographed over time; one moment = one surface. 纸上永远没有按钮.)
  - PM's mechanic: 接任务=接打印纸, 纸=手持物品带进任务 (the real LC-cut).

Beats: ①屏幕时刻(会变的) → ②Enter=打印(定格) → ③撕纸(怪客户长在纸上) → ④纸在手上(带进黑暗)

Output: design/ux/mockups/style-concepts/concept_e2_timeline.svg → PNG via Chrome.
"""
import os
import base64 as _b64

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "style-concepts")
W, H = 1920, 1080
_TTF = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project", "Art", "UI", "Fonts", "3270-Regular.ttf")
FONT_B64 = _b64.b64encode(open(_TTF, "rb").read()).decode()

SANS = "'Helvetica Neue','PingFang SC',sans-serif"
TERM = "'BC3270','Menlo','PingFang SC',monospace"      # production: Source Han Mono 统一中英
HAND = "'Bradley Hand','Hannotate SC',cursive"          # 怪客户手写 (EN primary)

BLACK  = "#0B0B09"
PHOS   = "#C8B830"   # green phosphor — SIGNATURE (was sulfur #D9B03F)
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
  <radialGradient id="vig"><stop offset="0.45" stop-color="#000" stop-opacity="0"/>
    <stop offset="1" stop-color="#000" stop-opacity="0.75"/></radialGradient>
  <radialGradient id="tube"><stop offset="0" stop-color="{PHOS}" stop-opacity="0.10"/>
    <stop offset="1" stop-color="#000" stop-opacity="0"/></radialGradient>
  <radialGradient id="torch"><stop offset="0" stop-color="#E8E2C0" stop-opacity="0.28"/>
    <stop offset="0.6" stop-color="#8E8A60" stop-opacity="0.08"/><stop offset="1" stop-color="#000" stop-opacity="0"/></radialGradient>
</defs>'''

b = r(0, 0, W, H, BLACK)

# ═══ top rail ═══
b += t(56, 66, "概念板 E2 ·「一台机器 · 一条时间线」", 30, "#E6DFCE", weight="bold", sp=2, font=SANS)
b += t(56, 102, "分配律: 会变的住屏幕 · 定格的上纸 · 拿走的进手里     纸上永远没有按钮     语言: EN 默认 / ZH 本地化", 16, "#B9B2A2", sp=1, font=SANS)
# tokens right
tx0 = 1170
for i, (hexv, name, star) in enumerate([
        (BLACK, "沥青黑", False), (PHOS, "磷光绿黄", True), (SCREEN, "屏底", False),
        (PAPER, "打印纸", False), (RED, "章红", False)]):
    x = tx0 + i * 96
    b += r(x, 34, 60, 60, hexv, stroke="#2A2A2C", sw=1)
    if star:
        b += t(x + 46, 52, "★", 13, "#111", font=SANS)
    b += t(x, 112, name, 11, "#9A948A", font=SANS)
b += t(tx0 + 5 * 96 + 14, 58, "字体: 思源等宽 统一中英", 13, "#9A948A", font=SANS)
b += t(tx0 + 5 * 96 + 14, 80, "(样稿回退近似 · 实装导入)", 11, "#5E5A50", font=SANS)
b += ln(0, 138, W, 138, "#1E1E20", 2)

CY, CH = 190, 640   # panels band
cap_y = CY + CH + 46

# ═══ beat 1: 屏幕时刻 (CRT lobby) ═══
p1x, p1w = 56, 420
b += r(p1x - 14, CY - 14, p1w + 28, CH + 28, "#23211C", rx=14)
b += r(p1x, CY, p1w, CH, SCREEN, rx=6)
b += f'<ellipse cx="{p1x + p1w/2}" cy="{CY + CH/2}" rx="{p1w * 0.75}" ry="{CH * 0.7}" fill="url(#tube)"/>'
sx, sy = p1x + 26, CY + 44
b += t(sx, sy, "BC-DOS v2.2", 13, PHOS_D)
b += t(sx, sy + 30, "> roster", 14, PHOS)
b += t(sx, sy + 66, "DISPATCH ROSTER  K7F2Q", 15, PHOS)
b += t(sx, sy + 88, "──────────────────────", 13, PHOS_D)
b += t(sx, sy + 116, "01 WANG (YOU) ■□◌□ [LEAD]", 13, PHOS)
b += t(sx, sy + 144, "02 AGENT 2  [MUTE][KICK]", 13, PHOS)
b += t(sx, sy + 172, "03 (VACANT)...", 13, PHOS_D)
b += t(sx, sy + 200, "04 (VACANT)...", 13, PHOS_D)
b += t(sx, sy + 228, "──────────────────────", 13, PHOS_D)
b += t(sx, sy + 262, "[C] COPY CODE  [F1] INVITE", 13, PHOS)
b += t(sx, sy + 300, "> report-in", 14, PHOS)
b += r(sx + 118, sy + 286, 10, 17, PHOS)
b += t(sx, sy + 340, "ENTER = REPORT IN", 13, PHOS_D)
for yy in range(int(CY) + 2, int(CY + CH), 4):
    b += ln(p1x, yy, p1x + p1w, yy, "#000", 1, op=0.26)
b += r(p1x, CY, p1w, CH, PHOS, filt="crtnoise", op=0.04, rx=6, style="mix-blend-mode:screen")
b += t(p1x + p1w / 2, cap_y, "① 屏幕时刻 — 会变的", 17, "#C9C2B2", anchor="middle", weight="bold", font=SANS)
b += t(p1x + p1w / 2, cap_y + 26, "花名册实时刷新 · 邀请/改色/踢人都在这", 13, "#8A857A", anchor="middle", font=SANS)

# arrow 1
b += t(p1x + p1w + 34, CY + CH / 2 - 10, "Enter", 15, "#8A857A", anchor="middle", font=SANS)
b += ln(p1x + p1w + 16, CY + CH / 2 + 8, p1x + p1w + 54, CY + CH / 2 + 8, "#8A857A", 2)
b += f'<path d="M {p1x + p1w + 54} {CY + CH/2 + 8} l -8 -5 v 10 z" fill="#8A857A"/>'

# ═══ beat 2: 打印 (printer slot) ═══
p2x, p2w = p1x + p1w + 76, 330
# printer body
b += r(p2x, CY + 60, p2w, 150, "#2A2823", rx=10)
b += r(p2x + 18, CY + 84, p2w - 36, 22, "#0A0A08", rx=4)          # slot
b += t(p2x + p2w - 24, CY + 146, "BC-PRN 09", 12, "#6E6A60", anchor="end")
b += f'<circle cx="{p2x + 34}" cy="{CY + 146}" r="6" fill="{PHOS}" opacity="0.9"/>'
b += t(p2x + 50, CY + 151, "PRINTING…", 12, PHOS_D)
# paper emerging from slot
ppw = p2w - 72
ppx = p2x + 36
b += f'<g transform="rotate(-1 {ppx + ppw/2} {CY + 300})">'
b += r(ppx, CY + 95, ppw, 330, PAPER, filt="rough")
b += r(ppx, CY + 95, ppw, 330, "#fff", filt="fiber", op=0.3, style="mix-blend-mode:multiply")
for hy_ in range(int(CY) + 116, int(CY) + 410, 34):
    b += f'<circle cx="{ppx + 12}" cy="{hy_}" r="4.5" fill="{BLACK}" opacity="0.8"/>'
    b += f'<circle cx="{ppx + ppw - 12}" cy="{hy_}" r="4.5" fill="{BLACK}" opacity="0.8"/>'
b += t(ppx + 30, CY + 140, "WORK ORDER 0037", 13, INKF)
b += t(ppx + 30, CY + 168, "JOB: INVENTORY THE", 13, INKF)
b += t(ppx + 30, CY + 190, "     DEAD'S EFFECTS", 13, INKF)
b += t(ppx + 30, CY + 214, "SITE: TOWER BLOCK B", 13, INKF)
b += t(ppx + 30, CY + 240, "------------------", 12, INKF, op=0.7)
b += t(ppx + 30, CY + 266, "1. LETTERS x?", 13, INKF)
b += t(ppx + 30, CY + 292, "2. FAMILY PHOTO x1", 13, INKF)
b += t(ppx + 30, CY + 318, "3. TAKE NOTHING ELSE.", 13, INKF, op=0.85)
b += "</g>"
b += ln(p2x + 18, CY + 95, p2x + p2w - 18, CY + 95, "#000", 3, op=0.4)
b += t(p2x + p2w / 2, cap_y, "② Enter = 打印 — 定格了", 17, "#C9C2B2", anchor="middle", weight="bold", font=SANS)
b += t(p2x + p2w / 2, cap_y + 26, "哒哒哒·卡纸半秒·继续 (机器必须是坏的)", 13, "#8A857A", anchor="middle", font=SANS)

# arrow 2
b += t(p2x + p2w + 34, CY + CH / 2 - 10, "撕", 15, "#8A857A", anchor="middle", font=SANS)
b += ln(p2x + p2w + 16, CY + CH / 2 + 8, p2x + p2w + 54, CY + CH / 2 + 8, "#8A857A", 2)
b += f'<path d="M {p2x + p2w + 54} {CY + CH/2 + 8} l -8 -5 v 10 z" fill="#8A857A"/>'

# ═══ beat 3: 撕下的工单特写 (怪客户长在纸上) ═══
p3x, p3w = p2x + p2w + 76, 330
b += f'<g transform="rotate(2 {p3x + p3w/2} {CY + CH/2})">'
b += r(p3x + 6, CY + 8, p3w, CH - 60, "#000", op=0.5)
b += r(p3x, CY, p3w, CH - 60, PAPER, filt="rough")
b += r(p3x, CY, p3w, CH - 60, "#fff", filt="fiber", op=0.35, style="mix-blend-mode:multiply")
b += ln(p3x, CY + 2, p3x + p3w, CY + 2, "#8A8064", 2, dash="5 7")   # torn edge
for hy_ in range(int(CY) + 26, int(CY + CH) - 70, 36):
    b += f'<circle cx="{p3x + 13}" cy="{hy_}" r="5" fill="{BLACK}" opacity="0.8"/>'
    b += f'<circle cx="{p3x + p3w - 13}" cy="{hy_}" r="5" fill="{BLACK}" opacity="0.8"/>'
qx = p3x + 34
b += t(qx, CY + 52, "BLACK COMMISSION * WORK ORDER", 12, INKF)
b += t(qx, CY + 92, "ORDER 0037 · THE DEAD'S EFFECTS", 14, INKF, weight="bold")
b += t(qx, CY + 120, "CLIENT: MR. ZHOU (REMARRYING)", 12, INKF)
b += t(qx, CY + 148, "--------------------------", 12, INKF, op=0.7)
b += t(qx, CY + 178, "[ ] LETTERS — ALL OF THEM", 13, INKF)
b += t(qx, CY + 208, "[ ] FAMILY PHOTO (EVEN BROKEN)", 13, INKF)
b += t(qx, CY + 238, "[ ] THE WEDDING RING ...", 13, INKF)
# weird client handwriting (the pillar lives HERE)
b += t(qx - 4, CY + 292, "don't read the letters after dark,", 19, "#2C4A8A", font=HAND, rot=-3, rx=qx, ry=CY + 292)
b += t(qx + 12, CY + 322, "they answer.", 19, "#2C4A8A", font=HAND, rot=-3, rx=qx, ry=CY + 322)
b += t(qx + 96, CY + 366, "— Zhou", 16, "#2C4A8A", font=HAND, rot=-2, rx=qx, ry=CY + 366)
# coffee stain + chop
b += f'<ellipse cx="{p3x + p3w - 74}" cy="{CY + 120}" rx="42" ry="30" fill="#6E4F22" opacity="0.14" filter="url(#rough)"/>'
b += (f'<g transform="rotate(-8 {p3x + p3w - 92} {CY + CH - 122})" opacity="0.82">'
      + r(p3x + p3w - 158, CY + CH - 146, 132, 44, None, stroke=RED, sw=3, style="filter:url(#rough)")
      + t(p3x + p3w - 92, CY + CH - 116, "ACCEPTED", 15, RED, anchor="middle", weight="bold", sp=3)
      + "</g>")
b += "</g>"
b += t(p3x + p3w / 2, cap_y, "③ 撕下来 — 怪客户长在纸上", 17, "#C9C2B2", anchor="middle", weight="bold", font=SANS)
b += t(p3x + p3w / 2, cap_y + 26, "手写批注/污渍/涂改 = '怪委托'支柱的家", 13, "#8A857A", anchor="middle", font=SANS)

# arrow 3
b += t(p3x + p3w + 34, CY + CH / 2 - 10, "带走", 15, "#8A857A", anchor="middle", font=SANS)
b += ln(p3x + p3w + 16, CY + CH / 2 + 8, p3x + p3w + 54, CY + CH / 2 + 8, "#8A857A", 2)
b += f'<path d="M {p3x + p3w + 54} {CY + CH/2 + 8} l -8 -5 v 10 z" fill="#8A857A"/>'

# ═══ beat 4: 纸在手上 (first person, in the dark map) ═══
p4x, p4w = p3x + p3w + 76, W - (p3x + p3w + 76) - 56
b += r(p4x, CY, p4w, CH, "#07080A")
# faint torch cone pointing away (light is elsewhere — reading costs light)
b += f'<ellipse cx="{p4x + p4w * 0.22}" cy="{CY + CH * 0.3}" rx="{p4w * 0.3}" ry="{CH * 0.34}" fill="url(#torch)"/>'
# doorway silhouette in torch pool
b += r(p4x + p4w * 0.13, CY + CH * 0.12, 86, 210, "#0E1210")
b += r(p4x + p4w * 0.13 + 8, CY + CH * 0.12 + 10, 70, 200, "#050605")
# arm from bottom-right
b += f'<path d="M {p4x + p4w} {CY + CH} L {p4x + p4w * 0.62} {CY + CH * 0.66} l 60 -28 L {p4x + p4w} {CY + CH * 0.72} Z" fill="#141210"/>'
# held paper (perspective-ish tilt)
hpx, hpy, hpw, hph = p4x + p4w * 0.42, CY + CH * 0.30, 300, 330
b += f'<g transform="rotate(-7 {hpx + hpw/2} {hpy + hph/2}) skewY(-3)">'
b += r(hpx + 8, hpy + 10, hpw, hph, "#000", op=0.6)
b += r(hpx, hpy, hpw, hph, "#D8D2BE", filt="rough")
b += r(hpx, hpy, hpw, hph, "#fff", filt="fiber", op=0.3, style="mix-blend-mode:multiply")
b += r(hpx, hpy, hpw, hph, "#3E3A20", op=0.16)      # dim ambient on paper
for hy_ in range(int(hpy) + 20, int(hpy + hph) - 8, 32):
    b += f'<circle cx="{hpx + 11}" cy="{hy_}" r="4" fill="#0B0B09" opacity="0.8"/>'
    b += f'<circle cx="{hpx + hpw - 11}" cy="{hy_}" r="4" fill="#0B0B09" opacity="0.8"/>'
b += t(hpx + 28, hpy + 44, "WORK ORDER 0037", 12, INKF)
b += t(hpx + 28, hpy + 78, "[x] LETTERS x3", 14, INKF)
b += t(hpx + 28, hpy + 108, "[ ] FAMILY PHOTO x1", 14, INKF)
b += t(hpx + 28, hpy + 138, "[ ] THE WEDDING RING", 14, INKF)
b += t(hpx + 24, hpy + 190, "they answer.", 16, "#2C4A8A", font=HAND, rot=-2, rx=hpx, ry=hpy + 190)
b += (f'<g transform="rotate(-9 {hpx + hpw - 76} {hpy + hph - 58})" opacity="0.8">'
      + r(hpx + hpw - 134, hpy + hph - 78, 116, 40, None, stroke=RED, sw=2.5, style="filter:url(#rough)")
      + t(hpx + hpw - 76, hpy + hph - 51, "ACCEPTED", 13, RED, anchor="middle", weight="bold", sp=2)
      + "</g>")
b += "</g>"
# thumb over the paper edge
b += f'<ellipse cx="{hpx + hpw * 0.78}" cy="{hpy + hph * 0.94}" rx="34" ry="22" fill="#181410" transform="rotate(-14 {hpx + hpw * 0.78} {hpy + hph * 0.94})"/>'
# HUD bare text (phosphor, minimal)
b += t(p4x + 24, CY + 40, "CARGO 45/120kg", 15, PHOS, op=0.9)
b += t(p4x + p4w - 24, CY + 40, "THREAT ▮▮▮▯▯", 15, PHOS_D, anchor="end")
b += t(p4x + p4w / 2, CY + CH - 26, "[G] HAND OVER   [F] STOW   flashlight lowered…", 13, PHOS_D, anchor="middle")
b += t(p4x + p4w / 2, cap_y, "④ 纸在手上 — 带进黑暗", 17, "#C9C2B2", anchor="middle", weight="bold", font=SANS)
b += t(p4x + p4w / 2, cap_y + 26, "占手持槽(与手电互斥) · 可递可丢 · 检视复用现有系统", 13, "#8A857A", anchor="middle", font=SANS)

# footer note
b += ln(0, H - 64, W, H - 64, "#1E1E20", 2)
b += t(56, H - 26, "vs LC 的三刀: 磷光绿黄≠琥珀橙 · 打印件层(它没有纸) · 纸=手持物件(玩法级差异; 英文默认后不再依赖中文切割)", 14, "#8A857A", font=SANS)
b += t(W - 56, H - 26, "Black Commission · E2 · 2026-07-04", 12, "#4E4B44", anchor="end", font=SANS)

b += r(0, 0, W, H, "url(#vig)")
b += r(0, 0, W, H, "#fff", filt="grain", op=0.06, style="mix-blend-mode:overlay")

svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'
       + DEFS + b + "</svg>")
os.makedirs(OUT, exist_ok=True)
path = os.path.join(OUT, "concept_e2_timeline.svg")
open(path, "w", encoding="utf-8").write(svg)
print("wrote", path)
