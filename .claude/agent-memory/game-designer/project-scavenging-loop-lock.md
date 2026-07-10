---
name: project-scavenging-loop-lock
description: PM-locked decisions on the scavenging core loop — removed systems, money-only economy, and settlement-as-satire — from 2026-06-17 synthesis session.
metadata:
  type: project
---

PM decisions locked 2026-06-17 (Yan Dai, final). These are hard constraints — do not re-import:

**Removed systems (ZERO presence anywhere in design or code):**
- Reputation / rep delta
- OfficeLevel (1–8)
- Takeover-pressure counter (0–100) and its FSM
- XP

**Economy is money-only.** Settlement produces one number: an integer deposited to `CompanyState.funds`. The `OutcomeKind` (Success / Partial / Failure) drives only the settlement card label and `failurePayoutRate` multiplier — nothing else.

**Scavenging is the universal core.** Every mission type (Free Salvage, Commissioned Job, Black Commission) runs the same scavenge → deposit → depart → settle loop. No mandatory single objective. Commissioned target is an optional bonus overlay that never gates completion.

**Eco-column (生态柱) designated objective: retired.** Cut from the tower map and from all future design docs.

**Settlement reveal is Core, not Polish.** Per-item reveal + client usage note + one-dispute step = Pillar 3 mechanical expression. Not a Presentation-layer deferral.

**Monthly safety cost sink is the highest-priority economy gap.** 40G/month Stage 1 → 120G/month Stage 4 (world-background-2098.md). Without it, the "broke office" pressure never bites. Must be resolved before economy GDD rewrite.

**Takeover survives only as narrative flavor** — letters or NPC visitors when funds reach zero. Never a UI bar. Never tied to a settlement-driven score.

**Why:** PM direction session 2026-06-17; prior draft (scavenging-core-loop.md) had re-imported all removed systems in §3.7 and §4.

**How to apply:** Any design touching economy, settlement, or mission state must have zero reference to rep/XP/pressure/OfficeLevel. If a cross-review finds such a reference, flag it as a blocking issue.

See also: [[project-style-lock]], [[project-tower-spatial-language]]
