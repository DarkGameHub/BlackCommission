# -*- coding: utf-8 -*-
"""派工名单卡 v3 — three PREMIUM palette directions after PM feedback 2026-07-04
("不好看/不够高级, 参考知名公司的配色"). Same locked layout (lobby.md), three skins:

  A  nier   — NieR:Automata beige-grey: clean warm grey paper, taupe ink, inverted
              chips, generous whitespace. (PlatinumGames' famous premium beige UI.)
  B  noir   — black-card luxury: near-black card, cream type, gold room code,
              hairline dividers. (Amex Centurion / Death Stranding minimalism.)
  C  swiss  — Swiss municipal: bright off-white paper, true black ink, one signal
              red. (Vignelli / Braun grid discipline.)

Typography premium pass baked into all three: Helvetica Neue + PingFang SC (mockup
stand-in; in-game font choice is a follow-up), strict left grid, bigger name type,
small-caps tracked labels, hairline rules.

Output: design/ux/mockups/ui-kit/03_roster_v3_{a_nier,b_noir,c_swiss}.svg → PNG via Chrome.
"""
import os

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "ui-kit")
W, H = 1920, 1080
SANS = "'Helvetica Neue','PingFang SC','Hiragino Sans GB',sans-serif"


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def t(x, y, s, size, fill, anchor="start", weight=None, sp=None, op=None):
    a = f' letter-spacing="{sp}"' if sp else ""
    w = f' font-weight="{weight}"' if weight else ""
    o = f' opacity="{op}"' if op else ""
    return (f'<text x="{x}" y="{y}" font-family="{SANS}" font-size="{size}" fill="{fill}"'
            f' text-anchor="{anchor}"{a}{w}{o}>{esc(s)}</text>')


def r(x, y, w, h, fill, op=None, stroke=None, sw=None):
    o = f' opacity="{op}"' if op is not None else ""
    s = f' stroke="{stroke}" stroke-width="{sw}"' if stroke else ""
    f = f' fill="{fill}"' if fill else ' fill="none"'
    return f'<rect x="{x}" y="{y}" width="{w}" height="{h}"{f}{o}{s}/>'


def ln(x1, y1, x2, y2, stroke, sw=1, op=None, dash=None):
    o = f' opacity="{op}"' if op is not None else ""
    d = f' stroke-dasharray="{dash}"' if dash else ""
    return f'<line x1="{x1}" y1="{y1}" x2="{x2}" y2="{y2}" stroke="{stroke}" stroke-width="{sw}"{o}{d}/>'


def dot(cx, cy, rr, state, color, ink, dim):
    if state == "sel":
        return (f'<circle cx="{cx}" cy="{cy}" r="{rr}" fill="{color}"/>'
                f'<circle cx="{cx}" cy="{cy}" r="{rr + 5}" fill="none" stroke="{ink}" stroke-width="1.5"/>')
    if state == "open":
        return f'<circle cx="{cx}" cy="{cy}" r="{rr}" fill="{color}" opacity="0.55"/>'
    return (f'<circle cx="{cx}" cy="{cy}" r="{rr}" fill="none" stroke="{dim}" '
            f'stroke-width="1.5" stroke-dasharray="3.5 3.5"/>')


PALETTES = {
    "a_nier": dict(
        backdrop="#33312B", card="#D8D3BE", card2=None,
        ink="#4C483C", dim="#938D7A", rule="#B9B29B", hair="#C6C0AA",
        accent="#4C483C",              # inverted taupe chips
        accent_tx="#D8D3BE",
        code="#4C483C", code_chip=True,
        red="#A8442F", vests=["#68724F", "#B9924C", "#8C5937", "#5F6D74"],
        band=None, shadow=0.35,
    ),
    "b_noir": dict(
        backdrop="#08080A", card="#141417", card2="#101013",
        ink="#EAE2CD", dim="#7E7A6C", rule="#2A2A2F", hair="#26262B",
        accent="#C9A45C",              # gold
        accent_tx="#0E0E10",
        code="#C9A45C", code_chip=False,
        red="#C23A2B", vests=["#7E8F6A", "#D2A45C", "#A66844", "#7C8B94"],
        band=None, shadow=0.7,
    ),
    # ── signature-colour test (PM 2026-07-04: "LC 的橙贯穿始终" → 我们的贯穿色是哪根?) ──
    "b_sig_red": dict(   # 黑卷宗 + 印章红做签名色 (章/选中/危险动作全走红, 金退场)
        backdrop="#08080A", card="#141417", card2="#101013",
        ink="#EAE2CD", dim="#7E7A6C", rule="#2A2A2F", hair="#26262B",
        accent="#C23A2B", accent_tx="#F0E8D6",
        code="#C23A2B", code_chip=False,
        red="#C23A2B", vests=["#7E8F6A", "#D2A45C", "#A66844", "#7C8B94"],
        band=None, shadow=0.7,
    ),
    "b_sig_amber": dict(  # 黑卷宗 + 琥珀做签名色 (现 art-bible 主 accent — 与 LC 橙近亲)
        backdrop="#08080A", card="#141417", card2="#101013",
        ink="#EAE2CD", dim="#7E7A6C", rule="#2A2A2F", hair="#26262B",
        accent="#E8912A", accent_tx="#0E0E10",
        code="#E8912A", code_chip=False,
        red="#C23A2B", vests=["#7E8F6A", "#D2A45C", "#A66844", "#7C8B94"],
        band=None, shadow=0.7,
    ),
    "c_swiss": dict(
        backdrop="#26241F", card="#F1ECE1", card2=None,
        ink="#161410", dim="#8F8A7D", rule="#C9C3B4", hair="#DAD4C5",
        accent="#161410",              # black chips
        accent_tx="#F1ECE1",
        code="#161410", code_chip=False,
        red="#C8102E", vests=["#5E6B48", "#C08A2E", "#96522C", "#4F6470"],
        band="double_rule", shadow=0.3,
    ),
}


def build(key):
    p = PALETTES[key]
    b = r(0, 0, W, H, p["backdrop"])
    b += (f'<radialGradient id="vig" cx="0.5" cy="0.45" r="0.8">'
          f'<stop offset="0.5" stop-color="#000" stop-opacity="0"/>'
          f'<stop offset="1" stop-color="#000" stop-opacity="0.5"/></radialGradient>')
    b += r(0, 0, W, H, "url(#vig)")

    cw, ch = 1060, 680
    cx, cy = (W - cw) // 2, (H - ch) // 2
    pad = 56
    gx = cx + pad                      # left content grid
    gr = cx + cw - pad                 # right content grid

    # card
    b += r(cx + 10, cy + 14, cw, ch, "#000", op=p["shadow"])
    b += r(cx, cy, cw, ch, p["card"])
    if key.startswith("b_"):
        b += r(cx, cy, cw, ch, None, stroke="#2E2E34", sw=1)
        b += ln(cx, cy, cx + cw, cy, p["accent"], 2)               # gold top hairline

    # ── header (no muddy band — typography does the work) ──
    hy = cy + 84
    b += t(gx, hy, "派 工 名 单", 40, p["ink"], weight="bold", sp=10)
    b += t(gx + 2, hy + 34, "黑色委托事务所 · 外勤派工", 16, p["dim"], sp=3)
    b += t(gx + 2, hy - 52, "DISPATCH ROSTER — FORM BC-02", 12, p["dim"], sp=4)
    if key == "c_swiss":
        b += r(gx, hy - 76, 16, 16, p["red"])                       # signal-red square
        b += ln(cx, cy + 150, cx + cw, cy + 150, p["ink"], 5)       # thick swiss rule
        b += ln(cx, cy + 158, cx + cw, cy + 158, p["ink"], 1)
    else:
        b += ln(gx, cy + 152, gr, cy + 152, p["rule"], 1)

    # room code — the hero on the right
    if p["code_chip"]:                 # A: inverted taupe chip
        b += r(gr - 264, cy + 44, 264, 78, p["accent"])
        b += t(gr - 250, cy + 74, "房间码 ROOM CODE", 13, p["accent_tx"], sp=3, op=0.75)
        b += t(gr - 250, cy + 112, "K7F2Q", 42, p["accent_tx"], weight="bold", sp=12)
    else:
        b += t(gr, cy + 66, "房间码 ROOM CODE", 13, p["dim"], anchor="end", sp=4)
        b += t(gr, cy + 122, "K7F2Q", 56, p["code"], anchor="end", weight="bold", sp=14)

    # ── roster rows ──
    row_h = 104
    ry0 = cy + 196
    ink, dim = p["ink"], p["dim"]

    # 01 — you
    ry = ry0
    b += t(gx, ry + 38, "01", 15, dim, sp=2)
    b += r(gx + 44, ry + 6, 8, 44, p["vests"][0])
    b += t(gx + 76, ry + 42, "老王", 30, ink, weight="bold")
    b += t(gx + 146, ry + 42, "（你）", 17, dim)
    px = gx + 320
    b += t(px, ry + 40, "‹", 26, dim)
    b += dot(px + 44, ry + 30, 11, "sel", p["vests"][0], ink, dim)
    b += dot(px + 90, ry + 30, 11, "open", p["vests"][1], ink, dim)
    b += dot(px + 136, ry + 30, 11, "taken", p["vests"][2], ink, dim)
    b += dot(px + 182, ry + 30, 11, "open", p["vests"][3], ink, dim)
    b += t(px + 222, ry + 40, "›", 26, dim)
    b += t(px + 262, ry + 38, "更换工装", 15, dim, sp=1)
    b += ln(px + 262, ry + 46, px + 322, ry + 46, dim, 1, op=0.6)
    # 负责 chip
    if key.startswith("b_"):
        b += r(gr - 92, ry + 8, 92, 40, None, stroke=p["accent"], sw=1.5)
        b += t(gr - 46, ry + 35, "负 责", 17, p["accent"], anchor="middle", weight="bold", sp=4)
    else:
        b += r(gr - 92, ry + 8, 92, 40, p["accent"])
        b += t(gr - 46, ry + 35, "负 责", 17, p["accent_tx"], anchor="middle", weight="bold", sp=4)
    b += ln(gx, ry + row_h - 24, gr, ry + row_h - 24, p["hair"], 1)

    # 02 — teammate
    ry += row_h
    b += t(gx, ry + 38, "02", 15, dim, sp=2)
    b += r(gx + 44, ry + 6, 8, 44, p["vests"][2])
    b += t(gx + 76, ry + 42, "Agent 2", 30, ink, weight="bold")
    b += t(gr - 168, ry + 38, "静音", 16, dim, anchor="end", sp=2)
    b += ln(gr - 200, ry + 46, gr - 168, ry + 46, dim, 1, op=0.6)
    b += t(gr, ry + 38, "按住除名", 16, p["red"], anchor="end", sp=2, op=0.9)
    b += r(gr - 96, ry + 50, 96, 3, p["hair"])
    b += r(gr - 96, ry + 50, 54, 3, p["red"])
    b += ln(gx, ry + row_h - 24, gr, ry + row_h - 24, p["hair"], 1)

    # 03 / 04 — empty
    for n in ("03", "04"):
        ry += row_h
        b += t(gx, ry + 38, n, 15, dim, sp=2, op=0.6)
        b += t(gx + 76, ry + 40, "（空缺）", 20, dim, op=0.65)
        b += ln(gx + 220, ry + 32, gr, ry + 32, dim, 2, op=0.35, dash="2 10")
        b += ln(gx, ry + row_h - 24, gr, ry + row_h - 24, p["hair"], 1, op=0.7)

    # ── footer ──
    fy = cy + ch - 68
    if key == "c_swiss":
        b += ln(cx, fy - 34, cx + cw, fy - 34, p["ink"], 3)
    else:
        b += ln(gx, fy - 34, gr, fy - 34, p["rule"], 1)
    key_chip_w = 96
    kx = cx + cw / 2 - 230
    b += r(kx, fy - 8, key_chip_w, 36, None, stroke=ink, sw=1.5)
    b += t(kx + key_chip_w / 2, fy + 16, "Enter", 17, ink, anchor="middle", weight="bold", sp=2)
    b += t(kx + key_chip_w + 24, fy + 17, "确认到岗，进入办公室", 24, ink, weight="bold", sp=3)
    b += t(cx + cw / 2 - 230 + key_chip_w + 24, fy + 44, "报到先后不限——办公室就是集合点", 13, dim, sp=2)
    # 成立 stamp
    b += (f'<g transform="translate({gr - 96},{fy - 12}) rotate(-8)" opacity="0.85">'
          + r(0, 0, 96, 50, None, stroke=p["red"], sw=3)
          + t(48, 34, "成 立", 21, p["red"], anchor="middle", weight="bold", sp=6)
          + "</g>")

    # variant label (top-left of frame, not part of the UI)
    label = {"a_nier": "A — 米灰 · NieR 系", "b_noir": "B — 黑金 · 负债黑卡", "c_swiss": "C — 瑞士市政 · 信号红",
             "b_sig_red": "B-红 — 签名色 = 印章红", "b_sig_amber": "B-琥珀 — 签名色 = 琥珀 (≈LC橙)"}[key]
    b += t(48, 64, label, 22, "#8E897B", sp=2)

    return (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'
            + b + "</svg>")


os.makedirs(OUT, exist_ok=True)
for key in PALETTES:
    path = os.path.join(OUT, f"03_roster_v3_{key}.svg")
    open(path, "w", encoding="utf-8").write(build(key))
    print("wrote", path)
