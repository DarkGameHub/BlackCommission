# -*- coding: utf-8 -*-
"""Black Commission UI mockup kit — v2 (LC-referenced, art-bible palette, English).

ONE visual system, three surfaces (per design/ux/hud.md + art-bible.md §4):
  - System chrome (menu / settings): dark concrete panels, film grain + vignette,
    flat boxy buttons, tungsten-amber accent, aged-paper text. (Lethal Company ref.)
  - Office computer terminal: CRT green (#6CFF5F) monospace on near-black + scanlines
    — the art bible's "CRT green = electronics & UI only" rule. (LC terminal ref.)
  - Modals & forms: aged-paper documents, civic-teal header, stamp-red stamps
    — the art bible's "UI pretends to be paperwork" identity.

Palette is LOCKED to art-bible.md §4 / AGENTS.md identity:
  concrete #5E5E5E/#4A4A4A · dead-rubber-black #1A1A17 · military green #55624A
  tungsten amber #FF9820/#FFAB40 (PRIMARY ACCENT) · CRT green #6CFF5F (screens only)
  aged paper #D6CCAE · civic teal #3F5F5C · stamp red #C23A2B (paper/sign only)

First pass is all-English. Output: design/ux/mockups/ui-kit/NN_name.svg + .png
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "ui-kit")
W, H = 1920, 1080

# The UI font is 3270 (the real Lethal Company face), embedded below so the mockups
# render exactly what the game now uses. Latin-only — the kit is all-English.
import base64 as _b64
_TTF = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project", "Art", "UI", "Fonts", "3270-Regular.ttf")
FONT_B64 = _b64.b64encode(open(_TTF, "rb").read()).decode()
SANS = "BC3270"
MONO = "BC3270"

# ── locked palette ──
BLACK   = "#1A1A17"   # dead rubber black (base bg)
CONC    = "#5E5E5E"   # concrete gray
CONC_D  = "#3A3A36"   # concrete shadow
PANEL   = "#21211C"   # panel fill (slightly translucent in use)
PANEL_L = "#2C2C26"
MILGRN  = "#55624A"   # military green
AMBER   = "#FF9820"   # tungsten amber — PRIMARY ACCENT
AMBER_L = "#FFAB40"   # amber text/highlight
AMBER_D = "#9A6418"   # dim amber
PAPER   = "#D6CCAE"   # aged paper (text on dark + doc base)
PAPER_D = "#9A917A"   # dim paper
PAPER_DD= "#6A6456"   # very dim paper
CRT     = "#6CFF5F"   # CRT green — electronics/terminal ONLY
CRT_D   = "#2E7A33"
RED     = "#C23A2B"   # stamp red — paper/sign ONLY
TEAL    = "#3F5F5C"   # civic teal — document headers
INK     = "#26201A"   # ink on paper
INK_D   = "#4A4136"
PAPER_BG= "#CFC4A4"   # document paper fill
PAPER_BG2="#B8AD8E"


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def t(x, y, s, size, fill, font=SANS, anchor="start", sp=None, weight=None, op=None, italic=False):
    a = f' letter-spacing="{sp}"' if sp else ""
    w = f' font-weight="{weight}"' if weight else ""
    o = f' opacity="{op}"' if op else ""
    i = ' font-style="italic"' if italic else ""
    return (f'<text x="{x}" y="{y}" font-family="{font}" font-size="{size}" fill="{fill}"'
            f' text-anchor="{anchor}"{a}{w}{o}{i}>{esc(s)}</text>')


def r(x, y, w, h, fill, op=None, stroke=None, sw=None, rx=None):
    o = f' opacity="{op}"' if op is not None else ""
    s = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    rr = f' rx="{rx}"' if rx else ""
    f = f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{s}{rr}/>'


def ln(x1, y1, x2, y2, stroke, sw=2, op=None):
    o = f' opacity="{op}"' if op is not None else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{stroke}" stroke-width="{sw}"{o}/>'


DEFS = f'''<defs>
  <style>@font-face{{font-family:'BC3270';src:url('data:font/ttf;base64,{FONT_B64}') format('truetype');}}</style>
  <radialGradient id="vig" cx="0.5" cy="0.46" r="0.75">
    <stop offset="0.45" stop-color="#000" stop-opacity="0"/>
    <stop offset="1" stop-color="#000" stop-opacity="0.72"/>
  </radialGradient>
  <radialGradient id="lamp" cx="0.5" cy="0.5" r="0.5">
    <stop offset="0" stop-color="{AMBER}" stop-opacity="0.34"/>
    <stop offset="0.55" stop-color="{AMBER_D}" stop-opacity="0.10"/>
    <stop offset="1" stop-color="{AMBER}" stop-opacity="0"/>
  </radialGradient>
  <radialGradient id="crtglow" cx="0.5" cy="0.5" r="0.6">
    <stop offset="0" stop-color="{CRT}" stop-opacity="0.16"/>
    <stop offset="1" stop-color="{CRT}" stop-opacity="0"/>
  </radialGradient>
  <linearGradient id="paperg" x1="0" y1="0" x2="0" y2="1">
    <stop offset="0" stop-color="{PAPER_BG}"/><stop offset="1" stop-color="{PAPER_BG2}"/>
  </linearGradient>
  <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="2" stitchTiles="stitch"/>
    <feColorMatrix type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  0 0 0 0.5 0"/></filter>
</defs>'''


def grain(op=0.05):
    return f'<rect x="0" y="0" width="{W}" height="{H}" filter="url(#grain)" opacity="{op}"/>'


def vignette():
    return r(0, 0, W, H, "url(#vig)")


def svg(body):
    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'
            + DEFS + body + "</svg>")


# ── shared dark-office backdrop (menu + paper-modal screens) ──
def office_bg():
    g = r(0, 0, W, H, BLACK)
    # faint concrete wall blocks on the right, lit slightly
    g += r(1060, 0, 860, H, "#1E1E1B")
    g += ln(1060, 0, 1060, H, "#000", 3, op=0.5)
    # amber desk-lamp pool, lower right — the one warm source
    g += f'<ellipse cx="1520" cy="720" rx="520" ry="430" fill="url(#lamp)"/>'
    # desk
    g += r(1300, 760, 520, 220, "#241F18")
    g += ln(1300, 760, 1820, 760, AMBER_D, 2, op=0.4)
    # CRT monitor on the desk, faint green glow
    g += f'<ellipse cx="1430" cy="660" rx="160" ry="130" fill="url(#crtglow)"/>'
    g += r(1360, 600, 150, 120, "#15170F")
    g += r(1372, 612, 126, 84, "#0E140C")
    g += ln(1384, 648, 1486, 648, CRT_D, 3, op=0.7)
    g += ln(1384, 664, 1460, 664, CRT_D, 3, op=0.5)
    # filing cabinet, lit edge
    g += r(1650, 560, 130, 420, "#201E18")
    g += ln(1650, 560, 1780, 560, AMBER_D, 2, op=0.3)
    for i in range(3):
        g += ln(1650, 660 + i * 110, 1780, 660 + i * 110, "#15140F", 4)
    # hanging debt notice (paper + red stamp) on the wall
    g += f'<g transform="translate(1140,300) rotate(-2)">'
    g += r(0, 0, 150, 196, PAPER, op=0.32)
    g += ln(16, 40, 134, 40, INK_D, 2, op=0.3)
    g += ln(16, 64, 134, 64, INK_D, 2, op=0.25)
    g += ln(16, 88, 110, 88, INK_D, 2, op=0.25)
    g += f'<g transform="translate(40,120) rotate(-10)">' + r(0, 0, 84, 44, None, stroke=RED, sw=4, op=0.4)
    g += t(42, 30, "OVERDUE", 13, RED, anchor="middle", op=0.4, weight="bold") + "</g></g>"
    # mop / pole silhouette to break symmetry
    g += r(1250, 600, 7, 380, "#1A1610")
    g += vignette()
    g += grain(0.055)
    return g


def logo(x=120, y=170):
    g = t(x, y, "BLACK COMMISSION", 68, PAPER, sp=6, weight="bold")
    g += r(x + 4, y + 26, 540, 5, AMBER)
    g += t(x + 6, y + 70, "OUTSOURCED COMMISSION OFFICE", 21, PAPER_D, sp=10)
    # overdue stamp by the logo
    g += f'<g transform="translate({x+560},{y-70}) rotate(-9)" opacity="0.85">'
    g += r(0, 0, 150, 70, None, stroke=RED, sw=5)
    g += t(75, 30, "MUNICIPAL", 16, RED, anchor="middle", weight="bold")
    g += t(75, 54, "OVERDUE", 20, RED, anchor="middle", weight="bold") + "</g>"
    return g


def footer():
    g = t(120, 1028, "ver 0.1", 18, PAPER_DD, font=MONO)
    g += t(232, 1028, "LAN DIRECT…", 18, PAPER_DD, sp=2)
    g += t(1800, 1028, "QUARTERLY DEBT: 1,200G  ·  OVERDUE", 18, RED, anchor="end", op=0.85)
    return g


# ── flat boxy menu button (LC reference) ──
def menu_btn(x, y, w, h, label, sub, selected=False, disabled=False):
    if disabled:
        fill, bd, tx, sx = "#1C1C18", "#2C2C26", PAPER_DD, PAPER_DD
    elif selected:
        fill, bd, tx, sx = "#2A2418", AMBER, AMBER_L, "#C9A86A"
    else:
        fill, bd, tx, sx = "#1F1F1B", "#39392F", PAPER, PAPER_D
    g = r(x, y, w, h, fill, stroke=bd, sw=2)
    if selected:
        g += r(x, y, 6, h, AMBER)               # amber spine
        g += t(x + 26, y + h * 0.46 + 8, "▸", 26, AMBER)
    g += t(x + 64, y + 44, label, 32, tx, sp=3, weight="bold")
    g += t(x + 64, y + 74, sub, 17, sx, sp=1)
    return g


def veil():
    return r(0, 0, W, H, "#000", op=0.6)


def stamp(x, y, label, w=160, h=64, rot=-8, size=26):
    return (f'<g transform="translate({x},{y}) rotate({rot})" opacity="0.82">'
            + r(0, 0, w, h, None, stroke=RED, sw=5)
            + t(w / 2, h / 2 + size * 0.36, label, size, RED, anchor="middle", weight="bold")
            + "</g>")


def paper_card(x, y, w, h, header, formno=""):
    g = r(x + 14, y + 16, w, h, "#000", op=0.5)
    g += r(x, y, w, h, "url(#paperg)")
    g += r(x, y, w, 60, TEAL)
    g += t(x + 28, y + 40, header, 26, PAPER, sp=8, weight="bold")
    if formno:
        g += t(x + w - 24, y + 39, formno, 17, "#A9C0B8", font=MONO, anchor="end")
    return g


# ── CRT terminal chrome (LC terminal reference) ──
def crt_scanlines():
    return ('<g opacity="0.10">'
            + "".join(ln(0, yy, W, yy, "#9CFF9C", 1) for yy in range(8, H, 5)) + "</g>")


def crt_chrome(tab=1):
    g = r(0, 0, W, H, "#070D08")
    g += f'<rect x="0" y="0" width="{W}" height="{H}" fill="url(#crtglow)" opacity="0.5"/>'
    g += r(0, 0, W, 54, "#0C160C")
    g += t(40, 36, "BC-DOS v2.2  ·  COMMISSION OFFICE TERMINAL", 22, CRT, font=MONO, sp=1)
    g += t(1300, 36, "FUNDS -180G", 22, CRT, font=MONO)
    g += t(1560, 36, "LICENSE: TIER 1 (PROVISIONAL)", 20, CRT_D, font=MONO)
    g += t(1880, 36, "14:32", 22, CRT_D, font=MONO, anchor="end")
    tabs = ["[1] COMMISSIONS", "[2] SUPPLY", "[3] LEDGER"]
    x = 40
    for i, lbl in enumerate(tabs, 1):
        wt = 300
        if i == tab:
            g += r(x, 78, wt, 50, CRT)
            g += t(x + 22, 112, lbl, 24, "#07120A", font=MONO, weight="bold")
        else:
            g += r(x, 78, wt, 50, None, stroke=CRT_D, sw=2)
            g += t(x + 22, 112, lbl, 24, CRT_D, font=MONO)
        x += wt + 16
    g += ln(40, 146, 1880, 146, CRT_D, 2)
    g += t(40, 1046, "[1][2][3] SWITCH TAB   ·   ↑↓ SELECT   ·   [ENTER] CONFIRM   ·   [ESC] LOG OFF",
           20, CRT_D, font=MONO)
    g += crt_scanlines()
    return g


def bar(x, y, w, pct, fg, bg="#23231D", h=12):
    return r(x, y, w, h, bg) + r(x, y, int(w * pct), h, fg)


# ───────────────────── screens ─────────────────────
S = {}

# 01 — main menu (style anchor)
b = office_bg() + logo()
rows = [("CONTINUE SHIFT", "Resume the last ledger", "sel"),
        ("NEW OFFICE", "Open a fresh commission office", ""),
        ("JOIN OFFICE", "Enter a teammate's room code", ""),
        ("SETTINGS", "Name · language · volume · sensitivity", ""),
        ("SHUT DOWN", "Clock out and power down", "")]
y = 360
for lbl, sub, st in rows:
    b += menu_btn(120, y, 560, 84, lbl, sub, selected=(st == "sel"))
    y += 100
b += footer()
S["01_main_menu"] = b

# 02 — join card (modal over menu)
b = office_bg() + logo()
yy = 360
for lbl, sub, st in rows:
    b += menu_btn(120, yy, 560, 84, lbl, sub, selected=(lbl == "JOIN OFFICE"))
    yy += 100
b += footer() + veil()
b += paper_card(660, 300, 600, 460, "TRANSFER REQUEST", "FORM BC-03")
b += t(700, 426, "Enter the host's 6-digit room code:", 21, INK_D)
for i in range(6):
    on = i < 3
    b += r(700 + i * 88, 456, 72, 88, "#EFE6C8" if on else "#DCD1AF", stroke="#8A7F63", sw=3)
    if on:
        b += t(736 + i * 88, 518, "K7F"[i], 46, INK, font=MONO, anchor="middle", weight="bold")
b += ln(700, 632, 1220, 632, "#8A7F63", 2)
b += t(700, 690, "[ENTER]  SUBMIT", 24, INK, weight="bold")
b += t(1220, 690, "[ESC] BACK", 20, INK_D, anchor="end")
S["02_join_card"] = b

# 03 — lobby roster
b = office_bg() + logo()
yy = 360
for lbl, sub, st in rows:
    b += menu_btn(120, yy, 560, 84, lbl, sub, selected=(st == "sel"))
    yy += 100
b += footer() + veil()
b += paper_card(640, 170, 760, 720, "DISPATCH ROSTER", "FORM BC-02")
b += t(1160, 220, "ROOM CODE", 17, "#A9C0B8")
b += t(1232, 226, "K7F2Q", 32, AMBER_D, font=MONO, weight="bold", sp=4)
crew = [("01", MILGRN, "Wang (you)", "LEAD", True),
        ("02", "#7C624A", "Agent 2", "", False),
        ("03", "", "(empty)", "", False),
        ("04", "", "(empty)", "", False)]
y = 310
for n, c, name, role, me in crew:
    if name != "(empty)":
        b += r(676, y - 30, 10, 46, c)
        b += t(700, y, n, 20, INK_D, font=MONO)
        b += t(744, y, name, 27, INK, weight="bold")
        if role:
            b += r(960, y - 26, 74, 34, TEAL)
            b += t(997, y - 2, role, 18, PAPER, anchor="middle", weight="bold")
        if me:
            b += t(1110, y, "‹", 28, INK, font=MONO)
            b += r(1136, y - 24, 36, 30, MILGRN)
            b += t(1192, y, "›", 28, INK, font=MONO)
            b += t(1216, y - 2, "CHANGE GEAR…", 17, INK_D)
        else:
            b += t(1130, y - 2, "[MUTE]", 18, INK_D)
            b += t(1228, y - 2, "[HOLD: KICK]", 18, "#9A4A40")
    else:
        b += t(700, y, n, 20, "#9A8F73", font=MONO)
        b += t(744, y, "(empty)", 24, "#9A8F73")
    b += ln(676, y + 26, 1364, y + 26, "#A2967A", 1.5)
    y += 96
b += t(720, 790, "[ ENTER ]  REPORT IN — ENTER THE OFFICE", 26, INK, weight="bold")
b += t(740, 826, "Report in any order — the office is the muster point.", 17, INK_D)
b += stamp(1180, 770, "FILED", 120, 54, -8, 24)
S["03_lobby_roster"] = b

# 04 — settings card
b = office_bg() + logo()
yy = 360
for lbl, sub, st in rows:
    b += menu_btn(120, yy, 560, 84, lbl, sub, selected=(lbl == "SETTINGS"))
    yy += 100
b += footer() + veil()
b += paper_card(610, 180, 700, 680, "PREFERENCE RECORD", "FORM BC-05")
y = 320
b += t(660, y, "Agent name", 23, INK, weight="bold")
b += r(960, y - 32, 300, 46, "#EFE6C8", stroke="#8A7F63", sw=2)
b += t(980, y, "Wang", 23, INK)
y += 100
b += t(660, y, "Language", 23, INK, weight="bold")
b += r(960, y - 32, 140, 46, TEAL)
b += t(1030, y - 2, "English", 19, PAPER, anchor="middle")
b += r(1116, y - 32, 144, 46, "#DCD1AF", stroke="#8A7F63", sw=2)
b += t(1188, y - 2, "中文", 19, INK_D, anchor="middle")
y += 100
b += t(660, y, "Master volume", 23, INK, weight="bold")
b += r(960, y - 16, 300, 14, "#DCD1AF", stroke="#8A7F63", sw=1)
b += r(960, y - 16, 210, 14, TEAL)
y += 90
b += t(660, y, "Look sensitivity", 23, INK, weight="bold")
b += r(960, y - 16, 300, 14, "#DCD1AF", stroke="#8A7F63", sw=1)
b += r(960, y - 16, 135, 14, TEAL)
y += 110
b += t(660, y, "[ESC]  SAVE & CLOSE", 22, INK, weight="bold")
b += stamp(1110, y - 50, "ON FILE", 130, 54, -8, 22)
S["04_settings_card"] = b

# 05 — quit card
b = office_bg() + logo()
yy = 360
for lbl, sub, st in rows:
    b += menu_btn(120, yy, 560, 84, lbl, sub, selected=(lbl == "SHUT DOWN"))
    yy += 100
b += footer() + veil()
b += paper_card(660, 320, 600, 400, "CLOCK-OUT NOTICE", "FORM BC-09")
b += t(700, 452, "Confirm clock-out and power down?", 27, INK, weight="bold")
b += t(700, 502, "Unsettled funds are held until next boot.", 19, INK_D)
b += t(700, 534, "Debt interest accrues while you are away.", 19, INK_D)
b += ln(700, 582, 1220, 582, "#8A7F63", 2)
b += t(700, 642, "[ENTER]  CLOCK OUT", 24, RED, weight="bold")
b += t(1220, 642, "[ESC] STAY", 20, INK_D, anchor="end")
b += stamp(1030, 600, "OFF DUTY", 150, 56, -8, 22)
S["05_quit_card"] = b

# 06 — terminal: commissions
b = crt_chrome(1)
b += t(60, 206, "AVAILABLE COMMISSIONS", 22, CRT_D, font=MONO)
b += r(40, 228, 880, 96, CRT)
b += t(70, 268, "\"REAL COAST\" ECO-COLUMN RECOVERY", 28, "#07120A", font=MONO, weight="bold")
b += t(70, 304, "Pvt. Collector · Earth Coast 01, derelict tower", 19, "#0E3013", font=MONO)
b += t(884, 288, "300G", 34, "#07120A", font=MONO, anchor="end", weight="bold")
b += t(60, 386, "ARCHIVED", 22, CRT_D, font=MONO)
arch = [("BC-2098-0006", "\"REAL COAST\" ECO-COLUMN", " 91%", "+273G"),
        ("BC-2098-0005", "TRIAL RUN · GEAR HAUL", "100%", "+120G")]
for i, (no, ti, pc, gd) in enumerate(arch):
    yy = 428 + i * 50
    b += t(70, yy, no, 19, CRT_D, font=MONO)
    b += t(330, yy, ti, 19, CRT_D, font=MONO)
    b += t(720, yy, pc, 19, CRT_D, font=MONO)
    b += t(884, yy, gd, 19, CRT_D, font=MONO, anchor="end")
# detail pane
b += r(960, 200, 920, 720, None, stroke=CRT_D, sw=2)
b += t(1000, 256, "COMMISSION DETAIL", 24, CRT, font=MONO, weight="bold")
det = ["TARGET:  recover the sealed \"Real Coast\" eco-column",
       "         (heavy object, two-handed carry)",
       "SITE:    Earth Coast 01 · derelict pre-sale tower (2F)",
       "PAY:     300G  ×  seal completeness"]
for i, d in enumerate(det):
    b += t(1000, 312 + i * 38, d, 21, CRT, font=MONO)
b += t(1000, 500, "C-7  Carry impacts deduct seal at 3% each.", 19, CRT_D, font=MONO)
b += t(1000, 532, "D-1  Below 50% completeness is rejected.", 19, CRT_D, font=MONO)
b += r(1000, 820, 420, 60, CRT)
b += t(1210, 860, "[ENTER]  ACCEPT & STAMP", 24, "#07120A", font=MONO, anchor="middle", weight="bold")
b += crt_scanlines()
S["06_terminal_jobs"] = b

# 07 — terminal: supply
b = crt_chrome(2)
items = [("FLASHLIGHT", "30G", "Night-shift basic. Runs on 1 battery.", True, True),
         ("BATTERIES (x2)", "10G", "Flashlight life. Check charge before boarding.", True, False),
         ("WRISTWATCH", "60G", "OUT OF STOCK — supplier approval pending.", False, False)]
y = 230
for name, price, desc, ok, sel in items:
    if sel:
        b += r(40, y - 42, 880, 92, CRT)
        b += t(70, y, name, 28, "#07120A", font=MONO, weight="bold")
        b += t(70, y + 32, desc, 18, "#0E3013", font=MONO)
        b += t(884, y + 4, price, 28, "#07120A", font=MONO, anchor="end", weight="bold")
    else:
        c = CRT if ok else CRT_D
        b += t(70, y, name, 26, c, font=MONO)
        b += t(70, y + 32, desc, 18, CRT_D, font=MONO)
        b += t(884, y + 4, price, 26, c, font=MONO, anchor="end")
    y += 132
b += r(960, 200, 920, 720, None, stroke=CRT_D, sw=2)
b += t(1000, 256, "PURCHASE ORDER", 24, CRT, font=MONO, weight="bold")
b += t(1000, 320, "Flashlight  x1", 24, CRT, font=MONO)
b += t(1000, 362, "Subtotal 30G   ·   Balance -210G", 21, CRT_D, font=MONO)
b += t(1000, 430, "Delivery: goods drop to the office floor", 20, CRT_D, font=MONO)
b += t(1000, 462, "(beside the old storage locker).", 20, CRT_D, font=MONO)
b += r(1000, 820, 420, 60, CRT)
b += t(1210, 860, "[ENTER]  CONFIRM BUY", 24, "#07120A", font=MONO, anchor="middle", weight="bold")
b += crt_scanlines()
S["07_terminal_shop"] = b

# 08 — terminal: ledger
b = crt_chrome(3)
b += t(40, 206, "NO.            COMMISSION                      COMPL.   PAID", 20, CRT_D, font=MONO)
led = [("BC-2098-0007", "\"REAL COAST\" ECO-COLUMN", " 91%", "+273G", True),
       ("BC-2098-0006", "\"REAL COAST\" ECO-COLUMN", " 64%", "+192G", False),
       ("BC-2098-0005", "TRIAL RUN · GEAR HAUL", "100%", "+120G", False)]
y = 256
for no, ti, pc, gd, sel in led:
    if sel:
        b += r(40, y - 30, 1200, 46, CRT)
        col = "#07120A"
    else:
        col = CRT
    b += t(60, y, no, 21, col, font=MONO)
    b += t(360, y, ti, 21, col, font=MONO)
    b += t(880, y, pc, 21, col, font=MONO)
    b += t(1060, y, gd, 21, col, font=MONO)
    y += 66
b += t(60, y + 14, "CLIENT USAGE NOTE  (BC-2098-0007):", 20, CRT_D, font=MONO)
b += t(60, y + 52, "\"Column in good condition. Now displayed in our Mars", 21, CRT, font=MONO, italic=True)
b += t(60, y + 84, " living room. The neighbors are envious.\"", 21, CRT, font=MONO, italic=True)
b += ln(40, 944, 1880, 944, CRT_D, 2)
b += t(40, 992, "CURRENT BALANCE: -180G", 28, CRT, font=MONO, weight="bold")
b += t(1880, 992, "DUE THIS QUARTER: 1,200G  [OVERDUE]", 22, CRT, font=MONO, anchor="end")
b += crt_scanlines()
S["08_terminal_ledger"] = b

# 09 — HUD (tower)
b = r(0, 0, W, H, "#0C0F10")
b += r(0, 640, W, 440, "#0A0C0D")
b += "".join(ln(220 + i * 330, 180, 160 + i * 330, 1080, "#13161A", 16) for i in range(6))
b += f'<ellipse cx="1480" cy="520" rx="440" ry="320" fill="url(#lamp)"/>'
b += r(1430, 300, 130, 320, "#15110C", op=0.85)
# work-order line (top-left)
b += t(48, 80, "OBJECTIVE", 16, AMBER, sp=4, weight="bold")
b += t(48, 116, "Recover the \"Real Coast\" eco-column", 25, PAPER, weight="bold")
b += t(48, 158, "SEAL COMPLETENESS  94%", 22, AMBER_L)
b += t(48, 196, "Power restored", 19, PAPER_DD, op=0.8)
# crosshair + hold bar
b += ln(950, 540, 970, 540, AMBER, 3) + ln(960, 530, 960, 550, AMBER, 3)
b += bar(905, 566, 110, 0.6, AMBER, h=8)
b += t(960, 602, "HOLD  E  ·  RESTORE POWER", 17, PAPER_D, anchor="middle")
# hotbar + status bars
slots = ["LIGHT", "BATT", "", "COLUMN", ""]
x0 = 960 - (5 * 96 - 12) // 2
b += r(x0 - 14, 916, 5 * 96 + 16, 108, "#0E110D", op=0.86)
for i, s in enumerate(slots):
    x = x0 + i * 96
    sel = i == 3
    b += r(x, 928, 84, 84, "#16191400" if not sel else "#1E2014", stroke=AMBER if sel else "#2E332C", sw=3 if sel else 2)
    if s:
        b += t(x + 42, 978, s, 17, PAPER if sel else PAPER_D, anchor="middle", weight="bold" if sel else None)
    b += t(x + 9, 950, str(i + 1), 14, PAPER_DD)
    if i == 0:
        b += bar(x + 9, 994, 66, 0.55, AMBER, h=7)
b += bar(x0, 884, 5 * 96 - 12, 0.8, MILGRN, h=10)
b += t(x0 - 22, 894, "HP", 16, PAPER_D, anchor="end")
b += bar(x0, 900, 5 * 96 - 12, 0.45, "#6E7A66", h=10)
b += t(x0 - 22, 910, "STA", 16, PAPER_D, anchor="end")
S["09_hud_tower"] = b

# ── van interior backdrop (transit screens) ──
def van_bg():
    g = r(0, 0, W, H, "#14130F")
    g += r(0, 0, W, 230, "#1B1A15")
    g += r(0, 850, W, 230, "#0F0E0B")
    g += r(110, 290, 300, 440, "#1E1D17")   # bench L
    g += r(1510, 290, 300, 440, "#1E1D17")   # bench R
    g += ln(0, 230, W, 230, "#000", 3, op=0.5)
    g += f'<ellipse cx="960" cy="150" rx="320" ry="90" fill="url(#lamp)" opacity="0.6"/>'
    g += vignette()
    g += grain(0.05)
    return g


# 10 — dispatch card
b = van_bg()
b += r(560, 70, 800, 60, PAPER, op=0.95)
b += t(600, 110, "ALL ABOARD  4/4", 25, INK, weight="bold")
b += t(1320, 110, "TICKET STRIP", 17, INK_D, anchor="end")
b += paper_card(560, 220, 800, 660, "DISPATCH ORDER", "FORM BC-07")
b += t(600, 356, "\"REAL COAST\" ECO-COLUMN RECOVERY", 25, INK, weight="bold")
b += t(600, 398, "Earth Coast 01 · derelict pre-sale tower", 20, INK_D)
b += ln(600, 432, 1320, 432, "#8A7F63", 2)
for i, n in enumerate(["Wang (lead)", "Agent 2", "Agent 3", "Agent 4"]):
    b += t(620, 490 + i * 52, f"0{i+1}", 19, INK_D, font=MONO)
    b += t(680, 490 + i * 52, n, 23, INK)
    b += t(1300, 490 + i * 52, "READY", 19, TEAL, anchor="end", weight="bold")
b += ln(600, 728, 1320, 728, "#8A7F63", 2)
b += t(600, 788, "[ SPACE ]  HOST STAMPS — DISPATCH", 26, INK, weight="bold")
b += t(600, 828, "Departs on stamp. Stragglers are left behind.", 17, INK_D)
b += stamp(1130, 752, "PENDING", 130, 54, -8, 22)
S["10_dispatch_card"] = b

# 11 — transit strip
b = van_bg()
b += r(460, 60, 1000, 96, PAPER, op=0.95)
b += t(500, 102, "DISPATCHING TO SITE · EARTH COAST 01", 23, INK, weight="bold")
b += t(1420, 102, "TICKET STRIP", 17, INK_D, anchor="end")
b += r(500, 118, 920, 16, PAPER_BG2)
b += r(500, 118, int(920 * 0.62), 16, TEAL)
b += t(500, 196, "MILEAGE 4.7 / 7.6 km", 20, PAPER_D)
b += t(1420, 196, "ARRIVING: SOON", 20, PAPER_D, anchor="end")
b += t(960, 560, "The ride IS the load screen — the scene streams in", 22, PAPER_DD, anchor="middle")
b += t(960, 592, "during transit. \"Loading\" is never shown.", 22, PAPER_DD, anchor="middle")
b += t(960, 1024, "[TAB]  REVIEW SETTLEMENT (on the return leg)", 19, PAPER_DD, anchor="middle")
S["11_transit_strip"] = b

# 12 — early return card
b = van_bg() + veil()
b += paper_card(560, 200, 800, 640, "EARLY CLOCK-OUT REQUEST", "FORM BC-08")
b += t(600, 338, "Objective not secured — paid as an empty run.", 24, INK, weight="bold")
b += ln(600, 376, 1320, 376, "#8A7F63", 2)
b += t(600, 434, "Clause B-2 estimate:", 23, INK)
b += t(1320, 434, "60G", 30, RED, anchor="end", weight="bold")
b += t(600, 474, "(attendance stipend, minus fuel & dispatch fee)", 18, INK_D)
b += ln(600, 516, 1320, 516, "#8A7F63", 2)
b += t(600, 576, "Host signs: HOLD [E] for 1.2s", 25, INK, weight="bold")
b += r(600, 606, 720, 18, PAPER_BG2, stroke="#8A7F63", sw=2)
b += r(600, 606, int(720 * 0.4), 18, TEAL)
b += t(600, 676, "[ESC]  CANCEL REQUEST", 21, INK_D)
b += t(600, 760, "Teammates see: \"Host is filing an early clock-out…\"", 18, "#6B6149")
S["12_early_return_card"] = b

# 13 — settlement card
b = van_bg() + veil()
b += paper_card(520, 120, 880, 800, "COMMISSION SETTLEMENT", "BC-2098-0007")
b += t(560, 244, "\"REAL COAST\" ECO-COLUMN RECOVERY", 25, INK, weight="bold")
b += t(560, 286, "Completeness 91%  ·  Full crew returned", 20, INK_D)
b += ln(560, 322, 1360, 322, "#8A7F63", 2)
acct = [("Commission pay", "300G", INK), ("Clause C-7 · carry impact x3", "-27G", "#9A4A40")]
y = 380
for label, v, c in acct:
    b += t(580, y, label, 24, INK)
    b += t(1340, y, v, 24, c, anchor="end", weight="bold")
    b += ln(560, y + 18, 1360, y + 18, "#A2967A", 1.5)
    y += 62
b += t(580, y + 14, "NET PAID", 30, INK, weight="bold")
b += t(1340, y + 14, "273G", 44, INK, anchor="end", weight="bold")
b += ln(560, y + 42, 1360, y + 42, "#8A7F63", 3)
b += t(560, y + 112, "Client usage note:", 20, INK_D)
b += t(560, y + 150, "\"Column in good condition. Now displayed in our", 22, INK_D, italic=True)
b += t(560, y + 182, " Mars living room. The neighbors are envious.\"", 22, INK_D, italic=True)
b += stamp(1120, y + 210, "SETTLED", 170, 60, -8, 26)
b += t(560, 884, "[E] CLOSE   ·   [TAB] REVIEW IN TRANSIT   ·   AUTO-CLOSES ON ARRIVAL", 19, INK_D)
S["13_settlement_card"] = b


os.makedirs(OUT, exist_ok=True)
for name, body in S.items():
    with open(os.path.join(OUT, name + ".svg"), "w", encoding="utf-8") as f:
        f.write(svg(body))
    print("wrote", name)
