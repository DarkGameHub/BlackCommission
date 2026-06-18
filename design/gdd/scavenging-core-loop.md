# Scavenging Core Loop — Unified Mission Model

> **Status**: DRAFT for PM review — 2026-06-17. Not yet canon.
> Supersedes the prior draft (same file path) which incorrectly re-imported
> reputation / XP / takeover-pressure from the stale `office-economy-progression.md`.
> Becomes canon once PM approves and the code + doc migration tasks below are executed.
>
> **Author**: Yan Dai (PM, all final decisions) + Claude (synthesis of game-designer,
> level-designer, creative-director proposals, 2026-06-17)
>
> **Implements Pillars**: 1 (the broke office) · 2 (the dispatch van ritual) ·
> 3 (the contract speaks) · 4 (partial settlement choices) · 5 (the moral slope) ·
> co-op extraction tension
>
> **Priority / Layer**: MVP / Core
>
> **Related docs**:
> - `design/quick-specs/scavenging-item-system-2026-06-16.md` — item pickup, weight classes,
>   commissioned target overlay, one-dispute mechanics (authoritative for those sub-specs)
> - `design/gdd/mission-state-machine.md` — the run state machine (REVISED by this doc)
> - `design/levels/abandoned-tower-earth-coast-01.md` — tower map (reframed by this doc)
> - `docs/world-background-2098.md` — lore, ending design, monthly safety cost context

---

## 0. PM Decisions Locked (2026-06-17, round 2) — governs the body below

> Where any section below conflicts with this block, **this block wins** (the body was drafted
> around a single "commissioned target item"; that model is now superseded — see D-A).

- **D-A — Scavenging is the theme of EVERY mission type. No single hero/fetch item in any type.**
  A *Commissioned Job* is defined by its **client**, not a target object: the commission text states
  the client's preferences, certain item **categories** pay a client-preference multiplier, and the
  satire lands at settlement through the client's per-item usage notes (Pillar 3). There is **no
  mandatory or single "commissioned target item."** This DROPS, throughout the doc: the guaranteed-
  target spawn, the `IsCommissionedTarget` single-item flag, and the `Partial` outcome kind.
  → Outcome kinds are now only **Success** (voluntary departure, any haul) and **Failure** (all downed).
  → "Less vs more" (Pillar 4) is the continuous haul size under the weight/threat levers, not a missed target.
- **D-B — The tower (Earth Coast No.1) ships as a Commissioned Job, but with NO sales scale model (沙盘).**
  Its commission is a Mars client who pays more for residents' personal effects / civic documents /
  residential fixtures (nostalgia for the unbuilt Earth dream). The 沙盘 is cut entirely (not even a bonus item).
- **D-C — Economy is money-only. APPROVED: gut `CompanyState`/`MvpPendingReward`/`OfficeComputer`** of
  Reputation, Experience/OfficeLevel/level-ups, and HostileTakeoverPressure + the takeover FSM (§9, load-bearing).
- **D-D — `failurePayoutRate` = 0.5 (LOCKED).**
- **D-E — Monthly safety cost is a REAL recurring deduction from `funds`, keyed to license stage
  (40G→120G), surfaced as a deduction LINE ITEM on the settlement/expense ledger (Pillar 3 grammar) —
  never a month-counter or pressure bar.**
- **D-F — Eco-column prop: repurpose as a Heavy residential-fixture scavenge item (not deleted).**

## 1. Overview

Black Commission is a **scavenging extraction co-op**: a near-bankrupt commission office sends
1–4 workers into strange, abandoned civic sites to haul out anything worth money under a shared
van weight limit and rising danger, then settle the haul against debt back at the office. Every
run follows the same shape — **search loose → fill the van → decide when to bail → settle the
verdict** — and the tension is continuous, not gated by grabbing one mandatory object.

This document unifies two implementations that had drifted apart: the scavenging system
(multi-item pickup, hidden value, van weight limit, per-item settlement) and the old
single-objective mission (the eco-column, binary full/partial settlement). It resolves them
into **one loop**: scavenging is the substrate of every mission; a **commissioned target** is an
optional high-value bonus item layered on top for some jobs, and it never gates completion.
The eco-column designated objective is cut. The mechanics it originally justified — the power
gate, heavy two-hand carry, the Infected Site Inspector — are retained because they serve the
scavenging tension directly.

The TARGET is that settlement produces **money and nothing else**: funds deposited into
`CompanyState.funds`, with no reputation deltas, no XP, no takeover-pressure-counter outputs.
Takeover survives only as narrative flavor (letters, visitors) tied to `CompanyState.funds`,
never to a settlement-driven score.

> ✅ **DONE 2026-06-17 — the economy is now money-only in CODE, verified.** `CompanyState`,
> `CompanyData`, `MvpPendingReward`, `OfficeComputer`, `OfficeTaskDefinition`, `MissionRewardCalculator`,
> `MvpMissionClock`, `TowerMissionManager` were gutted of Reputation / Experience / OfficeLevel /
> level-ups / HostileTakeoverPressure + the takeover FSM. `CompanyState.ApplyMissionResult(success,
> money, elapsed, …)` now moves `Funds` + the job counters only. Save schema bumped to v2 (old saves
> load — removed JSON keys are ignored). Job gating no longer uses OfficeLevel/Reputation (all jobs
> available; `// TODO: gate by license stage`). Verified in the live editor: **0 compile errors;
> 126/126 game-code EditMode tests pass** (rep/pressure/level-up tests deleted; money-only tests
> rewritten). The takeover-pressure counter no longer exists anywhere in the settlement path.

## 2. Player Fantasy

> "You are packing up your world, piece by piece, and shipping it to the people who
> abandoned it — because the bill is due." — Creative Director framing, 2026-06-17

The player fantasy is **grave-robbing the ruins of a civilization that left, to make this
month's rent**. You are not a hero retrieving a MacGuffin; you are a broke contractor with a
flashlight and a van that only holds so much, picking through what people abandoned and
judging — under time pressure and in the dark — what's worth carrying back.

The **pillar-4 argument ("do we leave now?")** is no longer a one-time flip when an object is
grabbed. It is a **standing, every-item negotiation** triggered by three concurrent levers:

**Lever 1 — Van Weight (primary).** The van has a shared capacity of 12 units (default). An
item is banked the moment it is set down inside the cargo zone and fits. When the van is full,
new items are rejected; the team must explicitly unload something to make room. Every new Heavy
item (4 units = one-third of default capacity) is a concrete displacement decision: "Is this
fixture worth dropping the three Light items we already loaded?"

**Lever 2 — Banked vs. Carried (risk).** Items deposited in the van are unconditionally safe —
they come home even if the run ends badly. Items held in hand when the crew goes down are
dropped in place and are never stowed. This makes every trip back to the van a real risk-
management choice: "bank what we have and go" versus "push one more room with the item in hand
and risk losing it — and ourselves — if the inspector catches us."

**Lever 3 — Threat Escalation (clock).** The Infected Site Inspector starts dormant (patrol)
and becomes actively hostile as time on site accumulates. The scavenging loop only reads this
lever's outcome (forced departure or all-downed). The longer the team stays, the more they
can take and the more dangerous staying becomes. Greed and survival pull opposite ways.

**MDA Aesthetics served:** Challenge (weight limit as finite resource under rising threat);
Expression (team negotiates its own risk tolerance in real time); Narrative (the settlement
reveal gives each item a story and lands the satire — "the punchline lands at settlement, not
on the map," `docs/world-background-2098.md`).

**SDT — Autonomy:** every departure is voluntary. The depart lever is always reachable from the
van. No mandatory objective forces a specific route or action. Players choose their risk horizon.

**SDT — Competence:** the hidden value system rewards players who read the commission text
correctly and build a mental model of which categories tend to be worth more per weight unit.
The per-item settlement reveal is immediate competence feedback: "we were right about the
ledger, wrong about the fixture."

## 3. Detailed Rules

### 3.1 The Loop (the dispatch van ritual — Pillar 2)

`HQ office → office computer (accept job / buy gear) → board dispatch van → in-van transit →
mission site → scavenge + deposit into the van → depart (voluntary) → van return → HQ
settlement → debt ledger updates.`

This ritual is the same shape for Free Salvage, Commissioned Job, and Black Commission. The
mission type changes the client, the satire register, and whether a bonus target exists — not
the loop structure.

### 3.2 Scavenging Baseline (every mission type)

Per `design/quick-specs/scavenging-item-system-2026-06-16.md` — these rules apply to all
missions without exception:

1. Items show **name, weight class, and visual category only** — never a price during the run.
2. **Hidden value**: each item has a `baseValue` (integer) the host reads only at settlement.
3. **Weight classes**: Light = 1, Medium = 2, Heavy = 4 van units. Heavy requires two-hand
   carry (hotbar locked, carrier speed 0.55×).
4. **Van weight limit** (shared, default 12 units): a set-down item loads into the cargo bay
   **only if it fits**; `ScavengeCargoZone.ScanForDeposits()` early-exits on `manifest.IsFull`
   when the van is full. The team decides what stays behind.
5. **Deposit = set the item down inside the van cargo zone.** `ScavengeCargoZone` is
   server-authoritative; only the host stows and despawns items. Deposited items are
   **banked** (safe regardless of subsequent run outcome).
6. **Light items** may be pocketed (up to 2 per player); Medium items occupy one hand; Heavy
   items require two hands and lock the hotbar.

### 3.3 The Designated Objective Is Removed

There is **no mandatory objective item** and **no "objective pickup" flip** that changes the
mission state. The search-to-extract transition is produced by weight filling and threat rising,
not by grabbing one object. This revises `mission-state-machine.md` rules 3–6 (see §9).

### 3.4 Commissioned Target — Optional Bonus Overlay

A job *may* designate **one** specific item as a commissioned target:

- If found and deposited in the van: **bonus pay** at settlement
  (`payout_t = round(baseValue_t × cond_t × commissionedBonusMultiplier)` where default
  multiplier = 1.4). `SettlementResult.CommissionedTargetDelivered == true`.
- If not found or not deposited: **no penalty to the salvage** — the run settles on whatever
  was hauled. Finding the commissioned target should feel like a good run; not finding it should
  not feel like failure.
- The commissioned target **never gates completion**. It is the seat of the client satire:
  the Mars client's "intended use" is revealed at settlement for this item above all others
  (Pillar 3 — the contract speaks).
- `LootSpawnPlanner` **must guarantee** the commissioned target spawns in every Commissioned
  Job and Black Commission run — players cannot deliver something that wasn't on the map. The
  guarantee is: if mission type is `Commissioned` or `BlackCommission`, the target item is
  always placed in a reachable location before run start.

### 3.5 Mission Taxonomy (Pillar 5 — The Moral Slope)

Pay and moral darkness move together. Every tier is the same scavenging loop; only the client
register and bonus structure change.

| Type | Chinese | Commissioned Target | Settlement Framing | Moral Register |
|---|---|---|---|---|
| **Free Salvage** | 自由采集 | None | Every voluntary return = Success. Market-rate estimates shown per category (not per item). | Neutral — just paying the bills |
| **Commissioned Job** | 指定委托 | One bonus item | Delivered target = Success; departed without target = Partial; all downed = Failure | Satirical — the client's stated use lands at settlement |
| **Black Commission** | 黑色委托 | One morally-loaded target | Same mechanics as Commissioned Job | Dark — the client identity or intended use implicates something larger |

**Falsifiability test (Pillar 5 design test):** if you can remove the mission type label and
the player cannot tell Free Salvage from Black Commission by reading the commission text and
the settlement notes, the moral slope has failed. The text must do the work. The game does not
state moral judgments; the contracts do.

### 3.6 Run State Machine (Revised)

As implemented in `ScavengeMissionLogic.cs`:

```
InProgress → Settled   (voluntary departure via depart lever)
InProgress → Failed    (whole crew downed — ScavengeMissionManager.AllPlayersDowned())
```

Terminal once it leaves `InProgress`. Single-fire — `ScavengeMissionLogic.IsTerminal` blocks
any second call to `ResolveDeparture()` or `NotifyAllDowned()`.

**Voluntary depart** → state `Settled`. `OutcomeKind` is computed at settlement time:
- `OutcomeKind.Partial` if `missionType ∈ {Commissioned, BlackCommission}` and
  `SettlementResult.CommissionedTargetDelivered == false`
- `OutcomeKind.Success` otherwise (including all Free Salvage departures)

**Whole crew downed** → state `Failed`. `OutcomeKind.Failure`. The banked cargo settles at
`failurePayoutRate` (see §4); items being carried at the moment of failure are lost.

`OutcomeKind` drives **exactly two things**: the settlement card label, and whether
`failurePayoutRate` is applied to the money total. Nothing else. It does not drive reputation
deltas, pressure counters, or XP — those systems do not exist.

### 3.7 Settlement

Settlement is host-authoritative, applied exactly once via `MvpPendingReward.Set()`.
The money payload is the scavenged sum from §4. `ScavengeMissionManager.Settle()` calls
`ScavengeCargoZone.SettleCargo()`, which runs `ScavengeSettlementCalculator.Settle()` on
`VanCargoManifest`, then calls `ApplyResultLocally()` with the computed money total.

**The per-item settlement reveal** (each item's price and the client's authored usage note,
displayed in emotional weight order, with the one-dispute step) is specified in
`design/quick-specs/scavenging-item-system-2026-06-16.md` §§4–6. This is **Core content,
not a deferred Presentation task**: it is Pillar 3 ("the contract speaks") in its most direct
mechanical expression. The current build shows a total-only settlement card; the per-item
reveal is the next required build milestone for the loop to carry its full satirical weight.

## 4. Formulas

All math as implemented in `ScavengeSettlementCalculator.cs`. All values live in
`Assets/Resources/Config/ScavengingConfig.asset` — none are hardcoded.

### Variable Definitions

| Symbol | Type | Source | Valid Range | Description |
|---|---|---|---|---|
| `n` | int | `VanCargoManifest.Count` | 0 to itemsPerMapInstance | Number of items banked in the van |
| `baseValue_i` | int | `ScavengeItemDefinition.baseValue` | 1–500 | Hidden during run; revealed at settlement |
| `cond_i` | float | `ItemCondition` enum | {1.0, 0.7, 0.4} | Good = 1.0, Worn = 0.7, Damaged = 0.4 (locked PM 2026-06-17) |
| `bonus_i` | float | `item.IsCommissionedTarget` | 1.0 or `commissionedBonusMultiplier` | 1.0 for all items; `commissionedBonusMultiplier` for the commissioned target |
| `commissionedBonusMultiplier` | float | `ScavengingConfig` | 1.2–1.8 (default 1.4) | Applied only to the commissioned target item |
| `failurePayoutRate` | float | `ScavengingConfig` | 0.0–1.0 (default 0.5, PM pending) | Fraction of banked cargo value paid out on all-downed failure |

### Per-Item Payout

```
payout_i = round( baseValue_i × cond_i × bonus_i )
```

Rounding is `MidpointRounding.AwayFromZero` (matching `ScavengeSettlementCalculator` line 84).
Negative `baseValue` is clamped to 0 before multiplication.

**Note on commissioned bonus decomposition**: the bonus is applied as a full multiplier to the
item's `baseValue × condition` product, not as a separate additive delta. The settlement UI
may optionally display the incremental bonus as a separate line
(`bonus_delta = payout_i - round(baseValue_i × cond_i)`) to make the commission's value
legible during the reveal sequence — but the arithmetic is identical either way.

### Salvage Sum

```
salvage = Σ payout_i    for i in 1..n
```

### Final Money by Outcome Kind

```
OutcomeKind.Success  (Free Salvage or commissioned target delivered)  : money = salvage
OutcomeKind.Partial  (commissioned target not delivered)              : money = salvage
OutcomeKind.Failure  (all downed)                                     : money = round( salvage × failurePayoutRate )
```

**Note — Partial vs Success money**: Partial is not penalized beyond the missed commissioned
bonus. A Partial haul pays the full salvage sum. "Less" for Partial means the bonus was not
earned (the client's specific request was not fulfilled), not that existing cargo is deducted.
This preserves Pillar 4 without requiring a reputation cost: the partial choice is an economic
trade-off, not a punishment.

### Worked Example — Commissioned Job, Target Delivered

Items in `VanCargoManifest`:
- Personal correspondence (baseValue 60, Worn): `round(60 × 0.7 × 1.0)` = 42G
- Family photograph (baseValue 40, Good): `round(40 × 1.0 × 1.0)` = 40G
- Sales scale model, commissioned target (baseValue 120, Good): `round(120 × 1.0 × 1.4)` = 168G

`salvage` = 42 + 40 + 168 = 250G. State = Settled. Target delivered. → `money` = **250G**.

### Worked Example — Same Run, All Downed Before Van

Banked at moment of failure: correspondence (42G) + photograph (40G) = 82G salvage.
Scale model was in hand, not banked → lost.
`failurePayoutRate` = 0.5 → `money` = `round(82 × 0.5)` = **41G**.

### Monthly Safety Cost Sink (Narrative Economy Context)

Per `docs/world-background-2098.md` (Ending Design Philosophy): environmental safety
infrastructure — air filtration, contamination suits, pathogen screening — costs **40G/month
in Stage 1, rising to 120G/month by Stage 4** as Earth's infection zones expand. This is a
recurring **sink** on `CompanyState.funds` that creates the low-margin pressure the broke
office depends on (Pillar 1). The sink's formula and timing are owned by the economy system;
this loop produces the **faucet** (the `money` value per run) that must cover it. If the
economy GDD is authored separately, it must reference this formula and the sink-faucet
balance across the five license stages. See Open Questions #3.

## 5. Edge Cases

| Scenario | Exact Behavior |
|---|---|
| **Empty van on departure** | `salvage = 0`, `money = 0`. State = Settled. Outcome = Success (Free Salvage) or Partial (Commissioned, target not banked). A wasted run is a valid outcome. The debt still bites next month. |
| **Full van (12/12), team wants to bank one more item** | `ScavengeCargoZone.ScanForDeposits()` early-exits because `manifest.IsFull == true`. New item remains on the floor. Team must `VanCargoManifest.TryUnload(index)` to remove an existing item, creating floor space. No partial loading of a single item. |
| **Carrying an item when all downed** | `CarrySystem` drops the item at the carrier's location. It is never stowed. Banked items in `VanCargoManifest` at the moment `ScavengeMissionLogic.NotifyAllDowned()` fires are already safe; dropped items are lost. |
| **Commissioned target deposited, van then fills with other items** | Target is banked. `CommissionedTargetDelivered = true`. Remaining salvage simply did not fit. Bonus applies. |
| **Commissioned target not spawned on the map** | `LootSpawnPlanner` must guarantee the target spawns on every Commissioned/Black Commission run. If for any reason it fails (spawner bug), the run settles as Partial. This is a bug state, not a valid authored outcome — add a `Debug.LogError` in `LootSpawnPlanner` if the target slot is unfilled on mission type Commissioned or BlackCommission. |
| **Two players simultaneously set down items in a full-ish van** | `ScavengeCargoZone.ScanForDeposits()` runs on host at 0.25s intervals. Items are processed in the order `FindObjectsByType` returns them. First item that fits is stowed; if loading it fills the van (`manifest.IsFull`), the scan loop breaks and the second item is not stowed that tick. The second item remains on the floor for the next scan (0.25s later); if the van is now full, it stays on the floor. No corruption of manifest state. |
| **Settlement triggered twice (mashing depart / network retry)** | `ScavengeMissionLogic.IsTerminal` is `true` after first resolution. Both `ResolveDeparture()` and `NotifyAllDowned()` return `false` and are no-ops. `MvpPendingReward.Set()` is called only once. No double-credit. |
| **Solo offline (PreviewWalker / no NetworkManager)** | `HasAuthority` returns `true` via offline fallback in both `ScavengeCargoZone` and `ScavengeMissionManager`. Settlement runs locally. Math is identical. |
| **Late joiner during active run** | Receives replicated `LoadUnits`, `Capacity`, `ItemCount` from `NetworkVariable`s immediately. Cannot access items already despawned (stowed). Participates in the run normally from that point. |
| **Host disconnects mid-run** | Host migration is out of MVP scope (per existing `mission-state-machine.md` note). Session ends; no settlement fires; banked cargo is lost. This is a documented limitation, not a silent loss — the UI should not show a reward screen on the client. |
| **`vanWeightCapacity` set to 0 or negative (misconfiguration)** | `VanCargoManifest` would immediately report `IsFull`. All deposits rejected. Settlement = 0G. Add a `Debug.LogError` guard in `ScavengeCargoZone.EnsureManifest()` if `capacity <= 0`. |
| **`failurePayoutRate` set to 0.0** | Banked cargo pays nothing on failure. Valid as an authored difficulty mode, but must not be the default — players who banked items must feel the system acknowledged that choice. |
| **`failurePayoutRate` set to 1.0** | Failure pays full banked cargo value. Failure has no monetary sting for banked items. Acceptable as a difficulty-down option; not recommended as default. |
| **All players downed simultaneously on same tick as a valid departure attempt** | `ResolveDeparture()` is called first (depart trigger is player-driven, evaluated before the `AllPlayersDowned()` poll in `ScavengeMissionManager.Update()`). If the depart RPC arrived on the host in the same frame: first-writer wins. The 1-second `downedPollTimer` interval means simultaneous departure and downed is resolved in favor of departure (the lever was pulled). |
| **Commissioned target is a Heavy item, carrier is downed mid-carry** | Item drops at carrier's location (existing `CarrySystem` behavior). Any alive teammate can pick it up. If a teammate banks it before the run fails, `CommissionedTargetDelivered = true` and bonus applies. If no one banks it and the run fails, it is lost — not counted as delivered. |

## 6. Dependencies

### Upstream (this system depends on)

| System | Hard/Soft | Interface |
|---|---|---|
| `ScavengeCargoZone` (built) | **Hard** | Deposit gate, `VanCargoManifest` host, `SettleCargo()` call. Produces `SettlementResult` at departure. |
| `VanCargoManifest` + `ScavengeSettlementCalculator` (built, unit-tested) | **Hard** | Per-item math and manifest storage. The §4 formulas live here. |
| `ScavengeMissionLogic` (built, unit-tested) | **Hard** | Pure state machine: `InProgress → Settled | Failed`. Single-fire terminal. |
| `ScavengeMissionManager` (built) | **Hard** | Networked wrapper: host-authoritative settle, `AllPlayersDowned()` poll, RPC routing, `MvpPendingReward.Set()`. |
| `PlayerHealth` + `CarrySystem` | **Hard** | `IsDowned.Value` for failure detection; drop-on-down for carried item loss. |
| Networking (ADR-0001, NGO) | **Hard** | Host-authoritative state via `NetworkVariable`; intent via `ServerRpc`. |
| `LootSpawnPlanner` / `LootSpawner` | **Hard** | Supplies items to the map (including the commissioned target on Commissioned/Black Commission runs). Must guarantee target spawns. |
| `ScavengingConfig.asset` | **Hard** | All tuning knobs. Loaded via `Resources.Load<ScavengingConfig>`. |

### Downstream (these depend on this system)

| System | Hard/Soft | What They Consume |
|---|---|---|
| `CompanyState` | **Hard** | TARGET: receives `money` (integer) via `MvpPendingReward`; updates `funds` only. ⚠️ As-built it STILL also mutates Reputation / Experience→OfficeLevel / HostileTakeoverPressure + the takeover FSM — must be gutted to money-only (§9 blocker). |
| Settlement UI (`SettlementCardOverlay`) | **Hard** | `OutcomeKind` label + `money` total for current build; per-item reveal lines for future build. |
| HUD (`MvpHud`) | **Soft** | Live `LoadUnits`/`Capacity` from `ScavengeCargoZone.NetworkVariable`s for cargo ticket-strip. |
| `SceneFlow` / `SceneManager` | **Hard** | This system triggers HQ scene load on settlement completion. |
| Economy system (future GDD) | **Hard** | Receives money faucet value; owns monthly safety cost sink and net solvency. Must list this system as upstream. |
| `mission-state-machine.md` | **REVISED** | See §9 — the single-objective states are replaced by `InProgress → Settled | Failed`. |

### Bidirectional Note

When the economy GDD (`office-economy-progression.md` or its replacement) is revised for
money-only, it must list `ScavengeSettlementCalculator` as its upstream faucet source. When
`abandoned-tower-earth-coast-01.md` is reframed, it must list `ScavengeMissionManager` as
its mission authority (replacing `TowerMissionManager`).

## 7. Tuning Knobs

All values in `Assets/Resources/Config/ScavengingConfig.asset`. None are hardcoded in
gameplay code. Programmers: never embed these as magic numbers.

| Knob | Category | Default | Safe Range | Effect if Too Low | Effect if Too High | Rationale |
|---|---|---|---|---|---|---|
| `vanWeightCapacity` | Gate | 12 units | 8–20 | Too few items banked; run feels short; displacement decisions never occur | Van never fills; no displacement decisions; weight tension evaporates | Primary tension lever. 12 allows 3 Heavy or 12 Light; displacement moment typically hits at 8–10u with mixed item weights |
| `commissionedBonusMultiplier` | Feel | 1.4× | 1.2–1.8 | Commissioned target not worth prioritising; players treat all items identically | Players skip all other loot to hunt only the target; loop becomes a single-objective run in disguise | 1.4 means target earns 40% more than a same-baseValue non-target item of the same condition |
| `failurePayoutRate` | Gate | 0.5 (PM pending — see Open Questions) | 0.0–1.0 | At 0.0: banking cargo has no failure-recovery value; reduces tactical discipline | At 1.0: failure is nearly free; removes the cost of being downed | 0.5 makes failure sting while rewarding banking discipline; the break-even is "half of what I banked is better than nothing" |
| `conditionGood` | Curve | 1.0 | — (locked) | — | — | Locked PM 2026-06-17; do not retune without PM |
| `conditionWorn` | Curve | 0.7 | — (locked) | — | — | Same lock |
| `conditionDamaged` | Curve | 0.4 | — (locked) | — | — | Same lock |
| `itemsPerMapInstance` | Gate | 10–14 | 8–18 | Too few items; van weight never approached; no search phase | Too many items; van fills immediately; no late-run decisions | 10–14 ensures the team explores most of the map before the van is full at 12u capacity |
| `lightItemPocketSlots` | Feel | 2 | 1–3 | Players must trip to the van after every Light item | Players carry too many unbanked items; the risk lever is diluted | 2 pockets per player means a 4-player team can carry 8 Light items simultaneously before anyone must bank |

## 8. Acceptance Criteria

### Functional (pass/fail testable)

- [ ] Every mission type (Free Salvage / Commissioned Job / Black Commission) runs the same
      scavenge → deposit → depart → settle loop. No mandatory objective exists in any type.
- [ ] Settlement money equals the §4 formula output for the run's `OutcomeKind`. No reputation
      delta, XP delta, or takeover-pressure counter is incremented at any point in the path.
- [ ] A Commissioned Job departed without its target settles as Partial (salvage only, no
      penalty deduction, no failure label) and is not identical to Failure.
- [ ] Commissioned target delivered: bonus multiplier applies to that item's payout exactly
      once. `SettlementResult.CommissionedTargetDelivered == true`.
- [ ] Commissioned target absent from manifest: no bonus, no penalty, zero deduction.
- [ ] All-downed failure: `money = round(salvage × failurePayoutRate)`. Items carried (not
      banked) at moment of failure are lost. Banked items are settled at the reduced rate.
- [ ] No reference to a mandatory eco-column or single-objective remains in the runtime mission
      loop (code or scene).
- [ ] Host-authoritative: outcome and payout are identical on all clients in a 4-player run.
      A client cannot trigger settlement or alter the manifest.
- [ ] Settlement is idempotent: mashing the depart trigger or network retrying applies the
      payout exactly once.
- [ ] `LootSpawnPlanner` guarantees the commissioned target spawns on every Commissioned Job
      and Black Commission run. If it fails, `Debug.LogError` fires and the run settles as
      Partial (not a crash).
- [ ] Van weight display (cargo ticket-strip) shows correct `LoadUnits / Capacity` on all
      clients. Late joiners see the correct count within one replication cycle.
- [ ] `vanWeightCapacity <= 0` triggers a `Debug.LogError` in `ScavengeCargoZone.EnsureManifest()`
      and does not crash the run.

### Experiential (playtest criteria)

- [ ] In a 4-player session, the team has at least one explicit verbal argument about whether
      to leave or stay on the majority of runs. (Target: >70% of observed runs in playtest.)
- [ ] The van filling up feels like a decision, not an accident. Players can articulate why
      they left behind what they left behind.
- [ ] Settlement reveal makes at least one item's fate feel surprising or uncomfortable per
      run. (Validated once per-item reveal is built — see Open Questions #4.)
- [ ] Failure (all downed) feels punishing but not arbitrary. Players can identify what
      banking decision they regret.
- [ ] Solo play is viable — a careful solo player can complete a Free Salvage run without
      the monster being an instant-kill wall.

---

## 9. Supersedes / Migration Impact

### Design Documents to Revise on Approval

**`design/gdd/mission-state-machine.md`**
- **Remove**: `ObjectiveHeld` state, `ReturnArmed` state, `valid_pickup` predicate,
  `valid_complete` predicate, `valid_partial` arm-then-confirm two-step, the
  `FullSuccess`/`PartialReturn` terminal distinction.
- **Retain**: the idempotency rule, the `failure_trigger` predicate (`alivePlayers == 0`),
  host-authoritative validation principle.
- **Replace terminal states with**: `Settled` (voluntary departure) and `Failed` (all downed).
- **Add**: `OutcomeKind` field on the settlement payload (`Success | Partial | Failure`),
  computed at settlement time per §3.6 rules (not at a mid-run state transition).
- **Revise reward_payload**: formula 5 (`reward_payload`) now emits `{money, outcomekind}`.
  Remove the rep/XP/pressure fields — they do not exist.

**`design/gdd/office-economy-progression.md`** (stale — pre-overhaul)
- This document currently describes Reputation, OfficeLevel (1–8), and a 0–100
  takeover-pressure FSM. These systems are **removed** (PM decision 2026-06-17).
- The settlement formula section must be replaced with the §4 money-only formula from this doc.
- The economy document's remaining valid content: the monthly safety cost sink (40G→120G
  across stages), the five license stages, and the solvency/debt narrative. Everything
  else is superseded.
- This GDD revision is a blocker for any new economy-related implementation stories.

**`design/levels/abandoned-tower-earth-coast-01.md`**
- Reframe as a **salvage map**: the abandoned pre-sale tower contains three overlapping loot
  layers (residents' staged belongings, construction workers' personal effects, the sales
  company's own materials). All are valid scavenge targets.
- **Remove**: the eco-column as mandatory objective. The scale model (沙盘) is demoted to one
  Heavy scavenge item in the deep zone — optionally designated as a commissioned target for
  Commissioned Job runs, never mandatory.
- **Retain**: the power gate (gate to richer Floor 2 loot), Heavy two-hand carry (for
  high-value fixtures in the deep zone), the Infected Site Inspector (threat escalation lever),
  the evidence item in the worker dorm (optional bonus side payment).
- **Revise monster activation**: the Inspector no longer aggros on pickup of one specific item.
  It activates through time on Floor 2 and proximity to the deep zone, regardless of what the
  team is carrying. Greed is punished cumulatively, not at a single dramatic flip.
- **Item distribution**: Floor 1 arrival zone → 4–5 Light/Medium worker effects; Floor 1 deep
  (pre-power gate) → mixed loot including 1–2 Heavy items; Floor 2 (gate-locked) → highest-
  value items including Heavy show-flat fixtures and the commissioned target if designated.
- If the tower ships as pure Free Salvage first (PM decision pending — Open Questions #2), the
  map functions identically with `IsCommissionedTarget = false` on all item spawns.

### Code Changes Required (Awaiting PM Approval)

| Change | Priority | Detail |
|---|---|---|
| **Gut `CompanyState` / `MvpPendingReward` / `OfficeComputer` of the removed systems** | **Blocking (load-bearing)** | `CompanyState.ApplyMissionResult()` must mutate `Funds` (and debt) ONLY — delete `Reputation`, `Experience`/`OfficeLevel`/`TryApplyLevelUps()`, and `HostileTakeoverPressure`/`TryApplyHostileTakeover()` (the FSM). Sequence this BEFORE/WITH the tower rebuild. Note it touches **mission-access gating**: `OfficeComputer` currently gates which tasks appear on `OfficeLevel`/`Reputation` — replace that gating with license-stage/story gating (§Progression). This is the change that makes "money-only" true instead of aspirational. |
| **Wire item condition + commissioned-target into the deposit path** | **Blocking** | The §4 condition (Good/Worn/Damaged) and 1.4× bonus math is implemented in `ScavengeSettlementCalculator` but NEVER FED: `ScavengeCargoZone.TryStow()` hardcodes `ItemCondition.Good` and `IsCommissionedTarget=false`, and `ScavengeItem` has no such fields. Add `ItemCondition` + `IsCommissionedTarget` to `ScavengeItem` + `ConfigureServer()`; have `TryStow()` read them; have `LootSpawner` stamp condition + the target flag at spawn. Until done, §4's condition/bonus curves are "specified, not wired." |
| **Emit `OutcomeKind.Partial` in `ScavengeMissionManager.Settle()`** | **Blocking** | Today `Settle()` computes kind as only `Failed`-or-`Success` and never reads `SettlementResult.CommissionedTargetDelivered`. Add: when mission type ∈ {Commissioned, BlackCommission} and the target was not delivered → `MvpMissionResultKind.Partial`. `Partial` already exists and `SettlementCardOverlay` already renders it — only the kind selection is missing. |
| **Retire `TowerMissionManager`** | Blocking | The binary 300G full / 60G partial table is gone. `ScavengeMissionManager` becomes the sole mission manager for the tower scene. |
| **Remove eco-column objective wiring** | Blocking | `EcoColumnCarriable` and its connections removed (the prop may be repurposed as a Heavy scavenge item or deleted — Open Questions #3). |
| **Apply `failurePayoutRate` in `ScavengeMissionManager.Settle()`** | Blocking | Current code uses `MvpMissionResultKind.Failed` but does not apply the rate multiplier to `money`. The rate must be applied before `MvpPendingReward.Set()`. |
| **Tower scene rebuild** | High | Add `LootSpawner` + `ScavengeCargoZone` + `ScavengeMissionManager` + depart trigger. Remove eco-column/cargo-zone objective wiring. |
| **`IMissionAuthority` interface** | Medium | Unify `VanTransitOverlay` / `MissionVanExitPoint` so they reference `IMissionAuthority` (implemented by `ScavengeMissionManager`) rather than hard-binding to `TowerMissionManager`. |
| **`failurePayoutRate` knob in `ScavengingConfig`** | High | Add the field and expose in the inspector. Default 0.5 pending PM decision. |
| **`LootSpawnPlanner` guaranteed-target placement** | High | NET-NEW FEATURE, not a guard: today the planner is pure `(seed, anchors, pool, min, max) → random subset` with NO mission-type or target input. Extend it with a guaranteed-placement input (mission type + required target item id + a reserved reachable anchor) that runs BEFORE the random fill, preserving the deterministic-seed contract; `Debug.LogError` if no eligible anchor exists. |
| **Per-item settlement reveal** | Next milestone | `SettlementCardOverlay` expanded to show per-item lines + client note + condition + dispute button. Current build shows total only. |

---

## 10. Open Questions for PM

> **RESOLVED 2026-06-17 (see §0):** Q1 `failurePayoutRate` = **0.5** (D-D). Q2 tower = **Commissioned
> Job, no 沙盘** (D-B). Q3 monthly sink = **real per-stage deduction as a ledger line item** (D-E).
> Q5 eco-column prop = **repurpose as Heavy fixture** (D-F). The single-commissioned-target /
> guaranteed-spawn / Partial-kind code rows in §9 are **dropped** per D-A.
> **STILL OPEN: Q4** — confirm the per-item settlement reveal is a blocking story in the next sprint
> (Core, not Polish). Originals kept below for traceability:

1. **`failurePayoutRate` value** — when the whole crew is downed, does the banked cargo
   settle at full value (1.0 — "the van made it back on its own"), reduced (0.5 —
   "salvage incident fee"), or zero?
   *Recommendation: 0.5 — failure should sting, but banking discipline should be rewarded
   at some level.*

2. **Does the tower ship as a Commissioned Job (with the scale model as commissioned target)
   or as pure Free Salvage first?**
   *Recommendation: ship as a Commissioned Job — the scale model commission is the tower's
   strongest satirical hook ("a Mars family paying to retrieve the miniature of the apartment
   their parents bought but never lived in"). Pure Free Salvage works mechanically but loses
   the satire that makes the tower memorable as the first Commissioned Job map.*

3. **The monthly safety cost sink** — the world background specifies 40G/month (Stage 1) to
   120G/month (Stage 4). Is this an explicit recurring deduction from `CompanyState.funds` on
   a real timer, or is it expressed as a higher implicit debt pressure in narrative events?
   *This is the highest-priority unresolved economy gap: without a running sink, the "broke
   office" pressure never bites mechanically. It must be resolved before the economy GDD
   rewrite is authored. Recommend a real monthly deduction keyed to license stage.*

4. **Per-item settlement reveal — target build milestone?** The reveal (each item's price +
   client usage note in authored emotional order + one-dispute step) is Core, not optional,
   per this document. Confirm it is in the next sprint as a blocking story, not a Polish
   backlog item.

5. **Eco-column prop** — repurpose as a single high-value Heavy scavenge item (renamed to fit
   the tower's residential fixture category), or delete outright?
   *Recommendation: repurpose as a Heavy fixture in the deep zone. It already has a prop and
   physics collider; retexturing it as a staged apartment piece is lower cost than a new asset.*
