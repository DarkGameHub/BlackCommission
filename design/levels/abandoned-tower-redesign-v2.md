# Level Map v2 — Earth Coast No.1 Abandoned Tower (LC-density redesign)

> ⚠️ **SUPERSEDED by `abandoned-tower-redesign-v3.md`** (2026-06-07). v3 changes:
> irregular jagged footprint (no rectangle), **1 van + 1 fire exit** (not 3) with
> internal descent redundancy instead, and a simple exterior/forecourt. v2 kept for
> history; build from v3.

> Supersedes the layout in `abandoned-tower-floorplan.md`. Same fiction
> (`abandoned-tower-earth-coast-01.md`), same objective (scale model / power gate / heavy
> carry). What changes: **density, interconnection, and — the headline — topology is
> now seed-randomized, not just room contents.** Rationale and principles:
> `lethal-company-design-study.md`. **Method-reference only; no LC layout copied.**
>
> Status: **DRAFT for PM approval.** 3D rebuild starts only after sign-off.
> Decisions baked in (PM, 2026-06-07): keep 2 floors; 3 sizes S/M/L; ~15 rooms/floor
> (~30 total); oppression + escape; topology randomization.

---

## 0. Legend (LC-style information density)

```
[ROOM (size)]   size = S 4×4 / M 8×8 / L 12×8   (XL dropped)
1H(n)   one-handed loot worth n        2H(n)   two-handed loot worth n (slows carrier)
█ wall    ═/║ corridor (4m)    ╥/╠ door (2m)    ▒ shaft void (double-height)
◆ MAIN entrance (van side)    ▲ FIRE EXIT (escape valve to surface/lower)
★ landmark/beacon    ☠ monster nest    ⚡ power gate    ! story beat    ? optional
~~~ SEED-TOGGLE connector (open OR rubble-blocked per seed — never on critical path)
```

Loot values are **relative tuning placeholders** (BC settlement units), set so a
careful 15-min run nets enough to matter; balance later with `/balance-check`.

---

## 1. What makes this version not "child's play"

1. **Seed-randomized topology** — a finite set of `~~~` connectors open/close per run.
   Loops, shortcuts and dead-ends differ every time → players get lost, can't memorize
   the building, tension survives repeat runs. (Critical path is *always* kept open.)
2. **Dense graph, not a spine** — every floor is a **loop + branches**, not a corridor
   with rooms hung off it. Multiple routes between any two points.
3. **Many escape valves** — 1 main entrance + **3 fire exits** so you can always *try*
   to flee under pressure, but you surface somewhere unfamiliar.
4. **Repetition + 2 beacons** — homogeneous concrete/scaffold (weak landmarks =
   disorientation) anchored by exactly two strong landmarks (lit show-flat + stair
   towers) so the team isn't *hopelessly* lost.
5. **Risk gradient + objective↔exit separation** preserved and sharpened.

Envelope per floor: **48 m (x, east) × 32 m (z, north)** = 12 × 8 grid (G = 4 m).
Stairs vertically aligned across floors (mandatory). Van forecourt south of z = 0.

---

## 2. FLOOR 1 — Ground / Arrival (low → med risk: learn, gather, restore power)

```
 z32 ┌───────────────┬─────────┬─────────┬─────────┬───────┐
     │  WAREHOUSE (L) │ DORM(M) │CANTEEN  │FOREMAN  │       │
     │   2H(140)      │ !EVID   │  (M)    │ (M)     │ STAIR │
     │   batteries    │ Isolation Notice│ 1H(40)  │ 1H(90)  │   A   │
 z24 ╞═══════╤════════╪════╤════╪════╤════╪════╤════╡ (4×8) │ ▲ Stair-A also
     ║       ~~~      ║    ~~~  ║    ~~~  ║    ║    ║  FAST  │   = fast/exposed
     ║   CENTRAL CONSTRUCTION HALL (L hub)      ║ EXPOSED   │   descent later
     ║   ........... ▒▒▒ SHAFT ▒▒▒ ...........  ║    ║    ║        │
 z16 ║       ║       ▒ (see up to F2)▒      ║   ╞════╧════╡
     ║ STAIR ║  ╥           ▒▒▒          ╥  ║   │ LOADING │
     ║   B   ╠══╪═══════════════════════╪══╣   │ DOCK(M) │
     ║ (4×8) ║  ║  WORKSHOP(M)  SAMPLE   ║  ║   │ 2H(110) │
 z12 ║ DIM   ╟──╨───┬──────┐ STORE (S)  ╟──╨───┤   ▲ FIRE │ ▲ Dock door = FIRE
     ║ SAFE  ║ TEMP │SECUR-│ 1H(55)      ║      │   EXIT 2 │   EXIT 2 (truck ramp)
 z8  ╞═══════╡OFFICE│ITY(S)├─────────────╡══════╡─────────┤
     │ PWR ⚡ │ (S)  │1H(30)│  LOBBY /    │ PUMP │ GEN     │
     │ RM(S) │ clue!│      │  SALES HALL │ (S)  │ ANNEX(S)│
     │ gate  │~~~   │      │  (L) ★ !    │      │ 1H(25)  │
 z4  ╞══╤════╧──────┴──────┤ "Floor 2 Power Off  ├──────┴─────────┤
     │  ║ FIRE EXIT 3       │  Shutter Locked"│                │
     │  ▲ (maint. ladder    │  ◆ MAIN     │                │
 z0  └──╨── up from forecourt┴──╥─────────┴────────────────┘
                                ║  VAN (S/E)  — dispatch van
        x0   4   8  12 16 20 24 28 32 36 40 44 48   forecourt z<0
```

### Floor 1 rooms (~15) & slots

| Slot | Room | Size | Role | Loot / beat |
|------|------|------|------|-------------|
| LOBBY | Lobby / Sales Hall ★ | L | **Fixed** (touches van) | "Floor 2 Power Off / Shutter Locked" readout (teach gate + tone) |
| PWR | Power Room ⚡ | S | **Fixed (gate)** | Restore power → unlock F2; hold-interact |
| TEMP | Temporary Office | S | Random S/F1 | **clue** to power room |
| SECUR | Security Office | S | Random S/F1 | 1H(30) keys/radio |
| WARE | West Material Warehouse | L | Random L/F1 | 2H(140), batteries, consumables |
| WORK | East Assistant Workshop | M | Random M/F1 | consumables (medkit/spray/decoy) |
| DORM | Worker Dorm | M | **Fixed-ish** | **EVIDENCE** Isolation Notice (bonus) |
| CANTEEN | Canteen / Break Room | M | Random M/F1 | 1H(40) |
| FOREMAN | Foreman Office | M | Random M/F1 | 1H(90) rich |
| SAMPLE | Sample Material Store | S | Random S/F1 | 1H(55) |
| PUMP | Pump / Utility | S | Random S/F1 | filler |
| GEN | Generator Annex | S | Random S/F1 | 1H(25); near power |
| DOCK | Loading Dock | M | **Fixed (fire exit)** | 2H(110); **truck-ramp escape** |
| HUB | Central Construction Hall | L | **Fixed** | double-height shaft; see up to F2 |
| STAIR-A/B | Main / Side stair | core 4×8 | **Fixed** | A fast/exposed, B dim/safe |

---

## 3. FLOOR 2 — Show-flat / Sales floor (med → high risk: objective + nest)

Stairs A/B at the **same x,z** as F1. Shaft is the same hole, now crossed by the
**scaffold bridge** — the only *direct* E↔W route. Objective is far NW, away from van
and from fast Stair-A.

```
 z32 ┌───────────────┬─────────┬─────────┬─────────┬───────┐
     │ DEEP TARGET(L)│ EXEC    │ MODEL   │ SALES   │       │
     │ ☠ Scale model 2H(★★★)│ SUITE(M)│SHOWROOM │OFFICE(M)│ STAIR │
     │ lit pedestal  │ 1H(120) │ (M)1H(60│ 2H(130) │   A   │
 z24 ╞═══════╤═══════╪════╤════╪════╤════╪════╤════╡ (4×8) │
     ║       ~~~     ║    ~~~  ║    ~~~  ║    ║    ║        │
     ║  [== SCAFFOLD ▒▒ BRIDGE ▒▒ ==] (only direct E↔W)    │
 z16 ║ SHOW  ║       ▒▒▒ SHAFT ▒▒▒        ║   ╞════╧════╡
     ║ FLAT  ║  ╥        (fall risk)   ╥  ║   │ VIP     │
     ║ ★(M)  ╠══╪═══════════════════════╪══╣   │ LOUNGE  │
     ║ warm  ║  ║ SHAFT-EDGE (L)        ║  ║   │ (M)     │
 z12 ║ light ╟──╨──┬───────┐ unfin.     ╟──╨───┤1H(70)   │
     ║       ║MAINT│ DANGER│ fall edge  ║      │ ▲ FIRE  │ ▲ Balcony scaffold
 z8  ╞═══════╡ (S) │SHAFT  ├────────────╡══════╡ EXIT 4  │   fast-drop to F1 dock
     │ STAIR │short│RM(S)  │ MARKETING  │STORE │ BALCONY │
     │   B   │cut  │2H(95) │ (S) 1H(35) │ (S)  │ (S)     │
     │  DIM  │~~~  │high-rk│            │      │ scaffold│
 z4  ╞═══════╧─────┴───────┴────────────┴──────┴─────────┤
     │  (Stair-B safe west loop ── long way to target)   │
 z0  └───────────────────────────────────────────────────┘
        x0   4   8  12 16 20 24 28 32 36 40 44 48
```

### Floor 2 rooms (~15) & slots

| Slot | Room | Size | Role | Loot / beat |
|------|------|------|------|-------------|
| TARGET | Deep Target Area ☠ | L | **Fixed (objective+nest)** | Scale model on lit pedestal (heavy 2H carry); monster nest |
| SHOWFLAT | Sample Office / Show-flat ★ | M | **Fixed (beacon)** | "wrong" warm light, visible across shaft |
| EXEC | Exec Suite | M | Random M/F2 | 1H(120) rich |
| MODEL | Model Showroom | M | Random M/F2 | 1H(60) |
| SALES | Sales Office | M | Random M/F2 | 2H(130) rich-but-slow |
| SHAFTEDGE | Unfinished Shaft Edge | L | **Fixed** | wraps void; fall risk; vertical sightline |
| MAINT | Maintenance Room | S | Random S/F2 | patrol-gap **shortcut** |
| DANGER | Dangerous Shaft Room | S | Random S/F2 | 2H(95) high-risk |
| MARKET | Marketing Room | S | Random S/F2 | 1H(35) |
| STORE | Storage | S | Random S/F2 | filler |
| VIP | VIP Lounge | M | Random M/F2 | 1H(70) |
| BALCONY | Balcony / Terrace | S | **Fixed (fire exit)** | **scaffold fast-drop to F1 Dock** |
| BRIDGE | Scaffold Bridge | corridor | **Fixed** | only direct E↔W; fall risk |
| STAIR-A/B | Main / Side stair | core 4×8 | **Fixed** | aligns with F1 |

---

## 4. Topology randomization (the LC borrow) — how it stays solvable

The generator already syncs one **seed** (`TowerLayoutGenerator.netSeed`). v2 uses it to
toggle a **finite set of connectors**, not just room contents.

- **Fixed graph (never toggled)** — the **critical path is always connected**:
  `VAN ◆ → LOBBY → (any stair) → F2 → TARGET ☠ → (any stair) → VAN`. Also fixed:
  both stairs (aligned), Power Room + gate door, Scaffold Bridge, Shaft, Deep Target.
- **Seed-toggle connectors `~~~`** (≈8 per floor): side-loop doors, cross-corridor
  shortcuts, the maintenance-room patrol-gap, the power-room back door. Each is OPEN or
  **rubble/tarp/locked** per seed.
- **Invariant the generator must enforce** (and a test must assert): for any seed,
  (a) the critical path is traversable, (b) the **PowerRoom clue** spawns in a reachable
  room *before* a stair, (c) **≥1 consumable room** is reachable before any stair,
  (d) **≥2 of the 3 fire exits** are reachable from the objective floor. If a roll
  violates these, re-roll the toggles (deterministically) until valid.
- **Why it's safe for netcode/NavMesh**: the door graph is a known finite set; identical
  seed → identical topology on every peer; bake NavMesh per-variant offline OR bake the
  full superset and let blocked connectors carry NavMeshObstacles. (Decide in 3D pass.)

> Net effect: every run is a *different maze that is always winnable* — DunGen's
> "main path always valid, branches vary," rebuilt in our slot system.

---

## 5. Oppression + escape (the two things PM asked for)

**Oppression (pressure):**
- Grabbing the scale model aggros the **Infected Site Inspector** (reskin `SchoolMonsterAI`).
- Monster **scales with time-in-building** (`MvpMissionClock`): patrol radius and
  hunt aggression rise the longer the team stays → punishes greed.
- Heavy 2H carry slows the carrier + locks hotbar → carrier is the vulnerable point.
- Darkness pre-power; even post-power the upper floor stays gloomy outside the show-flat.

**Escape (you can always *try* to flee):**
| Exit | Location | Trade-off |
|------|----------|-----------|
| ◆ MAIN | F1 Lobby → van | Safe, but far from F2 objective (full descent under chase) |
| ▲ FIRE 2 — Loading Dock ramp | F1 east | Fast out, but east side, away from van forecourt |
| ▲ FIRE 3 — Maintenance ladder | F1 SW from forecourt | Quick re-entry/exit; tight, exposed |
| ▲ FIRE 4 — Balcony scaffold drop | F2 east → F1 Dock | **F2→F1 fast-drop** to skip stairs; fall risk; lands east |
| Stair-B (dim/safe) | both floors, west | Long safe loop vs fast exposed Stair-A |

→ Under pressure the team chooses: fast-and-exposed (Stair-A / scaffold drop) vs
slow-and-safe (Stair-B west loop). That choice *is* the tension.

---

## 6. Pacing chart

```
Intensity
10 |                                       *  (carry-out chase, monster scaled up)
 8 |                                  *  *  * *
 6 |                       *    *   *          *
 4 |        *   *   *  * *    *  *               *
 2 |  * *     *            *                        *
 0 |S------------------------------------------------E
   [Van][Lobby/teach][Gather+Power gate][Ascend/maze][Scale model+nest][Descent/Exit]
```

- Valley: F1 explore (maze, gather, find power) — monster dormant.
- Rise: power on → ascend → F2 maze, scaffold, monster audible/patrolling wider.
- Climax: grab scale model → monster active + time-scaled → heavy carry descent.
- Decision at van: full / partial / push back in.

---

## 7. What carries over unchanged

- Objective = scale model (heavy 2H carry, droppable/relay, server-authoritative carrier id).
- Power gate (server-authoritative building state unlocks F2).
- Monster = Infected Site Inspector (reskin `SchoolMonsterAI` + `HidingSpot`).
- Mission state/return/partial/fail, van settlement, office-computer commission.
- Evidence photo (Isolation Notice) bonus pattern.

## 8. New work flagged for scoping (after approval)

1. **Topology-randomization upgrade** to the slot generator (the headline change) —
   connector toggle set + solvability re-roll + per-variant NavMesh strategy.
   ⚠️ Server-authoritative, seed-synced → **needs EditMode tests** (layout identical on
   host+clients; always solvable). This is the high-risk untested area from
   `systems-index.md`.
2. **Time-scaled monster aggression** curve.
3. **3 fire-exit interactions** (dock ramp, maintenance ladder, balcony scaffold drop)
   + fall-risk volumes.

## 9. Acceptance criteria (for the map, before 3D)

1. Two floors, 48×32m envelope, S/M/L only, ~15 rooms/floor.
2. Every floor is a loop+branches graph (≥2 routes between van and stairs), not a spine.
3. ≥3 fire exits + the dim safe stair, all reachable; critical path always solvable.
4. Topology toggles defined as a finite seed-driven set with the §4 invariants.
5. Objective NW on F2, van S on F1; two beacons (show-flat + stair towers).
6. Target 15-min 1–4p run; oppression (time-scaled monster, heavy carry) + escape valves.

---

> **Next step on approval:** I rebuild the whitebox to this map — update the slot
> skeleton + `RoomDef/RoomSlot/TowerLayout/TowerLayoutGenerator/TowerRoomCatalog` for
> topology randomization, then graybox the rooms. Until approved, no code changes.
