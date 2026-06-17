# Quick Design Spec: RoomDresser — Set-Dressing System

**Type**: New Small System
**Scope**: Fills shared room module shells (utility / office / large + corridors /
junctions) with non-interactive furniture so each room reads as its type. Does NOT
place scavengeable loot — it only exposes anchors the scavenging system uses.
**Date**: 2026-06-16
**Estimated Implementation**: ~3 days (framework + whitebox furniture prefabs + 3 dressing sets)

## Overview

`ModularRoomBuilder` generates bare room shells (walls / floor / ceiling / doors
only). RoomDresser furnishes each room **type** from a data-driven furniture set
(desk / cabinet / CRT in an office; shelving / panels / pipes in a utility room), so
maps read as a real abandoned institutional building. Dressing is baked once per room
type into the module prefab via an editor pass — like the existing
`HqOfficePropRestorer` — keeping it deterministic and free of runtime networking.
Per-run variety comes from topology, loot spawns, toggle doors, and monster start,
**not** furniture (matches map GDD "What Varies Per Seed").

## Core Rules

1. **Data-driven.** Each room type has a `RoomDressingSet` ScriptableObject = an
   ordered list of `DressingPlacement`. No furniture positions hardcoded in C#
   (coding-standard).
2. **`DressingPlacement` fields**: furniture prefab ref; local pos / rot / scale;
   wall-anchor side (N/S/E/W/none); `spawnProbability` (0–1, default 1); optional
   `lootAnchor` flag + surface tag (desk / shelf / cabinet / floor).
3. **Editor dress pass.** `Tools ▸ BC ▸ MVP ▸ Modules ▸ Dress Room Modules` reads each
   set, instantiates furniture as children under a `Dressing` group inside the matching
   module prefab, saves it. Idempotent (clears old `Dressing` group first).
4. **Furniture = whitebox prefabs** (primitives reusing V8 whitebox materials) for v1,
   at `Assets/_Project/Art/Maps/Shared/Furniture/`. Swapping to art later = new prefab
   refs in the SO, no code change.
5. **Loot boundary.** RoomDresser places only non-interactive decoration + drops
   `LootAnchor` markers (empty GameObjects, tagged by surface). The
   scavenging/loot-spawn system consumes anchors per seed. RoomDresser never spawns
   loot or interactive objects.
6. **Walkability.** Placements hug walls / perimeter, leaving the interior walkable. A
   placement flagged `centralBlocker` gets a carving `NavMeshObstacle`; everything else
   relies on the baked navmesh staying valid.
7. **Per-map themed variants.** Office / utility sets are identical across Kit A maps
   (1/5/6). The Large room ("themed per map") uses a per-map set (warehouse vs ward vs
   sales floor).
8. **No networking.** Baked furniture is part of synced level geometry → zero
   NetworkObjects on decoration. (Interactive/scavengeable pieces are out of scope here
   and remain server-authoritative in their owning systems.)

## Per-Room-Type Furniture (v1, from map GDD §One Room Kit)

| Module | Furniture (whitebox) | Loot anchors |
|--------|----------------------|--------------|
| `Room_Office_4x8` | desk(s), filing cabinet, chair, CRT (on desk), notice board (wall), wastebasket | desk surface, cabinet, floor corner |
| `Room_Utility_4x4` | electrical panel (wall), mechanical unit, shelving (wall), pipe run, crates (corner) | shelf slots, crate tops |
| `Room_Large_8x8` | perimeter shelving / racks; sparse central clusters; per-map theme override | shelf rows, floor clusters |
| `Corridor_*` / `Junction_*` | structural only: cable tray (ceiling), wall conduit, sparse debris | (rare) floor edge |

## Edge Cases

- **Door clearance**: no placement may overlap a door opening; the pass skips
  placements whose bounds intersect a `Con_*` door zone.
- **Re-dress**: running twice never duplicates (clears the `Dressing` group first).
- **Missing prefab ref**: logs a warning, skips that placement, the room still builds.
- **Determinism**: baked furniture is identical on all peers by construction. Runtime
  per-seed variation (if added later) = seed `runSeed ⊕ hash(slotId)`, same RNG on
  every peer (documented upgrade path).

## Tuning Knobs

| Knob | Default | Range | Affects |
|------|---------|-------|---------|
| `spawnProbability` (per placement) | 1.0 | 0–1 | optional-prop density |
| `centralBlocker` (per placement) | false | bool | navmesh carving |
| `lootAnchorsEnabled` (per set) | true | bool | emit loot anchors |

Values live in the `RoomDressingSet` ScriptableObjects under
`Assets/_Project/Art/Maps/Shared/Dressing/`.

## Affected Systems / Dependencies

| System | Relationship |
|--------|--------------|
| `ModularRoomBuilder` | runs after it; consumes its module prefabs |
| Scavenging item system | produces `LootAnchor`s; scavenging consumes them (spawn placement stays its job) |
| NavMesh (`TowerNavBaker` / map nav) | dressing must keep interior walkable; central blockers carve |
| `RoomDef` / `TowerRoomCatalog` | related slot-fill concept; the modular module carries its type, so the dresser keys off module type, not RoomDef |
| `HqOfficePropRestorer` | precedent (static editor dress pass); RoomDresser follows the same convention |

## Acceptance Criteria

- [ ] Each of the 3 room modules, after the pass, contains its specified furniture
      under a `Dressing` child group.
- [ ] Re-running the pass produces no duplicates.
- [ ] No furniture intersects a door opening.
- [ ] A NavMesh agent can still path entry→interior→far wall in each dressed room.
- [ ] `LootAnchor` markers exist, tagged by surface, consumable by scavenging.
- [ ] Dressing adds zero NetworkObjects.
- [ ] No regression: module shell geometry (verified dims) unchanged; tower scene
      unaffected.

## Systems Index

Not yet in `design/systems-index.md`. Suggest adding under the **Level/Map** layer,
Priority Tier 2 (after the modular room system, before per-map theming), as
"Set-Dressing (RoomDresser)".
