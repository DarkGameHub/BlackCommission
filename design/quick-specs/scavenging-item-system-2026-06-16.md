# Quick Design Spec: Scavenging Item System

**Type**: New Small System
**Scope**: Core mission loop — item pickup, hidden value, item condition,
van weight limit, settlement reveal, and one-dispute bargaining. Does NOT
define item art, map spawn placement, or individual client content (those
are authored separately).
**Date**: 2026-06-16 (updated 2026-06-18: item condition system, revised
negotiation, A镜 upgrade; **updated 2026-06-26: 双层架构 Salvage/Relic + 客户偏好→大类**, see `scavenging-two-tier-revision-2026-06-26.md`)
**Estimated Implementation**: 1–2 weeks (phased)

## Overview

Players enter a mission site and pick up items they judge to be valuable based
on the client's commission text. Item values are not shown during the mission.
At settlement, each item's price and the client's stated intended use are
revealed together. The van has a shared weight limit — the team cannot take
everything and must decide what stays.

This loop creates three distinct decision moments:
1. **Pre-mission**: read the client commission, form a theory of what they value
2. **On-site**: apply that theory under time pressure and weight constraints
3. **Settlement**: discover how right or wrong the theory was

## Core Rules

### 1. Items Have No Visible Value During Missions
Items show only: name, weight class (light / medium / heavy), condition
(完好 / 一般 / 受损 / 污染), and visual category (document, specimen,
personal effect, technology, etc.).
No price tag, no scanner value, no indication of client preference matching.

### 2. Commission Text Is the Only Signal
The client commission displayed in the CRT terminal before departure contains
the player's only legitimate clues. The text is authored to be suggestive but
not explicit — players must interpret, not calculate.

Corporate commissions: category-level hints ("prefers residential items over
industrial"), written in institutional register.
Personal commissions: emotional and contextual hints ("her mother always
talked about…"), written as a person, not a brief.

### 3. Van Weight Limit
The van has a shared team weight capacity: **[tunable, default: 12 units]**.
Weight classes: Light = 1 unit, Medium = 2 units, Heavy = 4 units.
Capacity is visible to all players on the van overlay (ticket-strip display
showing remaining slots). When full, no new items can be loaded.

The weight limit is the game's primary tension lever — not a timer, not a
quota. The team must decide what to leave behind.

### 4. Settlement Reveal
After extraction, the settlement screen shows each item in sequence:

```
[Item name]
Weight: light
Condition: good
Price paid: 85G
Client intended use: [authored text]
Client feedback: [authored text, optional]
```

Items are displayed in emotional weight order (authored per run, not sorted by
price). The last item revealed should be the one that lands hardest.

### 5. Client Preference Model — Two-Tier（双层，修订 2026-06-26 PM-locked；详见 `scavenging-two-tier-revision-2026-06-26.md`）

可捡拾物分**两层**，客户偏好对两层作用不同（取代旧的"偏好 1–2 个具体类目"）：

**层一 · 搜刮经济主体（Salvage）** — 视觉化物件（器物 / 家电 / 工具 / 衣物 / 医药 / 宗教器物 / 酒 / 标本 / 家具），看模型可判断价值，扛"重量 vs 价值"取舍。客户偏好作用于 **4 个大类（Material Class）**：**家居烟火 / 劳作器械 / 自然遗存 / 文化信仰**。Commissioned/Black 客户偏好 1–2 个大类（委托文本暗示、不点名），其下物件结算 `× materialClassPreferenceMultiplier`（默认 1.3）；其余物件与 *所有* Free Salvage = 市场价。

**层二 · 情感锚点（Relic）** — 私人遗物（信 / 照片 / 儿童画 / 日记 / 公文），**不走大类偏好**。每图手工 3–6 件，对应"谁的命运"。对**匹配的个人客户**（怀旧移民一代 / 二代）`× relicEmotionalMultiplier`（1.5–3.0，高方差）+ 特殊结算文本；对**不匹配的机构 / 三代客户** `× relicMismatchMultiplier`（≈0.8）+ 归档冷淡腔。**反差即讽刺**：同一封信，对怀念地球的一代是无价，对收藏地球的三代只是一个编号。

仍**无指定目标物**（`scavenging-core-loop.md` §0 D-G 不变）：层二遗物不是"必须找到否则失败"的目标，是高情感稀有点缀。检视系统（举起看痕迹，见 Task #2）是其主要呈现，**不改价值**。读懂委托、专挑偏好大类 + 偶遇匹配遗物 = 打得好。

### 6. Item Condition (3 states — PM locked 2026-06-18, no 污染)

Every item has a condition state shown on pickup alongside weight class. There are **three**
states (the earlier 污染/Contaminated state was cut 2026-06-18 — degradation bottoms out at 受损):

| State | Display | Settlement ×mult | Meaning |
|---|---|---|---|
| 完好 | Intact | ×1.0 | Sealed, clean, undamaged |
| 一般 | Worn | ×0.7 | Light dust, age, minor wear |
| 受损 | Damaged | ×0.4 | Physical/water damage, structural compromise — the worst state |

**Initial condition is determined by environment:**
- Sealed storage rooms, high shelves → 完好
- Open corridors, standard rooms → 一般
- Flooded/damaged areas, and deep infection zones near monster nests → 受损

**Condition degrades one step through player behaviour (受损 is the floor):**
- **A valuable item (baseValue ≥ `valuableConditionThreshold`) released "hard" outside the van**
  — dropped while **downed**, **knocked out of your hands by a monster hit**, dropped **from
  height**, or **thrown** to a teammate. Depositing into the van cargo zone, and gentle deliberate
  set-downs, do **not** degrade. (PM 2026-06-18 — only the van is a safe place to let go of a
  valuable in a panic; cheap items are unaffected.)
- Player **Infection Exposure > 70** when picking up an item in the deep zone → −1 step
  (see `danger-infection-system-2026-06-18.md`).

Condition multiplies the base price at settlement (`cond_i` in `scavenging-core-loop.md` §4:
完好 ×1.0 / 一般 ×0.7 / 受损 ×0.4) and shifts dispute outcomes (Rule 7).

**A镜 upgrade (late-game shop item):**
By default, condition is shown as one of the three labels only (完好/一般/受损).
Players can purchase an **A镜 (Analysis Lens)** from the HQ shop in later
license stages. When equipped, item condition shows as a **precise percentage
(0–100%)** within its bracket, giving more granular negotiation information.
Example: without A镜 → `状态：受损`; with A镜 → `状态：受损 (34%)`.
This is a meaningful late-game upgrade that rewards experienced players who
know how to use the extra precision in disputes.

### 7. One Dispute Per Settlement (revised 2026-06-18)

After the full settlement reveal, players may file **one dispute** on any
single item. The outcome is determined by item condition and commission match —
not random. The result is **locked**: once the client responds, the new price
(higher or lower) is final. Players cannot revert to the original offer.

**Outcome table:**

| Item condition | Commission match | Client response |
|---|---|---|
| 完好 or 一般 | High match | Concede: +15–30% |
| 完好 or 一般 | Low match | Reject (price unchanged) |
| 受损 | Any | Counter-reduce: −15–25% (locked) |

The counter-reduce is never random — the client always provides an authored
reason that is polite, institutional, and morally bankrupt. Example:

> `估价修订：120G → 85G`
> `理由：样品表面附有地表污染残留，影响展示价值。清洁成本已从价款中预扣。`
> `——地球遗产征集事务所 · 自动估价系统`

This expresses Martian client power: they always find a legitimate-sounding
excuse to pay less. The satire is in the tone, not the number.

**Co-op dispute protocol:** all players see the item list simultaneously.
The team discusses which item to dispute before anyone presses. One press
commits the whole team. This makes the dispute a genuine collective decision,
not an individual reflex.

The dispute response is always written in the client's register (institutional
or personal). The tone is the content.

## Item Weight Classes

| Class | Units | Examples |
|-------|-------|---------|
| Light | 1 | Documents, photos, small personal effects, medicine bottles, books |
| Medium | 2 | Electronics, plant specimens in containers, tools, ceramic objects |
| Heavy | 4 | Furniture pieces, large equipment, sealed specimen canisters |

Heavy items require two-hand carry (existing mechanic). Medium items occupy
one hand. Light items can be pocketed (up to 2 in one inventory slot).

## Item Categories — Two-Tier（双层归属，修订 2026-06-26）

每个类目仍有：4m 可读剪影 / 统一陈旧大地色（颜色不作价值信号）/ 默认载重 / 每客户类型的结算文本池。**新增**：每个类目归入**层一（Salvage）或层二（Relic）**；层一类目再归入一个**大类（Material Class）**。

**层一 · 搜刮经济主体（走大类偏好 ×`materialClassPreferenceMultiplier`）**

| Category | Material Class 大类 | Default Weight | Notes |
|---|---|---|---|
| 器物 / 摆件 Decorative objects | 家居烟火 Domestic | Medium | 陶瓷、钟表、乐器、装饰件 |
| 家用科技 Household technology | 家居烟火 Domestic | Medium | 坏电器、家电、终端 |
| 个人衣物 / 随身物 Personal clothing/effects | 家居烟火 Domestic | Light–Medium | 衣物、包、配饰、证件 |
| 住宅家具 / 固定件 Residential fixtures | 家居烟火 Domestic | Heavy | 家具段、灯具、招牌 |
| 医药 Medical / pharmaceutical | 家居烟火 Domestic | Light–Medium | 处方瓶、器械（家庭药箱）【D3：归家居】 |
| 专业工具 Professional tools | 劳作器械 Labour | Medium | 工作器械、仪器 |
| 本土植物标本 Native plant specimens | 自然遗存 Natural | Medium | 容器装植物、土芯、污染样本 |
| 宗教 / 礼仪器物 Religious/ceremonial | 文化信仰 Culture | Light–Medium | 家用神龛、仪式物 |
| 文化出版物 Cultural publications | 文化信仰 Culture | Light–Medium | 书、唱片、印刷品（题字本 / 家庭录像 → 特例可作层二） |
| 酒 / 奢侈消费品 Liquor / luxury goods | 文化信仰 Culture | Medium | 酒瓶、奢侈品（会所图） |

**层二 · 情感锚点（不走大类；匹配客户 ×`relicEmotionalMultiplier`，否则 ×`relicMismatchMultiplier`）**

| Category | Default Weight | Notes（含检视细节） |
|---|---|---|
| 私人信件 Personal correspondence | Light | 信、明信片、手写便条（信封收件人名字 = 检视细节） |
| 家庭照片 Family photography | Light | 照片、相册（照片里模糊的脸） |
| 儿童物品 Children's artifacts | Light | 蜡笔画、作业本、磨损的玩具 |
| 日记 / 家庭影像 Diaries / home media | Light | 翻开停在某一页 |
| 公文 / 制度物 Civic documents | Light | 欠债通知、公章、委托表（也是 BC 身份注入 art-bible §6） |

## Tuning Knobs

| Knob | Default | Range | Notes |
|------|---------|-------|-------|
| `vanWeightCapacity` | 12 units | 8–20 | Per team, not per player |
| `materialClassPreferenceMultiplier`（原 `clientPreferenceMultiplier`） | 1.3× | 1.1–1.6 | 层一物件命中客户偏好**大类**时（2026-06-26 改：作用于大类，非具体类目） |
| `relicEmotionalMultiplier`（新 2026-06-26） | 2.0× | 1.5–3.0 | 层二遗物对**匹配个人客户**的情感加价（高方差） |
| `relicMismatchMultiplier`（新 2026-06-26） | 0.8× | 0.6–1.0 | 层二遗物对**不匹配机构 / 三代客户**的冷淡折价 |
| `relicsPerMap`（新 2026-06-26） | 4 | 3–6 | 每图手工层二遗物数量 |
| `disputeConcedeRate` | ~40% | 30–60% | Authored per item/client combo, not random |
| `itemsPerMapInstance` | 10–14 | 8–18 | 层一物件每局生成数（层二另由 `relicsPerMap` 控制） |
| `lightItemPocketSlots` | 2 | 1–3 | Per player pocket capacity |
| `valuableConditionThreshold` | baseValue ≥ 80 | 40–150 | "Valuable" cutoff for hard-drop condition loss (Rule 6) |

All values in `Assets/Resources/Config/ScavengingConfig.asset`.

## Settlement Screen Architecture

Three display states (see also: `design/ux/settlement.md`):

**Standard** (Stages 1–4): item | weight | condition | price | client note

**Contextual** (unlocked after completing 5+ runs of a stage): adds a faint
secondary line showing which room the item was found in. The room name is
the map's authored name, not a generated label.

**Terminal** (Stage 4 final run only): item | what it was | what it became.
Client usage notes are replaced with a brief factual statement of the object's
original context. No editorial. No client voice. Just the object and its
trajectory.

## Affected Systems

| System | Impact | Action Required |
|--------|--------|----------------|
| `OfficeComputer.cs` | Commission display must show client profile + text | Show client type, background, and commission text |
| `MissionRewardCalculator.cs` / `ScavengeSettlementCalculator.cs` | 双层求和（2026-06-26） | `Σ层一(base×cond×大类倍率) + Σ层二(base×cond×情感倍率)` — 详见 `scavenging-two-tier-revision-2026-06-26.md` §7 |
| `ScavengeItemDefinition` | 双层数据（2026-06-26） | 加 `tier`(Salvage/Relic)；层一加 `materialClass`；层二加 `targetPersonId` + 可选 `inspectDetail` |
| `OfficeTaskDefinition` | 客户偏好改大类（2026-06-26） | `favouredCategories` → `favouredMaterialClasses`(1–2) + 个人客户 relic 情感匹配；client type / generational data |
| `InspectController`（新，Task #2） | 举起检视 = 层二主要呈现 | 第一人称举起旋转 + 联机同步；不改价值 |
| `VanTransitOverlay.cs` | Add weight display to van overlay (ticket strip) | Show remaining capacity |
| `CarrySystem.cs` | Enforce van weight limit on load | Check capacity before allowing cargo-zone deposit |
| `SettlementCardOverlay.cs` | Per-item reveal sequence, dispute button | Major UI update |
| `design/gdd/office-economy-progression.md` | Settlement formula section needs rewrite | Update after implementation |

## Acceptance Criteria

- [ ] Items show name, weight class, and category only — no price during mission
- [ ] Van weight display shows remaining capacity; full van rejects new items
- [ ] Settlement reveals each item's price and client note in authored sequence
- [ ] 层一物件命中客户偏好**大类**时 `× materialClassPreferenceMultiplier`；其余物件与 Free Salvage = 市场价
- [ ] 层二遗物：匹配个人客户 `× relicEmotionalMultiplier` + 特殊文本；不匹配机构 / 三代 `× relicMismatchMultiplier` + 归档冷淡腔
- [ ] 检视：捡起可举起旋转看细节（信封名字 / 照片的脸 / 玩具磨损），不改价值
- [ ] Dispute button appears after full reveal; one use per settlement
- [ ] Dispute response is authored (not random), written in client register
- [ ] Free Salvage runs show approximate market rate per category (not per item)
- [ ] No regression: van departure, scene load, and HQ return flow unchanged

## Systems Index

Add to `design/systems-index.md` under **Mission** layer, Priority Tier 1
(blocks full mission loop implementation).

Depends on: `CompanyState` (funds), `CarrySystem` (weight), `MvpPendingReward`
(settlement trigger), `OfficeTaskDefinition` (commission data).
Produces: per-run settlement payload → `CompanyState`.
