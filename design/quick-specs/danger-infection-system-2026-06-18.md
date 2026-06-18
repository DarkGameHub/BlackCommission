# Quick Design Spec: Danger Level & Infection Exposure System

**Type**: New Core System
**Scope**: Replaces fixed mission timer. Defines continuous in-mission pressure
via two independent tracks: Danger Level (building threat escalation, hidden)
and Infection Exposure (personal contamination cost, visible). Does NOT define
individual monster behaviour — see `design/gdd/monster-system.md`.
**Date**: 2026-06-18
**PM Decision**: Timer removed. This system is the full replacement.

## Overview

There is no mission clock. Instead, two parallel tracks create continuous
pressure that rises the longer the team stays inside:

- **Danger Level**: The building "wakes up" — monsters become more active,
  patrols widen, new spawns activate. Never shown as a number. Felt through
  world behaviour and audio.
- **Infection Exposure**: Each player accumulates MRC-7 contamination.
  Visible on the HUD. Converts to a settlement deduction (`去污处理费`).
  At extreme levels, starts affecting player movement and vision.

The two tracks reinforce each other: staying longer makes the building more
dangerous AND makes each item you pick up more contaminated (reducing
settlement value and negotiation leverage).

## Track 1 — Danger Level (hidden, environmental)

### Phases

| Phase | Trigger | Monster behaviour | Environmental signal |
|---|---|---|---|
| Survey 勘查 | Entry | Fixed patrol routes, narrow detection | Quiet, distant sounds |
| Active 活跃 | 8 min elapsed | Patrols widen, faster reaction | Frequent monster audio, unstable lights |
| Pursuit 追猎 | 18 min elapsed | Active searching, multiple spawn points live | Close sounds, building pressure audio |
| Saturation 饱和 | 28 min elapsed | Full building hostile | Van evacuation signal (horn + lights flash) |

> **Model (hybrid, locked 2026-06-18):** `danger_level` is a **continuous** time-based value
> (0→100 over ~28 min, pure time — no player-action spikes). The 4 phases above are **discrete
> markers** on that curve (they gate spawn activation, environmental signals, the infection
> `danger_multiplier`, and evac). Monster *activity* (move speed, sensor range, attack frequency)
> ramps **smoothly** within phases via `monster_activity = lerp(idle, frenzied, danger_level)` —
> see `design/gdd/monster-system.md`. Phase name **Pursuit** (not "Hunt") avoids colliding with the
> monster AI state `Hunt`.

Saturation triggers a **forced evacuation countdown** (60 seconds) — the only
hard deadline in the game. Players either reach the van or the run ends as Failure.

### Multi-trip escalation

Each time players re-enter the building after returning to the van, the Danger
Level phase advances by one. A team that makes a second trip enters at Active,
not Survey. Third trip enters at Pursuit.

```
Trip 1: Survey → Active → Pursuit → Saturation
Trip 2: Active → Pursuit → Saturation (compressed window)
Trip 3: Pursuit → Saturation (very short window)
```

This makes multi-trip runs a high-risk strategy, not a default optimal path.

### Tuning knobs

| Knob | Default | Range | Notes |
|---|---|---|---|
| `surveyDuration` | 8 min | 5–12 min | First safe exploration window |
| `activeDuration` | 10 min | 6–15 min | Middle tension phase |
| `pursuitDuration` | 10 min | 5–15 min | High-pressure phase (was huntDuration) |
| `saturationCountdown` | 60 s | 30–90 s | Hard evac window |
| `multiTripPhaseAdvance` | 1 phase | 0–2 | How much phase advances per re-entry |

## Track 2 — Infection Exposure (visible, economic)

### Accumulation formula

```
exposure_per_minute = base_rate × danger_multiplier[phase] × zone_factor[zone]
```

| Variable | Default | Notes |
|---|---|---|
| `base_rate` | 3 / min | Baseline just for being inside |
| `danger_multiplier[Survey]` | ×1.0 | — |
| `danger_multiplier[Active]` | ×1.5 | — |
| `danger_multiplier[Pursuit]` | ×2.5 | — |
| `danger_multiplier[Saturation]` | ×4.0 | — |
| `zone_factor[Entry/Corridor]` | ×1.0 | Standard areas |
| `zone_factor[Mid zone]` | ×1.3 | Deeper areas |
| `zone_factor[Deep/Objective]` | ×2.0 | Infection core |

**Example calculations:**

- 5 min in corridor, Survey phase: `3 × 1.0 × 1.0 × 5 = 15 exposure`
- 5 min in deep zone, Pursuit phase: `3 × 2.5 × 2.0 × 5 = 75 exposure`
- Second trip (starts at Active), 5 min in mid zone: `3 × 1.5 × 1.3 × 5 = 29 exposure`

Exposure is **per player**, not shared. A player who stays at the entry while
others go deep accumulates far less.

### Gameplay effects by exposure level

| Exposure | Effect |
|---|---|
| 0–40 | None (safe range) |
| 41–70 | Minor: slight vision vignette, faint audio distortion |
| 71–90 | Moderate: movement 10% slower, vision narrower |
| 91–100 | Severe: significant movement/vision impairment; strong signal to leave |

### Settlement deduction

```
去污处理费 = total_exposure × 1G (per player, summed across team)
```

Shown as a line item on the settlement screen:
`人员去污处理（4人）：−240G`

This is **added to** the safety infrastructure monthly cost, not the same line.

### Item condition interaction

Items picked up by high-exposure players in the deep zone have their condition
degraded one step. Specifically:

- Player exposure > 70 AND item picked up in deep zone → item condition downgrades
  one step (完好→一般→受损; **受损 is the floor** — the 污染 state was cut 2026-06-18)
- This connects the personal exposure cost to settlement value loss: staying
  long in the deep zone makes you pay more in decontamination AND makes
  your items worth less to the client.

### HUD display

Exposure is shown on the HUD as a **suit indicator** — a small suit icon with
a fill bar (0–100). This is the one numeric indicator permitted in the mission
HUD. It is a physical sensor readout on the contamination suit, not an
abstract game value.

The Danger Level phase is NOT shown numerically anywhere. Players feel it
through the world.

## Acceptance Criteria

- [ ] Danger Level advances automatically through 4 phases on elapsed time
- [ ] Monster behaviour visibly changes at each phase boundary (patrol range,
      speed, frequency)
- [ ] No Danger Level number appears in any UI at any time
- [ ] Saturation triggers van evacuation signal (horn/lights); 60s countdown
      then Failure if players not in van
- [ ] Re-entry after van return advances Danger Level phase by 1
- [ ] Exposure accumulates per player per minute using the formula above
- [ ] Exposure shown as suit fill indicator on HUD; 4 visual states matching
      the effect thresholds
- [ ] Settlement deduction `exposure × 1G` appears as `去污处理费` line item
- [ ] Items picked up at player exposure >70 in deep zone degrade one condition step
- [ ] No regression to prior `MvpMissionClock` fixed-timer behaviour

## Dependencies

- `design/gdd/monster-system.md` — Danger Level drives monster activity: the continuous
  `danger_level` feeds `monster_activity = lerp(idle, frenzied, danger_level)`, and phase
  boundaries gate spawn activation. (Phases are environmental escalation, distinct from the
  monster AI states `Roam`/`Investigate`/`Hunt`/`Attack`.)
- `design/gdd/office-economy-progression.md` — settlement deduction appended
  to `SettlementData` alongside safety infrastructure cost
- `design/quick-specs/scavenging-item-system-2026-06-18.md` — item condition
  degradation rule
- `Assets/_Project/Scripts/Office/Core/MvpMissionClock.cs` — **superseded**;
  this system replaces the fixed timer. Clock script to be refactored or removed.
