# Quick Design Spec: Scavenging Item System

**Type**: New Small System
**Scope**: Core mission loop — item pickup, hidden value, van weight limit,
settlement reveal, and one-dispute bargaining. Does NOT define item art,
map spawn placement, or individual client content (those are authored separately).
**Date**: 2026-06-16
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
Items show only: name, weight class (light / medium / heavy), and visual
category (document, specimen, personal effect, technology, etc.).
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

### 5. Commissioned Target (Optional Overlay)
A commission may designate one specific item for bonus pay. If found and
extracted, bonus is applied at settlement. If not found, no penalty — the
run settles on whatever was brought back. The bonus is 1.3–1.5× the item's
base value, never more. Finding the commissioned target should feel like a
good run, not finding it should not feel like failure.

### 6. One Dispute Per Settlement
After the full settlement reveal, players may file one dispute on any single
item they believe was undervalued. The client responds in writing (authored).
Possible outcomes:
- **Concede**: price adjusts upward (+15–30% of that item's value)
- **Reject with clause**: price unchanged, response cites contract language
- **Counter-reduce**: client invokes a previously unmentioned condition,
  reducing the price further

The outcome is not random — it is authored per item type and client type.
Players who dispute strategically (items with strong emotional or cultural
value to that specific client) fare better. Players who dispute reflexively
learn the system's limits.

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
| `commissionedBonusMultiplier` | 1.4× | 1.2–1.8 | Applied to commissioned item only |
| `disputeConcedeRate` | ~40% | 30–60% | Authored per item/client combo, not random |
| `itemsPerMapInstance` | 10–14 | 8–18 | Spawned items per run |
| `lightItemPocketSlots` | 2 | 1–3 | Per player pocket capacity |

All values in `Assets/Resources/Config/ScavengingConfig.asset`.

## Settlement Screen Architecture

Three display states (see also: `design/ux/settlement.md`):

**Standard** (Stages 1–4): item | weight | condition | price | client note

**Contextual** (unlocked after completing 5+ runs of a stage): adds a faint
secondary line showing which room the item was found in. The room name is
the map's authored name, not a generated label.

**Terminal** (Stage 5 final run only): item | what it was | what it became.
Client usage notes are replaced with a brief factual statement of the object's
original context. No editorial. No client voice. Just the object and its
trajectory.

## Affected Systems

| System | Impact | Action Required |
|--------|--------|----------------|
| `OfficeComputer.cs` | Commission display must show client profile + text | Show client type, background, and commission text |
| `MissionRewardCalculator.cs` | Replace binary success/partial with per-item sum | New formula: Σ(item base values) × condition modifier + commissioned bonus |
| `OfficeTaskDefinition` | Add: item category hints, client type, generational data | Extend ScriptableObject |
| `VanTransitOverlay.cs` | Add weight display to van overlay (ticket strip) | Show remaining capacity |
| `CarrySystem.cs` | Enforce van weight limit on load | Check capacity before allowing cargo-zone deposit |
| `SettlementCardOverlay.cs` | Per-item reveal sequence, dispute button | Major UI update |
| `design/gdd/office-economy-progression.md` | Settlement formula section needs rewrite | Update after implementation |

## Acceptance Criteria

- [ ] Items show name, weight class, and category only — no price during mission
- [ ] Van weight display shows remaining capacity; full van rejects new items
- [ ] Settlement reveals each item's price and client note in authored sequence
- [ ] Commissioned target bonus applies if found; no penalty if not found
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
