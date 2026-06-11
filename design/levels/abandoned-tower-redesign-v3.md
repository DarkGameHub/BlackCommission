# Level Map v3 — 地球海岸壹号 烂尾楼 (irregular footprint + single exit + exterior)

> Canonical layout. Supersedes v2 (`abandoned-tower-redesign-v2.md`) and the rectangle
> in `abandoned-tower-floorplan.md`. Same fiction (`abandoned-tower-earth-coast-01.md`),
> same objective (沙盘 / power gate / heavy two-hand carry). Principles & rationale:
> `lethal-company-design-study.md`. **LC is a method reference only — no layout copied.**
>
> Status: **AS-BUILT (whitebox).** The v3 whitebox is built. The authoritative geometry
> is now the coordinate table in `Assets/_Project/Editor/TowerV3WhiteboxBuilder.cs` plus
> the connectivity graph in `Assets/_Project/Scripts/Level/Topology/TowerTopologyV3.cs`;
> the exact F2 visual is `References/Tower_EarthCoast_01_F2_DesignPlan_v5.png`. This doc
> is the design *intent* — where it disagreed with the build, the build won (PM, 2026-06-09).
>
> **Reconciliation (PM, 2026-06-09):** two original §3/§9 claims were updated to match the
> as-built v5 layout: (1) the objective (TARGET/沙盘) sits in the **SE**, reached via
> EXEC→SALES→VIP→TARGET — *not* the NW as earlier drafts said; (2) F2's core footprint
> shrinks but an **east wing juts out** (TARGET + BALCONY past x48) so F2 is **not** uniformly
> smaller than F1. The "only stairs + shaft align across floors" rule is unchanged.
>
> **What changed from v2 (PM decisions, 2026-06-07):**
> 1. **Irregular, fiction-driven footprint** — no square envelope. Jagged silhouette
>    (finished core + half-built wings + collapsed rubble + scaffold extensions),
>    grid-snapped to 4 m. **Floors may have different footprints** (only stairs + shaft
>    must align).
> 2. **One extraction (van) + one meaningful fire exit** — not three. Redundancy moves
>    to **internal descent routes** (2 stairs + scaffold drop) so a chase can't be
>    death-camped at a single chokepoint.
> 3. **Simple exterior** — a forecourt + a one-sided perimeter run that gives the fire
>    exit its "escape but pay in exposure" value. Deliberately lightweight.
>
> Kept from v2: seed-randomized **topology**, S/M/L sizes, ~15 rooms/floor, LC-style
> loot annotation, time-scaled monster, risk gradient, objective↔exit separation.

---

## 0. Legend

```
[ROOM (size)]  S 4×4 / M 8×8 / L 12×8 (XL dropped)   1H(n)/2H(n) one/two-handed loot
█ wall   ═/║ corridor   ╥/╠ door (2m)   ▒ shaft/void   ░ COLLAPSE (open to sky, rubble)
◆ MAIN entrance + van side    ▲ FIRE EXIT (the one)    ⇣ internal descent (stair/drop)
★ landmark/beacon   ☠ nest   ⚡ power gate   ! story   ? optional
~~~ SEED-TOGGLE connector (open OR rubble-blocked per seed; never on critical path)
·/blank = OUTSIDE the building (irregular silhouette is intentional)
```

---

## 1. Massing — the building is an irregular ruin, not a box

Think of it as a **pinwheel of mismatched masses** on the 4 m grid, the way a real
abandoned mid-construction tower actually stands:

| Mass | Built state | Role |
|------|-------------|------|
| **Finished Core** (center) | the only complete part; full 2 floors | Lobby below, show-flat + 沙盘 above; the **beacon** |
| **Shaft** (in/next to core) | double-height void | vertical sightline; F2 scaffold bridge crosses it |
| **West Wing** (juts SW, shallow) | raw concrete | Power gate + offices; ends ragged at rebar |
| **East Wing** (juts E, deep) | raw concrete + dock | warehouse/workshop + Loading Dock sticking out |
| **North Wing** (offset, staggered) | raw concrete | dorm/canteen/foreman; uneven depths |
| **Collapse Corner** (NE or NW) | ░ caved-in, open sky | rubble; home of several seed-toggle blocks; **fire-exit side** |
| **Scaffold Extensions** | exterior steel | wraps parts; F2 balcony **fast-drop ⇣** to ground |

Silhouette rule for the builder: **wings have different depths and don't line up.**
Some room edges are open to sky (rebar/void). The grid governs *alignment*, not the
*outline*. This jaggedness + topology randomization = the disorientation we want.

---

## 2. FLOOR 1 — Ground / Arrival (irregular plan; van south, fire exit far north)

`·` = outside. Note the ragged edges and staggered wing depths.

```
 z40 · · · · · ┌─────────┬─────────┐ · · · · ·
               │FOREMAN  │ ▲ FIRE  │              ← fire exit at the FAR north,
 z36 · · ┌─────┤  (M)    │  EXIT   │ · · · ·        opposite the south van
         │░░░░░│ 1H(90)  │ stair   │
 z32 · · │COLLA│─────────┼────╥────┤ · · · ·
         │PSE ░│CANTEEN  │ STAIR-A │              ← Stair-A: fast, exposed
 z28 · · │open │  (M)    │ ⇣(4×8)  ├─────┐ ·
         │sky ░│ 1H(40)  │ EXPOSED │     │
 z24 · · └──╥──┼────┬────┴────╥────┘ DOCK│ ·      ← East wing juts out deep;
            ~~~│DORM│ CENTRAL  │     │(M) │          Dock = 2H loot, NOT an exit
 z20 · ┌─────┐ │(M)!│ CONSTR.  ╞═════╡2H  │ ·
       │STAIR│ │EVID│ HALL (L) │     │110 │
 z16 · │  B  ╞═╪════╡ ▒SHAFT▒  │     └──╥─┘ ·      ← Stair-B: dim, safe (west)
       │ ⇣4×8│ │WORK│ ▒ ▒ ▒ ▒  │        ~~~
 z12 · │ DIM │ │SHOP│──────────┤ · · · · · ·       ← west wing shallow, ragged
       ├──┬──┘ │(M) │ SAMPLE   │
 z8  · │PWR │~~~├────┤ STORE(S)│ · · · · · ·
       │RM⚡│TEMP│SECU│ 1H(55)  │
 z4  · │(S)│OFF │(S) ├─────────┤ · · · · · ·
       │gate│clue│1H30│ LOBBY / │
 z0  · └────┴────┴────┤SALES(L) │ · · · · · ·
                      │ ★ ! ◆   │                  ← MAIN entrance (van side)
       forecourt →    │  VAN▼   │
       x0  4  8 12 16 20 24 28 32 36 40 44
```

### Floor 1 rooms (~15)

| Room | Size | Role | Loot / beat |
|------|------|------|-------------|
| Lobby / Sales Hall ★ | L | **Fixed** (touches van) | "二层断电/卷帘锁定" readout |
| Power Room ⚡ | S | **Fixed (gate)** | restore power → unlock F2 |
| Temporary Office | S | Random | **clue** to power room |
| Security Office | S | Random | 1H(30) |
| Sample Store | S | Random | 1H(55) |
| Central Construction Hall + Shaft | L | **Fixed** | double-height; see up to F2 |
| East Workshop | M | Random | consumables |
| Worker Dorm | M | **Fixed-ish** | **EVIDENCE** 隔离公告 (bonus) |
| Canteen | M | Random | 1H(40) |
| Foreman Office | M | Random | 1H(90) |
| Loading Dock | M | Random | 2H(110) (loot, not an exit) |
| West Warehouse (in W wing) | L | Random | 2H(140), batteries |
| Stair-A | core 4×8 | **Fixed** | fast / exposed |
| Stair-B | core 4×8 | **Fixed** | dim / safe (west) |
| Collapse Corner ░ | — | **Fixed (fire side)** | rubble + open sky; leads to the one fire exit |

---

## 3. FLOOR 2 — Show-flat (offset footprint, shrunk core + east wing; objective SE)

F2 is **not the same outline as F1** — its **core shrinks** (much of the north/west wing
was never built, so there are **open gaps down to F1** where slabs were never poured) but
an **east wing juts out** past x48 carrying the objective and the balcony drop, so F2 is
**not uniformly smaller** than F1. Only **Stairs A/B and the Shaft align** with F1. The
scaffold bridge is the one direct E↔W route across the void, and the critical approach now
runs **STAIRA2 → EXEC → SALES → VIP → TARGET** (and SHOWFLAT → MODEL → EDGE → BRIDGE → SALES
from Stair-B), so the player crosses the shaft pinch before reaching the SE objective.

> **Exact visual is canonical in** `References/Tower_EarthCoast_01_F2_DesignPlan_v5.png`
> and the coordinate table in `TowerV3WhiteboxBuilder.BuildNodeTable()`. The schematic
> below is a directional summary of that as-built layout, not a pixel map.

```
 as-built F2 zones (x east 0→64, z north 0→48; grid 4 m; SHAFT void ≈ x18–44, z6–10)

   WEST edge (x0–12)        CENTER (x16–40)             EAST (x44–64)
 ─────────────────────────────────────────────────────────────────────────────
 N  ·                       NEGOT (M, densify) z40      ·
    STAIRB2 ⇣ + STAIRA2 ⇣ (A aligns F1 @26,28; B @0,16) EXEC (M) ·· DANGER(S,2H95)
    SHOWFLAT ★ (M, warm     MODEL (M) ·· SALES (2H130)  VIP (M, sealed gate)
    desk-lamp beacon)       ── SCAFFOLD ▒BRIDGE▒ pinch ──   │
 ─  MARKET(S)·ARCHIVE(S)    EDGE (L, ▒SHAFT▒ fall edge)  TARGET ☠ (L, 沙盘 2H★★★) ── BALCONY ⇣
 S  TANK(S)·FIN(S) densify  BRIDGE junction              (objective SE)   drop→F1 DOCK
```

Critical approach (red backbone): **STAIRA2 → EXEC → SALES → VIP → TARGET**, with the
Stair-B side feeding **SHOWFLAT → MODEL → EDGE → BRIDGE → SALES** across the shaft pinch.
Objective is **SE** (away from both stair landings and the F1 van); Balcony is the
scaffold fast-drop ⇣ to F1 DOCK (internal redundancy, NOT a building exit).

### Floor 2 rooms (~15)

| Room | Size | Role | Loot / beat |
|------|------|------|-------------|
| Deep Target ☠ | L | **Fixed (objective+nest)** | 沙盘 lit pedestal, heavy 2H carry |
| Sample Office / Show-flat ★ | M | **Fixed (beacon)** | cold overheads function cleanly (only zone); single warm tungsten desk lamp marks presence; pristine material condition = the wrong detail |
| Exec Suite | M | Random | 1H(120) |
| Model Showroom | M | Random | 1H(60) |
| Sales Office | M | Random | 2H(130) |
| Unfinished Shaft Edge | L | **Fixed** | wraps void; fall risk; sightline down |
| Maintenance | S | Random | patrol-gap shortcut |
| Dangerous Shaft Room | S | Random | 2H(95) high-risk |
| Marketing / Storage | S | Random | 1H(35) / filler |
| VIP Lounge | M | Random | 1H(70) |
| Scaffold Bridge | corridor | **Fixed** | only direct E↔W; fall risk |
| Balcony (scaffold drop ⇣) | S | **Fixed** | fast F2→F1 descent (redundancy) |
| Stair-A / Stair-B | core 4×8 | **Fixed** | aligned with F1 |

---

## 4. One extraction + one fire exit + internal redundancy

| Element | Count | Where | Trade-off |
|---------|-------|-------|-----------|
| **Extraction (van)** | **1** | F1 south forecourt, at Lobby ◆ | the only way to *settle*; fixed |
| **Fire exit ▲** | **1** | F1 far north (Collapse Corner side) | escape a chase by *not* using the south route — but it dumps you on the **far side**, so you pay with a **perimeter run** back to the van (exposed) |
| **Internal descents ⇣** | **3 routes** | Stair-A (fast/exposed), Stair-B (dim/safe west), Balcony scaffold drop (F2→F1) | redundancy so a monster can't camp one chokepoint; the "fast-exposed vs slow-safe" choice |

> Why this answers "do we only need one exit": the **extraction** is one (the van) and
> the **building egress** is effectively one real fire exit — but the **descent** has
> redundancy so chases stay fair and tense. We don't multiply exterior doors; we
> multiply *internal routes to the single van*.

---

## 5. Exterior — simple, just enough to make the fire exit mean something

The building sits in a **fenced construction lot (工地围挡)**. Keep it a thin shell, not
a second level.

```
            ░ COLLAPSE side ░     ▲ FIRE EXIT (far north)
        ╔═══════════════════════════════╗  ← site hoarding (fence)
        ║  spoil  ┌──────────────┐ crane ║
        ║  piles  │              │  base ║   PERIMETER RUN (east side only):
        ║   ▓     │   BUILDING   │  ▓    ║   fire exit ─┐
        ║         │  (irregular) │       ║              │ run down the
        ║  rebar  │              │ scaff ║              ▼ east strip,
        ║  stacks └──────────────┘ tower ║   past crane/stacks (cover),
        ║   ▓        forecourt           ║   back to forecourt
        ║        ┌──────────┐  ◆MAIN     ║              │
        ║  GATE  │   VAN▼   │  entrance  ║ ←────────────┘
        ╚════════╧══════════╧════════════╝
```

- **Forecourt (south)**: van parks (spawn / return / partial settlement); site gate in
  the hoarding; banners ("拥有真正的地球海岸"); signage points to the Lobby. **The van's
  glow is the safe beacon.**
- **Perimeter run (one side only)**: a navigable strip down the **east** side linking the
  far-north fire exit back to the forecourt. Not a full wrap — keep it simple.
- **Cover / readability props**: crane base, rebar & material stacks, stacked tarps, a
  site-office container, scaffold towers, spoil piles. They break sightlines so the
  outdoor dash has a little cat-and-mouse.
- **Lighting**: mostly dark; exterior site floods are dead or near-dead (cold-adjacent, barely sufficient to read ground geometry); van interior = cold strip (5000K); van headlights = white/cold forward beam. The van reads as safe through contrast — the only lit interior on a dark forecourt — not through warm color.
- **Hazard (light only)**: uneven ground / an open foundation trench you can fall into
  near the fire-exit landing. The monster may pursue **a few meters** outside the fire
  exit, then break off. **No exterior nest, no second monster** — exterior is a
  traversal/exposure beat, not a fight arena.

---

## 6. Topology randomization (unchanged from v2 — the LC borrow)

Seed (`TowerLayoutGenerator.netSeed`) toggles a **finite set of `~~~` connectors**
(side doors, cross-shortcuts, the power-room back door, collapse-zone gaps), never the
critical path. Generator invariants (assert in EditMode tests):

- (a) critical path `VAN ◆ → LOBBY → any stair → F2 → TARGET ☠ → any stair → VAN`
  always traversable;
- (b) PowerRoom **clue** reachable before a stair;
- (c) ≥1 consumable room reachable before a stair;
- (d) the **fire exit ▲ reachable** from the objective floor;
- (e) **≥2 of the 3 internal descents** open (no single campable chokepoint).

Invalid roll → deterministically re-roll toggles until valid. Identical seed → identical
topology on every peer → bakeable & net-syncable.

---

## 7. Oppression + escape, pacing, carryover (from v2)

- **Oppression**: 沙盘 pickup aggros the Infected Site Inspector; monster **scales with
  time-in-building** (`MvpMissionClock`); heavy 2H carry slows + locks hotbar; darkness.
- **Escape**: the single fire exit + 3 internal descents + the dim safe stair = "you can
  always try to flee," but the fire exit costs you the exposed perimeter run.
- **Pacing**: Van → Lobby(teach) → gather+power gate → ascend/maze → 沙盘+nest →
  descent/exit, climax on the carry-out chase (monster time-scaled).
- **Carryover unchanged**: 沙盘 heavy carry (droppable/relay, server-auth carrier id),
  power gate (server-auth), monster (reskin `SchoolMonsterAI`+`HidingSpot`), mission
  state/return/partial/fail, van settlement, office commission, evidence photo bonus.

---

## 8. New work flagged (after approval)

1. **Topology-randomization upgrade** to the slot generator + solvability re-roll +
   per-variant NavMesh strategy. ⚠️ server-authoritative, seed-synced → **EditMode tests**
   (identical layout host+clients; always solvable) — the high-risk untested area.
2. **Irregular footprint authoring** — wings/collapse/scaffold as grid-snapped masses;
   F2 shrunk core + jutting east wing, with open fall-gaps; bake NavMesh across the ragged plan.
3. **Single fire exit + exterior perimeter** — fire-exit interaction, exterior NavMesh,
   light exterior hazard, monster brief-pursue-then-break.
4. **Time-scaled monster aggression** curve.
5. **Deferred: LC/DunGen-style modular shell generator** — after the current fixed-shell
   tower is playable and tuned, evaluate replacing/augmenting the coordinate table with
   modular room tiles that have doorway sockets, bounds checks, seed-synced placement,
   solvability re-roll, and generated corridor/door geometry. This is intentionally **not**
   part of the current v3 whitebox pass; first polish the fixed-shell map, topology toggles,
   room-content catalog, NavMesh, lighting, power gate, objective carry, and monster pacing.

---

## 9. Acceptance criteria (for the map, before 3D)

1. Two floors with **irregular, non-rectangular** footprints, 4 m grid-snapped; F1 ≠ F2
   outline; only stairs + shaft align.
2. Each floor a loop+branches graph; S/M/L only; ~15 rooms/floor (~30 total).
3. **One van extraction + one fire exit + 3 internal descents**; critical path always
   solvable; no single campable chokepoint (invariant e).
4. Topology = finite seed-driven toggle set with §6 invariants.
5. Objective SE on F2 (reached via EXEC→SALES→VIP→TARGET), van S on F1; beacons = show-flat + stair towers. Show-flat beacon = brightest functioning cold-light zone on F2, visible through the shaft void from F1; a single warm tungsten desk lamp on the sales desk marks human presence (art-bible Section 4 accent rule).
6. **Simple exterior**: fenced lot, forecourt+van, one-sided perimeter run, cover props,
   mostly dark + van beacon, light hazard only — no exterior nest/fight.
7. Target 15-min 1–4p run; oppression (time-scaled monster, heavy carry) + escape valves.

---

> **Next step on approval:** rebuild the whitebox to this map — update the slot skeleton
> + `RoomDef/RoomSlot/TowerLayout/TowerLayoutGenerator/TowerRoomCatalog` for topology
> randomization and the irregular footprint, add the exterior, then graybox. No code
> until approved.
