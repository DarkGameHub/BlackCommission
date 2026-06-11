# Mission State Machine

> **Status**: In Design
> **Author**: Yan Dai (PM) + Claude (zeno/laplace lens)
> **Last Updated**: 2026-06-08
> **Implements Pillar**: partial settlement choices; co-op extraction tension
> **Priority / Layer**: MVP / Core
> **Related**: `docs/mvp-core-loop.md` (flow spec), `docs/architecture/ADR-0001-host-authoritative-networking.md`, `design/gdd/level-map-generation.md` (supplies objective/exit nodes)

## Overview

The Mission State Machine is the **server-authoritative brain of a single commission run**:
it tracks where the team is in the loop (boarding the van → in transit → on site → objective
found → returning) and which **terminal outcome** they reach — full success, early **partial
return**, or total failure — then hands the result to settlement **exactly once**. Every
transition that matters (picking up the objective, arming and confirming an early return,
triggering the exit, a player being downed) is validated and owned by the host: the server
checks the acting player, distance, alive/downed state, and objective-carrier identity, so a
client can never remotely grab the objective, finish on a non-carrier's behalf, or
double-claim a reward (per **ADR-0001**, host-authoritative). For the player it's the spine of
the tension they feel: the instant the objective is in hand the run flips from *search* to
*extract*, and the team negotiates out loud — push for the full payout, or cut losses and take
the partial settlement before someone goes down. Without it there is no run — no win, no loss,
no "do we leave now?" — just a building to walk around in.

## Player Fantasy

The fantasy is **the out-loud "do we leave now?" argument** — the state machine exists to
manufacture that conversation. Coming in, the team is in search mode: calm, splitting up,
grabbing loot. The machine's pivot is **objective-in-hand**: the run audibly flips to
*extract*, the clock and threats now bite, and someone says "we've got it, go" while someone
else says "one more room." The **arm-then-confirm** partial-return step is the mechanical seat
of that argument — a deliberate, reversible "are we really cutting losses?" beat instead of an
instant bail.

Emotional arc: **search (loose) → the flip (objective found) → squeeze (extract under
pressure) → the verdict** — full-payout pride, partial-return relief, or failure's grim van
ride home. The three terminal outcomes each feel distinct, and the machine's job is to make
*which one you get* feel **earned by the team's choices**, not random.

Experienced as **both**: players directly feel the decision points (pickup, arm-return,
confirm-return, exit), while the validation/sync underneath is invisible — noticed only if it
breaks (a desync where someone "finishes" a job they weren't carrying).

**Pillar**: the literal home of pillar 4 **partial settlement choices** ("extract now for
less, or push for more"), and a primary driver of **co-op extraction tension**.

## Detailed Design

### Core Rules

1. **Host-authoritative**: all mission state lives on the server. Clients hold a synced
   read-only copy and send **intent RPCs**; the server validates and is the only writer (ADR-0001).
2. **One run = one state-machine instance**, spawned on mission-scene load (after Level/Map-Gen
   reports `Ready`), owned by the host, despawned on return.
3. **Three mutually-exclusive terminal outcomes**: `FullSuccess` | `PartialReturn` | `Failure`.
   Exactly one is reached per run.
4. **Objective pickup is validated**: server checks the acting player is **alive**, **within
   distance**, and assigns **carrier identity**. Only the carrier can trigger a full-success completion.
5. **Return happens at the mission van rear cabin** (exit anchor). Two paths: **full** (carrier
   delivers the objective) and **partial** (early return without/before objective).
6. **Partial return is two-step `arm → confirm`** — reversible until confirmed — so a team never
   bails by accident; it's a deliberate "cut losses" decision.
7. **Failure** = all players downed → `Failure` terminal → failure settlement path.
8. **Settlement handoff is idempotent**: the terminal result is handed to settlement **exactly
   once** (`MvpPendingReward`); re-triggering (e.g. mashing E on the office computer) applies
   nothing further.
9. **Optional sub-objectives** (e.g. photograph the overdue ledger) **add reward but never gate
   completion** and never occupy the objective/carrier slot.
10. **The clock can force an end** (Hazard/forced-evac), but the outcome is still exactly one of
    the three terminals — the machine owns the outcome, Hazard only drives the timer.

### States and Transitions

| State | Meaning | Valid transitions (trigger) |
|---|---|---|
| `OnSite_Search` | entry; team searching, no objective held | → `ObjectiveHeld` (valid pickup) · → `ReturnArmed` (arm partial) · → `Failure` (all downed) |
| `ObjectiveHeld` | carrier assigned, objective in hand | → `FullSuccess` (carrier at van exit) · → `ReturnArmed` (arm partial anyway) · → `Failure` (all downed) · → `OnSite_Search` (carrier downs & drops → carrier cleared) |
| `ReturnArmed` | early-return armed, awaiting confirm | → `PartialReturn` (confirm at van) · → back to prior state (cancel) · → `Failure` (all downed) |
| `FullSuccess` *(terminal)* | objective delivered by carrier | → `Settlement` (once) |
| `PartialReturn` *(terminal)* | early return confirmed | → `Settlement` (once) |
| `Failure` *(terminal)* | all players downed / forced timeout | → `Settlement` (once) |
| `Settlement` | hand result to economy, return to HQ | (idempotent; end of run) |

*Note*: `Failure` is reachable from **any** on-site state. `ObjectiveHeld → OnSite_Search` only
if the carrier is downed and the objective is dropped (carrier identity cleared; another player
may re-pick-up).

### Interactions with Other Systems

| System | Data in | Data out / contract | Owner |
|---|---|---|---|
| **Networking** (ADR-0001) | intent RPCs (pickup, arm, confirm, exit) | state via NetworkVariable; server validates & writes; client cannot mutate | Networking transport; this system authority |
| **Scene Flow** | mission-load `Ready` (post Level-Gen) | on terminal → triggers return-to-HQ load | Scene Flow sequences; this system signals end |
| **Level / Map Generation** | objective node (TARGET) + exit node(s) | consumes; relies on I1/I2/I6 (run is completable) | Level owns geometry; this system owns progress |
| **Player** | alive/downed (`PlayerHealth`), carry (`CarrySystem`) | reads for validation; sets carrier | Player owns health/carry; this system reads |
| **Interaction Framework** | pickup / exit / arm / confirm interactions | exposes them as server-validated `IInteractable` | Interaction serves; this system validates |
| **Office Economy / Settlement** | — | the terminal outcome + reward payload, **once** | this system emits; Economy applies |
| **Van Transit** | — | the van rear cabin is the return/exit anchor for full & partial | Van owns the ritual; this system owns the gate |
| **Hazard / Escalation** | forced-evac countdown / area closures | timeout can force a terminal; must not break completability | Hazard drives clock; this system owns outcome |

## Formulas

> Mostly server-side validation predicates. **Reward magnitudes are NOT here** — Office
> Economy owns the money/rep/XP formula (core-loop: a clean full job ≈ 300G). A
> `systems-designer` pass on the radii is advisable before tuning.

**1. `valid_pickup`** — `valid_pickup(p) = alive(p) ∧ dist(p, objective) ≤ pickup_radius ∧ ¬objectiveHeld`

| Var | Type | Range | Description |
|---|---|---|---|
| `alive(p)` | bool | — | actor not downed |
| `dist(p,objective)` | float (m) | 0+ | server-measured distance |
| `pickup_radius` | float (m) | 1.5–3.0, **def 2.0** | interaction reach |
| `objectiveHeld` | bool | — | already carried? |

**Output**: bool, server-side only. *Example*: alive, 1.4 m away, unheld → `true`.

**2. `valid_complete`** (full success) — `valid_complete(p) = is_carrier(p) ∧ alive(p) ∧ at_exit(p)`,
where `at_exit(p) = dist(p, vanExit) ≤ exit_radius` (`exit_radius` def **3.0 m**). **Output**:
bool. Only the assigned carrier completes.

**3. `valid_partial`** (arm & confirm) — both steps require
`state ∈ {OnSite_Search, ObjectiveHeld, ReturnArmed} ∧ alive(p) ∧ at_exit(p)`; `arm` sets
`ReturnArmed`, a second `confirm` commits `PartialReturn`. Cancel returns to prior state.
**Output**: bool per step.

**4. `failure_trigger`** — `failure = (alivePlayers == 0) ∨ (missionClock ≤ 0 ∧ forcedTimeoutOutcome == Failure)`

| Var | Type | Range | Description |
|---|---|---|---|
| `alivePlayers` | int | 0–4 | count not downed |
| `missionClock` | float (s) | — | owned by Hazard; this reads it |
| `forcedTimeoutOutcome` | enum | Failure \| PartialReturn | what a timeout forces (tuning) |

**Output**: bool → `Failure` terminal.

**5. `reward_payload`** (deferred to Economy) — `reward_payload = { outcome, optionalsCollected, elapsedTime }`.
This machine emits **only** the payload; **Office Economy owns the money/rep/XP formula**. Do
not compute reward magnitude here — connect, don't reinvent.

## Edge Cases

- **If the carrier is downed while holding the objective**: objective drops at their location,
  carrier identity is cleared, state → `OnSite_Search`; any alive player may re-pick-up.
- **If two players pick up simultaneously**: server processes intents in arrival order — first
  valid wins; the second sees `objectiveHeld == true` and is rejected.
- **If a client sends a pickup/complete RPC while dead or out of range**: server validation
  fails → **no-op** (anti-cheat; the client cannot force it).
- **If a non-carrier reaches the van exit with no objective**: not a full success — only the
  arm/confirm partial path is available to them.
- **If the carrier completes on the same tick all players would be downed**: a passing
  `valid_complete` **wins over** `Failure` (the team made it out) — completion is resolved
  before the failure check.
- **If partial return is armed, then the objective is found before confirm**: arming persists;
  the team may **cancel** and go for full success, or **confirm** partial anyway (with or
  without the objective).
- **If the forced-evac timer hits 0 while `ReturnArmed`**: resolve per `forcedTimeoutOutcome`
  (default `Failure`; configurable to auto-`PartialReturn`).
- **If the host disconnects mid-mission**: host migration is **out of MVP scope** → the session
  ends with no settlement (documented limitation, not a silent loss).
- **If a client late-joins mid-mission**: receives the current synced state; cannot become
  carrier of an already-held objective; otherwise participates normally.
- **If settlement is triggered twice** (network retry / double E-press): **idempotent** —
  applied exactly once (Rule 8).
- **If the objective would be lost to the void/shaft**: it **never despawns and is always
  recoverable** — Level invariants keep it on a walkable surface (or it lands on F1 and stays
  pickable).
- **If a player tries to take the objective through the one-way scaffold drop (BALCONY→F1)**:
  **forbidden** — the drop is a body-only panic escape; attempting it **drops the objective at
  the top** (cross-ref Level/Map-Gen BALCONY). Preserves "carry it out properly vs. abandon the haul."

## Dependencies

**Upstream — this system depends on:**

| System | Hard/Soft | Interface |
|---|---|---|
| **Networking** (ADR-0001) | **Hard** | Intent RPCs in; state out via NetworkVariable; server is sole writer/validator. |
| **Scene Flow / Game State** | **Hard** | Instantiated on mission-load `Ready`; emits "run over" → return-to-HQ load. |
| **Level / Map Generation** *(GDD exists)* | **Hard** | Consumes objective (TARGET) + exit node(s); relies on I1/I2/I6 completability. |
| **Player** | **Hard** | Reads `alive/downed` (`PlayerHealth`), carry (`CarrySystem`); assigns carrier. |
| **Interaction Framework** | **Hard** | pickup / exit / arm / confirm are server-validated `IInteractable`. |

**Downstream — these depend on this system** *(all undesigned — contracts provisional)*:

| System | Hard/Soft | What they consume |
|---|---|---|
| **Office Economy / Settlement** | **Hard** | the terminal outcome + reward payload, exactly once. |
| **Settlement UI** | **Hard** | the outcome + payload to display the results screen. |
| **HUD** | **Hard** | live mission state, objective progress, clock for the in-mission HUD. |
| **Van Transit** | **Hard** | the return/exit gate (full vs partial routed through the van). |
| **Hazard / Escalation** | **Soft** | reads nothing from this; *drives* the clock this system reacts to (can force a terminal). |

**Bidirectional note**: Level/Map-Gen's GDD already lists "Mission State Machine (Hard)" as a
dependent — consistent. When Economy/Settlement/HUD/Van GDDs are authored, each must list
"depends on Mission State Machine." Flagged for `/consistency-check`.

## Tuning Knobs

| Knob | Default | Safe range | Too low → | Too high → | Interacts with |
|---|---|---|---|---|---|
| `pickup_radius` | 2.0 m | 1.5–3.0 | finicky/failed pickups | grab-through-wall cheese | Interaction reach |
| `exit_radius` | 3.0 m | 2–5 | fiddly returns | accidental returns | van placement |
| `partial_return_two_step` | `true` | bool | — | (if false) accidental bails, kills the deliberate choice | the pillar-4 decision beat |
| `forcedTimeoutOutcome` | `Failure` | Failure \| PartialReturn | — | (Partial) softer, less punishing timeout | Hazard clock |
| `mission_clock_duration` | 3 min (test) / 15 min (real) | — | unfair, no time to search | no extraction pressure | **owned by Hazard** — referenced, not defined here |
| `objective_drop_on_carrier_down` | `true` | bool | (false) objective vanishes with carrier — bad | — | Player downed handling |
| `optional_objective_bonus` | — | — | no reason to grab optionals | optionals dominate the run | **owned by Office Economy** — referenced |

**Source-of-truth notes**: the **mission clock** is a Hazard/Escalation knob (this system only
reads it); **reward magnitudes / optional bonus** are Office Economy knobs. Listed here for
visibility, not redefined — change them in their owning GDD.

## Visual/Audio Requirements

[To be designed]

## UI Requirements

[To be designed]

## Acceptance Criteria

[To be designed]

## Open Questions

[To be designed]
