# ADR-0003: Indoor Procedural Map Generation (Constrained Grid-Based, Host-Deterministic)

## Status

Accepted

*Accepted by David (PM), 2026-06-18 — indoor procedural generation greenlit for implementation.*

## Date

2026-06-18

## Decision Makers

David (PM, final decision maker); authored via `/architecture-decision`.

## Summary

Build a **custom, in-house grid-based tile-stitching generator** (a constrained
"DunGen-equivalent") for map interiors. Three hand-authored **hero anchors**
(Entry / Mid / Deep) keep fixed relative positions; the generator stitches the
connecting corridors/rooms from the module pool on a cell grid, driven by a single
server-chosen seed replicated via `NetworkVariable<int>` so every peer rebuilds the
identical layout. This is the deliberate middle ground between Lethal Company's
full free-form procedural maps and the current fixed authored shell — it gives real
per-run route + content variation while preserving the "memorable place identity"
pillar and staying host-authoritative.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6 (6000.4.7f1) |
| **Domain** | Core / Procedural Generation (+ Networking, Navigation) |
| **Knowledge Risk** | MEDIUM — NGO determinism (see ADR-0001) and runtime NavMesh (`NavMeshSurface` / `NavMeshObstacle`, `com.unity.ai.navigation`) sit near/after the LLM cutoff. |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `modules/networking.md`, `modules/navigation.md`, ADR-0001, and the shipping `TowerLayoutGenerator` / `TowerTopology` / `TowerLayout` / `RoomSlot` code. |
| **Post-Cutoff APIs Used** | NGO `NetworkVariable<int>` seed replication; runtime `NavMeshSurface.BuildNavMesh` and/or `NavMeshObstacle` carving. |
| **Verification Required** | (1) host + clients build byte-identical layout from one seed; (2) runtime navmesh valid on generated layouts; (3) 1000-seed reachability harness green. |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (host-authoritative networking) — the layout seed is shared state, server-owned. |
| **Enables** | per-map room-content / loot / monster placement; the future outdoor-approach ADR. |
| **Blocks** | the "maps" epic / level-design sprint (tower + maps 2–6). |
| **Ordering Note** | This is the "map generation" follow-up ADR-0001 explicitly anticipated. |

## Context

### Problem Statement

Maps need per-run replayability — a different route AND different contents each
descent — **without** copying Lethal Company's fully procedural maps (forbidden by
`@AGENTS.md`) and **without** losing the "you know which building you're in" identity
pillar. Today the tower is a fixed authored shell + per-seed connector open/close
(routes barely vary — only 9 of 44 connectors even have blockers) + a room-content
fill that was just wired (ADR pending). We need a generation model that delivers real
variation, stays host-deterministic for 1–4p co-op, and keeps site identity.

### Constraints

- Host-authoritative + seed-deterministic (ADR-0001; registry `all_shared_gameplay_state` → host-server; forbidden `client_authoritative_state_write`).
- Stay in Unity + NGO, no custom backend (registry forbidden `custom_backend`).
- `@AGENTS.md`: Lethal Company is a **production-method reference only** — do NOT copy its map layouts/assets.
- Design pillar: memorable, learnable site identity.
- Unity 6, 60 FPS, 4-player ceiling.

### Requirements

- Per-seed variation in **both** route and room contents.
- Three hero anchors (Entry/Mid/Deep) keep fixed relative positions (identity + loot-tier escalation).
- Deterministic across host + clients from one replicated seed.
- Objective + exits reachable on **every** seed.
- Valid runtime navmesh on the generated layout (players + monster path correctly).
- Reuse existing pieces (seed-sync, module tiles, content fill, validation).

## Decision

Build a **custom grid-based tile-stitching generator** (constrained model):

1. The map is laid out on a coarse **cell grid**.
2. The three hand-authored **hero anchors** (Entry / Mid / Deep) are placed at **fixed
   grid zones** — fixed relative positions preserve identity and the loot-tier escalation.
3. Between/around the anchors the generator **stitches** corridor / junction / room
   **tiles** from the module pool, cell by cell. Overlap prevention is **grid
   cell-occupancy** (trivial — the hard part of free-form generation disappears).
4. A single **seeded `System.Random`** (server-chosen seed, replicated via
   `NetworkVariable<int>` per ADR-0001) drives every choice → all peers rebuild the
   **identical** layout. **Only the int seed crosses the wire.**
5. Unused tile doorways are **capped** (wall / rubble + `NavMeshObstacle`).
6. **Reachability is validated per seed** (BFS Entry→Mid→Deep + exits); re-roll up to a
   cap; fall back to a known-good baseline if no seed validates — reusing the existing
   `TowerTopology.Resolve` validate-or-fallback pattern and the 1000-seed harness.
7. After geometry, per-seed **room content** fills via the existing
   `RoomDef` / `TowerRoomCatalog` / `TowerLayout.Fill`; loot via `LootSpawnPlanner`;
   one monster per map.
8. **Navmesh** for the assembled layout: resolved by an implementation spike — either
   a host-side runtime `NavMeshSurface.BuildNavMesh` after generation, or pre-baked
   per-tile navmesh + `NavMeshLink` at doorways + `NavMeshObstacle` carving on caps.

This is **not** Lethal Company's full free-form procedural map: the hero anchors are
fixed, the tile set and theme are our own, and the building stays recognizable across
runs. It is **not** the current fixed shell either: the connecting layout genuinely
re-stitches each seed.

### Architecture Diagram

```
server picks seed ──▶ NetworkVariable<int> netSeed ──▶ (every peer, deterministically)
   GridLayoutGenerator.Generate(seed):
     1. place 3 hero anchors at fixed grid zones
     2. stitch tiles between anchors (seeded RNG, cell-occupancy overlap, cap unused doorways)
     3. validate reachability (BFS) → re-roll up to cap → fallback baseline
     4. fill room content (TowerLayout.Fill) + loot (LootSpawnPlanner) + monster
     5. build/repair navmesh (runtime bake or pre-bake + links + carve)
```

### Key Interfaces

- **On the wire:** `NetworkVariable<int> netSeed` (write: Server, read: Everyone) — the
  ONLY synced datum. Reuses ADR-0001's server-owned-state model; no client writes.
- **Tile metadata:** each module prefab declares its **doorway sockets** (grid cell +
  facing + size). New authoring data added to the existing module prefabs.
- **Generator output:** a placed `cell → tile` map plus the `RoomSlot`s the existing
  content-fill consumes.

## Alternatives Considered

### Alternative 1: DunGen (the paid Unity asset Lethal Company uses)
- **Description**: Import/configure DunGen with our tiles for free-form procedural interiors.
- **Pros**: Battle-tested; solves 3D overlap + branching + capping; fastest to free-form quality.
- **Cons**: Third-party paid dependency; must verify host/client determinism through NGO; Unity 6 compat to confirm; less control.
- **Rejection Reason**: Our **constrained grid** model doesn't need DunGen's free-form power, and a third-party generator is the riskiest place for NGO determinism. Revisit only if we later commit to full free-form.

### Alternative 2: Full free-form custom generator (arbitrary angles/positions)
- **Description**: Grow tiles at arbitrary transforms like DunGen, but in-house.
- **Pros**: Maximum structural variation.
- **Cons**: Hardest engineering (oriented-bounds overlap, backtracking); weakest place-identity; largest bug surface.
- **Rejection Reason**: Over-scoped and it harms the identity pillar; the grid model gets most of the benefit far more cheaply.

### Alternative 3: Keep the current fixed shell + connector open/close toggle
- **Description**: Ship today's `TowerLayoutGenerator.ApplyTopology` (44 connectors, 9 blockers) as the only variation.
- **Pros**: Simplest; already exists.
- **Cons**: Routes barely vary; no real per-run replayability; under-authored blockers.
- **Rejection Reason**: Fails the per-run-variation requirement. (Retained as the fallback baseline + hero-anchor source.)

## Consequences

### Positive
- Real per-run route **and** content variation, while keeping site identity.
- Host-deterministic by construction (one seed, deterministic rebuild) — fits ADR-0001 exactly.
- No third-party dependency or cost; stays in Unity + NGO.
- Reuses the seed-sync, module tiles, content fill, and validation we already have.

### Negative
- Net-new generator code (stitcher + cell-occupancy + doorway capping + tile socket metadata).
- Runtime navmesh handling on a generated layout adds load-time cost + a spike.
- More test surface (determinism, reachability, overlap).

### Risks
| Risk | Prob | Impact | Mitigation |
|------|------|--------|-----------|
| **Netcode non-determinism** (float order, hash-set iteration, `Time`, unsynced RNG) desyncs host/clients | HIGH | HIGH | Single seeded `System.Random`; stable sort/iteration order (already the `TowerLayout.Fill` discipline); only the int seed on the wire; 4-player PlayMode determinism smoke. |
| A seed traps the objective (unreachable) | MED | HIGH | BFS reachability validation + re-roll cap + known-good fallback (reuse `TowerTopology.Resolve`) + 1000-seed harness. |
| Runtime navmesh invalid on generated geometry | MED | HIGH | Implementation spike: runtime `NavMeshSurface` bake vs pre-baked tiles + `NavMeshLink` + `NavMeshObstacle` carve; verify monster/player pathing. |
| Tile overlap | LOW | MED | Grid cell-occupancy makes it trivial; only a risk if off-grid tiles are later allowed. |
| Scope creep (too many tiles/features up front) | MED | MED | Start with the existing 6 corridor / 5 junction / 3 room modules; expand only after the loop is proven. |

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| `design/gdd/map-sequence-and-modular-system.md` | "Hero rooms fixed, connectors vary; different path per seed; one room kit, three maps." | Constrained grid generator: fixed hero anchors + seeded tile stitching + per-seed content fill; shared module/tile pool across maps. |
| `@AGENTS.md` | Host-authoritative state; LC as production-method reference only; don't copy LC map layouts. | Server seed + deterministic rebuild (no client authority); constrained (not full-procedural), our own tiles/theme, identity preserved. |

## Performance Implications

- **CPU**: Generation is a **one-time cost at mission load** (host and each client build locally from the seed), not per-frame. Target well under a load-screen budget. Runtime navmesh bake is the heaviest step — budget it at load, behind the van-transit screen.
- **Memory**: One map's tiles + content instances.
- **Load Time**: +generation +navmesh at mission start (acceptable; overlaps the van-transit/load window).
- **Network**: **Only the int seed** crosses the wire (existing pattern) — negligible. No layout streaming.

## Migration Plan

Evolve, don't replace: reuse `TowerLayoutGenerator`'s seed-sync; swap its content-fill
path to call the new grid generator. The current fixed-shell tower remains the
**fallback baseline** and the source of the hand-authored hero anchors. Phasing:
1. Grid stitcher + cell-occupancy overlap + doorway capping on the existing modules.
2. Hero-anchor placement at fixed grid zones.
3. Reachability validation + re-roll + fallback.
4. Content fill (already wired) + loot + monster + navmesh spike.
5. 1000-seed validation harness.

**Rollback plan**: if generation proves unstable, the fixed-shell + connector-toggle
path (Alternative 3) remains shippable.

## Validation Criteria

- [ ] Host + 3 clients build a **byte-identical** layout from one seed (PlayMode determinism smoke).
- [ ] 1000-seed harness: every seed → objective + exits reachable (0 unreachable; fallback count tracked).
- [ ] Runtime navmesh valid: monster + players path Entry→Deep on generated layouts.
- [ ] EditMode: stitcher is overlap-free (cell occupancy), capping covers all unused doorways, deterministic (same seed ⇒ same placement).
- [ ] No `NetworkVariable` written from a client path (registry `client_authoritative_state_write` audit).

## Related Decisions

- **Depends on ADR-0001** (host-authoritative networking).
- `design/gdd/map-sequence-and-modular-system.md` (§Modular Room System, §Implementation Status).
- Code: `TowerLayoutGenerator`, `TowerTopology`/`TopoGraph`/`TowerPlanV8`, `TowerLayout`, `RoomSlot`/`RoomDef`/`TowerRoomCatalog`, `LootSpawnPlanner`, `TowerNavBaker`; tests `TowerTopologyTests`, `TowerRoomFillTests`.
- **Outdoor biome approach** (terrain + seeded scatter) = a separate future ADR; no outdoor terrain/nature assets exist yet.
