# Quick Design Spec: Mission Pool Selection

**Type**: New Small System
**Scope**: The office computer terminal draws and displays a pool of 3 available
missions each run. This spec defines pool size, slot composition by license stage,
refresh timing, story mission injection, and carryover behavior. It does NOT
define individual task content, shop logic, or settlement math.
**Date**: 2026-06-16
**Estimated Implementation**: 3–5 days

## Overview

After returning from a mission and claiming settlement, the office computer
terminal displays a refreshed pool of 3 mission slots. Players select one per
run. Pool composition shifts as license stages unlock new tiers. A small number
of narrative story missions inject into the pool via a conditional slot override
and persist until accepted. All selection happens inside the CRT terminal — no
popups, no forced interrupts.

## Core Rules

### Pool Size
1. The pool always shows **exactly 3 slots**. No empty slots are shown — the
   pool is always full (padding with Commissioned jobs if the available pool is
   thin at early stages).

### Slot Composition by License Stage

| Stage | Slot 1 | Slot 2 | Slot 3 |
|-------|--------|--------|--------|
| 1 — 临时采回许可 | Commissioned | Commissioned | Commissioned |
| 2 — 正式采回许可 | Commissioned | Commissioned | Commissioned |
| 3 — 特殊样本转运许可 | Free Salvage | Commissioned | Black Commission |
| 4 — 移民资格审查 | Free Salvage | Commissioned | Black Commission |

Rules:
- **No pool ever shows two Black Commission slots simultaneously.** One is
  a temptation; two collapses the moral slope.
- Each slot's category is fixed by stage. Within each category, the specific
  job is drawn from the available pool for that tier.
- Free Salvage is always Slot 1 once unlocked (Stage 3+). It never competes
  with Commissioned or Black Commission slots.

### Refresh Trigger
2. The pool **refreshes exactly once per run**, triggered when the player
   claims the pending reward at the office computer (`MvpPendingReward`
   claimed → pool regenerates). This is the existing state-transition point;
   no new event is needed.
3. The pool does **not** refresh on session launch, on demand, or on any
   trigger other than reward claim. A team that returns to HQ but has not
   yet claimed settlement sees the same pool from before departure.

### Story Mission Slot Override
4. When a story mission's trigger condition is met (designer-flagged on the
   `OfficeTaskDefinition` asset: `isStoryMission = true`, `storyTriggerStage`
   or `storyTriggerJobCount`), the **next pool refresh overrides Slot 3**
   with that story mission.
5. A story mission in Slot 3 **does not expire** on the carryover cycle — it
   stays in Slot 3 until accepted. Slots 1 and 2 refresh normally.
6. A story mission slot is visually differentiated in the CRT terminal:
   a paper-card prefix glyph (e.g. `[★]`) on the row. No additional UI
   element is added — one glyph inside the existing row format.
7. Only one story mission can occupy the override slot at a time. If a second
   story mission triggers while one is already pending, the second is queued
   and enters Slot 3 on the next refresh after the first is accepted.

### Carryover Rule
8. If a player returns from a run without having selected a mission (edge
   case: starting a new session before claiming settlement), the pool carries
   forward unchanged.
9. If a player **skips** a non-story slot (accepts a different slot), the
   skipped slot's job is flagged as `carryoverCount += 1` and reappears in
   the next pool at **−20% base pay** (rounded down to nearest 10G).
10. A non-story slot that has been skipped twice (`carryoverCount == 2`)
    **expires** on the next refresh and is replaced by a fresh draw.
11. Story mission slots (Rule 5) ignore the carryover counter entirely.

### Client Variety (Commissioned Tier)
12. Within the Commissioned tier, client identity and commission strangeness
    are drawn from a sub-pool filtered by **license stage and missions
    completed** (not by reputation — reputation system removed 2026-06-18).
    Earlier stages → standard clients, mundane targets. Later stages →
    weirder clients, more satirical contracts. Clients simply start calling
    as the office advances — no player-visible unlock needed.
13. Black Commission clients are always drawn from a separate pool gated
    by Stage 3 (`特殊样本转运许可`), regardless of other factors.

## Tuning Knobs

| Knob | Default | Range | Category | Rationale |
|------|---------|-------|----------|-----------|
| `poolSize` | 3 | 2–5 | feel | 3 matches co-op group discussion dynamics and CRT layout |
| `carryoverPayPenalty` | 0.20 | 0.10–0.40 | curve | soft pressure without punishing team pacing |
| `carryoverExpireAfter` | 2 | 1–3 | gate | 2 skips = one full session grace before expiry |
| `weirdClientUnlockStage` | 2 | 1–3 | gate | license stage at which stranger clients start appearing |

All values live in a data asset (`Assets/Resources/Config/MissionPoolConfig.asset`
or equivalent), never hardcoded.

## Affected Systems

| System | Impact | Action Required |
|--------|--------|-----------------|
| `OfficeComputer.cs` | Pool draw + display replaces single `DemoTask` | Implement pool draw, slot rendering |
| `OfficeTaskDefinition` | Add `isStoryMission`, `storyTriggerStage`, `storyTriggerJobCount`, `carryoverCount` fields | Extend ScriptableObject |
| `CompanyState` | Pool reads `LicenseStage` and `MissionsCompleted` | Read-only; no new writes |
| `MvpPendingReward` | Claim event triggers pool refresh | Add pool-refresh hook on claim |
| `MvpLocale` | New terminal strings for pool rows and `[★]` glyph | Add locale keys |
| `design/gdd/office-economy-progression.md` | Section on "job availability" is currently undefined | Update after spec is implemented |

## Acceptance Criteria

- [ ] Terminal always shows exactly 3 slots; no empty slot is ever rendered
- [ ] Pool refreshes on `MvpPendingReward` claim; re-entering HQ before
      claim shows the pre-departure pool unchanged
- [ ] Stage 3+: Slot 1 is always Free Salvage; Stages 1–2: no Free Salvage appears
- [ ] Stage 4+: at most one Black Commission slot visible at any time
- [ ] Story mission (`isStoryMission = true`) occupies Slot 3 on next refresh
      and persists until accepted; `[★]` glyph visible on that row
- [ ] Skipped non-story job reappears in next pool at base pay × 0.80 (rounded
      down to nearest 10G); after 2 skips it is replaced by a fresh draw
- [ ] Skipped story mission slot does not accumulate `carryoverCount`
- [ ] No regression: accepting a mission and departing via office computer
      still triggers the existing scene-load flow unchanged

## Systems Index

Add to `design/systems-index.md` under **Office** layer, Priority Tier 1
(MVP-blocking once office computer UI is built).

Dependency graph:
- Reads: `CompanyState.LicenseStage`, `CompanyState.Reputation`
- Writes: `ActiveTask` → Scene Flow (existing contract, unchanged)
- No new outbound dependencies
