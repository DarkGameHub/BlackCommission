# v3 Connectivity & Corridor Spec — the blueprint for the generator rewrite

> Companion to `abandoned-tower-redesign-v3.md`. This is the part PM Yan Dai flagged:
> **connectivity and corridor design must be guaranteed, not assumed.**
>
> **Key technical finding:** the current Level scripts (`RoomDef`, `RoomSlot`,
> `TowerLayout`, `TowerLayoutGenerator`, `TowerRoomCatalog`) have **no topology model
> at all** — they fill *content* into a hand-authored, static shell. Corridors/doors/
> connectivity live only in the scene and never change. Once topology is seed-randomized,
> connectivity **must be modeled in code and validated every run**. This doc specifies
> that new layer. Status: **DRAFT for approval; no code until signed off.**

---

## 1. The model: rooms are nodes, corridors/doors are edges

Add a **Connector** layer on top of the existing slot system. The existing determinism
(seeded `System.Random`, stable slot ordering, seed-only-over-the-wire) is **kept**; we
extend it with a graph and a validator.

```
Node   = a RoomSlot (or a corridor junction)         // existing RoomSlot, +junctions
Edge   = a Connector between two nodes
Connector {
    string  id;            // stable, for deterministic ordering + tests
    string  a, b;          // slotId / junctionId it links
    EdgeKind kind;         // Corridor (4m) | Door (2m) | Stair | ScaffoldDrop
    bool    fixedOpen;     // TRUE = never toggled (critical path & ring backbone)
    bool    toggleable;    // TRUE = seed may open/close it (the ~~~ set)
    bool    criticalPath;  // TRUE = part of VAN→objective→VAN; MUST stay open
    GameObject geometry;   // the corridor/door mesh+collider to enable/disable
    GameObject blocker;    // rubble/tarp/locked mesh + NavMeshObstacle when closed
}
```

A Connector that is **open** enables `geometry` (walkable, NavMesh links/carves through);
**closed** disables `geometry`, enables `blocker` (rubble) which **carves a NavMeshObstacle**
so agents never path through it. Door/corridor widths are fixed (see §5).

**Rule:** `fixedOpen` and `toggleable` are mutually exclusive. Every `criticalPath` edge
is `fixedOpen`. Toggling only ever touches **non-critical** edges (side loops, shortcuts,
collapse gaps).

---

## 2. Generation order (extends `TowerLayout.Fill`)

1. **Resolve topology first, content second.** Before filling rooms, decide which
   toggleable connectors are open for this seed.
2. From `seed`, derive a topology sub-seed; for each toggleable connector (iterated in
   stable `id` order), roll open/closed at its `openChance`.
3. **Validate** (flood-fill, §3). If any invariant fails, **deterministically re-roll**:
   advance the topology RNG by a fixed step and retry, up to N attempts; if still failing,
   fall back to **all-toggleables-open** (always valid by construction — see §4).
4. Apply: enable/disable each connector's `geometry`/`blocker`.
5. **Then** run the existing content fill (`TowerLayout.Fill`) into the now-final topology.

Determinism preserved: same seed → same toggle result → same validation outcome → same
re-roll path → identical topology AND content on every peer. Only the seed crosses the
wire.

---

## 3. The reachability validator (flood-fill from the van)

After deciding open edges, build the open graph and BFS/flood-fill from `Van`. Assert:

| # | Invariant | Why |
|---|-----------|-----|
| **I1** | `Van → Lobby → (Stair-A OR Stair-B) → F2 → Objective` reachable | the run must be completable |
| **I2** | `Objective → (Stair-A OR Stair-B) → … → Van` reachable | must be able to carry it out |
| **I3** | **PowerGate** reachable from Van **without** crossing to F2 | gate must be solvable before ascent |
| **I4** | **PowerGate clue room** (Temp Office) reachable before any stair | the gate must be discoverable |
| **I5** | **≥1 consumable room** reachable before any stair | teach gear use early |
| **I6** | **Fire Exit** reachable from the objective floor | the escape valve must exist |
| **I7** | **≥2 of {Stair-A, Stair-B, Scaffold-Drop}** open | no single campable descent chokepoint |
| **I8** | **every room node reachable** from Van (no island rooms) | no orphaned loot/dead pockets |
| **I9** | shaft fall-gaps never the *only* link between two otherwise-separated areas | falling must never be mandatory |

Any failure → re-roll (§2 step 3). I7 and I8 are the two the irregular footprint most
threatens — they are the explicit answer to "注意连通性".

> A pure-function version of this validator (graph in → pass/fail + reason) is what the
> EditMode tests call, so connectivity is provable headlessly without a scene.

---

## 4. Why a valid layout always exists (fallback safety)

The **backbone** (all `fixedOpen` edges) already forms a connected graph that satisfies
I1–I9 on its own — it is the v3 corridor ring + the critical path + both stairs + the one
fire-exit corridor, none of which are toggleable. Toggleable edges only **add** loops and
shortcuts. Therefore "all toggleables open" is always valid, so the fallback in §2 step 3
can never produce a broken map. Toggling can only make the maze *more* interesting, never
*disconnected*.

This is DunGen's "main path always valid, branches vary," enforced by construction.

---

## 5. Corridor design standards (the "走廊设计" rules)

1. **Backbone = a ring, not a spine.** Each floor's fixed corridors form a **loop**
   through the core so there are always **≥2 routes** between the van side and the stairs
   (kills the single-chokepoint death). The v2 "main spine" is explicitly replaced.
2. **Width**: corridors 4 m (1 G); doors 2 m, centered on a grid line. Stair cores 4×8.
   Scaffold bridge 4 m but railless (fall risk). These are fixed so NavMesh bakes clean.
3. **Dead-ends are allowed but capped**: a dead-end spur (loot/optional room) is fine, but
   **no critical-path node sits at the end of a dead-end** — objective, power, stairs, van,
   fire exit always sit on the ring or a through-route.
4. **Sightline discipline**: long straight corridors are broken by the shaft, the show-flat
   beacon, and the two stair towers so the player has *some* orientation amid repetition.
5. **Junctions are nodes too**: where 3+ corridors meet, author a junction node so the
   graph/validator treats it as a first-class vertex (not an implicit crossing).
6. **Irregular ≠ disconnected**: a jutting wing must connect to the ring by **at least one
   fixedOpen** corridor; only its *internal* and *cross-wing* links may be toggleable.

---

## 6. Floor 1 — connection graph (nodes + edges)

```
                         ▲ FIRE EXIT
                          |  (F1 far north)
        ░COLLAPSE░ ~T6~ [FOREMAN] === [CANTEEN] === [DORM]!
            |                |                         |
           ~T5~          [STAIRA]⇣                   ~T3~
            |                |                         |
        [WAREHOUSE] ==== J-NE ======= J-N ============ J-NW
            |                            |              |
           ~T4~                       [HALL+SHAFT] ==== J-W
            |                            |              |
        [STAIRB]⇣ === J-SW === [WORKSHOP] ~T2~ [SAMPLE] |
            |          |           |                    |
         [TEMP]!clue  [PWR]⚡    [SECUR]               |
            |  ~T1~     |                              |
            +========= [LOBBY] ★ ========================+
                          |
                        ◆ VAN  (extraction, F1 south)
```

`===` fixedOpen corridor (ring/backbone) · `~Tn~` toggleable connector · `⇣` stair down

### F1 edge table (representative; final ids authored on the slots)

| id | A ↔ B | kind | fixed/toggle | critical? |
|----|-------|------|--------------|-----------|
| E-VAN | Van ↔ Lobby | Corridor | fixed | ✔ |
| E-LH | Lobby ↔ Hall | Corridor | fixed | ✔ |
| E-HW | Hall ↔ J-W ↔ Workshop | Corridor | fixed | — (ring) |
| E-HNW | Hall ↔ J-NW | Corridor | fixed | — (ring) |
| E-NW-N | J-NW ↔ J-N ↔ Dorm/Canteen | Corridor | fixed | — (ring) |
| E-N-NE | J-N ↔ J-NE ↔ Foreman | Corridor | fixed | — (ring) |
| E-NE-SA | J-NE ↔ Stair-A | Corridor | fixed | ✔ (a descent) |
| E-LSW | Lobby ↔ J-SW | Corridor | fixed | — (ring back to west) |
| E-SW-SB | J-SW ↔ Stair-B | Corridor | fixed | ✔ (a descent) |
| E-SW-PWR | J-SW ↔ Power Room | Corridor | fixed | ✔ (gate access) |
| E-PWR-TEMP | Power ↔ Temp Office (clue) | Door | fixed | ✔ (clue) |
| E-FIRE | Foreman ↔ Fire Exit | Door | fixed | ✔ (escape) |
| T1 | Temp ↔ Sample | Door | toggle | — |
| T2 | Workshop ↔ Sample | Door | toggle | — |
| T3 | Dorm ↔ (north shortcut) | Corridor | toggle | — |
| T4 | Warehouse ↔ Stair-B | Door | toggle | — |
| T5 | Warehouse ↔ Collapse | Corridor | toggle | — |
| T6 | Collapse ↔ Foreman/Fire | Corridor | toggle | — |

Note: the **ring** (Lobby–Hall–J-NW–J-N–J-NE–Stair-A and Lobby–J-SW–Stair-B) is all
fixed, so I1/I2/I7/I8 hold even with **every** toggle closed. Toggles add the west/collapse
loops and cross-shortcuts that make each run feel different.

---

## 7. Floor 2 — connection graph

F2 is a smaller offset cap; only Stairs A/B and the Shaft align with F1. The **scaffold
bridge** is the only direct E↔W; the safe alternative is the long Stair-B west loop.

```
   [DEEP TARGET ☠] === [EXEC] ~T7~ [MODEL] === [SALES] === [STAIRA]⇣
        |  (objective)                                          |
   [SHOW-FLAT ★] === J-W2                                    J-E2
        |              |                                       |
   [STAIRB]⇣ === [SHAFT-EDGE(L)] ==[SCAFFOLD BRIDGE]== J-MID == [VIP]
        |              |                                       |
      ~T9~          [MAINT] ~T8~ [DANGER]                  [BALCONY]⇣
        |              |            |                          (scaffold drop → F1)
        +===== (Stair-B west safe loop) ======================+
```

### F2 edge table (representative)

| id | A ↔ B | kind | fixed/toggle | critical? |
|----|-------|------|--------------|-----------|
| E-TS | Deep Target ↔ Show-flat | Door | fixed | ✔ (objective access) |
| E-SF-W2 | Show-flat ↔ J-W2 | Corridor | fixed | ✔ |
| E-W2-SB | J-W2 ↔ Stair-B | Corridor | fixed | ✔ (descent) |
| E-W2-EDGE | J-W2 ↔ Shaft-Edge | Corridor | fixed | — (ring) |
| E-BRIDGE | Shaft-Edge ↔ J-MID (scaffold bridge) | Corridor | fixed | — (direct E↔W) |
| E-MID-E2 | J-MID ↔ J-E2 ↔ Sales | Corridor | fixed | — (ring) |
| E-E2-SA | J-E2 ↔ Stair-A | Corridor | fixed | ✔ (descent) |
| E-TARG-EXEC | Deep Target ↔ Exec ↔ Model ↔ Sales | Corridor | fixed | — (north ring) |
| E-BALC | VIP ↔ Balcony (scaffold drop) | Door | fixed | ✔ (3rd descent) |
| T7 | Exec ↔ Model | Door | toggle | — |
| T8 | Maint ↔ Danger | Door | toggle | — |
| T9 | Show-flat ↔ Stair-B (back) | Door | toggle | — |

Inter-floor edges: **Stair-A**, **Stair-B** (both fixedOpen, aligned), **Scaffold-Drop**
(Balcony F2 → ground near F1 Dock, fixedOpen, one-way down). I7 ⇒ at least two of these
three are always open (all three are fixed here, so I7 holds trivially; toggles never touch
descents).

---

## 8. NavMesh strategy (decide at build)

- **Bake the full superset** (all connectors' `geometry` present) once. Each toggleable
  connector carries a **`NavMeshObstacle` (carve)** on its `blocker`; closing the connector
  enables the obstacle so agents re-path around it. Pro: one bake; Con: obstacles must fully
  seal the opening.
- *Alternative* (if carving proves leaky on the ragged plan): bake per-variant NavMeshData
  offline for the finite toggle combinations and load by seed. The toggle set is small and
  finite, so this is feasible but heavier. **Recommend superset+carve first; fall back if
  agents path through rubble.**
- Verify: agents traverse Stairs A/B; agents do **not** path across shaft fall-gaps or the
  railless scaffold unless intended; closed connectors fully block.

---

## 9. Tests (EditMode, headless — the connectivity guarantee)

1. `topology_allTogglesClosed_criticalPathReachable` — backbone alone satisfies I1–I3,I6,I8.
2. `topology_randomSeeds_allInvariantsHold` — N seeds (e.g. 1..1000) each pass I1–I9 after
   the re-roll loop; assert no fallback-to-all-open is ever *needed* beyond the allowed rate.
3. `topology_sameSeed_identicalOnHostAndClient` — same seed → identical open-edge set.
4. `topology_noIslandRooms` — I8 explicitly: flood-fill covers every room node for N seeds.
5. `topology_atLeastTwoDescents` — I7 for N seeds.
6. `validator_isPureFunction` — graph in → deterministic verdict, no scene/Unity deps.

These run without a scene (pure graph), so connectivity is provable in CI and is exactly the
high-risk, server-authoritative, seed-synced area called out in `systems-index.md` and
ADR-0001's validation criteria.

---

## 10. Code changes this implies (for the rebuild — task #3)

1. **`RoomSizeClass`**: drop `Hall` → `Small/Medium/Large` only (update gizmo box switch).
2. **New `Connector.cs`** (MonoBehaviour or data) + a `TowerConnectorGraph` collector
   (mirrors `FindSlots`).
3. **New `TowerTopology.cs`**: pure functions — `ResolveOpenEdges(seed)`,
   `Validate(graph) → (bool, reason)`, `ResolveWithReroll(seed) → openSet`.
4. **`TowerLayout.Fill`**: call topology resolution+apply **before** content fill.
5. **`TowerLayoutGenerator`**: unchanged seed/netcode flow; just also drives topology.
6. **Tests** in `tests/` per §9 (first automated tests in the project — also closes part of
   the test gap).

> Scene authoring (placing junction nodes, corridor/blocker geometry, slot ids) edits scene
> YAML. Per `@AGENTS.md` I will **not** rewrite scene YAML without your explicit go-ahead —
> I can instead deliver the code + an editor tool/checklist to author it, or do the scene
> pass if you tell me to. Flagged for the §11 decision.

---

## 11. Open decisions before code

1. **Scope of the rebuild now**: (a) code layer + EditMode tests only (safe, no scene YAML),
   then you author the scene with my checklist/tool; or (b) I also do the scene authoring
   (needs explicit YAML-edit go-ahead per `@AGENTS.md`).
2. **NavMesh**: confirm "superset + carve" first (§8), fall back to per-variant if leaky.
3. Toggle `openChance` default (e.g. 0.5) and re-roll attempt cap N (e.g. 16) — tunable.
```
