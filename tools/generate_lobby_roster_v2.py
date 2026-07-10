# -*- coding: utf-8 -*-
"""派工名单卡 v2 — P0-1 lobby roster card, per design/ux/lobby.md (PM 锁定 2026-06-12)
+ ui-review-priorities-2026-07-03.md. Replaces the retired 940×620 green WaitingTerminal.

Language: Chinese-first (game default locale = ZH), EN small subtext where the spec has it.
Colours follow the IMPLEMENTED theme (BlackCommissionUiTheme): document header = OXBLOOD
#5A2E2A (the "civic teal" token was retired from UI chrome by PM 2026-06-12 — the docs'
name survives, the colour does not), aged paper #D6CCAE, stamp red #C23A2B, amber room code.

CJK renders via PingFang SC fallback (3270 is Latin-only), matching the in-game
MvpFontProvider behaviour (3270 for Latin + system CJK).

Output: design/ux/mockups/ui-kit/03_lobby_roster_v2.svg  (render to PNG via headless Chrome)
Shows the HOST view (richest state: 2/4 joined, own row palette, kick/mute on row 02).
"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import generate_ui_mockups as k

OUT = k.OUT
W, H = k.W, k.H
CJK = "BC3270, 'PingFang SC', 'Hiragino Sans GB', sans-serif"
OX     = "#5A2E2A"   # oxblood — document header band (theme MilitaryGreen slot)
OX_TX  = "#E2C9B8"   # warm bone text on oxblood
INK    = k.INK
INK_D  = k.INK_D
RULE   = "#A2967A"   # paper rule lines
EMPTY  = "#9A8F73"   # (空缺) grey-ink
AMBER  = k.AMBER_L

# character vest palette (mockup stand-ins for CharacterIndex colours)
VESTS = ["#55624A", "#C98A2A", "#8C5937", "#5C6B72"]


def zh(x, y, s, size, fill, anchor="start", weight=None, sp=None, op=None):
    return k.t(x, y, s, size, fill, font=CJK, anchor=anchor, weight=weight, sp=sp, op=op)


def dot(cx, cy, rr, state, color):
    """palette dot: sel=filled+ring, open=outline, taken=dashed grey"""
    if state == "sel":
        return (f'<circle cx="{cx}" cy="{cy}" r="{rr}" fill="{color}"/>'
                f'<circle cx="{cx}" cy="{cy}" r="{rr + 4}" fill="none" stroke="{INK}" stroke-width="2"/>')
    if state == "open":
        return f'<circle cx="{cx}" cy="{cy}" r="{rr}" fill="{color}" opacity="0.5" stroke="{INK_D}" stroke-width="1.5"/>'
    # taken → ◌ 置灰 (occupied by a teammate)
    return (f'<circle cx="{cx}" cy="{cy}" r="{rr}" fill="none" stroke="#8A7F63" '
            f'stroke-width="2" stroke-dasharray="4 4"/>')


b = k.office_bg()
b += k.logo()
b += k.veil()   # 卡片弹出时背后场景调暗 60% (spec)

# ── the card ──
cw, ch = 1000, 660
cx, cy = (W - cw) // 2, (H - ch) // 2 - 10
b += k.r(cx + 14, cy + 16, cw, ch, "#000", op=0.5)          # drop shadow
b += k.r(cx, cy, cw, ch, "url(#paperg)")

# header band — oxblood, 派工名单 + unit line left, room code right
hb = 88
b += k.r(cx, cy, cw, hb, OX)
b += k.r(cx + 22, cy + 20, 8, 48, OX_TX, op=0.85)            # ▌ civic sidebar tick
b += zh(cx + 48, cy + 44, "派 工 名 单", 30, "#EFE6D2", weight="bold", sp=6)
b += zh(cx + 48, cy + 72, "黑色委托事务所 · 外勤派工", 16, "#C9B4A4", sp=2)
b += zh(cx + cw - 220, cy + 36, "房间码 / ROOM CODE", 14, "#C9B4A4", anchor="end", sp=1)
b += k.t(cx + cw - 220, cy + 76, "K7F2Q", 40, AMBER, font=k.MONO, anchor="end", weight="bold", sp=8)
b += k.t(cx + cw - 26, cy + 36, "FORM BC-02", 14, "#A98F80", font=k.MONO, anchor="end")
b += zh(cx + cw - 26, cy + 76, "[C] 复制", 15, "#C9B4A4", anchor="end")

# ── roster: 4 fixed slots ──
rows_y = cy + hb + 24
row_h = 96
pad = 40

# row 01 — you (host): vest strip, name, palette dots, 负责 chip, 更换工装 link
ry = rows_y
b += k.t(cx + pad, ry + 40, "01", 20, INK_D, font=k.MONO)
b += k.r(cx + pad + 44, ry + 14, 10, 40, VESTS[0])                       # vest colour strip
b += zh(cx + pad + 74, ry + 44, "老王（你）", 26, INK, weight="bold")
# palette quick-switch ‹ ● ○ ◌ ○ ›
px = cx + pad + 320
b += zh(px, ry + 42, "‹", 24, INK_D)
b += dot(px + 42, ry + 34, 11, "sel", VESTS[0])
b += dot(px + 84, ry + 34, 11, "open", VESTS[1])
b += dot(px + 126, ry + 34, 11, "taken", VESTS[2])                       # Agent 2 占用
b += dot(px + 168, ry + 34, 11, "open", VESTS[3])
b += zh(px + 204, ry + 42, "›", 24, INK_D)
b += zh(px + 240, ry + 42, "更换工装…", 16, INK_D)
# 负责 chip (host badge) — oxblood chip, fixed width
chx = cx + cw - pad - 96
b += k.r(chx, ry + 12, 96, 40, OX)
b += zh(chx + 48, ry + 39, "负 责", 18, "#EFE6D2", anchor="middle", weight="bold", sp=4)
b += k.ln(cx + pad, ry + row_h - 22, cx + cw - pad, ry + row_h - 22, RULE, 1.5)

# row 02 — teammate (host view: mute + hold-to-kick)
ry += row_h
b += k.t(cx + pad, ry + 40, "02", 20, INK_D, font=k.MONO)
b += k.r(cx + pad + 44, ry + 14, 10, 40, VESTS[2])
b += zh(cx + pad + 74, ry + 44, "Agent 2", 26, INK, weight="bold")
b += zh(cx + cw - pad - 210, ry + 42, "[静音]", 18, INK_D)
b += zh(cx + cw - pad, ry + 42, "[按住除名]", 18, INK_D, anchor="end")
# hold-progress ink bar under the kick button (0.8s 按住=签字), shown mid-fill
b += k.r(cx + cw - pad - 118, ry + 52, 118, 5, "#C6BB9B")
b += k.r(cx + cw - pad - 118, ry + 52, 66, 5, OX)
b += k.ln(cx + pad, ry + row_h - 22, cx + cw - pad, ry + row_h - 22, RULE, 1.5)

# rows 03/04 — (空缺) dotted
for n in ("03", "04"):
    ry += row_h
    b += k.t(cx + pad, ry + 40, n, 20, EMPTY, font=k.MONO)
    b += zh(cx + pad + 74, ry + 42, "（空缺）", 22, EMPTY)
    b += (f'<line x1="{cx + pad + 210}" y1="{ry + 34}" x2="{cx + cw - pad}" y2="{ry + 34}" '
          f'stroke="{EMPTY}" stroke-width="3" stroke-dasharray="3 9" stroke-linecap="round" opacity="0.6"/>')
    b += k.ln(cx + pad, ry + row_h - 22, cx + cw - pad, ry + row_h - 22, RULE, 1.5, op=0.8)

# ── footer: main action + note + 成立 stamp ──
fy = cy + ch - 92
b += k.ln(cx + pad, fy - 18, cx + cw - pad, fy - 18, "#8A7F63", 2)
b += zh(cx + cw / 2, fy + 26, "[ Enter ]  确认到岗，进入办公室", 26, INK, anchor="middle", weight="bold", sp=2)
b += zh(cx + cw / 2, fy + 60, "报到先后不限——办公室就是集合点", 16, INK_D, anchor="middle")
b += f'<g transform="translate({cx + cw - 150},{fy + 2}) rotate(-9)" opacity="0.8">'
b += k.r(0, 0, 108, 56, None, stroke=k.RED, sw=4)
b += zh(54, 38, "成 立", 24, k.RED, anchor="middle", weight="bold", sp=4)
b += "</g>"

b += k.grain(0.05) + k.vignette()

os.makedirs(OUT, exist_ok=True)
path = os.path.join(OUT, "03_lobby_roster_v2.svg")
open(path, "w", encoding="utf-8").write(k.svg(b))
print("wrote", path)
