# Handoff — Verify the v3 Tower Whitebox (run on the Unity machine)

> **TL;DR (中文)**：这台开发机没有 Unity,下面的代码都是**盲写、未编译验证**的。请在有 Unity 的机子上
> 按本文步骤跑一遍:① 编译过没过 → ② 跑 EditMode 测试(应全绿)→ ③ 跑菜单生成 v3 白模 → ④ 烘 NavMesh →
> ⑤ 目视/试走 → ⑥ 把**报错、重叠警告、截图、测试结果**发回。坐标第一版可能要在编辑器里挪,但
> **连通性是图驱动的,从第一版就应是对的**。

This session redesigned the first level (Earth Coast abandoned tower) toward Lethal-Company-grade
density + disorientation, and built the code for it. Nothing here was compiled or run — the dev
machine has no Unity or C# toolchain. This doc is the step-by-step to verify on the Unity machine.

## 0. Background (what changed and why)

- New design docs (read these first if anything is unclear):
  - `design/levels/lethal-company-design-study.md` — why LC works / why players get lost; the 8 principles.
  - `design/levels/abandoned-tower-redesign-v3.md` — **the canonical v3 map** (irregular footprint,
    S/M/L sizes, ~15 rooms/floor, 1 van + 1 fire exit + 3 internal descents, simple exterior).
  - `design/levels/abandoned-tower-v3-connectivity.md` — the connectivity/corridor spec + invariants.
  - (`abandoned-tower-redesign-v2.md` is superseded; `abandoned-tower-floorplan.md` 36×20 is superseded.)
- New ADRs: `docs/architecture/ADR-0001-host-authoritative-networking.md`,
  `ADR-0002-relay-transport-connection-approval.md` (+ `docs/registry/architecture.yaml`).
- **Headline engineering change:** topology is now **seed-randomized with guaranteed connectivity**,
  defined in code (single source of truth), with the project's **first automated tests**.

## 1. Prerequisites

- Unity **6 (6000.4.7f1)** (per `docs/engine-reference/unity/VERSION.md`).
- Packages: **Netcode for GameObjects** and **Test Framework** (com.unity.test-framework) — both
  should already be in the project; if Test Runner is empty, confirm the package is installed.
- Pull the branch this was pushed on (see the push note the dev gave you), e.g.:
  ```
  git fetch origin
  git checkout feat/tower-v3-topology-whitebox
  ```

## 2. Open the project & let Unity import

1. Open the project in Unity 6. Unity will **generate `.meta` files** for the new scripts/asmdefs
   (they were created outside Unity, so new `.meta` files will appear — that is expected; commit them
   afterward, see step 8).
2. Wait for compilation. **Check the Console.**

### Expected new files (so you know what should be there)
```
Assets/_Project/Scripts/Level/Topology/TopoGraph.cs
Assets/_Project/Scripts/Level/Topology/TowerTopology.cs
Assets/_Project/Scripts/Level/Topology/TowerTopologyV3.cs
Assets/_Project/Scripts/Level/Topology/BlackCommission.Level.Topology.asmdef
Assets/_Project/Scripts/Level/Connector.cs
Assets/_Project/Tests/EditMode/Level/TowerTopologyTests.cs
Assets/_Project/Tests/EditMode/Level/BlackCommission.Level.Topology.Tests.asmdef
Assets/_Project/Editor/TowerV3WhiteboxBuilder.cs
```
Modified: `RoomSlot.cs` (RoomSizeClass dropped to S/M/L), `TowerLayoutGenerator.cs` (applies topology).

### If compilation FAILS
- **Test asmdef reference errors** (most likely): open
  `Assets/_Project/Tests/EditMode/Level/BlackCommission.Level.Topology.Tests.asmdef` in the Inspector
  and re-add the references via the UI: **BlackCommission.Level.Topology**, **UnityEngine.TestRunner**,
  **UnityEditor.TestRunner**, and the **nunit.framework.dll** precompiled reference. (The file uses
  name-based references which usually resolve, but the Inspector will rewrite them as GUIDs.)
- Any other error: **copy the full Console error text** and send it back — do not try to "fix and
  guess." These files were written blind; a typo is possible.

## 3. Run the automated tests (connectivity guarantee)

1. **Window → General → Test Runner → EditMode → Run All.**
2. Expect **all green** (10 tests in `TowerTopologyTests`). They prove: backbone-only is connected,
   1000 seeds all valid with no island rooms, ≥2 descents, determinism (host/client agree), and the
   validator catches broken graphs.
3. If any **fail**, screenshot the Test Runner + the failure message and send back. (A failure here is
   a real logic bug to fix, not a tuning issue.)

## 4. Build the v3 whitebox

1. **Tools → Black Commission → MVP → Tower → Rebuild v3 Whitebox.**
2. This opens `Assets/Scene/AbandonedBuilding_Blockout.unity`, **deletes the old whitebox root(s)**
   (`AB_FloorPlan_Blockout`), and builds the v3 layout under a new `Tower_v3_Whitebox` root.
3. **Read the Console:**
   - Success line: `[TowerV3] Rebuilt v3 whitebox from the connectivity graph (...)`.
   - **Overlap warnings**: `[TowerV3] Room rects overlap on floor X: 'A' and 'B'`. These are the
     coordinates to nudge. **List every overlap warning and send it back** — that is the main thing
     to fix in v2 of the coords. (Connectivity is unaffected by overlaps.)
   - Any graph-node-without-coordinate warnings.

## 5. Bake NavMesh

1. Select the `Tower_v3_Whitebox` root (and exterior) → mark **Navigation Static**.
2. **Window → AI → Navigation → Bake.** (Unity 6 may use the NavMeshSurface workflow — if so, add a
   NavMeshSurface to the root and Bake.)
3. Verify the corridors connect into one navmesh; note any rooms that did not connect.

## 6. Verify visually

- **Quick walk (no netcode):** the geometry exists as soon as you build. Use the existing
  **Tools → … → Tower → Setup Blockout Walkthrough** (`BlockoutPreviewTool`) + `PreviewWalker` to walk
  the raw whitebox. Connectors default to **open** (geometry on, blocker off) when not hosting.
- **See topology toggling (needs hosting):** `TowerLayoutGenerator` is a `NetworkBehaviour`; it runs
  `ApplyTopology(seed)` on `OnNetworkSpawn`, i.e. when you **Start Host**. Different seeds open/close
  different `Connector`s. To force a repeatable layout for testing, set `fixedSeedForTesting` on the
  `TowerLayoutGenerator` component to a non-zero int.
- **Screenshot** the top-down (Scene view, top ortho) of both floors and send back so we can fix coords.

## 7. What to send back

1. Did it **compile**? (yes / paste errors)
2. **Test Runner** result (all green / paste failures).
3. **All `[TowerV3]` overlap warnings** from the build.
4. **Top-down screenshots** of Floor 1 and Floor 2 (Scene view, top orthographic).
5. Did **NavMesh** bake into one connected surface? Any disconnected rooms?
6. Anything that looks obviously wrong (corridors going through rooms, rooms off in space, etc.).

With that, the next pass fixes the coordinate table in `TowerV3WhiteboxBuilder.BuildNodeTable()` and
any real bugs.

## 8. After verifying — commit the generated metas/scene

Opening in Unity generates `.meta` files; running the builder modifies the blockout scene. If we want
to keep that state:
```
git add -A
git commit -m "chore: unity-generated metas + v3 whitebox scene build"
git push
```
(Only do this once the build looks acceptable — otherwise just report back first.)

## 9. Known limitations (set expectations)

- **Written blind** — no compile/run happened on the dev machine. A typo or asmdef-reference hiccup is
  possible (see step 2).
- **Coordinates are a first pass** — expect overlap warnings and some ugly corridor routing. This is
  the intended iteration point; connectivity itself is graph-driven and should be correct.
- **No room content yet** — `TowerRoomCatalog` / `RoomDef` assets don't exist, so slots stay empty.
  That's fine for verifying the whitebox + topology. (Backlog item below.)
- **NavMesh across stairs** — discrete step cubes may need NavMeshLinks for agents to traverse floors;
  note if AI can't path up/down.

## 10. Backlog (after the whitebox is verified)

1. Fix coordinate overlaps in `BuildNodeTable()` from the reported warnings.
2. Author a `TowerRoomCatalog` + `RoomDef` assets (S/M/L pools) so slots fill.
3. Wire connector `geometry`/`blocker` for any hand-placed extras; confirm blockers carry working
   `NavMeshObstacle` carving.
4. The two server-authoritative mechanics still unbuilt: **heavy two-hand 沙盘 carry** + **power gate**
   (high-risk untested — add EditMode tests like the topology ones).
5. Time-scaled monster aggression; the infected-inspector monster + nest placement.
6. Re-run `Tools → … → Tower → Setup Playable Tower Mission` to promote the blockout into the playable
   `Tower_EarthCoast_01` scene once the layout is settled.
7. Lighting/atmosphere pass (Municipal Debt Noir: sodium amber, dead-rubber black, the warm "wrong"
   show-flat beacon).
8. Deferred architecture spike: LC/DunGen-style modular shell generation (room tiles with doorway
   sockets, bounds checks, seed-synced placement, solvability re-roll, generated corridor/door
   geometry). Do this only after the current fixed-shell tower is playable and tuned.
```
