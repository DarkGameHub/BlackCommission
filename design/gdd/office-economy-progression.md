# Office / HQ Economy & Progression

> **Status**: In Design
> **Author**: Yan Dai (PM) + Claude (zeno/hilbert lens)
> **Last Updated**: 2026-06-09
> **Implements Pillar**: 1 (the broke office) · 4 (partial settlement choices) · 5 (the moral slope) · hostile-takeover pressure
> **Priority / Layer**: MVP / Core
> **Progression model (locked 2026-06-09)**: **Layered** — numeric `OfficeLevel` / `Reputation` / `HostileTakeoverPressure` are INTERNAL host-authoritative state that drives all logic; the player-facing surface is license stages + narrative events (letters/visits). No raw 0–100 bars in UI.
> **Related**: `docs/mvp-core-loop.md`, `design/game-pillars.md` (license stages), `design/gdd/mission-state-machine.md` (emits reward payload), `docs/architecture/ADR-0001-host-authoritative-networking.md`, as-built `Assets/_Project/Scripts/Office/CompanyData.cs`

## Overview

The **Office / HQ Economy & Progression** system is the **persistent host-authoritative ledger that turns "the broke office" from backstory into pressure**. It owns the company's money and debt, the hidden internal progression that decides which jobs the office is even allowed to take, and the hostile-takeover threat that closes in when the team keeps failing. Mechanically it is one server-owned record — `CompanyState` — that only the host mutates and persists to disk (clients hold a synced read-only copy for display); every settlement, purchase, and acquisition flows through it exactly once. What the player *feels* is narrower and sharper than what the system tracks: on the surface there is **money** (you are in the red and the rent is due), a **license stage** that marks how far the office has clawed up the Mars-capital ladder, and **narrative pressure** that arrives as letters and visitors rather than a meter. Underneath, the system also keeps an internal reputation value and an internal office/experience level that the player never sees as numbers — they quietly steer which clients and job tiers appear — and a numeric takeover-pressure counter that the system reads to decide *when to send the next threatening letter*. The economy exists to give every mission a consequence and every settlement a verdict: a clean job buys you another week of solvency, a partial return keeps you alive for less, and a string of failures invites a competitor to restructure you into something worse. Without it there is no debt, no stakes, and no reason to climb into the van.

## Player Fantasy

> *Note: drafted in Lean review mode — `creative-director` (gate CD-GDD-ALIGN) not consulted during authoring. Review the framing manually before production, or run in Full mode.*

The fantasy is **being permanently, structurally broke — and feeling each job as a stay of execution rather than a victory**. You do not play to get rich; you play to not get evicted this week. The economy's emotional job is to make sure the relief of finishing a mission is always undercut at the settlement screen: you carried the sales model out alive, and you are *still* in the red, and the rent notice is still on the desk. That contrast — survived the haunting, lost to the arithmetic — is the core feeling.

It is experienced as **both** a thing you act on and a weather system you live under. Directly, you feel it at four moments: the **shop** (spending money you don't have on a cheap flashlight), the **moral-slope job choice** (the safe pittance vs. the darker commission that actually clears debt), the **stay-or-go settlement** (push for the full payout or cut losses for partial), and the **claim** (watching the number tick up and knowing it isn't enough). Indirectly, you feel the parts you can't see as *mood*: jobs you're "not licensed for yet," clients who stop calling, and — the signature beat — **a letter or a visitor** appearing in the office when the company has been failing. There is no pressure bar. There is a man in a good coat standing in your doorway, and you know what that means.

The progression fantasy is **bitter advancement**: each license stage is a stamp from the Mars-capital authority that "promotes" you into stranger, darker work. Climbing the ladder is the satire — every rung up is a step toward consuming Earth for someone else's benefit. The reward for paying your debts is being trusted with worse jobs.

Emotional arc per session: **open in the red → take the job you can stomach → survive the site → the settlement verdict (solvency bought / partial shame / failure dread) → the desk, where the letter either is or isn't waiting.** The system's whole purpose is to make *which verdict you get* feel earned by the team's choices — the job tier they accepted, whether they pushed or bailed — never arbitrary.

**Pillars served**: pillar 1 *the broke office* (survival is the only motivation), pillar 4 *partial settlement choices* (the economic seat of "extract now for less"), pillar 5 *the moral slope* (higher reward, darker price), and the *hostile-takeover pressure* as lived dread, not a meter.

## Detailed Design

### Core Rules

1. **Host-authoritative ledger.** All economy state lives in one server-owned `CompanyState` record (the host's company). Only the server mutates it; clients receive a read-only synced copy via `ApplySnapshot` for display and send **intent RPCs** (claim, purchase, acquire). Per **ADR-0001**, a client can never write the ledger. (Cross-ref: networking owns transport; this system owns authority over the values.)
2. **One company, persisted host-side.** `CompanyState` is serialized to schema-versioned JSON (`company.json`) under `persistentDataPath` via `SaveIO`, **written only by the server**. A client that is a guest in someone else's session never overwrites its own save; on returning to solo/host it reloads its own from disk.
3. **Money is the only player-facing currency.** `Funds` (int, may go negative — negative = in debt) is what the player sees and spends. `Debt` is a separate **static backdrop** figure (the "you owe this" mood); it grows on hostile restructure (+500) but does **not** drain funds per cycle. There is no reputation number, no level number, and no pressure bar on the surface.
4. **Settlement is the only mission money event, applied exactly once.** The Mission State Machine hands a terminal outcome + payload to settlement via `MvpPendingReward`; the office computer applies it on claim. Re-triggering (mashing `E`, network retry) applies **nothing further** — idempotent (`RewardsGranted`).
5. **Four reward outcomes** map to the mission's terminal state: **FullSuccess**, **PartialReturn**, **Failure**, each with money/rep/XP magnitudes (see Formulas), plus an additive **Bonus evidence** payload that stacks onto success/partial but never gates completion.
6. **Hidden internal progression drives content, never shown as numbers.** `Reputation` (−100…100) and `OfficeLevel` (1…8, with `Experience`) are **internal**: they steer which clients/job tiers appear (`UnlockedCategoryCount = clamp(OfficeLevel,1,8)`) and feed threat logic. The player never sees them as values — only their effects (jobs available, clients calling).
7. **License stage is the player-facing progression surface.** For MVP scope, the single license-advance moment is the **tutorial acquisition** (Rule 10) standing in for stage 1→2. The full five-stage ladder (临时采回 → 正式采回 → 轨道运送 → 特殊样本转运 → 移民资格审查) is deferred (see Open Questions). Internal `OfficeLevel` remains the dev-facing driver beneath it.
8. **Takeover pressure is internal, surfaced only as narrative events.** `HostileTakeoverPressure` (int 0–100) rises on partial/failure and falls on success. It is **never** drawn as a bar. The system reads it to decide *when to send a letter or a visitor*: at 100 while broke (`Funds<0`) and disliked (`Reputation<0`), the **first** trigger issues an ultimatum (a final-warning letter); a **subsequent** failure under the same bad conditions executes a forced restructure.
9. **The shop deducts money, with readable refusal.** Gear is purchased at the office computer **only when no pending reward is waiting** and only while in range. A purchase deducts `Funds`; **insufficient funds produces readable feedback and changes nothing** (no hotbar change, no partial buy). Funds may be spent into the negative only if a tuning knob allows it (default: purchases cannot push below 0 — see Tuning Knobs).
10. **Tutorial acquisition (MVP license stand-in).** A one-time offer appears when `OfficeLevel == 1 ∧ CompletedLostItemJobs ≥ 2 ∧ ¬HasAcquiredTutorialOffice`. It costs **150G** (valuation 100 × 3/2), requires `HostileTakeoverPressure < 70` and sufficient funds, and on accept: deducts 150G, sets `OfficeLevel ≥ 2`, +1 reputation, −20 pressure, clears any ultimatum, and unlocks the next job category as locked future content.
11. **Experience and leveling are success-only.** Successful jobs grant XP; **failed jobs grant no XP**. Level-ups consume `Experience ≥ max(100, OfficeLevel × 300)` thresholds, capped at `OfficeLevel 8`. Leveling raises internal scale (unlocked category count), not combat power.
12. **Settlement breakdown is display-only.** A per-mission `SettlementData` (income, expenses, net, optionals, time) is produced for the Settlement UI; it does not itself mutate the ledger — the magnitudes in the payload do.

### States and Transitions

Money, reputation, and level are **scalar accumulators** (no FSM). The system's only true state machine is the **hostile-takeover threat ladder**, evaluated server-side after every settlement:

| State | Meaning | Entry condition (post-settlement) | Player-facing manifestation |
|---|---|---|---|
| `Stable` | No active threat | `Pressure < 100` **or** not (broke ∧ disliked) | Nothing; normal office |
| `UnderPressure` | Pressure rising but no ultimatum | `70 ≤ Pressure < 100`, or `Pressure = 100` but solvent/liked | Ambient: fewer clients, colder notices (no bar) |
| `UltimatumIssued` | Final warning delivered | `Pressure = 100 ∧ Funds < 0 ∧ Reputation < 0 ∧ ¬HasHostileTakeoverUltimatum` → sets ultimatum | A **final-warning letter / visitor** appears in HQ |
| `Restructured` | Competitor forcibly took over | Next failure while `UltimatumIssued ∧ broke ∧ disliked` | Restructure event: Debt +500, OfficeLevel −1, Funds clamped ≤ −500, Reputation ≤ −5, XP/job-progress reset, pressure reset to 35 |

Transitions:
- **Any settlement → recompute pressure** (success −25; partial +12/+gains; failure +35/+gains — see Formulas), then evaluate the ladder.
- `UltimatumIssued → Stable`: a **success** that drops pressure below threshold (or below 100) clears the ultimatum flag.
- `UltimatumIssued → Restructured`: a failure under unchanged bad conditions. Restructure is **soft** — the player keeps operating under worse terms (not a hard game-over).
- `Restructured → Stable/UnderPressure`: restructure resets pressure to 35, so the company resumes from a damaged-but-alive position.

Macro solvency flags read by other systems (not FSM states): `IsInDebt = Funds < 0`; `IsHostileTakeoverRisk = HasHostileTakeoverUltimatum ∨ (Pressure ≥ 70 ∧ Funds < 0)`.

### Interactions with Other Systems

| System | Data in | Data out / contract | Owner |
|---|---|---|---|
| **Networking** (ADR-0001) | intent RPCs (claim, purchase, acquire) | `CompanyState` via server-only writes; `ApplySnapshot` to clients (display) | Networking transport; **this system is sole authority/writer** |
| **Save / Persistence** | load on boot (`CompanyData.Load`) | schema-versioned JSON write, **host-only** (`CompanyData.Save`) | this system owns the ledger; SaveIO owns disk I/O |
| **Scene Flow / Game State** | HQ-load event; return-from-mission | exposes claimable pending reward at HQ | Scene Flow sequences; this system holds persistent state across scenes |
| **Mission State Machine** *(GDD exists)* | terminal outcome + reward payload `{outcome, optionalsCollected, elapsedTime}` — **once** | applies money/rep/XP magnitudes; returns nothing to the machine | Mission emits; **this system owns the magnitude formula** |
| **Office Computer / HUD UI** | purchase / claim / acquire intents | current funds, license stage, pending-reward block, shop availability, narrative-event flags | UI sends intents & displays; this system validates & owns truth |
| **Settlement UI** *(undesigned)* | — | per-mission `SettlementData` breakdown + outcome | this system produces; Settlement UI displays |
| **Equipment & Consumables / Shop** *(undesigned)* | purchase request (item, cost) | funds deduction or readable refusal | Shop defines catalog/cost; this system owns the wallet |
| **Narrative / Threat events** *(undesigned)* | — | pressure-threshold triggers → "send letter / visitor" event | this system emits the trigger; narrative system renders the event |

## Formulas

> All values below are **as-built** from `Assets/_Project/Scripts/Office/CompanyData.cs` and `Assets/_Project/Scripts/Mission/LostItemMissionManager.cs`. This GDD documents shipped behavior; tuning changes happen in those files (see Tuning Knobs).

**1. `settlement_reward`** — money/rep/XP applied to `CompanyState` on claim, by mission outcome.

Base magnitudes (per `LostItemMissionManager` serialized defaults):

| Outcome | money | reputation | experience |
|---|---|---|---|
| `FullSuccess` | +300 | +5 | +80 |
| `PartialReturn` | +60 | +0 | +15 |
| `Failure` | +20 | −2 | 0 |
| `Bonus evidence` *(additive)* | +90 | +1 | +20 |

Applied as: `Funds += base_money + bonus_money − overtime_money_penalty − wrong_item_penalty`; `Reputation += base_rep + bonus_rep − overtime_rep_penalty`; `Experience += (outcome == Failure ? 0 : base_xp + bonus_xp)`.

| Variable | Type | Range | Description |
|---|---|---|---|
| `base_money/rep/xp` | int | per table | outcome base (tuning knobs) |
| `bonus_*` | int | 0 or table | added only if optional evidence collected |
| `overtime_money_penalty` | int | 0+ | **owned by `MvpMissionClock`** — referenced, not defined here |
| `overtime_rep_penalty` | int | 0+ | owned by `MvpMissionClock` |
| `wrong_item_penalty` | int | 0+ | mission-specific (wrong homework) — referenced |

**Output range:** money net can be negative (penalties exceed base). **Example:** full success + bonus, no overtime → `+300 +90 = +390G`, `+5 +1 = +6 rep`, `+80 +20 = +100 XP`.

> ⚠️ **As-built vs design-doc conflict (flagged):** `docs/design-decisions.md` specifies Partial = **30–50%** of full pay. As-built Partial money is **60/300 = 20%**. Either the doc or the value should change — see Open Questions.

**2. `xp_to_next_level`** — internal office leveling (hidden from player).

`xp_to_next_level(L) = max(100, L × 300)` — level-up loop: `while L < 8 ∧ Experience ≥ xp_to_next_level(L): Experience −= xp_to_next_level(L); L += 1`.

| Variable | Type | Range | Description |
|---|---|---|---|
| `L` (`OfficeLevel`) | int | 1–8 | internal office level (caps at 8) |
| `Experience` | int | 0+ | accrued XP (success-only) |

**Output range:** threshold 300 (L1) … 2100 (L7); no level 9. **Example:** L1 with 380 XP → spend 300, L2, 80 XP remain.

**3. `pressure_update`** — internal takeover pressure, piecewise by outcome.

```
Success:  Pressure = max(0, Pressure − 25)
Partial:  gain = 12 + (Funds<0 ? 5 : 0) + (Reputation<0 ? 5 : 0);  Pressure = min(100, Pressure + gain)
Failure:  gain = 35 + (Funds<0 ? 15 : 0) + (Reputation<0 ? 10 : 0); Pressure = min(100, Pressure + gain)
```

| Variable | Type | Range | Description |
|---|---|---|---|
| `Pressure` | int | 0–100 | internal; never shown as a bar |
| `Funds<0`, `Reputation<0` | bool | — | "broke" / "disliked" surcharge gates |

**Output range:** 0–100 (clamped). **Example:** Pressure 60, partial return while broke (Funds<0) but liked → gain `12+5 = 17` → 77.

**4. `takeover_resolution`** — evaluated on a **Failure** settlement only (the threat ladder).

```
if Pressure < 100:                      → none
elif Funds ≥ 0 OR Reputation ≥ 0:       → none (solvent/liked shields you)
elif ¬HasUltimatum:                     → set HasUltimatum (final-warning letter)   [UltimatumIssued]
else:                                   → RESTRUCTURE                               [Restructured]
```

Restructure effects (soft loss, not game-over): `Debt += 500; OfficeLevel = max(1, OfficeLevel − 1); Funds = min(Funds, −500); Reputation = min(Reputation, −5); Experience = 0; CompletedLostItemJobs = 0; HasAcquiredTutorialOffice = false; Pressure = 35; clear ultimatum flags`.

**Output:** one of `{none, UltimatumIssued, Restructured}`. **Example:** Pressure 100, Funds −120, Rep −3, ultimatum already issued, team fails again → restructure.

**5. `tutorial_acquisition_cost`** — the MVP license-advance purchase.

`cost = TutorialOfficeValuation × 3 / 2 = 100 × 3 / 2 = 150G` (integer division). **Eligibility:** `OfficeLevel == 1 ∧ CompletedLostItemJobs ≥ 2 ∧ ¬HasAcquiredTutorialOffice ∧ Funds ≥ cost ∧ Pressure < 70`. On accept: `Funds −= 150; OfficeLevel = max(OfficeLevel, 2); Reputation += 1; Pressure = max(0, Pressure − 20); clear ultimatum`.

| Variable | Type | Range | Description |
|---|---|---|---|
| `TutorialOfficeValuation` | int | 100 (const) | level-0 office valuation |
| acquisition multiplier | rational | 3/2 = 1.5× | cost = valuation × 1.5 |

**Output:** 150G fixed at current tuning. **Example:** Funds 160, 2 clean jobs, pressure 40 → eligible; accept → Funds 10, OfficeLevel 2.

**Economic shape note:** start `−300G / 300 debt`; two clean full jobs `= +600G` → `+300G` net, comfortably affording the `150G` acquisition while leaving the debt backdrop intact. Partial returns (`60G`) cannot escape debt alone — they're a survival valve, not a growth path. This is the intended squeeze.

## Edge Cases

- **If a reward is claimed twice** (double `E`-press, network retry): **idempotent** — applied exactly once via `RewardsGranted`; the second claim is a no-op.
- **If a guest client sends a claim/purchase/acquire intent**: the server validates authority and is the sole writer; the guest's local `CompanyData` is display-only and is **never written to disk** while a guest. Unauthorized writes are rejected.
- **If funds are insufficient for a purchase**: **readable refusal, no state change** — no hotbar change, no partial purchase, funds untouched.
- **If a purchase would push `Funds` below 0**: **forbidden by default** (`allow_negative_purchase = false`). The squeeze is felt by *not affording gear*, not by buying into deeper debt.
- **If acquisition is accepted but funds dropped below cost between offer and accept** (e.g., a concurrent purchase): eligibility is **re-checked at accept time** (`CanAffordTutorialAcquisition`); if no longer affordable → **no-op refusal**, offer remains.
- **If `Pressure` reaches 100 but the company is solvent (`Funds ≥ 0`) or liked (`Reputation ≥ 0`)**: **no ultimatum** — solvency/standing shields you. Pressure stays pinned at 100 until a success drops it.
- **If a restructure would drop `OfficeLevel` below 1**: **clamped to 1** — the office is gutted but never erased (soft loss, not game-over).
- **If a success occurs while an ultimatum is active**: pressure falls by 25; if it drops below the threshold the **ultimatum flag clears** and the company recovers — the noose loosens.
- **If two reward payloads arrive for one run** (should never happen — Mission emits once): the second is **rejected** by `RewardsGranted` idempotency.
- **If the host disconnects mid-mission or before the reward is claimed**: host migration is **out of MVP scope** (per Mission GDD) → the session ends with **no settlement persisted**; the save reflects only the last committed HQ state. Documented limitation, not a silent corruption.
- **If the save file is missing or corrupt on load**: fall back to a fresh `NewState` (`−300G / 300 debt / level 1`). A one-time legacy `PlayerPrefs` save is imported if present, then deleted.
- **If the save schema version is newer than the build understands**: `Migrate()` handles known bumps (v1 is a no-op); an unknown future version is a documented migration gap, not silently mutated.
- **If `Experience` accrues past the level cap (`OfficeLevel 8`)**: the level-up loop stops; excess XP keeps accumulating with **no further effect** (harmless dead value at cap). *Minor: consider an XP sink or hard stop post-MVP.*
- **If overtime / wrong-item penalties exceed the base money reward**: **net money goes negative** and `Funds` decreases (deeper debt). Allowed — the penalty is meant to bite.
- **If bonus evidence was collected but the run ends in `Failure`**: the bonus is **forfeit** — failure pays only the failure base and zeroes XP; you didn't get out with the evidence. (Design intent: bonus rewards *extraction*, not collection.)
- **⚠️ Known gap — reputation is not clamped on settlement.** `ApplyMissionResult` adds reputation without clamping to the stated `−100…100` range (only restructure floors it). Over many runs reputation can drift out of range. **Resolution: clamp `Reputation` to `[−100, 100]` after every settlement.** Flagged for implementation fix + Open Questions.

## Dependencies

**Upstream — this system depends on:**

| System | Hard/Soft | Interface |
|---|---|---|
| **Networking** (ADR-0001, *implemented*) | **Hard** | Intent RPCs in (claim/purchase/acquire); `CompanyState` out via server-only writes + `ApplySnapshot` to clients. Server is sole writer/validator. |
| **Save / Persistence** *(inferred, no GDD)* | **Hard** | `CompanyData.Load/Save` — schema-versioned JSON via `SaveIO`, host-only disk write. Provisional contract until Save GDD authored. |
| **Scene Flow / Game State** *(inferred, no GDD)* | **Hard** | Persistent state survives HQ↔mission scene loads; pending reward exposed on HQ load; settlement on return. Provisional. |
| **Mission State Machine** *(GDD exists)* | **Hard** | Consumes the terminal outcome + reward payload `{outcome, optionalsCollected, elapsedTime}`, exactly once. Economy cannot settle without it. |

**Downstream — these depend on this system** *(all undesigned — contracts provisional):*

| System | Hard/Soft | What they consume |
|---|---|---|
| **HUD / Office Computer UI** | **Hard** | funds, license stage, pending-reward block, shop availability, narrative-event flags; sends purchase/claim/acquire intents. |
| **Settlement UI** | **Hard** | the per-mission `SettlementData` breakdown + outcome to render the results screen. |
| **Equipment & Consumables / Shop** | **Hard** | the wallet — purchase deducts funds; this system owns money truth, the shop owns the catalog/costs. |
| **Narrative / Threat events** | **Soft** | pressure-threshold triggers ("send letter / visitor"); reads nothing back. The economy works even if the narrative layer is absent (the threat just wouldn't render). |

**Bidirectional consistency notes:**
- The **Mission State Machine** GDD already lists "Office Economy / Settlement (Hard)" as a *downstream dependent of Mission* — i.e. Mission emits, Economy consumes. That is the same single direction stated here (Economy depends on Mission for the payload). **Consistent** ✅.
- When **HUD**, **Settlement UI**, **Shop**, and **Save** GDDs are authored, each must list "depends on Office Economy & Progression." Flagged for `/consistency-check`.
- **Hard vs soft**: the economy cannot function without Networking, Save, Scene Flow, or Mission (no income event). It *can* function without the Narrative layer — pressure logic still runs; only the letter/visitor presentation would be missing.

## Tuning Knobs

| Knob | Default | Safe range | Too low → | Too high → | Interacts with |
|---|---|---|---|---|---|
| `start_funds` | −300 | −500…0 | not broke enough; no squeeze | impossible opening | `start_debt`, full reward |
| `start_debt` | 300 | 0…1000 | debt backdrop disappears | hopeless tone | restructure debt add |
| `full_money_reward` | 300 | 100…600 | can't escape debt in 2 jobs | trivializes the squeeze | acquisition cost, debt |
| `full_rep_reward` | 5 | 1…15 | internal rep stagnates | tiers unlock too fast | job pool, threat shield |
| `full_xp_reward` | 80 | 20…200 | leveling crawls | leveling races | `xp_curve_multiplier` |
| `partial_money_reward` | 60 | 30…150 | partial worthless, no valve | partial dominates; kills "push for full" | **pillar-4 tension** (see conflict below) |
| `partial_xp_reward` | 15 | 0…40 | — | partial rivals full | leveling |
| `failure_money` | 20 | 0…50 | brutal | failure painless | pressure spiral |
| `failure_rep` | −2 | −5…0 | rep punishment toothless | death-spirals standing | threat shield |
| `bonus_money` | 90 | 30…150 | no reason to grab evidence | optionals dominate the run | mission optional design |
| `tutorial_office_valuation` | 100 | 50…300 | acquisition trivial | acquisition unreachable | acquisition cost = ×1.5 |
| `acquisition_multiplier` | 3/2 (1.5×) | 1.0…2.0 | — | acquisition too expensive | acquisition cost |
| `acquisition_pressure_gate` | 70 | 50…90 | acquisition rarely offered | always available | `pressure_update` |
| `pressure_success_relief` | 25 | 10…40 | pressure never falls | one win erases the threat | threat ladder |
| `pressure_partial_base` | 12 | 5…25 | partial feels free | partial too punishing | pillar-4 valve |
| `pressure_failure_base` | 35 | 20…50 | failure toothless | 3 fails = restructure (too fast) | restructure cadence |
| `broke_surcharge_partial / _failure` | 5 / 15 | 0…25 | broke penalty toothless | death-spiral when poor | `Funds<0` gate |
| `disliked_surcharge_partial / _failure` | 5 / 10 | 0…20 | rep penalty toothless | death-spiral when disliked | `Reputation<0` gate |
| `restructure_debt_add` | 500 | 100…1000 | restructure painless | restructure unrecoverable | soft-loss feel |
| `restructure_pressure_reset` | 35 | 0…60 | restructure → instant re-threat | restructure → long safe window | threat cadence |
| `level_cap` | 8 | 4…12 | content gated early | dead levels | `UnlockedCategoryCount` |
| `xp_curve_multiplier` | 300 (`max(100, L×300)`) | 150…500 | trivial leveling | grind | leveling pace |
| `allow_negative_purchase` | `false` | bool | — | buy into debt; softens squeeze | shop |
| `reputation_clamp` | `[−100, 100]` *(proposed)* | bounds | — | rep drifts out of range (current gap) | rep-driven gates |

**Source-of-truth notes** (referenced, **not redefined** here — change them in their owning system):
- `overtime_money_penalty` / `overtime_rep_penalty` — **owned by `MvpMissionClock`** (Hazard/clock).
- `wrong_item_penalty` — mission-specific (Lost-Item mission config).
- `mission_clock_duration`, `optional_objective_bonus` magnitude trigger — **owned by Mission/Hazard GDDs**.

> ⚠️ **Tuning conflict (carried from Formulas):** `partial_money_reward = 60` is **20%** of `full_money_reward`. `docs/design-decisions.md` calls for **30–50%** (i.e. 90–150). Raising it to ~120 would match the doc and strengthen the partial-return valve — but check it doesn't erode the "push for full" tension. Decision deferred to Open Questions / PM.

## Visual/Audio Requirements

> This system owns *state*, not pixels. Its visual/audio expression lives in the **HUD / Office Computer UI** and **Settlement UI** GDDs and the **Art Bible**. Direction relevant to the economy:
> - **No numeric pressure bar anywhere** (anti-pillar). Takeover threat is rendered as a **letter on the desk** or a **visitor in a good coat** — see Narrative/Threat events.
> - **Settlement screen carries the satire** via client usage notes and deduction clauses (pillar 3 *the contract speaks*), not narration.
> - Money/license read in the office-computer terminal's loud register (dispatch green / amber); the in-mission HUD stays quiet (per `design/ux/hud.md`).
> - Audio cues (claim "ka-chunk," stamp thud, the dread of the ultimatum letter) are specified in the **Audio** GDD; this GDD only names the *events* that need a cue: claim, purchase, refused-purchase, acquisition accept, ultimatum issued, restructure.

## UI Requirements

The office computer is the economy's primary surface; the settlement screen is its verdict. Requirements (detailed layout deferred to UX specs):

- **Office computer terminal** must always show: **funds** (with debt implied by negative/red), **license stage**, current job availability, the **pending-reward block** (blocks shopping until claimed), and shop commands (`F1`–`F4`). It must **not** show reputation, office level, or a pressure number.
- **Highest-priority `E` action** order: claim pending reward → accept available job → accept tutorial acquisition (when unlocked & affordable).
- **Shop feedback**: purchased gear appears immediately in the hotbar with icon + quantity; insufficient funds shows readable refusal; unpurchased gear never appears.
- **Settlement screen**: shows the per-mission `SettlementData` breakdown (income, deductions, net) and outcome, with satire delivered through client usage notes / deduction clauses.
- **Narrative threat surface**: the ultimatum and restructure are presented as a **letter/visitor event in HQ**, not a UI meter.

> **📌 UX Flag — Office / HQ Economy & Progression**: This system has UI requirements. In Pre-Production, run `/ux-design` to create UX specs for the **office computer terminal** and **settlement screen** before writing epics. Stories that reference this UI should cite the UX spec, not this GDD directly. Note this in the systems index.

## Acceptance Criteria

**Settlement & idempotency**
- **GIVEN** a returned mission with a `FullSuccess` payload, **WHEN** the player presses `E` on the office computer to claim, **THEN** `Funds` increases by `300` (plus bonus/minus penalties), `Reputation` by `5`, `Experience` by `80`, exactly once.
- **GIVEN** a reward already claimed, **WHEN** the player presses `E` again (or a network retry fires), **THEN** no further change occurs (`RewardsGranted` idempotent).
- **GIVEN** a `PartialReturn` payload, **WHEN** claimed, **THEN** `Funds += 60`, `Experience += 15`, `Reputation += 0`.
- **GIVEN** a `Failure` payload, **WHEN** claimed, **THEN** `Funds += 20`, `Reputation −= 2`, `Experience` unchanged (failure grants no XP).
- **GIVEN** optional evidence was collected on a successful run, **WHEN** claimed, **THEN** the bonus (`+90G / +1 rep / +20 XP`) stacks onto the success base.

**Host authority & sync**
- **GIVEN** a 2-player session, **WHEN** the host's economy state changes at settlement, **THEN** the client's displayed funds/license match the host's within one sync tick (`ApplySnapshot`), and the client never writes its own save.
- **GIVEN** a guest client, **WHEN** it sends a purchase/claim/acquire intent it is not authorized for, **THEN** the server rejects it and no ledger change occurs.

**Shop**
- **GIVEN** sufficient funds and no pending reward, **WHEN** the player buys gear (`F1`–`F4`), **THEN** `Funds` decreases by the item cost and the item appears in the hotbar.
- **GIVEN** insufficient funds, **WHEN** the player attempts a purchase, **THEN** readable feedback shows and nothing changes (no hotbar change, funds untouched).
- **GIVEN** a pending reward is unclaimed, **WHEN** the player attempts to shop, **THEN** the computer blocks shopping until the reward is claimed.

**Leveling (internal)**
- **GIVEN** `OfficeLevel 1` with `Experience ≥ 300` after a settlement, **WHEN** level-ups resolve, **THEN** `OfficeLevel` becomes 2 and `Experience` carries the remainder; the loop never exceeds `level_cap 8`.

**Takeover ladder (high-risk)**
- **GIVEN** `Pressure = 100 ∧ Funds < 0 ∧ Reputation < 0` with no prior ultimatum, **WHEN** a `Failure` settles, **THEN** an ultimatum is issued (a narrative letter/visitor flag is set) and **no** numeric pressure bar is rendered in any UI.
- **GIVEN** an active ultimatum under the same bad conditions, **WHEN** another `Failure` settles, **THEN** a restructure executes: `Debt += 500`, `OfficeLevel −= 1` (floored at 1), `Funds ≤ −500`, `Reputation ≤ −5`, `Experience`/`CompletedLostItemJobs` reset, `Pressure = 35`, and play continues (no game-over screen).
- **GIVEN** `Pressure = 100` but `Funds ≥ 0` **or** `Reputation ≥ 0`, **WHEN** a `Failure` settles, **THEN** **no** ultimatum is issued (solvency/standing shields the company).
- **GIVEN** an active ultimatum, **WHEN** a `Success` drops pressure below threshold, **THEN** the ultimatum flag clears.

**Tutorial acquisition (MVP license advance)**
- **GIVEN** `OfficeLevel 1`, two completed lost-item jobs, `Pressure < 70`, and `Funds ≥ 150`, **WHEN** the player accepts the acquisition prompt, **THEN** `Funds −= 150`, `OfficeLevel` becomes 2, `Pressure −= 20`, and the next category unlocks as locked future content — exactly once.
- **GIVEN** the same prompt but `Funds < 150` at accept time, **WHEN** accept is pressed, **THEN** it is refused with no state change and the offer remains.

**Persistence**
- **GIVEN** a saved company, **WHEN** the host relaunches, **THEN** the loaded `CompanyState` matches the last host-committed state (funds, debt, level, pressure, flags).
- **GIVEN** a missing/corrupt save, **WHEN** the game loads, **THEN** it falls back to `NewState` (`−300G / 300 debt / level 1`) without crashing.

**Performance**
- **GIVEN** any settlement or purchase, **WHEN** it applies, **THEN** the operation completes within frame budget (negligible CPU; JSON save I/O must not stall the main thread beyond ~2 ms on the host).

## Open Questions

| # | Question | Owner | Target |
|---|---|---|---|
| 1 | **Partial-pay %**: as-built `60G` (20%) vs `design-decisions.md` 30–50%. Raise `partial_money_reward` to ~90–120, or amend the doc? | PM Yan Dai | before balance pass |
| 2 | **Full license ladder**: how do the five license stages advance beyond the MVP tutorial-acquisition stand-in? (Story-mission gates? internal-level gates?) | PM + zeno | post-MVP design |
| 3 | **Reputation clamp gap**: `ApplyMissionResult` doesn't clamp `Reputation` to `[−100,100]`. Confirm fix + add EditMode test. | gameplay-programmer | next economy story |
| 4 | **Narrative/Threat events system**: the letter/visitor presentation has no GDD yet. When authored, define the event payload and HQ spawn. | narrative-director | when threat content starts |
| 5 | **XP at level cap**: `Experience` keeps accruing past `OfficeLevel 8` with no effect. Add a hard stop or a money/prestige sink? | systems-designer | post-MVP |
| 6 | **Old doc retirement**: this GDD is meant to replace the progression model in `docs/mvp-core-loop.md`. When approved, add a deprecation header pointing here. | PM | after design-review |
| 7 | **Reputation's surface absence**: confirmed internal-only — but does *any* player-facing hint of standing exist (client tone, jobs offered)? | PM + hilbert | UX pass |
