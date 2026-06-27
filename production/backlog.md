# Black Commission — Backlog

Deferred work, not in the active sprint. Pull into a sprint when prioritized.

## Audio

- **[B] Holistic audio quality overhaul (`/team-audio`)** — added 2026-06-13.
  - **Why**: every SFX is procedurally synthesized in `SynthAudio` (raw tones/noise) → reads as cheap beeps; PM: "太不好听". Audio is also a required feedback/accessibility channel (glass thud = completeness-loss, breaker buzz = progress, stamp = settlement) per `design/ux/hud.md`, so deleting it is not an option.
  - **Scope**: run the full `/team-audio` pipeline — `audio-director` (sonic identity + palette) → `sound-designer` (per-event SFX specs, mix groups, ducking) → `technical-artist` (bus structure, budgets) → `gameplay-programmer` (re-implement `AudioManager`/`SynthAudio` or move to real assets). Produces `design/audio/…` then implementation.
  - **PM intent**: "我肯定是做 B" — committed, just deferred until after the current UI pass.
  - **Note**: a quick stop-gap (synth warmth + mix pass, or master SFX mute) was offered and declined in favor of doing B properly later.

## Procedural maps (ADR-0003)

- **Multiplayer 4-peer determinism smoke** — *deferred 2026-06-19 by PM ("先不管多端一致了").*
  `GridMapNetworkBuilder` is already written + compiles (server rolls seed → `NetworkVariable<int>` → every
  peer rebuilds the layout locally). What's left is the Play-mode proof: open Multiplayer Play Mode (2+
  instances) on the map-2 scene and confirm all peers build a **byte-identical** layout from the one synced
  seed. The generator determinism underneath is already proven headless (`GridMapReachabilityHarnessTests`:
  1000-seed byte-identical, 0 unreachable), so this is a wiring/Play smoke, not a logic risk.

- **Indoor double-wall z-fighting (old module path)** — the per-cell module instantiator
  (`GridLayoutInstantiator`) placed two coplanar walls at every shared edge. Superseded for map 2 by the
  **edge-based `MapSiteBuilder`** (each edge built exactly once → no doubles, no holes, real front door). If
  `MapSiteBuilder` fully replaces the corridor path, retire the module-based instantiator + `Corridor_*`/
  `Junction_*` prefabs (keep `Room_*` for anchor rooms if used).

- **Outdoor art pass** — *noir whitebox pass DONE 2026-06-20 (hybrid direction, zero-import): locked-palette
  recolor + dead-tree silhouettes + derelict industrial-yard dressing + Tirgames night skybox; verified green,
  awaiting PM visual sign-off.* **Remaining (deferred):** swap the procedural industrial dressing for real
  Tirgames/Sat prefabs (needs Resources wiring or a serialized ref array on `MapSiteRuntime` to stay
  runtime-safe), and/or import a stylized dead-foliage/terrain pack re-mapped to the palette. The seeded
  `OutdoorScatterGenerator` placement + `BuildYardDressing` seams stay; only the meshes/materials swap.

- **Runtime navmesh for the big map (NavMeshSurface, tiled)** — *2026-06-19.* On the revised map 2 (28×24
  interior + ~2000-tree forest, ~3500 objects), a single `NavMeshBuilder.BuildNavMeshData` call over the whole
  site **fragments** (DROP-OFF→DEEP came back `PathPartial` — a deep island), so the headless test now verifies
  indoor traversal by pure-logic reachability + the outdoor approach by a scoped bake. This is a bake-method
  limit, NOT a geometry block (players walk by physics; the generator guarantees ENTRY→DEEP). When the monster
  is wired onto this map, bake the runtime navmesh with **`NavMeshSurface` (auto-tiled)**, which handles large
  areas — and verify the full DROP-OFF→DEEP path then.

- **Map 2 perf pass** — the whitebox site spawns ~3500 primitives on load (≈2000 forest + ≈1500 interior),
  a one-time hitch at generation. Fine for whitebox/playtest; later: combine meshes / pool / cull / reduce
  forest density (`OutdoorScatterGenerator` fill) once the feel is locked.

- **Map 2 → 20-min loop content** — the SPACE is now big + winding + getting-lost, but a true 20-min level
  also needs objectives, searchable loot (van-weight gate), locked-area/key progression, and monster pressure
  layered onto this map. Space done; gameplay-pacing is the next design+impl pass.

- **Map 2 multiplayer layout determinism (ADR-0003 seed-sync)** — *IMPLEMENTED 2026-06-26 (pending compile + 2-4p playtest verify).*
  `MapSiteRuntime` is now a `NetworkBehaviour`: the server rolls one seed → `NetworkVariable<int>` → every peer
  rebuilds the identical layout from it (`MapSiteBuilder.Build` is seed-deterministic — generator scan confirmed
  all RNG is `System.Random(seed)`, no unseeded/Time/hash-set order). Offline keeps a local-seed fallback so the
  solo walk-test still works. `Map2SceneBuilder` bakes a NetworkObject on MapSite. The host-authoritative
  `LootSpawner` fills the now-identical anchors + replicates the items, so loot lands correctly on every peer.
  Mirrors `GridMapNetworkBuilder` / `TowerLayoutGenerator` seed-sync. **Verify:** 2-4p PlayMode — host + clients
  build byte-identical layouts + see the same loot at the same spots.
