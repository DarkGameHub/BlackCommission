# HUD Design — Black Commission

> **Status**: Approved（PM 终审通过 2026-06-11）
> **Author**: Yan Dai + ux-designer
> **Last Updated**: 2026-06-11
> **Template**: HUD Design
> **统一语言定位**: 三类表面之「现场工单」（CRT 终端绿=世界内屏幕 / 盖章公文卡=一切模态 / 现场工单=HUD）

---

## HUD Philosophy

The HUD does not pretend to be a game overlay. It pretends to be paperwork.

During missions, all survival-critical information is always visible — but organized as a field brief clipped to a mental clipboard, not as a game UI. The information exists; its presentation reads as bureaucratic documentation. A line of text, a ruled column, a stamp block.

**The field-form test**: every HUD element must be reproducible in black ink on a photocopied form from a third-rate civic office. If it looks like game chrome rather than administrative paperwork, it fails.

**Density rule — "present but subdued."** All decision-relevant data surfaces during missions: objective, seal completeness, HP, stamina, hotbar. None of it competes for attention. Elements exist until they need to be urgent — at which point they shift register to stamp-red to demand a glance, then fall back.

**HQ context**: the office computer terminal is the game's loud information surface. The in-mission HUD is its inverse — quiet, ledger-formatted, background. They share the same visual grammar but operate at opposite volume levels.

**The earned-precision rule (原「手表规则」)**: the HUD earns precision only when the player invests in it or the fiction grants it. The original carrier was the wristwatch/clock mechanic (no watch = ambient daylight-judgment language; watch = exact clock values) — that mechanic was removed with the school mission, but the principle stays as a binding design test. Current application: **seal completeness shows no number until the column is actually lifted** — before that moment the figure is not the player's business. If a timed mechanic returns, the wristwatch grammar returns with it.

---

## Information Architecture

### Full Information Inventory

All information the game generates during a tower mission, mapped to its source system.
*(Reconciled 2026-06-11 against the post-school-deletion codebase: monster, field clock /
overtime, carrier identity, and bonus evidence rows removed with their systems. If a
monster or timed mechanic returns, extend this inventory then — the spec carries no
dormant rows.)*

| # | Information | Source System | Notes |
|---|---|---|---|
| 1 | HP (current / max) | `PlayerHealth.CurrentHP` | NetworkVariable, per-player |
| 2 | Stamina (current / max) | `PlayerController.Stamina` | Depletes on sprint; recovers on idle |
| 3 | 5-slot hotbar (item icon, quantity, slot number) | `PlayerHotbar` | Slot 1–5, bottom-center |
| 4 | Flashlight battery | `FlashlightController.BatteryNormalized` | Displayed inside slot, not as a separate element |
| 5 | Mission objective (state-driven text) | `TowerMissionManager.SyncedState` | 5 states: InProgress / ObjectiveSecured / Delivered / PartialReturn / Failed |
| 6 | 密封完整度 seal completeness (%) | `TowerMissionManager.SyncedCompleteness` | −3% per hard drop; <50% = 不可交付 (reject threshold) |
| 7 | Hard-drop deduction event | `EcoColumnCarriable` HardImpact | Fires with the GlassThud audio at the same tick as the −3% |
| 8 | Power restored | `PowerGateBreaker.PowerRestored` | Server-auth; fires once — shutters drop, stair lights on |
| 9 | Breaker hold progress (3 s) | `PowerGateBreaker.RestoreProgress` | Hold-to-interact; release resets |
| 10 | Heavy carry state | `CarrySystem.IsCarryingHeavy` | 0.55× speed, hotbar locked while carrying |
| 11 | Downed / being revived state | `PlayerHealth.IsDowned` | Per-player; fires on HP = 0 |
| 12 | Crosshair / interaction prompt | `MvpHud` + `IInteractable` hints | Center dot; turns sodium-amber on interactable (PM 2026-06-10: "冷空间一个暖点") |
| 13 | Damage flash | `PlayerHealth` | Full-screen red tint on hit |
| 14 | Floor / orientation | Environment | Implicit — stair geometry, light anchors, BEACON sightline |

*Out of scope for this spec*: the van boarding / depart-lever / partial-return confirm
panels (`MissionVanExitPoint`) belong to the **上车+加载** spec (queue item 4); the
settlement breakdown belongs to the **结算** spec (queue item 3).

### Categorization

**Design direction**: Minimal persistent HUD — only survival-critical inputs the player cannot perceive without UI. Everything else is contextual (appears on trigger, fades) or diegetic.

| Category | Element | Rationale |
|---|---|---|
| **Must Show** (always visible) | HP bar | Cannot judge remaining survivability without it |
| **Must Show** | Stamina bar | Cannot judge sprint availability without it |
| **Must Show** | 5-slot hotbar | Inventory state is a continuous tactical input |
| **Must Show** | Flashlight battery (in slot) | Darkness makes battery state safety-critical |
| **Must Show** | Crosshair / interaction prompt | Spatial anchor; interaction requires feedback |
| **Must Show** | Damage flash | Immediate hit confirmation — no other channel |
| **Must Show** (工单行) | Mission objective line | Persistent one-line work order (PM 2026-06-11): multi-step objective with no quest log — fading it would strand the player. Row refreshes + brief highlight on state change |
| **Contextual — persistent once triggered** | 密封完整度 | Hidden during InProgress; from ObjectiveSecured (first lift) it stays on the work order until a terminal state. Earned-precision rule: the number is not the player's business until the column is lifted |
| **Contextual** (triggers, then fades) | Hard-drop deduction toast | "磕碰 −3%" beside the completeness line, ~2 s |
| **Contextual** | Power restored notice | One-time notification: 供电已恢复 |
| **Contextual** | Breaker hold progress | Visible only during the 3 s hold, at the crosshair |
| **Contextual** | Downed / revive prompt | Active while downed; clears on revive |
| **Contextual** | Heavy carry badge | Appears on pickup (≈4 s) then fades; hotbar locked-state visual persists for the whole carry |
| **Hidden / Environmental** | Floor / orientation | Architecture, light anchors, and the BEACON sightline do this job |

---

## Layout Zones

### Zone Map

```
┌─────────────────────────────────────────────┐
│ ZONE A — 现场工单 (top-left)                 │
│ 工单行1: 目标（常驻）                         │
│ 工单行2: 密封完整度（取柱后常驻）             │
│ 通知行: 供电恢复 / 磕碰扣减 / 重物标记        │
│        （仅触发时显示，淡入淡出）             │
│                    +                         │  ← ZONE B: 准星（中心固定；
│                  [▓▓░░]                      │     按住交互时下方出现进度细条
│                                              │
│                                              │
│          ┌──┬──┬──┬──┬──┐                  │
│          │  │  │  │  │  │                   │  ← ZONE C: 热栏（底部居中）
│          └──┴──┴──┴──┴──┘                  │
│ ┌体检/VITALS┐                              │
│ │HP  78/100 │                              │  ← ZONE D: 体检监视器（左下角常驻，
│ │STA   60%  │                              │     带数字；PM 2026-06-13 由热栏上方移来）
│ └───────────┘                              │
└─────────────────────────────────────────────┘
```

### Zone Definitions

| Zone | 位置 | 元素 | 显示规则 |
|---|---|---|---|
| **A — 现场工单** | 左上，屏幕边距 24px | 工单行1=任务目标（**常驻**，状态切换整行刷新+0.6s 高亮）；工单行2=密封完整度（**取柱后常驻**至终态）；其下通知行=供电恢复、磕碰扣减、重物携带标记 | 工单行常驻；通知行触发时淡入（0.2s），保持 4s（通知类）或持续到状态结束（警告类），淡出（0.5s） |
| **B — Crosshair** | 屏幕正中 | 准星（默认中性纸灰；交互目标时变**钠灯琥珀**——"冷空间一个暖点"，PM 2026-06-10）；按住式交互（电闸 3s）时准星下方出现水平进度细条 | 常驻；进度条仅按住期间显示 |
| **C — Hotbar** | 底部居中，距底边 24px | 5格热栏（图标、数量、槽号）；手电格内含电量细条（健康=钠灯琥珀，临界=印章红） | 常驻；重物搬运中整排切「双手占用」压暗态 |
| **D — 体检监视器 (VITALS)** | 屏幕**左下角**，边距 24px（PM 2026-06-13 改：原热栏正上方细条太不显眼，改为致命公司式常驻监视器） | 混凝土黑卡：标题「体检/VITALS」+ HP 行（数字 `78 / 100` + 条）+ 体力 STA 行（数字 `60%` 或 `WINDED` + 条） | 常驻；纸色上沿；HP≤25% 或体力耗尽时该行填充切印章红 |

### 体检监视器规则（VITALS — 2026-06-13 改版）

- 位置：屏幕**左下角**，距左/下边各 24px。一块约 216×98px 的混凝土黑卡（`panelTexture` / ConcreteBlack），纸色 2px 上沿。
- 内含两行：**HP 行**（左标签 `HP`，右数字 `当前 / 100`，下方填充条）与**体力行**（左标签 `体力 STA`，右数字 `xx%`，耗尽时显 `WINDED`，下方填充条）。
- **带数字**：与原「无数字」克制版的区别即来自 PM 2026-06-13——任务里要一眼看清生命/体力，沿用致命公司的常驻可读监视器。
- HP 健康填充=骨色（bone），≤25% 切**印章红 `#C23A2B`**（数字同步转 warning 红）。
- 体力健康填充=钠灯琥珀，≤24% 或耗尽切红橙；耗尽时数字显 `WINDED`。
- 颜色不单独承载：红档同时伴随数字下降与 `WINDED` 字样（双通道，无障碍基线）。

### Damage Flash

全屏叠层，不属于任何固定区域。红色（#C23B2B，alpha 0.35），受击瞬间满帧，0.3s 淡出。

---

## HUD Elements

色板引用 `design/art/art-bible.md`：印章红 `#C23A2B`、钠灯琥珀（手电/警示暖色）、纸色 `#D6CCAE`、市政青 `#3F5F5C`、墨黑。CRT 绿**不进 HUD**（绿限世界内屏幕/指示灯——见统一语言三类表面）。

### A1 — 工单行·任务目标（Must Show）

- **内容**: `TowerMissionState` 驱动的单行目标文案（5 态各一句，沿用现行文案）。
- **视觉形态**: 墨字单行，无底框或仅极淡纸条底（field-form test: 一行手写在工单上的字）。前缀工单编号样式 `№` 或 `▌` 竖标，不用图标。
- **更新行为**: 事件驱动（状态切换时整行刷新）。
- **动效**: 刷新时整行 0.6s 钠灯琥珀高亮后回墨色；Failed 态切入时一次性印章红高亮。

### A2 — 工单行·密封完整度（Contextual — 取柱后常驻）

- **内容**: `密封完整度: 91%`（`SyncedCompleteness`，P0 格式）。
- **触发**: ObjectiveSecured（首次扛起生态柱）出现，常驻至终态。InProgress 阶段**不显示**（earned-precision rule）。
- **视觉形态**: 墨字，目标行正下方。≥50% 墨色；**<50% 切印章红 + 前缀 `!` + 追加「不可交付」字样**（红+字形双通道，对齐终端 spec 的反白+`!` 警示原则）。
- **动效**: 首次出现淡入 0.2s；每次扣减时数字滚动一档 + 该行 0.4s 红闪。

### A3 — 通知行（Contextual — 触发淡出）

工单行下方的临时通知队列，最多同屏 2 条，新顶旧：

| 通知 | 触发 | 文案 | 停留 |
|---|---|---|---|
| 供电恢复 | `PowerRestored` 置真（每 peer 一次） | `供电已恢复 — 卷帘门已开` | 4s |
| 磕碰扣减 | HardImpact 事件 | `磕碰 −3%` | 2s |
| 重物携带 | 扛起生态柱 | `重物搬运中 — 双手占用，移速受限` | 4s（热栏锁定态持续整个搬运期） |

视觉：墨字，警示类（磕碰）印章红。淡入 0.2s / 淡出 0.5s。

### B1 — 准星（Must Show）

- **默认**: 中性纸灰小点，屏幕正中。
- **可交互目标**: 点变钠灯琥珀 + 下方一行纸色 E 提示文案（来自 `IInteractable` hint，如「扛起生态柱（重物，双手）」「按住恢复供电」）。
- **按住式交互进度**（B2）: 电闸等 hold 类交互进行中，准星正下方出现水平进度细条（墨底+琥珀填充，宽约 64px），松手即消。field-form 语感：表格里被逐渐涂满的一格。

### C1 — 热栏（Must Show）

- 5 格，图标+数量+槽号；选中格框线提亮。
- **手电格**: 格内底部电量细条——健康=钠灯琥珀，临界（≤25%）=印章红。
- **重物锁定态**: 搬运生态柱期间整排压暗 + 盖「双手占用」斜条戳记（印章语法），任何使用输入被拒时戳记 0.3s 抖动一次。

### D1 — 体检监视器 HP / D2 — 体力（Must Show）

- 左下角常驻卡（见「体检监视器规则」），实现于 `MvpHud.DrawVitalsBlock` / `DrawVitalRow`。
- HP 行：`当前 / 100` 数字 + 条；健康=骨色，≤25% 切印章红（数字同步转红）。
- 体力行：`xx%` 数字 + 条；健康=钠灯琥珀，≤24% 或耗尽切红橙并显 `WINDED`。
- **可见性兜底（bug 修复 2026-06-13）**：`FindLocalPlayerHealth` 现与 `FindLocalPlayer`/`FindLocalHotbar` 一致带 `[0]` 兜底——离线/预览走查也能画出 HP（此前血条会静默消失而热栏照常）。

### E1 — 受击红闪（Must Show）

全屏叠层，印章红 alpha 0.35，受击瞬间满帧，0.3s 淡出（已定稿，见 Layout Zones）。

### F1 — 倒地/救援层（Contextual）

- **倒地者视角**: 画面去饱和 + 屏幕中下方墨字「倒地 — 等待同事救援」；被救援时同位置显示救援进度细条（同 B2 语法）。倒地期间 Zone C/D 隐藏（无可操作项），工单行保留。
- **救援者视角**: 对准倒地队友时准星琥珀 + E 提示「救援同事」，按住期间 B2 进度条。

---

## Dynamic Behaviors

HUD 密度不随战斗/探索模式切换（本作没有模式切换）——动态性体现为**登记簿换档（register shift）**：元素平时墨色安静，紧急时切印章红索取一瞥，回落后归位。

| 行为 | 触发 | 变化 | 回落 |
|---|---|---|---|
| HP 紧急换档 | HP ≤25% | HP 条填充 → 印章红 | HP >25% 回墨色系 |
| 完整度红线 | 完整度 <50% | A2 行印章红 + `!` + 「不可交付」 | 不可逆（完整度只降不升） |
| 耐力耗尽 | 冲刺被锁 | 耐力条填充 → 印章红 | 可再冲刺时回落 |
| 重物模式 | 扛起生态柱 | 热栏整排压暗+「双手占用」戳记；A3 通知一次 | 放下/交付后恢复 |
| 倒地模式 | `IsDowned` 置真 | Zone C/D 隐藏，倒地层接管；工单行保留 | 被救起后 0.5s 内恢复全 HUD |
| 状态行刷新 | `SyncedState` 变化 | A1 整行刷新 + 0.6s 琥珀高亮（Failed 红高亮） | 自动 |

**联机/离线一致性**: PreviewWalker 离线走查模式下 HUD 元素与联机版相同，仅网络专属数据（队友相关）缺省；任何元素不得假设 `TowerMissionManager` 联机在场（离线 fallback 已在管理器内）。

**同屏上限**: Zone A 通知行最多 2 条（新顶旧）；任何时刻全屏常驻元素 ≤ 7 个（工单2 + 准星 + 热栏 + HP + 耐力 + 受击层）。

---

## Platform & Input Variants

- **目标平台**: PC (Windows) 单一平台；键鼠为主输入（`technical-preferences.md`），手柄 TBD —— 本 spec 不为手柄设计变体，手柄立项时需补：热栏选择的肩键循环方案 + 提示文案的按键图标替换层。
- **基准分辨率**: 1920×1080 / 16:9。所有 Zone 以锚点布局：A=左上角锚（边距 24px）、B=屏幕中心、C/D=底边中心锚（边距 24px）。
- **超宽屏 (21:9+)**: Zone 锚点保持——A 不漂向更远的左上，C/D 始终底部居中；不在两翼新增任何元素。
- **低分辨率下限 (1280×720)**: 工单行墨字最小 14px 等效字号；热栏格不缩，必要时 C/D 整体等比 0.85×。
- **渲染栈注意**: 全局 renderScale 0.5 + Point 上采样（LC retro stack）**只作用于 3D 画面**；HUD 必须画在上采样之后的全分辨率层（uGUI overlay / IMGUI 天然如此），文字不得被像素化压糊。lo-fi 颗粒感来自字体与构图，不来自降采样。
- **实施备注**: 现实现为 IMGUI（`MvpHud`）；统一实施批次将迁移到与「盖章公文卡」组件库同源的 uGUI/TMP。本 spec 定义视觉与行为，不锁实现技术。

---

## Accessibility

> 项目尚无 `design/ux/accessibility-requirements.md`（无障碍层级未定）。以下为本 HUD 自带的基线承诺，层级文档落地后按需上调。

- **颜色不单独承载信息**: 所有印章红警示均配第二通道——完整度红线带 `!`+「不可交付」字样；HP/耐力红档伴随条长本身（位置+长度可读）；磕碰扣减带文字。红绿色弱玩家不丢失任何警示。
- **文字尺寸**: 工单行/提示文案 1080p 下 ≥16px、720p 下 ≥14px 等效；不用纯大写英文段落。
- **受击红闪与光敏**: 全屏红闪 0.3s 单次、非频闪；设置页（队列第 7 屏）应含「减弱屏闪」开关——开启时红闪改为屏幕四边 8px 红框脉冲。挂入设置 spec 依赖。
- **键鼠全覆盖**: HUD 本体无需指针交互（全部由世界内交互驱动），无键盘陷阱。
- **音频冗余**: 关键状态已有声音通道（电闸滋滋/卷帘砸开/玻璃鸣/落章）；HUD 与声音互为冗余而非互斥——听障玩家仅凭 HUD 可完整获知任务状态（A1/A2/A3 覆盖全部声音事件对应的状态变化）。
- **动效**: 无持续运动元素；高亮/淡入淡出均 ≤0.6s，无视差或屏幕摇晃，无需 reduced-motion 变体（受击红闪除外，见上）。

---

## Open Questions

1. **无障碍层级未定** — 建议以 WCAG-AA 对比度为基线建 `design/ux/accessibility-requirements.md`；本 spec 的墨字/纸色组合需实测对比度。
2. **玩家旅程图缺失** — `design/player-journey.md` 不存在；本 spec 对玩家到场情绪的假设（黑暗压力、协作分工）未经旅程图校验。模板在 `.claude/docs/templates/player-journey.md`。
3. **印章红色值分裂** — 本文件历史段落用 `#C23B2B`，art-bible/V8 材质用 `#C23A2B`。差一字符，应统一为 art-bible 的 `#C23A2B`（实施时以 `BlackCommissionUiTheme` 单一来源为准）。
4. **怪物警示语法预留** — 怪物已删；下一只怪定稿时需补：追击警报的工单语法（建议沿用 A3 通知行+印章红+独立声音通道），以及「Hidden/Audio-primary」分类是否回归。
5. **工单唤回交互** — 目标行已常驻故暂无需求；若未来信息量增长（多目标/子任务），考虑按住 Tab 展开完整工单纸（盖章公文卡语法）。
6. **手表商品去留** — 时钟机制已删但「廉价工时表」仍在商店出售（买了无效果）。需经济 GDD + 采购目录页签同步处理：下架或保留为彩蛋。归口办公电脑终端 spec 的采购目录部分。
7. **减弱屏闪开关** — 依赖设置 spec（队列第 7 屏）落地。
8. **交互模式库缺失** — `design/ux/interaction-patterns.md` 尚未建库；本 spec 新增 3 个待入库模式：**工单行（field work-order line）**、**准星下按住进度条（hold-progress strip）**、**印章红换档（stamp-red register shift）**。加上终端 spec 的 3 个（CRT 状态条/页签/反白警示块），建库时共 6 个种子模式。
