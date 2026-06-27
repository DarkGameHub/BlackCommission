# Map Sequence & Modular Room System

**Status**: In Design
**Date**: 2026-06-16
**PM**: David

## Overview

Black Commission targets 6 maps for the full game, with a procedural modular
room system (Route B) that makes each map replayable across multiple runs.
The final 1–2 maps are reserved for the hidden ending path (see Ending Design).

## Map Sequence

| # | Map | Biome (approach) | Stage | Mission Types | Narrative Function |
|---|-----|------------------|-------|--------------|-------------------|
| 1 | 地球海岸壹号·烂尾预售楼 | Coastal 海岸 | Stage 1 | Commissioned | Dream of home that never was |
| 2 | 地下私人会所 | Suburban grassland 郊区草地 | Stage 2 | Commissioned + Free Salvage | Where Earth's wealthy entertained themselves while others suffered |
| 3 | 地铁换乘枢纽 | Urban meadow 城市草甸 | Stage 2–3 | Commissioned + Free Salvage | Last trains before lockdown — everyone left something behind |
| 4 | 市民债务仲裁局 | Open plain 开阔平原 | Stage 3 | Commissioned + Black Commission | The bureaucracy that processed people's ruin |
| 5 | 区域医疗分诊站 | Forest 森林 | Stage 3–4 | Commissioned + Free Salvage + Black Commission | Human cost of MRC-7; truth fragments begin |
| 6 | 轨道货运中转站 | Industrial wasteland 工业废土 | Stage 4 | Commissioned + Black Commission | Where Earth's things were packaged and sent — the finale |

> **Biomes are proposals (2026-06-18, R-B) — adjust freely.** Map 1 = Coastal is locked (literally
> 地球海岸 / "Earth Coast"). Each map is a man-made site **set in** its biome (outdoor approach + indoor
> core), not pure wilderness — see §Hero Room Framework → Biome Approach Layer.

**Hidden ending map (Mars)**: Triggered only by the Stage 4 "Stay on Earth" choice
if enough truth fragments are collected. Not a regular mission map.
Players who choose "Go to Mars" see a CG cutscene — no playable Mars level.

### Narrative Arc

```
Map 1  →  The dream of a home (fake, commercial, unfinished)
Map 2  →  How the wealthy spent their last years on Earth
Map 3  →  How ordinary people moved through the city, then couldn't
Map 4  →  The system that processed their debt and their complaints
Map 5  →  Where MRC-7's human cost was documented (truth emerges)
Map 6  →  Where everything was loaded and sent away (truth complete)
```

By the end of Map 6, players who have read the commission text and settlement
notes carefully can piece together the origin of the Noble Guest Plague.

## Modular Room System (Route B)

### Core Principle

Maps share a common pool of room modules. Each run, a seed-based topology
generator connects the modules in a different configuration. Hero rooms
(narrative/gameplay anchors) maintain fixed relative positions. Connecting
corridors and filler rooms vary per seed.

This gives each map:
- Consistent spatial identity (you know which building you're in)
- Variable routes per run (the path between landmarks changes)
- Stable learning curve (key rooms always in the same relative zone)

### Comparison to Lethal Company

| | Lethal Company | Black Commission |
|--|---------------|-----------------|
| Room positions | Fixed every run | Hero rooms fixed, connectors vary |
| What changes | Item spawns, enemy positions | Corridor layout + item spawns + Toggle doors |
| Route variation | None (same path each run) | Different path per seed |
| Replayability driver | What you find | Where you go + what you find |

BC has more structural variation per run than LC, at the cost of requiring
a topology generation system.

### One Room Kit — Three Maps

Maps 1, 5, and 6 (the first three built) all share **Kit A: Institutional Interior**.

**Shared modules (build once, used in all three maps):**

| Module | Size | Notes |
|--------|------|-------|
| Standard corridor | 2m wide, variable length | Cable tray ceiling, 2.4m perceived height |
| Service junction | 4×4m | Node connecting multiple corridors |
| Small utility room | 4×4m | Electrical, mechanical, storage |
| Admin/office room | 4×8m | Desk, cabinet, CRT terminal, notice board |
| Stairwell | 4×8m | Dog-leg, two-run, connects floors |
| Large open space | 8×12m+ | Warehouse, ward, sales floor — themed per map |

**Hero rooms (unique per map, built separately):**

| Map | Hero Rooms |
|-----|-----------|
| 烂尾预售楼 | Sales lobby (沙盘室), show flat section, construction zone, power gate room |
| 医疗分诊站 | Examination/triage room, treatment ward, pharmacy, filing room |
| 货运中转站 | Loading dock, cargo processing floor, freight office, sealed container bay |

### Hero Room Position Logic

Hero rooms are anchors at fixed positions in the map's zone structure.
The topology generator fills in connecting routes between them.

```
[Van / Entry] ──paths──  [Mid-point anchor]  ──paths──  [Deep anchor / Objective]
```

| Map | Entry Anchor | Mid Anchor | Deep Anchor |
|-----|-------------|-----------|------------|
| 烂尾楼 | Sales lobby | Power gate room | Show flat + objective |
| 医疗站 | Reception/triage | Treatment ward | Pharmacy + archive |
| 货运站 | Loading dock | Freight office | Sealed container bay |

The van is always near the entry anchor. The objective is always in the
deep anchor zone. Players learn the narrative structure of each map across
runs even as the specific routes change.

### Hero Room Framework (formal — 2026-06-18)

A **hero room** is a hand-authored, fixed **narrative + gameplay anchor**. Each map has exactly
three (Entry / Mid / Deep); connectors, toggle doors, and item/monster spawns vary per seed, but the
three anchors keep fixed relative positions so players learn each site across runs. Every hero room
must do four jobs:

1. **Identity** — a memorable, readable silhouette/space that says which site you are in.
2. **Loot tier** — it holds a defined loot tier (escalating Entry → Mid → Deep, see Systems Integration).
3. **Encounter beat** — a danger/decision moment (a chokepoint, a power gate, a deep-zone push).
4. **Environmental story** — it carries the map's slice of the Municipal Debt Noir / infected-Earth fiction.

Filler rooms and corridors carry none of these four; only hero rooms do. A map with fewer than three
working anchors is not ready for the level sprint.

### Biome Approach Layer (R-B, 2026-06-18)

Each map is a **man-made site set in a natural biome** (PM decision R-B): the team disembarks the van
in an **outdoor biome approach** (each map's biome column in §Map Sequence) and moves into the man-made
core that holds the loot and danger. The biome is the map's outdoor identity and first impression; the
interior anchors are the loot/danger core. Earth is being reclaimed by infection, so the biome
encroaches at the site's edges (overgrowth, water ingress, root-broken concrete at the approach).

```
[Van — biome approach]  →  [Entry anchor]  →  [Mid anchor]  →  [Deep anchor / objective zone]
   outdoor, low danger          man-made core, escalating danger  →→→
```

### Systems Integration — B / A / C

The hero-room structure is where the locked economy / danger / monster systems land on the map:

- **Danger zones (C) ↔ anchors.** The danger spec's zones map onto the three anchors:
  Entry = `zone_factor ×1.0`, Mid = `×1.3`, Deep = `×2.0` (infection core). The Deep anchor is where
  the `Pursuit`/`Saturation` phases bite hardest — pushing deep for the top loot tier is the core
  greed/risk lever.
- **Loot tiers escalate by anchor (B).** Entry = Light/Medium effects; Mid = mixed + 1–2 Heavy;
  Deep = highest-value items + Heavy fixtures (two-hand carry). No designated target item exists.
- **Client category preference (B).** Each Commissioned/Black client favours 1–2 item categories
  thematically tied to that site, paying `× clientPreferenceMultiplier` (1.3) at settlement; per-item
  usage notes carry the satire.
- **Monster (C).** One Echo Mold per map roams the interior; `danger_level` (pure time, 4 phases:
  Survey/Active/Pursuit/Saturation) drives its activity via `monster_activity` lerp. The Deep anchor
  is its highest-pressure ground.
- **License access (A).** Which maps/clients are available is gated by license stage (story missions at
  4 / 10 / 18 completed missions); the map's Stage column is its earliest access.

### Tower Hero-Room Template (worked example — copy this structure for maps 2–6)

Map 1 (地球海岸壹号·烂尾预售楼) is the reference prototype. Every other map fills the same slots:

| Slot | Tower instance | Loot tier | Danger / encounter beat |
|---|---|---|---|
| **Approach** | Coastal shore + parking apron, van drop | sparse outdoor scatter | Survey phase; quiet, distant sound |
| **Entry anchor** | Sales lobby (售楼大堂) | Light/Medium worker & sales effects | first interior; narrow monster detection |
| **Mid anchor** | Power gate room (电闸房) | mixed + 1–2 Heavy | the power gate to the deep loot floor; Active phase |
| **Deep anchor** | Show flat + deep loot (样板间+深区) | highest value + Heavy residential fixtures | Pursuit/Saturation bite; the "do we leave?" decision |

- **Client:** a Mars family commission — favours residential fixtures / personal effects / civic
  documents (×1.3). No 沙盘, no designated target (per `scavenging-core-loop.md` D-B / D-G).
- **Monster:** one Echo Mold roams the interior, escalating with `danger_level`.
- **To author a new map:** assign its biome (§Map Sequence), name its three anchors, pick the client's
  favoured categories, and place its loot tiers + Echo Mold spawn points. The four-slot structure
  (Approach → Entry → Mid → Deep) is invariant.

### What Varies Per Seed

- Corridor connection topology (how shared modules are linked)
- Toggle door open/closed state (13 toggle doors in the tower; similar counts for other maps)
- Item spawn locations within rooms
- Monster starting position (from pre-defined seed points)

### What Never Changes

- Hero room positions relative to each other
- Objective location zone
- Van extraction point
- Map 1 (tower): the **geometry shell** (rooms, corridors, stairs) is hand-authored and fixed —
  the reference prototype for the shared module specs. NOTE: the *shell* is fixed, but Map 1 **does**
  run `TowerLayoutGenerator`, which varies **connector (door/passage) open-closed state per seed**, so
  the route through the fixed rooms changes per run. See §Implementation Status.

### Implementation Status (verified against code + scene, 2026-06-18)

The variation **machinery** is built and wired into Map 1; the **content** that machinery consumes is
only partly authored. Verified against `TowerLayoutGenerator.cs`, `Tower_EarthCoast_01.unity`, and the
asset tree:

| Layer | Status | Detail |
|-------|--------|--------|
| Connector topology (routes) | ✅ Live | `TowerLayoutGenerator.ApplyTopology` toggles each scene `Connector` open/closed per replicated seed, validated solvable (fallback = all-open). The route through the fixed rooms varies per run. |
| Room-slot scaffold | ✅ Built | 25 `RoomSlot`s in the tower: **13 Random** + 12 fixed (1 Objective `TARGET`, 1 PowerGate `POWER`, 1 `VAN`, 4 Stair, 5 Fixed hero rooms `LOBBY`/`HALL`/`SALES`/`SHOWFLAT`/`BALCONY`). |
| Room-content fill | ⚠️ Dormant | The generator fills slots only `if (catalog != null)`. The scene's `catalog` ref is **null** and **zero `RoomDef` / `TowerRoomCatalog` assets exist** — so the 13 Random slots receive no per-seed content today (cleanly skipped, no error). |
| Scavenge loot re-roll | ❌ Not in Map 1 | `LootSpawner` / `LootAnchor` are **absent from the tower scene** (they live in `Scavenge_Testbed`). The tower is the objective-retrieval mission (`TARGET` + `POWER` gate + `VAN`), not the free-scavenge loot loop. Only 2 placeholder `ScavengeItemDefinition`s exist. |
| Monster start position | ❓ Unverified | Not checked this pass. |

**Net:** Map 1's only *live* per-run variation today is **which doors are open** (route variation). To make
rooms vary, author a `RoomDef` pool + a `TowerRoomCatalog` and assign it to the generator. Wiring the
scavenge loot loop into the tower (vs. keeping it in `Scavenge_Testbed`) is a separate, unmade decision.

## Build Order

### Phase 1 — Extract from Tower
The tower's existing geometry contains the first instances of all shared
modules. Extract the following as reusable prefabs:

1. Standard corridor segment (with cable tray and pilaster variants)
2. Service junction
3. Small utility room (POWER room is the reference)
4. Admin office room (SECUR, SAMPLE, TEMP rooms are references)
5. Stairwell (A and B stair geometry already built)

### Phase 2 — Build Map 5 (Medical Station)
Using shared modules + medical hero rooms. Connect via topology generator
with 3–5 seed variants validated. Target: plays differently from the tower
despite sharing the same corridor vocabulary.

### Phase 3 — Build Map 6 (Freight Hub)
Using shared modules + freight hero rooms. Larger scale — the large open
space module scales up for the loading dock and cargo floor.

### Phase 4 — Topology validation
Run 1,000-seed validation harness (already exists for tower topology) on
each new map. Confirm: all hero rooms reachable from van in all seeds;
no seed produces a dead-end that blocks the objective.

## Ending Map (Mars) — Design Stub

Triggered by: Stage 4 choice = Stay on Earth + sufficient truth fragments
collected across Maps 5 and 6.

The player is dispatched to Mars as a contract worker (not an immigrant)
to physically deliver evidence into the Martian network.

Visual direction: Art Director recommendation — worker habitation block
inside the Mars dome. Same institutional kit, different surface materials
(clean panels, no weathering). One hero exterior shot through dome window:
red terrain, black sky. That shot is the moral question of the whole game.

This map is shorter than a regular mission map. It is earned, not purchased.
Players who choose "Go to Mars" see a CG instead — the dinner party,
the Earth Heritage Procurement Consultant assignment, the snow lotus in a case.

---
*Related: `design/quick-specs/mission-pool-selection-2026-06-16.md`,
`design/quick-specs/scavenging-item-system-2026-06-16.md`,
`docs/world-background-2098.md` (Client Types section)*
