# Office / HQ Economy & Progression

> **Status**: In Design
> **Author**: Yan Dai (PM) + Claude (zeno/hilbert lens)
> **Last Updated**: 2026-06-18
> **Implements Pillar**: 1 (the broke office) · 4 (partial settlement choices) · 5 (the moral slope)
> **Priority / Layer**: MVP / Core
> **Model (locked 2026-06-18)**: Two independent tracks — **License Stage** (job access, 4 stages) and **Earth Deterioration Level** (safety costs, advances with time). Player-facing surface: money + license stage. No reputation. No pressure bar.
> **Related**: `design/game-pillars.md`, `design/gdd/mission-state-machine.md`, `docs/world-background-2098.md` (pressure model source), `docs/architecture/ADR-0001-host-authoritative-networking.md`, as-built `Assets/_Project/Scripts/Office/CompanyData.cs`

## Overview

The economy system is the **host-authoritative ledger that turns the broke office from backstory into pressure**. It runs on two independent tracks that advance at different rates:

- **License Stage** (1–4): gates which job tiers you can access. Advances through story mission completions. The player sees this directly — it is the Mars-capital authority's measure of whether your office is permitted to operate. Stage 4 is the ending-choice gate.
- **Earth Deterioration Level** (1–5): drives the monthly safety infrastructure cost. Advances with total missions elapsed — Earth keeps getting worse regardless of what you choose to do. The player feels this through the budget, not a meter.

The squeeze: safety costs rise continuously, but income is bounded by your license stage. You must advance your license (take darker jobs) to keep pace with Earth's decay. If you can't cover safety costs for too long, the office gets infected and Mars revokes your license — game over. If you reach Stage 4 and choose to stay on Earth, Mars cuts your commissioned work and you live on free salvage alone while Earth continues deteriorating underneath you.

## Player Fantasy

The fantasy is **being permanently, structurally broke — and feeling each job as a stay of execution**. You do not play to get rich; you play to not get evicted this week. The settlement screen is where the squeeze is felt: the gross income looks fine until you see the safety cost line item underneath it, and the net is smaller than you hoped.

Two pressures run simultaneously. The **short-term** one is per-run: did this mission pay enough to cover the safety fee and leave something left over? The **long-term** one is quieter: every few missions the world gets a little worse and that line item grows. Stage 1 it is 40G. By Stage 3 it is 90G. The gap between what a run earns and what it costs to stay alive on Earth is narrowing. Players feel the planet deteriorating not through narration but through arithmetic.

Going to Mars eliminates the safety cost entirely. That is part of why it is tempting — not just narratively, but mechanically. Staying means the bill keeps growing. You chose this. The final chapter missions are what you chose it for.

**Pillars served**: pillar 1 (the broke office), pillar 4 (partial settlement choices), pillar 5 (the moral slope).

## Detailed Design

### License Stages (player-visible)

| Stage | License | Job access | Narrative register |
|---|---|---|---|
| 1 | 临时采回许可 | Basic commissioned jobs | Just trying to pay the debt |
| 2 | 正式采回许可 | Weirder clients; free salvage unlocks | These people are consuming Earth |
| 3 | 特殊样本转运许可 | Black Commission formally available | These jobs are getting darker |
| 4 | 移民资格审查 | Ending-choice gate — Go to Mars or Stay | Do you still want to go? |

Stage 4 is not a gameplay tier with new map content — it is the gate that presents the final choice.

**License advancement (locked 2026-06-18).** Each stage advance requires completing that stage's
**story mission** — an authored commissioned job, flagged in the pool, that becomes available in the
office-computer job list only after the player has completed a minimum number of total missions
(`license_story_gate[stage]`). Completing the story mission advances the license; ignoring it leaves
the player at the current stage. There is **no auto-advance** — but the rising safety cost (Earth
Deterioration) makes camping a low stage unsustainable: a low stage's job access cannot out-earn the
growing deduction, so the economy itself pushes the player toward the next story mission. Default
gates (tuning, balance-pass TBD) keep the license roughly aligned with — and a touch ahead of — Earth
Deterioration, so better-paying access unlocks just before each cost spike:

| Advance | Story mission available after | EarthDet ticks at | Result |
|---|---|---|---|
| Stage 1 → 2 | 4 completed missions | ~5 | 正式采回许可; free salvage unlocks |
| Stage 2 → 3 | 10 completed missions | ~12 | 特殊样本转运许可; Black Commission available |
| Stage 3 → 4 | 18 completed missions | ~20 | 移民资格审查 — presents the ending choice |

**License revocation = game over.** Mars revokes the license when the office can no longer meet safety standards (mid-game failure path) or when a player who chose to stay on Earth eventually cannot sustain operations (ending path). In both cases the revocation authority is Mars-capital — the license was always theirs to withdraw.

### Earth Deterioration Level (internal, felt through budget)

| Level | Safety cost per settlement | When it advances |
|---|---|---|
| 1 | 40G | Start of game |
| 2 | 60G | After ~5 total missions |
| 3 | 90G | After ~12 total missions |
| 4 | 120G | After ~20 total missions |
| 5 | 150G | After ~30 total missions / late Stay-on-Earth path |

Earth Deterioration advances every N completed missions (host-side counter), independent of license stage. During normal play the two tracks roughly align — you advance license stages through story missions at roughly the same pace Earth deteriorates. On the Stay-on-Earth path, deterioration continues past Stage 3 while your license freezes, eventually making the math impossible on free salvage income alone.

The player never sees "Earth Deterioration Level 3." They see `Safety infrastructure: −90G` on the settlement screen.

### Core Rules

1. **Host-authoritative ledger.** All economy state lives in one server-owned `CompanyState`. Only the server mutates it; clients receive a read-only synced copy via `ApplySnapshot` and send intent RPCs (claim, purchase). Per ADR-0001, clients cannot write the ledger.
2. **One company, persisted host-side.** `CompanyState` serialized to schema-versioned JSON (`company.json`) via `SaveIO`, host-only. Guests never overwrite their own save during another player's session.
3. **Money is the only player-facing currency.** `Funds` (int, may go negative). `Debt` is a static backdrop (the "you owe this" mood). No reputation number, no deterioration bar, no pressure bar shown on any surface.
4. **Settlement applied exactly once.** Mission State Machine hands terminal outcome + payload via `MvpPendingReward`; office computer applies on claim. Re-triggering is idempotent (`RewardsGranted`).
5. **Two reward outcomes (money only)**: **Success** / **Failure**. Gross money = the run's scavenged salvage sum (`scavenging-core-loop.md` §4, `Σ payout_i`); Failure pays `round(salvage × failurePayoutRate)` (0.5). No XP. An optional **Bonus evidence** money payload may stack on Success, forfeit on Failure.
6. **Safety cost deducted every settlement.** The current Earth Deterioration Level determines the deduction. Shown as a line item on the settlement screen. Net earnings = gross − safety cost − other penalties.
7. **Underpayment tracking.** If net funds after safety cost deduction go negative (or were already negative and the deduction worsens them), a `ConsecutiveUnderpayments` counter increments. A settlement that leaves funds positive resets it to 0. At threshold the office is deemed unsafe and Mars revokes the license (see Rule 10).
8. **License stage is the player-facing progression surface.** Each stage advance requires completing that stage's **story mission** — a flagged commissioned job that only appears in the office-computer job list after a minimum cumulative mission count (`license_story_gate[stage]` = 4 / 10 / 18; see Tuning Knobs and §License Stages → License advancement). Completing it advances the stage; there is **no auto-advance**. There is **no** internal office level or per-level category unlock (removed 2026-06-18, money-only) — all item categories are always present in the world; license stage gates which *jobs* appear, and the client's category preference differentiates value.
9. **Earth Deterioration advances with missions elapsed.** Every N completed missions (host-counter), `EarthDeteriorationLevel` increments (max 5). This is independent of license stage — Earth does not wait for you.
10. **License revocation = end of run (game over for standard path).** When `ConsecutiveUnderpayments ≥ threshold`, Mars issues a revocation notice (narrative letter/visitor first, then final revocation). Play ends; the player restarts. This is not a soft restructure — there is no coming back from a revoked license.
11. **Stay-on-Earth path exception.** After choosing Stay on Earth at Stage 4: commissioned jobs disappear (Mars clients stop calling), free salvage remains available indefinitely. Earth Deterioration continues advancing. The player can keep playing until the safety cost outpaces free salvage income and ConsecutiveUnderpayments triggers revocation — experienced as a slow fade, not a sudden cut. The revocation notice in this path reads differently ("insufficient operational capacity" rather than "safety failure") but the mechanism is the same.
12. **The shop deducts money, with readable refusal.** Gear purchased at office computer only when no pending reward is waiting and funds are sufficient. Insufficient funds: readable feedback, no state change.
13. **No XP, no office levels, no per-level category unlock** (removed 2026-06-18, money-only). There is no `Experience`, `OfficeLevel`, or `UnlockedCategoryCount`. Progression is carried entirely by `funds`, `license` stage (Rule 8), and `EarthDeteriorationLevel`.
14. **Settlement breakdown is display-only.** `SettlementData` (gross income, safety cost line, other deductions, net, time) produced for Settlement UI; does not itself mutate the ledger.

### States and Transitions

The system's primary state machine is **office safety status**, evaluated after every settlement:

| State | Meaning | Entry condition | Player-facing |
|---|---|---|---|
| `Operating` | Normal | `ConsecutiveUnderpayments < warning_threshold` | Nothing unusual |
| `SafetyWarning` | Underpayments accumulating | `ConsecutiveUnderpayments ≥ warning_threshold` | Narrative warning (office condition notes, colder client calls) |
| `RevokedNotice` | Mars has sent final notice | `ConsecutiveUnderpayments ≥ revocation_threshold ∧ ¬NoticeIssued` | Letter / visitor event in HQ |
| `LicenseRevoked` | Game over | Next settlement after `RevokedNotice` with no recovery | Revocation scene; restart |

Transitions:
- Any settlement that leaves `Funds ≥ 0` → `ConsecutiveUnderpayments = 0` → back to `Operating`.
- Consecutive underpayments reaching `warning_threshold` → `SafetyWarning` (narrative only, still playable).
- Reaching `revocation_threshold` → `RevokedNotice` issued (one final mission possible).
- Failing to recover → `LicenseRevoked` (game over).

**Stay-on-Earth variant:** same state machine, but the narrative framing of `RevokedNotice` and `LicenseRevoked` is different — it reads as "Mars withdraws cooperation" rather than "safety failure."

### Interactions with Other Systems

| System | Data in | Data out | Owner |
|---|---|---|---|
| **Networking** (ADR-0001) | intent RPCs (claim, purchase) | `CompanyState` server-only writes; `ApplySnapshot` to clients | Networking; this system is sole writer |
| **Save / Persistence** | load on boot | schema-versioned JSON, host-only write | SaveIO owns disk; this system owns values |
| **Scene Flow** | HQ-load, return-from-mission | pending reward, office safety state | Scene Flow sequences; this system holds persistent state |
| **Mission State Machine** | terminal outcome + payload, once | money magnitude applied | Mission emits; this system owns formula |
| **Office Computer / HUD UI** | purchase/claim intents | funds, license stage, pending reward, shop availability, safety-state flags | UI sends intents; this system validates |
| **Settlement UI** | — | `SettlementData` with safety cost line item | this system produces |
| **Shop** | purchase request | funds deduction or refusal | Shop owns catalog; this system owns wallet |
| **Narrative / Events** | — | safety-state threshold triggers → letter/visitor | this system emits; narrative renders |

## Formulas

> Code: `Assets/_Project/Scripts/Office/CompanyData.cs`, `CompanyState.cs`, `MissionRewardCalculator.cs`. Verify before any tuning pass.

**1. `settlement_reward`** — applied on claim. Money only — no XP.

**Gross** money is the run's **scavenged salvage sum**, computed per `scavenging-core-loop.md` §4
(`gross = Σ payout_i`, each `payout_i = round(baseValue_i × cond_i × pref_i)`). There is no flat
per-outcome reward table — the haul is the reward.

| Outcome | gross money |
|---|---|
| **Success** (voluntary departure, any haul) | `salvage` |
| **Failure** (whole crew downed) | `round(salvage × failurePayoutRate)` (failurePayoutRate = 0.5) |

An optional **Bonus evidence** money payload (`bonus_money`, default 90G) may stack on a Success.

```
net_money = gross_money + bonus_money − safety_cost[EarthDeteriorationLevel] − overtime_penalty
Funds += net_money
if Funds < 0: ConsecutiveUnderpayments += 1
else:         ConsecutiveUnderpayments  = 0
```

`overtime_penalty` owned by `MvpMissionClock`. Net money can be negative — that is the squeeze.

**Example (Stage 1 / EarthDet 1, a 227G haul + bonus, no overtime):**
`227 + 90 − 40 = +277G net`, ConsecutiveUnderpayments reset to 0.

**Example (EarthDet 3, a thin 60G haul, no bonus):**
`60 − 90 = −30G net`, ConsecutiveUnderpayments += 1 — a thin haul at high EarthDet nets negative; the
squeeze is real. (Tuning lever: safety cost vs typical haul size — see Open Questions / balance pass.)

**2. `earth_deterioration_advance`** — advances every N missions.

```
MissionsCompleted += 1
if MissionsCompleted % deterioration_interval == 0:
    EarthDeteriorationLevel = min(5, EarthDeteriorationLevel + 1)
```

| EarthDeteriorationLevel | Safety cost | Default missions to reach |
|---|---|---|
| 1 | 40G | 0 (start) |
| 2 | 60G | 5 |
| 3 | 90G | 12 |
| 4 | 120G | 20 |
| 5 | 150G | 30 |

**3. `underpayment_resolution`** — evaluated every settlement.

```
if ConsecutiveUnderpayments == 0:              → Operating (normal)
elif ConsecutiveUnderpayments < warning_threshold:  → Operating (quiet)
elif ConsecutiveUnderpayments < revoke_threshold:   → SafetyWarning (narrative signal)
elif ¬NoticeIssued:                            → RevokedNotice (letter/visitor; set flag)
else:                                          → LicenseRevoked (game over)
```

Default thresholds: `warning_threshold = 3`, `revoke_threshold = 5`. A single successful paying settlement resets ConsecutiveUnderpayments to 0 and clears SafetyWarning (but not RevokedNotice once issued).

## Edge Cases

- **Reward claimed twice**: idempotent via `RewardsGranted` — second claim is no-op.
- **Guest sends unauthorized intent**: server rejects, no state change.
- **Insufficient funds for purchase**: readable refusal, no hotbar change, funds untouched.
- **Safety cost exceeds gross income on success**: net goes negative, `ConsecutiveUnderpayments += 1`. Allowed — this is the intended late-game squeeze.
- **Bonus evidence collected but run ends in Failure**: bonus forfeit; failure pays `round(salvage × failurePayoutRate)` only.
- **EarthDeteriorationLevel at max (5) when Stay-on-Earth is chosen**: deterioration stops advancing (already at maximum); safety cost stays at 150G but free salvage income (~40–60G/run) cannot cover it, so revocation is inevitable within a few missions. Intended — the ending has a fixed horizon.
- **ConsecutiveUnderpayments resets mid-warning**: one good run clears the counter but not narrative consequences already delivered (the letter stays on the desk).
- **RevokedNotice issued, then player pays in full next settlement**: `NoticeIssued` flag remains set; the notice was real even if temporarily staved off. A second underpayment after recovery triggers `LicenseRevoked` immediately (no second notice). Tuning knob available to reset notice on sustained recovery.
- **Host disconnects mid-mission**: host migration out of MVP scope. Session ends; save reflects last committed HQ state only.
- **Save missing or corrupt**: fall back to `NewState` (−300G / 300 debt / EarthDet 1).

## Dependencies

**Upstream:**

| System | Hard/Soft | Interface |
|---|---|---|
| Networking (ADR-0001) | Hard | Intent RPCs in; `CompanyState` server-writes + `ApplySnapshot` out |
| Save / Persistence | Hard | `CompanyData.Load/Save`, host-only JSON |
| Scene Flow | Hard | Persistent state across HQ↔mission loads |
| Mission State Machine | Hard | Terminal outcome + payload, exactly once |

**Downstream:**

| System | Hard/Soft | Consumes |
|---|---|---|
| HUD / Office Computer UI | Hard | funds, license stage, pending reward, safety-state flags |
| Settlement UI | Hard | `SettlementData` with safety cost line item |
| Shop | Hard | wallet (purchase deduction / refusal) |
| Narrative / Events | Soft | underpayment threshold triggers → letter/visitor events |

## Tuning Knobs

| Knob | Default | Safe range | Notes |
|---|---|---|---|
| `start_funds` | −300 | −500…0 | Opening debt feel |
| `start_debt` | 300 | 0…1000 | Backdrop mood |
| `bonus_money` | 90 | 30…150 | Optional bonus-evidence side payment (money) |
| `failurePayoutRate` | 0.5 | 0…1 | Failure pays this fraction of banked salvage (lives in `ScavengingConfig`) |
| `safety_cost_det_1` | 40G | 20…80 | Stage 1 feel — should be survivable easily |
| `safety_cost_det_2` | 60G | 40…100 | First real squeeze |
| `safety_cost_det_3` | 90G | 60…130 | Partial return starts hurting |
| `safety_cost_det_4` | 120G | 80…160 | Requires Black Commission income |
| `safety_cost_det_5` | 150G | 100…200 | Stay-on-Earth endgame horizon |
| `deterioration_interval` | 5 missions | 3…10 | How fast Earth worsens |
| `warning_threshold` | 3 | 2…5 | Underpayments before narrative signal |
| `revoke_threshold` | 5 | 3…7 | Underpayments before notice issued |
| `allow_negative_purchase` | false | bool | Prevents buying into deeper debt |
| `license_story_gate[1→2]` | 4 missions | 2…8 | Min total missions before the Stage 1→2 story job appears |
| `license_story_gate[2→3]` | 10 missions | 6…14 | Min total missions before the Stage 2→3 story job appears |
| `license_story_gate[3→4]` | 18 missions | 12…24 | Min total missions before the Stage 3→4 story job appears |

## Visual / Audio Requirements

- **No deterioration bar, no underpayment counter** shown anywhere in UI. Players feel Earth getting worse through the safety cost line item on the settlement screen.
- **Safety cost line item** on settlement screen: `Safety infrastructure: −40G`. Changes amount as EarthDeteriorationLevel advances.
- **Safety warning** expressed as narrative events: colder client messages, office condition details (flickering filtration light, condensation on seals), not a UI indicator.
- **Revocation notice** = a letter on the desk or a visitor in a good coat. Not a game-over screen until the final resolution.
- **Stay-on-Earth revocation** reads differently from safety-failure revocation — Mars's language is bureaucratic withdrawal, not condemnation.
- Audio cues: claim ("ka-chunk"), refused-purchase, license advance stamp, warning letter arrival, revocation final.

## UI Requirements

- **Office computer terminal**: always shows funds (negative = red), license stage, pending reward block, job availability, shop commands. Never shows EarthDeteriorationLevel or underpayment count.
- **Settlement screen**: gross income, safety infrastructure deduction (line item), other deductions, net, outcome. Client usage notes deliver the satire.
- **Narrative events**: warning and revocation delivered as physical objects/visitors in HQ, not UI overlays.

## Acceptance Criteria

**Settlement**
- GIVEN Success at EarthDet 1 with a 227G haul, WHEN claimed, THEN `Funds += 227 − 40 − overtime` (net), exactly once. No XP.
- GIVEN a thin 60G haul at EarthDet 3, WHEN claimed, THEN `Funds += 60 − 90 = −30` (net negative), `ConsecutiveUnderpayments` increments.
- GIVEN Failure, WHEN claimed, THEN `Funds += round(salvage × failurePayoutRate) − safety_cost`. No XP.
- GIVEN bonus evidence on success, WHEN claimed, THEN `+bonus_money` (90G) stacks onto the haul. No XP.
- GIVEN reward already claimed, WHEN E pressed again, THEN no state change.

**Safety cost line item**
- GIVEN EarthDet 1, WHEN any settlement, THEN settlement screen shows `Safety infrastructure: −40G`.
- GIVEN EarthDet 3, WHEN any settlement, THEN deduction shown as `−90G`.

**Earth deterioration**
- GIVEN 5 missions completed, WHEN 5th mission settles, THEN `EarthDeteriorationLevel` advances to 2 and safety cost on next settlement is 60G.
- GIVEN EarthDet already at 5, WHEN further missions complete, THEN level stays at 5.

**Underpayment / revocation**
- GIVEN 3 consecutive settlements where Funds went negative after safety deduction, WHEN 4th settlement resolves, THEN SafetyWarning state active (narrative signal only, still playable).
- GIVEN 5 consecutive underpayments, WHEN next settlement resolves without recovery, THEN RevokedNotice issued (letter/visitor in HQ), no numeric UI shown.
- GIVEN RevokedNotice active and another underpayment settlement, THEN LicenseRevoked (game over / ending scene).
- GIVEN any settlement that leaves Funds ≥ 0, THEN ConsecutiveUnderpayments resets to 0.

**Stay-on-Earth path**
- GIVEN player chose Stay on Earth at Stage 4, WHEN next office computer session, THEN commissioned jobs absent from job list; free salvage still available.
- GIVEN EarthDet 5 and Stay-on-Earth active, WHEN free salvage settled (~50G gross − 150G safety = −100G net), THEN ConsecutiveUnderpayments increments normally.

**Shop**
- GIVEN sufficient funds and no pending reward, WHEN purchase, THEN Funds decreases and item in hotbar.
- GIVEN insufficient funds, WHEN purchase attempted, THEN readable refusal, no state change.

**Persistence**
- GIVEN saved company, WHEN host relaunches, THEN loaded CompanyState matches last committed state including EarthDeteriorationLevel and ConsecutiveUnderpayments.
- GIVEN missing/corrupt save, WHEN game loads, THEN falls back to NewState (−300G / 300 debt / EarthDet 1).

## Open Questions

| # | Question | Owner | Target |
|---|---|---|---|
| 1 | **Thin-haul viability**: a ~60G haul nets negative at EarthDet 3+ (60−90). Intended (survival-only runs) or retune safety cost / haul sizing? | PM Yan Dai | balance pass |
| 2 | ✅ **RESOLVED 2026-06-18** — advance = complete that stage's **story mission**, which appears after `license_story_gate[stage]` total missions (**4 / 10 / 18**). Both: authored story mission + min-count availability gate; no auto-advance. | PM Yan Dai | done |
| 3 | **Deterioration interval tuning**: 5 missions/level feels right for a ~30 mission full game. Confirm with playtesting. | PM + QA | first playtest |
| 4 | **RevokedNotice recovery**: should one paying settlement after notice clear the notice flag, or is the notice permanent once issued? | PM | balance pass |
| 5 | **Stay-on-Earth revocation narrative**: the letter/visitor event needs a different authored text from mid-game safety failure. Two distinct GDD events. | narrative-director | when narrative system is designed |
| 6 | **Free salvage income cap**: what is the realistic gross income per free salvage run? Must be below EarthDet 5 cost (150G) to ensure the Stay-on-Earth horizon works. | systems-designer | balance pass |
| 7 | **Failure safety cost**: does a Failure run deduct the full safety cost, or reduced (shorter exposure)? | PM | balance pass |
