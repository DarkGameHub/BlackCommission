# Cross-GDD Review Report

**Date:** 2026-06-14
**GDDs reviewed:** 3 — `level-map-generation.md`, `mission-state-machine.md`, `office-economy-progression.md`
**Registry:** `design/registry/entities.yaml` (loaded as baseline)
**Skill:** `/review-all-gdds` (full mode) · **Verdict: CONCERNS**

---

## What's solid (do not touch)

- **Dependency graph between the 3 GDDs is clean & bidirectional** — Level → Mission → Economy
  all reciprocate correctly (the Economy GDD even self-audits the link to Mission).
- **No competing progression loops, no dominant strategy.** Money is the single player-facing
  currency; partial return (60G) is 5× weaker than full success (300G), so it reads as a
  survival valve, not an optimal farm.
- **Pillar alignment is exemplary.** Every system maps to a pillar, and the Economy GDD
  *honors the anti-pillar* ("no 0–100 bar") by surfacing takeover pressure as letters/visitors.
  Player fantasies ("I know this place… mostly" / "do we leave now?" / "permanently broke")
  reinforce one coherent identity: bottom-rung contractor under pressure.

---

## Consistency issues

### High priority

**C1 — Economy GDD's "as-built" provenance cites a deleted file.**
`office-economy-progression.md` Formulas claims values are *"as-built from
`Assets/_Project/Scripts/Mission/LostItemMissionManager.cs`"* — **that script no longer exists**
(replaced by `TowerMissionManager` + `Core/TowerMissionLogic` + `Core/MissionRewardCalculator`).
The reward values (300/60/20/90) must be re-verified against `MissionRewardCalculator.cs`, and
the provenance line updated.

**C2 — Mission State Machine GDD is incomplete.** `Acceptance Criteria`, `Visual/Audio`, `UI`,
and `Open Questions` are all `[To be designed]`. It is a **Core/MVP** system; without Acceptance
Criteria it cannot generate test stories. Largest completeness hole across the three GDDs.

### Warnings

**C3 — Stale doc pointers (post-supersession).** `mission-state-machine.md` and
`office-economy-progression.md` both list `docs/mvp-core-loop.md` under "Related," and the
Economy GDD repeatedly frames the partial-% as a conflict with `docs/design-decisions.md` —
**both now superseded (2026-06-14)**. The partial-% is no longer a doc-vs-doc conflict; it is
just an open balance question. The registry's `referenced_by` for `company_start_state` and
`tutorial_acquisition_cost` also still point at the dead `docs/mvp-core-loop.md`.

**C4 — Clock/overtime ownership ambiguous.** Mission GDD attributes the mission clock + overtime
penalties to a **"Hazard/Escalation"** system (no GDD, doesn't exist); Economy attributes them to
**`MvpMissionClock`** (real, in `Office/Core/`). Same knob, two named owners.

**C5 — School-era artifacts left in tower GDDs.** Mission GDD's optional-objective example is
still "photograph the overdue ledger"; Economy still uses `CompletedLostItemJobs` and a "wrong
homework" penalty. The code field names are as-built, but the fiction is school-era — terminology
drift from the migration.

---

## Game-design (holism) issues

**D1 — The "permanently broke" fantasy has no mechanical enforcement. (Important.)**
The Economy's pillar is "every job is a stay of execution." But mechanically: gear is cheap and
one-time, the tutorial acquisition is one-time, and **`Debt` is an explicit static backdrop that
does NOT drain funds**. Once a competent team clears the −300 opening, **money has no recurring
sink → it accumulates → the broke fantasy collapses mid-game** (classic Source ≫ Sink). The
opening squeeze works; the *sustained* squeeze does not exist. Needs a recurring sink (rent that
deducts, consumable burn, upkeep) or an explicit "MVP-only, sink comes post-MVP" note.

**D2 — Reputation isn't clamped (known bug, flagged in the GDD).** `ApplyMissionResult` adds
reputation without clamping to `[−100,100]`. Since reputation gates the "disliked" takeover shield
and the job pool, drift silently corrupts those gates. Elevated because it touches the threat ladder.

**D3 — Takeover threat has nothing to render it.** The Economy correctly emits "send letter/visitor,"
but the **Narrative/Threat system has no GDD and no code** — so the signature "man in a good coat"
beat is currently unbuilt; the pressure ladder computes into the void. (Matches Economy Open Q #4.)

**D4 — Failure-spiral cadence.** Success −25 vs failure +35→+60 pressure; ~2 bad runs from mid can
reach restructure. The soft-reset (→35, not game-over) is good catch-up design, but the GDD's own
tuning note flags "3 fails = restructure (too fast)." Balance-pass watch item.

---

## Cross-system scenarios walked (5)

| Scenario | Systems | Result |
|---|---|---|
| Objective pickup → heavy carry → extract | Mission + Level + Player | ✅ defined |
| Carrier completes on same tick all would be downed | Mission | ✅ defined (complete wins over Failure) |
| Scaffold-drop objective-abandon (BALCONY→F1) | Mission + Level | ✅ defined (drops objective at top) |
| Settlement → pressure → threat | Mission + Economy + Narrative | ⚠️ threat has no renderer (D3) |
| Forced-evac timeout while ReturnArmed | Mission + Hazard | ⚠️ depends on undesigned Hazard owner (C4) |

---

## GDDs flagged for revision

| GDD | Reason | Type | Priority |
|---|---|---|---|
| `mission-state-machine.md` | Missing Acceptance Criteria + 3 stub sections (C2); school-era optional (C5); stale Related (C3) | Completeness | High |
| `office-economy-progression.md` | As-built cites deleted file (C1); no recurring money sink (D1); reputation clamp (D2); stale doc refs (C3) | Consistency + Design | High |
| `level-map-generation.md` | Own Open Qs only: F2 toggle count vs escalation; NavMesh carve-leak strategy | Design | Warning |

---

## Coverage gaps (no GDD exists)

These are **hard dependencies of the 3 reviewed GDDs** with no GDD of their own:
**Scene Flow / Game State**, **Save / Persistence**, **Interaction Framework**, **Hazard /
Escalation (clock)**, **Narrative / Threat events**, **Loot / Content Fill**.

And — the headline gap for current design focus — **the Auditor (核查专员) monster has no GDD and
no AI script** (only bestiary *data* in `Office/`). It cannot be reviewed, only authored.

---

## Verdict: CONCERNS

No hard contradictions between the GDDs — architecture can proceed — but **C1** (broken
provenance), **C2** (incomplete Core GDD), and **D1** (no money sink) should be resolved before
these docs are trusted as the build spec.

### Recommended next actions
1. **Author the Auditor monster GDD** (`/design-system`) — biggest gap in the current design focus.
2. `/design-review mission-state-machine` — fill the missing Acceptance Criteria + stub sections.
3. Reconcile `office-economy-progression.md` — re-verify rewards vs `MissionRewardCalculator.cs`,
   reframe the stale refs, decide the money sink (D1).
