# Epic: Mission State Machine

> **Layer**: Core
> **GDD**: `design/gdd/mission-state-machine.md` — NOT YET WRITTEN (source: `docs/world-background-2098.md` + design decisions 2026-06-09)
> **Architecture Module**: MissionStateModule (not yet defined — `docs/architecture/architecture.md` missing)
> **Status**: Ready (stories Blocked pending mission ADR)
> **Stories**: Not yet created — run `/create-stories mission-state-machine`

## Overview

The Mission State Machine owns the full mission lifecycle for Black Commission's
commission-running loop. It is responsible for:

1. **Job presentation** — each dispatch cycle, 3 random jobs are drawn from the
   player's current license-tier pool (pool selection is driven by a hidden internal
   level invisible to the player) and presented at the office computer.

2. **Van dispatch** — the selected mission is locked; the team boards the dispatch
   van to begin transit. The van is the physical gate between office and mission site.

3. **In-mission state** — server-authoritative state machine governs the mission
   from arrival to resolution: `NotStarted → Active → Complete | PartialReturn | Failed`.

4. **Mission type routing** — three mission tiers operate under the same state machine
   but with different risk/reward/moral profiles:
   - **自由采集** (free collection / junk run) — unlocks at license Stage 3; low risk,
     stable income, no satirical client reveal
   - **指定委托** (commissioned job) — available from Stage 1; Mars client requirements,
     satirical settlement feedback, moderate-to-high risk
   - **黑色委托** (black commission) — unlocks at license Stage 4; high reward, dark
     moral cost, ambiguous client identity

5. **Settlement** — on mission resolution, the host computes the outcome (full/partial/fail)
   and writes a pending reward entry. The team claims it at the office computer on return.

6. **License stage progression** — completing missions contributes to license stage
   advancement, which is the game's primary progress backbone (not reputation, not
   a visible XP bar).

7. **Story mission pool** — 3-5 specially authored missions are woven into the
   指定委托 pool. Completing all story missions unlocks the Stage 5 移民资格审查
   (immigration qualification check), which triggers the endgame moral choice.

This system depends on ADR-0001 (host-authoritative networking) for its authority
model. All state transitions, reward computation, and pool selection run on the
host and replicate to clients via NetworkVariable.

## Governing ADRs

| ADR | Decision Summary | Engine Risk |
|---|---|---|
| ADR-0001: Host-Authoritative Networking | All shared state on host; clients request via ServerRpc; settlements host-only | MEDIUM |
| ADR-mission-state-machine: **MISSING** | Must be written before stories can be Ready — run `/architecture-decision "mission-state-machine"` | — |

## GDD Requirements

> ⚠️ TR registry is empty — all IDs below are provisional. Run `/architecture-review`
> after the mission GDD and ADR are written to assign stable IDs.

| TR-ID (provisional) | Requirement | ADR Coverage |
|---|---|---|
| TR-mission-001 | 3 random jobs per dispatch cycle; player selects one | ❌ No ADR |
| TR-mission-002 | Hidden player level drives job pool tier selection | ❌ No ADR |
| TR-mission-003 | State machine: NotStarted → Active → Complete/PartialReturn/Failed, server-auth | ⚠️ ADR-0001 implied |
| TR-mission-004 | Mission taxonomy: 自由采集 / 指定委托 / 黑色委托 | ❌ No ADR |
| TR-mission-005 | 自由采集 unlocks at license Stage 3 | ❌ No ADR |
| TR-mission-006 | 黑色委托 unlocks at license Stage 4 | ❌ No ADR |
| TR-mission-007 | Settlement math (full/partial/fail) computed host-only | ⚠️ ADR-0001 implied |
| TR-mission-008 | Pending reward handoff — claimed at office computer on return | ❌ No ADR |
| TR-mission-009 | License stage progression gated by mission completion | ❌ No ADR |
| TR-mission-010 | Story mission pool (3-5 missions) completion unlocks endgame | ❌ No ADR |

## Out of Scope (separate epics)

- **Office economy** — debt tracking, monthly operating costs, takeover threat narrative events
- **Van dispatch scene flow** — scene loading, transit overlay, boarding sequence
- **Monster AI** — per-level enemy behaviour (SchoolMonsterAI, future level monsters)
- **Tower-level mechanics** — power gate (F1 PowerRoom), heavy carry objective

## Existing Code (to extend, not replace)

| File | Current Role |
|---|---|
| `Assets/_Project/Scripts/Mission/LostItemMissionManager.cs` | Current main mission state manager (school MVP) |
| `Assets/_Project/Scripts/Office/OfficeComputer.cs` | Job selection UI and dispatch trigger |
| `Assets/_Project/Scripts/Office/OfficeTaskDefinition.cs` | ScriptableObject per job definition |
| `Assets/_Project/Scripts/Office/MvpMissionRuntime.cs` | Runtime handoff office → mission scene |
| `Assets/_Project/Scripts/Office/MvpPendingReward.cs` | Pending reward handoff on return |
| `Assets/_Project/Scripts/Mission/MvpMissionClock.cs` | Mission timer (feeds monster aggression) |

The new state machine extends this foundation to support the 3-job random pool,
license-tier gating, mission type routing, and license progression tracking.

## Prerequisites (must exist before stories can be Ready)

- [ ] `design/gdd/mission-state-machine.md` — run `/design-system mission-state-machine`
- [ ] `/architecture-decision "mission-state-machine"` — covers TR-mission-001 through TR-mission-010
- [ ] `docs/architecture/control-manifest.md` — run `/create-control-manifest`

## Definition of Done

This epic is complete when:

- All stories are implemented, reviewed, and closed via `/story-done`
- All acceptance criteria from `design/gdd/mission-state-machine.md` are verified
- All Logic and Integration stories have passing test files in `tests/`
- Host-authoritative state machine confirmed: mission outcome and rewards identical
  on all clients in 4-player smoke test
- 3-job random pool presents correctly per license stage with no cross-tier leakage
- License stage advances correctly after qualifying mission completions
- All three mission types (自由采集 / 指定委托 / 黑色委托) route through the
  same state machine with correct tier gating

## Next Step

Run `/architecture-decision "mission-state-machine"` to unblock stories, then
`/create-stories mission-state-machine`.
