# -*- coding: utf-8 -*-
"""UI 风格概念板 ×4 — PM 2026-07-04「先出概念图统一整体风格,你都画一套我选」.

Four full style-concept boards, one per anchor direction. Each board = left rail
(palette swatches / type specimen / material chips / one-line 气质) + hero mockup
(the SAME dispatch-roster content rendered in that style, over a dark scene).

Real-material pass: SVG filter compositing (feTurbulence paper fiber via diffuse
lighting, displacement-map stains, brushed-metal streaks, wood grain, film grain,
mix-blend-mode multiply/overlay) — NO flat vector look.

  A  desk   — 桌面实物恐怖 (Buckshot Roulette / Inscryption): oily paper on dark wood,
              tungsten lamp, heavy grime. UI pretends to be a physical object.
  B  cold   — 军规冷硬 (GTFO 系): pure black, bone-white condensed caps, hairlines,
              red warnings. Premium via restraint, zero skeuomorphism.
  C  lofi   — 工业 lo-fi (Lethal Company 系, 硫磺黄): dark CRT panel, sulfur-yellow
              signature (yellow-family, NOT LC's saturated orange), mono type, noise.
  D  files  — 军事档案写实 (Tarkov 系): worn gunmetal panel, stencil labels, kraft
              tags, olive/khaki + red stamps.

Output: design/ux/mockups/style-concepts/concept_{a_desk,b_cold,c_lofi,d_files}.svg
Render PNG via headless Chrome.
"""
import os
import random

OUT = os.path.join(os.path.dirname(__file__), "..", "design", "ux", "mockups", "style-concepts")
W, H = 1920, 1080
SANS = "'Helvetica Neue','PingFang SC','Hiragino Sans GB',sans-serif"
MONO = "'Menlo','PingFang SC',monospace"
COND = "'Arial Narrow','Helvetica Neue','PingFang SC',sans-serif"   # B 军规窄体
TYPE = "'American Typewriter','Courier New','PingFang SC',serif"     # A 打字机
TERM = "'BC3270','Menlo','PingFang SC',monospace"                    # C 游戏真字体 (LC 同款 3270)
PLATE = "'Copperplate','Arial Narrow','PingFang SC',serif"           # D 军牌雕刻体
import base64 as _b64
_TTF = os.path.join(os.path.dirname(__file__), "..", "Assets", "_Project", "Art", "UI", "Fonts", "3270-Regular.ttf")
FONT_B64 = _b64.b64encode(open(_TTF, "rb").read()).decode()


def esc(s):
    return s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")


def t(x, y, s, size, fill, anchor="start", weight=None, sp=None, op=None, font=SANS):
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


# ── shared filter defs (materials engine) ──
DEFS = f'''<defs>
  <style>@font-face{{font-family:'BC3270';src:url('data:font/ttf;base64,{FONT_B64}') format('truetype');}}</style>
  <filter id="fiber" x="0" y="0" width="100%" height="100%">
    <feTurbulence type="fractalNoise" baseFrequency="0.9" numOctaves="3" seed="4" result="n"/>
    <feDiffuseLighting in="n" lighting-color="#ffffff" surfaceScale="1.6" result="l">
      <feDistantLight azimuth="45" elevation="55"/>
    </feDiffuseLighting>
    <feComposite in="l" in2="SourceGraphic" operator="in"/>
  </filter>
  <filter id="grain"><feTurbulence type="fractalNoise" baseFrequency="0.75" numOctaves="2" seed="11" stitchTiles="stitch"/>
    <feColorMatrix type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  0 0 0 0.6 0"/></filter>
  <filter id="blob"><feTurbulence type="fractalNoise" baseFrequency="0.02" numOctaves="3" seed="7" result="t"/>
    <feDisplacementMap in="SourceGraphic" in2="t" scale="55"/></filter>
  <filter id="blob2"><feTurbulence type="fractalNoise" baseFrequency="0.035" numOctaves="3" seed="23" result="t"/>
    <feDisplacementMap in="SourceGraphic" in2="t" scale="70"/></filter>
  <filter id="rough"><feTurbulence type="fractalNoise" baseFrequency="0.012" numOctaves="4" seed="9" result="t"/>
    <feDisplacementMap in="SourceGraphic" in2="t" scale="8"/></filter>
  <filter id="wood" x="0" y="0" width="100%" height="100%">
    <feTurbulence type="fractalNoise" baseFrequency="0.0035 0.11" numOctaves="4" seed="5" result="n"/>
    <feColorMatrix in="n" type="matrix" values="0 0 0 0 0.13  0 0 0 0 0.09  0 0 0 0 0.055  0 0 0 0.9 0" result="c"/>
    <feComposite in="c" in2="SourceGraphic" operator="in"/>
  </filter>
  <filter id="metal" x="0" y="0" width="100%" height="100%">
    <feTurbulence type="fractalNoise" baseFrequency="0.002 0.35" numOctaves="3" seed="14" result="n"/>
    <feColorMatrix in="n" type="matrix" values="0 0 0 0 0.75  0 0 0 0 0.78  0 0 0 0 0.8  0 0 0 0.16 0" result="c"/>
    <feComposite in="c" in2="SourceGraphic" operator="in"/>
  </filter>
  <filter id="crt"><feTurbulence type="fractalNoise" baseFrequency="0.4 0.9" numOctaves="2" seed="3"/>
    <feColorMatrix type="matrix" values="0 0 0 0 0  0 0 0 0 0  0 0 0 0 0  0 0 0 0.5 0"/></filter>
  <radialGradient id="vig"><stop offset="0.42" stop-color="#000" stop-opacity="0"/>
    <stop offset="1" stop-color="#000" stop-opacity="0.78"/></radialGradient>
  <radialGradient id="lamp"><stop offset="0" stop-color="#E8B25C" stop-opacity="0.5"/>
    <stop offset="0.55" stop-color="#B57F33" stop-opacity="0.14"/><stop offset="1" stop-color="#000" stop-opacity="0"/></radialGradient>
  <radialGradient id="lampCold"><stop offset="0" stop-color="#CFE4E4" stop-opacity="0.16"/>
    <stop offset="1" stop-color="#000" stop-opacity="0"/></radialGradient>
  <radialGradient id="lampYellow"><stop offset="0" stop-color="#E5C25A" stop-opacity="0.34"/>
    <stop offset="0.6" stop-color="#8E7326" stop-opacity="0.1"/><stop offset="1" stop-color="#000" stop-opacity="0"/></radialGradient>
</defs>'''


def rand(seed):
    return random.Random(seed)


def scratches(x, y, w, h, color, n, seed, op=0.1):
    g, rd = "", rand(seed)
    for _ in range(n):
        x1 = x + rd.random() * w
        y1 = y + rd.random() * h
        dx = (rd.random() - 0.3) * 120
        dy = (rd.random() - 0.5) * 30
        g += ln(x1, y1, x1 + dx, y1 + dy, color, rd.choice([0.6, 0.8, 1.1]), op=op * rd.random())
    return g


def stain(cx, cy, rx_, ry_, color, op, which="blob"):
    return f'<ellipse cx="{cx}" cy="{cy}" rx="{rx_}" ry="{ry_}" fill="{color}" opacity="{op}" filter="url(#{which})"/>'


def coffee_ring(cx, cy, rr, color, op):
    return (f'<circle cx="{cx}" cy="{cy}" r="{rr}" fill="none" stroke="{color}" stroke-width="7" '
            f'opacity="{op}" filter="url(#blob2)"/>')


def grain(op):
    return r(0, 0, W, H, "#fff", filt="grain", op=op, style="mix-blend-mode:overlay")


def vignette():
    return r(0, 0, W, H, "url(#vig)")


# ═══════════════ hero card content (shared, style hooks) ═══════════════
def hero_roster(s):
    """Draw the roster mockup inside hero zone. s = style dict with painters."""
    hx, hy, hw, hh = 700, 120, 1120, 840
    g = s["scene"](hx, hy, hw, hh)

    cw, ch = 880, 620
    cx, cy = hx + (hw - cw) // 2, hy + (hh - ch) // 2
    g += s["surface"](cx, cy, cw, ch)

    ink, dim, hair, sig = s["ink"], s["dim"], s["hair"], s["sig"]
    pad = 48
    gx, gr = cx + pad, cx + cw - pad

    # header
    g += t(gx, cy + 58, s["kicker"], 12, dim, sp=5, font=s["font_small"])
    g += t(gx, cy + 106, "DISPATCH ROSTER", 36, ink, weight="bold", sp=6, font=s.get("font_title", s["font"]))
    g += t(gx, cy + 136, "BLACK COMMISSION — OUTSOURCED FIELD WORK", 13, dim, sp=3, font=s["font"])
    g += s["code"](gr, cy)          # room code treatment
    g += ln(gx, cy + 158, gr, cy + 158, hair, s["rule_w"])

    # rows
    row_h = 86
    ry = cy + 176
    vests = s["vests"]
    g += t(gx, ry + 34, "01", 13, dim, sp=2, font=s["font_small"])
    g += r(gx + 36, ry + 4, 7, 38, vests[0])
    g += t(gx + 62, ry + 36, "WANG", 25, ink, weight="bold", font=s["font"])
    g += t(gx + 148, ry + 36, "(YOU)", 13, dim, font=s["font_small"])
    px = gx + 260
    for i, st_ in enumerate(["sel", "open", "taken", "open"]):
        ccx = px + 38 * i
        if st_ == "sel":
            g += f'<circle cx="{ccx}" cy="{ry + 26}" r="9" fill="{vests[i]}"/><circle cx="{ccx}" cy="{ry + 26}" r="13" fill="none" stroke="{ink}" stroke-width="1.4"/>'
        elif st_ == "open":
            g += f'<circle cx="{ccx}" cy="{ry + 26}" r="9" fill="{vests[i]}" opacity="0.55"/>'
        else:
            g += f'<circle cx="{ccx}" cy="{ry + 26}" r="9" fill="none" stroke="{dim}" stroke-width="1.4" stroke-dasharray="3 3"/>'
    g += t(px + 160, ry + 33, "CHANGE GEAR", 12, dim, sp=1, font=s["font_small"])
    g += s["chip"](gr, ry)          # 负责 chip treatment
    g += ln(gx, ry + row_h - 20, gr, ry + row_h - 20, hair, s["rule_w"], op=0.8)

    ry += row_h
    g += t(gx, ry + 34, "02", 13, dim, sp=2, font=s["font_small"])
    g += r(gx + 36, ry + 4, 7, 38, vests[2])
    g += t(gx + 62, ry + 36, "AGENT 2", 25, ink, weight="bold", font=s["font"])
    g += t(gr - 150, ry + 33, "MUTE", 13, dim, anchor="end", sp=1, font=s["font_small"])
    g += t(gr, ry + 33, "HOLD: KICK", 13, s["danger"], anchor="end", sp=1, font=s["font_small"])
    g += r(gr - 84, ry + 42, 84, 3, hair, op=0.6)
    g += r(gr - 84, ry + 42, 46, 3, s["danger"])
    g += ln(gx, ry + row_h - 20, gr, ry + row_h - 20, hair, s["rule_w"], op=0.8)

    for n in ("03", "04"):
        ry += row_h
        g += t(gx, ry + 34, n, 13, dim, sp=2, op=0.6, font=s["font_small"])
        g += t(gx + 62, ry + 34, "(VACANT)", 15, dim, op=0.6, font=s["font_small"])
        g += ln(gx + 180, ry + 27, gr, ry + 27, dim, 2, op=0.3, dash="2 9")
        g += ln(gx, ry + row_h - 20, gr, ry + row_h - 20, hair, s["rule_w"], op=0.7)

    # footer
    fy = cy + ch - 62
    g += ln(gx, fy - 24, gr, fy - 24, hair, s["rule_w"])
    g += r(gx, fy - 4, 78, 32, None, stroke=ink, sw=1.4, rx=2)
    g += t(gx + 39, fy + 17, "Enter", 14, ink, anchor="middle", weight="bold", sp=2, font=s["font_small"])
    g += t(gx + 100, fy + 18, "REPORT IN — ENTER THE OFFICE", 19, ink, weight="bold", sp=2, font=s["font"])
    g += s["stamp"](gr, fy)
    g += s["post"](hx, hy, hw, hh, cx, cy, cw, ch)
    return g


# ═══════════════ left rail (palette / type / materials) ═══════════════
def rail(s):
    g = r(0, 0, 660, H, "#0B0B0C")
    g += ln(660, 0, 660, H, "#1E1E20", 2)
    g += t(56, 96, s["tag"], 20, "#6E6A60", sp=4)
    g += t(56, 158, s["title"], 44, "#E6DFCE", weight="bold", sp=4)
    g += t(56, 196, s["ref"], 15, "#8A857A", sp=1)
    g += t(56, 252, s["mood"], 17, "#B9B2A2", sp=1)

    # palette swatches
    g += t(56, 330, "色板 · 签名色打星", 14, "#6E6A60", sp=3)
    sx = 56
    for i, (hexv, name, star) in enumerate(s["swatches"]):
        x = sx + i * 112
        g += r(x, 350, 96, 96, hexv, stroke="#2A2A2C", sw=1)
        if star:
            g += t(x + 84, 374, "★", 16, "#E6DFCE" if i < 2 else "#111")
        g += t(x, 470, name, 13, "#B9B2A2")
        g += t(x, 490, hexv, 12, "#6E6A60", font=MONO)

    # type specimen
    g += t(56, 566, "字体样张", 14, "#6E6A60", sp=3)
    g += t(56, 622, s["spec_en"], 30, "#E6DFCE", weight=s["spec_weight"], sp=s["spec_sp"], font=s.get("font_title", s["font"]))
    g += t(56, 662, "AaBb0123 — ROOM CODE K7F2Q", 19, "#8A857A", sp=3, font=s["font_small"])
    g += t(56, 694, s["spec_zh"] + "（中文回退: 苹方）", 14, "#5E5A50", sp=2)

    # material chips
    g += t(56, 740, "材质样片", 14, "#6E6A60", sp=3)
    for i, painter in enumerate(s["mat_chips"]):
        g += painter(56 + i * 190, 762, 166, 120)

    g += t(56, 952, s["verdict"], 16, "#B9B2A2", sp=1)
    g += t(56, 980, s["verdict2"], 16, "#B9B2A2", sp=1)
    g += t(56, H - 40, "Black Commission · UI 风格概念板 · 2026-07-04", 13, "#4E4B44", sp=2)
    return g


# ═══════════════ material painters ═══════════════
def m_paper_dirty(x, y, w, h):
    g = r(x, y, w, h, "#C9BFA4")
    g += r(x, y, w, h, "#fff", filt="fiber", op=0.5, style="mix-blend-mode:multiply")
    g += stain(x + w * 0.3, y + h * 0.4, w * 0.3, h * 0.25, "#7A5B33", 0.16)
    g += stain(x + w * 0.75, y + h * 0.7, w * 0.2, h * 0.2, "#5A4426", 0.13, "blob2")
    g += scratches(x, y, w, h, "#4A3A22", 8, seed=x + 1, op=0.25)
    return g


def m_wood_dark(x, y, w, h):
    g = r(x, y, w, h, "#241A10")
    g += r(x, y, w, h, "#fff", filt="wood", op=0.85)
    g += r(x, y, w, h, "#000", op=0.25)
    return g


def m_black_matte(x, y, w, h):
    g = r(x, y, w, h, "#101012")
    g += r(x, y, w, h, "#fff", filt="fiber", op=0.08, style="mix-blend-mode:screen")
    g += ln(x, y + h * 0.3, x + w, y + h * 0.3, "#1E1E22", 1)
    return g


def m_hairline(x, y, w, h):
    g = r(x, y, w, h, "#0C0C0E")
    for i in range(1, 4):
        g += ln(x + 12, y + h * i / 4, x + w - 12, y + h * i / 4, "#CFD6D2", 0.8, op=0.5)
    g += t(x + 12, y + 24, "AA 07", 13, "#CFD6D2", sp=3, font=MONO, op=0.8)
    return g


def m_crt_panel(x, y, w, h):
    g = r(x, y, w, h, "#15130C")
    g += r(x, y, w, h, "#E5C25A", filt="crt", op=0.1, style="mix-blend-mode:screen")
    for yy in range(int(y) + 4, int(y + h), 5):
        g += ln(x, yy, x + w, yy, "#000", 1, op=0.3)
    g += t(x + 12, y + h / 2, "> BC-DOS", 15, "#D9B03F", font=MONO)
    return g


def m_sulfur(x, y, w, h):
    g = r(x, y, w, h, "#C9A23B")
    g += r(x, y, w, h, "#fff", filt="fiber", op=0.35, style="mix-blend-mode:multiply")
    g += stain(x + w * 0.6, y + h * 0.5, w * 0.3, h * 0.3, "#6E5410", 0.25)
    return g


def m_gunmetal(x, y, w, h):
    g = r(x, y, w, h, "#33373A")
    g += r(x, y, w, h, "#fff", filt="metal", op=0.7, style="mix-blend-mode:overlay")
    g += r(x, y, w, h, "#000", op=0.2)
    g += scratches(x, y, w, h, "#C9CFD4", 10, seed=int(x) + 3, op=0.3)
    return g


def m_kraft_tag(x, y, w, h):
    g = r(x, y, w, h, "#8C6E3F")
    g += r(x, y, w, h, "#fff", filt="fiber", op=0.4, style="mix-blend-mode:multiply")
    g += f'<circle cx="{x + 20}" cy="{y + 20}" r="6" fill="none" stroke="#3A2D18" stroke-width="2"/>'
    return g


def m_stencil(x, y, w, h):
    g = r(x, y, w, h, "#2A2D26")
    g += t(x + w / 2, y + h / 2 + 8, "K-7", 30, "#B8B29A", anchor="middle", weight="bold", sp=6, op=0.85)
    g += r(x, y, w, h, "#fff", filt="metal", op=0.3, style="mix-blend-mode:overlay")
    return g


# ═══════════════ style definitions ═══════════════
def make_styles():
    styles = {}

    # ── A 桌面实物恐怖 ──
    def a_scene(x, y, w, h):
        g = r(x, y, w, h, "#17100A")
        g += r(x, y, w, h, "#fff", filt="wood", op=0.9)
        g += f'<ellipse cx="{x + w * 0.5}" cy="{y + h * 0.42}" rx="{w * 0.62}" ry="{h * 0.6}" fill="url(#lamp)"/>'
        g += r(x, y, w, h, "#000", op=0.22)
        return g

    def a_surface(x, y, w, h):
        g = f'<g transform="rotate(-0.8 {x + w/2} {y + h/2})">'
        g += r(x + 10, y + 14, w, h, "#000", op=0.6)
        g += r(x, y, w, h, "#C6BB9E", filt="rough")
        g += r(x, y, w, h, "#fff", filt="fiber", op=0.55, style="mix-blend-mode:multiply")
        g += stain(x + w * 0.82, y + h * 0.16, 90, 60, "#6E5228", 0.18)
        g += stain(x + w * 0.14, y + h * 0.85, 110, 60, "#54401F", 0.14, "blob2")
        g += coffee_ring(x + w * 0.72, y + h * 0.82, 46, "#6E4F22", 0.3)
        g += scratches(x, y, w, h, "#3A2C16", 14, seed=41, op=0.2)
        # tape corners
        g += r(x - 14, y + 18, 54, 22, "#D8CCA8", op=0.5)
        g += r(x + w - 40, y + h - 34, 54, 22, "#D8CCA8", op=0.5)
        g += "</g>"
        return g

    def a_code(gr, cy):
        g = f'<g transform="rotate(1.2 {gr - 100} {cy + 90})">'
        g += r(gr - 208, cy + 44, 208, 74, "#8C6E3F")
        g += r(gr - 208, cy + 44, 208, 74, "#fff", filt="fiber", op=0.4, style="mix-blend-mode:multiply")
        g += t(gr - 192, cy + 70, "ROOM CODE", 11, "#3A2D18", sp=4, font=MONO)
        g += t(gr - 192, cy + 104, "K7F2Q", 32, "#2E2312", weight="bold", sp=8, font=MONO)
        g += "</g>"
        return g

    def a_chip(gr, ry):
        return (r(gr - 80, ry + 4, 80, 34, "#8C6E3F", rx=2)
                + t(gr - 40, ry + 27, "LEAD", 13, "#2E2312", anchor="middle", weight="bold", sp=3, font=MONO))

    def a_stamp(gr, fy):
        return (f'<g transform="rotate(-11 {gr - 60} {fy + 8})" opacity="0.8">'
                + f'<circle cx="{gr - 60}" cy="{fy + 8}" r="38" fill="none" stroke="#A33324" stroke-width="2.5" filter="url(#rough)"/>'
                + f'<circle cx="{gr - 60}" cy="{fy + 8}" r="30" fill="none" stroke="#A33324" stroke-width="1.2" filter="url(#rough)"/>'
                + t(gr - 60, fy + 13, "FILED", 15, "#A33324", anchor="middle", weight="bold", sp=3, font=TYPE) + "</g>")

    def a_post(hx, hy, hw, hh, cx, cy, cw, ch):
        return grain(0.14) + ""

    styles["a_desk"] = dict(
        tag="方向 A", title="桌面实物恐怖", ref="参照: Buckshot Roulette · Inscryption",
        mood="UI 是桌上一件真实的东西——油纸、钨丝灯、木桌、胶带。",
        swatches=[("#17100A", "黑木桌", False), ("#C6BB9E", "油纸", False), ("#8C6E3F", "牛皮签", True),
                  ("#E8B25C", "钨丝光", False), ("#A33324", "章红", False)],
        spec_zh="派工名单 黑色委托", spec_en="DISPATCH ROSTER 0123", spec_weight="bold", spec_sp=4,
        font=TYPE, font_small="'Courier New',monospace", font_title=TYPE,
        mat_chips=[m_wood_dark, m_paper_dirty, m_kraft_tag],
        verdict="强: 质感天花板·恐怖氛围满 / 弱: 实现最贵(要纹理资产),", verdict2="小字可读性要盯紧。",
        scene=a_scene, surface=a_surface, code=a_code, chip=a_chip, stamp=a_stamp, post=a_post,
        ink="#2B241A", dim="#7A6E55", hair="#A99C7E", sig="#8C6E3F", danger="#A33324",
        vests=["#68724F", "#B9924C", "#8C5937", "#5F6D74"], rule_w=1, kicker="INTERNAL FILE — DO NOT REMOVE",
    )

    # ── B 军规冷硬 ──
    def b_scene(x, y, w, h):
        g = r(x, y, w, h, "#050506")
        g += f'<ellipse cx="{x + w * 0.5}" cy="{y + h * 0.4}" rx="{w * 0.6}" ry="{h * 0.55}" fill="url(#lampCold)"/>'
        return g

    def b_surface(x, y, w, h):
        g = r(x, y, w, h, "#0C0C0E")
        g += r(x, y, w, h, None, stroke="#26262B", sw=1)
        g += ln(x, y, x + w, y, "#CFD6D2", 2, op=0.9)
        g += r(x, y, w, h, "#fff", filt="fiber", op=0.05, style="mix-blend-mode:screen")
        return g

    def b_code(gr, cy):
        g = t(gr, cy + 62, "ROOM CODE", 12, "#5E6560", anchor="end", sp=6, font=MONO)
        g += t(gr, cy + 118, "K7F2Q", 50, "#DDE4DE", anchor="end", weight="bold", sp=16, font=COND)
        g += ln(gr - 232, cy + 132, gr, cy + 132, "#C4372B", 3)
        return g

    def b_chip(gr, ry):
        return (r(gr - 80, ry + 4, 80, 34, None, stroke="#DDE4DE", sw=1.2)
                + t(gr - 40, ry + 27, "LEAD", 13, "#DDE4DE", anchor="middle", weight="bold", sp=3, font=MONO))

    def b_stamp(gr, fy):
        return (r(gr - 130, fy - 6, 130, 38, None, stroke="#C4372B", sw=2)
                + t(gr - 65, fy + 19, "ACCEPTED", 15, "#C4372B", anchor="middle", weight="bold", sp=4, font=COND))

    def b_post(hx, hy, hw, hh, cx, cy, cw, ch):
        g = t(cx + 2, cy - 12, "BC//DISPATCH.02", 12, "#4E554F", sp=6, font=MONO)
        g += grain(0.05)
        return g

    styles["b_cold"] = dict(
        tag="方向 B", title="军规冷硬", ref="参照: GTFO · 10 Chambers",
        mood="纯黑+骨白+一根红。没有拟物,高级感全靠克制和字距。",
        swatches=[("#050506", "纯黑", False), ("#DDE4DE", "骨白", True), ("#5E6560", "哑灰", False),
                  ("#C4372B", "警示红", False), ("#26262B", "发丝线", False)],
        spec_zh="派工名单 黑色委托", spec_en="DISPATCH ROSTER 0123", spec_weight="bold", spec_sp=10,
        font=COND, font_small=MONO, font_title=COND,
        mat_chips=[m_black_matte, m_hairline, m_stencil],
        verdict="强: 最容易做统一·实现最便宜 / 弱: 最不'事务所',", verdict2="世界观味道最淡,容易像科幻。",
        scene=b_scene, surface=b_surface, code=b_code, chip=b_chip, stamp=b_stamp, post=b_post,
        ink="#DDE4DE", dim="#5E6560", hair="#26262B", sig="#DDE4DE", danger="#C4372B",
        vests=["#7E8F6A", "#D2A45C", "#A66844", "#7C8B94"], rule_w=1, kicker="BC//ROSTER — INTERNAL",
    )

    # ── C 工业 lo-fi (硫磺黄) ──
    def c_scene(x, y, w, h):
        g = r(x, y, w, h, "#0D0C08")
        g += f'<ellipse cx="{x + w * 0.5}" cy="{y + h * 0.42}" rx="{w * 0.62}" ry="{h * 0.58}" fill="url(#lampYellow)"/>'
        g += r(x, y, w, h, "#fff", filt="crt", op=0.06, style="mix-blend-mode:screen")
        return g

    def c_surface(x, y, w, h):
        g = r(x + 8, y + 10, w, h, "#000", op=0.5)
        g += r(x, y, w, h, "#171509")
        g += r(x, y, w, h, None, stroke="#3A3210", sw=2)
        g += r(x, y, 8, h, "#D9B03F")
        for yy in range(int(y) + 3, int(y + h), 6):
            g += ln(x, yy, x + w, yy, "#000", 1, op=0.22)
        g += r(x, y, w, h, "#D9B03F", filt="crt", op=0.05, style="mix-blend-mode:screen")
        return g

    def c_code(gr, cy):
        g = t(gr, cy + 62, "ROOM CODE", 12, "#8E7326", anchor="end", sp=5, font=MONO)
        g += r(gr - 218, cy + 74, 218, 52, "#D9B03F", op=0.14)
        g += t(gr - 12, cy + 112, "K7F2Q", 38, "#E9C654", anchor="end", weight="bold", sp=10, font=MONO)
        return g

    def c_chip(gr, ry):
        return (r(gr - 80, ry + 4, 80, 34, "#D9B03F")
                + t(gr - 40, ry + 27, "LEAD", 13, "#171509", anchor="middle", weight="bold", sp=3, font=TERM))

    def c_stamp(gr, fy):
        return (r(gr - 130, fy - 6, 130, 38, None, stroke="#B5372A", sw=3, style="filter:url(#rough)")
                + t(gr - 65, fy + 19, "FILED", 15, "#C0473A", anchor="middle", weight="bold", sp=6, font=TERM))

    def c_post(hx, hy, hw, hh, cx, cy, cw, ch):
        g = t(cx + 14, cy - 12, "> BC-DOS v2.2 — DISPATCH", 13, "#8E7326", sp=2, font=MONO)
        g += grain(0.1)
        return g

    styles["c_lofi"] = dict(
        tag="方向 C", title="工业 lo-fi · 硫磺黄", ref="参照: Lethal Company 家族 (签名色避开它的橙)",
        mood="廉价终端+硫磺黄贯穿。噪点扫描线,像公司发的破设备。",
        swatches=[("#0D0C08", "沥青黑", False), ("#D9B03F", "硫磺黄", True), ("#171509", "屏底", False),
                  ("#8E7326", "暗黄", False), ("#B5372A", "章红", False)],
        spec_zh="派工名单 黑色委托", spec_en="DISPATCH ROSTER 0123", spec_weight="bold", spec_sp=4,
        font=TERM, font_small=TERM, font_title=TERM,
        mat_chips=[m_crt_panel, m_sulfur, m_black_matte],
        verdict="强: 赛道验证过·便宜感=氛围 / 弱: 离 LC 最近,", verdict2="签名色靠硫磺黄(非橙)拉开,仍有像它的风险。",
        scene=c_scene, surface=c_surface, code=c_code, chip=c_chip, stamp=c_stamp, post=c_post,
        ink="#E4D9A8", dim="#8E7326", hair="#3A3210", sig="#D9B03F", danger="#C0473A",
        vests=["#7E8F6A", "#D2A45C", "#A66844", "#7C8B94"], rule_w=1, kicker="> ROSTER — INTERNAL FILE",
    )

    # ── D 军事档案写实 ──
    def d_scene(x, y, w, h):
        g = r(x, y, w, h, "#101210")
        g += r(x, y, w, h, "#fff", filt="metal", op=0.25, style="mix-blend-mode:overlay")
        g += f'<ellipse cx="{x + w * 0.5}" cy="{y + h * 0.4}" rx="{w * 0.58}" ry="{h * 0.52}" fill="url(#lampCold)"/>'
        g += r(x, y, w, h, "#000", op=0.3)
        return g

    def d_surface(x, y, w, h):
        g = r(x + 8, y + 10, w, h, "#000", op=0.55)
        g += r(x, y, w, h, "#3A3E3C")
        g += r(x, y, w, h, "#fff", filt="metal", op=0.55, style="mix-blend-mode:overlay")
        g += r(x, y, w, h, "#000", op=0.28)
        g += r(x, y, w, h, None, stroke="#1C1E1C", sw=3)
        # rivets
        for px_, py_ in [(x + 18, y + 18), (x + w - 18, y + 18), (x + 18, y + h - 18), (x + w - 18, y + h - 18)]:
            g += f'<circle cx="{px_}" cy="{py_}" r="5" fill="#232624" stroke="#0E100E" stroke-width="1.5"/>'
        g += scratches(x, y, w, h, "#C9CFD4", 16, seed=77, op=0.18)
        # paper sheet riveted onto the metal panel
        g += r(x + 26, y + 26, w - 52, h - 52, "#CDC4A9", filt="rough")
        g += r(x + 26, y + 26, w - 52, h - 52, "#fff", filt="fiber", op=0.5, style="mix-blend-mode:multiply")
        g += stain(x + w * 0.8, y + h * 0.2, 80, 50, "#5E4E2C", 0.14)
        return g

    def d_code(gr, cy):
        g = f'<g transform="rotate(-1.4 {gr - 100} {cy + 90})">'
        g += r(gr - 196, cy + 48, 196, 66, "#8C6E3F")
        g += r(gr - 196, cy + 48, 196, 66, "#fff", filt="fiber", op=0.4, style="mix-blend-mode:multiply")
        g += f'<circle cx="{gr - 180}" cy="{cy + 64}" r="5" fill="none" stroke="#3A2D18" stroke-width="2"/>'
        g += t(gr - 164, cy + 72, "ROOM CODE", 10, "#3A2D18", sp=3, font=MONO)
        g += t(gr - 164, cy + 102, "K7F2Q", 28, "#2E2312", weight="bold", sp=8, font=MONO)
        g += "</g>"
        return g

    def d_chip(gr, ry):
        return (r(gr - 88, ry + 4, 88, 34, "#4A5240", rx=2)
                + t(gr - 44, ry + 27, "LEAD", 13, "#CDC4A9", anchor="middle", weight="bold", sp=3, font=PLATE))

    def d_stamp(gr, fy):
        return (f'<g transform="rotate(-7 {gr - 70} {fy + 10})" opacity="0.85">'
                + r(gr - 140, fy - 10, 140, 42, None, stroke="#A33324", sw=3, style="filter:url(#rough)")
                + t(gr - 70, fy + 18, "ARCHIVED", 15, "#A33324", anchor="middle", weight="bold", sp=4, font=PLATE) + "</g>")

    def d_post(hx, hy, hw, hh, cx, cy, cw, ch):
        return grain(0.09)

    styles["d_files"] = dict(
        tag="方向 D", title="军事档案写实", ref="参照: Escape from Tarkov · Marauders",
        mood="磨损钢板上铆一张任务纸——金属+旧纸+模板字,硬核写实。",
        swatches=[("#101210", "机库黑", False), ("#3A3E3C", "枪钢灰", False), ("#CDC4A9", "任务纸", False),
                  ("#4A5240", "军灰绿", True), ("#A33324", "章红", False)],
        spec_zh="派工名单 黑色委托", spec_en="DISPATCH ROSTER 0123", spec_weight="bold", spec_sp=6,
        font="'Courier New',monospace", font_small="'Courier New',monospace", font_title=PLATE,
        mat_chips=[m_gunmetal, m_paper_dirty, m_stencil],
        verdict="强: 写实不卡通·耐看 / 弱: 军味重,'怪客户的黑事务所'", verdict2="的诡异感弱,容易像战术射击游戏。",
        scene=d_scene, surface=d_surface, code=d_code, chip=d_chip, stamp=d_stamp, post=d_post,
        ink="#2B2A20", dim="#6E6650", hair="#A69C80", sig="#4A5240", danger="#A33324",
        vests=["#68724F", "#B9924C", "#8C5937", "#5F6D74"], rule_w=1, kicker="FIELD DISPATCH — INTERNAL",
    )
    return styles


os.makedirs(OUT, exist_ok=True)
for key, s in make_styles().items():
    body = r(0, 0, W, H, "#0B0B0C") + rail(s) + hero_roster(s) + vignette()
    svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{W}" height="{H}" viewBox="0 0 {W} {H}">'
           + DEFS + body + "</svg>")
    path = os.path.join(OUT, f"concept_{key}.svg")
    open(path, "w", encoding="utf-8").write(svg)
    print("wrote", path)
