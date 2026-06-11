# Level / Map Generation & Topology

> **Status**: In Design
> **Author**: Yan Dai (PM) + Claude (zeno/laplace lens)
> **Last Updated**: 2026-06-08
> **Implements Pillar**: production-method — low-cost readable staging, co-op extraction tension, replayability
> **Priority / Layer**: MVP / Feature
> **Related**: `design/levels/abandoned-tower-v3-connectivity.md` (technical blueprint), `docs/architecture/ADR-0001-host-authoritative-networking.md`

## Overview

The Level / Map Generation system turns a fixed pool of authored rooms into a
different-but-always-valid mission space every run. Each floor is modeled as a **graph** —
rooms (and corridor junctions) are nodes; corridors, doors, and stairs are **Connectors**
(edges) that can be open or closed. A **backbone** of always-open edges guarantees the
critical route (entry → objective → exit) and forms a loop, while a set of **seedable
toggle edges** open/close per run to add or remove side loops, shortcuts, and dead-end
loot pockets. From a single shared **seed**, the host resolves which toggles are open,
validates that every required space is still reachable (re-rolling if not), then fills the
now-final rooms with content — and the same seed reproduces the identical map on every
player's machine. For the player this is the system they feel without naming it: the
abandoned tower stays recognizable enough to learn, but the doors that are rubble-blocked
*this* time, the loot rooms worth the detour, and the corridor a monster corners you in are
different enough that no two extractions play the same. Without it, every mission is the
memorized same building; with it, the production-method goals — readable low-cost staging,
co-op extraction tension, and replayability — all hang off one guaranteed-connected,
seed-synced layout.

## Player Fantasy

The fantasy is **"I know this place… mostly."** Players should feel the unease of a
half-learned building — enough orientation to make a plan (the warm show-flat is always
there, the shaft is always mid-floor, the two stairs always land where they landed last
time), but never enough certainty to autopilot. The intended per-run arc: **confidence**
on entry (we've run this tower before) → **friction** when a remembered shortcut is rubble
*this* time and the team re-routes under the clock → **dread** when the only open approach
to the objective threads past the shaft edge or a danger room → **greed tension** at the
loot dead-ends (do we commit to the cul-de-sac with the monster active?) → **commitment**
on the carry-out, escaping through the same layout you scouted coming in.

Mostly it's **infrastructure felt indirectly** — nobody thinks "nice topology graph" — but
it has one **direct, recurring beat**: the door you expected is a blocked rubble pile,
forcing a live re-route decision. That single beat is what sells "this building is
condemned and unreliable," which *is* the Municipal Debt Noir tone — the site looks
ordinary and traversable until its collapse/seal state says otherwise.

**Pillar alignment**: serves *co-op extraction tension* (the in/out asymmetry of a
learned-then-re-routed map) and *readable low-cost staging* (fixed landmarks anchor an
otherwise-varying maze); replayability is the explicit payoff.

## Detailed Design

### Core Rules

1. **Each floor is a graph.** Nodes = rooms + corridor junctions. Edges = **Connectors**.
   The graph is the single source of truth for connectivity; geometry is keyed to it by
   edge id so the two can never drift.
2. **Connector fields**: `id` (stable, for deterministic ordering + tests), `a`/`b`
   (linked node ids), `kind` ∈ {Corridor (4 m) | Door (2 m) | Stair | ScaffoldDrop},
   `fixedOpen`, `toggleable`, `criticalPath`, `geometry` (walkable mesh), `blocker`
   (rubble mesh + `NavMeshObstacle`). `fixedOpen` and `toggleable` are **mutually exclusive**.
3. **Backbone guarantee**: every room hangs off ≥1 `fixedOpen` edge, and the `fixedOpen`
   edges alone form a connected graph that satisfies every reachability invariant. Toggles
   only *add* loops/shortcuts — they can never disconnect the map.
4. **Backbone is a ring, not a spine**: fixed corridors loop through the core so there are
   always ≥2 routes between the entry side and the stairs (no single chokepoint). Every
   `criticalPath` edge is `fixedOpen`; toggling only ever touches non-critical edges.
5. **Cross-floor lock**: stair/descent node positions are **fixed inputs aligned to the
   floor below** (F2 STAIRA2 @ (26,28) over F1 STAIRA1; STAIRB2 @ (0,16) over F1 STAIRB1).
   The generator never moves descents. **Deeper floors escalate** maze density (more
   toggles), winding (longer/zig-zag critical path), and danger (hazards on/near the route,
   objective deeper).
6. **Resolve topology first, content second**: from the seed → derive a topology sub-seed →
   roll each toggleable connector open/closed in stable `id` order → **validate** → apply
   geometry/blocker → *then* run content fill into the final topology.
7. **Open vs closed**: open connector enables `geometry` (walkable, NavMesh passes); closed
   enables `blocker` (rubble) whose `NavMeshObstacle` carves so agents never path through.
8. **Corridor standards**: corridors 4 m, doors 2 m grid-centered, stair cores 4×8, scaffold
   bridge 4 m railless (fall risk); **no critical-path node at a dead-end** (objective /
   stairs / exit / power always on the ring or a through-route); dead-end spurs allowed for
   loot; junctions are first-class nodes; long corridors broken by landmarks (shaft, beacon,
   stair towers).
9. **Exactly three room size classes**: `Small` (≤25 m²) / `Medium` (≤80 m²) / `Large`
   (>80 m²). No other class exists (the legacy "Hall" class is dropped). Slot size and
   content-fill density key off these three only.
10. **Locked floors are never regenerated.** Authored/approved floors — currently **F1,
    locked in the Unity scene** — are read-only inputs. Generation/rebuild operates on the
    **target floor only**; F1 geometry, slots, and connectors must not be modified by an F2
    (or any other) rebuild. *Implication*: the current full-scene `Rebuild v3 Whitebox` menu
    must gain a floor-scoped mode (or preserve F1) before it is used to apply an F2 change.

### States and Transitions

| Stage | State | Transition |
|---|---|---|
| Connector | `Open` / `Closed` | Resolved once at generation; **static for the whole run** |
| Pipeline | `Seeded` | → derive topology sub-seed |
| | `ResolveToggles` | roll each toggleable in id order → `Validate` |
| | `Validate` | flood-fill from entry; check I1–I9 → `Valid` or `Invalid` |
| | `Invalid` | deterministic re-roll (advance RNG fixed step), up to **N** attempts → `Validate`; after N → `Fallback` |
| | `Fallback` | force all toggleables **open** (valid by construction) → `Apply` |
| | `Valid` → `Apply` | enable/disable each connector's geometry/blocker |
| | `Apply` → `FillContent` | run existing slot content fill on final topology |
| | `FillContent` → `Ready` | NavMesh ready; players gain control |

### Interactions with Other Systems

| System | Data in | Data out / contract | Owner |
|---|---|---|---|
| **Networking** (ADR-0001) | the run seed (host→clients) | host resolves topology+content; only the seed crosses the wire; peers reproduce identically | Networking owns transport; this system owns determinism |
| **Scene Flow** | mission-scene load event | generation completes **before** players spawn/gain control | Scene Flow triggers; this system gates "ready" |
| **Interaction Framework** | — | exposes RoomSlots + interactable connectors (e.g. power-gate door) | this system places, Interaction serves |
| **Loot / Content Fill** | final resolved RoomSlots | consumes slots *after* topology is locked | Loot owns fill; this system owns the slot set |
| **Enemy AI** | baked NavMesh (superset + carve) | guarantees a connected navigable mesh per seed | this system owns the mesh; AI consumes |
| **Hazard / Escalation** | room/area + connector refs | hazards (water, collapse) may close areas; must not violate invariants | Hazard owns escalation; this system owns the graph it edits |
| **Mission State Machine** | objective node (TARGET) + exit nodes | I1/I2 guarantee entry→objective→exit reachable (completable run) | Mission owns objective logic; this system guarantees access |

## Formulas

> These are deterministic-RNG / parameter definitions, not balance curves. Defaults come
> from `design/levels/abandoned-tower-v3-connectivity.md` §11. A `systems-designer` balance
> pass was not run (lean mode + values pre-decided) — recommended before production tuning.

**1. Topology seed derivation**

`topoSeed(attempt) = mix(runSeed, floorIndex, attempt)`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| run seed | `runSeed` | int | full int | the one value synced over the wire (ADR-0001) |
| floor index | `floorIndex` | int | 1–N | which floor (so floors differ under one seed) |
| re-roll counter | `attempt` | int | 0–`N_max` | 0 = first try |
| mixer | `mix` | fn | — | deterministic hash (xor + multiply-shift); identical on all peers |

**Output**: a 32-bit seed feeding `System.Random`. Same inputs → same stream on every
machine. *Example*: `runSeed=42, floor=2, attempt=0` → fixed value X on host and all clients.

**2. Per-connector open roll**

`open(c) = Random(topoSeed).NextDouble() < openChance(c)` — evaluated for each `toggleable`
connector in **stable `id` order**; `fixedOpen` edges are always open and never rolled.

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| open chance | `openChance(c)` | float | 0.0–1.0 | per-connector open probability; **default 0.5**, overridable per edge (e.g. bias shaft escape loops higher) |

**Output**: boolean per toggleable connector → the run's open-edge set.

**3. Re-roll resolution**

`resolve(runSeed, floor) = first attempt a ∈ [0, N_max) where Validate(graph(open@a)) passes;
else all-toggles-open`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| max attempts | `N_max` | int | 1–64 | re-roll attempts before fallback; **default 16** |
| validator | `Validate` | predicate | — | flood-fill checking invariants I1–I9 (pure function; no Unity deps) |

**Output**: the final validated open-edge set. Fallback (all-open) is valid by construction,
so this *always* returns a connected map.

**4. Depth escalation (design guideline, not a runtime calc)**

`toggleCount(floor) ≈ base_toggles + k · (floor − 1)` and
`dangerBudget(floor) ≈ base_danger + m · (floor − 1)`

| Variable | Symbol | Type | Range | Description |
|---|---|---|---|---|
| base toggles | `base_toggles` | int | ~9 (F1) | toggle count on the shallowest floor |
| toggle slope | `k` | int | ~2–4 | extra toggles per floor deeper (more loops/winding) |
| base danger | `base_danger` | int | — | hazard/danger-room count on F1 |
| danger slope | `m` | int | — | extra danger per floor deeper |

**Output**: per-floor authoring targets (F1 = 9 toggles; F2 v4 draft currently = 5 — flagged
in Open Questions). Encodes "deeper = windier + more dangerous" as a tunable target.

**Note — room pool is fixed, not generated**: rooms are drawn only from the **three size
classes** (`Small`/`Medium`/`Large`, Rule 9); the generator chooses *which authored rooms*
and *which connectors are open*, never room dimensions or a fourth size. No formula scales
room size.

## Edge Cases

- **If all `N_max` re-rolls fail validation**: force **all toggles open** (valid by
  construction) and log the seed for QA. The run is never broken or aborted for this reason.
- **If a rolled toggle set would isolate a room** (island): caught by invariant **I8** →
  re-roll. By construction it shouldn't happen (every room anchors on a `fixedOpen` edge),
  but the validator double-checks rather than trusting the authoring.
- **If the objective or an exit ends up reachable only behind a toggle**: forbidden by
  Rule 4 (every `criticalPath` edge is `fixedOpen`); **I1/I2/I6** catch any authoring slip →
  treated as a build error, not a runtime re-roll.
- **If NavMesh carve leaks** (an agent paths through a closed rubble blocker): fall back to
  **per-variant baked NavMeshData** loaded by seed (connectivity §8). This is the known
  **MEDIUM-risk** item — must be verified before shipping.
- **If host and client produce different open-edge sets** (version/RNG drift): hash the
  resolved open-edge set; on mismatch, **abort the mission to lobby with a clear error**
  rather than play a desynced map. (Determinism across versions is out of scope — a version
  gate's job.)
- **If a player would stand where a blocker spawns**: cannot happen — generation completes
  **before** players spawn/gain control (Scene-Flow contract).
- **If a regenerate is triggered while the locked F1 is in the scene**: the rebuild must
  operate **floor-scoped** and leave F1 untouched (Rule 10); if it cannot scope to the target
  floor, it **aborts with an error** instead of clobbering F1.
- **If the seed is 0 / unset**: 0 is a valid seed; but the host must always assign a concrete
  seed before sync — there is no "unseeded" run.
- **If a floor has zero toggleable connectors**: valid — the backbone alone passes all
  invariants; the floor is simply identical every run (less variety, not a failure).
- **If a client late-joins after generation**: it receives the seed and reconstructs the
  identical topology; already-changed *content* (looted rooms, opened doors) comes from
  normal state sync, not re-generation.

## Dependencies

**Upstream — this system depends on:**

| System | Hard/Soft | Interface (data flow) |
|---|---|---|
| **Networking** (ADR-0001) | **Hard** | Host computes topology; syncs only `runSeed`. Clients reconstruct. Mismatch → abort (see Edge Cases). |
| **Scene Flow / Game State** | **Hard** | Mission-scene load triggers generation; this system gates "ready" → players spawn only after `Ready`. |
| **Interaction Framework** | **Hard** (for doors) | Connectors that are interactable (doors, power-gate) and RoomSlots are registered as `IInteractable`. Topology *resolution* doesn't need it; in-world door use does. |

**Downstream — these depend on this system** *(all undesigned — contracts provisional)*:

| System | Hard/Soft | What they consume |
|---|---|---|
| **Loot / Content Fill** | **Hard** | The final resolved `RoomSlot` set, after topology is locked. |
| **Enemy / Monster AI** | **Hard** | A baked, connected, leak-free NavMesh per seed (superset + carve, or per-variant). |
| **Mission State Machine** | **Hard** | Objective node (TARGET) + exit nodes; the I1/I2/I6 reachability guarantee that the run is completable. |
| **Hazard / Escalation** | **Soft** | Room/area + connector refs to close areas dynamically; must not break invariants. Map works without it. |
| **Environment / Style Director** | **Soft** | The resolved rooms to dress (Municipal Debt Noir pass). Map works un-dressed (whitebox). |

**Bidirectional note**: when each downstream GDD is authored, it must list "depends on
Level/Map Generation" with the matching interface. Flagged for `/consistency-check`.

## Tuning Knobs

| Knob | Default | Safe range | Too low → | Too high → | Interacts with |
|---|---|---|---|---|---|
| `openChance` (per connector) | 0.5 | 0.2–0.8 | lots of rubble; runs feel samey & cramped, more re-rolls | most loops open; little variety, easier nav | re-roll rate; validator |
| `N_max` (re-roll cap) | 16 | 4–32 | frequent fallback-to-all-open (kills variety) | wasted compute (cheap — pure fn, negligible) | `openChance` (lower chance → more re-rolls needed) |
| `base_toggles` (F1) | ~9 | 6–14 | floor barely varies | unreadable maze, orientation lost | readability landmarks |
| `k` (toggles added per deeper floor) | 2–4 | 1–5 | depth feels flat | deep floors chaotic | danger budget |
| `base_danger` / `m` (danger per floor) | TBD | — | no tension | unfair, frustrating | objective depth; Hazard system |
| per-connector `openChance` override | — (=default) | 0–1 | n/a | a forced-open "loop" stops adding variety | validator (don't force-open something that breaks an invariant assumption) |

**Authoring-time config (not runtime knobs, but the main tuning lever)**: the per-floor
**graph itself** — which edges are `criticalPath` / `fixedOpen` / `toggleable`, room size
assignments (S/M/L only), and the locked descent positions. These are edited in the topology
definition, validated by the headless tests, and are how "F2 should be windier/more
dangerous than F1" is actually dialed in.

## Visual/Audio Requirements

This system *produces the space*; final materials belong to the Environment/Style Director,
but generation must **support these art beats** (it owns where they go):

- **Readable landmarks** so the varying maze stays navigable: the **SHOWFLAT amber beacon**,
  the central **EDGE shaft** (vertical darkness + stamp-red hazard lip), and the two **stair
  towers** (A exposed/grey daylight bleed, B dark/sodium-failed). Long corridors must be
  broken by these.
- **Connector open vs closed need two readable looks** — and the closed look is a
  *storytelling beat*: open = clear threshold; **closed = rubble / tarp / sealed pile** in
  dead-rubber black with stamp-red seizure paper. "The door you remembered is now rubble"
  must read instantly.
- **Objective room**: the **seized sand-table** under an amber key light with a `查封 SEIZED`
  stamp-red notice; debt/eviction paper on EXEC/VIP doors.
- **Light palette by role**: amber = beacon/objective key · civic teal = hub/circulation ·
  stamp red = danger/debt · dispatch green = safe/extract · dead-rubber = unlit back-of-house.
- **Audio (brief)**: closed-connector rubble has a settling/ambient cue; the shaft carries a
  vertical air/echo bed; safe beacon rooms are quieter. Full spec → Audio system.

📌 **Asset Spec** — after the art bible is approved, run `/asset-spec system:level-map-generation`
for per-asset specs of the connector-closed rubble, the sand-table objective, and the landmark set.

## UI Requirements

No dedicated screen. **Dev-only**: an optional debug overlay showing the active `runSeed` +
resolved open-edge set (for reproducing a reported map). Player-facing orientation is the
HUD/minimap system's job reading the fixed landmarks — not this system. *(No `/ux-design`
needed.)*

## Acceptance Criteria

> Maps to the headless test spec in `design/levels/abandoned-tower-v3-connectivity.md` §9.
> A `qa-lead` independent pass is recommended before these become test stories.

- **GIVEN** any seed, **WHEN** topology resolves on host and clients, **THEN** all peers
  produce an **identical open-edge set** (hash match); a mismatch aborts to lobby. *(Formula 1–2)*
- **GIVEN** all toggles forced closed, **WHEN** validated, **THEN** entry→objective→exit and
  **every room** are reachable — the backbone alone is valid (I1, I2, I8). *(Rule 3)*
- **GIVEN** seeds 1..1000, **WHEN** each resolves through the re-roll loop, **THEN** all pass
  I1–I9, and fallback-to-all-open is required at **≤ an allowed rate**. *(Rule 6, Formula 3)*
- **GIVEN** a generated floor, **WHEN** flood-filling from entry, **THEN** there are **no
  island rooms** (I8). *(Rule 3)*
- **GIVEN** any seed, **WHEN** descents resolve, **THEN** **≥2** of {Stair-A, Stair-B,
  Scaffold-Drop} are open (I7 — no single campable descent). *(Rule 4)*
- **GIVEN** the validator, **WHEN** called with a graph, **THEN** it returns a **deterministic
  verdict with zero Unity/scene dependency** (pure function, runs headless in CI). *(test §9.6)*
- **GIVEN** F2 generation, **WHEN** stairs are placed, **THEN** STAIRA2 = (26,28) over F1
  STAIRA1 and STAIRB2 = (0,16) over F1 STAIRB1, and the descents physically connect. *(Rule 5)*
- **GIVEN** an F2 regenerate, **WHEN** it runs, **THEN** F1 geometry/slots/connectors are
  **unchanged** — or the rebuild aborts rather than touching F1. *(Rule 10)*
- **GIVEN** a closed connector, **WHEN** an agent pathfinds, **THEN** it **never** paths
  through the rubble (carve seals, or per-variant bake used). *(Rule 7)*
- **GIVEN** generation, **WHEN** it completes, **THEN** players gain control only after
  `Ready` — no player exists while blockers spawn. *(Scene-Flow contract)*
- **GIVEN** a closed connector in-world, **WHEN** a player sees it, **THEN** it reads as
  rubble/sealed, visually distinct from an open threshold. *(Visual)*
- **GIVEN** generation on mission load, **WHEN** it runs, **THEN** topology resolve + validate
  completes within budget (target < a few ms; pure graph) and NavMesh is ready before control. *(perf)*

## Open Questions

| # | Question | Owner | Notes |
|---|---|---|---|
| 1 | **NavMesh strategy**: superset-bake + carve, or per-variant baked data? | laplace | Decide after a carve **leak test** on the irregular plan (connectivity §8). The MEDIUM risk. |
| 2 | **F2 toggle count vs depth-escalation**: v4 draft has **5** toggles, but F1 has 9 and the escalation rule says deeper = *more*. Reconcile: raise F2 toggles, or revise the formula. | zeno | Flagged in Formula 4. |
| 3 | **F1-locked vs full-scene rebuild**: `Rebuild v3 Whitebox` regenerates both floors. Need a **floor-scoped rebuild mode**, or author F2 in-scene by hand. | laplace + PM | Connectivity §11.1: code+editor-tool vs explicit scene-YAML go-ahead (per @AGENTS.md). |
| 4 | **Scaffold-drop landing**: confirm BALCONY drop lands coherently relative to F1 DOCK. | banach | Grounding note from art review. |
| 5 | **`base_danger` / `m` values**: danger budget per floor is TBD. | zeno + systems-designer | Needs a balance pass. |
| 6 | **`openChance` overrides**: which loops bias open (e.g. shaft escape ring) for guaranteed kiting routes? | zeno | Tuning. |
