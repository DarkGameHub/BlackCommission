# Systems Index — Black Commission (黑色外包 / 外包事故组)

> **Status**: Approved (upgraded from as-built scan to full decomposition)
> **Created**: 2026-06-06 (as-built scan)
> **Last Updated**: 2026-06-08 (upgraded via /map-systems: + missing systems, dependency layers, priority tiers, design order)
> **Source Concept**: design/game-concept.md · pillars: design/game-pillars.md · core loop: docs/mvp-core-loop.md
>
> Bridges the framework to the real Unity project. Game code lives in
> `Assets/_Project/Scripts/<System>/`, NOT `src/`. **No per-system GDDs exist yet**
> (`design/gdd/` is empty) — this index defines the order to author them.

---

## Overview

Black Commission is a 1–4 player host-authoritative co-op "commission-running" game.
Identity: **Municipal Debt Noir**. The mechanical scope is organized around one
signature loop — `HQ office → office computer (accept job) → gear/shop → dispatch van
→ in-van transit → mission site → objective / partial-return choice → van return →
HQ settlement` — plus the systems that make a single mission tense and replayable
(procedural level topology, loot, one or more monsters, escalating hazards) and the
distinctive economic squeeze (funds/debt/reputation, acquisition, hostile-takeover
pressure). Most of the loop is already implemented in code; the highest-value design
work now is documenting the **procedural level/topology system** (active: the
abandoned-tower map) and the high-sync-risk **mission state machine** and
**economy/settlement** systems.

---

## Systems Enumeration

| # | System Name | Category | Priority | Status | Design Doc | Depends On |
|---|-------------|----------|----------|--------|------------|------------|
| 1 | Networking | Core (Foundation) | MVP | Implemented | — (`docs/mvp-core-loop.md`) | — |
| 2 | Interaction Framework (inferred) | Core (Foundation) | MVP | Implemented | — | Networking |
| 3 | Scene Flow / Game State (inferred) | Core (Foundation) | MVP | Implemented | — | Networking |
| 4 | Player | Core | MVP | Implemented | — | Networking, Interaction |
| 5 | Office / HQ Economy & Progression | Progression / Economy | MVP | Implemented | `docs/mvp-core-loop.md` | Networking, Save, Scene Flow |
| 6 | Save / Persistence (inferred) | Persistence | MVP | Partial | — | — |
| 7 | Mission State Machine | Gameplay | MVP | Implemented | `docs/mvp-core-loop.md` | Networking, Scene Flow, Player, Interaction |
| 8 | Level / Map Generation & Topology (inferred) | Gameplay | MVP | Designed (pending review) | `design/gdd/level-map-generation.md` | Scene Flow, Interaction, Networking |
| 9 | Loot / Content Fill & Item Economy (inferred) | Economy | Vertical Slice | Stub | — | Level Gen, Save |
| 10 | Equipment & Consumables | Economy / Gameplay | MVP | Stub (flashlight only) | — | Player, Item Economy, Office |
| 11 | Enemy / Monster AI | Gameplay | MVP | Partial | — | Networking, Level Gen (NavMesh), Player |
| 12 | Van Transit Ritual | Gameplay | MVP | Partial | `@AGENTS.md` | Scene Flow, Networking, Mission |
| 13 | Hazard / Escalation (inferred) | Gameplay | Vertical Slice | Not Started | — | Mission, Level Gen, Player |
| 14 | Carry / Objective Transport | Gameplay | Alpha | Partial | — | Player, Interaction, Mission |
| 15 | HUD / Office Computer UI | UI | MVP | Implemented | `docs/mvp-core-loop.md` | Office Economy, Mission, Equipment, Player |
| 16 | Settlement UI | UI | MVP | Implemented | — | Mission, Office Economy |
| 17 | Main Menu / Settings | UI | MVP | Implemented | — | Networking, Scene Flow |
| 18 | Audio / Proximity Voice | Audio | Vertical Slice | Partial | — | Networking |
| 19 | Environment / Style Director | Meta / Presentation | Vertical Slice | Stub/Partial | `docs/art/black-commission-style-lock-v1.md` | Level Gen, Scene Flow |

> "(inferred)" = surfaced during decomposition; not in the original as-built scan.
> Counting note: 19 rows; "Networking" through "Environment". Foundation x3, Core x4,
> Feature x7, Presentation x3, Polish x2.

---

## Code Locations (as-built, 2026-06-06 scan)

| System | Code Location | Files | Key Components |
|--------|---------------|-------|----------------|
| Networking | `Assets/_Project/Scripts/Network/` | 10 | ConnectionManager, DisconnectHandler, HQController, HQSpawnManager, MvpConnectionLimiter, ProximityVoiceChat, AutoPort, QuickNetworkUI |
| Mission | `Assets/_Project/Scripts/Mission/` + `Scripts/MVP/` | 12 | LostItemMissionManager, MvpMissionClock, SchoolEntranceDoor, SchoolExitPoint, HidingSpot, evidence/homework items, MissionTimeOfDayDirector |
| Office / Economy | `Assets/_Project/Scripts/Office/` | 16 | OfficeComputer, OfficeTaskDefinition, MvpMissionRuntime, MvpPendingReward, CompanyData, SaveIO, OfficeDepartureVan, OfficeMonsterBestiary, OfficeCabinetStorage |
| Player | `Assets/_Project/Scripts/Player/` | 15 | PlayerController, PlayerInteraction, PlayerHotbar, CarrySystem, PlayerHealth, PlayerOxygen, PlayerCameraController, PlayerFirstPersonRig, PlayerCharacterModels/Palette |
| UI / HUD / Settlement | `Assets/_Project/Scripts/UI/` | 11 | MvpHud, SettlementUIController, MainMenuUI, SettingsOverlay, BlackCommissionUiTheme |
| Level / Map Gen | `Assets/_Project/Scripts/Level/` + `Editor/Tower*` | — | TowerTopologyV3, TopoGraph, TowerLayoutGenerator, RoomSlot, Connector, TowerV3WhiteboxBuilder |
| Van transit | `Assets/_Project/Scripts/Van/` + `MVP/VanTransitOverlay` | 2 | OfficeDepartureVan, VanTransitOverlay |
| Equipment | `Assets/_Project/Scripts/Equipment/` | 1 | flashlight only |
| Enemy AI | `Assets/_Project/Scripts/MVP/SchoolMonsterAI.cs` | 1 | SchoolMonsterAI (NavMeshAgent chase) |
| Environment | `Assets/_Project/Scripts/Environment/` + `MVP/MvpSceneStyleDirector` | 2 | billboard stub, runtime style director |
| Audio | `Assets/_Project/Scripts/Audio/` + `Network/AudioManager` | 2 | AudioManager, ProximityVoiceChat |

### Scenes
- `Assets/_Project/Scenes/HQ.unity` — HQ (also runtime-generated via `MvpSceneStyleDirector`)
- `Assets/_Project/Scenes/Snow_Lotus_01.unity` — current playable mission (白棘雪莲)
- `Assets/_Project/Scenes/School_LostItem_01.unity` — school lost-item MVP mission
- `Assets/Scene/AbandonedBuilding_Blockout.unity` — abandoned-tower blockout (procedural)

---

## Categories

| Category | Used by |
|----------|---------|
| **Core/Foundation** | Networking, Interaction Framework, Scene Flow, Player |
| **Gameplay** | Mission State Machine, Level Gen, Enemy AI, Van Transit, Hazard, Carry |
| **Progression / Economy** | Office Economy & Progression, Loot/Item Economy, Equipment |
| **Persistence** | Save / Persistence |
| **UI** | HUD/Computer, Settlement, Main Menu |
| **Audio** | Audio / Proximity Voice |
| **Meta / Presentation** | Environment / Style Director |

---

## Priority Tiers

| Tier | Definition | Target Milestone |
|------|------------|------------------|
| **MVP** | Required for the office→mission→office loop to function and be testable for fun | First playable (school lost-item + tower) |
| **Vertical Slice** | Required for one complete, polished mission with loot, hazard, audio, set-dressing | Demo |
| **Alpha** | All gameplay present in rough form (more monsters, carry, more job categories) | Alpha |
| **Full Vision** | Polish, accessibility, full 8-category content, full acquisition strategy layer | Beta / Release |

---

## Dependency Map

### Foundation Layer (no dependencies)
1. **Networking** — host-authoritative bedrock; every synced system rides on it.
2. **Interaction Framework** — `IInteractable` + server-validated E-key; objectives, doors, computer, pickups all plug in. (Uses Networking for validation.)
3. **Scene Flow / Game State** — menu→HQ→mission→settlement→HQ orchestration + build settings + runtime handoff; everything is sequenced by it.

### Core Layer (depends on foundation)
1. **Player** — depends on: Networking, Interaction.
2. **Save / Persistence** — depends on: — (serialization); serves Economy. (Foundation-adjacent but exists to back Economy → listed Core.)
3. **Office / HQ Economy & Progression** — depends on: Networking, Save, Scene Flow. Owns the distinctive acquisition + hostile-takeover pressure.
4. **Mission State Machine** — depends on: Networking, Scene Flow, Player, Interaction.

### Feature Layer (depends on core)
1. **Level / Map Generation & Topology** — depends on: Scene Flow, Interaction (room slots/doors), Networking (toggle/seed sync). Feeds Loot, Enemy spawn, Hazard, NavMesh.
2. **Loot / Content Fill & Item Economy** — depends on: Level Gen (RoomSlots), Save.
3. **Equipment & Consumables** — depends on: Player (hotbar), Item Economy, Office (shop).
4. **Enemy / Monster AI** — depends on: Networking, Level Gen (NavMesh), Player.
5. **Van Transit Ritual** — depends on: Scene Flow, Networking, Mission (return/partial anchor).
6. **Hazard / Escalation** — depends on: Mission (phase clock), Level Gen (area blocking), Player (slow/oxygen).
7. **Carry / Objective Transport** — depends on: Player, Interaction, Mission (objective).

### Presentation Layer (depends on features)
1. **HUD / Office Computer UI** — depends on: Office Economy, Mission, Equipment, Player.
2. **Settlement UI** — depends on: Mission, Office Economy.
3. **Main Menu / Settings** — depends on: Networking, Scene Flow.

### Polish Layer (depends on everything)
1. **Audio / Proximity Voice** — depends on: Networking.
2. **Environment / Style Director** — depends on: Level Gen, Scene Flow.

---

## Recommended Design Order

> No per-system GDDs exist yet. Order weights **active work** (Level Gen — the
> abandoned-tower map being built now) and the index's flagged **sync risks**
> (mission/economy) ahead of pure dependency order, since the foundation is already
> implemented in code. Author GDDs top-down; same-layer independent systems can be parallel.

| Order | System | Priority | Layer | Agent(s) | Est. Effort |
|-------|--------|----------|-------|----------|-------------|
| 1 | Level / Map Generation & Topology | MVP | Feature | zeno + laplace | L |
| 2 | Mission State Machine | MVP | Core | zeno + laplace | M |
| 3 | Office Economy & Progression | MVP | Core | zeno + hilbert | M |
| 4 | Networking | MVP | Foundation | laplace | M |
| 5 | Loot / Content Fill & Item Economy | Vertical Slice | Feature | zeno | M |
| 6 | Enemy / Monster AI | MVP | Feature | zeno + laplace | M |
| 7 | Hazard / Escalation | Vertical Slice | Feature | zeno | M |
| 8 | Equipment & Consumables | MVP | Feature | zeno + hilbert | S |
| 9 | Van Transit Ritual | MVP | Feature | zeno + banach | S |
| 10 | Player | MVP | Core | laplace | M |
| 11 | HUD / Office Computer UI | MVP | Presentation | hilbert | M |
| 12 | Settlement UI | MVP | Presentation | hilbert | S |
| 13 | Carry / Objective Transport | Alpha | Feature | zeno + laplace | S |
| 14 | Audio / Proximity Voice | Vertical Slice | Polish | banach | S |
| 15 | Environment / Style Director | Vertical Slice | Polish | banach | M |
| 16 | Interaction Framework | MVP | Foundation | laplace | S |
| 17 | Scene Flow / Game State | MVP | Foundation | laplace | S |
| 18 | Save / Persistence | MVP | Core | laplace | S |
| 19 | Main Menu / Settings | MVP | Presentation | hilbert | S |

> Effort: S = 1 session, M = 2–3 sessions, L = 4+ sessions.

---

## Circular Dependencies

- **None blocking.** Watch one soft cycle: **HUD ↔ Mission/Economy** (HUD reads
  state and issues input intents). Resolved by the existing host-authoritative
  contract — UI sends intents, server mutates state, state syncs back; no direct
  two-way mutation. No interface refactor needed.

---

## High-Risk Systems

| System | Risk Type | Risk Description | Mitigation |
|--------|-----------|-----------------|------------|
| Mission State Machine | Technical | completion / partial-return / failure transitions must sync to all clients; silent desync hard to catch in solo play | EditMode logic tests on the state machine; two-client smoke per `docs/mvp-core-loop.md` |
| Office Economy & Settlement | Technical | reward/penalty math (`MvpPendingReward`, `SettlementUIController`, `CompanyData`); idempotency of reward claim | Unit tests on reward formulas + reward-idempotency smoke |
| Networking | Technical | spawn/ownership, connection approval, state sync are the bedrock; failures cascade | Connection/ownership tests; keep host-authoritative invariant in a control doc |
| Level / Map Generation & Topology | Design + Technical | connectivity invariants (every room reachable with all toggles closed); NavMesh must bake on generated geometry | Headless topology test (already unit-testable); bake-validation pass after generation |
| Project-wide test coverage | Scope | only ~1 test file exists for a 4-player host-auth game | Prioritize logic tests for the three sync-risk systems above before adding features |

---

## Progress Tracker

| Metric | Count |
|--------|-------|
| Total systems identified | 19 |
| Design docs started | 1 |
| Design docs reviewed | 0 |
| Design docs approved | 0 |
| MVP systems designed | 1 / 13 |
| Vertical Slice systems designed | 0 / 4 |
| Systems implemented in code (any maturity) | 14 / 19 |

---

## Next Steps

- [ ] Author the first GDD: `/design-system level-map-generation` (active work — the abandoned-tower map)
- [ ] Then Mission State Machine and Office Economy (highest sync risk)
- [ ] Run `/design-review design/gdd/[system].md` on each completed GDD
- [ ] Add EditMode logic tests for the three high-risk sync systems regardless of GDD order
- [ ] Run `/gate-check pre-production` when MVP-tier systems are designed
