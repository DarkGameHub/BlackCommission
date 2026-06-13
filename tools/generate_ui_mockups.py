# -*- coding: utf-8 -*-
"""Generates the full UI mockup kit (13 screens) as 1920x1080 SVGs.

Visual languages (locked):
  - Menu layer  : mockup B — olive gradient, amber accents, quiet rows
  - Modal layer : stamped civic paperwork cards (paper/teal/ink/stamp-red)
  - Screen layer: office computer CRT — phosphor green on near-black
  - HUD layer   : field work-order, quiet, per design/ux/hud.md

Output: design/ux/mockups/ui-kit/NN_name.svg
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "ui-kit")
W, H = 1920, 1080

YH = "Microsoft YaHei"
KAI = "KaiTi,FangSong"
MONO = "Consolas,Microsoft YaHei"

# palette
OLIVE_TOP, OLIVE_BOT = "#222B22", "#181F18"
BONE, AMBER, AMBER_D = "#D8D2BC", "#E8B25C", "#C78A33"
ROW, ROWDIM, MUTED, DIV = "#BFC4AE", "#5A6052", "#7E8872", "#3A453A"
PAPER, PAPER_D, INK, INK_SOFT = "#CFC4A4", "#B8AD8E", "#26201A", "#4A4136"
TEAL, RED = "#3F5F5C", "#C23A2B"
CRT_BG, CRT, CRT_DIM = "#0A140C", "#6CDC6C", "#2E5C36"


def text(x, y, s, size, fill, font=YH, anchor="start", spacing=None, weight=None, opacity=None, style=None):
    a = f' letter-spacing="{spacing}"' if spacing else ""
    w = f' font-weight="{weight}"' if weight else ""
    o = f' opacity="{opacity}"' if opacity else ""
    st = f' font-style="{style}"' if style else ""
    return (f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" fill="{fill}"'
            f' text-anchor="{anchor}"{a}{w}{o}{st}>{s}</text>')


def rect(x, y, w, h, fill, opacity=None, stroke=None, sw=None, rx=None):
    o = f' opacity="{opacity}"' if opacity else ""
    s = f' stroke="{stroke}" stroke-width="{sw}" ' if stroke else ""
    r = f' rx="{rx}"' if rx else ""
    f = f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{s}{r}/>'


def line(x1, y1, x2, y2, stroke, swd=2, opacity=None):
    o = f' opacity="{opacity}"' if opacity else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{stroke}" stroke-width="{swd}"{o}/>'


def svg(body):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'
            '<defs>'
            f'<linearGradient id="olive" x1="0" y1="0" x2="0" y2="1">'
            f'<stop offset="0" stop-color="{OLIVE_TOP}"/><stop offset="1" stop-color="{OLIVE_BOT}"/></linearGradient>'
            '<linearGradient id="paperg" x1="0" y1="0" x2="0" y2="1">'
            f'<stop offset="0" stop-color="{PAPER}"/><stop offset="1" stop-color="{PAPER_D}"/></linearGradient>'
            '<radialGradient id="sodium" cx="0.5" cy="0.5" r="0.5">'
            '<stop offset="0" stop-color="#C8861E" stop-opacity="0.5"/>'
            '<stop offset="1" stop-color="#C8861E" stop-opacity="0"/></radialGradient>'
            '</defs>' + body + "</svg>")


def olive_bg(scan=True):
    b = rect(0, 0, W, H, "url(#olive)")
    if scan:
        b += '<g opacity="0.08">' + "".join(line(0, y, W, y, "#000", 2) for y in range(60, H, 60)) + "</g>"
    return b


def office_silhouette():
    """Low-contrast office backdrop band for the menu (PM: 背景不能空)."""
    g = '<g opacity="0.45">'
    g += rect(1050, 330, 870, 690, "#1C241C")                       # back wall block
    g += rect(1050, 860, 870, 12, "#161D16")                        # floor line
    g += rect(1120, 600, 260, 260, "#222C24")                       # desk block
    g += rect(1150, 480, 160, 120, "#283430")                       # CRT
    g += rect(1162, 492, 136, 84, "#1A241E")                        # CRT glass
    g += rect(1180, 520, 100, 6, CRT_DIM)                           # glow line
    g += rect(1430, 560, 90, 300, "#202A22")                        # shelf
    g += "".join(rect(1438, 580 + i * 70, 74, 8, "#161D16") for i in range(4))
    g += rect(1600, 470, 300, 390, "#242E26")                       # roll door
    g += "".join(line(1600, 500 + i * 44, 1900, 500 + i * 44, "#1A221C", 6) for i in range(8))
    g += rect(1330, 760, 8, 100, "#2A1F14")                         # mop stick
    g += '<ellipse cx="1240" cy="880" rx="220" ry="26" fill="#10160F"/>'
    g += "</g>"
    return g


def menu_rows(sel=0):
    rows = [("继续营业", "继续上一份账本"), ("新事务所", "开一间新的事务所"),
            ("加入事务所", "输码加入队友的局"), ("设置", "名字 / 语言 / 音量 / 灵敏度"),
            ("关机", "离岗，关掉这台机器")]
    g = ""
    y = 468
    for i, (t, d) in enumerate(rows):
        if i == sel:
            g += text(120, y, "▸", 34, AMBER, weight="bold")
            g += text(164, y, t, 40, AMBER, spacing=4, weight="bold")
            g += text(660, y - 6, d, 20, MUTED)
        else:
            g += text(164, y, t, 36, ROW, spacing=4)
        g += line(120, y + 28, 640, y + 28, DIV, 2)
        y += 98
    return g


def menu_chrome(title=True):
    g = ""
    if title:
        g += text(120, 240, "黑色委托", 96, BONE, spacing=10, weight="bold")
        g += rect(124, 268, 430, 6, AMBER_D)
        g += text(124, 318, "BLACK COMMISSION · 外包黑色委托事务所", 24, "#8B9484", font=MONO, spacing=8)
    g += text(120, 1016, "ver 0.1", 18, "#6E7A66", font=MONO)
    g += text(220, 1016, "局域网直连…", 18, "#6E7A66")
    g += text(1800, 1016, "本季度欠款：1,200G", 18, "#9A5A4A", anchor="end")
    return g


def veil():
    return rect(0, 0, W, H, "#000", opacity="0.55")


def stamp(x, y, t, size=34, w=150, h=64, rot=-8, op=0.8):
    return (f'<g transform="translate({x},{y}) rotate({rot})" opacity="{op}">'
            + rect(0, 0, w, h, None, stroke=RED, sw=5)
            + text(w / 2, h / 2 + size * 0.36, t, size, RED, font=KAI, anchor="middle") + "</g>")


def paper_card(x, y, w, h, header, formno=""):
    g = rect(x + 14, y + 16, w, h, "#000", opacity="0.45")
    g += rect(x, y, w, h, "url(#paperg)")
    g += rect(x, y, w, 64, TEAL)
    g += text(x + 28, y + 43, header, 30, "#D6CCAE", font=KAI, spacing=12, weight="bold")
    if formno:
        g += text(x + w - 24, y + 41, formno, 18, "#9DB5AE", font=MONO, anchor="end")
    return g


def crt_chrome(tab=1):
    g = rect(0, 0, W, H, CRT_BG)
    g += '<g opacity="0.12">' + "".join(line(0, y, W, y, "#000", 3) for y in range(0, H, 6))[:0] + "</g>"
    g += '<g opacity="0.10">' + "".join(line(0, y, W, y, "#46FF46", 1) for y in range(8, H, 16)) + "</g>"
    # status bar
    g += rect(0, 0, W, 56, "#0F1D10")
    g += text(40, 38, "BC-DOS v2.2 · 黑色委托办公系统", 22, CRT, font=MONO, spacing=2)
    g += text(1180, 38, "资金 −180G", 22, CRT, font=MONO)
    g += text(1420, 38, "许可证 一级（临时）", 22, CRT_DIM, font=MONO)
    g += text(1860, 38, "14:32", 22, CRT_DIM, font=MONO, anchor="end")
    # tabs
    tabs = ["[1] 委托文件", "[2] 采购目录", "[3] 公司账本"]
    x = 40
    for i, t in enumerate(tabs, 1):
        wt = 240
        if i == tab:
            g += rect(x, 80, wt, 52, CRT)
            g += text(x + 20, 116, t, 26, CRT_BG, font=MONO, weight="bold")
        else:
            g += rect(x, 80, wt, 52, None, stroke=CRT_DIM, sw=2)
            g += text(x + 20, 116, t, 26, CRT_DIM, font=MONO)
        x += wt + 16
    g += line(40, 150, 1880, 150, CRT_DIM, 2)
    g += text(40, 1048, "[1][2][3] 切换页签　·　↑↓ 选择　·　[Enter] 确认　·　[Esc] 离开终端", 20, CRT_DIM, font=MONO)
    return g


def hold_bar(x, y, w, pct, fg, bg="#262E26"):
    return rect(x, y, w, 10, bg) + rect(x, y, int(w * pct), 10, fg)


# ───────────────────────── screens ─────────────────────────
S = {}

# 01 main menu (with background treatment)
S["01_main_menu"] = olive_bg() + office_silhouette() + menu_chrome() + menu_rows(0)

# 02 join card 调岗申请单
b = olive_bg(False) + office_silhouette() + menu_chrome() + menu_rows(2) + veil()
b += paper_card(660, 280, 600, 480, "调 岗 申 请 单", "表 BC-03")
b += text(700, 410, "向房主索取 6 位房间号：", 22, INK_SOFT)
for i in range(6):
    filled = i < 3
    b += rect(700 + i * 88, 440, 72, 88, "#EFE6C8" if filled else "#DDD2B0", stroke="#8A7F63", sw=3)
    if filled:
        b += text(736 + i * 88, 502, "K7F"[i], 48, INK, font=MONO, anchor="middle", weight="bold")
b += text(700, 600, "申请人（你）：老王", 20, INK_SOFT)
b += line(700, 660, 1220, 660, "#8A7F63", 2)
b += text(700, 706, "[Enter] 提交申请", 26, INK, weight="bold")
b += text(1220, 706, "[Esc] 撤回", 22, INK_SOFT, anchor="end")
S["02_join_card"] = b

# 03 lobby 派工名单
b = olive_bg(False) + office_silhouette() + menu_chrome() + menu_rows(0) + veil()
b += paper_card(560, 180, 800, 700, "派 工 名 单", "")
b += text(1100, 224, "房间码", 18, "#9DB5AE")
b += text(1180, 230, "K7F2Q", 34, AMBER, font=MONO, weight="bold", spacing=4)
rows = [("01", "#3E6E4E", "老王（你）", "负责", True), ("02", "#7A5230", "Agent 2", "", False),
        ("03", "", "（空缺）", "", False), ("04", "", "（空缺）", "", False)]
y = 300
for n, c, name, role, me in rows:
    if name != "（空缺）":
        b += rect(596, y - 30, 10, 44, c)
        b += text(620, y, n, 20, INK_SOFT, font=MONO)
        b += text(664, y, name, 28, INK, weight="bold")
        if role:
            b += rect(880, y - 26, 70, 34, TEAL)
            b += text(915, y - 2, role, 20, "#D6CCAE", anchor="middle")
        if me:
            b += text(1020, y, "‹", 30, INK, font=MONO)
            b += rect(1050, y - 24, 34, 30, "#3E6E4E")
            b += text(1100, y, "›", 30, INK, font=MONO)
            b += text(1140, y - 2, "更换工装…", 20, INK_SOFT)
        else:
            b += text(1060, y - 2, "[静音]", 20, INK_SOFT)
            b += text(1150, y - 2, "[按住除名]", 20, "#8A5A50")
    else:
        b += text(620, y, n, 20, "#9A8F73", font=MONO)
        b += text(664, y, "（空缺）", 24, "#9A8F73")
        b += line(800, y - 8, 1280, y - 8, "#A2967A", 2)
    b += line(596, y + 26, 1324, y + 26, "#A2967A", 1.5)
    y += 96
b += text(680, 760, "[ Enter ] 确认到岗，进入办公室", 30, INK, weight="bold")
b += text(700, 802, "报到先后不限——办公室就是集合点", 18, INK_SOFT)
b += stamp(1170, 740, "成立", 30, 120, 56)
S["03_lobby_roster"] = b

# 04 settings 偏好登记表
b = olive_bg(False) + office_silhouette() + menu_chrome() + menu_rows(3) + veil()
b += paper_card(610, 180, 700, 660, "偏 好 登 记 表", "表 BC-05")
items = [("调查员代号", None, "老王"), ("界面语言", None, None), ("主音量", 0.7, None), ("水平灵敏度", 0.45, None)]
y = 320
for label, frac, val in items:
    b += text(660, y, label, 24, INK, weight="bold")
    if val:
        b += rect(900, y - 32, 340, 46, "#EFE6C8", stroke="#8A7F63", sw=2)
        b += text(920, y, val, 24, INK)
    elif frac is None:
        b += rect(900, y - 32, 150, 46, TEAL)
        b += text(975, y - 2, "English", 20, "#D6CCAE", anchor="middle")
        b += rect(1070, y - 32, 170, 46, "#DDD2B0", stroke="#8A7F63", sw=2)
        b += text(1155, y - 2, "中文 (简体)", 20, INK_SOFT, anchor="middle")
    else:
        b += rect(900, y - 18, 340, 14, "#DDD2B0", stroke="#8A7F63", sw=1)
        b += rect(900, y - 18, int(340 * frac), 14, TEAL)
    b += line(660, y + 28, 1260, y + 28, "#A2967A", 1.5)
    y += 102
b += text(660, 770, "[Esc] 收起并备案", 22, INK_SOFT)
b += stamp(1100, 718, "备案", 30, 120, 56)
S["04_settings_card"] = b

# 05 quit 离岗单
b = olive_bg(False) + office_silhouette() + menu_chrome() + menu_rows(4) + veil()
b += paper_card(660, 320, 600, 400, "离 岗 单", "表 BC-09")
b += text(700, 450, "确认离岗并关闭终端？", 30, INK, weight="bold")
b += text(700, 500, "未结算的款项将保留至下次开机。", 20, INK_SOFT)
b += text(700, 532, "离岗期间欠款利息照常累计。", 20, INK_SOFT)
b += line(700, 580, 1220, 580, "#8A7F63", 2)
b += text(700, 640, "[Enter] 确认离岗", 26, RED, weight="bold")
b += text(1220, 640, "[Esc] 留下加班", 22, INK_SOFT, anchor="end")
b += stamp(1040, 600, "离岗", 30, 120, 56)
S["05_quit_card"] = b

# 06 terminal jobs
b = crt_chrome(1)
b += text(60, 210, "可接委托", 24, CRT_DIM, font=MONO)
b += rect(40, 230, 880, 96, CRT)
b += text(70, 270, "「真实海岸」生态柱回收", 30, CRT_BG, font=MONO, weight="bold")
b += text(70, 308, "城东·私人收藏会 · 地球海岸壹号 烂尾楼", 20, "#0E2E14", font=MONO)
b += text(880, 290, "300G", 34, CRT_BG, font=MONO, anchor="end", weight="bold")
b += text(60, 390, "已归档", 24, CRT_DIM, font=MONO)
for i, (no, t, pct, gold) in enumerate([("BC-2098-0006", "「真实海岸」生态柱回收", "91%", "+273G"),
                                        ("BC-2098-0005", "试运营·器材搬运", "100%", "+120G")]):
    y = 430 + i * 56
    b += text(70, y, no, 20, CRT_DIM, font=MONO)
    b += text(280, y, t, 20, CRT_DIM, font=MONO)
    b += text(700, y, pct, 20, CRT_DIM, font=MONO)
    b += text(860, y, gold, 20, CRT_DIM, font=MONO, anchor="end")
# detail pane
b += rect(960, 200, 920, 700, None, stroke=CRT_DIM, sw=2)
b += text(1000, 260, "委托详情", 24, CRT, font=MONO, weight="bold")
b += text(1000, 320, "目标：回收封存的「真实海岸」生态柱（重物，双手）", 22, CRT, font=MONO)
b += text(1000, 360, "地点：地球海岸壹号 · 烂尾预售楼（两层）", 22, CRT, font=MONO)
b += text(1000, 400, "报酬：300G × 密封完整度", 22, CRT, font=MONO)
b += text(1000, 460, "条款 C-7：搬运磕碰造成的密封损失按 3%/次 扣减。", 20, CRT_DIM, font=MONO)
b += text(1000, 492, "条款 D-1：完整度低于 50% 拒收。", 20, CRT_DIM, font=MONO)
b += rect(1000, 800, 400, 60, CRT)
b += text(1200, 840, "[Enter] 接单盖章", 26, CRT_BG, font=MONO, anchor="middle", weight="bold")
S["06_terminal_jobs"] = b

# 07 terminal shop
b = crt_chrome(2)
items = [("手电筒", "30G", "巡夜基本款，吃 1 号电池", True),
         ("电池（2 节）", "10G", "手电续命；上车前检查余量", True),
         ("腕表", "60G", "缺货 — 进货渠道审批中", False)]
y = 240
for name, price, desc, ok in items:
    if name == "手电筒":
        b += rect(40, y - 40, 880, 88, CRT)
        b += text(70, y, name, 28, CRT_BG, font=MONO, weight="bold")
        b += text(70, y + 32, desc, 18, "#0E2E14", font=MONO)
        b += text(880, y + 6, price, 28, CRT_BG, font=MONO, anchor="end", weight="bold")
    else:
        c = CRT if ok else CRT_DIM
        b += text(70, y, name, 26, c, font=MONO)
        b += text(70, y + 32, desc, 18, CRT_DIM, font=MONO)
        b += text(880, y + 6, price, 26, c, font=MONO, anchor="end")
    y += 130
b += rect(960, 200, 920, 700, None, stroke=CRT_DIM, sw=2)
b += text(1000, 260, "采购单", 24, CRT, font=MONO, weight="bold")
b += text(1000, 320, "手电筒 × 1", 24, CRT, font=MONO)
b += text(1000, 360, "小计：30G　结余：−210G", 22, CRT_DIM, font=MONO)
b += text(1000, 420, "交付：货品送达办公室地面（旧货柜旁）。", 20, CRT_DIM, font=MONO)
b += rect(1000, 800, 400, 60, CRT)
b += text(1200, 840, "[Enter] 确认购入", 26, CRT_BG, font=MONO, anchor="middle", weight="bold")
S["07_terminal_shop"] = b

# 08 terminal ledger
b = crt_chrome(3)
b += text(40, 210, "单号          委托                    完整度   实付", 22, CRT_DIM, font=MONO)
rows = [("BC-2098-0007", "「真实海岸」生态柱回收", " 91%", "+273G", True),
        ("BC-2098-0006", "「真实海岸」生态柱回收", " 64%", "+192G", False),
        ("BC-2098-0005", "试运营·器材搬运", "100%", "+120G", False)]
y = 260
for no, t, pct, gold, sel in rows:
    if sel:
        b += rect(40, y - 32, 1100, 48, CRT)
        col, cd = CRT_BG, "#0E2E14"
    else:
        col, cd = CRT, CRT_DIM
    b += text(60, y, no, 22, col, font=MONO)
    b += text(360, y, t, 22, col, font=MONO)
    b += text(820, y, pct, 22, col, font=MONO)
    b += text(1000, y, gold, 22, col, font=MONO)
    y += 70
b += text(60, y + 10, "客户使用备注（BC-2098-0007）：", 20, CRT_DIM, font=MONO)
b += text(60, y + 48, "“柱体状态良好。已陈列于火星新居客厅，邻居均表示羡慕。”", 22, CRT, font=MONO, style="italic")
b += line(40, 940, 1880, 940, CRT_DIM, 2)
b += text(40, 990, "当前结余：−180G", 30, "#E86A50", font=MONO, weight="bold")
b += text(1880, 990, "本季度应还：1,200G　[逾期]", 24, "#E86A50", font=MONO, anchor="end")
S["08_terminal_ledger"] = b

# 09 HUD in tower
b = rect(0, 0, W, H, "#0D1113")
b += rect(0, 660, W, 420, "#0A0D0F")
b += "".join(line(200 + i * 320, 200, 140 + i * 320, 1080, "#15191C", 14) for i in range(6))
b += f'<ellipse cx="1500" cy="540" rx="420" ry="300" fill="url(#sodium)"/>'
b += rect(1430, 300, 140, 320, "#1A1410", opacity="0.9")
# zone A 工单行
b += text(48, 80, "目标：取回「真实海岸」生态柱", 26, BONE, weight="bold")
b += text(48, 120, "密封完整度 94%", 24, AMBER)
b += text(48, 160, "供电已恢复", 20, MUTED, opacity="0.7")
# zone B crosshair + hold bar
b += line(952, 540, 968, 540, AMBER, 3)
b += line(960, 532, 960, 548, AMBER, 3)
b += hold_bar(910, 566, 100, 0.6, AMBER)
b += text(960, 600, "按住 E · 恢复供电", 18, MUTED, anchor="middle")
# zone C hotbar + D bars
slots = ["手电", "电池", "", "生态柱", ""]
x0 = 960 - (5 * 92 - 12) // 2
b += rect(x0 - 14, 920, 5 * 92 + 16, 104, "#10140F", opacity="0.85")
for i, s in enumerate(slots):
    x = x0 + i * 92
    sel = i == 3
    b += rect(x, 932, 80, 80, "#1A201A", stroke=AMBER if sel else DIV, sw=3 if sel else 2)
    if s:
        b += text(x + 40, 980, s, 20, BONE if sel else ROW, anchor="middle")
    b += text(x + 8, 950, str(i + 1), 14, MUTED)
    if i == 0:
        b += hold_bar(x + 8, 1000, 64, 0.55, AMBER)
b += hold_bar(x0, 880, 5 * 92 - 12, 0.8, "#7FA083")
b += text(x0 - 24, 890, "HP", 16, MUTED, anchor="end")
b += hold_bar(x0, 902, 5 * 92 - 12, 0.45, "#8B9484")
b += text(x0 - 24, 912, "STA", 16, MUTED, anchor="end")
S["09_hud_tower"] = b

# 10 dispatch card 派车单 (van interior bg)
van = rect(0, 0, W, H, "#13171A") + rect(0, 0, W, 240, "#1A2023") + rect(0, 840, W, 240, "#101417")
van += rect(120, 300, 280, 420, "#1C2326") + rect(1520, 300, 280, 420, "#1C2326")
b = van + rect(560, 60, 800, 64, "#D6CCAE", opacity="0.95")
b += text(600, 102, "全员到齐 4/4", 26, INK, weight="bold")
b += text(1320, 102, "票据条", 18, INK_SOFT, anchor="end")
b += paper_card(560, 220, 800, 660, "派 车 单", "表 BC-07")
b += text(600, 360, "委托：「真实海岸」生态柱回收", 28, INK, weight="bold")
b += text(600, 404, "地点：地球海岸壹号 · 烂尾预售楼", 22, INK_SOFT)
b += line(600, 440, 1320, 440, "#8A7F63", 2)
for i, n in enumerate(["老王（负责）", "Agent 2", "Agent 3", "Agent 4"]):
    b += text(620, 496 + i * 52, f"0{i+1}", 20, INK_SOFT, font=MONO)
    b += text(680, 496 + i * 52, n, 24, INK)
    b += text(1300, 496 + i * 52, "已就位", 20, TEAL, anchor="end")
b += line(600, 730, 1320, 730, "#8A7F63", 2)
b += text(600, 790, "[ 空格 ] 房主落章，签发派车", 28, INK, weight="bold")
b += text(600, 830, "签发后即刻出发，未上车者将被留下", 18, INK_SOFT)
b += stamp(1120, 756, "待发", 30, 120, 56)
S["10_dispatch_card"] = b

# 11 transit strip 在途
b = van
b += rect(460, 60, 1000, 96, "#D6CCAE", opacity="0.95")
b += text(500, 102, "派车去现场 · 地球海岸壹号", 24, INK, weight="bold")
b += text(1420, 102, "票据条", 18, INK_SOFT, anchor="end")
b += rect(500, 118, 920, 16, "#B8AD8E")
b += rect(500, 118, int(920 * 0.62), 16, TEAL)
b += text(500, 196, "里程 4.7 / 7.6 km", 20, "#8B9484")
b += text(1420, 196, "预计抵达：很快", 20, "#8B9484", anchor="end")
b += text(960, 560, "（车厢即加载：在途时间 = 场景加载窗口，永不显示“加载中”）", 22, "#5A6052", anchor="middle")
b += text(960, 1020, "[Tab] 重看结算单（返程时）", 20, "#6E7A66", anchor="middle")
S["11_transit_strip"] = b

# 12 early return 提前收工申请单
b = van + veil()
b += paper_card(560, 200, 800, 640, "提前收工申请单", "表 BC-08")
b += text(600, 340, "目标未取得 —— 本次出勤按空跑折算", 26, INK, weight="bold")
b += line(600, 380, 1320, 380, "#8A7F63", 2)
b += text(600, 436, "条款 B-2 折算预估：", 24, INK)
b += text(1320, 436, "60G", 30, RED, anchor="end", weight="bold")
b += text(600, 478, "（含出勤补贴，扣除油费与调度费）", 18, INK_SOFT)
b += line(600, 520, 1320, 520, "#8A7F63", 2)
b += text(600, 580, "房主签字：按住 [E] 1.2 秒", 26, INK, weight="bold")
b += rect(600, 610, 720, 18, "#DDD2B0", stroke="#8A7F63", sw=2)
b += rect(600, 610, int(720 * 0.4), 18, TEAL)
b += text(600, 680, "[Esc] 撤回申请", 22, INK_SOFT)
b += text(600, 760, "队友视角：票据条附加行「房主正在填写收工申请…」", 18, "#6B6149")
S["12_early_return_card"] = b

# 13 settlement 结算单
b = van + veil()
b += paper_card(520, 120, 880, 800, "委 托 结 算 单", "BC-2098-0007")
b += text(560, 260, "委托：「真实海岸」生态柱回收", 26, INK, weight="bold")
b += text(560, 300, "完整度：91%　·　全员返岗", 20, INK_SOFT)
b += line(560, 336, 1360, 336, "#8A7F63", 2)
acct = [("委托报酬", "300G", INK), ("条款 C-7 · 搬运磕碰扣损 ×3", "−27G", "#8A5A50")]
y = 392
for label, v, c in acct:
    b += text(580, y, label, 24, INK)
    b += text(1340, y, v, 24, c, anchor="end", weight="bold")
    b += line(560, y + 18, 1360, y + 18, "#A2967A", 1.5)
    y += 64
b += text(580, y + 10, "实付", 30, INK, weight="bold")
b += text(1340, y + 10, "273G", 44, INK, anchor="end", weight="bold")
b += line(560, y + 40, 1360, y + 40, "#8A7F63", 3)
b += text(560, y + 110, "客户使用备注：", 20, INK_SOFT)
b += text(560, y + 148, "“柱体状态良好。已陈列于火星新居客厅，邻居均表示羡慕。”", 22, INK_SOFT, style="italic")
b += stamp(1130, y + 190, "已结清", 30, 170, 60)
b += text(560, 880, "[E] 收起　·　[Tab] 在途重看　·　到站自动收起", 20, INK_SOFT)
S["13_settlement_card"] = b


os.makedirs(OUT, exist_ok=True)
for name, body in S.items():
    with open(os.path.join(OUT, name + ".svg"), "w", encoding="utf-8") as f:
        f.write(svg(body))
    print("wrote", name)
