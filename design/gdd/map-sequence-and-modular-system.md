# Map Sequence & Modular Room System

**Status**: In Design
**Date**: 2026-06-16
**PM**: Yan Dai

## Overview

Black Commission targets 6 maps for the full game, with a procedural modular
room system (Route B) that makes each map replayable across multiple runs.
The final 1–2 maps are reserved for the hidden ending path (see Ending Design).

## Map Sequence

| # | Map | Stage | Mission Types | Narrative Function |
|---|-----|-------|--------------|-------------------|
| 1 | 地球海岸壹号·烂尾预售楼 | Stage 1 | Commissioned | Dream of home that never was |
| 2 | 地下私人会所 | Stage 2 | Commissioned + Free Salvage | Where Earth's wealthy entertained themselves while others suffered |
| 3 | 地铁换乘枢纽 | Stage 2–3 | Commissioned + Free Salvage | Last trains before lockdown — everyone left something behind |
| 4 | 市民债务仲裁局 | Stage 3 | Commissioned + Black Commission | The bureaucracy that processed people's ruin |
| 5 | 区域医疗分诊站 | Stage 3–4 | Commissioned + Free Salvage + Black Commission | Human cost of MRC-7; truth fragments begin |
| 6 | 轨道货运中转站 | Stage 4 | Commissioned + Black Commission | Where Earth's things were packaged and sent — the finale |

**Hidden ending map (Mars)**: Triggered only by the Stage 5 "Stay on Earth" path
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

### What Varies Per Seed

- Corridor connection topology (how shared modules are linked)
- Toggle door open/closed state (13 toggle doors in the tower; similar counts for other maps)
- Item spawn locations within rooms
- Monster starting position (from pre-defined seed points)

### What Never Changes

- Hero room positions relative to each other
- Objective location zone
- Van extraction point
- Map 1 (tower): remains a fully hand-crafted fixed layout, serving as the
  reference prototype for all shared module specifications

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

Triggered by: Stage 5 choice = Stay on Earth + sufficient truth fragments
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
