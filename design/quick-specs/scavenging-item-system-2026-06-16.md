# Quick Design Spec: Scavenging Item System

**Type**: New Small System
**Scope**: Core mission loop — item pickup, hidden value, item condition,
van weight limit, settlement reveal, and one-dispute bargaining. Does NOT
define item art, map spawn placement, or individual client content (those
are authored separately).
**Date**: 2026-06-16 (updated 2026-06-18: item condition system, revised
negotiation, A镜 upgrade)
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

### 5. Client Category Preference (Commissioned / Black Jobs)
A Commissioned or Black Commission client favours 1–2 item **categories**,
hinted at — never named — in the commission text. Items of a favoured category
settle at `× clientPreferenceMultiplier` (default 1.3); every other item, and
all Free Salvage, settles at market rate. There is **no designated target
item** and nothing gates completion (per `scavenging-core-loop.md` §0 D-G).
Reading the commission well and hauling the favoured categories should feel
like a good run; ignoring it just means market rate.

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
| 污染 | Matched client type | Concede or reject |
| 污染 | Mismatched client | Counter-reduce: −20–30% (locked) |

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

## Item Categories (12 minimum for launch)

Each category has:
- A visual silhouette readable at 4m in the lo-fi art style
- A color accent matching the BC palette (no price-coded colors — all items
  use the same neutral aged-earth palette; color is not a value signal)
- A weight class default (individual items may vary within category)
- A pool of authored settlement notes per client type

| Category | Default Weight | Notes |
|----------|---------------|-------|
| Personal correspondence | Light | Letters, postcards, handwritten notes |
| Family photography | Light | Photos, printed images, home media |
| Children's artifacts | Light | Drawings, schoolwork, small toys |
| Medical / pharmaceutical | Light–Medium | Prescription bottles, files, equipment |
| Civic documents | Light | Debt notices, permits, official stamps |
| Cultural publications | Light–Medium | Books, music, printed media |
| Personal clothing / effects | Light–Medium | Clothing, bags, accessories, ID cards |
| Household technology | Medium | Broken electronics, appliances, terminals |
| Professional tools | Medium | Work equipment, instruments |
| Native plant specimens | Medium | Contained flora, soil cores |
| Religious / ceremonial | Light–Medium | Household altars, ritual objects |
| Residential fixtures | Heavy | Furniture sections, fittings, signage |

## Tuning Knobs

| Knob | Default | Range | Notes |
|------|---------|-------|-------|
| `vanWeightCapacity` | 12 units | 8–20 | Per team, not per player |
| `clientPreferenceMultiplier` | 1.3× | 1.1–1.6 | Applied to favoured-category items on Commissioned/Black runs |
| `disputeConcedeRate` | ~40% | 30–60% | Authored per item/client combo, not random |
| `itemsPerMapInstance` | 10–14 | 8–18 | Spawned items per run |
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
| `MissionRewardCalculator.cs` | Replace binary success/partial with per-item sum | New formula: Σ(baseValue × condition × clientPreferenceMultiplier) — see `scavenging-core-loop.md` §4 |
| `OfficeTaskDefinition` | Add: item category hints, client type, generational data | Extend ScriptableObject |
| `VanTransitOverlay.cs` | Add weight display to van overlay (ticket strip) | Show remaining capacity |
| `CarrySystem.cs` | Enforce van weight limit on load | Check capacity before allowing cargo-zone deposit |
| `SettlementCardOverlay.cs` | Per-item reveal sequence, dispute button | Major UI update |
| `design/gdd/office-economy-progression.md` | Settlement formula section needs rewrite | Update after implementation |

## Acceptance Criteria

- [ ] Items show name, weight class, and category only — no price during mission
- [ ] Van weight display shows remaining capacity; full van rejects new items
- [ ] Settlement reveals each item's price and client note in authored sequence
- [ ] Favoured-category items pay `× clientPreferenceMultiplier` on Commissioned/Black runs; Free Salvage pays market rate
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
