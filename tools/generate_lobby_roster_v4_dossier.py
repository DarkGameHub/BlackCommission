# -*- coding: utf-8 -*-
"""派工名单卡 v4 —「黑卷宗」direction test (PM 2026-07-04: 不喜欢"市政"概念 → 试
私营事务所机密卷宗拟物; 签名色候选 = 牛皮纸档案黄, 红只做章不做主色).

Surface fiction: a private agency's kraft dossier folder on a dark desk — folder tab
with case number, cream roster sheet clipped inside, one small red intake seal (受理).
No government/municipal vocabulary anywhere.

Signature colour: manila/kraft #B08D57 family (yellow-family per PM instinct, aged &
desaturated to stay clear of LC's saturated orange). Stamp red demoted to seals only.

Output: design/ux/mockups/ui-kit/03_roster_v4_dossier.svg → PNG via headless Chrome.
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "ui-kit")
W, H = 1920, 1080
SANS = "'Helvetica Neue','PingFang SC','Hiragino Sans GB',sans-serif"

# palette
DESK   = "#141210"   # dark desk
KRAFT  = "#B08D57"   # manila folder — SIGNATURE
KRAFT_D= "#8C6E3F"   # folder edge/shadowed kraft
KRAFT_L= "#C4A univers"  # placeholder (unused)
KRAFT_L= "#C2A268"
SHEET  = "#E9E2CB"   # cream roster sheet
INK    = "#2B251C"
DIM    = "#847A61"
HAIR   = "#CFC7AC"
RED    = "#B5372A"   # seal red (oxide, desaturated) — seals ONLY
VESTS  = ["#68724F", "#B9924C", "#8C5937", "#5F6D74"]


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def t(x, y, s, size, fill, anchor="start", weight=None, sp=None, op=None):
    a = f' letter-spacing="{sp}"' if sp else ""
    w = f' font-weight="{weight}"' if weight else ""
    o = f' opacity="{op}"' if op else ""
    return (f'<text x="{x}" y="{y}" font-family="{SANS}" font-size="{size}" fill="{fill}"'
            f' text-anchor="{anchor}"{a}{w}{o}>{esc(s)}</text>')


def r(x, y, w, h, fill, op=None, stroke=None, sw=None, rx=None):
    o = f' opacity="{op}"' if op is not None else ""
    s = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    rr = f' rx="{rx}"' if rx else ""
    f = f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{s}{rr}/>'


def ln(x1, y1, x2, y2, stroke, sw=1, op=None, dash=None):
    o = f' opacity="{op}"' if op is not None else ""
    d = f' stroke-dasharray="{dash}"' if dash else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{stroke}" stroke-width="{sw}"{o}{d}/>'


def dot(cx, cy, rr, state, color):
    if state == "sel":
        return (f'<circle cx="{cx}" cy="{cy}" r="{rr}" fill="{color}"/>'
                f'<circle cx="{cx}" cy="{cy}" r="{rr + 5}" fill="none" stroke="{INK}" stroke-width="1.5"/>')
    if state == "open":
        return f'<circle cx="{cx}" cy="{cy}" r="{rr}" fill="{color}" opacity="0.55"/>'
    return (f'<circle cx="{cx}" cy="{cy}" r="{rr}" fill="none" stroke="{DIM}" '
            f'stroke-width="1.5" stroke-dasharray="3.5 3.5"/>')


b = r(0, 0, W, H, DESK)
b += ('<radialGradient id="vig" cx="0.5" cy="0.42" r="0.85">'
      '<stop offset="0.45" stop-color="#000" stop-opacity="0"/>'
      '<stop offset="1" stop-color="#000" stop-opacity="0.6"/></radialGradient>')
b += ('<linearGradient id="kraftg" x1="0" y1="0" x2="0" y2="1">'
      f'<stop offset="0" stop-color="{KRAFT_L}"/><stop offset="1" stop-color="{KRAFT}"/></linearGradient>')
# faint desk texture: a second folder + a loose sheet peeking under, desk edge light
b += r(310, 260, 620, 560, "#1D1913")
b += r(1290, 700, 360, 260, "#211C14", op=0.8)
b += r(0, 0, W, H, "url(#vig)")

# ── the folder ──
fw, fh = 1160, 760
fx, fy = (W - fw) // 2, (H - fh) // 2 + 8
# folder tab (top-left), case number on it
b += r(fx + 42, fy - 40, 330, 52, "url(#kraftg)", rx=6)
b += t(fx + 66, fy - 6, "案卷 BC-02 · 外勤派工", 19, "#4A3A22", weight="bold", sp=3)
# folder body
b += r(fx + 12, fy + 14, fw, fh, "#000", op=0.55)          # drop shadow
b += r(fx, fy, fw, fh, "url(#kraftg)", rx=8)
b += ln(fx + 26, fy + 24, fx + 26, fy + fh - 24, KRAFT_D, 2, op=0.5)   # spine crease
# string-tie button (档案袋绕线扣) on the right edge
b += f'<circle cx="{fx + fw - 46}" cy="{fy + 92}" r="13" fill="{KRAFT_D}"/>'
b += f'<circle cx="{fx + fw - 46}" cy="{fy + 92}" r="13" fill="none" stroke="#5E4926" stroke-width="2"/>'
b += (f'<path d="M {fx + fw - 46} {fy + 105} q -18 26 -6 52 q 10 22 -8 40" '
      f'fill="none" stroke="#5E4926" stroke-width="2.5" opacity="0.75"/>')
# folder-cover small print (top-right, on kraft)
b += t(fx + fw - 88, fy + 52, "黑色委托事务所 · 内部卷宗", 14, "#4A3A22", anchor="end", sp=3, op=0.85)
b += t(fx + fw - 88, fy + 74, "DOSSIER — DISPATCH COPY", 11, "#4A3A22", anchor="end", sp=3, op=0.6)

# ── the roster sheet clipped inside ──
sw_, sh_ = fw - 128, fh - 118
sx, sy = fx + 64, fy + 66
b += r(sx + 6, sy + 8, sw_, sh_, "#000", op=0.35)
b += r(sx, sy, sw_, sh_, SHEET)
# paperclip top-left
b += (f'<path d="M {sx + 60} {sy - 18} v 64 a 14 14 0 0 0 28 0 v -54 a 9 9 0 0 0 -18 0 v 48" '
      f'fill="none" stroke="#9A958A" stroke-width="5" stroke-linecap="round"/>')

pad = 52
gx, gr = sx + pad, sx + sw_ - pad

# sheet header
hy = sy + 78
b += t(gx, hy - 46, "DISPATCH ROSTER — 内部文件", 12, DIM, sp=4)
b += t(gx, hy, "派 工 名 单", 38, INK, weight="bold", sp=10)
b += t(gx + 2, hy + 32, "黑色委托事务所 · 外勤派工", 15, DIM, sp=3)
# room code on a stapled kraft tag (top-right of sheet)
tgw, tgh = 252, 86
tgx, tgy = gr - tgw, sy + 30
b += f'<g transform="rotate(-1.6 {tgx + tgw/2} {tgy + tgh/2})">'
b += r(tgx + 3, tgy + 4, tgw, tgh, "#000", op=0.25)
b += r(tgx, tgy, tgw, tgh, "url(#kraftg)", rx=4)
b += t(tgx + 20, tgy + 30, "房间码 ROOM CODE", 12, "#4A3A22", sp=3, op=0.8)
b += t(tgx + 18, tgy + 68, "K7F2Q", 38, "#3A2D18", weight="bold", sp=10)
b += ln(tgx + tgw / 2 - 12, tgy - 2, tgx + tgw / 2 + 12, tgy - 2, "#8A857A", 4)   # staple
b += "</g>"
b += ln(gx, sy + 128, gr, sy + 128, HAIR, 1)

# ── roster rows ──
row_h = 96
ry = sy + 154
# 01 you
b += t(gx, ry + 36, "01", 14, DIM, sp=2)
b += r(gx + 40, ry + 4, 8, 42, VESTS[0])
b += t(gx + 70, ry + 40, "老王", 28, INK, weight="bold")
b += t(gx + 136, ry + 40, "（你）", 16, DIM)
px = gx + 300
b += t(px, ry + 38, "‹", 24, DIM)
b += dot(px + 40, ry + 28, 10, "sel", VESTS[0])
b += dot(px + 82, ry + 28, 10, "open", VESTS[1])
b += dot(px + 124, ry + 28, 10, "taken", VESTS[2])
b += dot(px + 166, ry + 28, 10, "open", VESTS[3])
b += t(px + 202, ry + 38, "›", 24, DIM)
b += t(px + 240, ry + 36, "更换工装", 14, DIM, sp=1)
b += ln(px + 240, ry + 44, px + 296, ry + 44, DIM, 1, op=0.6)
# 负责 chip — kraft signature chip (red reserved for seals)
b += r(gr - 88, ry + 6, 88, 38, "url(#kraftg)", rx=3)
b += t(gr - 44, ry + 32, "负 责", 16, "#3A2D18", anchor="middle", weight="bold", sp=4)
b += ln(gx, ry + row_h - 22, gr, ry + row_h - 22, HAIR, 1)

# 02 teammate
ry += row_h
b += t(gx, ry + 36, "02", 14, DIM, sp=2)
b += r(gx + 40, ry + 4, 8, 42, VESTS[2])
b += t(gx + 70, ry + 40, "Agent 2", 28, INK, weight="bold")
b += t(gr - 156, ry + 36, "静音", 15, DIM, anchor="end", sp=2)
b += ln(gr - 186, ry + 44, gr - 156, ry + 44, DIM, 1, op=0.6)
b += t(gr, ry + 36, "按住除名", 15, "#8C5937", anchor="end", sp=2)
b += r(gr - 92, ry + 48, 92, 3, HAIR)
b += r(gr - 92, ry + 48, 52, 3, KRAFT_D)
b += ln(gx, ry + row_h - 22, gr, ry + row_h - 22, HAIR, 1)

# 03/04 empty
for n in ("03", "04"):
    ry += row_h
    b += t(gx, ry + 36, n, 14, DIM, sp=2, op=0.6)
    b += t(gx + 70, ry + 38, "（空缺）", 19, DIM, op=0.65)
    b += ln(gx + 206, ry + 30, gr, ry + 30, DIM, 2, op=0.35, dash="2 10")
    b += ln(gx, ry + row_h - 22, gr, ry + row_h - 22, HAIR, 1, op=0.7)

# ── footer ──
fy2 = sy + sh_ - 64
b += ln(gx, fy2 - 30, gr, fy2 - 30, HAIR, 1)
kx = sx + sw_ / 2 - 220
b += r(kx, fy2 - 6, 88, 34, None, stroke=INK, sw=1.5, rx=2)
b += t(kx + 44, fy2 + 17, "Enter", 16, INK, anchor="middle", weight="bold", sp=2)
b += t(kx + 112, fy2 + 18, "确认到岗，进入办公室", 22, INK, weight="bold", sp=3)
b += t(kx + 112, fy2 + 44, "报到先后不限——办公室就是集合点", 12, DIM, sp=2)
# 受理 round seal (red lives ONLY here) — overlapping the footer rule
sealx, sealy = gr - 74, fy2 - 4
b += f'<g transform="rotate(-12 {sealx} {sealy})" opacity="0.85">'
b += f'<circle cx="{sealx}" cy="{sealy}" r="46" fill="none" stroke="{RED}" stroke-width="3"/>'
b += f'<circle cx="{sealx}" cy="{sealy}" r="37" fill="none" stroke="{RED}" stroke-width="1.5"/>'
b += t(sealx, sealy + 9, "受 理", 24, RED, anchor="middle", weight="bold", sp=6)
b += "</g>"
# sheet micro-footer
b += t(gx, sy + sh_ - 16, "内部文件 · 阅后归档", 11, DIM, sp=3, op=0.7)
b += t(gr, sy + sh_ - 16, "第 1 页 / 共 1 页", 11, DIM, anchor="end", sp=2, op=0.7)

# frame label
b += t(48, 64, "v4 —「黑卷宗」· 牛皮纸签名色 · 红只做章", 22, "#8E897B", sp=2)

svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'
       + b + "</svg>")
os.makedirs(OUT, exist_ok=True)
path = os.path.join(OUT, "03_roster_v4_dossier.svg")
open(path, "w", encoding="utf-8").write(svg)
print("wrote", path)
