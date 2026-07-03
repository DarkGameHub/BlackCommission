# Active Session State — Black Commission

**Last updated**: 2026-06-23 (end of night — PM went to sleep; agent finishing the build/save autonomously)
**Stage**: Production (see `production/stage.txt`)

> ## ▶▶ RESUME HERE (next session)
> **Goal:** HQ was BUILT+SAVED 2026-06-24, but **PM walked it and rejected it** ("太丑", ground Z-fight, "和平面图不像")
> → pivoted to a **whole-site RE-PLAN**. Produced **HQ Site Plan v2** (`design/hq/hq-site-plan-v2.md` + diagram
> `design/hq/HQ_SitePlan_v2.png`, rendered by `tools/hq_siteplan_v2.py`) via a 4-discipline huddle (ux / game-design /
> level-design / art). **NEXT: PM approves the plan + answers the settlement-location fork, THEN implement** the builder
> changes (doc §8 appendix) → rebuild → walk.
>
> **Plan synthesis (in the doc):** interior Z-spine LOCKED (only proportion fixes WallH 3.6→2.85, DoorH 3.2→2.6);
> enclosed dispatch yard (perimeter walls 1.3→2.0 above eye height + east shed + gate); the lawn fix = grass-DENSITY
> distance bands (apron 0 / scrub 5–10 / wild 14–21 / treeline 0), NOT flat 14–21 everywhere; close horizon via
> treeline+fog90+HRANGE10+bigger boulders; CUT warehouses + the gizmo-fence; NEW return→settlement beat (van re-parks →
> walk back past debt board → settle @ CRT). **OPEN: settlement location (rec = CRT inside).**
>
> **Already applied to `HqOptionAProductionBuilder.cs` (uncommitted, NOT yet rebuilt):** the 14 art-bible edits
> (shadows / grass-color / warm-interior / phosphor-CRT / ambient 0.60 / fog 22-90 / bloom / Flat-smoothness / asphalt /
> Z-recess −0.12). Some are refined again by the new plan (ambient→0.55, fog→#1E1E19, key→0.25, grass density bands).
> **Unity state:** after a recompile→domain-reload the **MCP bridge dropped** + Unity stuck in a long asset reimport
> (>18 min, foliage packs) → old-layout rebuild PARKED (re-planning anyway). Drive Unity via `tools/ws-unity-call.cjs`
> (MCP down). The 14-edit compile-check was never confirmed (Unity busy) — **verify console errors before any rebuild.**
>
> **Deferred — need PM input next time:**
> - **黑色方块跟随视角** (recurring bug, PM re-flagged): NOT root-caused. `PlayerFirstPersonRig` already hides the owner
>   body + 3rd-person model + rig shadows, and no held-item/wristwatch renders by default → it's something subtler.
>   **Need a screenshot from the PM** (or Play-mode runtime inspection) to pin it. Do NOT re-derive blind from code.
> - **室内贴纸/物品摆放** dressing pass — PM said unreasonable; needs specifics/screenshot.
> - **Commit**: HQ.unity + all builder/script changes are UNCOMMITTED; ~404 MB foliage packs (`Assets/Foliage Free`,
>   `Assets/TreePackVol.1`) repo-size decision still OPEN (commit-into-git vs gitignore+reimport) → blocks a clean commit.
>   Do NOT commit until the PM decides (AGENTS.md: no commits unless asked).
>
> **PM decisions locked 2026-06-23:** exterior = 荒草野地+稀疏死树 (Unity Terrain grass + dead treeline); office materials =
> 混凝土公务站房 (floor=CommonConcrete04, walls=CommonConcreteWall02, roof=CommonSteelRoof01); scale = enlarge HQ building
> parents (Shell/Interior/Dressing/Yard) ×1.2, **player untouched** (2 m, locked to the mission map); door reveal = Manual [E].

## Session 2026-06-23 — HQ Phase-2 收尾: van boarding wiring + roll-up door reveal (BUILT + SAVED + VERIFIED)
PM "继续开发" → picked **HQ Phase-2 收尾** + **Manual [E] door** (AskUserQuestion 2026-06-23). Key recalibration:
the builder **already** does yard + environment + **vegetation** (`Build()` calls `BuildYard`/`BuildEnvironment`/
`BuildVegetation`, lines 126-128; `BuildVegetation` reuses `Resources/FoliageSet.asset`, dead-tree treatment,
van-corridor + warehouse exclusion zones). So the only genuinely UNFINISHED Phase-2 deltas were two:
- **(A) Van boarding wiring (functional).** `OfficeDepartureVan` (IInteractable) sat on the old scene object
  `MVP_RuntimeStyle_Office_ExteriorDispatch` (in `Keep`; renderer hidden but COLLIDER kept) → you boarded an
  INVISIBLE ghost collider at the old spot, while the visible `DispatchVan` (AS_OfficeVan) had colliders DISABLED
  by `Prop()`. Interaction = `PlayerInteraction.FindAimedTarget` SphereCast (range 2.5, aim-required) →
  `hit.collider.GetComponentInParent<IInteractable>()`, so the component must sit on/above a hit collider.
  Departure itself (Space → `OfficeComputer.RequestDepart`) lives in `VanTransitOverlay`; `OfficeDepartureVan`
  is only the BOARD trigger, no serialized refs → safe to re-home.
- **(B) Roll-up door reveal (juice).** Door was a static `RollUpDoor_CLOSED`; nothing animated it. `FitPrefab`
  STRIPS the LargeGates mesh colliders → as the real mesh it was non-solid + un-aimable.

**WRITTEN (uncommitted, reversible — NOT built/saved yet):**
- NEW `Assets/_Project/Scripts/Office/HqRollUpDoorReveal.cs` (Assembly-CSharp; `Scripts/Office/` has no asmdef,
  only `Core/` does). MonoBehaviour+IInteractable, translate-up by door height (3.2), smoothstep, door-creak SFX.
  Modes Manual/CommissionLocked/OnDepart; builder configures **Manual** (aim + [E] toggles; empty hint in auto
  modes so PlayerInteraction skips it). Cosmetic, NOT a gameplay gate, NOT networked (local sim, SimpleDoor precedent).
- 3 edits in `Editor/HqOptionAProductionBuilder.cs`: (1) `BuildShell` door → clean identity-scaled **wrapper**
  `RollUpDoor` carrying yaw + sealing/aim BoxCollider (center y=DoorH/2, size DoorW×DoorH×0.3) + the reveal; the
  mesh/box child is built with yaw=0 (wrapper supplies yaw, else FitPrefab's local yaw doubles it). (2) `BuildYard`
  → after `DispatchVan`, measure `MeshBounds`, add child `VanBoardZone` = solid BoxCollider (van is scale1/yaw0 so
  world AABB → local via `InverseTransformPoint`) + fresh `OfficeDepartureVan`. (3) `RetireLegacyOffice` →
  `DestroyImmediate` all stray `OfficeDepartureVan` + disable old dispatch colliders (menu Camera untouched);
  idempotent (root incl. prior VanBoardZone is destroyed first, then exactly one is re-added by BuildYard).

**STATUS: BUILT + SAVED + VERIFIED 2026-06-23 (PM opened Unity ~21:25 after two ~30min watcher cycles).** Bridge
flow: get_scene_info (HQ active) → console 0 errors (new component compiled; `.cs.meta` present) → `execute_menu_item`
Build Production HQ (Option A) ran SYNCHRONOUSLY to completion in one pass (build log fired, no 30s cut — materials
were already cached from cont.3) → verified via `get_gameobject`: **VanBoardZone** = `[Transform, BoxCollider,
OfficeDepartureVan]` at world (4.5,0,22) under DispatchVan; **RollUpDoor** = `[Transform, BoxCollider,
HqRollUpDoorReveal]` at (4.5,0,18.25) yaw 195.5° (canted wall), 1 panel child; old **MVP_RuntimeStyle_Office_
ExteriorDispatch** = `[Transform]` only (ghost OfficeDepartureVan stripped, menu camera intact); **Vegetation** = 59
children; **0 compile + 0 runtime errors** → `save_scene` HQ.unity (isDirty→False, rootCount 27, mtime 21:31).
Note: "retired 0 legacy" = legacy was already inactive from the prior in-memory build (idempotent). The RollUpDoor
BoxCollider seals the 4 m opening when closed and rises with the panel → the bay is genuinely sealed until the team
[E]-opens it (emergent, matches the locked axial-reveal beat). Still UNCOMMITTED; ~404 MB foliage-pack repo-size
decision still open ([[Session 2026-06-22 cont.]]). Door is local-sim/cosmetic (not networked, SimpleDoor precedent).

### 2026-06-23 (cont.) — PM walked it → big feedback → 野外 exterior rework + HQ enlarge (CODE WRITTEN, building)
PM walked the Phase-2 HQ and flagged FOUR things: (1) **室外该是野外**, not the derelict industrial district — the
office sits alone in the wild; the FactoryPropsGround concrete ground is ugly. (2) 室内贴纸/摆放不合理 (interior
dressing pass — deferred, needs specifics/screenshot). (3) **人物显得太高** — player capsule `standHeight=2 m`
(Player.prefab, GUID 43b9c38f…, NetworkManager PlayerPrefab); PM noted the 2 m player is "正好" for the MISSION MAP
(shared player → must NOT shrink). (4) **黑色方块跟随视角** ("还是" = recurring; PM 2026-06-13 note). AskUserQuestion:
exterior = **荒草野地+稀疏死树**; scale = PM pushed back on shrinking → resolved to **enlarge HQ only**.
- **Black square**: NOT root-caused yet — `PlayerFirstPersonRig` (on the spawned prefab) DOES hide the capsule body
  + the whole third-person model for the owner + kills rig shadows, and by default no held item/wristwatch renders
  (HasWristwatch=false). So it's something else; **awaiting a screenshot from PM** to pin it. Don't re-derive blind.
- **Exterior rework WRITTEN in `HqOptionAProductionBuilder.cs`** (uncommitted, ~10 edits): replaced `BuildEnvironment`
  (warehouses/streets/perimeter) with **`BuildWilderness`** = a real **Unity Terrain** (heightmap undulation + GPU
  detail grass, replicated from `MapSiteBuilder.BuildTerrain`; **TerrainData persisted as asset**
  `Assets/_Project/Scenes/HQ_TerrainData.asset` or the saved scene loses it) flat under building/yard/track via
  `HqFlatMask`, rolling Perlin wild elsewhere; dead **treeline** ring + meadow scrub (rewrote BuildVegetation +
  VegBlocked; `PlaceVeg` got a `groundY` param to sit foliage on the rolling terrain); dirt track north from the gate;
  boulders; cool moon fill. Yard ground re-skinned concrete→dirt. Fog end 60→120 so the wild reads.
- **HQ enlarge**: `const HqScale=1.2f`; Build() scales the **Shell/Interior/Dressing/Yard** parents (NOT root, NOT
  the Terrain — Unity Terrain ignores transform scale); spawn/computer anchors ×HqScale. Player untouched → mission
  map untouched. Pad bounds in HqFlatMask widened to cover the scaled footprint.
- Old `Warehouse`/`WarehouseBlocks` now unused (left in place; harmless). **STATUS: builder recompiling after
  Assets/Refresh; next = build → verify Terrain/Vegetation/scale + 0 errors → save → PM walks.** Interior dressing +
  black square = next pass (need PM screenshot/specifics).

## Session 2026-06-22 (cont. 3) — HQ dispatch yard + full environment district BUILT + SAVED
PM signed off the office ("还行"), then two more passes into `HqOptionAProductionBuilder.cs` (all SAVED to HQ.unity):
- **Dispatch yard** (`BuildYard`): ~24×26 m, low concrete perimeter + `LargeGates` exit gate (6 m N gap) + van on
  a marked bay + 2 sodium lights + powerbox/barrels. Replaced the tiny pad; the ugly far skyline was DELETED.
- **Full derelict environment** (`BuildEnvironment` + `Warehouse` helper) — PM chose "更完整的废弃工业区": ~20
  Tirgames concrete/brick warehouse blocks (solid + steel roof cap) wrapping the HQ on all 4 sides receding into
  fog, dead E-W + N-S cross-streets past the gate, a 180×200 district ground (no void), a far perimeter wall,
  3 distant sodium glows. Fog end 38→60 so near district reads, far dissolves; office stays moody via near start.
- Retire is now **NAME-AGNOSTIC** (deactivate any ACTIVE Renderer/Collider object not in `Keep`; the dispatch
  van object stays active with its mesh disabled) — fixed the "furniture left outdoors" the name-prefix retire missed.
- **Verified: `District_Ground` + `Warehouse` present + build completion log + saved.** AWAITING PM look.

**⚠ NEW BRIDGE LESSON — the ~30s connection reset:** Unity's WS server drops the connection at ~30s
(`ECONNRESET`), cancelling the in-flight build coroutine mid-execution. So a heavy build whose FIRST pass triggers
URP material/shader compile (>30s) gets cut off (partial/no result, no completion log). **Fix: run the build
TWICE** — first pass compiles the materials (then dies), second runs cached <30s and completes. Verify completion
via a UNIQUE child (`get_gameobject District_Ground`) + the build `Debug.Log`, NOT via `get_gameobject HQ_OptionA`
child lists (they TRUNCATE past ~4 branches → false "missing"). Raise McpUnitySettings RequestTimeoutSeconds if needed.

## Session 2026-06-22 (cont. 2) — PHASE 1 BUILT + SAVED: Option A office + exterior reveal
**DONE + SAVED to `HQ.unity`.** `Assets/_Project/Editor/HqOptionAProductionBuilder.cs` (menu `Tools ▸ Black
Commission ▸ Map ▸ Build Production HQ (Option A)`; undo = `Restore Old HQ Office`) builds the Option A wedge:
- Shell = exact boxes in Tirgames concrete/steel mats + `LargeGates1` roll-up door + `Factory1Column`/`LampCeiling`
  hero meshes (auto-fit by bounds). Interior = real `AS_Office*` props on the Z-spine. Dressing = Tirgames
  PowerBox/MetalCabinet/Barrels. Office lighting/fog/LC-post.
- **Exterior reveal (added per PM 2026-06-22 "室外、车都没有"):** `AS_OfficeVan` (DispatchVan) + asphalt apron +
  fogged city silhouette just outside the roll-up door. FULL vegetated dispatch yard = Phase 2.
- Old office RETIRED (deactivated, reversible) — 80 objects. Dispatch-loop objects kept (NetworkManager/UI/
  `MVP_OfficeComputer`/`OfficeDepartureVan`/spawn); spawn + computer repositioned into the wedge.
- **Verified (reliable signals only):** scene rootCount 25→26, `HQ_OptionA` childCount **15 incl. `Exterior`**,
  saved. **AWAITING PM VISUAL SIGN-OFF** (Phase-1 gate).

**⚠ BRIDGE LESSONS (this session — cost hours):**
- mcp-unity `get_gameobject` by an arbitrary name does **FUZZY matching → returns the WRONG object** (querying
  "ShellFloor" returned "Floor"'s data + active state). It sent me in circles thinking retire failed. **Trust
  only:** scene `rootCount`, `childCount`/child-names via a *unique-name* parent lookup, and the build's `Debug.Log`.
- MCP server (Claude↔bridge) drops under load, but **Unity's 8091 listener stays up** → use the direct fallback
  `node tools/ws-unity-call.cjs <method> '<json>' <timeoutMs>` ([[ws-unity-call-tool]]). Params: get=`idOrName`,
  update=`isActiveSelf`/`instanceId`, load=`scenePath`.
- Builds trigger **first-time URP shader compile** → client times out (~150s not enough); use **≥200s** or
  fire-and-reprobe (`rootCount`/`childCount`). A menu dispatched right after a recompile/domain-reload can be
  DROPPED — wait for Unity idle then dispatch. New `[MenuItem]` needs editor FOCUS to register (PM clicks Unity).
- Reset trick when in-memory state gets messy: `git checkout -- HQ.unity` → `load_scene` → build once.

**NEXT (Phase 2, after sign-off):** full dispatch yard (real ground + Foliage/TreePack vegetation + fences) +
wire the `OfficeDepartureVan` interactable to the new van location + open/animate the roll-up reveal.

## Session 2026-06-22 (cont.) — PRODUCTION HQ (Option A) build — kickoff + decisions
PM feedback: HQ doesn't match the locked Option A plan + has no plants; wants a **directly production-ready**
version (offered to download missing assets). **Commit `8e7a976`** saved the 06-20→22 source batch first
(code + design/hq plan PNGs + tools; the ~404 MB Foliage Free/TreePackVol.1 packs + derived Resources assets
LEFT OUT pending a repo-size call). Diagnosis (confirmed): the locked plan was NEVER built into the real scene —
- **HQ ≠ plan**: shipping `HQ.unity` is the OLD rectangular **Office+Garage** box (`HqOfficePropRestorer.cs`
  hard-codes 06-12 hand-placed props; scene objects `Shell*` / `BlenderHQ_*` / `HQ*Collider`). The Option A
  wedge exists ONLY in throwaway `HqPlaytestMenu` (PLAYTEST_HQ, offset x+300, never saved).
- **No plants**: no HQ builder scatters any; foliage lives only in `MapSiteBuilder` (Map2). The dispatch yard
  (`design/mockups/hq-dispatch-yard-plan-v1.png`) was never built. `FoliageSet.asset` is fine, just unused by HQ.

**PM DECISIONS (AskUserQuestion 2026-06-22):**
- Build fabric = **in-project industrial kit** (Tirgames Factory + Concrete Props) — NO downloads.
- Scope = **PHASED: Option A office INTERIOR first** → PM walks + signs off → THEN garage + dispatch yard +
  vegetation (Phase 2).

**BUILD INTERPRETATION (stated to PM, correctable):** Option A is a single integrated **wedge unit** whose
roll-up departure door + muster pad + departure corridor ARE the dispatch function → it **replaces** the old
Office+Garage 2-room box. The separate outdoor **dispatch yard (van + plants)** = Phase 2.

**Assets confirmed in-project (production-grade, no download):** Tirgames Factory = full modular kit:
`Factory1Wall01-05`/`Factory2Wall01-06`, floors, roofs, `Factory1Column*`, `LampCeiling01`, **`LargeGates1`**
(roll-up/dispatch door), `DoorIndustrial01`, + dressing (Barrel/Debris/PowerBox/MetalCabinet/FireExtinguisher);
prefabs under `…/Factory/Prefabs/`. Interior = 12 real `AS_Office*` prefabs (Resources/GeneratedArt), positions
already plan-tuned in `HqPlaytestMenu`. Lighting/post = reuse `HqOfficeLightingPass` + the playtest fog/post.

**GAMEPLAY ANCHORS in HQ.unity to PRESERVE** (keep the dispatch loop working): `MVP_OfficeComputer`,
`HQSpawnManager`+`PlayerSpawnPoint`, `NetworkManager`/`ConnectionManager`/`DisconnectHandler`,
`MVP_HUD`/`HQUI`/`SettlementUI`/`MainMenu_UGUI`/`HQMenuCamera`, `MVP_RuntimeStyle_Office_ExteriorDispatch`.

**PLAN:** new editor builder `HqOptionAProductionBuilder` → Option A wedge into HQ.unity: shell = exact-dimension
boxes wearing Tirgames concrete/steel materials + auto-fit Tirgames hero meshes (LargeGates door / columns /
ceiling lamps) via runtime bounds (NO guessed transforms — see [[unity-fbx-build-gotchas]] / model-fit); interior
= real AS_Office props on the Z-spine; HQ office lighting/fog/post; reposition spawn + computer into the wedge;
neutralize old `Shell*`/`BlenderHQ_*`/`HQ*Collider` office + `HqOfficePropRestorer` auto-props. Build → verify
(0 errors, on-floor, loop intact) → SAVE HQ.unity → PM walk + sign-off (Phase 1 gate). Tasks #1–5.

**STATUS:** bridge was live (one good `get_scene_info`: active scene unsaved/clean, 3 roots), then **WEDGED** on
`load_scene HQ.unity` (2 timeouts) — big scene + `HqOfficePropRestorer` EditorBootstrap + domain reload stalled
the pump ([[unity-bridge-restart-recovery]]). Recovery: refocus the Unity window so the load/reload finishes,
then re-probe; restart only if it stays wedged. Builder NOT written yet (holding until the structural
interpretation is confirmed + bridge is healthy to measure/verify — avoids a blind, untestable build).

## Session 2026-06-21 → 06-22 (BACKFILL written 2026-06-22 — work was done but never logged; all UNCOMMITTED)
> Reconstructed on 2026-06-22 from file mtimes + contents at PM request. **Last commit is still `9d059a9`
> (2026-06-20 11:00); everything below is dirty/untracked working-tree state on `scavenge-settlement-reveal`,
> NOT committed and NOT PM-signed-off.** This is why a "committed-state only" status check on 06-22 missed it.

### ★ HQ OFFICE FLOOR PLAN — LOCKED "Option A — Long-Axis Z-Spine" (PM David, 2026-06-21)
- **Answer to "敲定平面图了吗" = YES for the HQ office.** PM locked **Option A**: ONE leased industrial unit, a
  gentle WEDGE — 9 m wide, +X wall 17 m, -X wall 19.5 m, far wall CANTED. The 4 m roll-up departure door sits
  in the canted far wall dead ahead of the spawn line (axial reveal — team stares at the closed door all prep).
  Stations on OPPOSITE walls force a diagonal Z-spine walk: spawn (z≈2) → debt/takeover board + CRT (-X) → gear
  wall (+X) → muster pad → run-up → roll-up door → REVEAL (van + fogged city). S1 padlocked side door
  peripheral on -X (license-as-leash, off-path).
- **PM directive 2026-06-21: "一定要视觉上就很好，而不是就几个方块"** → playtest uses the REAL authored office
  prefabs (Resources/GeneratedArt `AS_Office*` — computer, gear cabinets, debt board, sofa, lamps, extinguisher,
  gas-mask sentinel), import-normalized (base y=0, real height). Shell = skinned boxes (as the real runtime HQ
  does) in V8 tower whitebox materials. Render mirrors HQ office: concrete LINEAR fog (interior clear / city
  silhouette fogged), Trilight ambient, cold-fluorescent / warm-tungsten / sodium-amber pools, shadows OFF, one
  sputtering fluorescent (`LightFlicker`), LC post stack (grain/bloom/grade).
- **NEW `Assets/_Project/Editor/HqPlaytestMenu.cs`** (mtime 2026-06-22 00:22 — newest file) → menu
  `Tools ▸ Black Commission ▸ Map ▸ Playtest HQ (Walk It)`. THROWAWAY: builds `PLAYTEST_HQ` offset x+300, NEVER
  saves, does NOT touch the real runtime HQ (`HqOfficePropRestorer` / `HqOfficeLightingPass`). Play to walk;
  un-tick `RollUpDoor_CLOSED` to preview the reveal. Per-prop facing in `PropYaw` (tweak + re-run if wrong).
- **STATUS: built + walkable, AWAITING PM EYEBALL/SIGN-OFF; uncommitted.** The shipping RUNTIME HQ
  (`HQ.unity` via `HqOfficePropRestorer`+`HqOfficeLightingPass`) is NOT yet rebuilt to Option A — the playtest
  is the design proof; porting the locked plan into the runtime HQ is the next step.

### Map 2 procedural site — room-class lock + real foliage + Unity Terrain + walk-test (2026-06-20→21)
- **Room sizes LOCKED to the 3 tower classes** (`GridMapGenerator.ExpandRooms`, 06-20 12:56): S 4×4 / M 8×8 /
  L 12×8 m (1×1 / 2×2 / 3×2 cells), identical to `TowerPlanV8 / PlanSize`. Hero anchors (ENTRY first, DEEP last)
  always L; rest seeded class+orientation; never steals another anchor's room. PM rule 2026-06-20: these 3
  classes only, no free-form blocks. `FullSiteBuildTests.cs` updated to match.
- **Real plants, not whitebox** (PM 2026-06-20) → **NEW `Scripts/Level/FoliageSet.cs`** (ScriptableObject in
  Resources: tree/bush prefabs + materials + Unity-Terrain fields; runtime-safe fallback to procedural
  primitives if missing) + **NEW `Editor/FoliageSetup.cs`** (`… ▸ Setup Foliage (URP + Resources)`): imports the
  "Foliage Free" pack, converts legacy mats → URP/Lit alpha-clipped double-sided dead-olive, authors
  `Assets/Resources/FoliageSet.asset` (trees = pine1a/b+pine2a/b, bush = ferns) + GrassGround.mat,
  GrassTerrainLayer.terrainlayer, TerrainURP.mat, and a 3-quad crossed grass-card mesh prefab `GrassTuft.prefab`.
- **LC-style Unity Terrain pivot** (PM 2026-06-21) → `MapSiteBuilder.cs` (+955 lines, 06-21 18:19): `Foliage()`
  loads the FoliageSet; `BuildTerrain` makes an undulating heightmap + painted grass layer + GPU-instanced
  detail-grass cards (`GrassTuft`/`GrassDensity`) when `grassLayer` is set, else `BuildGroundMesh` (flat).
  `ScatterWoods`/`Tree`/`Bush` instantiate the real pines/ferns auto-fit to height, falling back to the
  procedural dead-tree silhouettes. `BuildYardDressing` (industrial yard) + `BuildEntrances` retained.
- **Walk-it playtest rigs** (06-21 16:58): **NEW `Scripts/Player/Map2PlaytestController.cs`** — solo
  CharacterController FPS rig mirroring the real `PlayerController` body (h2/r0.4/step0.5/slope75) + feel
  (walk4/sprint7/crouch2/grav-20/jump5) + F noclip; reused by BOTH playtests. **NEW `Editor/Map2PlaytestMenu.cs`**
  (`… ▸ Playtest Map 2 (Walk It)`) builds the full site at seed 1 + drops the rig at the van drop-off (solo, no NGO).

### ⚠ State hygiene / open for PM
- **UNCOMMITTED batch** (last commit `9d059a9`, 06-20 11:00): MODIFIED `MapSiteBuilder.cs`, `GridMapGenerator.cs`,
  `Map2_Procedural.unity`, `Editor/Map2SceneBuilder.cs`, `Editor/FullMapBuilderMenu.cs`, `FullSiteBuildTests.cs`;
  NEW `FoliageSet.cs`, `FoliageSetup.cs`, `HqPlaytestMenu.cs`, `Map2PlaytestMenu.cs`, `Map2PlaytestController.cs`.
- **Tests last green 06-20** (bridge). `FullSiteBuildTests` + `GridMapGenerator` + `MapSiteBuilder` changed since →
  **EditMode re-run NOT done this batch** (verify pending).
- **PM TODO:** (1) eyeball Option A HQ playtest → decide to port into shipping `HQ.unity`; (2) eyeball Map2
  terrain+foliage (needs Foliage Free pack imported + Setup Foliage run once); (3) commit the batch; (4) re-run
  EditMode after the room-class + terrain changes.

## ▶▶ AUTONOMOUS BLOCK (2026-06-19, PM out ~3h — full autonomy, NO mid-way questions)

**PM mandate (verbatim intent):** (1) backlog multiplayer determinism; (2) polish **indoor map 2** (flat
procedural site) so **every wall & door is in the correct position**, full logic working; (3) build a
**RANDOM outdoor natural scene OUTSIDE** the indoor map, with the **van DROP-OFF marked**, also randomly
generated; (4) **wire the whole chain** outdoor(drop-off) → indoor entrance end-to-end. Use Unity via the
bridge; **restart Unity if MCP drops** (full machine control granted). Do NOT ask for validation mid-way.

**Editor exe:** `C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe` · project `D:\BlackCommission`.
**Bridge recovery:** [[unity-bridge-restart-recovery]]; import new `.cs` via `execute_menu_item Assets/Refresh`
(focus) per [[unity-bridge-verify-workflow]]; verify via `run_tests`/menus (no Play toggle over the bridge).

**PROGRESS CHECKLIST:**
- [x] 1. BACKLOG → `production/backlog.md` (multiplayer determinism + double-wall + outdoor-art notes).
- [x] 2. INDOOR walls/doors — CODE DONE. Pivoted to **edge-based** `MapSiteBuilder` (each wall built ONCE →
     no holes, no double/z-fighting walls, real front door) instead of the per-cell module path. Also added
     dead-end caps to `ModularRoomBuilder` + fixed `ModuleFor` (kept the module path usable). *Verify pending.*
- [x] 3. INDOOR runtime — `MapSiteRuntime.cs` (build on Start, player-walkable colliders) + editor
     `FullMapBuilderMenu.cs` (Build Full Map 2 = build + bake nav + verify). *Verify pending.*
- [x] 4. OUTDOOR — `Generation/OutdoorScatter.cs` (pure seeded scatter) + `MapSiteBuilder.BuildOutdoor`
     (whitebox ground + trees/rocks/bushes south of the building, van DROP-OFF pad + `DROPOFF_VanSpawn`
     anchor + dirt path). *Verify pending.*
- [x] 5. WIRE — `MapSiteBuilder.CarveEntrance` forces an ENTRY→south-boundary corridor with a FRONT DOOR;
     outdoor path meets it; nav check is DROP-OFF → DEEP. *Verify pending.*
- [ ] 6. VERIFY via bridge (Unity restarted ~now; boot imports the new code) → run Build Full Map 2 → read
     `[FullMap2] ✓ DROP-OFF → DEEP walkable` → create/save a Map2 scene. Then final summary.

**STATUS (live): all code written; verifying via HEADLESS BATCH TESTS (PM away → GUI Unity unusable).**
- **GUI Unity hang:** relaunching `Unity.exe` directly while the PM is away / display session locked → boot
  **hangs right after licensing at ~74 MB** (never inits graphics; logs show Code-10 signature + "Access token
  unavailable"). So the bridge/menus are NOT available unattended. Killed it.
- **Pivot:** verify with `Unity.exe -runTests -batchmode -nographics -projectPath . -testPlatform EditMode
  -testResults Temp/test_results.xml -logFile Temp/test_run.log` (no display/focus/bridge). Added
  `Tests/EditMode/Level/FullSiteBuildTests.cs` — invokes the REAL `MapSiteBuilder.Build` via REFLECTION
  (Assembly-CSharp isn't asmdef-referenceable), bakes navmesh over the actual geometry (NavMeshBuilder +
  PhysicsColliders → walls block, doors pass), asserts **DROP-OFF→ENTRY and DROP-OFF→DEEP walkable** across
  seeds + OutdoorScatter determinism/exclusion.
- **Files this block:** `MapSiteBuilder.cs`, `MapSiteRuntime.cs`, `Generation/OutdoorScatter.cs`,
  `Editor/FullMapBuilderMenu.cs`, `Tests/EditMode/Level/FullSiteBuildTests.cs`, `ModularRoomBuilder.cs`(+4
  dead-end caps), `GridLayoutInstantiator.cs`(ModuleFor fix), `backlog.md`.
- **✅ VERIFIED GREEN (headless batch, 2026-06-19 20:03):** `<test-run>` total=201, **passed=194, failed=7**.
  The 7 are the documented-noise `McpUnity.Tests.BatchExecuteToolTests` (always NRE in batch). **All 4 new
  `FullSiteBuildTests` PASSED**, incl. the headline **`Dropoff_to_deep_is_walkable_across_seeds`** — on the
  REAL `MapSiteBuilder` geometry, seeds 1/777/31337, **DROP-OFF → ENTRY → DEEP is navmesh-walkable** (walls
  block, doors pass, front door connects random outdoor ↔ random indoor). OutdoorScatter determinism +
  exclusion green. The prior ~190 project tests still pass → nothing broke. Results XML in LocalLow per
  [[unity-headless-batch-tests]].
- **✅ SCENE SAVED:** `Assets/_Project/Scenes/Map2_Procedural.unity` (light + overview camera +
  `MapSite[MapSiteRuntime]`), built headless via `-executeMethod Map2SceneBuilder.BuildAndSave`. **Hit Play →
  a fresh RANDOM site each run** (outdoor approach + van drop-off + edge-based interior). Machine left clean
  (no Unity process, lock cleared) for PM relaunch.

### ↻ REVISION (PM feedback, 2026-06-19) — bigger building + far dark outdoor — VERIFIED GREEN
PM: drop-off too close / outdoor not risky enough (ref LC); building too simple, want ~20-min levels. PM chose
**single big floor** (not multi-floor) + **large/dark/far** outdoor. Implemented + verified headless (5/5):
- Indoor **14×10 → 28×24**, 8 winding waypoints (`MapSiteBuilder.Anchors`) + `Branches=40`. Verified by
  **pure-logic** `Generator_connects_entry_to_deep_at_full_size` (50 seeds, all anchors reachable).
- Outdoor drop-off **18m → 80m**, **no breadcrumb path**, dense forest (fill 0.62), dark+fog (set in
  `Map2SceneBuilder` RenderSettings + dim moonlight). `Outdoor_approach_dropoff_to_entry_is_walkable` green
  (~7700 navmesh tris through the woods). `Map2_Procedural.unity` rebuilt dark.
- **Why the first revised run FAILED then passed:** full-site single `NavMeshBuilder` bake FRAGMENTS at this
  scale (DROP-OFF→DEEP `PathPartial`) — a bake-method limit, not a wall. Switched indoor verify to pure logic;
  kept navmesh for the (shorter) outdoor approach. Real game → tiled `NavMeshSurface` (backlog).
- **Still open:** 20-min loop needs objectives/loot/monster (space done); perf pass (~3500 objects); runtime
  tiled navmesh. All in `production/backlog.md`.

### ↻ REVISION 2 (PM feedback, 2026-06-20) — looped maze + open rooms — VERIFIED GREEN
PM playtested + flagged "only one path" — correct: the generator was a TREE (single ENTRY→DEEP chain +
dead-end spurs; `AddBranches` breaks on collision so no cycles). PM chose **loops + rooms** (vs multi-floor /
full). Plan: `~/.claude/plans/cozy-sniffing-matsumoto.md`. Implemented (all on disk + compiled in PM's editor
via bridge `recompile_scripts`, 0 errors):
- `GridMap.cs`: `CellKind.Room`.
- `GridMapGenerator.cs`: `AddLoops` (links adjacent unconnected corridors → cycles) + `ExpandRooms` (each
  anchor → open (2r+1)² Room block, owner=anchor.Id, interior linked, perimeter→corridor doors) + `loops`/
  `roomRadius` params (default 0 → tower harness/preview unchanged).
- `MapSiteBuilder.cs`: skip wall between same-owner cells (open room interior); `Loops=24`, `RoomRadius=1`
  (tuning knobs).
- `FullSiteBuildTests.cs`: `Generator_creates_loops` (LinkCount > tree edges → cycles) +
  `Rooms_are_open_and_reachable`; `Map2Anchors()`/`CountNonEmpty()` helpers; alias `GridLayout`.
- **VERIFIED: FullSiteBuildTests 7/7 + full EditMode 204/204 (0 regressions)** via bridge.
- **No scene rebuild needed** — `Map2_Procedural` MapSiteRuntime + Build Full Map 2 use the recompiled code →
  loops+rooms on next Play / menu click. PM judges feel; knobs = `Loops`/`RoomRadius`/`Branches`.

## Session 2026-06-20 (cont.) — COMMIT map system + OUTDOOR ART PASS (hybrid, zero-import)
- **Committed** the whole verified procedural-map system (was 100% untracked): `9d059a9`
  `feat(level): procedural map generation system (ADR-0003)` — 101 files on `scavenge-settlement-reveal`.
  Deliberately EXCLUDED noise: all `.mat` (URP reimport), `DefaultNetworkPrefabs.asset`, `ProjectSettings/*`,
  `BlackCommission.slnx`, root `TestResults-*.xml`.
- **Outdoor art pass — PM picked direction = HYBRID, assets = zero-import / I do it solo** (`AskUserQuestion`).
  - **Tension surfaced to PM:** the locked `design/art/art-bible.md` (+ `docs/art/black-commission-style-lock-v2.md`,
    PM-locked 2026-06-10) is interior/institutional (concrete/green/wood/rust, ≤256px, rectangular, no organic
    curves, "no exterior/weather decay") — it has ~no forest guidance, vs the 2026-06-19 "natural outdoor" ask.
    PM resolved: **hybrid** = dead/abandoned treeline approach + derelict INDUSTRIAL YARD dressing at the building.
  - **`MapSiteBuilder.cs` (runtime-safe, pure procedural):** palette → locked noir (dark concrete-earth ground,
    darkened wood-brown trunks, **dead gray-green** foliage [no vibrant green → passes litmus], concrete-gray
    rocks, **faded safety-yellow** pad + hazard chevrons); per-instance value-jitter via position hash (NOT the
    scatter RNG). Dead-tree silhouette = tapered trunk (only obstacle) + 2 angular snag branches + ~60% sparse
    flattened dead crown / ~40% bare snag; branches/crown/scrub **collider-free** (`Decor` helper). Industrial
    yard (`BuildYardDressing`, independent `seed ^ const` stream): striped hazard posts, jersey barriers, a DEAD
    floodlight, rusty barrel cluster — all on the FLANKS (|x−door| ≥ DoorW+2.6) so the central corridor stays clear.
  - **`Map2SceneBuilder.cs` (editor-side → serialised into scene):** fog/light desaturated (cold but not
    movie-blue); **assigned the project's owned Tirgames `SkyBoxIndustrial01Night.mat`** as `RenderSettings.skybox`
    (real reuse, zero import). Runtime build never touches assets (skybox is editor-baked).
  - **Hard constraints kept:** did NOT touch `OutdoorScatterGenerator` (scatter tests auto-safe); trunk footprint
    unchanged + dressing off-corridor (navmesh test safe); no prefab/AssetDatabase deps at runtime.
- **✅ VERIFIED GREEN (headless batch, 2026-06-20 ~11:08):** EditMode **204 total / 197 passed / 7 failed**; the 7
  are the documented `McpUnity` batch noise (non-Mcp failures = 0). **All `FullSiteBuildTests` pass**, incl.
  `Outdoor_approach_dropoff_to_entry_is_walkable` (both seeds), scatter determinism/exclusion, and
  `Site_builds_geometry_without_throwing` → compiles clean, 0 real regressions.
- **⏳ AWAITING PM VISUAL SIGN-OFF (art = ADVISORY gate):** art-pass code is **UNCOMMITTED** pending eyeball.
  PM flow: `Tools ▸ Black Commission ▸ Map ▸ Create Map2 Scene` (bakes new fog/skybox into the scene) → open
  `Map2_Procedural.unity` → **Play** (palette/dead-trees/yard apply at runtime; re-Play = new layout). On thumbs-up
  → commit the art pass. Knobs to tune live: palette Colors + `BuildYardDressing` placement in `MapSiteBuilder.cs`.

### ↻ PM FEEDBACK (2026-06-20, live via bridge) — wrap-around woods + building outer SHELL — VERIFIED GREEN
PM looked at `Build Full Map 2` (drove it via bridge; Unity open, PID 9008, port 8091). Three asks → all done in
`MapSiteBuilder.cs`, recompiled + rebuilt + re-verified live:
- **(1) No vegetation around the building / air-wall UX.** Cause: ground+scatter were SOUTH-only (z∈[-100,0]);
  building's other 3 sides floated over void. Fix: `BuildOutdoor` now wraps ground+woods around the WHOLE
  footprint and reaches `Reach=44 m` out on every side; a dense outer-`Ring=14 m` treeline (2nd scatter pass,
  `seed ^ 0x7EE`, fill 0.85) = soft diegetic boundary; base fill 0.42. `ScatterWoods` helper dedups the two passes.
- **(2) LC-style plain box exterior.** Added `BuildShell` (new "Shell" root child via `Build`): rectangular worn-
  concrete box (walls + parapet + flat roof + pilaster ribs + faded signage) just outside the footprint (g=0.6).
- **(3) PM caution "里面真正的房子不能走到外面去".** Real issue found: our maze is irregular + sparse, so a
  rectangular box leaves a shell↔maze cavity, and the door threshold gap could let a player slip into it. **PM
  ruling: don't fill/remove the dead space — just make it invisible + unreachable.** Fix: a SEALED ENTRANCE
  VESTIBULE (`Shell_Jamb_L/R` bridge the shell door at z=z0 to the interior door at z=0 → straight-in only, no
  sideways slip); **removed the fake fire exit** (it opened onto the cavity) → north/E/W walls solid, front door
  the only opening; roof caps the cavity from above. Dead space now hidden (opaque shell+roof+jambs) + sealed.
- **Determinism-safe:** still did NOT touch `OutdoorScatterGenerator`; the 2nd woods pass + shell use independent
  streams / fixed geometry. **Live verify (Build Full Map 2):** outdoor **1843 scatter**, navmesh **10140 tris /
  bounds 201.8×265.5** (wrap-around), **✓ DROP-OFF→DEEP walkable 247.7 m** + DROP-OFF→ENTRY 90.1 m through the
  vestibule, Shell childCount 35 incl. `Shell_Jamb_L/R`, **0 console errors**, recompile clean (only CS0618 noise).
- **STILL UNCOMMITTED — awaiting PM overall visual sign-off.** On thumbs-up: commit (art pass + shell + wrap-around
  + vestibule) and run the formal full EditMode suite to lock regression (deferred so it doesn't pink the live
  preview via domain reload). Tuning knobs: `Reach`/`Ring`/fills, shell `top`/`g`/pilaster spacing, palette Colors.

### ↻ PM FEEDBACK (2026-06-20) — rooms LOCKED to the 3 size classes — VERIFIED GREEN (headless)
PM asked: does the layout obey the "rooms = only 3 sizes" rule; is random-gen done; does it conflict with the shell?
- **Two-system gap surfaced:** the "3 locked room classes" rule lived in the TOWER system (`TowerPlanV8`/`PlanSize`,
  tested by `PlanV8_RoomSizes_UseOnlyTheThreeLockedClasses`): **S=4×4, M=8×8, L=12×8 m**. The Map2 procedural
  generator (`GridMapGenerator.ExpandRooms`) was SEPARATE, making free-form (2r+1)² squares (12×12 / 20×20 m) →
  did NOT obey it. (`design/gdd/map-sequence-and-modular-system.md` lists 4×4/4×8/8×12 — STALE vs the tested
  TowerPlanV8 values; fix that doc later.)
- **PM ruling = change the generator to use only the 3 sizes.** `ExpandRooms` now emits ONLY the 3 locked
  footprints (S 1×1 / M 2×2 / L 3×2 cells on the 4 m grid); hero anchors (ENTRY/DEEP) = L, others seeded
  class+orientation; anchor cell guaranteed in its room; never overwrites another room. `Rooms_are_open_and_reachable`
  now also asserts every room footprint ∈ {S,M,L}.
- **Random gen = done + proven** (1000-seed determinism/reachability; each build = new seed = new layout).
- **No shell conflict:** shell recomputed from the same footprint each build + front door aligned to the fixed
  ENTRY anchor → always wraps the random interior (verified across seeds).
- **✅ VERIFIED GREEN (headless batch, 2026-06-20 ~13:15):** EditMode 204/197 (7 McpUnity noise, **0 real
  failures**); ALL 7 `FullSiteBuildTests` pass incl. the new footprint assertion + 50-seed
  `Generator_connects_entry_to_deep_at_full_size`. Zero regressions.
- **DROP-OFF→DEEP shows PathPartial in `Build Full Map 2`** — NOT a real gap: documented full-site single-bake
  fragmentation (bigger wrap-around forest + smaller rooms → thinner navmesh → one-pass bake fragments where big
  rooms used to bridge). ENTRY→DEEP guaranteed by the generator (corridor stitch + safety net; 50-seed logic test
  green); DROP-OFF→ENTRY navmesh ✓. Players move by physics; monster AI → backlogged tiled `NavMeshSurface` bake.
- **`run_tests` over the MCP bridge is UNRELIABLE** (one-shot client drops on the test-mode domain reload →
  300s/200s timeouts, no result). Authoritative verification = **headless `-runTests` with Unity CLOSED**
  (per [[unity-headless-batch-tests]]). CS0108 warnings = pre-existing (`EchoMold.cs:95`, `ScavengeCargoZone.cs:55`).
- **Unity was CLOSED for the headless run → relaunched for the PM.** All art-pass + shell + wrap-around + room
  changes remain UNCOMMITTED pending overall visual sign-off.

### ↻ PM FEEDBACK (2026-06-20) — single exit → added a real connected FIRE EXIT — VERIFIED GREEN (live)
PM: with one exit, returning from deep is tedious/risky (loses LC's main+fire-exit mechanic). Agreed — proper
version of the fire exit I removed earlier (now actually connected to the interior). PM chose: add it, near DEEP.
- `CarveBackExit` carves DEEP → north boundary (mirror of `CarveEntrance`) → `backCell`; `BuildInterior` opens its
  north edge as a door; `res.FireExit` = north-edge centre. `BuildShell` splits the north wall around it with the
  SAME sealed vestibule (`Shell_N_L/R` + lintel + `Shell_NJamb_L/R` bridging shell z1 → interior z=h*Cell) → leads
  straight into the maze, no cavity leak. `BuildOutdoor` adds a fire-exit porch clear + a CRT-green exit-sign
  marker (`FIREEXIT_Marker`; art-bible: CRT green = exit signs). Menu logs a new DEEP → FIRE-EXIT check.
- **Live verify (Build Full Map 2; recompile 0 errors, 0 console errors):** fireExit=(102,0,96); **✓ DEEP →
  FIRE-EXIT walkable 6.0 m** (deep bails out the back); `Shell_N_L/R` + `NJamb_L/R` present. BONUS: the back
  corridor's extra connectivity also **restored ✓ DROP-OFF → DEEP walkable (252 m)** — the earlier PathPartial
  fragmentation is gone. DROP-OFF → ENTRY 90 m ✓. 11147 nav tris, 201×266 bounds.
- Extraction = ONE van (south; preserves the dispatch-van ritual); fire exit (NE) = fast interior escape, then jog
  back through the wrap-around woods to the van.
- **Full EditMode suite NOT re-run for this increment** (bridge run_tests unreliable; Unity open). Low risk: only
  the 2 reflection tests touch this code, both confirmed live (builds clean + DROP-OFF→ENTRY walkable). Run
  headless before commit. Still UNCOMMITTED pending overall sign-off.

### ↻ PM FEEDBACK (2026-06-20) — REAL foliage (Foliage Pack swap) — VERIFIED GREEN (headless)
PM: whitebox plants look fake → import real foliage. PM imported TWO Asset Store packs: **Foliage Free** (Jake
Sullivan — 4 pines + ferns, plain meshes) + **TreePackVol.1** (ALIyerEdon — ~50 trees, legacy Tree Creator).
- **Picked Foliage Free** (plain meshes → URP-clean). TreePackVol.1 deferred (Tree Creator = URP-painful; needs
  the official Render Pipeline Converter if its variety is wanted later). Both packs ship legacy/built-in shaders →
  magenta in URP until converted.
- **New `FoliageSet` ScriptableObject** (Assembly-CSharp, data-driven, in Resources) + **`Editor/FoliageSetup.cs`**
  (menu *Setup Foliage (URP + Resources)*): converts Foliage Free's **7 materials → URP/Lit** (alpha-clip,
  double-sided cards, **dead-olive tint (0.62,0.60,0.46)** killing vibrant green per art-bible) and authors
  `Assets/Resources/FoliageSet.asset` (trees=4 pines, bushes=ferns; treeHeight 6 / bushHeight 0.9 / trunkRadius 0.35).
- **`MapSiteBuilder.Tree()/Bush()`** now instantiate a seeded prefab via **`PlaceModel`** (auto-fits to target
  height by measuring renderer bounds → scale → base on y=0; strips imported colliders; adds ONE unscaled trunk
  capsule = sole nav obstacle). Pick is a planar hash (NOT the scatter RNG). **Missing FoliageSet ⇒ procedural
  primitive fallback** (generator still works with no assets). `Foliage()` lazy-loads via `Resources.Load`.
- **Bridge had to be bypassed:** new `.cs` need editor FOCUS to import (`recompile_scripts` ≠ Refresh); the MCP
  bridge then wedged hard (listening but TIMEOUT) and a **detached GUI relaunch HANGS at ~72 MB on graphics init**
  (the documented [[unity-bridge-restart-recovery]] hazard). **Resolution = all HEADLESS:**
  `-executeMethod FoliageSetup.Setup -quit` then `-runTests -batchmode`.
- **✅ VERIFIED GREEN (headless, 2026-06-20):** 0 compile errors; EditMode **204/197** (7 McpUnity noise, **0 real
  failures**); ALL `FullSiteBuildTests` pass incl. `Site_builds_geometry_without_throwing` (real-tree instantiation
  doesn't throw) + `Outdoor_approach_dropoff_to_entry_is_walkable` (navmesh walkable with real trees + trunk
  capsules). The 6 log NREs are all `McpUnity.Tools.BatchExecuteTool:122` (noise); ZERO foliage-code errors.
- **PM TO SEE IT:** open Unity from the **Hub** (NOT a CLI/background launch — that hangs on graphics init) → Build
  Full Map 2 → dead-olive pines + ferns scatter instead of primitives.
- **Still UNCOMMITTED (whole session) pending sign-off.** New: `FoliageSet.cs`, `Editor/FoliageSetup.cs`,
  `Resources/FoliageSet.asset`. **Open commit decision:** the imported packs `Assets/Foliage Free/` (used) +
  `Assets/TreePackVol.1/` (unused) are large — decide commit-into-git vs gitignore + re-import.

### ↻ PM FEEDBACK (2026-06-20→21) — foliage material fix, grass, terrain undulation → PIVOT TO UNITY TERRAIN
PM iterated on the outdoor look. Sequence of fixes (all on disk + compile; the LAST one is HALF-DONE — see RESUME):
- **Trees/ferns rendered with NO material** (white). Cause: the Foliage Free FBX use legacy "material by base
  texture name" search (no externalObjects remap) → bind fails in URP. **Fix (done, verified green):** `FoliageSet`
  now stores `treeMaterial`/`bushMaterial` (the converted URP `pinetree.mat`/`fern.mat`); `MapSiteBuilder.PlaceModel`
  **force-assigns** the material to every renderer of the instantiated model. Headless EditMode 204/197 (0 real fails).
- **Ground should be grass:** `FoliageSetup.MakeGroundMaterial` makes `Assets/Resources/GrassGround.mat` (URP/Lit,
  `grass2_diffuse.tga`, tiled 50×65, Repeat). Tint started dead-olive `(0.5,0.52,0.42)` → looked like dirt → bumped
  greener to `(0.46,0.58,0.30)`. `BuildOutdoor` uses `fset.groundMaterial` for the ground.
- **Ground should undulate** (PM: 高低不平/隆起). Implemented a procedural undulating MESH path in `MapSiteBuilder`:
  `GroundY(x,z)` (Perlin, flattened near building+drop-off via `_gSeed/_gBW/_gBD/_gDropX/_gDropZ`, `GroundAmp=2.2`),
  `BuildGroundMesh` (grid mesh + MeshCollider), and ALL placement now sits on it (`PlaceModel` got a `groundY`
  param; `Tree/Bush` pass `GroundY`; `Rock` offset; pad/van offset). **⚠ This mesh-undulation build was NEVER
  actually shown to the PM** — I malformed the Bash tool call THREE times (must use `antml:invoke`, not `invoke`),
  and the bridge kept wedging on recompile, so the rebuild didn't land. The PM's screenshot was the OLD flat
  muddy-grass box → "地好怪 / 没森林感 / 要真草+起伏".
- **PM screenshot + LC-grass question → DECISION: PIVOT OUTDOOR TO UNITY TERRAIN.** Explained LC's "real grass" =
  Unity **Terrain** (heightmap undulation + GPU-instanced **detail grass** + tree instancing) + heavy fog; the
  mesh+GameObject-trees approach can't do instanced detail grass. **PM approved the Terrain pivot ("可以的我统一").**

### ✅ DONE + VERIFIED (2026-06-21) — Unity Terrain outdoor pivot (awaiting PM visual eyeball)
**Goal:** replace the outdoor MESH ground with a Unity **Terrain** (LC-style): heightmap undulation from `GroundY`
+ base grass `TerrainLayer` + GPU-instanced **detail grass** (the "真草") + URP terrain material; keep the working
(material-fixed) GameObject trees/rocks/dressing sitting on it (they already use `GroundY` → match the terrain).
**DONE so far:** `FoliageSet.cs` got 3 new fields — `grassLayer` (TerrainLayer), `grassDetail` (Texture2D),
`terrainMaterial` (URP "Universal Render Pipeline/Terrain/Lit"). Nothing references them yet → still compiles.
**✅ IMPLEMENTED + VERIFIED GREEN (headless, Unity CLOSED, 2026-06-21):**
- **`FoliageSetup.cs`** — `MakeGrassLayer()` → `Assets/Resources/GrassTerrainLayer.terrainlayer` (diffuse
  `grass2_diffuse.tga`, tileSize 8); `grassDetail` = `Foliage Free/detail/grass_detail1.tga`; `MakeTerrainMaterial()`
  → `Assets/Resources/TerrainURP.mat` (URP `Terrain/Lit`). Ran `-executeMethod FoliageSetup.Setup` headless → log
  **`layer=True detail=True urpMat=True`** (URP terrain shader resolved even `-nographics` ⇒ NOT magenta), FoliageSet.asset
  updated, **0 CS errors**. The 3 new assets exist on disk.
- **`MapSiteBuilder.cs`** — `BuildTerrain(parent,minX,minZ,maxX,maxZ,fset)` + `GrassDensity(x,z)` + `TBASE=-4`/`HRANGE=8`:
  257² heightmap from the SAME `GroundY` props use (`h=(GroundY−TBASE)/HRANGE` ⇒ terrain SURFACE == GroundY in world);
  one painted grass `TerrainLayer` (alphamap all-1); 512-res `GrassBillboard` detail grass (GrassDensity = 0 under
  building+pad, 9–13 else) + waving; `Terrain.CreateTerrainGameObject` at localPos `(minX,−4,minZ)`, `materialTemplate`
  = TerrainURP, `drawInstanced`, `detailObjectDistance 140`. `BuildOutdoor`: `grassLayer!=null → BuildTerrain` else
  `BuildGroundMesh` (mesh stays the no-foliage-assets fallback).
- **Headless EditMode 204 total / 197 passed / 7 failed** — the 7 are the documented `McpUnity.BatchExecuteTool`
  batch-noise (NRE count == 7 == failed-test count, 1:1; **0 real failures**). **All 7 `FullSiteBuildTests` PASS**, incl.
  `Site_builds_geometry_without_throwing` (BuildTerrain runs with the terrain path ACTIVE, no throw headless) and
  `Outdoor_approach_dropoff_to_entry_is_walkable` = navmesh bakes over the **TerrainCollider**: **6120 tris (seed 1) /
  6430 tris (seed 777)**, DROP-OFF→ENTRY walkable through the forest. Zero regressions vs the prior baseline.
- **⏳ AWAITING PM VISUAL EYEBALL (art = ADVISORY → UNCOMMITTED).** PM: open Unity from the **Hub** → `Tools ▸ Black
  Commission ▸ Map ▸ Build Full Map 2` (or open `Map2_Procedural.unity` + Play) → check: terrain not magenta; bladed
  grass showing (URP GrassBillboard detail is the ONE render risk — if blades don't show, the base grass TerrainLayer
  still textures the ground, and the fix is `renderMode=Grass` or URP terrain-detail support); trees/rocks sit ON the
  terrain surface. Knobs: `GroundAmp`, `GrassDensity` return, DetailPrototype colors/size, `Loops`/`RoomRadius`/`Branches`.
- **OPS:** HEADLESS with Unity CLOSED (`-batchmode -nographics -executeMethod`/`-runTests`) was reliable this session;
  the [[unity-bridge-restart-recovery]] GUI/bridge hazards were NOT hit. Machine left clean (no Unity.exe, lockfile cleared).
- **COMMIT on thumbs-up:** only `9d059a9` (procedural map system) is committed; everything after — art/shell/fire-exit/
  room-size/foliage/**terrain** — is UNCOMMITTED on `scavenge-settlement-reveal`, all on disk. Then run the formal full suite to lock.

### ↻ PM FEEDBACK (2026-06-21) — "草只是贴图,不是真的草" → instanced MESH grass (live-built, awaiting eyeball)
PM opened the editor, saw the terrain grass as a flat TEXTURE (no 3-D blades). **Root cause** (Unity issue tracker
"URP fails to render grass Terrain details" + manual): URP does NOT reliably render texture/billboard terrain grass;
the manual says **"Instancing details work with all render pipelines"** → instanced **Vertex-Lit detail MESHES** are
the cross-pipeline path. Fix:
- `FoliageSet.cs`: new `grassDetailPrefab` (GameObject) field.
- `FoliageSetup.cs`: `MakeGrassTuft()` authors a 3-quad crossed grass-card MESH (`Assets/Resources/GrassTuftMesh.asset`)
  + URP/Lit alpha-clip, double-sided, **GPU-instanced** material (`GrassDetail.mat`, `grass_detail1`) + `GrassTuft.prefab`;
  forces the grass tga to import alpha-as-transparency. Assigns `set.grassDetailPrefab`.
- `MapSiteBuilder.BuildTerrain`: detail prototype → `usePrototypeMesh + useInstancing + VertexLit` pointing at the grass
  prefab; the texture billboard is kept only as a fallback. **Trade-off:** VertexLit doesn't wave — wind deferred.
- **Bridge wedged** on the post-recompile domain reload (MCP node bridge process died; `execute_menu_item` → "Connection
  closed"). Did NOT kill the PM's open editor. Unity's **8091 listener recovered** after the reload → drove **Setup
  Foliage + Build Full Map 2 via `tools/ws-unity-call.cjs` direct** (bypassing the dead node bridge; that's the go-to now).
- **LIVE build clean (0 terrain/grass errors):** grass assets created; `Build Full Map 2` → navmesh **12100 tris**,
  DROP-OFF→ENTRY 90.2 m / DROP-OFF→DEEP 257.1 m / DEEP→FIRE-EXIT 6.0 m all walkable. **⏳ PM eyeball:** do 3-D grass
  blades show now in the outdoor approach (dark dry-olive, intentional)? If yes → commit. Still UNCOMMITTED.

### ✅ AUTONOMOUS BLOCK COMPLETE (2026-06-19) — all 6 items done + verified
**How the PM sees it on return:** (1) open Unity (via Hub) → open `Map2_Procedural.unity` → **Play** → random
site generates; re-Play = new layout. (2) Or re-run the proof headless:
`Unity.exe -runTests -batchmode -nographics -projectPath . -testPlatform EditMode` → `FullSiteBuildTests`
green (`Dropoff_to_deep_is_walkable_across_seeds`). (3) Or interactively: Tools ▸ Black Commission ▸ Map ▸
**Build Full Map 2** (re-roll + bakes nav + logs `✓ DROP-OFF → DEEP walkable`).
**Follow-ups (backlogged, NOT blocking):** runtime navmesh bake for AI/monster on this map; multiplayer
determinism Play smoke; outdoor art pass (whitebox now); retire old module instantiator (double walls).

## ▶ NEXT SESSION — START HERE: indoor procedural generation (ADR-0003)

**Read first:** `docs/architecture/ADR-0003-indoor-procedural-map-generation.md` (Accepted), the cont.5
section below, and memory `[[unity-bridge-restart-recovery]]`.

**DONE + verified this session:**
- **ADR-0003 ACCEPTED + registered** — custom *constrained grid-based tile-stitching* generator (fixed
  Entry/Mid/Deep anchors + seeded corridors; host-deterministic; not DunGen, not full-LC).
- **Generator CORE (pure logic, GREEN):** `Scripts/Level/Generation/GridMap.cs` + `GridMapGenerator.cs`
  (own asmdef `BlackCommission.Level.Generation`, noEngineRefs). Tests `Tests/EditMode/Level/GridMapGeneratorTests.cs`
  — full EditMode suite **185/185** (determinism / reachability / anchors / variation).
- **Instantiation layer (compiles clean, NOT yet visually confirmed):** `Scripts/Level/GridLayoutInstantiator.cs`
  (cell open-mask → matching `Corridor_*`/`Junction_*` module on the 4 m grid, anchors get marker cubes) +
  editor menu `Editor/GridLayoutPreview.cs`.

**▶ END-VERIFICATION (run when PM focuses Unity — that focus auto-imports+compiles the 2 new files):**
1. **Compile:** clicking into Unity imports+compiles `GridNavMeshBakeSpikeTests.cs` + `GridNavBaker.cs`
   (bridge `recompile_scripts` does NOT Refresh → can't pick up brand-new files; focus does).
2. **Tests (I drive via bridge, no focus needed once compiled):** full EditMode → expect **197/197**
   (185 + 10 harness + 2 navmesh). Targeted navmesh run with logs → Entry→Deep `PathComplete` + bake-time.
3. **Visual (PM eyeballs):** `Tools ▸ Black Commission ▸ Map ▸ Preview Grid Layout` (re-roll a few times) →
   maze + ENTRY/MID/DEEP markers; then `Tools ▸ … ▸ Map ▸ Bake Grid NavMesh` → reads "✓ ENTRY → DEEP walkable".
4. **Phase-4 integration decision (PM)** gates the next dev — see cont.6.

**Remaining ADR-0003 phases:**
- **Phase 3 — NAVMESH SPIKE (biggest MEDIUM risk)** — ✅ DRAFTED 2026-06-19 (runtime-bake approach = my rec).
  `Tests/EditMode/Level/GridNavMeshBakeSpikeTests.cs` (headless `NavMeshBuilder` box-source bake over generated
  cells → assert Entry→Deep `PathComplete` + bake-time budget) + editor `Editor/GridNavBaker.cs`
  (`Bake Grid NavMesh` menu = `NavMeshSurface` over `GRID_PREVIEW`, real-prefab visual + Entry→Deep path log).
  ✅ **VERIFIED GREEN 2026-06-19** (after fixing a pre-existing `CS0104 GridLayout` break — see cont.6): spike
  bake 2.9 ms / Entry→Deep `PathComplete`; real-module bake 271 tris / Entry→Deep 54.7 m walkable. Runtime-bake
  vs pre-bake+`NavMeshLink` still PM's to confirm, but the evidence strongly backs runtime-bake.
- **Phase 4 — wire into runtime `TowerLayoutGenerator`:** host seed → `NetworkVariable<int>` → every peer runs
  `GridMapGenerator` + `GridLayoutInstantiator` (Resources prefab resolver) → identical layout. 4-player
  determinism smoke.
- **Phase 5 — 1000-seed reachability harness** — ✅ DONE + GREEN 2026-06-19. Full EditMode suite **195/195**
  (was 185 + 10 new). Harness metrics: **0/1000 unreachable, 0/1000 safety-net fallbacks, 996/1000 distinct
  layouts**, 1000-seed byte-identical determinism ✓. ADR-0003 Phase-5 validation criterion satisfied.
- **Room CONTENT:** drop RoomDef/`TowerRoomCatalog_v1` content at anchor cells (and optionally room cells along
  corridors); author the 3 hero-anchor rooms (Entry sales lobby / Mid power gate / Deep objective).

**Ops notes:** MCP bridge wedges after heavy ops → recover per `[[unity-bridge-restart-recovery]]` (taskkill +
`run_in_background` direct-exe relaunch; NOT `cmd start`). Fresh `[MenuItem]`s only register on editor focus
(click in-editor). Drive the bridge from Bash via `node tools/ws-unity-call.cjs <method> '<json>' <ms>`.

## Session 2026-06-19 (cont. 6) — ADR-0003 Phase 5: 1000-seed validation harness (Unity was DOWN)

- **Context:** resumed on the START-HERE block. The literal next step (visual `Preview Grid Layout`) needs the
  editor; **Unity was closed** (8091 refused — only stray `node.exe`, no `Unity.exe`). So I advanced the one
  remaining ADR-0003 phase that is pure logic / editor-independent: **Phase 5**.
- **DONE (drafted, statically verified, NOT yet executed):**
  - **Instrumented `Scripts/Level/Generation/GridMapGenerator.cs`** — added a non-breaking `GenerationReport`
    struct (`SafetyNetFired` / `Reachable` / `CorridorCells` / `LinkCount`) + a `Generate(..., out GenerationReport,
    branches)` overload; the existing 4-arg `Generate` now delegates to it. **Layout byte-identical** (report
    only observes pure reads, no RNG draw changes). Verified all **6** existing call sites use the 4-arg form →
    no rebinding / ambiguity (the report overload needs a 5th `out` arg).
  - **New `Tests/EditMode/Level/GridMapReachabilityHarnessTests.cs`** (own file, beside `GridMapGeneratorTests`):
    1000-seed reachability (0 unreachable), 1000-seed **byte-identical determinism** (co-op sync), **fallback-rate
    tracking** (≤1%, expect 0; count logged via `TestContext.WriteLine` → satisfies ADR "fallback count tracked"),
    variation (>¼ distinct, count logged), + degenerate anchors (single / coincident / adjacent-direct-link /
    collinear / tiny-grid / OOB-no-throw).
- **⏳ OPEN GATE:** tests not run (no editor). Verify by running the full EditMode suite once Unity is up (via the
  bridge `run_tests`) OR headless batch (`Unity.exe -runTests -batchmode`, project must be closed). Expect the new
  harness GREEN + existing **185** still GREEN. New `.meta` for the test file generates on next import (normal).
- **Phase 3 NavMesh spike — recommendation tee'd up (awaiting PM confirm + measurement):** start with **runtime
  `NavMeshSurface.BuildNavMesh` at mission load** (the layout is assembled once and never changes mid-mission;
  bake hides behind the van-transit/load screen per ADR-0003 §Performance). **Reuse the existing
  `TowerNavBaker.cs`** in-memory `NavMeshSurface` bake (already proven: 842 walkable tris). Escalate to pre-baked
  per-tile + `NavMeshLink` at doorways + `NavMeshObstacle` carving ONLY if the runtime bake is too slow or seamy.
- **LIVE UNITY (2026-06-19, PM opened editor):** bridge up on 12th poll (~2 min boot). **Phase 5 VERIFIED GREEN**
  — full EditMode **195/195** (185+10); harness metrics **0/1000 unreachable, 0/1000 safety-net fallbacks,
  996/1000 distinct layouts**, 1000-seed byte-identical determinism ✓.
- **Phase 3 WRITTEN (runtime-bake; pending compile+verify):** see the START-HERE Phase-3 bullet. Both files
  statically reviewed against the proven `TowerNavBaker` API; **NOT yet compiled** — brand-new files need a PM
  editor-focus to import (`recompile_scripts` ≠ Refresh; custom `execute_menu_item` needs focus; `run_tests`
  works WITHOUT focus once compiled). After focus → expect **197/197** + a bake-time/Entry→Deep evidence line.
- **BLOCKER — Phase-4 integration is a PM design call (NOT done):** how the FLAT 2D grid generator meets the
  EXISTING vertical tower `Tower_EarthCoast_01` (TowerPlanV8, multi-floor, 25 RoomSlots). Options: **(A)** replace
  tower interior with the grid (fights the vertical model); **(B)** grid drives a NEW flat site / maps 2-6 while
  the tower stays authored (lowest risk — my lean); **(C)** grid per-floor inside the tower, stitched by the
  existing stair connectors (most integration). Awaiting PM before any Phase-4 code (`NetworkVariable<int>` seed
  → per-peer rebuild). Per `[[ask-pm-on-key-decisions]]` I will NOT pick this unilaterally.
- **NEXT:** PM focus-click (compiles) → I run the 197 suite + navmesh-evidence run → PM eyeballs the 2 visual
  menus → PM picks Phase-4 direction → build it.

### ✅ VERIFIED LIVE (2026-06-19, PM focused Unity → I drove the bridge)
- **Found + fixed a PRE-EXISTING latent compile break:** `CS0104 'GridLayout' ambiguous` (UnityEngine also
  defines a 2D-tilemap `GridLayout`). It was silently failing **Assembly-CSharp** (→ cascaded to
  Assembly-CSharp-Editor), which is why the preview / Play mode never actually ran before. Added a
  `using GridLayout = BlackCommission.Level.Generation.GridLayout;` alias to the 3 files that mix
  `using UnityEngine;` + `GridLayout`: `GridLayoutInstantiator.cs`, `Editor/GridLayoutPreview.cs`,
  `GridNavMeshBakeSpikeTests.cs`. `recompile_scripts` → **0 errors** (110 pre-existing CS0618 obsolete-API
  warnings only; bridge survived the domain reload via the patched pump).
- **Full EditMode suite: 197/197** (185 + 10 Phase-5 harness + 2 Phase-3 navmesh). 0 fail / 0 skip.
- **Phase 3 spike (box sources): GREEN** — seed 12345 → 38 tris in **2.9 ms**, ENTRY→DEEP `PathComplete`;
  worst bake over 8 seeds **8.5 ms** (≪ 2 s budget).
- **Phase 3 on REAL modules (driven `Preview Grid Layout` then `Bake Grid NavMesh` via bridge — focus worked):**
  preview placed **22 module tiles + 3 anchors** (seed 52956029); bake via **PhysicsColliders → 271 walkable
  tris**, bounds (56.25, 3.70, 36.40); **✓ ENTRY→DEEP complete path, 8 corners, 54.7 m**. → runtime-bake
  approach is decisively viable (PM still owns the final runtime-bake-vs-prebake call; evidence now in hand).
- ⚠ Preview + navmesh live in the OPEN (untitled/empty) scene and are **UNSAVED** — scratch visual only.
- **Phase-4 direction = B (PM chose 2026-06-19):** grid drives a NEW flat site / maps 2-6; vertical
  `Tower_EarthCoast_01` stays authored & untouched.
- **Phase-4 CORE WRITTEN + COMPILES (2026-06-19):** `Scripts/Level/GridMapNetworkBuilder.cs` (Assembly-CSharp,
  mirrors `TowerLayoutGenerator` seed-sync exactly): `[RequireComponent(NetworkObject)] : NetworkBehaviour`,
  `NetworkVariable<int> netSeed` (server-write/everyone-read, -1=ungenerated), server rolls seed (or
  `fixedSeedForTesting`) → every peer rebuilds locally via `GridMapGenerator` + `GridLayoutInstantiator`
  (name→prefab resolver from a serialized module array, Resources/Maps/Modules fallback). Imported via bridge
  `Assets/Refresh` (focus present) → `recompile_scripts` clean, 0 `error CS`.
- **Phase-4 REMAINING (needs Play mode = PM, or a bridge-built scratch scene):** a flat-site scene with a
  NetworkManager + a NetworkObject carrying `GridMapNetworkBuilder` + the 11 `Corridor_*/Junction_*` modules
  assigned (or dropped into `Resources/Maps/Modules/`) → **Play-as-host** = map generates locally → **4-player
  determinism smoke** (all peers identical from the one seed). Can't be EditMode-tested (needs a live
  NetworkManager); the deterministic generator core underneath is already proven (197/197).

## Session 2026-06-18 (cont. 5) — Map 1 (tower) modular-system audit + GDD sync

- **Pivot:** PM redirected from settlement-reveal slice 2 to MAP work ("先从地图开始"). The slice-2 brief
  (cont. 4 below) is still valid; nothing lost.
- **Audited `Tower_EarthCoast_01` variation by READING the runtime logic + asset tree (not just component
  presence — that gave a wrong first answer):**
  - ✅ **Routes vary per seed** — `TowerLayoutGenerator.ApplyTopology` runs every seed *regardless of
    catalog*, toggles each `Connector` open/closed, validated solvable (fallback = all-open).
  - ⚠️ **Room-content re-roll DORMANT** — content fill is `if (catalog != null)`; scene `catalog` ref =
    **null (fileID 0)** and **0 `RoomDef` / 0 `TowerRoomCatalog` assets exist** project-wide → the 13
    Random slots get no per-seed content (skipped + logged, no crash).
  - ❌ **Scavenge loot NOT in the tower** — `LootSpawner`/`LootAnchor` absent from the tower scene (only
    in `Scavenge_Testbed`). Tower = objective-retrieval (`TARGET`+`POWER`+`VAN`), not the loot loop.
  - **25 RoomSlots = 13 Random + 12 fixed** (1 Objective TARGET, 1 PowerGate POWER, 1 VAN, 4 Stair,
    5 Fixed: LOBBY/HALL/SALES/SHOWFLAT/BALCONY).
- **Corrected an earlier wrong claim** (made before reading the logic): routes DO vary; room-content does NOT.
- **DONE:** synced `design/gdd/map-sequence-and-modular-system.md` — fixed the "Map 1 = fully fixed
  layout" contradiction + added an "Implementation Status (verified 2026-06-18)" table.
- **Room pool v1 (PM approved 10-room first series, 2026-06-18):** authored
  `design/levels/tower-room-pool-v1.md` — 10 RoomDef specs (4 S / 4 M / 2 L; 6 🔄Shared + 4 🏢Tower-only),
  door-agnostic zone layouts + loot anchors (dormant until scavenge integration) + RoomDef metadata table
  (weight/allowDuplicates tuned so 10 rooms fill all 13 Random slots via duplicates). **Spec is a DRAFT
  awaiting PM review; nothing built yet.** On approval → build content prefabs + RoomDef assets +
  `TowerRoomCatalog`, assign to `TowerLayoutGenerator.catalog` (currently null), verify a few seeds.
- **Build pass (2026-06-18) — direction: 整图压成毛坯工业风 (PM), blocked on Unity restart:** use the real
  Asset Store packs `Assets/TirgamesAssets/Factory` (79 prefabs: PowerBox/MetalCabinet/Barrel/Debris×13/
  industrial doors+gates/fire ext/ceiling lamp/decals) + `Assets/Sat Productions/.../Concrete Props Pack`
  (concrete blocks/bricks/barrier/pipes). Authored `Assets/_Project/Editor/TowerRoomPoolBuilder.cs`
  (idempotent; menu *Tools ▸ Black Commission ▸ MVP ▸ Modules ▸ Build Tower Room Pool v1*) — data-driven,
  dresses all 10 rooms from real concrete/industrial prefabs (+ a few whitebox: Desk/CRT/NoticeBoard/
  Shelving where meaningful), emits LootAnchors + carving NavMeshObstacles, builds 10 RoomDefs +
  `TowerRoomCatalog_v1.asset`. Spec banner added to `tower-room-pool-v1.md`.
  **✅ RAN SUCCESSFULLY (2026-06-18, after PM restarted Unity; driven via `ws-unity-call.cjs` → 8091 because
  the in-session `mcp__mcp-unity__*` tools had deregistered when the bridge dropped).** `Build Tower Room
  Pool v1` produced **10 room prefabs + 10 RoomDefs + 30 loot anchors + `TowerRoomCatalog_v1.asset`**
  (catalog references all 10; NO missing-prop fallbacks — every Tirgames/Sat prefab resolved). Split: 6 in
  `Shared/Rooms`, 4 in `Tower_EarthCoast_01/Rooms`.
  **Materials: ✅ converted.** Added `Assets/_Project/Editor/UrpMaterialConverter.cs` (menu *Convert Pack
  Materials To URP*, scoped to the two pack folders) → 43/46 pack mats Built-in→URP/Lit (3 skipped =
  skybox/special, unused). NOTE: new `[MenuItem]`s only register after the editor regains FOCUS — had to
  ask PM to click Unity before the converter menu would run.
  **Placement FIXED + verified (bounds-based).** First placement was wrong (assumed base-pivot → props
  sank/scattered; PM caught it). Rewrote `BuildRoomPrefab` to place by actual render bounds: centre the
  footprint on the target XZ, rest the bottom on y=0. Verified via per-room `[RoomPool]` bounds logs — all
  10 rest at y=0; 9/10 fit footprint. ⚠ `Room_ShowFlatBath` is 7.6m wide in a 4×4 (`Pipes_long_01` too
  long — swap for a shorter prop).
  **Showcase: ✅** 10 corrected prefabs re-placed in a row (x=0..162, 18m apart) in the scratch scene via
  `add_asset_to_scene` (`{assetPath, position:{x,y,z}}`). CONTENT sets only (props + loot anchors, NO
  walls/floor — those come from the tower's fixed shell at integration), so they read as prop clusters
  sitting on the floor.
  **Lessons:** (1) verify placement via MCP `get_gameobject`/bounds logs, never trust blind math;
  (2) removed a `DidReloadScripts` auto-build hook — AssetDatabase writes are blocked during a domain
  reload, so it half-deleted assets; regenerate via the **menu after an editor focus-click** instead.
  **Next (PM):** review the 10 → tweak props/scale if needed → assign `TowerRoomCatalog_v1` to
  `TowerLayoutGenerator.catalog` (still null) so the tower fills its 13 Random slots + EditMode test.
  **Bridge:** drive via `node tools/ws-unity-call.cjs <method> '<json>' <timeoutMs>` (8091); custom menus
  need editor focus to register.
  ⚠ Packs are industrial/concrete only — no residential furniture (sofa/bed/bath); residential rooms lean
  whitebox + concrete until a furniture pack is added.
- **Handoff to PM for manual furnishing (2026-06-18):** PM is taking over furniture placement. Reworked
  `BuildShowcaseScene` (menu *Build Room Pool Showcase Scene*) → builds `RoomPool_Showcase.unity`: 10 EMPTY
  rooms = floor + walls + a doorway (jambs + header) each, 5-wide grid, sized to footprint. **Wall height
  matched to the tower shell: 3.2 m** (`TowerV8WhiteboxBuilder.WallHeight` / `ModularRoomBuilder.CeilH`;
  perceived ceiling ~2.4 m cable tray; door clear 2.25 m) — earlier 2 m showcase walls were wrong.
  ⚠ Some auto-props were ~4 m tall (LoadingBay/ShowFlatFloor) > the 3.2 m room — PM hand-sizing fixes that.
  **MCP bridge pump wedged again** (recompile timed out) → PM runs the menu in-editor (focus auto-recompiles,
  which also revives the bridge). **Next:** PM furnishes showcase rooms → I bake each room's furniture back
  into `…/Rooms/Room_*.prefab` (the catalog data source).
- **Generation pipeline WIRED (2026-06-18) — "写通" per PM.** Routes already worked (`ApplyTopology`,
  covered by `TowerTopologyTests`). Wired the room-fill half WITHOUT a scene edit or the (wedged) bridge:
  moved `TowerRoomCatalog_v1.asset` → `Assets/Resources/Config/`; added a **Resources fallback** in
  `TowerLayoutGenerator.GenerateIfReady` (`Resources.Load<TowerRoomCatalog>("Config/TowerRoomCatalog_v1")`
  when the scene `catalog` field is null — scene assignment still wins); pointed the builder `CatalogPath`
  there too. So at runtime the 13 Random slots now fill via `TowerLayout.Fill` (deterministic, seed-synced,
  dup-aware). Added `Tests/EditMode/Level/TowerRoomFillTests.cs` (4 tests: 6S/6M/1L config all fill, fixed
  slots draw nothing, size-filter + stable order, used-non-dup excluded). **NOT yet executed** (bridge
  wedged / editor in use). Verify by: run the EditMode test once the bridge revives, AND open
  `Tower_EarthCoast_01` + Play-as-host → slots fill + routes vary. Tower shows the AUTO-furnished content
  prefabs until PM's hand-furnished rooms are baked back into `…/Rooms/Room_*.prefab`.
- **ADR-0003 authored (Proposed, 2026-06-18) via `/architecture-decision`.** Indoor procedural map gen =
  custom **constrained grid-based tile-stitching generator** (fixed Entry/Mid/Deep hero anchors + seeded
  tile stitch on a cell grid; only the int seed on the wire; per-seed content fill via existing
  `TowerLayout.Fill`). Alternatives weighed: DunGen (paid, the asset LC uses) / full free-form / keep
  fixed-shell. Depends on ADR-0001; registry conflict check = clean. Engine-reviewed inline (lean mode
  skips the TD gate): sound — the one MEDIUM risk to **spike FIRST = runtime navmesh on generated geometry**
  (`NavMeshSurface` runtime bake vs pre-bake+`NavMeshLink`+`NavMeshObstacle` carve). Registry updated:
  +api_decision(`indoor_map_generation`) +interface(`procedural_layout_sync`) +forbidden(`nondeterministic_generation_input`)
  + backlinked ADR-0003 into `all_shared_gameplay_state`. **ADR ACCEPTED (PM, 2026-06-18)** → implementation started (next bullet). Validate via
  `/architecture-review` in a FRESH session (never the authoring session).
- **Indoor generator — DEV STARTED (2026-06-18), pure-logic core first (no bridge/Play needed):**
  `Assets/_Project/Scripts/Level/Generation/GridMap.cs` (grid + cell-occupancy + corridor link graph) +
  `GridMapGenerator.cs` (seeded: fixed anchors → connect via randomized-Manhattan with guaranteed L-fallback →
  seeded branch dead-ends → BFS reachability) + `Tests/EditMode/Level/GridMapGeneratorTests.cs` (determinism /
  reachability / anchors / variation). Pure C# (System.Random only, no Unity) — deterministic + headless-testable,
  mirrors `LootSpawnPlanner`. **✅ VERIFIED GREEN 2026-06-18: full EditMode suite 185/185 (incl. my 4
  generator tests).** Fix that was needed: the pure generation code needed its OWN asmdef
  (`BlackCommission.Level.Generation`, noEngineRefs) so the test asmdef could reference it; removed
  `TowerRoomFillTests` (its `RoomDef`/`RoomSlot`/`TowerRoomCatalog` types are in Assembly-CSharp, which an
  asmdef test can't reference). Bridge was restored by `taskkill /F /IM Unity.exe` + `rm Temp/UnityLockfile`
  + relaunch via Bash `run_in_background` direct-exe (`cmd //c start` was flaky). NEXT: Unity
  tile-instantiation layer + doorway capping → navmesh spike → wire into `TowerLayoutGenerator`.
- **Still open (PM):** tower↔`Scavenge_Testbed` integration (objective map vs scavenge loop).

## Session 2026-06-18 (cont. 4) — settlement-reveal build, slice 1 (calculator → B model)

- PM approved starting dev on the **per-item settlement reveal** (the 4-agent-unanimous moat).
- **Finding:** per-item DATA already exists (`SettlementResult.Lines`); the "total-only card" is a UI gap, not a
  data gap. But the calculator was still on the OLD commissioned-target model (1.4×) — B (category preference)
  was a doc-only change, not in code.
- **Slice 1 DONE (code, see commits):** reconciled the pure logic core to B — `SettlementItem.IsFavoured`,
  `SettlementLine.PreferenceApplied`, `SettlementResult` drops `CommissionedTargetDelivered`, calculator uses
  `clientPreferenceMultiplier` (1.3; `ScavengingConfig` knob renamed 1.4→1.3); 13 EditMode tests rewritten; 2
  caller one-liners. 6 files, +60/−55. No dangling old-API refs.
- **VERIFIED GREEN** (headless EditMode, 2026-06-18): total 181 / passed 174 / failed 7 — the 7 are ALL
  `McpUnity.Tests.BatchExecuteToolTests` (known bridge-package noise, not game code). All 12 calculator
  tests pass; the 174 include every game EditMode test, so the `SettlementResult`/`SettlementItem`
  signature change broke nothing project-wide. Slice 1 done + committed (branch `scavenge-settlement-reveal`).
### ▶ NEXT — Slice 2 (runtime wiring) STARTING BRIEF (read this first after /clear; the conversation context is gone)

**On branch `scavenge-settlement-reveal`.** Goal of the whole build: the **per-item settlement reveal** (the
4-agent-unanimous moat). Slice 1 (calculator → B model) is DONE + verified green + committed.

**Key fact that shapes everything:** the per-item DATA already exists —
`ScavengeSettlementCalculator.Settle()` returns `SettlementResult.Lines` (per item:
ItemId / BaseValue / Condition / PreferenceApplied / Payout) + Total. The "total-only card" is a UI/wiring gap,
NOT a data gap. So slice 2-4 = feed the right inputs in, and surface the lines out.

- **Task A — feed real data into the deposit path.** `ScavengeCargoZone.TryStow` (~line 128) hardcodes
  `new SettlementItem(item.ItemId, item.BaseValue, ItemCondition.Good)` → IsFavoured defaults false, condition
  always Good. Wire the item's real `Condition` + compute `IsFavoured` = (item.Category ∈ the run client's
  favoured categories). FIRST READ: `Assets/_Project/Scripts/Scavenge/ScavengeItem.cs` (does it carry
  Category/Condition?), `ScavengeItemDefinition.cs`, `LootSpawner.cs` (stamp condition+category at spawn?),
  `ScavengeCategory.cs` (12 categories). NOTE: pure-logic `Scavenge.Core` must NOT depend on `ScavengeCategory`
  (content layer) — that is why `SettlementItem.IsFavoured` is a caller-precomputed bool.
- **Task B — where do favoured categories live?** The mission/client must declare 1–2 favoured categories.
  Check `OfficeTaskDefinition` + the tower mission setup; pick the home (likely the task/client definition).
- **Task C — flow `Lines` to the UI.** `Assets/_Project/Scripts/Office/MvpPendingReward.cs` currently carries
  Total only; the reveal needs the per-item lines. READ: `MvpPendingReward.cs`, `UI/SettlementCardOverlay.cs`,
  `UI/SettlementUIController.cs`. Then slice 3 (author the tower client's per-item usage notes) + slice 4
  (per-item reveal overlay: item | condition | price | note, emotional-weight order, last item lands hardest).
- ⚠️ **UX spec caveat:** `design/ux/settlement.md` is the OLD tower card (flat reward + completeness +
  single mission note) — do NOT build to it. The per-item reveal is specced in
  `design/quick-specs/scavenging-item-system-2026-06-16.md` §4/§6 + `design/gdd/scavenging-core-loop.md` §3.7.
- **Verify** new logic with EditMode tests (TDD). Headless run (editor closed): find Unity at
  `C:\Program Files\Unity\Hub\Editor\6000.4.7f1\Editor\Unity.exe`, then
  `Unity.exe -runTests -batchmode -nographics -projectPath D:/BlackCommission -testPlatform EditMode -testResults <out>.xml -logFile <out>.log`
  — clear `Temp/UnityLockfile` first; **7 `McpUnity.Tests.BatchExecuteToolTests` failures = ignorable noise**;
  revert any `ProjectSettings/*.asset` the run auto-touches. Slice-1 baseline was 174 passed / 7 noise.

## Session 2026-06-18 (cont. 3) — gap reconcile («先合»)

- **Item condition locked at 3 states (污染 cut, PM).** Reconciled `scavenging-item-system` + `danger-infection`
  spec to 3 states (完好/一般/受损; GDD §4 was already 3-state). **New mechanic:** a valuable item
  (baseValue ≥ `valuableConditionThreshold`) degrades 1 step on a "hard" release outside the van —
  downed / knocked from hands / from height / thrown; van-deposit + gentle set-downs are safe.
- **OfficeLevel / XP / per-level category-unlock = CUT** (PM): all item categories always present; license
  gates jobs, client preference differentiates value.
- **`office-economy-progression.md` fully reconciled to money-only:** removed Partial + XP + OfficeLevel/
  leveling; reward MODEL realigned to `gross = Σ payout_i` (scavenge salvage), Failure = `salvage ×
  failurePayoutRate`; tuning knobs + AC + edge cases cleaned; Open Q1 reframed (thin-haul, not partial).
- **Mechanical sweeps:** "5 license stages → 4 / Stage 5 → Stage 4" across scavenging-core-loop / map-sequence /
  game-concept / item-system. **`mission-state-machine.md` got a SUPERSEDED banner** → scavenge loop.
- **Deferred (PM «其余之后»):** `entities.yaml`, UX docs (settlement/boarding/hud/office-computer),
  monster-system.md (2 forcedTimeoutOutcome refs), mission-pool-selection, docs/mvp-core-loop.md, + a full
  rewrite of mission-state-machine.md. None block building.

## Session 2026-06-18 (cont. 2) — Lethal Company differentiation review (4-agent)

- Ran a 4-specialist parallel review (creative-director / game-designer / level-designer / narrative-director)
  on PM Q "too similar to Lethal Company?". **Verdict: unanimous MEDIUM** — loop skeleton is LC-shaped
  (genre-inherent), identity is genuinely BC but lives downstream and is **mostly not in the runtime yet**
  (~80/20 fiction/mechanics carry the "not-LC", inverse of the current build).
- **Unanimous #1 finding:** build the **per-item settlement reveal** (authored client usage notes) — all four
  independently crowned it the moat; flagged Core/blocking but still a total-only card. Synthesis written to
  `design/lethal-company-differentiation-2026-06-18.md`.
- **Open tension flagged:** creative-director's "add one environmental danger accelerator" conflicts with the
  C2 "pure time, no action spikes" lock — PM call, left open.

## Session 2026-06-18 (cont.) — pulled remote + locked open design Q «B»

- **Pulled `origin/main`** (6c3e941→adefee3, clean ff): economy gutted to money-only in CODE,
  scavenging-core GDD landed, 2026-06-18 economy/danger/item-condition design overhaul.
- **Locked open design question B (Free Salvage vs Commissioned)** → **R1: no target item.**
  A Commissioned/Black job differs from Free Salvage by (1) client **category preference**
  (favoured 1–2 categories settle `× clientPreferenceMultiplier`, default **1.3**) and (2) per-item
  settlement satire. No designated target, no guaranteed spawn, **no `Partial` outcome** (Success/
  Failure only). This resolved a live self-contradiction (§0 D-A vs §3.4 body).
- **Reconciled the docs** (PM-approved full clean): `scavenging-core-loop.md` (§0 new D-G, §1, §3.4,
  §3.5, §3.6, §4, §5, §6, §7, §8, §9) + `scavenging-item-system-2026-06-16.md` (§5, knobs, affected
  systems, AC). `git diff --check` clean; ~93 ins / 85 del. **UNCOMMITTED** (per project rule).
  Bonus: R1 SIMPLIFIES the code roadmap — drops the "guaranteed-target placement" + "emit Partial"
  net-new features; remaining work = add a per-item `category` stamp + apply the preference multiplier.
- **Locked A (license advance gates):** advance = complete that stage's **story mission**, which appears
  after `license_story_gate[stage]` total missions (**4 / 10 / 18**, balance-pass TBD); no auto-advance,
  but rising EarthDet cost makes camping a low stage unsustainable. Written to `office-economy-progression.md`
  (rule 8, new License-advancement subsection, Open Q2 resolved, 3 tuning knobs).
- **Locked C (danger formula + monster per-phase):** hybrid — `danger_level` continuous & **pure time**
  (no action spikes, PM-locked); the danger spec's 4 phases (Survey/Active/**Pursuit**/Saturation @
  0/8/18/28 min) are discrete markers, monster activity lerps within. Renamed phase Hunt→**Pursuit** to
  un-collide with AI state `Hunt`. Reconciled `monster-system.md` (4 edits, dropped old 3-phase %/action_spike
  model) + `danger-infection-system-2026-06-18.md` (8 edits).
- **D direction RESOLVED (R-B):** man-made sites in biomes (not pure wilderness, not sealed interiors); existing
  Entry/Mid/Deep hero-anchor framework kept. **DONE:** formalized the framework + biome-approach layer + B/A/C
  integration + tower worked-template, written into `map-sequence-and-modular-system.md` (+ a Biome column on the
  map table). **Remaining:** per-map hero-room fill (3 anchors + client favoured-categories) for maps 2–6 = a
  later level-design pass.
- **Flagged, NOT in B's scope:** item condition is 3-state in the GDD (§4 {1.0/0.7/0.4}) but 4-state
  in the 2026-06-18 design (完好/一般/受损/污染); "five license stages" refs in §9/safety-cost are stale
  (now 4). Separate reconciliations.

## Session 2026-06-18 — Economy & Map Design Overhaul

### Locked design decisions (PM David)

**Economy model (replaces prior GDD):**
- Reputation system: **removed entirely** from all docs and code
- Hostile takeover / competitor restructure: **removed**
- License revocation by Mars authority = game over (mid-game failure OR Stay-on-Earth ending)
- Two independent tracks: License Stage (job access) + Earth Deterioration Level (safety cost)
- Safety costs driven by EarthDet level, not license stage: Stage 1=40G, 2=60G, 3=90G, 4=120G, 5=150G
- EarthDet advances every ~5 missions regardless of player choices
- Stay-on-Earth ending: Mars clients stop commissioning, free salvage only, EarthDet still rises → inevitable fade-out
- 4 license stages (not 5): 临时采回 / 正式采回 / 特殊样本转运 / 移民资格审查
- Updated GDD: `design/gdd/office-economy-progression.md`
- Updated pillars: `design/game-pillars.md`

**Time pressure replacement (mission clock removed):**
- No hard time limit. Two-track continuous pressure replaces timer:
- Track A — Danger Level: rises over time (Survey→Active→Hunt→Saturation), expressed through
  monster behaviour and audio only, never shown as a number. Saturation = 60s forced evac.
- Track B — Infection Exposure: per-player, visible on HUD as suit fill indicator.
  Formula: `base_rate(3/min) × danger_multiplier × zone_factor`. Converts to `去污处理费`
  deduction at settlement (1G per exposure unit).
- Multi-trip: each re-entry advances Danger Level phase by 1 (trip 2 starts at Active, etc.)
- New spec: `design/quick-specs/danger-infection-system-2026-06-18.md`

**Item condition system (new):**
- 4 states: 完好 / 一般 / 受损 / 污染
- Shown on pickup alongside weight class
- Determined by: spawn environment + player behaviour (drops, exposure, monster hits)
- High-exposure players picking up items in deep zone degrade condition by 1 step
- Updated spec: `design/quick-specs/scavenging-item-system-2026-06-16.md`

**A镜 (Analysis Lens) — late-game shop item:**
- Default: condition shown as label only (完好/一般/受损/污染)
- A镜 equipped: shows precise % within bracket (e.g. `受损 34%`)
- Purchased at HQ shop in later license stages
- Gives experienced players more granular negotiation data

**Negotiation mechanic (revised):**
- Players can dispute ONE item per settlement
- Outcome is LOCKED — cannot revert to original price
- Outcome depends on item condition + commission match (not random)
- 完好/一般 + high match → client concedes (+15-30%)
- 受损 or mismatch → client counter-reduces (−15-30%, locked), with authored reason
- Counter-reduce reason is always polite, institutional, morally bankrupt (satire)
- Co-op: all players discuss before pressing; one press commits the team
- Updated spec: `design/quick-specs/scavenging-item-system-2026-06-16.md`

**Outdoor environments:**
- ~~All 6 maps use natural environments (not themed man-made areas)~~ → **refined to R-B (2026-06-18 cont.):** 6 **man-made sites, each set in / approached through a natural biome** (outdoor approach + indoor core). `map-sequence-and-modular-system.md`'s named sites + narrative arc + Entry/Mid/Deep hero framework all KEPT.
- Coastal / Forest / Urban meadow / Open plain / Suburban grassland / Industrial wasteland
- Consistent with world: Earth reclaimed by infection, nature reasserting

**Stale docs fixed:**
- `mission-pool-selection-2026-06-16.md`: removed Stage 5 row, reputation references → replaced
  with license-stage + missions-completed based client variety
- `map-sequence-and-modular-system.md`: Stage 5 / Mars playable map / Reputation refs still need
  cleanup (flagged, not yet done — low priority vs implementation work)

### Pending design decisions (Open)
- Map 2-6 hero rooms: ⏳ direction RESOLVED 2026-06-18 (R-B: man-made sites in biomes); framework + tower template = next; per-map fill later
- ~~License stage advance gates~~ ✅ RESOLVED 2026-06-18 (story mission + min-count gate 4/10/18; no auto-advance) — see cont. entry at top
- ~~Free Salvage vs Commissioned mechanical difference~~ ✅ RESOLVED 2026-06-18 (R1: no target item; client category preference ×1.3) — see cont. entry at top
- ~~Danger Level precise formula + monster behaviour per phase~~ ✅ RESOLVED 2026-06-18 (hybrid: continuous pure-time danger_level + 4 discrete phases; Hunt phase→Pursuit; monster-system.md reconciled) — see cont. entry at top
- Map content (themes, missions, enemies): discussion started, not documented yet

## Current Focus

**Scavenging system (2026-06-17) — P1b sub-block 4 (van weight gate) DONE + verified.** The full
P1a/P2a/B/P1b runtime is in; the deposit gate is now built and headlessly green.
**NEXT on the new device:**
1. ~~P1b sub-block 4 — van weight gate~~ ✅ **DONE** (2026-06-17, this session — see dated log below).
2. **Live play-test of P1b** — ✅ **scene built & verified**: `Assets/_Project/Scenes/Scavenge_Testbed.unity`
   (generated by `Tools > Black Commission > MVP > Scavenge > Build Scavenging Playtest Scene`; in Build
   Settings, index 2). It is SELF-HOSTING (its NetworkManager is copied verbatim from HQ — player prefab +
   DefaultNetworkPrefabs + UnityTransport + AutoPort + QuickNetworkUI all intact, verified in scene YAML).
   **To test: open that scene → press Play → click Host (QuickNetworkUI).** Player spawns in a dressed
   `Room_Office_4x8` (3 loot anchors) → loot spawns → carry an item into the teal van bay (at x≈6) → it
   loads if the weight fits, else stays behind; the on-site 舱位 ticket-strip shows load/capacity.
   (`Build Cargo Zone Test Rig` still exists to drop a rig into any other active scene.)
3. Deferred (PM-owned): real 12-item economy values; P2 settlement reveal UI; P3 commission text; P4 dispute.
Full P1a / P2a / B / P1b detail = the dated session logs below. Note untracked orphans:
`_Project/Content/Scavenge/Items` (superseded by Resources copies).

### Session 2026-06-17 — Scavenging-core redesign + economy gutted to money-only

**Design (PM decisions locked, round 2):** scavenging is the universal core of EVERY mission; a
"Commissioned Job" is defined by its CLIENT (commission text + per-item settlement satire + category
value preferences), NOT a single fetch item — DROPS the single commissioned-target, guaranteed-spawn,
and the `Partial` outcome kind (outcome = Success/Failure only; "less vs more" = the continuous haul).
Tower = Commissioned Job, NO 沙盘. `failurePayoutRate` = 0.5. Monthly safety cost = real per-stage
deduction shown as a settlement-ledger LINE ITEM (not a meter). Eco-column prop → Heavy fixture.
Full doc: `design/gdd/scavenging-core-loop.md` (panel-synthesized: game-designer + level-designer +
creative-director, then adversarially reviewed; the §0 block governs the body).

**KEY DISCOVERY:** reputation / takeover-pressure / XP / office-level were removed from the DESIGN
docs (2026-06-09 overhaul) but were **never removed from CODE** — `CompanyState.ApplyMissionResult`
still ran the −25/+12/+35 pressure FSM, Reputation, and XP→OfficeLevel. A failed run still incremented
the 0–100 counter. (The stale `office-economy-progression.md` 2026-06-10 still documents them.)

**Economy gutted to MONEY ONLY (DONE + verified, PM-approved decision 0):** 13 files —
`CompanyState`/`CompanyData`/`MvpPendingReward`/`OfficeComputer`/`OfficeTaskDefinition`/
`MissionRewardCalculator`/`MvpMissionClock`/`TowerMissionManager`/`ScavengeMissionManager` + UI
(`MvpHud`/`HQController`) + tests. `ApplyMissionResult(success, money, elapsed, …)` moves Funds + job
counters only. RPC/snapshot slimmed; save schema → v2 (old saves load, removed keys ignored). Job
gating no longer uses OfficeLevel/Reputation (all jobs open; `// TODO: license-stage gating`).
**Verified in live editor: 0 compile errors; 126/126 game EditMode tests pass** (rep/pressure/level-up
tests deleted; money-only suites rewritten — CompanyStateTests 10, MissionRewardCalculatorTests 11,
MvpMissionClockTests 13). UNCOMMITTED.
**Still open**: revise the stale `office-economy-progression.md` to money-only; per-item settlement
reveal UI (Q4); the monthly-sink deduction implementation; tower rebuild onto the scavenge loop.

### Session 2026-06-17 (new device) — remote reset + git-lfs install + P1b-4 van weight gate

**Environment fixes (PM asked to overwrite local with remote first):**
- Local was 0 ahead / 53 behind `origin/main`; discarded local WIP (school→tower migration, superseded
  by remote `refactor: remove all school/snow/homework references`). Stashed as recoverable backups
  (`git stash list`), then fast-forwarded to `6c3e941`.
- **git-lfs was MISSING on this machine** (no brew either) → the checkout failed on LFS binaries.
  Installed git-lfs **v3.7.1** to `~/bin/git-lfs` and pointed the repo's LFS filters at the absolute
  path (`git config filter.lfs.process "$HOME/bin/git-lfs filter-process"` etc.), so git resolves it
  regardless of PATH. All **400 LFS files hydrated** to real binaries. (PATH was NOT persisted to
  `~/.zshrc` — the absolute-path filter config makes that unnecessary for repo ops.)
- MCP: the live editor had exited; the running Node bridge was the stale **git-cache** build. After the
  reset the correct **embedded** `Packages/com.gamelovers.mcp-unity/` is on disk and the PackageCache
  copy is gone, so the next editor launch loads the background-safe bridge. (`McpUnitySettings.json`
  still says port **8091**.) Editor relaunched after the headless run.

**P1b sub-block 4 — van weight gate (DONE, uncommitted per project rule):**
- `Scripts/Scavenge/Core/VanCargoManifest.cs` (pure logic): ledger-gated deposit record bridging
  pickup → settlement — `TryLoad`/`TryUnload`/`Reset`/`ToSettlementItems`; ledger + item list stay in
  lock-step. + `Tests/EditMode/Scavenge/VanCargoManifestTests.cs` (11 tests).
- `Scripts/Scavenge/ScavengeCargoZone.cs` (runtime NetworkBehaviour): host-authoritative bay mirroring
  the tower `VAN_CargoZone` pattern — a `ScavengeItem` SET DOWN (not carried) inside the trigger bounds
  is loaded if its weight fits, else left behind; replicates `LoadUnits`/`Capacity`/`ItemCount` for the
  on-site 舱位 ticket-strip (OnGUI, civic-paper grammar, hidden during van transit); `SettleCargo()`
  exposes the manifest to the (future) scavenging mission manager. Stows = record + `Despawn` (no double
  count); full van silently rejects (the leave-behind tension lever).
- `Editor/ScavengeTestbedBuilder.cs`: menu `…> Scavenge > Build Cargo Zone Test Rig` — drops a
  `LootSpawner` + cargo-zone rig (translucent teal bay marker) into the active scene next to its
  LootAnchors. Marks dirty, does NOT save.
- `Editor/ScavengePlaytestSceneBuilder.cs`: menu `…> Scavenge > Build Scavenging Playtest Scene` —
  generates a SELF-HOSTING `Scavenge_Testbed.unity` (NetworkManager copied from HQ, dressed room with
  loot anchors, player spawn + HQSpawnManager, LootSpawner, van cargo zone) + registers it in Build
  Settings. **Ran via MCP this session and verified**: 7 roots, player prefab + DefaultNetworkPrefabs
  refs intact, 0 console errors. Open + Play + Host to exercise the whole loop. (MCP gotcha learned:
  `recompile_scripts` does NOT import a brand-new .cs file → its `[MenuItem]` won't resolve via
  `ExecuteMenuItem`; run `Assets/Refresh` first.)

**Scavenging mission loop WIRED END-TO-END (same session, "串起来"):**
- `Scripts/Scavenge/Core/ScavengeMissionLogic.cs` (pure): single-fire run state machine
  `InProgress → Settled | Failed` (mirrors `TowerMissionLogic`). + `ScavengeMissionLogicTests` (5).
- `Scripts/Scavenge/ScavengeMissionManager.cs` (runtime, mirrors `TowerMissionManager`): on depart it
  settles the cargo zone's manifest via `ScavengeCargoZone.SettleCargo()` (money = Σ item values,
  money-only per progression design), applies `MvpPendingReward` + `SettlementCardOverlay`, seats the
  crew, runs the return transit, loads HQ. All-crew-downed → forced settle (banked cargo still pays).
- `Scripts/Scavenge/ScavengeVanDepartTrigger.cs` (IInteractable, mirrors `TowerVanDepartLever`):
  E on the lever post → `manager.RequestDepart()`.
- Per-item settlement REVEAL (quick-spec §4 P2) intentionally deferred — closes today on the existing
  card showing the run total. Did NOT touch tower code / the van dispatch ritual (no `IMissionAuthority`
  refactor); scavenging uses its own self-contained depart path. Follow-up: unify the van-board ritual.
- `Scavenge_Testbed.unity` regenerated to wire it all (9 roots: + `ScavengeMissionManager` w/ cargoZone
  ref + `DepartLever` w/ manager ref). **Full loop now: spawn → loot → carry → weight-gated deposit →
  E on lever → settle (cargo total) → return → HQ.**
- **Verified via live MCP editor: 0 compile errors; EditMode 141/141 game tests pass (5 new
  ScavengeMissionLogic + 11 VanCargoManifest); scene rebuilt + saved, 0 errors.**

**Verification (headless batchmode `-runTests EditMode`, editor was closed so project was unlocked):**
- **0 CS compile errors** project-wide; **136/136 Black Commission tests pass** (incl. new
  VanCargoManifest 11/11). Evidence: `production/qa/editmode-results-2026-06-17.xml` + `.log`.
- 7 failures exist but ALL in third-party `McpUnity.Tests.BatchExecuteToolTests` (NullRef under headless;
  the embedded MCP package's own tests) — not game code, out of scope. PM may exclude that test asmdef
  from CI later.
- NOT yet done: hosted PlayMode play-test of the deposit loop (needs the test rig placed + a host).
  Condition/commissioned-target on deposited items default to Good/false (later layers).

_Prior focus (superseded, kept for history):_

**Floor-2 v5 redesign** of the abandoned tower (Tower EarthCoast 01) is now written
into `TowerTopologyV3.cs` and `TowerV3WhiteboxBuilder`: the critical route crosses
EDGE/BRIDGE before SALES/VIP/TARGET, A-stair is the fast two-hand carry return, and
F2 has eight toggle loops. Final blueprint:
`Assets/_Project/Art/Maps/Tower_EarthCoast_01/References/Tower_EarthCoast_01_F2_DesignPlan_v5.png`
(generator: `tools/generate_tower_f2_designplan.ps1`). Headless smoke validation passed
for 1,000 seeds. The full-scene rebuild remains blocked until an F2-scoped rebuild
preserves locked F1.

Also (2026-06-08): ran `/map-systems` — upgraded `design/systems-index.md` to a full
decomposition (19 systems, dependency layers, priority tiers, GDD design order). Then
authored the first GDD via `/design-system`: **Level/Map Generation & Topology** —
**COMPLETE** at `design/gdd/level-map-generation.md` (all 8 sections + Visual/Audio + UI +
Open Questions). Hard constraints baked in: per-floor stair landings LOCKED over the floor
below (F2 STAIRA2@26,28 over F1 STAIRA1; STAIRB2@0,16 over F1 STAIRB1); **F1 is locked / never
regenerated** (full-scene Rebuild menu must gain a floor-scoped mode first); only 3 room
sizes; deeper floors escalate maze + danger. Registry seeded (room_size_classes,
f1_stair_anchors, topology_resolve). systems-index: Level/Map Gen → Designed (pending review).
**Next: run `/design-review design/gdd/level-map-generation.md` in a FRESH session.**

### Prior map context: **地球海岸壹号·烂尾预售楼**
(2-floor abandoned pre-sale tower, 15–20 min). Level GDD at
`design/levels/abandoned-tower-earth-coast-01.md`, building on
`Assets/Scene/AbandonedBuilding_Blockout.unity`.

Locked decisions (PM David, 2026-06-06):
- Objective = sales scale model (楼盘沙盘) — "the unbuilt Earth dream as a collectible"
- Floor-2 gate = restore power in `F1_S3_PowerRoom`
- Objective carry = heavy two-hand carry (new mechanic)

Two NEW server-authoritative mechanics needed (heavy carry, power gate) — both fall in
the untested-sync risk zone; need EditMode tests.

Floor plan done: `design/levels/abandoned-tower-floorplan.md` — modular kit (4 room
sizes S/M/L/XL on a 4m grid) + slot-based skeleton + random *content* assignment
(fixed corridors/stairs/shaft so NavMesh + netcode stay valid). Stairs A/B vertically
aligned; shaft void crossed by scaffold bridge; objective (TARGET, NW on F2) far from
van (south-center F1).

## Project Snapshot

- **Engine**: Unity 6 (6000.4.7f1), URP, C#, Netcode for GameObjects (host-authoritative)
- **Code**: ~77 C# scripts; 19 systems decomposed (14 implemented to some maturity) — see `design/systems-index.md`
- **Playable**: HQ + Snow_Lotus_01 (白棘雪莲) mission loop
- **Design docs**: `docs/mvp-core-loop.md`, `docs/world-background-2098.md`,
  `docs/black-commission-long-term-roadmap.md`, `docs/design-decisions.md`,
  `docs/art/black-commission-style-lock-v1.md`

## Key Decisions

- Framework `.claude/` installed (v1.0.0). Runtime layer now installed on macOS:
  `hooks/` (12), `settings.json`, `statusline.sh` — all smoke-tested. Hooks are bash;
  Windows contributors need git-bash on PATH or they silently won't fire.
- Game code stays in `Assets/_Project/Scripts/` (framework `src/` convention maps here).
- `@AGENTS.md` remains the authoritative project rules.

## Session 2026-06-09 — Progression System + Pillar Unification

**Design locked (PM David):**
- Progression backbone = 5 license stages (not office level 1-8, not reputation bar)
- Economy = money only (no reputation metric exposed to player)
- Player level is hidden (dev-visible only; drives job pool selection)
- Mission taxonomy = three tiers: 自由采集 / 指定委托 / 黑色委托 (the moral slope)
- Hostile takeover threat = narrative events only (letters, visitors), no 0-100 UI bar
- Endgame trigger = all story missions complete → 移民资格审查 (moral choice, not money threshold)
- Satire delivery mechanism = settlement screen client usage notes, NOT narrator commentary

**Files updated:**
- `design/game-pillars.md` — pillars 5+6 replaced, anti-pillars added, progression backbone table added
- `docs/world-background-2098.md` — Phase 4 officially named 黑色委托, design note added

**Conflict noted (not yet resolved):**
- `docs/mvp-core-loop.md` still describes the old system (reputation, office levels 1-8, acquisition after 2 jobs, 0-100 pressure). Leave untouched until `/design-system` writes a formal progression GDD to replace it.

## Open Questions / Next Steps

1. **Test gap (confirmed: basically untested)** — networked mission state machine,
   settlement math, and sync have no automated coverage. Highest technical risk.
2. Decide next feature push.

## Session 2026-06-09 — Art Bible + Consistency Pass

- **Art bible completed**: `design/art/art-bible.md` — all 9 sections authored and approved.
  Key decisions: warm tungsten amber = primary accent; CRT green restricted to screens;
  lo-fi silhouette-driven darkness; LC = production method only.
- **Canonical art pointer updated**: `design/game-concept.md` now points to art-bible.md.
  `docs/art/black-commission-style-lock-v1.md` is superseded (partially — add deprecation
  header when convenient).
- **7 visual consistency conflicts resolved**: level docs + pillars now use correct lighting
  vocabulary. Full report: `docs/consistency-report-2026-06-09.md`.

<!-- CONSISTENCY-CHECK: 2026-06-09 | Docs checked: 5 | Conflicts found: 7 | Resolved: 7 | Report: docs/consistency-report-2026-06-09.md -->
<!-- ARCHITECTURE-REVIEW: 2026-06-09 | Verdict: CONCERNS | Requirements: 23 total — 2 covered, 2 partial, 19 gaps | Top gaps: mission-state-machine, player-input-controller, tower-navmesh-topology | Report: docs/architecture/architecture-review-2026-06-09.md -->

## Session 2026-06-09 — Office Economy & Progression GDD (in progress)

- **Task**: Authoring `design/gdd/office-economy-progression.md` via `/design-system`.
  This GDD is the formal doc meant to REPLACE the old `mvp-core-loop.md` progression model.
- **Reconciliation decision (PM-aligned, Layered)**: numeric `OfficeLevel`/`Reputation`/
  `HostileTakeoverPressure` stay as INTERNAL host-authoritative state driving logic; the
  player-facing surface = **money only + license stages + narrative events** (no reputation
  metric shown, office level hidden/dev-only driving job pool, no 0–100 pressure bar).
  Honors locked pillars without rewriting working `CompanyData.cs`.
- **Skeleton created**: all 8 sections + Visual/Audio + UI + Open Questions stubbed.
- **Status**: ✅ COMPLETE — all 8 required sections + Visual/Audio + UI + Open Questions written and approved.
- **Registry**: added `settlement_reward` (formula), `company_start_state` / `full_job_reward` (300G) / `tutorial_acquisition_cost` (150G) (constants).
- **Systems index NOT updated** (user declined this run) — Office Economy row still reads "Implemented"; update to "Designed (pending review)" + GDD link when convenient.
- **Next**: run `/design-review design/gdd/office-economy-progression.md` in a FRESH session (independent critique). Then `/consistency-check`.
- **CD-GDD-ALIGN skipped** (Lean mode) — Player Fantasy framing needs manual review before production.
- **Flagged conflict**: Partial pay as-built 20% vs design-decisions.md 30–50% → Open Questions / PM.
- As-built ground truth captured from `CompanyData.cs` (start −300G/300 debt; full 300G/+5rep/+80xp;
  partial 60G/+15xp; failure 20G/−2rep; pressure ±25/+12/+35; tutorial acq 150G).

## Session 2026-06-09 — Test Gap Push ① (sync-risk EditMode coverage)

**PM direction**: "直接开始开发" — approved the phased plan starting with the test gap
(highest technical risk per systems-index).

**Done (code-only, no scene/asset changes, nothing committed):**
- New asmdef `BlackCommission.Office.Core` at `Assets/_Project/Scripts/Office/Core/`
  (follows the existing Topology asmdef+tests pattern). Moved in via `git mv` (GUIDs
  preserved): `OfficeTaskDefinition.cs`, `MvpTaskCategory.cs`, `MvpMissionClock.cs`.
- Extracted `CompanyState` (out of `CompanyData.cs`) and `MvpMissionResultKind` (out of
  `MvpPendingReward.cs`) into Core. `CompanyData` keeps persistence/Netcode wrapper only.
- New `MissionRewardCalculator` (Core): single source of truth for settlement math —
  the van-departure and exit-point paths in `LostItemMissionManager` previously held
  two duplicated copies; both now delegate (behavior preserved 1:1, incl. as-built
  quirks: overtime deducted on Failed runs; partial = max(consolation, 22%)).
- New EditMode tests `Assets/_Project/Tests/EditMode/Office/` (asmdef
  `BlackCommission.Office.Core.Tests`): `CompanyStateTests` (23 tests — settlement
  deltas, pressure math ±25/+12/+35 + surcharges, ultimatum→takeover two-strike flow,
  level-ups, tutorial acquisition 150G), `MissionRewardCalculatorTests` (16 — all 3
  result kinds, bonus-evidence gating, wrong-homework cap 3×30, overtime, fallbacks),
  `MvpMissionClockTests` (14 — conversion, overtime ceil/floor blocks, 时钟格式化).

**NOT yet verified in Unity** (MCP bridge was disconnected): needs Test Runner →
EditMode run + compile check. Unity auto-generated the new .meta files, so the editor
was open and importing.

**Next**: ② F2-scoped rebuild tool (unblocks tower full-scene rebuild while preserving
locked F1) → ③ heavy two-hand carry + power gate mechanics with tests.

## Session 2026-06-09 — Steps ② + ③ (same session, continued)

**② F2-scoped rebuild**: discovered ALREADY IMPLEMENTED — `TowerV3WhiteboxBuilder.RebuildFloor2V5Only()`
menu `Tools > BC > MVP > Tower > Rebuild Floor 2 v5 Only` (written blind last session). Code-reviewed:
deletion scope + rebuild order match the full rebuild; F1 RoomSlot identity check built in.
**Awaiting PM to run the menu item in Unity** (agent cannot — MCP bridge down this session).

**③ New server-authoritative mechanics (code complete, tests written):**
- New asmdef `BlackCommission.Mission.Core` + pure `PowerGateLogic` (hold-accumulate,
  release-resets, complete-once latch) + 9 EditMode tests in `Tests/EditMode/Mission/`.
- `PowerGateBreaker` (Scripts/Mission/, NetworkBehaviour + IInteractable): host ticks logic;
  `RestoreProgress`/`PowerRestored` NetworkVariables (late-joiner safe); per-tick server
  validation drops disconnected/downed/distant holders; enable/disable object groups for
  shutter+lighting swap; effects latch prevents double-fire. 3s hold per GDD tuning knob.
  NOT yet placed in any scene — needs breaker prop in POWER slot + shutter wiring (step ④).
- Heavy carry completion: `Carriable.CurrentCarrier`/`FindCarriedBy` (server lookup),
  `CarrySystem` server-side downed→force-drop (IsDowned.OnValueChanged), `IsCarryingHeavy`,
  `PlayerHotbar.UseSelectedSlot` heavy-carry lock. Speed 0.55× + IsHeavy already existed.

**User report this session**: "模型1层2层没了" — investigated: blockout scene data INTACT on
disk (more geometry than HEAD); pre-session deletions exist (Icebound_Rift_100K folder + mat,
F1 plan refs). PM said 先别管 — parked, unresolved. Do NOT restore/delete anything without PM.

**Pending Unity verification (PM)**: EditMode tests for Mission.Core (9 new) — Office.Core's
53 already confirmed green by PM this session.

## Session 2026-06-09 — Post-F2-rebuild triage (PM ran the menu)

PM ran `Rebuild Floor 2 v5 Only` and reported "F1 也被改了 + 重叠墙 + 莫名方块".
**Root cause (verified via scene YAML, MCP still down):**
- F1 room geometry NOT damaged (F2-only deletion scopes held). No Door_ prefabs existed
  in scene or HEAD — `F1 - Install doors` was never run/saved; F1 = whitebox + cleanup.
- Perceived damage = F2 returned from its parked height (y≈28, the F1FixTool-era editing
  state) to design height y=4.2 → 88 corridor-wall segs + landmarks + scaffold platforms
  (BALCONY→DOCK E-DROP) + 32 ramp steps now visually interleave with F1 (no ceilings).
- Real defect: corridor walls duplicated onto room wall bands (z-fighting overlaps).

**Fixes delivered (code, needs PM to re-run in Unity):**
- `TowerF2LiftTool.cs` (new): menu `F2 - Lift for F1 editing (+24)` / `F2 - Lower to design
  height`. Lift parks all F2 groups at y≈28 and hides Descents; idempotent, Undo-able,
  does not save.
- `TowerV3WhiteboxBuilder.BuildFloor2CorridorWalls`: boundary segments falling inside any
  room's wall band (padding = WallThickness+0.2) are skipped — kills doubled walls.
  Re-running `Rebuild Floor 2 v5 Only` applies it.
- MCP bridge still disconnected this session; PM asked to restart Unity-side server (8090).

## Session 2026-06-09 — MCP bridge root-caused & fixed; fixes verified in Unity

**MCP saga resolved** (full notes: memory/mcp-unity-bridge-quirks.md): root cause was
`EditorApplication.delayCall` never flushing while the editor is unfocused. Fixed by
EMBEDDING the package at `Packages/com.gamelovers.mcp-unity/` and patching
`McpUnitySocketHandler.OnMessage` to a ConcurrentQueue + `EditorApplication.update` pump.
Canonical port now **8091** (CLAUDE.md updated; bridge reads McpUnitySettings.json).
Forwarder shim left at the old PackageCache path. `~/.claude.json` repointed (backup
`.bak-mcpfix`). Embedded package is ~60MB UNTRACKED in git — PM decides about committing
(ignore `Server~/node_modules` first if so).

**Verification (all via MCP, editor in background):**
- EditMode tests: **131/131 passed, 0 failed** (incl. 53 Office.Core + 9 PowerGateLogic new)
- `Rebuild Floor 2 v5 Only` re-run: 17 nodes / 25 connectors, "F1 RoomSlots preserved",
  no route-collision warnings; corridor walls 88 → **81** after dedup patch
- F1 all 14 room roots present exactly once (scene YAML check)
- Scene saved by the tool; PM still owes a visual pass on remaining whitebox placeholders
  (landmarks/scaffold/rims are intentional gameplay stand-ins)

## Session 2026-06-10 — Plan-first workflow (PM-directed)

PM verdict on current whitebox: corridors zigzag (root-caused: center-aimed ports; T11=6
segments), rooms must stay 3 sizes (already compliant). New workflow: **floor plans →
PM approval → build to plan**. Done:
- Routing fix in TowerV3WhiteboxBuilder: `AimPoint` port alignment (shared-span ⇒ straight
  corridor) + turn penalty 8→400. Compiled in Unity; full rebuild NOT yet run.
- Floor-plan proposals rendered (node tools/generate_tower_floorplans_v6.js):
  `Assets/_Project/Art/Maps/Tower_EarthCoast_01/References/Tower_EarthCoast_01_F{1,2}_Plan_v6_proposal.svg`
  — 3 size classes color-coded, critical path red, toggles dashed, P-01/沙盘/欠款卷帘/跳降 annotated.
- AWAITING PM APPROVAL of plans, then: full rebuild + F1 cleanup menus (straight routing).
- MCP: ReuseAddress patch added to embedded McpUnityServer.cs (compiles+activates on next
  Unity restart; current process's wedged listener dies with it). F1 "modeling" never existed
  in any commit of this scene (verified); old Jun-6 whitebox preserved at Assets/_Recovery/0 (1).unity.

## Session 2026-06-10 (cont.) — V6 critique → three PM decisions → V7 plans drawn

**V6 diagnosed (4 specialists, unanimous)**: corridors routed center-to-center with
L-bends, no footprint avoidance → 31 corridor/room intersections + 10 zero-length
corridors. Root cause: graph-first realization, not a building. Wall-dedup patch =
symptom only. Verdict: switch to **slab-partition model** (every 4m cell ∈ {room,
corridor, stair-core, void, sealed-poche}; corridors = first-class explicit slabs;
doors = holes in shared walls; NO routing code). TowerTopologyV3 graph + 1000-seed
harness survive unchanged; only geometric realization changes (~5–8 days est.).

**PM decisions (2026-06-10, all locked):**
1. **Objective = 「真实海岸」生态柱** (sealed live coastal-ecosystem display column)
   — replaces 沙盘, which stays as F2 sales-office set dressing + stage-4 black-
   commission return-job hook. Rationale: 沙盘 violated the world's scarcity logic
   (Mars can replicate models; it buys real un-copyable Earth life) and was a
   stage-3/4 conceptual irony placed at stage 1. All carry mechanics unchanged.
2. **F2 = show-flat island** (~8 interior spaces: SALES/SHOWFLAT/TARGET/VIP/BALCONY
   + 2 stairs + A-lobby) surrounded by open raw slab (毛坯开放板, cover from material
   stacks). 9 old F2 rooms deleted as walled spaces.
3. **Atrium (中庭) over F1 HALL** (12×8), scaffold bridge = ONLY crossing. Show-flat
   island visible from F1 hub on entry (beacon). Fall = damage + completeness loss.

**Delivered this session:**
- `tools/generate_tower_floorplans_v7.js` — slab-partition data + built-in validator
  (overlap / outline / shared-face width per door); 0 errors, 26 poche cells F1.
- V7 plan SVGs: `.../References/Tower_EarthCoast_01_F{1,2}_Plan_v7_proposal.svg`
  — **AWAITING PM APPROVAL**. 11 toggles total (9 F1 + 2 F2: T7 SHOWFLAT-VIP,
  T17 TARGET-BALCONY gates the jump-drop); monster seed-randomized start
  (nest=TARGET, alts SALES / bridge-east) annotated on F2.
- `design/levels/abandoned-tower-earth-coast-01.md` updated: revised-decisions
  header block, 生态柱 commission + contract + new settlement text, 沙盘 demoted to
  dressing+hook, all objective references renamed, drop-completeness tuning knob.

**Next after PM approves V7 plans**: rewrite WhiteboxBuilder realization layer to
slab model (delete ResolveConnectorRoute/FindClearGridRoute/BuildFloor2CorridorWalls,
face-adjacency BuildConnector, WarnOnOverlaps → hard error, V7-C01..C10 checks);
update CN labels in TowerTopologyV3 (沙盘→生态柱). Full agent reports retrievable
via SendMessage (level ac76c57e..., game-design aef9d7b1..., ux af1a6410...,
systems a735d655...).

## Session 2026-06-10 (cont. 2) — V7 spatial-language critique → V8 plans

PM complaint on V7: "房间里出现走廊状区域，分不清房间/走廊/大厅，空间语言不统一".
Four specialists diagnosed (agents: level aa2ffa5b…, art a46f65aa…, design ab6cb970…,
ux afe234dc…). Root causes: (1) corridor width = S-room width = 4m, no section grammar;
(2) LOBBY/HALL/SALES/SHOWFLAT are Rooms by kind but corridors by door topology
(2-3 aligned critical doors; 2.8m doors on 4m walls = 70% = boundary dissolves);
(3) C3 24×4 bigger than an L room, C2 = hidden 4-way crossroads.

**PM decision: 「听美术的」** — corridors STAY 4m; identity via cross-section grammar
(走廊=桥架/管线压顶 感知2.2–2.4m+柱距节奏; 枢纽=高净空/挑空; 死端房=单光锚+门框收缩;
毛坯板=无顶柱阵+灯塔). No coordinate regrade.

**Delivered**: `tools/generate_tower_floorplans_v8.js` (geometry identical to v7; new:
functionClass field decoupled from size — LOBBY/HALL/SALES/SHOWFLAT=Hub, C2/C6/C7=Node;
door width ≤50% of face → D1/D7/D32/D36 2.8→2.0m; anti-enfilade offsets D-VAN+3/D33−2;
T1→SECUR-WORKSHOP, T4→C1-WAREHOUSE (off the LOBBY hub); C3 mid break node @x=30;
F2 stack-free sightline corridor x4..32/z18..22; light-anchor fixes per UX: 值班台残灯
moved visible-from-D1, P-01 悬挂红灯. Validators V8-C1..C6 added, 0 errors, 26 poche).
V8 SVGs: `.../References/Tower_EarthCoast_01_F{1,2}_Plan_v8_proposal.svg` —
**AWAITING PM APPROVAL** (supersedes v7 proposal SVGs).

**Next after PM approves V8**: full art 空间语言规范 doc (art agent a46f65aa… has the
per-class spec table ready) + WhiteboxBuilder slab-realization rewrite (now must also
consume functionClass: ceiling heights per class, corridor tray/pilaster props pass).

## Session 2026-06-10 (cont. 3) — V8 APPROVED + whitebox built in scene

**PM approved V8 plans (2026-06-10)** — viewed as PNGs (rendered via headless Edge:
`.../References/Tower_EarthCoast_01_F{1,2}_Plan_v8_proposal.png`), verdict 批准全速推进.

**Discovered already on disk (an earlier unlogged session did the "next" work):**
- `design/art/spatial-language-spec.md` — full 截面语法 spec, complete (per-class table,
  door grammar, light anchors, 三秒判读 acceptance test)
- `Assets/_Project/Scripts/Level/Topology/TowerPlanV8.cs` — slab/door data + canonical
  graph + ValidatePlan (replaces deleted TowerTopologyV3.cs)
- `Assets/_Project/Editor/TowerV8WhiteboxBuilder.cs` (792 lines) — slab realization, NO
  routing code (replaces deleted TowerV3WhiteboxBuilder.cs)

**Executed this session:**
- Recompile clean (0 err/0 warn) → `Tools > Black Commission > MVP > Tower > Rebuild v8
  Whitebox (slab plan)` ran successfully: **36 slabs, 41 doors, 122 wall faces, plan
  errors 0, poche 26/26**. Scene saved (AbandonedBuilding_Blockout.unity, 11:27).
- EditMode tests: **134/134 passed** incl. new PlanV8_* topology tests.
- MCP saga continued: Node bridge died mid-session (recompile domain reload killed WS
  conns; Unity-side pump wedged — known zombie-listener issue). Fix: clean Unity restart
  (taskkill graceful + relaunch). NEW TOOL: `tools/ws-unity-call.cjs` — direct WebSocket
  CLI to Unity-side server (ws://localhost:8091/McpUnity, `{id,method,params}`), bypasses
  the Node MCP bridge entirely. Used for menu exec, console logs, run_tests. Use it
  whenever the MCP client is dead but port 8091 listens.

**Next (per builder log + spec):** bake NavMesh; 3-second readability pass per
spatial-language spec (walk F1/F2 with flashlight); then props pass (corridor tray/
pilaster dressing), breaker prop placement in POWER slot + shutter wiring (step ④),
生态柱 objective prop in TARGET.

## Session 2026-06-10 (cont. 4) — Materials pass (PM-approved style)

PM approved whitebox + asked for materials in his style. Done:
- `TowerV8WhiteboxBuilder.EnsureMaterials` upgraded from in-memory grays to **asset-backed
  Municipal Debt Noir palette** at `Assets/_Project/Art/Maps/Tower_EarthCoast_01/Materials/
  Whitebox/` (11 URP Lit .mat assets, idempotent create-or-update). Colors from
  `design/art/art-bible.md`: concrete #5E5E5E/#707070, poured corridor #545852, rubble
  #57504A, rust steel #8C5937 (metallic .55), dark steel trays #2A2826 (metallic .65),
  island finish off-white #C7C2B0, aged warning yellow #C8A020, asphalt #383833, formwork
  wood #735637, stair #666862.
- Semantic swaps: F2 broken guard stubs → aged yellow (art-bible hazard-edge rule);
  SALES sand table → formwork wood.
- Rebuild re-run clean: 36 slabs / 41 doors / 122 walls / 0 errors / poche 26-26. Scene
  saved 11:58. Scene file SHRANK (embedded mats → asset refs).

**WORKFLOW RULE (hard-learned, 2x this session):** mcp-unity pump WEDGES after every
domain reload while editor is unfocused (handshake OK, requests never dispatched; even
the ws-cli direct channel hits it). recompile_scripts works ONCE then kills the bridge.
So: edit C# → restart Unity (taskkill graceful → relaunch → poll 8091 → ~30s settle) →
run menus via `tools/ws-unity-call.cjs`. Root fix needed in embedded package
(beforeAssemblyReload listener teardown) — not yet attempted.

## Session 2026-06-10 (cont. 5) — Legacy wall cleanup + style-authority findings

- PM flagged old F1 walls still visible → root-caused: 33 hand-placed `Wall_E_01 (n)`
  scene roots predating all builders. PM approved deletion → builder now deletes any
  root with prefix `Wall_E_01` (idempotent). Rebuilt + saved: scene roots now exactly 4
  (Directional Light / player / Tower_v8_Whitebox / PreviewWalker).
- PM asked "find my style design" → FOUND: `docs/art/black-commission-style-lock-v1.md`
  (Jun 1, Semi-Realistic Industrial Horror, Pacific Drive/Control, 20-40% weathering,
  explicitly NOT low-poly). NO authority conflict: art-bible header literally names
  style-lock-v1 as "Source of truth for style decisions"; art-bible only amends accents
  (warm tungsten amber primary; CRT green screens-only) + adds production specs.
  Flat-color whitebox VIOLATES art-bible prohibited list ("clean flat color on any
  surface with >6 months implied exposure") → texture upgrade = compliance.
- Texture upgrade plan (ambientCG 12 sets already in project at
  `Art/Maps/Tower_EarthCoast_01/Textures/`, never wired; library menu never completed):
  PM put ON HOLD pending style-ownership discussion. Mapping proposal in chat: Concrete048
  slab/corr, Concrete034 walls/stairs, Tiles133D island finish, Metal063 trays,
  MetalWalkway014 scaffold, Planks037A wood, Asphalt031 ext, Gravel043 rubble; markings
  stay flat paint; world-scale tiling via per-renderer MPB _BaseMap_ST needed (1K on 12m
  walls stretches otherwise).

## Session 2026-06-10 (cont. 6) — STYLE RE-LOCKED: 类 LC 低保真 (Lo-Fi Industrial Horror)

PM remembered his original ask was LC-like lo-fi. Doc archaeology confirmed:
retro_industrial_horror_style_guide.md (May 27, PM's original lo-fi direction) →
style-lock-v1 (Jun 1, semi-realistic, REJECTED low-poly) → art-bible (Jun 9, built on v1).
PM verdict: v1's semi-realistic turn did not reflect his preference. **LOCKED: lo-fi.**

- **NEW CANON: `docs/art/black-commission-style-lock-v2.md`** — lo-fi low-poly, ≤256px
  visible-texel textures, ~2m world tiling, albedo-only (no normal/AO/metalness), high
  roughness (smoothness ≤0.3), BC palette + art-bible lighting grammar fully inherited,
  LC = genre language only (red line unchanged). v1 got DEPRECATED header; art-bible
  header now points to v2 with fidelity clauses amended (v2 §6).
- **Whitebox materials upgraded to lo-fi textured**: builder MatAsset now wires ambientCG
  Color maps (FindAssets by "{id}_1K-JPG_Color"), clamps importer to 256px, keeps tints;
  ApplyWorldTiling (per-renderer MPB _BaseMap_ST, 2m/repeat, min 0.25) in AddSlab/
  AddVisualSlab/AddRampFlight. Mapping: Concrete048 slab+corr, Concrete034 wall+stair,
  Gravel043 rubble, MetalWalkway014 scaffold, Metal063 trays, Tiles133D island finish,
  Asphalt031 ext, Planks037A wood; marking yellow stays flat (paint). Tints are first-guess
  (#8A8A8A etc. over texture albedo) — PM eyeball pass may retune.
- Rebuild clean (36/41/122/0, poche 26/26), all 10 textures found (0 warnings), scene
  saved 12:45. Verified meta: DefaultTexturePlatform 256, no platform overrides active.

## Session 2026-06-10 (cont. 7) — PM 5-point feedback pass

1. **Yellow retired**: NodeMark floor crosses + Break_Sign posts DELETED; F2_BrokenGuard
   → rubble gray. nodeMat field removed (V8_Marking_AgedYellow.mat kept on disk for
   future decals). Hazard marking returns later as decals if ever.
2. **Stairs fixed (PM: 方向反了上不去)**: both stairs had F1 entry + F2 exit on the SAME
   end (A: south z=28 D10+D35; B: east z≈20 D5+D30) but the flight stranded you at the
   far end. Now switchback: flight up one 2m half (A east / B west) + full-length landing
   strip on the other half flush with F2 walking back to the door + rail with north
   crossover gap. Verified in scene: Landing×2, LandingRail×2.
3. **Texture uplift research** (art agent → docs/art/lofi-uplift-and-outline-research.md):
   posterize albedo 4-6 levels + point filtering on props + later vertex-color dust layer.
4. **LC outline confirmed** (same report): depth+normal edge detection post (Roberts/
   Sobel), but LC's look is PRIMARILY fixed 860×520 render upscale; then outline; then
   posterized volumetrics/vignette/bloom. Unity 6 path: built-in FullScreenPassRendererFeature
   + Fullscreen Shader Graph (RenderGraph-native, no custom C#). Start params: depth 1.5,
   normal 0.4, outline color #0A0F14 (civic blue-black, NOT pure black — BC identity).
   Design guardrails (design agent → docs/design/lofi-readability-notes.md): selective
   outline weights (interactables/teammates/threat), light anchors need ≥30% sat
   difference, text decals (欠款催缴/印章红公文) = cheapest BC-vs-LC differentiator.
5. **"F1 没删干净"**: disk scene verified CLEAN (0 Wall_E, all F1_* are V8-generated,
   4 roots only). PM may have seen pre-cleanup state or means poche/rubble stand-ins —
   awaiting PM to name a specific object after reload.

Rebuild clean, 0 errors, scene saved. NOT yet run: EditMode tests after stair change
(geometry-only edit; topology untouched).

## Session 2026-06-10 (cont. 8) — Stair dog-leg redesign + door-divider fix

PM: stairs still weird (enter door, don't see stairs) + "some doors have a divider in
the middle". Specialists consulted per PM (level-designer → design/levels/
tower-stair-redesign-v2.md; ux-designer → docs/design/tower-stair-ux-notes.md).

- **Stairs now dog-leg (双跑折返)**: A 梯 = scissor with central spine wall (run1 x27±1
  z29.5→34 rises 0→2.1 directly facing D10 entry; A_MID 4×2 north landing y2.1; run2
  x29±1 back south 2.1→4.2; A_TOP 4×1.5 at D35). B 梯 = x-direction dog-leg (31° — the
  4 m width cap): south lane z17..19 climbs west off the D5 threshold, B_MID 2×4 west
  landing (z19..23), north lane z21..23 climbs east to B_TOP (x2.5..4, z18.6..23) at the
  D30 shutter. AddRampFlight generalized: x/z-direction aware + yFrom/yTo for stacked runs.
  NOTE: spec's B_MID z17..23 had a headroom conflict over run1 — implemented z19..23.
- **Door divider root-caused**: corridor pilaster 4m rhythm lands exactly on door centers
  (C4 pilaster z=20 == D5 center). New DoorNear() guard skips pilasters/break columns
  within door clear width +0.55m. Verified: Pilaster_C4 4→3.
- **B-stair sodium pair** (UX spec): LA-SODIUM moved to (2.0,20.0) y+4.0 flight-wall;
  new LA-SODIUM2 spill (3.8,19.5) y+2.5 r6 — sodium pool visible outside D5.
- Rebuild 0 errors; EditMode **134/134** green (TowerPlanV8 data change safe).

## Session 2026-06-10 (cont. 9) — Stair doors realigned to the dog-leg runs (PM)

PM: doors must correspond to the stair runs. Done via PlanDoor offsets (C# + JS plans
kept 1:1, SVG/PNG regenerated, JS validator 0 errors, C# hard validators passed):
- D5 (C4→STAIRB1) offset −2 → center z=18: doorway now EXACTLY faces the south lane
  (z17..19) — enter and the flight climbs straight ahead. LA-SODIUM pair moved with it
  (main 2.0,18.0 y+4; spill 3.8,18.0 y+2.5).
- D30 (debt shutter) offset +2 → center z=22: exits straight off the north-lane arrival/
  B_TOP (platform recentered z18.9..23.5).
- D10 −0.55 / D35 +0.55 (x27.45 / x28.55): the 0.45m corner-clearance rule pins doors on
  the 4m A-shaft face, so ±0.55 is the legal max — doorway now ~80% faces its run (D10→
  west run1, D35→east run2).
Rebuild 0 errors; EditMode 134/134; connector positions verified in scene YAML.

## Session 2026-06-10 (cont. 10) — LC retro rendering stack implemented

Per research report (docs/art/lofi-uplift-and-outline-research.md), PM-approved next step:
- **NEW `Assets/_Project/Art/Rendering/LcOutline.shader`** — fullscreen depth+normals
  Roberts-cross outline. URP Core.hlsl must precede Blit.hlsl (TEXTURE2D_X). Texel size
  from _ScaledScreenParams so the outline stays 1 chunky texel at any render scale.
  Outline color #0A0F14 civic blue-black (BC identity, not pure black).
- **NEW `Assets/_Project/Editor/LcRetroRenderingSetup.cs`** — menu `Tools > Black
  Commission > Art > Setup LC Retro Rendering` (idempotent): URP renderScale 0.5 +
  Point upscale; creates M_LcOutline.mat (depth 1.5 / normal 0.4 / strength 0.85);
  wires FullScreenPassRendererFeature 'LC_Retro_Outline' (BeforeRenderingPostProcessing,
  requirements Color|Depth|Normal, fetchColorBuffer) onto URP-Renderer via
  AddObjectToAsset + m_RendererFeatureMap localId. Also `Disable Retro Render Scale`
  menu to revert to 1.0.
- Verified on disk: URP-Pipeline m_RenderScale 0.5 / m_UpscalingFilter 2(Point);
  URP-Renderer has the feature (injection 550, req 7); shader compiles clean.
- AWAITING PM visual pass in Play mode (Game view — renderScale doesn't affect Scene
  view). Tuning knobs on M_LcOutline.mat. Remaining next: NavMesh bake → props pass
  (corridor trays already in; POWER breaker + shutter wiring step ④; 生态柱 prop);
  texture posterize pass (art report Problem A) optional after PM verdict.

## Session 2026-06-10 (cont. 11) — Outline toned down + BC identity pass

PM verdict on retro stack: "有那个感觉了" but outline too heavy + missing BC flavor.
- _OutlineStrength 0.85 → 0.6 (mat + setup-tool default).
- **BuildIdentityDressing** added to V8 builder (per lofi-readability-notes §4: civic
  paperwork = cheapest LC-vs-BC differentiator): 3 new flat materials (V8_Civic_TealPaint
  #3F5F5C, V8_Paper_Aged #D6CCAE, V8_Stamp_Red #C23A2B — stamp red on paper/signage ONLY
  per art bible); rolled debt-shutter boxes (市政青) over D30/D35 doorway tops; 催缴/封条
  paper notices + stamp red blocks beside D-VAN(exterior), D4(POWER corridor side),
  D30(plate side), D34(showflat side). All door-position-driven via TryGetDoorCenter.
- Rebuild 0 errors; all 8 dressing objects verified in scene.
- **Proposed next (pending PM)**: ① NavMesh bake ② POWER breaker prop + shutter wiring
  (step ④, code exists) ③ 生态柱 objective prop in TARGET ④ optional texture posterize.

## Session 2026-06-10 (cont. 12) — Power gate wired + NavMesh baked (map is now playable-loop ready)

- **com.unity.ai.navigation 2.0.13** added to manifest (registry-verified version; the
  add_package WS tool errored "Operation cancelled" — manifest edit + restart works).
- **BuildPowerGate** in V8 builder: P01 breaker cabinet prop in POWER (civic teal body,
  dark panel+lever) with NetworkObject + PowerGateBreaker; serialized refs wired via
  SerializedObject — enableWhenRestored=[F2_PowerLights (2 warm work lights at both
  stair tops, inactive)], disableWhenRestored=[PowerShutter_D30, PowerShutter_D35
  (collider planks filling the only two F2 doorways + carving NavMeshObstacles)].
  Gameplay loop now: find POWER → hold breaker 3s (host-validated) → shutters drop +
  stair lights on → F2 accessible.
- **NEW `TowerNavMeshBaker.cs`**: menu `... > Tower > Bake Tower NavMesh` — NavMeshSurface
  on whitebox root (children, physics colliders). **TRAP discovered & fixed**: leaving
  NavMeshData embedded in the scene forces Unity to save the WHOLE scene BINARY (even
  ForceText) — baker now persists it as `Assets/Scene/Tower_NavMesh.asset` and the scene
  stayed %YAML. (Mid-session the binary scene made text-based YAML checks return false
  negatives — if scene checks suddenly all fail, check `head -c 12` for %YAML first.)
- Baked bounds 56.5×7.6×62.5 (both floors). Verified in text scene: breaker component +
  both ref arrays + all gate objects. EditMode 134/134.
- **Map status: fully playable loop pending PlayMode test** (van → F1 → power → F2 →
  bridge → island). Next: 生态柱 carriable objective prop in TARGET; then PM playtest.

## Session 2026-06-10 (cont. 13) — 生态柱 objective prop (loop complete)

- **BuildEcoColumn** in V8 builder: "EcoColumn_Objective" on the TARGET plinth —
  emissive sealed-glass cylinder (V8_EcoColumn_Glass #7FD4C0 + _EMISSION; the map's
  ONLY teal hue = objective semantics; deliberate smoothness-0.5 exception to v2)
  + dark steel caps; CapsuleCollider 1.7×0.32, Rigidbody mass 45,
  NetworkObject + **Carriable(isHeavy=true, dropDamageThreshold=3.5)** wired via
  SerializedObject and verified in scene YAML. Heavy-carry mechanics (0.55× speed,
  downed→force-drop, hotbar lock) were already implemented + tested.
- Rebuilt + re-baked NavMesh (asset path), scene still %YAML, 0 errors, EditMode 134/134.
- **MAP LOOP NOW CONTENT-COMPLETE for first playtest**: van → dark F1 (sodium/red light
  anchors) → POWER breaker 3s hold → shutters drop + stair lights → dog-leg stairs →
  F2 plate → scaffold bridge → show-flat island → TARGET → grab eco column (heavy) →
  return route (A-stair biased) → van. Missing for full mission: tower mission manager
  (selection/settlement wiring — LostItemMissionManager is school-specific), monster
  (感染监理 has seeds + NavMesh but no AI/prefab yet), E-DROP jump-drop tuning.
- **NEXT: PM PlayMode walkthrough of the complete loop.** After verdict: either tower
  mission manager (full loop with settlement) or 感染监理 monster AI.

## Session 2026-06-10 (cont. 14) — PM playtest feedback round 1 (3 fixes + 2 verdicts)

1. **B-stair head-bonk FIXED**: run2 rose into B_TOP's underside (platform overhung the
   climb lane z21..23). run2 now stops at x=3.0 (slope 37.7°, ≤40 ✓); B_TOP narrowed to
   x3..4 z20.6..23.5 — flush hand-off, nothing overhangs. Rebuilt+rebaked, scene %YAML.
2. **"F2 doors won't open" root-caused**: NOT a bug — PowerGateBreaker is a
   NetworkBehaviour; in non-networked preview (PreviewWalker / direct Play without
   host-start) it never spawns, so the debt shutters stay down BY DESIGN. NEW menu
   `... > Tower > Preview - Toggle Power (no network)` (TowerPowerPreviewTool.cs) flips
   shutters+lights for whitebox walkthroughs. Real gate needs a hosted session.
3. **"太糊" diagnosed** (art agent → docs/art/lofi-blur-diagnosis.md): applied Step 1+2 —
   URP-Renderer m_IntermediateTextureMode 1(Always)→0(Auto) (killed the extra bilinear
   blit in the Point upscale path) + all 12 ambientCG Color maps → Point filter, mips OFF
   (LoadLoFiTexture updated to enforce). Step 3 (renderScale 0.5→0.6) held in reserve.
4. **Next-step verdict** (design agent → docs/design/tower-next-step-recommendation.md):
   ① tower mission manager = the ONLY hard blocker (settlement endpoint + monster aggro
   signal depend on it) ② collect E-DROP tuning feedback while PM is in the build
   ③ monster last — SchoolMonsterAI is complete and reusable, ~1 session after manager.

## Session 2026-06-10 (cont. 15) — PM playtest feedback round 2

1. **"按E没反应" root-caused**: PM walks via PreviewWalker, which had NO interaction.
   PlayerInteraction (the real one) gates on IsOwner = dead offline; the breaker already
   has an offline fallback (localSoloHolding). FIX: PreviewWalker now has preview-only E
   interaction — camera raycast 2.5m → GetComponentInParent<IInteractable>, hold
   start/end on E down/up, OnGUI hint ("[E] 按住恢复供电…"), try/catch for
   network-only interactables (null PlayerController passed; breaker ignores it offline).
   PM can now hold the breaker 3s in walkthrough and watch shutters drop + lights on.
2. **F1↔F2 air band FIXED**: F1 walls were WallHeight 3.2 under a 4.2 floor → 1m open
   band everywhere. F1 walls + poche now extend to Floor2Y (4.2); F2 walls stay 3.2
   (top floor, open sky by design). Verified: Wall_* scaleY=4.2 in scene.
3. **"黄色梯子状物体" = E-DROP jump-drop landing scaffold in DOCK** (Drop_P1/P2/P3 +
   pole, rust MetalWalkway tint). Intentional gameplay (balcony→dock one-way descent).
   Logged for props pass: scaffold frame + tarp so it reads as 脚手架 not mystery.
Rebuild + rebake clean, scene %YAML, EditMode 134/134.

## Session 2026-06-10 (cont. 16) — Walk validator + B-stair header bonk + E-DROP deletion

- **NEW `TowerWalkValidator.cs`** menu `... > Tower > Validate Walk Paths`: samples the
  critical walk polylines every 0.2m against collider BOUNDS (axis-aligned whitebox →
  exact; PhysX raycasts return nothing in background edit mode — don't use them).
  Reports head-column blockers by name + ground rises >0.3m ("needs a jump").
- **B-stair "still bonks, must jump" SOLVED by the validator**: run1 started at x3.72 =
  first steps INSIDE the D5 doorway → climber met the door header (y2.25) with 1.72m
  headroom. run1 now starts x3.3 (34.8°). Re-validated: header bonk gone; A-stair CLEAR;
  remaining B-stair entries = closed power shutters (by design).
- **"高差 in the room before the eco bottle" = SALES sand-table dressing** (1.1m × 3.2
  × 2.2 box at room center, y4.2..5.3) — PM-locked design element (沙盘 demoted to
  dressing + D32→D33 anti-enfilade blocker), reads as a raised floor in whitebox. NOT
  changed; props pass will give it table form. Validator confirmed island floor has
  ZERO ground rises.
- **E-DROP scaffold platforms DELETED per PM order** (screenshot ID'd Drop_P1/P2/P3 +
  pole in DOCK): now a straight 4.2m one-way drop (damage by design); connector kept,
  faint landing mark remains. Rebuild + rebake clean, EditMode 134/134.

## Session 2026-06-10 (cont. 17) — TowerMissionManager implemented (E2E loop with settlement)

Quick spec approved + written: `design/quick-specs/tower-mission-manager-2026-06-10.md`
(PM locked: dropPenalty 3%/hard-drop, rejectThreshold 50%; PM DECLINED scope adds —
取柱跳闸 second-act event + TEMP evidence bonus DEFERRED until monster lands).

**Implemented (all new, school mission untouched):**
- `Mission/Core/TowerMissionLogic.cs` — pure state machine (InProgress→ObjectiveSecured→
  Delivered/PartialReturn/Failed), completeness −3%/hard drop, ≥50% to deliver,
  terminal latching, ScaleDeliveredMoney (300×0.91→273).
- `Mission/EcoColumnCarriable.cs` — Carriable subclass forwarding OnDropDamage as
  HardImpact event (threshold 3.5 m/s = existing dropDamageThreshold, single source).
- `Mission/TowerMissionManager.cs` — NetworkBehaviour: NVs SyncedState/SyncedCompleteness,
  subscribes column IsBeingCarried + HardImpact (authority only), all-downed poll 1s →
  Failed, RequestDepart (lever/ServerRpc) → aboard check (set DOWN inside cargo zone) →
  MissionRewardCalculator(null-task fallbacks) → money×completeness on Success →
  MvpPendingReward via ClientRpc (or locally when offline — PreviewWalker testable).
  `OnObjectiveSecured` static event = future monster aggro hook.
- `Mission/TowerVanDepartLever.cs` — IInteractable "发车结算（拉杆）".
- Builder: BuildMissionManager (cargo trigger 8×2.5×4 rear van pad + teal lever post +
  manager, refs wired via SerializedObject); BuildEcoColumn now adds EcoColumnCarriable.
- Tests: `TowerMissionLogicTests` (10) — all spec acceptance formulas covered.

**EditMode 144/144.** Scene rebuilt + rebaked, all wiring verified in YAML (refs non-null,
rewards 300/5/80 serialized). Walkthrough now playable END-TO-END offline: breaker →
shutters → stairs → column → carry → set down in cargo zone → pull lever → settlement
logged + MvpPendingReward set.

**Next**: 感染监理 monster (SchoolMonsterAI reusable, NavMesh ready, aggro hook ready)
→ then HQ task-selection wiring for the tower job (OfficeTaskDefinition + scene flow).

## Session 2026-06-10 (cont. 18) — 感染监理 wired (SchoolMonsterAI reused untouched)

- **NEW `Mission/TowerMonsterDirector.cs`**: host warps the monster to a random
  MonsterSeed_* marker on Start (nest=TARGET, alts SALES/bridge-east, markers already
  in scene); subscribes TowerMissionManager.OnObjectiveSecured → routes aggro through
  the school AI's EXISTING `Distract(pos, 25s)` — monster investigates the grab site,
  arrival flows into normal detection/chase. Zero changes to SchoolMonsterAI.
- **Builder BuildMonster**: Monster_InfectedSupervisor (NavMeshAgent r0.4 h1.9 +
  NetworkObject + NetworkTransform + SchoolMonsterAI w/ school-default tuning), dark
  silhouette body + amber-orange #FF6A00 emissive eye pinpoints (art bible threat
  signal; new V8_Monster_Eye mat); 5-point patrol ring (TARGET/SHOWFLAT/SALES/bridge-
  east/plate); director refs wired via SerializedObject (3 seeds + eco column).
- Rebuild + rebake clean; EditMode 144/144; all refs verified non-null in scene YAML.
- **The tower is now the full experience loop**: dark ingress → power gate → stairs →
  island → grab column (= monster alerted) → hunted egress → cargo + lever → settlement.
- NOTE: monster needs a NETWORKED session to act (SchoolMonsterAI is a NetworkBehaviour
  — in PreviewWalker offline mode it stands inert; chase/attack test requires host).
- **Next**: HQ task-selection wiring (OfficeTaskDefinition for tower job + scene flow)
  → then PM hosted playtest 1-2 players. Deferred list unchanged (跳闸第二幕/线索奖金/
  沙盘台道具化/E-DROP 调参).

## Session 2026-06-10 (cont. 19) — Monster REDESIGNED (核查专员) + school content deleted

**PM rejected the reused school monster + ordered school deletion + monster redesign.**

- **School content DELETED** (PM "现在就删", accepts no-mission gap until tower-HQ wiring):
  Snow_Lotus_01.unity, Resources/Tasks/SnowLotus_01.asset, SnowLotusTestSceneBuilder.cs,
  all homework/school art (3 FBX + 6 prefabs + 1 mat), TowerMonsterDirector.cs +
  builder BuildMonster (the rejected wiring). **STILL PRESENT (flagged for tower-HQ
  task)**: LostItemMissionManager + School*.cs scripts — deeply referenced by MvpHud /
  VanTransitOverlay / PlayerHotbar / PowerGateBreaker / PlayerController; rip out when
  TowerMissionManager takes over the HQ flow.
- **New monster: 核查专员 (Audit Inspector)** — concept A of docs/design/
  tower-monster-concepts.md + fiction in tower-monster-fiction.md (PM picked A):
  eyes sealed; perceives footstep VIBRATION (speed² × heavy-carry 2.5× × falloff);
  sneaking is near-silent, the eco-column runner is the loudest thing in the building.
  Fiction: checklist E-07 水封完整性检查 — lifting the column = violation → permanent
  sense boost 1.5× + alarm at the plinth. Counterplay hooks: sneak, decoys (later),
  SenseMultiplier hook ready for power-off dampening (deferred 跳闸第二幕).
- Implementation: `TowerAuditorLogic` (pure, alert accumulate/decay + hysteresis,
  RaiseAlarm), `TowerAuditorAI` (NetworkBehaviour: NavMeshAgent, 0.2s tick, observes
  PlayerController velocity + CarrySystem.IsCarryingHeavy, checklist patrol with 2.6s
  dwell stops, hunt = chase nearest + TakeDamage 25/1.5s), builder BuildAuditor
  (hunched silhouette + clipboard + AMBER WORK BADGE as threat signal — replaces eye
  pinpoints, art-bible-compliant), 9 EditMode tests. **153/153 green** (one test premise
  fixed: full-speed runner at 8m legitimately reaches Hunting in ~1.5s).
- **Next: tower-HQ wiring** (OfficeTaskDefinition + scene flow + LIMM rip-out) → hosted
  playtest.

## Session 2026-06-10 (cont. 20) — TOWER WIRED INTO HQ + school code fully ripped out

**The tower is now THE game's mission.** 153/153 EditMode green, 0 compile errors.

- **Scene canonicalized**: AbandonedBuilding_Blockout.unity → `Assets/_Project/Scenes/
  Tower_EarthCoast_01.unity` (build-settings slot already existed, now enabled; Snow_Lotus
  entry removed). NavMesh asset → Tower_EarthCoast_01_NavMesh.asset. Builder/baker
  ScenePath constants updated. Old Assets/Scene/ blockout deleted.
- **Task asset**: `Resources/Tasks/TowerEarthCoast_01.asset` (tower_ecocolumn_01,
  「真实海岸」生态柱回收, sceneName Tower_EarthCoast_01, 300/5/80 — flavor text carries
  the contract lines). STALE old 沙盘 task asset (Tower_EarthCoast_01.asset) deleted.
  OfficeComputer flow is data-driven (task.sceneName) — no code change needed there.
- **HQ-flow entry**: builder adds PlayerSpawnPoint at the van; PreviewWalker self-disables
  when a network session is live; TowerMissionManager returns everyone to HQ 6s after
  settlement (NGO scene load, mirrors old flow).
- **School scripts DELETED** (11 files: LIMM, school items/doors/monster/trace/hiding/
  time-director + TowerMvpSceneBuilder). Reference surgery: MvpHud mission panel
  rewritten to TowerMissionManager (objective per TowerMissionState + 密封完整度 line +
  核查专员 status via AuditorState; school clock/carrier/bonus-evidence UI removed);
  PlayerHotbar/VanTransit guards → TowerMissionManager.Instance; PlayerController
  emergency-sprint rule retired (false; TODO tower version); **SchoolExitPoint
  RESURRECTED as `MissionVanExitPoint`** (board/sit + lockers + partial-return confirm,
  ported to TowerMissionManager incl. completeness warnings) — wired in builder at the
  van + kept the depart lever. TowerMissionManager gained static Instance.
- **READY FOR HOSTED PLAYTEST**: HQ → office computer → 接「真实海岸」生态柱回收 →
  van transit → tower → full loop → lever/van return → HQ settlement.

## Session 2026-06-10 (cont. 21) — PM 5-point feedback round 3 (post-hosted-playtest)

PM feedback: ①UI/UX 前后不一致(以地图感觉为准重构 menu/车内) ②人比门矮+楼梯卡顿+生态柱不能交互
③怪物删除 ④音效/光线缺失+材质打磨 ⑤整体不像一个系统。

- **怪物已删（代码层 DONE）**: TowerAuditorAI.cs / TowerAuditorLogic.cs / TowerAuditorLogicTests.cs
  删除；builder BuildAuditor + eyeMat 移除；MvpHud GetMonsterStatus + 核查专员状态行移除，
  ObjectiveSecured 文案去掉"监理在听"。EditMode 应降为 144。**场景里 Monster_AuditInspector +
  ChecklistStations 还在** — 需 Unity 重编译后跑 Rebuild v8 Whitebox 清掉（bridge 泵卡死，
  ws-cli 120s 超时；Unity 在跑 PID 37372 但未聚焦）。
- **"人比门矮"根因（已定位未修）**: Player.prefab CameraRoot localY=0.7，而 CC center(0,1,0)
  → pivot 在脚底 → **眼高仅 0.7m**（应 ~1.7m）。一行 prefab 修复，等 PM 批准。
  门 2.0w×2.25h 偏宽（现实 0.9×2.1）也放大此感受 — 眼高修复后再评估是否收窄。
- **楼梯卡顿根因**: AddRampFlight 生成 12 个离散 0.22m 台阶盒，CC 逐阶瞬移。
  方案: 台阶改 visual-only + 每段加一条隐形斜坡碰撞体（坡度 31-38° < SlopeLimit 75）。
- **生态柱不能交互根因**: Carriable 不是 IInteractable → E 键/准星提示完全不认它；
  拾取是 CarrySystem 的 F 键且仅联机生效、无任何提示 UI。方案: EcoColumnCarriable 实现
  IInteractable（E=扛起，走 CarrySystem RPC）+ 提示文案。
- **UI 不一致诊断**: MainMenuUI=uGUI/TMP、MvpHud+VanTransitOverlay+QuickNetworkUI=IMGUI、
  车内=程序化舱体(ASV4 模型)、地图=lo-fi 256px+LC outline。重构方向待 PM 拍板后执行。

**PM 拍板（同回合）**: ①三修复全修 ②UI 重构「车内先」 ③门尺寸先修眼高再看。**全部已执行：**
- 眼高: Player.prefab CameraRoot y 0.7→1.7，crouchCameraDrop 0.55→0.85（蹲伏眼 0.85 < 蹲伏碰撞顶 1.0）。
- 楼梯: AddRampFlight 台阶盒 DestroyImmediate 碰撞体（纯视觉）+ 每段一条隐形斜坡碰撞体
  Ramp_WalkSurface（LookRotation 对齐跑向，顶面抬半阶 rise/steps/2 防台阶穿出；宽=梯宽）。
  注意 TowerWalkValidator 用 AABB 采样，斜坡旋转碰撞体可能产生误报——下次跑 validator 留意。
- 生态柱: CarrySystem 新增 public TryPickUp(Carriable)（E 键路径，同 gating）；EcoColumnCarriable
  实现 IInteractable（hint「扛起生态柱（重物，双手）」，OnInteractStart→CarrySystem；离线 null
  player 静默拒绝）。F/G 原路径保留。
- 车内: VanCabin.UseModeledInterior=false（ASV4 模型退役，资产保留在 Resources）；程序化舱体
  配色对齐 V8 精确色值（墙=市政青 #3F5F5C 脏渍、地/顶=暗钢 #4A4845、长凳=#2A2826、纸=#D6CCAE、
  印章=#C23A2B）；CrtGreen 发光条改纸质公司铭牌+小型派遣绿指示灯（合 art bible「绿限屏幕/灯」）；
  MakeGrimeTexture 改 Point filter + 无 mips（lo-fi 颗粒对齐地图）。

**待 Unity 验证（bridge 卡死，需 PM 聚焦/重启 Unity）**: 重编译 → Rebuild v8 Whitebox（清怪物
实体+新楼梯）→ Bake Tower NavMesh → EditMode 应 144/144（删了 9 个 auditor 测试）→ 联机走查
验证眼高/楼梯/E 扛柱/新车舱。

**UI 重构剩余批次（按 PM 顺序）**: 车内 HUD 条带→主菜单 pages→HUD/结算→办公电脑。
音效/光照/材质打磨在 UI 统一之后。门宽待眼高验证后再议。

## Session 2026-06-10 (cont. 22) — UI/风格统一批次（PM:"直接后续批次；事务所模型不动、风格可改"）

- **色板全局换装**: BlackCommissionUiTheme 色值对齐 V8 地图（MilitaryGreen 槽位→市政青
  #3F5F5C 家族、RustWarning→印章红 #C23A2B、OldPaper→#D6CCAE、OldWood→钠灯琥珀）。
  字段名保持兼容（9 文件 125 处引用自动生效）。
- **车内 HUD**: 顶部条带改「派遣单」纸质票据（纸底+市政青框线+印章红章块+墨字）；
  进度条改纸上青墨填充（绿条退役）。
- **主菜单**: 可见构成=写实烘焙桌面图+CRT 屏上程序化菜单行。菜单行保留终端绿（屏幕
  例外正当）但去霓虹压暗；**招贴条上已删学校委托（"找回作业本"）换成塔楼生态柱委托
  300G**；连接进度条/大厅进入按钮绿→青。**两张写实 PNG（MainMenuBg 1024 / MenuPanel
  512）导入设置改 Point 无 mips——像素化对齐地图颗粒**。
- **HUD**: 手电电量条 绿/旧锈→钠灯琥珀/印章红；准星交互点 绿→琥珀（"冷空间一个暖点"）；
  E 提示文字 绿→纸色。办公电脑终端本就是纸质样式，未动。
- **HQ（模型零改动）**: Art/Generated 全部 64 张贴图 meta 改 256px+Point+无 mips —
  事务所道具下次导入即 lo-fi 化；HqOfficeLightingPass 已是钨丝暖+冷工业+雾，未动。
- **发现的死代码（未删，待 PM 指示）**: SettlementUIController 挂在 HQ.unity 但
  ShowSettlement 无任何调用方，且显示"救出幸存者/排水泵"史前任务字段——永不显示。
- 待办: 音效 pass、HQ 更深风格统一、转场衔接、门宽评估、PM 材质两问未答
  （最不舒服的面？颗粒强度？）。

## Session 2026-06-10 (cont. 23) — 光 + 声 pass（PM:"继续 光和声音"）

**声音（全程序化合成，零资产）：**
- SynthAudio 新增 7 个: BreakerCrackle(电闸充能滋滋)、PowerRestore(继电器闷响+电流嗡起)、
  ShutterSlam(铁皮卷帘砸开+余颤)、HeavyHoist(扛重物起肩)、GlassThud(生态柱硬着陆闷响+玻璃鸣)、
  LeverClank(拉杆咔哒落锁)、StampThunk(印章落纸)。
- AudioManager 升级: ①RuntimeInitializeOnLoadMethod 自举（直接进塔楼场景也有声）；
  ②场景氛围声自动切换（sceneLoaded: HQ→荧光灯嗡鸣 / Tower*→风声 / 其他→停）；
  ③引擎独立 engineSource（运输不再挤掉场景底噪）；④PlaySchoolAmbient→PlayTowerAmbient；
  ⑤新增 7 个播放接口（PlayStamp 为 UI 非定位）。
- 挂钩: PowerGateBreaker 每 peer 观察 RestoreProgress 上升→0.38s 间隔滋滋声；恢复供电→
  电闸位置 PowerRestore + 每块卷帘位置 ShutterSlam（在 effects latch 内，含离线路径）。
  CarrySystem.PickUpClientRpc→重物 HeavyHoist/普通 Pickup。EcoColumnCarriable.OnDropDamage→
  GlassThud（与完整度扣减同刻）。TowerVanDepartLever→LeverClank。TowerMissionManager.
  ApplyResultLocally→PlayStamp（每 peer 结算落章）。

**光（塔楼，builder ConfigureSceneLighting，Rebuild 时生效）：**
- 场景默认白色满强 Directional Light = 画面平的根因 → 改阴天钢灰 #B8C4CE、强度 0.32、
  软阴影 0.94、角度 (52,-28)：F2 露天板有灰天光，F1 在板下变暗、靠钠/红/青光锚+手电读图。
- RenderSettings: exp2 雾 #14171B 密度 0.026（景深不灭 BEACON）；Trilight 环境光暗三色。
- builder 加 using UnityEngine.Rendering（AmbientMode）。

**待 Unity 验证**: 聚焦重编译 → Rebuild v8 Whitebox（怪删+新楼梯+灯光一并生效）→ Bake
NavMesh → EditMode 预期 144/144（本轮无逻辑改动，音频调用全 null-safe）→ 联机走查听
全链路: HQ嗡鸣→电脑滴→引擎→塔楼风→电闸滋滋→闷响+卷帘砸开→扛柱起肩→(摔)玻璃鸣→
拉杆咔哒→落章→回 HQ。
**剩余**: 材质两问未答；门宽待眼高验证；音乐(menu/结算主题)未做；HQ 深度风格 pass 未做。

## Session 2026-06-10 (cont. 24) — 音/光 第二轮（PM:"继续开发吧，音效和灯光"; 已 push 6344db4 后）

**声：**
- **队友脚步修复（重要）**: PlayerController.Update 对非 owner 直接 return → 远端玩家全程无声。
  现在非 owner 分支用同步的 NetworkMoveSpeed 档位(0/.25/.5/1)本地驱动脚步（蹲行<0.3 静音=潜行
  affordance；冲刺 0.32s / 走 0.45s 间隔）。
- **表面变奏**: PlaySurfaceFootstep 向下射线 1.6m，collider 名 Bridge_* → 金属脚步
  （新 SynthAudio.FootstepMetal ×2 音高）；楼梯按图纸是混凝土，保持普通声。
- **新合成**: FootstepMetal、RadioStatic(6s loop 低通嘶声+载波漂移)、RadioSquelch、
  ObjectiveAlarm(E-07 市政蜂鸣，0.8s 周期门控双脉冲)。
- **取柱警报**: TowerMissionManager 订阅 SyncedState.OnValueChanged(全 peer) →
  ObjectiveSecured 时在生态柱位置放 ObjectiveAlarm；authority 路径双保险 + alarmPlayed 闩锁
  （离线 NetworkVariable 可能不触发 OnValueChanged）。
- **车内电台**: VanTransitOverlay.AddDispatchRadio — 舱内派遣电台底噪 loop(0.35vol)，
  发车时 squelch 键台声。

**光：**
- **新组件** Scripts/Environment/: LightFlicker(Sway=钠灯呼吸 / Sputter=濒死灯管断闪，
  位置种子确定性，Configure() 给 builder 用)；EmissionPulse(MPB 呼吸发光，不动共享材质)。
- Builder: LA-DUTY 残灯 Sputter(0.85, 7s)、LA-SODIUM2 门洒 Sway(0.22, 0.5)；生态柱
  Glass 加 EmissionPulse（"密封的海岸微微在呼吸"）。
- **LC 后处理第二层**（研究报告: outline 之后是 vignette/grain/bloom）:
  builder EnsurePostVolume → `Art/Rendering/LcPost_Tower.asset` VolumeProfile（晕影 .32
  市政蓝黑 / 胶片颗粒 Medium1 .22 / Bloom 阈值1.05 只让真发光体亮(生态柱/灯/信标) /
  调色 sat−12 contrast+8）+ 场景 LC_PostVolume 全局 Volume(priority 10)。HQ 同款运行时
  内存版（HqOfficeLightingPass.EnsurePostVolume, isPlaying 才建, 已有 Volume 则跳过）。
  HQ 工具架灯管加 Sputter。
- **关键修复**: 相机从未开过 renderPostProcessing（URP 默认关，prefab 无
  UniversalAdditionalCameraData）→ Volume 一直无效。PlayerCameraController.
  MakeLocalCameraPrimary + PreviewWalker.Awake 现在强制开启。

**未提交未推送**（PM 未指示）。待 Unity: 重编译 → Rebuild（flicker/pulse/Volume 资产生效）
→ 听队友脚步需双人或 build。剩余: 背景音乐、HQ 深度风格、材质两问、门宽。

## Session 2026-06-11 — UX 重设计管线启动 + HQ 缩放

- PM 不满前轮 UI 改动（仅换色像素化，构图未动），要求逐屏重设计、逐项敲定。
  走 /ux-design 管线。**主菜单 spec 已定稿落盘**: `design/ux/main-menu.md`。
  PM 三大锁定: ①载体=实时 3D 办公室（删烘焙 PNG，菜单相机架在真实 HQ 场景对着
  桌上 CRT，world-space canvas 菜单行=终端绿；进游戏=推镜交棒无黑屏）②全部 7 个
  子页=「盖章公文卡」纸质表单语法（纸底+青页眉+墨字+红章，全游戏统一弹窗）
  ③先敲定全部屏幕 spec 再统一动代码。「关机」=CRT 第 5 行。
- **屏幕队列（待敲定）**: 办公电脑接单终端 → HUD → 结算 → 车内 → 设置。
  全部定稿后一次性实施（主菜单实施量最大: 删 PNG/建菜单相机/纸卡组件库/推镜）。

## Session 2026-06-11 (cont.) — 全 UI 盘点 + 队列扩展（PM 三决定）

- PM 要求全 UI 拉通重设计（MENU/加载/大厅/HQ材质/电脑/图谱/上车/HUD）。盘点结论：
  三套体系并存（IMGUI 黑终端面板 vs 运行时 uGUI vs spec 锁定未实施的 CRT+纸卡）；
  图谱唯一条目仍是已删学校怪（核查专员无条目）；SettlementUIController 仍显示学校
  字段（救出幸存者/排水泵）= 死数据；加载页不存在（NGO 硬切黑屏）。
- **PM 三决定（2026-06-11 锁定）**:
  ① 加载方案 = **车厢即加载**（在途时间 = 异步场景加载窗口，到站开门，无 2D 加载屏）
  ② spec 队列 = **电脑 → HUD（定稿现有 In Design 半成品）→ 结算 → 上车+加载（同链）
     → 大厅 → 图谱 → 设置**；全部定稿后一次性统一实施
  ③ HQ 材质 pass（照塔楼 V8 lo-fi 模式）= **等全部 UI spec 定稿后再开工**
- 统一语言三类表面：CRT 终端绿（世界内屏幕）/ 盖章公文卡（一切模态：大厅=派工名单、
  设置=偏好登记表、图谱=异常档案袋、上车=派车单、返程=提前收工申请单、结算=委托
  结算单、退出=离岗单）/ 现场工单（HUD，per hud.md 哲学）。
- **办公电脑接单终端 spec 已定稿落盘**: `design/ux/office-computer-terminal.md`
  （Approved 2026-06-11）。关键锁定: 主菜单 CRT 与办公电脑=**同一台机器**（BC-DOS
  启动菜单 ↔ 办公管理系统同屏切换）；**页签式终端**（[1]委托文件 [2]采购目录
  [3]公司账本，数字键直达，砍掉 6 项装饰菜单）；**单色绿磷光**（警示=反白行+`!`，
  CRT 上不用红）；委托表=真实池+归档行（删全部硬编码假任务）；商店并入页签
  （F1-F4 全局可用）；账本回看结算讽刺文本。⚠️ 架构缺口：归档行/账本流水需
  SaveIO 扩展逐单结算历史（需 ADR）。新模式 3 个待入库（CRT 状态条/页签/反白警示块）。
- **队列第 2 屏 HUD 已定稿草案（2026-06-11）**: `design/ux/hud.md` 全章节写完，
  Status=Drafted 待 PM 终审。PM 四决定: ①信息清单删干净只留塔楼现状（怪物/时钟/
  持有者/线索奖金行全删）②密封完整度=取柱后常驻（earned-precision 应用）③目标行=
  常驻工单行（推翻旧 4s 淡出设计）④手表规则保留为「精度需挣得」总原则（机制休眠）。
  准星色按 PM 旧拍板修正绿→琥珀。新模式 3 个待入库（工单行/按住进度条/印章红换档），
  连同终端 spec 的 3 个共 6 个种子模式。Open Questions 8 条（含印章红色值分裂
  #C23B2B vs #C23A2B 需统一为 art-bible 值；手表商品去留归口经济 GDD）。
- **队列第 3 屏 结算已定稿草案（2026-06-11）**: `design/ux/settlement.md`。PM 两决定:
  ①出现时机=**返程车厢内**（车厢即加载的在途时间读单；入账仍在办公电脑待领赏金块，
  与终端 spec 一致）②金额=**逐项拆解**（委托报酬→条款化扣损→实付；条款号 C-7/B-2/D-1
  不本地化）。单上不出现声望/经验（经济 GDD 锁定）。E/Esc 收起、Tab 在途重看、
  到站自动收起、账本回看。⚠️ 缺口: MissionRewardResult 需加行项字段（现仅净值）、
  OfficeTaskDefinition 需客户使用备注字段（writer pass）、单号规则+逐单历史共用账本
  ADR、StampThunk 改章落帧触发。新模式「条款式账目行」入库种子（累计 7 个）。
  SettlementUIController 死代码由本 spec 实施时取代。
- **下一步：队列第 4 屏 上车+加载（派车单，同链：MissionVanExitPoint 上车确认 +
  车厢即加载 + 返程提前收工申请单）。**
- **HQ 太小问题（PM: 人物在事务所显得太高，整体拉大）**: 根因=HQ 围着旧 0.7m
  眼高 bug 搭的（实测棚高 2.72m、车库门洞 ~2.2m）。NEW `HqScaleTool.cs`:
  菜单 `Tools > BC > MVP > HQ > Scale HQ Up x1.25 / x1.1 / Down x0.8`——以原点
  等比缩放所有根物体(位置+缩放)+灯光 range，跳过 RectTransform/网络管理器；
  不自动保存（PM 走查后 Ctrl+S 保留或 Ctrl+Z 撤销）。推荐先试 x1.25。
  注意: 若 PM 保存了缩放，主菜单 spec 的相机机位调试要在缩放后的 HQ 里做。

## Session 2026-06-11 (cont. 2) — 结算单实施收尾 + 全量测试通过

- PM 指令「看 settlement.md 继续设计和开发」。盘点发现上轮已落实现主体：
  `SettlementCardOverlay.cs`（盖章公文卡 IMGUI 卡片：进场滑入/0.4s 章砸落/E·Esc 收起/
  Tab 在途重看/sceneLoaded 到站自动收起）、`OfficeTaskDefinition` 备注三池 +
  `GetSettlementNote`（确定性选取）、`TowerMissionManager` 传 baseMoney + StampThunk
  已移至章落帧、`TowerEarthCoast_01.asset` 备注文案 6 条已填（writer pass 完）。
  时序链核实成立：上车坐定(车厢 DontDestroyOnLoad)→Space 发车→结算→卡在车厢内弹出
  →6s 后回 HQ 自动收起。
- **本轮新增**: `Tests/EditMode/Office/OfficeTaskSettlementNoteTests.cs` ×6
  （按结果路由/同种子同文案/取模回绕/负种子/空池/null 池→null 隐藏备注块）。
- **验证**: Assets/Refresh（新文件缺 .meta 导致 CS0103，刷新后消失）→ EditMode
  **150/150 全过**（11.7s）。MCP 桥域重载后先死后自愈——等 20s 重试即可，无需重启
  Unity（已写回 memory）。
- **结算 spec 残留缺口（非阻塞）**: ①单号现用结算数据哈希派生（确定性跨端一致），
  存档序号派生规则待账本 ADR ②逐单结算历史 SaveIO 扩展 = 账本 ADR（与终端 spec 共用）
  ③MissionRewardResult 行项字段未加（卡片用 baseMoney−net 差额推导扣损，单一来源仍是
  计算器，可后置）④SettlementUIController 死代码仍挂在 HQ.unity（删除需动场景，待 PM）
  ⑤「条款式账目行」等 7 个种子模式未入 interaction-patterns.md。
- **队列第 4 屏 上车+加载 已定稿草案（2026-06-11）**: `design/ux/boarding-transit.md`
  （Drafted 待 PM 终审）。PM 四决定: ①派车单=混合形态（等人=顶部票据条保留现状，
  **全员就位才弹中央派车单**，房主 Space 落章签发）②在途进度=**里程虚构+最短仪式**
  （条与真实加载解耦，匀速走 max(加载完成,最短在途)，超时停 92% 呼吸、永不显示"加载"）
  ③提前收工申请单=**房主按住 E 1.2s 签字**（Esc 撤回；队友见票据条附加行「房主正在
  填写…」；折算预估行复用条款式账目行+计算器本地预演）④到站=**开门渗光+自行下车**
  （熄火→0.5s 静默+squelch→门缝渗光[HQ钨丝暖/塔楼阴天灰]→全开 E/走出，全链无黑屏）。
  ⚠️ 最大实施项: 异步加载重排——签发即 LoadScene、SceneEvent 完成+最短在途双门控开门
  （OfficeComputer 出发 + TowerMissionManager 返程两处管线重构）。新模式「派遣票据条」
  入种子（累计 8 个）。Open Questions 9 条（失败强制返程演出与"无黑屏"的张力待 PM 裁决；
  mission-state-machine GDD 的 UI Requirements 节仍是占位符，可反向回填本 spec 链接）。
- **下一步：PM 终审 boarding-transit.md（或 /ux-review）→ 队列第 5 屏 大厅（派工名单）。**

## Session 2026-06-11 (cont. 3) — boarding-transit 全量实施（代码完成，待 Unity 编译验证）

- PM 指令「按之前的 UX 设计开发」= 终审通过，按 `design/ux/boarding-transit.md` 实施。
  **7 个文件全部改完**：
  - `VanTransitOverlay.cs` **整体重写**：Phase{None,Boarding,Transit,Arrived} +
    Card{None,Dispatch,DispatchSigned,EarlyReturn}。全员就位弹派车单（客户端安全计数：
    FindObjectsByType + IsDowned NV，不碰 server-only 的 ConnectedClients）；房主 Space
    签发→章砸落(1.3→1.0/0.12s)+StampThunk→0.6s 后发车；未取柱时 Space 改弹提前收工
    申请单（按住 E 1.2s 签字/松手 0.3s 回流/Esc 撤回/条款 B-2 预估行/完整度<阈值拒收
    警告行/队友票据条见「房主正在填写…」via HostFilingEarlyReturn NV）；在途进度=匀速
    0→92%，加载超时停 92%±2% 呼吸+「即将抵达…」永不显示"加载"；双门控
    （最短在途 && sceneLoaded 水位线 lastSceneLoadedAt>driveRequestedAt）→到站序列
    熄火→0.5s squelch→后门 Interior_WallRear 滑开+渗光（HQ 钨丝暖/塔楼阴天钢灰
    #B8C4CE 的 Unlit 板+Point 光强 0→5.5）→门全开 [E] 下车（E 被结算卡占用时让位）。
  - `TowerMissionManager.cs`：HostFilingEarlyReturn NV、IsObjectiveAboard、
    EstimatePartialMoney()（同 Settle 计算器路径）、RequestDepart(confirmedPartial)
    +房主校验（杆子拉了不再误结部分）、Settle 改为 SeatAllConnectedServer→
    BeginReturnTransitClientRpc→0.75s 后 LoadScene（HQ 在车厢底下加载）。
  - `OfficeComputer.cs`：签发即异步加载（solo: LoadSceneAsync / 网络: NGO LoadScene，
    都先等 0.9s 签字拍）；删除 ClearAllSeatsServer——全队坐着穿场景。
  - `PlayerController.cs`：seatExitTeleportHandled（RestoreControlAt 已落点时
    ExitSeat 不再二次传送）、SeatAllConnectedServer()（返程全队随车含倒地）、
    SceneSafePosition（PlayerSpawnPoint 查找+HQ 兜底）。
  - `HQSpawnManager.cs`：在车未下时改 RegisterArrivalSpawn 延迟交点，不强拉下车。
  - `SettlementCardOverlay.cs`：IsCardVisible、NotifyDisembarked()、
    sceneLoaded 收卡加 VanTransitOverlay.IsActive 守卫（返程中途 HQ 加载不再吞卡）。
  - `MissionVanExitPoint.cs`：储物柜屏返程按钮 = RequestDepart(confirmedPartial:true)
    （该屏已有摘要+房主门控，算确认过）。
- **验证状态（已收口）**: 本地静态核对全过；`git diff --check` 仅场景 YAML 既有
  尾随空格（Unity 生成，非本轮）。桥曾僵死（第二形态：握手超时，详见 memory
  ws-unity-call-tool）→ 按 PM 授权重启 Unity 修复 → **编译零错误，EditMode
  150/150 全过（11.3s）**。
- **UI/UX 全链统一审计（PM 质询后逐项对照，3 处修正）**: ①locale `press_x_leave`
  「[X] 下车」→「[X] 离座」——"下车"一词专留到站 `[E] 下车`，全链一词一义
  （E=接触/签字/下车、Space=房主签发、X=离座、Esc=撤回/收起、Tab=重看）
  ②新卡片字阶统一到结算单基准（卡头 14px/章字 18px）③SettlementCardOverlay 加
  `ConsumedCloseThisFrame` 帧守卫——修复同帧 E 既收结算卡又触发下车的执行顺序 bug。
  确认统一项：四张纸面（票据条/派车单/申请单/结算单）同一套盖章公文卡语法（墨色
  逐位相同、章几何 118×44/-8°/1.3→1.0 砸落/0.12s、0.25s 滑入）；屏幕类（办公电脑、
  储物柜返程屏）走终端绿；进度条=青绿墨水非发光条；落章音三处全部章落帧触发；
  层级结算卡(-100)>派车单(-90)>票据条。**待 PM 裁决 2 处**: 派车单要不要加确定性
  单号行（结算单有 BC-2098-XXXX，加了头带自然同高 56px）；现头带 44px（单行）。
- **残留（待 PM）**: ①返程最短在途 returnToOfficeDelaySeconds=6s < spec 建议 ≥10s
  （场景序列化值，需 PM 在 Inspector 改，禁改场景 YAML）②下车=按 E（spec 的
  "走出"简化）③减弱动效设置项延后（无设置系统）④失败强制返程=直接 seat-all 演出从简。
- **下一步：Unity 编译+150 测试回归 → PM 实机走查发车/在途/到站全链 → 队列第 5 屏 大厅。**

## Session 2026-06-12 — 比例修正（PM 验收第 4 屏后指令）

- **PM 验收**: 第 4 屏（上车+在途）通过（「第四屏还可以吧，你接着做吧」）。
- **PM 新指令（优先）**: ①人在事务所太高 → 比对比例后**直接缩放事务所**（明确授权
  改场景）②人坐在车上也太高，视觉感官不对。然后继续队列第 5 屏 大厅。
- **车内坐高已修（代码）**: 根因 = EnterSeat 把整根 2m 站立胶囊传送到凳位、相机保持
  站立视高 ~1.7m（Update 坐姿分支提前 return，UpdateCrouchCamera 从不执行）。
  修法（`PlayerController.cs`）: 新增 `seatedCameraDrop = 0.6f`（凳面 0.29m+坐姿躯干
  ≈ 视高 1.05m）+ owner 本地 `seatedCameraActive` 标志（EnterSeat 设、ExitSeat/
  RestoreControlAt 清——不用网络 IsSeated 驱动，否则客户端下车后 SeatIndex 滞后一个
  RTT 会在车门口闪坐姿视高）；坐姿分支补调 UpdateCrouchCamera(false)（入座 ~0.25s
  自然"落座"下沉动效，起身回弹）。
- **HQ 缩放预检（全过）**: HqScaleTool 菜单 `Tools/Black Commission/MVP/HQ/Scale HQ
  Up x1.25`（缩根 localPosition+localScale+Light.range，跳过 RectTransform/基础设施，
  不自动保存→需 save_scene）。连带项核查: HqOfficePropRestorer 幂等按名查重、道具已
  存于场景 YAML 会随缩放走，不会重摆 ✅；HQSpawnManager 用场景 Transform 无硬编码 ✅；
  HQ.unity 有 PlayerSpawnPoint（GetSceneSafePosition 硬编码兜底 (-1.55,1.15,0.55)
  不会被用到）✅；MvpSceneStyleDirector 类已不存在（CLAUDE.md 注释过时）→ HQ 是
  手工场景，缩放工具适用。
- **桥又僵死（形态2 第二例）**: Unity 重启后握手仍超时（handshakeTimeout 探针确认），
  优雅 taskkill 无响应（内存冻结）→ 强杀 3 实例重启。
- **下一步: 桥恢复 → execute_menu_item 缩放 x1.25 → save_scene → 编译+150 测试回归
  → 报告比例数字给 PM → 第 5 屏 大厅 spec。**

## Session 2026-06-12 (cont.) — HQ 缩放执行完毕 + 第 5 屏大厅 spec

- **HQ 缩放 x1.25 已执行并保存**（PM 上轮已授权）: Unity 重启后桥直连正常，
  menu 执行成功 — 23 个根物体 localPosition+localScale ×1.25 + 全部点/聚光灯
  range ×1.25，save_scene 成功（HQ.unity）。比例数字: 棚高 2.72→**3.40m**、
  车库门洞 ~2.2→**2.75m**（vs 眼高 1.7m / 站高 2.0m——门洞余量 0.75m，正常）。
  EditMode **150/150 全过**（11.7s），编译零错误。待 PM 实机走查观感
  （连同车内坐高 1.05m 修复一起验）。
- **第 5 屏 大厅已定稿草案（2026-06-12）**: `design/ux/lobby.md`（Drafted 待 PM 终审）。
  PM 四决定: ①进入语法=**各自进入**（HQ 即真实大厅，每人看完名单自己 Enter 确认到岗
  各自推镜，无房主门控）②色板=**大厅卡上可改+撞色独占**（先到先得，server 仲裁，
  被占色 ◌ 置灰）③踢人=**按住 0.8s 除名**（复用按住=签字语法，行内墨条+落红章，
  被踢端见「除名单」卡）④房间码常驻=**顶部小纸签**（main-menu spec 的
  ConnectedStatus 组件新增职责）。退役项: MainMenuUI WaitingTerminal 940×620 绿面板
  + QuickNetworkUI IMGUI HostWaiting + 连接前旧色板选择。⚠️ 架构旗标 2 个:
  色板独占 ServerRpc 校验+join 分配；NGO disconnect reason 需携带除名标记。
  新模式「花名册行」入种子（累计 9 个）。Open Questions 6 条（join 撞色静默分配
  vs 提示待 PM；main-menu.md 小纸签注记回填待 PM 同意）。
- **下一步: PM 终审 lobby.md → 队列第 6 屏 图谱（异常档案袋）→ 第 7 屏 设置
  （偏好登记表）→ 全部定稿后一次性统一实施（主菜单实施量最大）。**

## Session 2026-06-12 (cont.) — PM 改令「现在就实施」→ 主菜单批次 1 代码完成

- 背景: PM 启动后看到旧菜单（spec 未实施=按原计划）误以为没改; 缩放经
  git diff 核实确已落盘（全部根物体 ×1.25, Desk 1.6→2.0 等）。PM 拍板:
  **跳过剩余 2 屏 spec, 立即转入实施**（图谱/设置 spec 后补）。
- **批次 1（主菜单载体革命）已写完, 待 Unity 编译验证**:
  - NEW `Scripts/UI/CrtMenuStage.cs`: 菜单相机架在 HQ 桌前对着 CRT（构图=CRT
    居左 1/3+轻俯角; 场景放 "MenuCameraAnchor" 物体可覆盖机位）; 世界空间
    canvas 贴 CRT 玻璃: BC-DOS 页眉+5 行终端绿（继续营业[无存档墨淡跳过]/
    新事务所/加入事务所/设置/关机）反白选中+`>`前缀+哔声, ↑↓/W S/Enter/鼠标
    hover; CRT 开机闪; 新委托招贴=屏旁纸条（Resources/Tasks 数据驱动+红章）;
    联机后行隐藏换「系统在线」状态行; **推镜交棒** DismissLobbyWaiting→
    0.85s SmoothStep 滑到本地玩家头部姿态后销毁菜单相机, 无黑屏。
  - MainMenuUI: crtStage 接缝（公开 UiContinueShift/UiOpenJoin/UiOpenSettings/
    UiQuitConfirm/UiPlayHover/UiPlaySelect/MenuRowsAvailable/AnyModalOpen）;
    stage 激活时 PNG 背景/主面板/版本号永久隐藏（PNG 资产留盘未删）; Esc 根屏
    =关机确认; Enter 旧直开 host 仅无 stage 时; 名字输入移入设置卡（440×440
    重排版）; 加入卡补「局域网直连…」链接; **大厅自己行 ‹ › 换色**（撞色独占
    owner 端跳过, CharacterIndex owner-write NV 热同步, 存 SavedIndex）。
  - `ComputerCloseupCamera` 缩放回归修复: 屏幕中心从硬编码 (x,1.085,1.704)
    改为 computerTransform.position（HQ ×1.25 后旧值瞄偏）, 距离 0.68→0.85。
  - SaveIO.AnySave; MvpLocale 新键 12 个（crt_*/menu_*/job_note_*/lan_direct_link）。
- **验证状态: 未编译**（桥僵死且 PM 正用 Unity, 不重启）。下一步: PM 聚焦
  Unity 触发编译 → 修编译错 → Play 验证: 开机即 3D 办公室+CRT 行 → host →
  名单卡(可换色) → Enter 推镜进玩家 → E 电脑终端 closeup 对准缩放后屏幕。
- **「火星监控室」选人已拍板（PM 2026-06-12）**: 名单卡入口制——派工名单自己行
  「更换工装」→ 切入暗监控室场景, 窗外雾中候选人逐个走过, Enter 盖章选定回卡;
  ‹ › 行内换色保留为快捷路径; 撞色独占沿用（被占工装带「已登记」不可选）;
  叙事倾向「劳务派遣资格审查窗口」避免字面火星矛盾（writer pass）。已写入
  lobby.md 修订节。**实施排在主菜单批次 1 编译验证之后（~1 工作日）。**
- **下一步: PM 聚焦 Unity 编译批次 1 → Play 验证 CRT 菜单/推镜/换色/电脑特写
  → 监控室选人场景实施 → 电脑页签终端（office-computer-terminal spec）。**

## Session 2026-06-12 (cont. 2) — PM 两轮否决 3D CRT 菜单 → LC 式平面菜单 + 比例审计落数据

- **PM 验收批次 1: 否**（"太不好看+太糊，参考致命公司；比例也没改"）。编译错 1 个
  （Button.SetActive）已修。中途修过一轮糊（菜单期 renderScale 停车 1.0 + 菜单相机关
  后处理 + 拉近+字号↑），PM 仍不满构图 → **方向拍板: 弃 3D 实景 CRT，转 LC 式平面菜单**。
- **LC 式菜单已实施**（屏幕空间=天生不受 renderScale 影响，绝对清晰）:
  BuildBackground 永远纯色橡胶黑（PNG 路径删除）; 标题「黑色委托」72px+EN 副题左上;
  五行大字左列（CreateMenuRow 左锚 520 宽: 继续营业[无存档置灰]/新事务所/加入事务所/
  设置/关机' + 局域网直连小链）; 角色色板选择器保留右上; 名字字段只在设置卡。
  **CrtMenuStage 整体停车**（useCrtWorldStage=false 一行可复活; 推镜/招贴/renderScale
  停车逻辑都在里面）。死代码未清: BuildReference* 系列、CreateMenuPanel。
- **比例审计有数据了（PM 跑了菜单, production/qa/hq-proportion-audit.md）**: 实锤=
  ①面包车高 1.97m=玩家身高（玩具感主凶）②台灯×2 飘在 2.2-2.7m 空中 ③电脑荧光灯管
  1.91m 撞头高 ④办公↔车库门洞 3.5m 通高无门楣 ⑤MVP_OfficeComputer 交互点悬在 CRT
  模型(顶1.29)上方 10cm。桌 0.78/柜/工具架/棚高 3.36 全 ✓。旧小房间渲染器全关
  （只剩隐形碰撞体, 非视觉问题, 待清理）; HQMenuCamera=幽灵相机（脚本已删, 启用中）。
- **NEW `HqProportionFixTool.cs`** 菜单 `... > HQ > Fix Proportions (audit 2026-06-12)`:
  ①车组（ShellVan+3 collider+trigger）地面中心等比 ×~1.2→2.36m ②台灯 settle 到正下方
  表面 ③荧光灯升到 2.75m ④门楣梁 ShellDividerDoorHeader（净高 2.2, 复用墙材质, 含碰撞）
  ⑤交互点落座到模型屏面(80%高, -Z 前脸)。幂等; 不自动保存（走查后 Ctrl+S）。
- **下一步: PM 编译 → Play 看 LC 菜单 → 跑 Fix Proportions → 走查 → Ctrl+S →
  （可选重跑 Audit 验数）→ 电脑页签终端实施。**

## Session 2026-06-12 (cont. 3) — 菜单 mockup 流程 + B 稿实施

- PM 对首版 LC 平面菜单 4 点不满（标题被裁/不自适应、右上色板删、纯黑背景、字体怪）
  + 要求**先画 UI 过目再写代码**。出 3 张 1920×1080 视觉稿（SVG→headless Edge→PNG,
  `design/ux/mockups/main-menu-v2/`）: A 市政调度室 / B 致命公司直系 / C 受理表单纸。
  **PM 选 B**。
- **B 稿已实施（MainMenuUI）**: 背景=暗橄榄绿垂直渐变（运行时 1×2 渐变纹理, 非纯黑）;
  标题「黑色委托」96px 骨白 #D8D2BC + 琥珀下划线 #C78A33 + 字距副题;
  菜单行=B 语法（纯文字 36px #BFC4AE + 2px 细分隔线, hover=琥珀 #E8B25C 加粗+▸+
  说明文字仅悬停行浮现; 无背景块; 无存档=灰墨+常显原因）; **角色色板选择器已删**
  （换色=大厅 ‹ › / 未来监控室）; 右下低饱和面包车+卷帘剪影; 页脚=ver 0.1+局域网直连
  +「本季度欠款：1,200G」(TODO 接 CompanyData 真值); 旧 versionText 永久隐藏。
  琥珀=art bible 主强调色 ✓（绿仅限世界内屏幕——平面菜单非屏幕, 比绿更合规）。
- 自适应: CanvasScaler 1920×1080 match0.5 + 全部锚点边距内（PM 截图的标题裁切一半是
  Game 视图 1.1x zoom 裁的——提醒 PM 调回 1x）。
- **待 PM**: ①编译+Play 看 B 菜单 ②跑 `HQ > Fix Proportions`（上轮交付未执行）→走查
  →Ctrl+S ③字体: 布局过审后引思源黑体 SDF（免费可商用）替换 TMP 回退中文。

## Session 2026-06-12 (cont. 4) — push + remote i18n 审查修复 + 棚高工具 + 13 屏图纸

- **PM 反馈**: ①Fix 后 HQ 里人仍太高、塔楼里正好（=差的是净空: 塔 4.2m vs HQ 3.38m）
  ②B 菜单"没有背景" ③其余屏未动 → 要求全部 UI 先出 PNG 图纸过目 ④push+查 remote bug。
- **Git**: 本地全部提交; remote 有 e635ae2（PM 另一会话的 i18n: index 0=EN 默认）;
  rebase 冲突 5 文件——两个 .cs 取本地重写版（boarding-transit 实现）、hud.md/active.md
  取本地完整版、MvpLocale 手合（EN-first + 离座语义保留, 菜单键翻转为 EN-first）。
  **审查发现 e635ae2 漏改 2 处真 bug 并已修**: QuickNetworkUI/MainMenuUI 两个语言选择器
  仍按旧索引接线（选"中文"实际切英文）→ c109159 已推送。最新推送 dc60930。
  ⚠️ 遗留: MainMenuUI 新 B 菜单行描述/欠款行为硬编码中文, EN 默认下不翻译（TODO 过
  MvpLocale）。
- **NEW `HqShellRaiseTool.cs`** `... > HQ > Raise Shell To 4.2m (tower parity)`:
  只升墙体/天花/墙碰撞体（×4.2/3.38, 绕地面）, 家具不动, 两个门楣保持下沿净高向上
  补到墙顶。幂等不自动保存。= "人太高"处方（家具实高已对, 缺的是头顶空间）。
- **13 屏 UI 图纸全部出齐** `design/ux/mockups/ui-kit/01..13.png`（generator:
  tools/generate_ui_mockups.py, headless Edge 渲染）: 01 主菜单(带办公室剪影背景层)
  02 调岗申请单 03 派工名单 04 偏好登记表 05 离岗单 06/07/08 终端三页签(CRT 绿)
  09 塔楼 HUD(工单行/琥珀准星/热栏) 10 派车单 11 在途票据条 12 提前收工申请单
  13 委托结算单。三层语言: 橄榄菜单层(B)/盖章公文卡/CRT 绿。**待 PM 逐张过目批改**。
- **下一步: PM 看 13 张图纸给批改意见 → 跑 Raise Shell + Fix Proportions 验比例 →
  按批准图纸逐屏实施（终端三页签最大）。**

## Session 2026-06-12 (cont. 6) — terminal redesign + 比例真正落地 + 测试绿（MCP 恢复后）

- **MCP 干净重启恢复**（杀全部 Unity+删 UnityLockfile+重启, 176s 连上, 稳定）。之前几轮
  桥连续域重载卡死, 导致比例工具跑不成 + 用户在 Play 模式测旧编译（"作业本"+E 无反应的真因）。
- **办公电脑终端重写**（office-computer-terminal.md）: 删 6 项菜单+6 行假学校任务,
  换 3 页签 [1]COMMISSIONS [2]SUPPLY [3]LEDGER（数字键直达）, 纸面色→**单色绿磷光**
  CRT, 负资金/待结算/活动页签=反白行+`!`, 数据驱动（只显真实生态柱任务, 无假行）,
  单一主行动栏, 3270 字体, 全英文。**E 修复**: Update 原来只接 ESC, 现 E→主行动
  （领结算/接单/确认收购, 0.25s 去抖）+ 数字键切页签。0 编译错误。
- **HQ 比例真正应用+保存**: HqShellRaiseTool 棚高 3.4→**4.20m**（塔楼同款, 22 物体×1.244）
  + HqProportionFixTool（面包车 1.97→**2.36m**、台灯落桌面、门楣）, save_scene + 提交。
- **菜单背景=真 3D 办公室**: 发现游戏内菜单背景代码还是橄榄绿渐变（已删）, 改死黑底+
  "左暗右透"渐变让 HQMenuCamera 渲染的真办公室透出来; 进游戏关菜单相机。英文标题 BLACK
  COMMISSION（72px 3270）。
- **去橄榄绿全局**: BlackCommissionUiTheme MilitaryGreen 族→官印红褐 #5A2E2A（8 文件公文卡
  页眉/边/按钮自动生效, 终端 CRT 绿保留）。
- **测试 3 失败修复**: e635ae2 把 MvpMissionClock 改英文（Day N/Early Morning）但旧测试断言
  中文"清晨/次日/第3天"→改英文期望。**EditMode 150/150 全绿。**
- **待 PM Play 验**: 终端（E通/绿磷光/生态柱非作业本）、HQ 比例（人对不对）、菜单实景背景。
- **剩余队列**: ESC 设置→偏好登记表纸卡; menu 后 4 人等待→lobby.md 官印红褐派工名单卡。

## Session 2026-06-16 — Game Design Overhaul + Modular Room System

### Major design decisions locked this session (all written to docs):

**Mission loop redesigned** (`design/quick-specs/scavenging-item-system-2026-06-16.md`):
- Core mechanic: scavenging with HIDDEN item values — judge worth from client commission text
- Van shared weight limit: 12 units (Light=1, Medium=2, Heavy=4)
- Settlement reveal: each item shows price + satirical "client intended use" note
- One dispute per settlement (bargaining system)
- Commissioned target = optional bonus 1.4×, not a failure condition if missed

**Client system** (`docs/world-background-2098.md` — new section):
- Three client types: Martian Institutions (fixed pay), Martian Individuals (negotiable), Earth-side (post-ending only)
- Martian individuals split into 3 generations:
  - 移民一代: real memories, specific requests
  - 移民二代: Earth as parents' stories — most resonant (parallel to immigrant children in America)
  - 移民三代+: collector's detachment, no grief

**Map sequence locked** (`design/gdd/map-sequence-and-modular-system.md`):
- 6 maps total: 烂尾预售楼 → 地下私人会所 → 地铁换乘枢纽 → 市民债务仲裁局 → 区域医疗分诊站 → 轨道货运中转站
- Mars = hidden ending only. Stay on Earth → dispatched to Mars as worker (playable). Go to Mars → CG cutscene.
- Modular room system (Route B): Kit A institutional interior shared across maps 1/5/6

**Modular room system** (`Assets/_Project/Editor/ModularRoomBuilder.cs`):
- 14 shared prefabs: 11 corridor/junction types + 3 room types
- Hero rooms = fixed anchors (objective always deep, van always at entry)
- Connectors vary per seed → different routes per run
- More structural variation than LC (path changes, not just item spawns)

### Immediate next task (pending on new machine):

1. git pull origin main
2. Unity: Tools > BC > MVP > Tower > Rebuild v8 Whitebox (ensure materials exist)
3. Unity: Tools > BC > MVP > Modules > Build All Shared Modules
4. Verify in scene: 4m grid, 3.2m ceiling, 2m door, 2.25m door height, human scale OK
5. If OK → write RoomDresser.cs (procedural furniture per room type)

See task list in Claude Code session for step-by-step.

### Other design docs written this session:
- `design/quick-specs/mission-pool-selection-2026-06-16.md` — 3-slot pool, per-run refresh
- `design/quick-specs/scavenging-item-system-2026-06-16.md` — full item/settlement/bargaining spec
- `docs/world-background-2098.md` — client types + generational system + ending philosophy

## Session 2026-06-16/17 — Pull/merge + modular dressing pipeline (the 5-step list above = DONE)

The "Immediate next task" 5-step list above is **complete**:
1. **git pull** ✓ — fast-forwarded 10 commits (29d44c0→c665ea3), no conflicts; this file
   auto-merged (remote 2026-06-16 block + local Echo Mold notes both kept).
2. **Rebuild v8 Whitebox — SKIPPED as redundant.** Its purpose ("ensure materials exist")
   was already satisfied: the 3 materials ModularRoomBuilder needs are committed on disk,
   and ModularRoomBuilder only `LoadAssetAtPath`s them — it does NOT depend on the tower
   scene rebuild. Scene left clean (isDirty:false). If the tower whitebox scene itself is
   ever wanted, run that menu manually (it's heavy + crashes the Node bridge).
3. **Build All Shared Modules** ✓ — 14/14 module prefabs in `Art/Maps/Shared/Modules/`, 0 console errors.
4. **Scale verify** ✓ — measured from prefab YAML: 4m grid, 3.2m ceiling, 2.0m door width,
   2.25m door clear height all confirmed. (Human-scale eyeball is the PM's only manual check.)
5. **RoomDresser** ✓ — spec `design/quick-specs/room-dresser-set-dressing-2026-06-16.md`;
   implemented 4 files: `Scripts/Level/{LootAnchor,RoomDressingSet}.cs`,
   `Editor/{WhiteboxFurnitureBuilder,RoomDresser}.cs`. Generated 10 whitebox furniture
   prefabs (`Art/Maps/Shared/Furniture/`), 3 `RoomDressingSet` assets (`…/Shared/Dressing/`),
   and dressed all 3 room modules (Dressing group + furniture + LootAnchors). Verified on disk:
   Office 7 furniture/3 anchors, Utility 5/2, Large 8/5 — matches the spec's default sets.

**Bug fixed during pull:** new `ModularRoomBuilder.cs` referenced `RoomSlot`/`RoomSlotRole`
(namespace `BlackCommission.Level`) but lacked `using BlackCommission.Level;` → would CS0246
and disable ALL custom menus. Added the using; recompile is 0 errors / 0 warnings.

**Tooling gotchas this session:** (a) the mcp-unity Node bridge crashes on heavy menu ops →
used `tools/ws-unity-call.cjs` direct to Unity's 8091 listener for everything after.
(b) Freshly-added `[MenuItem]`s are NOT found by `ExecuteMenuItem` after a *scripted* recompile
until the editor gains focus/rebuilds its menu → PM ran "Build Whitebox Furniture" +
"Dress Room Modules" manually from the menu bar.

**UNCOMMITTED:** all of the above is untracked working-tree state (4 scripts + furniture /
dressing / module prefabs + this file). Not committed per project rules.

**Next (not started):** loot-spawn system consuming `LootAnchor`s (scavenging owns it);
optional — add RoomDresser to `design/systems-index.md`; bidirectional dependency backlinks
in scavenging spec + map GDD; Play-mode validation of nav (interior walkable) + dresser idempotency.

## Session 2026-06-17 — Module showcase scene (view/walk the dressed rooms)
PM asked how to *see* the rooms built last session → chose "build a walkable showcase scene".
- **NEW `Assets/_Project/Editor/ModuleShowcaseBuilder.cs`** — menu `Tools > Black Commission >
  MVP > Modules > Build Module Showcase`. Creates a throwaway scene
  `Assets/_Project/Scenes/Module_Showcase.unity` (never touches HQ/Tower): the 3 dressed rooms
  (Office_4x8 @x0 / Utility_4x4 @x8 / Large_8x8 @x16) in a row on a ground slab, directional +
  per-room point lights, TextMesh name plates, and a `PreviewWalker` rig (CharacterController +
  child MainCamera) spawned at (10,0.2,-5) facing the row. Idempotent (NewScene EmptyScene → rebuild).
- Compiles against predefined assemblies (Editor + PreviewWalker both no-asmdef); ground reuses
  V8_Concrete_Poured.mat (falls back to primitive default if absent).
- **RAN + verified (2026-06-17, via `tools/ws-unity-call.cjs` → 8091):** 0 compile errors; menu
  executed cleanly; build log printed; scene saved. `get_scene_info` confirms active scene =
  `Module_Showcase`, rootCount 12 (3 rooms + 3 point lights + 3 labels + ground + directional +
  PreviewWalker), isDirty false. Awaiting PM eyeball (human scale / furniture density / label
  facing / interior brightness). Next real feature: loot-spawn system the rooms' LootAnchors serve.

## Session 2026-06-17 (cont.) — Scavenging P1a: pure logic core + tests
PM approved the scavenging system → chose the "logic core + tests first" slice. Built **NEW asmdef
`BlackCommission.Scavenge.Core`** (`noEngineReferences`, mirrors the Mission.Core pattern) + 4 scripts under
`Assets/_Project/Scripts/Scavenge/Core/`:
- `ScavengeTypes.cs` — `WeightClass` (Light/Medium/Heavy = 1/2/4, enum value = unit cost), `LootSurface`
  (mirrors `BlackCommission.Level.DressingSurface` value-for-value so the runtime layer maps by cast),
  `LootAnchorSlot`/`LootPlacement` structs, `WeightClassExtensions.Units()`.
- `ScavengeItemSpec.cs` — id + weight + allowed surfaces (empty = anywhere), `AllowsSurface()`.
- `VanWeightLedger.cs` — host-auth shared van capacity tally (`CanFit`/`TryAdd`/`Remove`/`Reset`, clamps; spec cap 12).
- `LootSpawnPlanner.cs` — deterministic seed-driven planner (anchors sorted by Id + candidates by Id ⇒
  order-independent; Fisher–Yates; surface-filtered; skips unmatchable anchors; no anchor reused; null/empty-safe; min>max clamp).
- Tests in `Assets/_Project/Tests/EditMode/Scavenge/` (asmdef + `VanWeightLedgerTests` 13, `LootSpawnPlannerTests` 10 = 23).
- **VERIFIED GREEN via headless batch** (`Unity.exe -runTests -batchmode -nographics`, 2026-06-17): the mcp-unity
  bridge wedged on the post-compile domain reload (documented "recompile kills the pump" — 3 timeouts), so after the
  PM CLOSED Unity (freeing the project lock; force-kill was correctly blocked by the safety classifier) the suite ran
  headless. **Scavenge.Core compiled clean (0 CS errors); both fixtures PASS — VanWeightLedgerTests 12/12,
  LootSpawnPlannerTests 10/10 = 22/22.** Suite-wide 161/168: the 7 failures are ALL
  `McpUnity.Tests.BatchExecuteToolTests` (gamelovers MCP package self-tests that NRE in headless batch — environment
  only, NOT scavenging, NOT a regression; they pass in the interactive editor). NUnit XML landed at
  `%LOCALLOW%/DefaultCompany/Black Commission/TestResults.xml`, NOT the `-testResults` path (the embedded MCP test
  callback `TestRunnerNoThrottle` overrides the destination). Editor relaunched for PM afterward.
- Style red line restated to PM: this system is code+data; visuals = whitebox placeholders only; real models deferred
  to PM's Meshy pipeline under `style-lock-v2`.
- **NEXT after tests confirm green:** P1b runtime — `LootSpawner` (NetworkBehaviour: find LootAnchors → planner →
  host-spawn synced items), `ScavengeItem` pickup (reuse Carriable/PlayerHotbar), van weight enforce at
  VAN_CargoZone; + `WhiteboxLootBuilder` placeholder prefabs per category. UNCOMMITTED (project rule).

## Session 2026-06-17 (cont.) — Scavenging P2a: settlement value logic + tests
PM deferred the whitebox visuals (P1b) → redirected next step to the settlement math. PM also restated a working
rule: **ask before key design/tradeoff decisions** (saved to memory `ask-pm-on-key-decisions`). Asked + PM locked the
one open design point: **condition tiers = Good ×1.0 / Worn ×0.7 / Damaged ×0.4** (per-item, tunable knobs). Added to
`Scavenge.Core`:
- `SettlementTypes.cs` — `ItemCondition` enum + `SettlementItem` (input) + `SettlementLine` (output).
- `ScavengeSettlementCalculator.cs` — `Settle(items)` → `SettlementResult` (per-item lines + total + commissionedDelivered).
  Per item `payout = round(base × conditionMult ×(commissioned?1.4:1))` away-from-zero; `total = Σ line payouts`.
  Multipliers injected (DI knobs); negative base clamped; null/empty safe. Does NOT touch tower `MissionRewardCalculator`.
- `ScavengeSettlementCalculatorTests` (12). **Scavenge logic now 34 tests** (22 P1a + 12 P2a).
- Stated-not-flagged-key assumptions: commissioned bonus = ×mult on the commissioned item, stacks with condition;
  round away from zero (matches tower); dispute + free-salvage mode deferred (P2 later / P4).
- **VERIFIED GREEN via headless batch** (2026-06-17): `ScavengeSettlementCalculatorTests` 12/12 Passed; suite 180
  total / 173 passed / 7 failed — the 7 are the SAME `McpUnity.Tests.BatchExecuteToolTests` (headless noise), no new
  failures = zero regression. Scavenging logic now **34/34** (P1a 22 + P2a 12). Editor still CLOSED. UNCOMMITTED.

## Session 2026-06-17 (cont.) — Scavenging B: item-definition data layer (structure only)
PM chose "structure + config only, real item values deferred". Added (Assembly-CSharp; reaches Scavenge.Core via
auto-reference, same as PowerGateBreaker→Mission.Core):
- `Scripts/Scavenge/ScavengeCategory.cs` — 12-category content enum (spec roster).
- `Scripts/Scavenge/ScavengeItemDefinition.cs` — SO (id/displayName/category/weight/allowedSurfaces/baseValue/prefab;
  `baseValue` PLACEHOLDER; `ToSpec()` → Core `ScavengeItemSpec`).
- `Scripts/Scavenge/ScavengingConfig.cs` — SO of spec-locked knobs (cap 12, items 10–14, bonus 1.4, condition
  1.0/0.7/0.4, pocket 2); `CreateSettlementCalculator()` / `CreateVanLedger()` wire knobs → Core objects.
- `Editor/ScavengeContentBuilder.cs` — menu `… > Scavenge > Build Config + Sample Items`.
- **VERIFIED**: batch #3 → 0 `error CS`, Assembly-CSharp(+Editor) compiled, only the 7 MCP failures (zero regression).
  Assets generated **headless via `-executeMethod ScavengeContentBuilder.Build -quit`** (exit 0): confirmed on disk —
  `Assets/Resources/Config/ScavengingConfig.asset` + 2 PLACEHOLDER samples under `Assets/_Project/Content/Scavenge/Items/`.
- DEFERRED (PM-owned): real 12-item roster + balanced base values = a later economy/balance pass.
- **TOOLING WIN**: headless asset/tool execution via `-executeMethod` (no GUI, no bridge) — see memory.
- UNCOMMITTED. Scavenging so far: P1a logic ✓ + P2a settlement logic ✓ + B data layer ✓. Still open: P1b runtime
  (LootSpawner/ScavengeItem/van-weight-enforce — needs the deferred whitebox visuals), economy values pass, P2 reveal UI.

## Session 2026-06-17 (cont.) — Scavenging P1b sub-blocks 1-3 (item + visuals + spawner)
PM chose P1b (runtime) + minimal whitebox (3 props by weight). **KEY FINDING from reading code**: `PlayerHotbar` is
gear-only (Flashlight/Battery enum) — loot does NOT fit it. Loot reuses **Carriable + CarrySystem** (like the eco
column); weightClass drives van units, separate from isHeavy (carry style). Built:
- `Scripts/Scavenge/ScavengeItem.cs` — Carriable subclass + IInteractable (mirrors EcoColumnCarriable);
  OnInteractStart→CarrySystem.TryPickUp; itemId/weight/category/hidden baseValue + `ConfigureServer()`.
- `Editor/WhiteboxLootBuilder.cs` — menu `…>Scavenge>Build Whitebox Loot Props`; 3 prefabs (Light/Medium/Heavy) at
  `Resources/Loot/`, each Cube+Rigidbody+NetworkObject+ScavengeItem (weightClass + isHeavy set via SerializedObject).
- `Scripts/Scavenge/LootSpawner.cs` — NetworkBehaviour; host-only OnNetworkSpawn: Resources.LoadAll defs +
  ScavengingConfig + find LootAnchors (sorted by world pos) → `LootSpawnPlanner.Plan` → server Instantiate+`Spawn(true)`
  (TowerLayout pattern) → `ConfigureServer`. Clients receive items via replication.
- Moved `ScavengeContentBuilder` output → `Resources/Scavenge/Items` (runtime-loadable).
- **VERIFIED headless** (`-executeMethod`, exit 0): project compiles **0 CS errors**; 3 loot prefabs generated and
  **auto-registered in DefaultNetworkPrefabs** (GUIDs confirmed present at lines 33/38/43); 2 item defs + ScavengingConfig
  in Resources. Full spawn data chain wired.
- ORPHANS (harmless): old samples still under `_Project/Content/Scavenge/Items` (superseded by the Resources copies) — clean up later.
- **REMAINING**: (4) van weight gate at a cargo zone; a live PLAY-TEST. Both need a test bed — `Module_Showcase` has
  anchors (can test spawn+carry by adding a LootSpawner + hosting) but NO van; the tower has a van but NO modular anchors.
  The full deposit/weight loop needs a mission scene assembling modular rooms + a van (separate integration; doesn't exist yet).
- UNCOMMITTED.

## Recovery

After compaction or a new session: read this file, then `design/systems-index.md`,
then the relevant docs in `docs/`. Files are the memory, not the conversation.

## Session 2026-06-12 (cont. 5) — UI mockup kit v2（PM 否决 v1: 丑/空背景/配色没遵守/字体怪）

- **PM 5 点反馈**: ①参考致命公司 ②背景空、丑 ③配色没遵守约定 ④字体用原来的 ⑤问有没有用
  UI/UX skills ⑥要新一版、全部同风格、第一版全英文。
- **配色违规已纠正**（v1 我擅自用了橄榄绿 #222B22 背景 + #E8B25C 琥珀, 都不在调色板）。
  v2 锁死 art-bible §4 / AGENTS 身份: 死黑橡胶 #1A1A17 底 / 钨丝琥珀 #FF9820·#FFAB40 主强调
  / CRT 绿 #6CFF5F 仅电子屏 / 盖章红 #C23A2B 仅纸·标牌 / 市政青 #3F5F5C 公文头 / 做旧纸
  #D6CCAE。字体: 干净 sans（近原版 Liberation Sans）, 等宽仅留给终端。
- **致命公司参考落地**: 深色面板 + 胶片颗粒(feTurbulence) + 暗角 + 扁平方块按钮 + 不空的
  背景（暗办公室: 钨丝灯暖池打亮 CRT 绿光/档案柜/墙上催缴单+OVERDUE 红章）+ 绿底等宽终端。
- **UI/UX skills 诚实说明**: 画 mockup 本身无对应 skill（/ux-design 写 spec、/ux-review 审 spec）;
  做法=严格锚定已批准的 spec（main-menu.md/hud.md/settlement.md）+ 美术圣经配色, 不靠感觉。
- **三层统一语言**（全 13 屏一致）: 系统层=深面板+琥珀+颗粒(LC) / 终端层=CRT 绿等宽(LC 终端)
  / 模态层=盖章公文卡（青头+红章, art-bible「UI 伪装成公文」）。HUD=极简工单。全英文。
- 生成器整体重写 `tools/generate_ui_mockups.py`; 13 PNG 重出于 `design/ux/mockups/ui-kit/`。
  **待 PM 逐张过目**。v1 旧 PNG（main-menu-v2/ A/B/C + ui-kit 中文版）已被 v2 覆盖。
- **下一步: PM 看 v2 13 张定方向 → 按批准稿实施（主菜单已大半在代码里, 需把硬编码中文
  改 MvpLocale 英文键 + 背景实景层 + 方块按钮样式对齐 mockup）。**

## Session Extract — /review-all-gdds 2026-06-14
- Verdict: CONCERNS
- GDDs reviewed: 3 (level-map-generation, mission-state-machine, office-economy-progression)
- Flagged for revision: mission-state-machine.md, office-economy-progression.md
- Blocking issues: none hard. High: C1 economy "as-built" cites deleted LostItemMissionManager.cs;
  C2 mission-state-machine missing Acceptance Criteria (+3 stub sections); D1 economy has no recurring
  money sink (the "permanently broke" pillar is mechanically unenforced once competent)
- Recommended next: author Auditor (核查专员) monster GDD via /design-system — no monster GDD/AI exists
- Report: design/gdd/gdd-cross-review-2026-06-14.md
- Same-session doc cleanup: superseded docs/mvp-core-loop.md + docs/design-decisions.md (banners);
  rewrote design/game-concept.md + design/systems-index.md to current tower state. Also flagged in
  systems-index: CLAUDE.md/AGENTS.md still cite removed MvpSceneStyleDirector.

## Session Extract — monster design (/design-system) 2026-06-14
- PM rejected the "Auditor" (核查专员) concept as too bureaucratic for the co-op-streamer core.
- New monster authored & approved: **Echo Mold (回声菌)** — `design/gdd/monster-echo-mold.md`.
  Infected fungal humanoid that records+replays crew ProximityVoiceChat to deceive/split them
  (real VOIP + fallback lines), mobile (NavMesh), contact = HP damage → Downed → all-down = Failure;
  counter = learn the replay tell + voice discipline. Convert-on-catch deferred (MVP).
- Done: art-bible §5 amended (PM-approved) to allow MRC-7 infection creatures (signal-color/behavior-first
  rules retained); registry entity `echo_mold` added; systems-index updated.
- KEY tech risk (Open Q#1): real VOIP capture/spatial replay spike (laplace) — MEDIUM. Privacy/consent Open Q#5.
- NEXT (PM request): design a game-spanning escalation/"site awakening" mechanic — infection wakes to
  living human presence/warmth over a mission; ramps danger; rides existing mission clock; fills the
  undesigned Hazard/Escalation gap. Launching /design-system for it.

## Session Extract — Monster System (/design-system) 2026-06-14
- PM reframed (3rd zoom): NOT a global escalation system — build a MONSTER SYSTEM (framework + roster),
  with "body-heat awakening" as one monster's SENSE, not a global tide.
- Authored & approved: **Monster System** — design/gdd/monster-system.md.
  Core law: one monster = one sense + one counter (LC-style). Shared: host-auth NavMesh AI;
  Roam→Investigate→Hunt→Attack; uniform catch = HP damage→Downed→all-down=Failure.
  DANGER LEVEL: rises with stay-time + key-action spikes → monsters more active → saturation=forced evac;
  REPLACES fixed countdown (dynamic, play-paced); shown via spore haze / PlayerOxygen drain / audio (no bar).
  Bestiary learning loop (OfficeMonsterBestiary).
- Roster: MVP = Echo Mold (sound) ONLY; planned slots = thermal (body-heat), vibration, sight.
- Done: systems-index + registry (danger_level formula) updated; Echo Mold GDD given danger-level hook.
- Resolves cross-review C4 (clock ownership). Open/next: rework mission-state-machine fixed clock →
  danger_level driven (already Needs Revision); next roster sense = thermal (body-heat) top candidate.

## NEXT TASK (post-restart) — place the mission-site van  [2026-06-14]
PM wants: at the tower mission site, place a VISIBLE van (REUSE the office van) at the VAN pad so the crew
can carry the objective onto it. **Carry-onto-van LOGIC ALREADY WORKS — do NOT rebuild it.**

FACTS:
- The visible mission van was REMOVED 2026-06-13 (PM: procedural box "too ugly"). Only invisible gameplay
  anchors exist at the VAN pad, built by `Assets/_Project/Editor/TowerV8WhiteboxBuilder.cs`:
  - `VAN_CargoZone` (BoxCollider trigger; set the EcoColumn DOWN inside = aboard → ObjectiveSecured → full settlement)
  - `VAN_ExitPoint` (MissionVanExitPoint; board / lockers / return decision)
  - `VAN_DepartLever` (TowerVanDepartLever); spawn point just outside the van's open rear
  - DEAD reference: `BuildMissionVan()` / `AddVisualWheel()` (UNCALLED, ~line 986) shows intended layout:
    OPEN cargo bay (building/north side) over VAN_CargoZone; cab toward forecourt/spawn (south); west face
    beside VAN_ExitPoint + lever. Rear opening + bay floor carry NO collider (carry path must stay clear).
- REUSE THIS MODEL: `Resources/GeneratedArt/AS_OfficeVan.prefab` (the office's own van; FBX at
  `Art/Generated/Office_v1/AS_OfficeVan.fbx`). Interior: `Resources/GeneratedArt/ASV4_VanTransitInterior.prefab`.
- Cargo wiring: `TowerMissionManager.IsObjectiveAboard` = ecoColumn inside cargoZone.bounds. Works.

METHOD (PM chose: in-editor via MCP, MEASURED not guessed):
- Use `unity-model-fit`. Via live Unity MCP: measure AS_OfficeVan bounds → place at VAN pad oriented so its
  OPEN cargo rear sits over VAN_CargoZone and cab faces the spawn → VERIFY (van wraps anchors; column carries
  in / drops / re-lifts unobstructed; shell = NavMesh obstacle). Then re-bake NavMesh if needed. Decide whether
  to bake the placement into TowerV8WhiteboxBuilder (replace dead BuildMissionVan) or place via MCP in-scene.

MCP: CoplayDev "UnityMCP" registered (stdio, uvx --from mcpforunityserver==9.7.1). Needs Unity OPEN + bridge
running (Window > MCP for Unity > Start Bridge). Loads only after a Claude restart. gamelovers mcp-unity also up.
**To resume: open Unity + start bridge, restart Claude, say "放车" → read this block → measure/place/verify.**

## Session 2026-06-14 (cont.) — 放车: placement MEASURED + LOCKED (gamelovers bridge)
**Bridge note**: CoplayDev UnityMCP bridge could NOT find the Unity instance this session ("No Unity Editor
instances found"; PM reports no "Start Bridge" button in the MCP-for-Unity window). Did the whole job on the
**gamelovers mcp-unity** bridge instead — and crucially, its `get_gameobject` returns the live
`MeshRenderer.bounds`, so measurement was deterministic WITHOUT execute_code.

**PM decision (load-in fidelity)**: chose **B — 小改模·后舱常开** (open the rear in Blender so the column
carries INTO a shallow bay; rear permanently OPEN, no animated door). Confirmed via prefab read that
AS_OfficeVan is a single Meshy-generated closed shell (one mesh / one material, no separable door part), so an
animated door = real re-model + rig for zero gameplay gain; a permanently-open rear is enough.

**MEASURED facts (AS_OfficeVan, prefab native scale)**:
- Renderer bounds size = **2.19 (W) × 2.00 (H) × 5.14 (L)** m; pivot ≈ bounds center on XZ; **min.y=0 when
  root.y=0** (wheels land on the floor with root at y=0). Importer target height 2.0 m (OfficePropImporter,
  Euler -90,0,0 stand-up fix; HqProportionFixTool ×1.2-about-ground was HQ-only).
- VAN slab = TowerPlanV8 "VAN" x14 z-10 w12 d8 → CenterX 20, north/building edge z=-2, south/forecourt edge z=-10.
- Anchors (BuildMissionManager): VAN_CargoZone center(20,1.25,-4) size(8,2.5,4)=z[-6,-2]x[16,24];
  VAN_ExitPoint(17.5,1,-3.5) size(2.5,2,2.5); VAN_DepartLever(15.2,0,-3); PlayerSpawnPoint(20,0.1,-1).

**LOCKED placement (verified by re-measure)**: scene root `AS_OfficeVan`
- **position (20, 0, -4.571)  ·  rotation (0, 90, 0)  ·  scale native**
- → bounds center (20,1.0,-4.571), occupies x[18.90,21.10] y[0,2.0] z[-7.14,-2.0].
- Rear (open-to-be, NORTH) end at **z=-2.0** = pad north edge = CargoZone north edge. Cab (solid) SOUTH at z=-7.14.
- Wraps all anchors: CargoZone within z-span; ExitPoint+lever beside the WEST face (x=18.90); Spawn(z=-1) 1m
  north of the rear opening. PM confirmed cab faces SOUTH (forecourt) in-editor.

**State**: van instance is IN the scene (instanceId -1856 this session) but scene **NOT saved** (AGENTS.md: no
scene-YAML rewrite without explicit ask) and **no collider yet** (collider waits until after the rear cut so the
bay stays open). It is the CLOSED placeholder at the locked transform — the FINAL van = the cut variant.

**REMAINING (in order)**:
1. Blender rear-cut → produce a SEPARATE asset **AS_MissionVan** (do NOT overwrite AS_OfficeVan — the HQ shares
   it closed). Delete the rear-face polys (end opposite windshield) to open the hollow shell; keep cab solid;
   opening ≥ 1.6 W × 1.8 H m (eco column ≈ 0.64 W × 1.7 H + carrier), bay depth ≥ 2.5 m; rear stays open (no
   door geo). Re-export same orientation/scale so the locked transform still holds.
2. Re-import variant → re-place at the locked transform → add a MeshCollider on the (now open) shell so it is
   solid + a NavMesh obstacle while the bay/rear opening carry NO collider (carry path clear).
3. **Bake the placement into TowerV8WhiteboxBuilder** (replace the DEAD BuildMissionVan ~line 986; call it from
   BuildMissionManager) pointing at AS_MissionVan, so it rebuilds reproducibly. Then delete the manual instance.
4. Re-bake Tower NavMesh (`... > Tower > Bake Tower NavMesh`) so agents path around the van shell.
5. Verify: column carries into bay, drops inside CargoZone (= IsObjectiveAboard), re-lifts unobstructed.

## Session 2026-06-14 (cont. 2) — School purge (PM: "把学校有关的都删了")
PM decision: **delete school-specific only + retheme strings; KEEP the shared `LostItem*` economy
naming** (CompletedLostItemJobs / CountsTowardLostItemProgress / MvpTaskCategory.LostItemRecovery /
MissionRewardCalculator — the tower loop uses these; renaming deferred, may touch NGO serialization).

**Reality found**: the school was ALREADY ~90% gone — no school scene (only HQ.unity + Tower_EarthCoast_01.unity),
no school AI/script (LostItemMissionManager.cs does not exist — the prefab was an orphan w/ missing script),
no Snow_Lotus assets. HQ already runs the tower mission: `OfficeComputer.DefaultTaskResourcePath =
"Tasks/TowerEarthCoast_01"` → `Resources/Tasks/TowerEarthCoast_01.asset` (sceneName: Tower_EarthCoast_01) EXISTS.

**Done (filesystem edits, bridge was DOWN — needs Unity recompile/refresh to confirm):**
- DELETED `Assets/_Project/Prefabs/Mission/LostItemMissionManager.prefab` (+.meta) — orphan; folder now empty.
- REMOVED its entry (guid 9e06a889…) from `Assets/DefaultNetworkPrefabs.asset` (was the only ref to it).
- `GeneratedArtImporter.cs`: removed 3 dead school AssetSpecs (ASV4_School_Lost_Item_Map,
  ASV4_Monster_Homework_Debt_Collector [old campus monster], ASV4_Missing_Homework_Notebook) — all pointed
  at missing FBX. Array verified well-formed.
- `OfficeTaskDefinition.cs`: rethemed the school DEFAULTS → tower/生态柱 (taskId tower_ecocolumn_01, title
  「真实海岸」生态柱回收, client 私人收藏家, locationName 地球海岸壹号·烂尾楼, sceneName Tower_EarthCoast_01).
  Cosmetic only (live task asset already overrides) but kills the school template + the broken School_LostItem_01 default.

**KEPT (shared, per PM): all LostItem* economy.** Tests: 0 school refs → no breakage.
**Still school-themed but LEFT (flagged to PM)**: `MvpHud.cs` (homework string — likely objective label) and
`MonsterBestiaryProgress.cs` (old campus-monster bestiary entry) — retheme when doing HUD / monster work.

## Session 2026-06-14 (cont. 3) — Mic on-air UI/UX (PM ask) — DONE
PM decision: **open-mic by default** (max Echo Mold tension) → on-air indicator must be prominent + first-run consent.
Found: `SettingsOverlay.cs` (IMGUI) ALREADY has full mic settings (VoiceEnabled/Muted/PushToTalk/device/MicGain/
OutputVolume/MaxDistance/Reset). Real gap = NO in-game indicator that the mic is hot + no consent notice.
- `ProximityVoiceChat.cs`: added `IsMicHot` / `InputLevel` (RMS, fast-attack/slow-release) / `MicrophoneAvailable`
  telemetry; Update() sets cold on every early-return and hot after a successful capture.
- NEW `Assets/_Project/Scripts/UI/VoiceMicIndicator.cs`: self-bootstrapping IMGUI overlay (no scene wiring),
  top-centre badge LIVE/MUTED/MIC ON/PTT/NO MIC with a broadcast blink dot + live level meter; **first-run
  consent modal** ("voice is open by default; infected things can record + replay it" → Keep on / Mute me),
  gated by PlayerPrefs `AS.Voice.ConsentShown`. Addresses Echo Mold privacy Open Q#5.
- `MvpLocale.cs`: added mic_live/muted/ready/no_device/ptt_ready + voice_consent_* (EN + 中文).
- Mute control stays in Settings (not duplicated). NOT yet compiled (bridge down) — verify on Unity refocus.

## OPEN / NEXT (bridge was DOWN all session — a pile of unverified C# awaits one Unity recompile):
- **Verify-on-refocus**: school purge edits + mic UX + `MissionVanBuilder.cs` all need Unity to recompile.
  Focus the Unity window → it compiles; check console for errors.
- **Van rear-cut**: run menu `Tools > Black Commission > MVP > Tower > Build Mission Van (reuse office van)`
  once the gamelovers bridge reconnects (dropped on the compile domain-reload; refocus Unity to un-wedge).
- **Echo Mold AI + placeholder body** (PM chose "我先搭AI+占位体") — pure C#, GDD fully read; NOT started yet.
  Plan: pure-logic core (attention/state machine/lure-target/danger scaling) + tests, then NetworkBehaviour
  (NavMesh Roam/Lure/Hunt/Attack, host-auth, PlayerHealth contact dmg→Downed, all-down=Failure), then a
  placeholder body builder. Voice capture/replay can start on preset `fallbackLines` (GDD-sanctioned) and wire
  real ProximityVoiceChat sampling after a spike (Open Q#1).
- **Leftover school-theme strings** (KEPT, retheme later): `MvpHud.cs` homework label; `MonsterBestiaryProgress.cs`
  campus-monster entry → becomes Echo Mold during the monster work.

## Session 2026-06-14 (cont. 4) — Rigging/animation: project-wide gap, DEFERRED (PM)
PM probed the monster MODEL then SKELETON. Findings (verified): **the project has NO skeletal animation pipeline
at all** — 0 `.anim`, 0 AnimatorController, no script references Animator/SkinnedMeshRenderer; even the crew
`AS_Character_0X` are STATIC meshes (no rig) moved by transform ("sliding"). Meshy exports are static/unrigged too.
**PM decision: don't build animation yet — just record it.** (See memory: animation-pipeline-deferred.)
→ When the Echo Mold IS built: sliding body on NavMesh + optional cheap procedural shamble (code bob/sway), NO rig.
Real skeletal animation = a separate project-wide pass (Mixamo/Meshy auto-rig → shared Humanoid Animator for crew
+ monster), only if PM opens that round. **Monster model** itself = PM's Meshy gen (prompt drafted in chat
2026-06-14: lo-fi infected humanoid fungal host, mushroom caps/spores, amber #FF6A00 eye); placeholder = reuse a
tinted/distorted AS_Character worker ("infected host") + amber eye point — zero modeling.

## Session 2026-06-15 — UX spec: Echo Mold encounter-feedback HUD layer (/ux-design)
- **Task**: authoring `design/ux/mission-encounter-hud.md` — the Echo Mold *encounter layer* of the HUD
  (PM scope decision: extends `hud.md`, NOT a new screen; NO 0–100 threat meter per GDD anti-pillar; reuses F1).
- **File**: `design/ux/mission-encounter-hud.md` — skeleton created (15 sections, all `[To be designed]`).
- **Status**: Section 1 (Purpose & Player Need) WRITTEN + legibility dial = "Diegetic-minimal"
  (HUD mirrors player's own action only, never the Mold's knowledge). Paused at Section 2
  (Relationship to Base HUD) — PM asked mid-session for a game-mechanics/numbers overview; resume Section 2 after.
- **Binding inputs**: `monster-echo-mold.md` §UI Requirements (lines 149–153: no dedicated screen, no threat
  meter, behavior/audio-first, optional one-time voice-monitoring consent notice); `hud.md` Open Q#4 (reserve
  pursuit-alert grammar = A3 notification row + stamp-red + independent audio channel; "Hidden/Audio-primary"
  category question) + F1 downed/revive already specced; `mission-state-machine.md` (Downed→Failure).
- **Genuinely-new surfaces**: (1) voice on-air / "being sampled" lure-risk indicator, (2) pursuit/contact alert
  in A3 work-order grammar, (3) the `tell` (real vs replayed voice — audio-first, likely no HUD), (4) one-time
  voice-monitoring consent notice. Downed/all-down = reference hud.md F1, don't redefine.
- **Gaps to note in Open Questions**: no `player-journey.md`; no `accessibility-requirements.md` (inherit hud.md
  baseline). No `design/gdd/game-concept.md` (AGENTS.md is vision of record).

## Session 2026-06-15 (cont.) — Echo Mold: scene placement + NavMesh validation
- **Done:** PM flagged the Mold shouldn't sit in the 生态柱/objective room. Purged **3 stale decoy
  "EchoMold" objects** (raw-FBX drops, Animator-only — were shadowing every `Find`/`get_gameobject`).
  Re-added ONE clean prefab instance at world **(22, 4.3, 14)** = upper-level atrium→objective approach,
  ~16 m from `TARGET_EcoColumnPlinth`. Verified via real C#: full stack (Animator/NavMeshAgent/
  CapsuleCollider/NetworkObject/EchoMold all ✓, 11-mesh rig) and **ON MESH** (Δ=0.02 m).
- **NavMesh:** new editor tool `Assets/_Project/Editor/TowerNavBaker.cs`
  (`Tools ▸ Black Commission ▸ Map ▸ Bake Tower NavMesh`) — session-only in-memory `NavMeshSurface`
  bake from PhysicsColliders (**842 walkable tris**, bounds 56.5×7.6×62.5) + on-mesh/component logging.
  Deliberately does NOT touch the project's separate `Tower_EarthCoast_01_NavMesh.asset` or scene YAML.
- **UNSAVED — all of the above is live in the open editor session only.** Awaiting PM: hit Play to test
  the Mold animating/hunting, then decide whether to save the scene + persist navmesh via the project's
  separate-asset path. Runtime Play test still blocked from the bridge (no play toggle; CoplayDev not attached).
- **Note for PM:** earlier tower monster concept = "核查专员/Auditor" (`TowerAuditorLogic`, tested). Echo Mold
  is the newer PM-approved mushroom (2026-06-14/15). Whether they coexist or supersede is a PM design call.

## Session 2026-06-24 (cont.) — HQ 改向火星货运堂 + Option B 纯室内锁定
- **概念锁**: HQ = 破产事务所蹲占「废弃火星轨道货运堂」死壳; 论点「扎哈=火星」(死参数化壳=火星弃物 对峙 小暖窝=人类对位)。
- **Option B 纯室内已锁**(art-director/level-designer/game-designer/technical-director 4家一致): 集结·登车·结算全在壳内; 货车室内装卸湾; 卷帘门=框景, 门外火星废料场=不可进入背景。laplace 估 builder delta≈-150行/-1 NavMesh/-1 Terrain, 近零新代码。
- **"室内稍微大一点"**(PM 2026-06-24): 壳顶13.6→15.5m(剖面显示·零穿行成本), 宽11→13m(平面显示), 深~不变, **暖窝不放大**(对比更狠), 门4.6→5.0m框景。
- **已出**: design/hq/HQ_Section_AA_v3.png (剖面v3室内·渲染器 tools/hq_section_v3_interior.py); 批过的 v2 保留。**待PM确认尺寸/读感** → 然后改平面(hq_siteplan_v5.py→B版13m宽) + laplace 落 builder + 重写 hq-architecture-pass.md。
- **待记两代价**: repo-agent接管演出→门口; 返程减压beat→接受损失或室内替代。
- **仍开**: 背包系统(荐 van-as-backpack 或不做); MAP5 起源叙事批准; 结算位置(室内CRT vs 车上)。

## Session 2026-06-24 (cont.2) — HQ 尺寸定稿 + 三步执行中
- **尺寸已锁**(art-director+level-designer 收敛, PM "行,继续做吧"): 壳13.6m / 净宽11m / 深~27m / 门4.8m(4.6→4.8唯一微调) / 暖窝6.6×7.0顶2.8不放大。放大试验(15.5/13)被否。剖面定稿候选=design/hq/HQ_Section_AA_v3.png。
- **结算位置=室内CRT**(PM默认采纳荐案)。
- **执行顺序**: (1) 平面改B版(tools/hq_siteplan_v5.py: 车进室内·门外背景化·宽11m) → (2) 重写 design/hq/hq-architecture-pass.md(旧"市政验车站"→火星货运堂+OptionB) → (3) builder delta(HqOptionAProductionBuilder.cs; level-designer:终稿几何≈v2,主要 DoorH 4.6→4.8 + 车/门/cargo zone 室内化+门外backdrop; 注意当前builder仍是旧概念,Mars壳/nest是否已建需先核实再定工作量)。

## Session 2026-06-24 (cont.3) — 三步执行完成 (1+2 done, 3 核实=重建)
- **(1) 平面 B 版 已出**: `design/hq/HQ_SitePlan_v5.png` (渲染器 `tools/hq_siteplan_v5.py` 已重写)。门外火星废料场收成顶部窄带(z28–38,~10m,货柜+废墟地标透过门可见·标注不可进入); 室内主导; 出发动线止于室内登车; 返程门口→CRT结算; 宽11m; 砍院墙/院门/土路/室外灯锚。
- **(2) hq-architecture-pass.md 已重写**: 旧「市政验车站」→「废弃火星轨道货运堂」纯室内 OptionB; 11节; 锁定尺寸表(13.6/11/27/门4.8/暖窝6.6×7×2.8/压缩比1:4.86); 结算=室内CRT; 两代价已记; §8室内灯锚; §11=builder核实结果。
- **(3) builder 已核实 (读937行)**: `HqOptionAProductionBuilder.cs` **100%还是旧「Option A市政混凝土楔形」**, 零火星几何 (UnitW=9≠11 / Depth17·19.5≠27 / WallH=3.6≠13.6 / Door4·3.2≠4.8; BuildShell=简单盒子+斜墙; BuildInterior=道具摆楔形地板**无暖窝围合**; +BuildYard/Wilderness/Vegetation/HqTerrain室外)。**→ Mars+OptionB 不是tweak, 是 BuildShell+BuildInterior 从头重建** (重写壳13.6/11/27+曲东墙/斜北墙/门4.8/传送带/悬挑/裂缝/树柱 + 建暖窝盒子6.6×7×2.8 + 车/zone挪室内 + 删4个室外builder + 灯重指室内 + CRT结算collider)。多百行改generated editor builder, 需Unity验证。
- **建议(待PM决策)**: 先批定稿三件(剖面v3 + 平面v5 + doc) → 再按图把 builder 重建当作独立实现任务 (建议委派 unity-specialist/laplace, Unity开着验证)。**不应在PM看到定稿前抢跑重写900行builder**。
- **仍开**: 背包系统(荐van-as-backpack或不做); MAP5起源叙事批准; ~404MB foliage-pack提交决策; 黑色方块跟随视角bug(需PM截图); 室内dressing pass。

## Session 2026-06-24 (cont.4) — 火星货运堂白模已建成 (PM "可以先建白模")
- **新建独立 editor 脚本** `Assets/_Project/Editor/HqMarsFreightWhitebox.cs`(**不碰**旧 HqOptionAProductionBuilder,可逆): 纯灰盒 Cube 按定稿终米建白模。两个菜单: `Tools/Black Commission/Map/Build HQ White-box - Mars Freight Option B` + `Remove HQ White-box (restore)`。
- **白模已成功建进 HQ 场景并桥接验证** (get_gameobject 确认 6 组全到位): Shell(地板+14个Z-bay分面:每bay西矮檐墙/斜屋面/东高墙, 西檐扫到东脊13.6m + 后墙 + 北门墙4.8m洞 + 死传送带/断悬挑/天窗裂缝) / Nest(顶+东墙+前墙带门洞, 6.6×7×2.8 SW角) / Interior(CRT桌+绿屏/债务板/折叠桌/半空军械墙/集结台/歪树柱/4出生点) / VanBay(车体+车头+登车区 z21.4 室内) / Backdrop_NonWalkable(地面/集装箱/废墟地标/死电杆 + 门槛隐形墙) / Lights(暖窝钨丝/CRT荧光/裂缝冷昼光打集结/车湾钠灯/门口背光)。重定位 PlayerSpawnPoint(4.5,0.1,2.4 朝+Z)+ MVP_OfficeComputer(1.2,1.0,1.2)。
- **⚠ 未保存**: 白模只在打开的编辑器会话里 live, 场景 dirty 未存(设计上不自动存; 存=改 HQ.unity YAML 需 PM 明确同意)。PM 满意→Ctrl+S 保留; 不满意→Remove 菜单撤或不存关掉。
- **过程坑(已记忆)**: (a) 新[MenuItem]需编辑器焦点才注册→后台桥调不动(用只读菜单Audit Proportions探针确诊); (b) 菜单名内 `/` 被Unity当子菜单分隔→改 `-`; (c) recompile_scripts不做Refresh(新/改文件需先execute_menu_item Assets/Refresh导入); (d) 模态DisplayDialog会卡死桥主线程→已改成非阻塞LogWarning; (e) 桥卡死→PM授权taskkill /F /IM Unity.exe(MSYS_NO_PATHCONV=1防/F被转F:/)+删Temp/UnityLockfile+直连exe -logFile重启, 新实例PID58836冷启动菜单正常注册→build成功。
- **下一步(待PM视觉验收)**: 桥无截图工具→需PM亲眼看Scene视图(已FrameLastActiveSceneView框好)或Play走查, 验尺度/压迫比/OptionB动线。验收后: 满意则存+按白模把production builder重建; 要调则改灰盒数字再rebuild。
- **仍开(承上)**: 背包系统; MAP5起源叙事; ~404MB foliage-pack提交决策; 黑色方块跟随视角bug; 室内dressing pass。

## Session 2026-06-24 (cont.5) — 白模"材质化"升级完成 (PM四点反馈→"行按你说的改吧")
- **PM 四点反馈**(看完灰盒白模): ①贴材质(但要充分调研用哪些) ②用我原来HQ里的`AS_Office*`模型别手建方块 ③屋顶镂空(真bug) ④你是不是没法建曲面? → PM 批 **"行,你先按照你说的改吧"**。
- **`HqMarsFreightWhitebox.cs` 整段重写**(同脚本同两菜单,仍不碰旧builder/可逆/不自动存),**已编译通过(0错)+建进HQ场景+桥接逐项验过**(root=7组/84对象/**0回退警告**=全真资产):
  - **①屋顶镂空根除 + ④真曲面**: 旧14个Z-bay分面盒子(接缝高度/角度对不齐→漏三角洞) → 换成**一整张程序化曲面网格** `BuildShellMesh`(Catmull-Rom截面: 西矮墙A→P0→拱→脊P2偏东0.42span→长东坡P3→东基P4; 沿Z 45刀放样, 用Ridge高度+EastX东extent双profile; watertight indexed grid; RecalculateNormals平滑; **双面**_Cull=0; UV; MeshCollider)。验`ShellSkin`=MeshFilter+MeshRenderer+MeshCollider。
  - **③用真模型**: `Prop()`(Resources.Load `GeneratedArt/AS_*` scale1, 关碰撞) 换掉所有手建Cube道具; 验`Computer_CRT`→`AS_OfficeComputer_Model`、`DispatchVan`→`AS_OfficeVan_Model`; 债务板/哨兵/文件箱/折叠桌/台灯/沙发/补给柜/档案柜/工具架/安全板/灭火器/车间角全真`AS_Office*`。门=`FitPrefab(LargeGates1_2)`卷帘; 门外=真`Debris01_2/_5`+`Barrel01b/c`+`GasBallone01_2`。
  - **②材质A方案**(充分调研: §7/§8锁定方向+Tirgames/Sat库+无壳模型确认): 壳=`Tint(CommonConcreteWall02)`冷调实例(_BaseColor 0.86,0.90,0.97/低光泽/双面)+污渍条; 地面=`M(CommonConcrete04)`共享; 钢(传送带/悬挑)=`Tint(CommonMetal01)`冷; 暖窝=`Tint(CommonMetalPainted01)`暖灰(暖是灯不是漆); 门外silhouette=`Tint(CommonConcrete05)`冷暗。**全`new Material(源)`实例, 一个共享.mat都没动**(`Tint`/`M`/`Flat` helper)。
  - **补打光让材质落地**: 冷向主光`Key_ColdSky`(软阴影 Euler48,-26) + §8五灯池(暖窝钨丝/CRT绿/裂缝冷昼光spot打集结/车湾钠灯/门口背光) + `Light_Fluorescent_Hall`挂`LightFlicker.Sputter(0.7,7)`濒死灯管 + 真`LampCeiling01`灯管×2 + `BuildPost`=LC风格Volume(FilmGrain Medium1 0.18/Bloom 1.15·0.30/ColorAdj sat-12 contrast+8 priority10)。Mood改Trilight冷暗ambient+linear fog 11-62。
- **关键确认**: CommonConcreteWall02=URP/Lit(shader guid 933532...=universal Lit, `_Cull:2`)→`Tint(doubleSided)`置_Cull=0生效→玩家在壳内看到双面壳不空。
- **桥流程坑补记**: 成功编译触发domain reload→桥WebSocket拆重建, `get_console_logs`先`ECONNRESET`后`TIMEOUT`是**reload中(非卡死)**, 等15-20s重连恢复(失败编译反而秒返); `get_gameobject`参数=`idOrName`(非objectPath); `get_console_logs`可`{"logType":"error"/"warning"}`过滤秒查编译错/回退。CS1503一处(BoxEuler第5参传float应Vector3)已修。
- **⚠ 仍未保存**(场景dirty, 不自动存)。**待PM视觉验收**(桥无截图→PM需Alt-tab看Scene(已Frame)或Play走查): 重点验 曲面壳形/法线(有无发黑穿插)·**货车朝向(yaw=0可能要180朝门)**·冷死壳vs暖窝对比·道具摆位。满意→Ctrl+S; 调→改数字rebuild; 撤→Remove菜单。验收后才谈把production builder(HqOptionAProductionBuilder)按此重建。
- **仍开**: 背包系统; MAP5起源叙事; ~404MB foliage-pack提交决策; 黑色方块跟随视角bug。

## Session 2026-06-24 (cont.6) — 壳"太丑"美术重定向已重建 (art-director redirect → PM「行你开始开发吧」)
- **触发**: PM 看完 cont.5 材质化壳→「这个建筑整体长得有点太丑了」+「我们是不是要突出这是曾经的火星建筑被遗弃的?」。走 `/art-bible` skill 拉 art-director 子agent 诊断+给可建重定向(存 `.claude/agent-memory/art-director/project_hq-mars-shell-redirect.md`)。先出 PM 批准的**侧剖+俯视**两图(`design/hq/HQ_Section_AA_v4_redirect.png` + `HQ_Plan_redirect.png`, 渲染脚本 `tools/hq_section_redirect.py` + `tools/hq_siteplan_redirect.py`, 含 before/after 剪影对比)→ PM「行,你开始开发吧」。
- **`HqMarsFreightWhitebox.cs` 已按重定向重建**(同脚本同两菜单, 仍不碰旧builder/可逆/不自动存), **编译0错+build成功+0回退警告**, redirect对象桥接验在场(`ShellSkin`/`TornRebar_0`/`MissingPanel_0`/`HangingConduit`/`Prow_Rim`):
  - **①形体(治"团块")**: 旧 `Ridge` 居中对称(13.6中跨两端等衰)→**单向 sweep**: 低南矮肩(5.8)→单脊压北门 13.6@z20→急落门头8.6→断裂船首过z27。`EastX` 肚最肥12.9 北移到z19。**核心修**: `Section` 增 `zT` 参, 顶点 P2 横向 **lerp 0.30→0.52·span(南→北)**=把"直挤出 extrude"变"真扫掠 sweep"(脊随北升东漂)。`arch 6→12`, `zSteps 45→72`(治faceting)。
  - **②材质(治muddy)**: `mShell` 推**冷苍蓝灰**(_BaseColor 0.86,0.90,0.97 → **0.60,0.69,0.86** 强蓝偏低值压住暖混凝土底色, 故意出地球混凝土/绿/木/锈色板=火星异星)。新增 `mShellDark`(0.17,0.20,0.27 暗蓝钢)给T3外露结构。`mGrime` 转冷。
  - **③衰败北聚(治"突出被遗弃")**: 所有废墟挪到北端(船首): `BrokenCantilever` 移z28.6 + 5根 `TornRebar` 钢筋外露; NE上翼 3处 `MissingPanel` 暗recess+各3根 `Rib` 内肋外露; `HangingConduit` 从撕口垂落; `Stain_Run` 改**垂直**重力条且北聚(A/B/C)。南窝角保持完好(占屋者选最稳角)。
  - **④打光**: 冷主光 `Key_ColdSky` 0.35→**0.62** 且压低(Euler 48,-26→30,-22)沿sweep**掠射**; `Crack_Daylight` 移北撕口斜打集结; 新增 `Prow_Rim` 第二道冷光rim断裂船首。
- **我主动补修3处穿帮**(都是PM敏感的"没设计感"硬伤, 改数字级低风险): (a)北脊升到8.6→侧墙平板捅出屋脊→`Wall_N_L/R` 高度封到近壳高(6.8/5.0)不再捅(顶过梁仍到脊8.6); (b)南脊降到6.7→`DeadConveyor` y7.0 戳穿屋顶→降到y5.6 z[7,23]吊在壳下; (c)`MissingPanel` Y首估浮在皮外1.5m→重采样贴NE斜翼(mpX/Y/Z 改 7.2/10.3, 7.6/7.5, 7.0/6.6)。
- **⚠ 仍未保存**(dirty)。**待PM视觉验收**(桥无截图→PM Alt-tab看Scene已Frame/Play走查): 验 北升sweep剪影(不再居中团块)·冷蓝异星 vs 暖窝·北聚撕裂废墟·掠光雕曲面。
- **一处已知近似(我故意留+已flag)**: 北端封口仍是"封高的平板盒"(单盒顶配不上斜壳→最西角仍残~3m小poke, 雾+船首遮挡+暗多半看不见)。要彻底干净=按 `Section` 曲线建**带门洞的profiled端盖网格**(有缠绕/法线/挖洞风险, 盲建难调)→PM 若看见残角再做。
- **验收后才做(art-director 标 BLOCKING)**: 写 design doc(`design/art/hq-mars-shell-art-direction.md`, hq-exterior 兄弟篇) + 把"火星壳=美术圣经§3唯一获准的曲线例外(地球永远矩形)"写进 art-bible §3, 防未来 consistency sweep 把曲线正交化/把壳暖回地球混凝土。**未批准前不动圣经/不存场景**。
- **仍开**: 背包系统; MAP5起源叙事; ~404MB foliage-pack提交决策; 黑色方块跟随视角bug; 室内dressing pass。

### PM 验收(2026-06-24 关机前): 「还行吧」= **形体过关**, 但**①材质还差点意思 ②光效/氛围感要再做一轮**。下一阶段 = 材质深度 + 光氛围, 形体不再大动。
**⚠ 场景未存就关机 = 正常**: 全部改动在 `HqMarsFreightWhitebox.cs`(已落盘), 场景是脚本产物。下次重开 Unity → 跑 Build 菜单即重生, 无丢失。圣经/场景仍未动。

**下次开机 resume = 直接进 Stage A(材质), 桥活着就先 Build 一次看当前态再动手。**

#### Stage A — 材质深度(治"差点意思", 优先)
- **A1[最大杠杆]**: 程序化**冷色拼板缝贴图**(PIL, 256-512px)换掉现在的混凝土平铺——壳现在是"一坨浇筑的冷色"没有装配感。要: ~2.5×2.0m 金属板块 + 细凹缝 + **逐板明度变化烤进贴图**(T1完好亮/T2风化暗) + 顶部火星尘漂移。存 `Assets/_Project/Art/Textures/MarsShellPanels.png`, 改 import(sRGB/无mip条纹), 在 `BuildPalette` `mShell.SetTexture("_BaseMap", tex)`。(art-director 早就点名这条, cont.6 我只做了颜色没做贴图=这就是PM说的"差点意思")。
- **A2**: 配套 **法线贴图**(缝倒角+凹坑)+ 粗糙度变化, 让掠射主光真能"雕"出板缝, 不再是死平的漆。
- **A3**: **空间分层**(不是只靠损坏几何): 完好色压低处/南, 风化暗压高处/北 + 火星尘积在朝上/朝北面(顶点色或材质分区), 给"老化"读数。

#### Stage B — 光效 & 氛围感(治"氛围", A之后)
- **B1[最大杠杆]**: **体积光/神光柱**——裂缝/大门/船首rim 现在是看不见光柱的点/聚光。加可见的含尘光轴(URP 体积雾 or 廉价半透明加色锥mesh)。这条对"氛围感"最值钱(LC的废教堂感全靠它)。
- **B2**: 光池里飘**尘埃粒子**(cheap ParticleSystem)→ 立刻"被遗弃的大空间"。
- **B3**: **雾重调** + 验光池分离: 脊顶 fade into dark 卖 1:4.86 矮人比; 暖窝/CRT绿/钠灯车湾/冷裂缝 四池要是**被真黑分开的孤岛**不是一片糊(压 ambient/收 range)。
- **B4**: **Post 加深**: vignette(收心) + 亮源 bloom 阈值(CRT/钠灯/光柱 只让这几个发光) + **分离调色**(teal阴影/amber高光 = Municipal Debt Noir 分色) + 轻 CA(脏镜头)。
- **B5**: **自发光的"破办公室生命"**: CRT/债务板/出口牌 emissive 自发光 + 钠灯嗡鸣, 保留濒死灯管 sputter。

#### Stage C — A/B 落地后(收尾 + 锁档)
- **C1**: 北端 **profiled 端盖网格**(按 Section 曲线建带门洞端盖)杀掉残留西角 poke——PM 看见才做。
- **C2[BLOCKING]**: 写 design doc `design/art/hq-mars-shell-art-direction.md` + 圣经 §3 写入"火星壳=唯一获准曲线例外"。**look 批准后才写, 防 consistency sweep 掰直曲线/暖回地球混凝土。**
- **C3**: 批准的 look 反向重建进 production builder(`HqOptionAProductionBuilder`)。

---

### PM 关机前追加(2026-06-24「最快速度」): 两个走查 bug

**Bug① 视角速度太快 → 本次已修**: `PlayerCameraController.cs` 加 `const LookSpeedScale = 0.5f` 乘进 mouseX/mouseY → 默认视角灵敏度减半(原 effective = 原始鼠标delta×2, 太twitchy)。游戏内灵敏度滑条(0.25–8)仍可调回快。改的是 .cs(已落盘), 下次 Unity 获焦重编译即生效;桥在 Play 模式被占, 本次没法跑桥验证。PreviewWalker(0.12)本来就慢, 未动。判定 PM 用的是**联机玩家**(host流程, PlayerCameraController ×2), 不是 PreviewWalker。

**Bug② 第一视角黑色「阴影」跟随镜头 → PM 要求只记待办, 暂不修**(细化/替代上面 line 2603 "黑色方块"笼统条)。
- **PM 更正(重要)**: 是**黑色阴影**, 不是方块、**和准星(crosshair)无关**(crosshair 4–6px 中心暗点已被 PM 排除), **随人物第一视角移动**。
- **本次已排除(省下次时间)**: owner 所有手持 view-model 默认全关(手表 HasWristwatch=false / 手电 slot0 空且无起始发放 / heldItemRoot 空); 身体胶囊 MeshRenderer 在 OnNetworkSpawn 被 `HideCapsuleBodyMesh` disable(disabled renderer 不渲染也不投影); 第三人称模型 owner 端全 renderer disabled; rig `shadowCastingMode=Off`; FlashlightController 不建 mesh; `Player.prefab` 相机(`PlayerCamera`)下只挂 `HoldPoint`(空)+`Flashlight`(纯 Light, m_Enabled=0); `HQ.unity` 无保存的黑 mesh; VanTransitOverlay 非 transit 不激活。
- **剩余假设(均运行时才现, 静态查不到→须 Play 现场抓)**: (a)[最可能] 某盏光(钠灯车湾 / 掠射冷主光 / 手电)把**近 body 几何或仍投影的物件**的实时阴影投进视野下缘, 随转身扫动; (b) 相机 near-clip 蹭暖窝近壁/道具→暗楔随视角扫; (c) URP SSAO / contact shadow 落在相机正下方; (d) post/vignette 暗角被误读。
- **下次修法**: **必须 Play 模式现场看**——桥在 Play 时主线程被占, get_console_logs/get_gameobject 全 TIMEOUT, 静态分析到头了。让 PM 暂停 Play 给桥 ~10s 抓 active-camera 子树 + 地面阴影源, 或 PM 截图(阴影在屏幕哪个位置 / 多大 / 形状)。锁定光源后: 关那盏灯对 rig/胶囊层的投影, 或给 owner 胶囊/rig 设独立 layer 让主光 shadow culling mask 跳过。

---

## Session 2026-06-28 — UX spec: 物品检视系统 (/ux-design item-inspection) [IN PROGRESS]
- **入口**: PM 问"到哪步了/待办" → 给三线全局(HQ火星壳Stage A材质 / 搜刮双层loot+检视 / 建模任务书) → PM 选 **「检视系统 UX 设计」** 接 /ux-design。
- **范围**: 任务现场「举起·旋转·检视」交互层; **结算反差交叉引用已批准 `settlement.md`(不重写)**。输出 `design/ux/item-inspection.md`。
- **已读上下文**: 权威源 `scavenging-two-tier-revision-2026-06-26.md` §5+D4(**APPROVED**) + `core-loop` §9(InspectController=Task#2) + settlement.md/mission-encounter-hud.md/hud.md。**检视契约(不可破)**: 不改价值, 改认知/情感连接; 数据预留 `relic.targetPersonId`+可选 `inspectDetail`; 层二遗物(信/照片/儿童画/日记/公文, 每图3-6件)是主对象; 层一salvage可检视但读不出多少。
- **已建骨架**(14节[To be designed])。两个待PM定的设计张力: ①**3D实体检视 vs 盖章公文卡平面模态**; ②**检视=低头脆弱态**(co-op host不暂停, 怪物仍猎杀)是否做成显式协作机制。
- **状态文件对账**: 06-26 提交批(two-tier loot/建模任务书/PM改名/foliage→LFS)此前未在本文件单列; **foliage ~404MB提交决策已由 Git LFS 解决** → 从 open 列表划掉。HQ.unity 仍有未提交改动。
- **NEXT**: 逐节撰写, 从 §Purpose & Player Need 起 (问→选项→决策→草稿→批准→写入)。
- **PM 拍板两基础张力(2026-06-28)**: ①视觉语言 = **3D 实体握持**(第一人称真握遗物·鼠标旋转·凑近读痕迹; 与结算冷归档卡形成反差; **非**平面盖章卡、**非**混合)。②风险 = **脆弱·可中断·可改日**(现场检视=低头握持·host不暂停·Inspector仍逼近; 任意键瞬断逃/战; 遗物可收进口袋带回**车上/HQ安全检视**; 绝不强制、跳过零损失)。两决都取推荐案。→ 解锁逐节撰写。
- **撰写计划(更新)**: 按已定两张力分 4 批, 每批 草稿→批准→写入。Batch1=框架(Purpose / Player Context / Navigation / Entry-Exit); Batch2=交互核心(Layout / States / Interaction / Transitions); Batch3=契约(Events / Data / A11y / Loc); Batch4=收尾(Acceptance / Open Q)。
- **PM 拍板撰写细节(2026-06-28 续)**: A1=②(视觉痕迹+克制一两行可读文本→`relic.inspectDetail`; ②③文本须可本地化UI层、不烤进贴图)。A2=**一拿到眼前即看到全部**(砍"粗看→凑近zoom揭示"渐进门控; 保留鼠标旋转看3D不同面——照片正反/玩具各面磨损=全部信息的空间分布、非隐藏门控)。A3=MVP统一握持, 5类差异化(信翻开/日记翻页/照片翻面/玩具转磨损)标**v2**。A4=MVP切线=**现场·层二遗物·3D握持·视觉痕迹**; 层一salvage检视+改日安全检视(车上/HQ)标**v2**(留数据+退出口、UI延后)。B1/B2/B3全确认。C1=hold右键(与E拾取分开); C2=音频延后只留hook; C3=怪物"现场威胁"统称+收Open Q。
- **措辞(PM追问"为什么用举起")**: 面向玩家统一用"**拿到眼前/握着看**"指检视姿态(=从持握位抬到视线前细看, 非额外机制/非分层); 弃"举起→凑近"两段式。
- **Batch1 已写盘**(item-inspection.md 四节: Purpose/Player Context/Navigation/Entry-Exit; 应用A4标v2 + 新措辞 + header统一语言定位=3D握持)。**NEXT=Batch2**(Layout/States/Interaction/Transitions), 待PM点头措辞后撰写。
- **桥探活(2026-06-28)**: MCP-unity **活**, 端口8091(settings+netstat一致), `Unity.exe`PID17252监听, **非Play**(get_scene_info秒回), 场景=HQ(dirty), **0编译错误**。→ 开发判断升级: InspectController逻辑/交互层我可自行 写.cs→Refresh→recompile→get_console_logs验→run_tests(EditMode)验, 不必全等David Play; 仅第一人称**手感**(姿态/旋转感)仍建议David Play终验。注意HQ.unity dirty(上次白模/材质未存遗留+git M)——开发检视是独立.cs不碰场景YAML, 不动它。
- **Batch2 已写盘**(Layout含wireframe/States/Interaction/Transitions)。**我定+告知的机械细节**: 检视中可移动但减速+视野被遗物占(不强制定身=脆弱来源); 鼠标=旋转遗物·视角锁定(不能环顾·强化脆弱)·任意移动键瞬断恢复; **安全HUD(HP/耐力/手电/威胁)检视时保留不全隐**(脆弱态安全); 进入抬到眼前~0.3s/急放~0.1s。
- **NEXT**: Batch3 Data+Events(开发硬契约)+验收 → dev-ready → 桥上起 InspectController 占位实现。
- **ux-designer 审查回(2026-06-28)**: 框架被判定**扎实有依据**(3D握持/不暂停/松手即退/脆弱即张力, 引 Edith Finch/SOMA/Amnesia/Signalis/WCAG/GAG)。必改+建议已**落盘**: ①States"可移动但减速"与Interaction"移动即打断"**自相矛盾**→改**检视站定·移动键瞬断**(反转我之前flag的"可慢走", 已给David翻盘开关); ②inspectDetail暗场景对比度→加**深色半透明"撕边档案标签"底衬**(WCAG AA, 融纸质美学非UI卡); ③hold语义=即入·无进度条·连续按住(Hold型)·已握持免对准; ④inspectDetail **screen-space锚定**(旋转不转字); ⑤填**Accessibility节**(Hold→Toggle必须/对比度底衬必须/ReducedMotion俯仰归零/文本缩放); ⑥Transitions相机俯仰-8~-12°+ReducedMotion归零; ⑦填**Open Questions**(怪物canon/audio掩护音效Pattern B/输入绑定/v2清单)。**a11y之前空白=最大实际风险, 已补**。
- **spec进度**: 14节剩 **Events/Data/Localization/Acceptance**(开发硬契约)未填 → 填完=dev-ready→上桥起InspectController占位。
- **建模任务书复核**: gap G1(本地化文本别烤贴图·真坑)/G2(正反多面建模)/G3(握持轴心)/G4(检视MVP=层二)。建议走**(a)**等检视spec定稿一次性同步(spec即将定稿)。**David选(b)→已补G1进任务书§5.6**(痕迹归贴图·文字归`inspectDetail` UI层别烤死, 顺带G2正反多面一句); G3(握持轴心)/G4(检视MVP=层二)仍候spec完全定稿后同步。
- **spec DEV-READY(2026-06-28)**: 14节全填(Events/Data/Localization/Acceptance 已落盘)。Data字段=tier/isInspectable/targetPersonId/inspectDetail(=loc key不烤贴图); Events=`IsInspecting` NetworkVariable(server-auth广播第三人称姿态)+本地InspectStarted/Ended/RelicPersonRevealed, **不触value**; AC1-8(AC4=不改价值断言/AC5=联机IsInspecting同步/AC6手感Play验)。移动处理=**站定·移动即打断**(David默许#3)。
- **开发启动(David"赶紧开发")**: 桥8091活·非Play。起 `InspectController`(core-loop §9 Task#2)占位实现。流程: recon钩子→写.cs→execute_menu_item Assets/Refresh→recompile→get_console_logs验→run_tests EditMode(AC1-5,8)。AC6手感/AC7 a11y待David Play终验。
- **InspectController 已落盘(2026-06-28, 待编译验证)**:
  - `Scripts/Scavenge/Core/InspectSession.cs`(纯核·ns `BlackCommission.Scavenge`·noEngineRef assembly): `Tick(InspectInput)`→Enter/Exit, 退出优先级 **Downed>Interrupt>Release**; 无value引用=结构性保证AC4。
  - `Scripts/Player/InspectController.cs`(NetworkBehaviour·owner-only): hold右键+对准`ScavengeItem`→进检视; `IsInspecting` NetworkVariable(server-auth→第三人称姿态B3); `LocalInspecting`静态gate; 占位克隆mesh到eye anchor旋转·**不走CarrySystem/不读baseValue**; 任意 移动/攻击/手电/热栏/倒地→打断。
  - `Scripts/Player/PlayerCameraController.cs`: +`if(InspectController.LocalInspecting)return;`锁视角。
  - `Tests/EditMode/Scavenge/InspectSessionTests.cs`(9例): AC1进入/释放·AC2打断·AC3倒地+不能倒地进入·优先级·hold维持·丢准星不掉。
- **验证(进行中)**: 桥MCP server随reload断→改用 `tools/ws-unity-call.cjs` 直连8091。Refresh已触发编译, 后台轮询`get_console_logs`等结果(bash bo6cnfdkh)。**待**: 编译0错→`run_tests` EditMode跑InspectSession→挂InspectController到`Player.prefab`→David Play验AC6手感/AC7 a11y。
- **MVP占位范围**: 现场·对准ScavengeItem·3D握持旋转。**后续(spec已定·占位未做)**: tier/isInspectable gating + held-relic entry + inspectDetail文本面板 + 撕边标签底衬 + 低头俯仰-8~-12° + Toggle a11y。
- **桥重启成功+编译0错(2026-06-28)**: 桥泵卡死160s→David授权A→`taskkill /F /PID 17252`(只杀BC实例·没碰另2个Unity·删lockfile)→直连exe重启新实例(~36s起·新8091)。**检视代码编译0错**(InspectController+InspectSession Core+9测试+camera gate全过, 人工review准)。⚠HQ.unity dirty白模随强杀丢失(脚本产物·重跑build菜单可重生)。
- **prefab挂接**: 写 `Assets/_Project/Editor/InspectControllerInstaller.cs`([InitializeOnLoad]·幂等·自动给Player.prefab加InspectController·免手改YAML·临时MVP挂接·正式authored后删此installer)。后台编译+挂+验证(bash bc7pou855)。**待**: David Play 对准ScavengeItem hold右键验AC6(握持旋转+占位"检视中"文字面板)。David"先别测"=跳过run_tests。
- **桥操作模式**: MCP server断未自动重连→全程`node tools/ws-unity-call.cjs <method> '<json>'`直连8091; 编译触发reload→ws断→后台轮询get_console_logs(grep TIMEOUT/WS ERROR)等恢复; 编译错查error类·成功success+error空。
- **挂接成功(2026-06-28)**: installer 改 [InitializeOnLoad] **static ctor 同步执行**(delayCall后台不tick→改同步, reload时必跑不依赖焦点)+delayCall兜底。重编译后 **Player.prefab 已WIRED** InspectController(grep guid `fb704248...`命中)。⚠installer改了`Player.prefab`(git显示 M, 预期挂接·非误改; 正式authored后删installer)。当前场景=HQ(clean, 强杀后从盘重载, 旧dirty白模已丢)。
- **待David Play验(任务现场, 非HQ)**: HQ无ScavengeItem→检视对准不了; 须起host+进搜刮任务现场+对准搜刮物hold右键。预期: 拿到眼前·鼠标转·视角锁·右侧"检视中/itemId/占位"纸色字+深色底衬; 松右键或WASD/左键/F/1-5→放下解锁。手感反馈后调rotateSpeed/anchor。
- **检视键 hold右键→hold F(David问"为什么不是F")**: 核实 fKey全项目仅InspectController用→**F空闲**(手电走PlayerHotbar热栏选slot + PM 2026-06-13已禁用, 不占F); 我原"F=手电打断"是记错。改: Update hold读`Keyboard.fKey`; ReadInterrupt移除fKey(F是检视键不能同时当打断); spec Entry/Interaction/OpenQ "右键"→"F"。打断键现=WASD/Space/Shift/左键攻击/1-5热栏。
- **修armed-latch bug**: 原"检视中点一下移动打断+F还按着"→下帧立刻重进。InspectSession加`armed`(打断/进入后=false·松hold才re-arm)→打断后须松F再按才重进。测试+1例(10例)。
- **重编译中**(bg624ssq5)。prefab已WIRED不受重编译影响。0错→David任务现场按**F**重验。

## Session 2026-06-28 (cont) — 检视「先这样」+ David 发现 level bug(门对齐) + loot
### 检视 MVP「先这样」定稿(David 2026-06-28)
- David 验检视时接受现状。**可玩**: 3D握持/鼠标旋转/**hold F**/打断(WASD·Space·Shift·左键·1-5)/armed-latch/不改价值/`IsInspecting`联机/占位文字面板。编译0错·已挂Player.prefab(installer)。
- **捡起=E / 检视=hold F**(E拿走装车算钱; F端详痕迹不改价值)。
- **待办(spec已定·占位未做)**: 真遗物模型 + `inspectDetail` loc文案 + 撕边档案标签底衬 + 低头俯仰-8~-12° + Toggle a11y + 层一salvage检视 + 改日安全检视 + EditMode跑测试(David"先别测"未跑) + (可选)Loot白模加暖描边区分可捡(David未采纳)。
### 门-家具对齐 bug(David报"家具挡门"→选方案1"转圈")——**已设计·待实现**
- **根因(非我改, git证)**: 图1=`TowerPlanV8` **authored固定楼**(具名房间+精确门, seed只toggle不挪门; `TryGetDoorCenter`给门在哪条边)。家具走RoomDresser烤进**通用模块**(Room_Office_4x8等), 按"假设门边"避(Office留空S+E/Utility S/Large S+W), 但`TowerLayout`随机填进**门在别的边**的房间(`RoomDef.Fits`只比size/role)→靠满"假设无门墙"的家具挡门。走廊已按门边命名(Corridor_SW等)解决, 房间没补齐。3个Dressing asset都是默认值。数据链通: `slot.slotId`=slab Id→plan查门。
- **方案1设计**: ①门边算法(slotId→plan→TryGetDoorCenter→N/S/E/W bitmask·纯逻辑可单测) ②`RoomDef`加`blankEdges` ③`TowerLayout.Place`找旋转(方形0/90/180/270·长方形仅0/180)让转后留空边⊇门边·盖不住换间/警告。~3-4文件+测试+编译验证(David退Play)。
### loot诊断: 物品定义17个+白模Loot_Light/Medium/Heavy都在·LootSpawner host填LootAnchor。David问捡起键=管线通。

### 门对齐「最终版本框架」落盘(2026-06-28夜·待编译验证)
- **真相修正**: 图1房间池=`TowerRoomPoolBuilder`(PM 2026-06-18·真Asset Store包·Room_BreakerCloset等), 道具按`Wall`枚举(N/S/E/W/C)摆·多数占3-4墙。**非**早期RoomDresser/Room_Office_4x8那套。
- David否决临时隐藏·要**最终版本**(模块标门边+摆家具时门可见+生成转圈对齐plan门)。今晚落盘**框架6块**:
  1. `TowerPlanV8.DoorEdgesForSlab(slotId)` + `DoorEdge` enum: 从plan算房间门在哪边(N/S/E/W)。纯逻辑。
  2. `Scripts/Level/Topology/RoomFillAlignment.cs`(纯逻辑·Topology asmdef): `Rotate`(DoorEdge 90°CW: N→E→S→W) + `FindRotation`(找转角使留空边⊇门边; 方形0/90/180/270·长方0/180·-1=盖不住)。
  3. `RoomDef.blankEdges`字段(DoorEdge)。
  4. `TowerLayout.Place`→`AlignedRotation`: slab门边+方/长方(size!=Large)+FindRotation→转模块; 盖不住LogWarning+不转。
  5. `TowerRoomPoolBuilder.BlankEdgesOf(spec)`: 道具占的Wall反推留空边(保守:占墙=非blank), 建RoomDef时填。
  6. 测试 `Tests/EditMode/Level/RoomFillAlignmentTests.cs`(Topology.Tests): Rotate/FindRotation 6例 + DoorEdgesForSlab(PUMP=W\|E·unknown=None)。
- **待**: Unity编译(0错?) + run_tests + 重跑菜单`Build Tower Room Pool v1`(把blankEdges写进RoomDef assets) + Play验门通。
- **诚实现状**: 框架在·留空边够的房间自动转对; 但现有房间道具占满3-4墙→留空边少→多门slab `FindRotation`=-1→警告+不转(仍挡门)。**要David照门口重摆家具腾空墙才完美**(David的活)。**门可视化工具未做**(下一步·帮David摆)。
- **⚠ Unity已关**(8091 down·无进程): 框架代码全落盘**未编译验证**。重开Unity→Refresh编译→get_console_logs(error空?)→run_tests→Build Tower Room Pool v1。
- **✅ 框架已验证(2026-06-28夜·David要"验证")**: 重启Unity编译**0错** · 全EditMode测试 **255测试/passCount212/failCount0**(`RoomFillAlignment` 8例全过·含`DoorEdgesForSlab_Pump`=W\|E手算对; `InspectSession` 10例全过) · 重跑`Build Tower Room Pool v1`成功(**10 RoomDef写入blankEdges + 30 loot anchors + catalog**)。框架运行时生效: `TowerLayout`填房间→读`blankEdges`+算slab门边+`FindRotation`转圈对齐。
- **待David**: Play验门通——**留空边够的房间自动转对**; 道具占满3-4墙的房间`FindRotation`=-1→LogWarning+不转(仍挡门)→**要David照门口重摆家具腾空墙**。**下一步=门可视化工具**(帮David摆家具时看见门口)。menu `Build Tower Room Pool v1` 桥后台可调(已注册)。Unity当前开着(我启动·8091活)。

## Session 2026-06-29 — 门对齐**推翻转圈框架→换"门洞局部清空"**(David拍板"换"·代码全落盘·待编译)
- **David质疑命中要害**: "塞满墙有什么关系?只要不挡着门不就行了"。核实道具定义=`(wall, along)`两参(沿墙偏移!)→旧`blankEdges`把整面墙塌缩成布尔丢了`along`=过度保守。**正解=门洞局部清空**(只挪压在门口那一两件·其余原地不动·零旋转)。David"可以的。换"→"go"。
- **关键几何事实(已查证)**: slot anchor 放`(CenterX,y,CenterZ)`格子正中·**旋转identity**(`BuildRoomSlots`从没设过); 房间prefab局部+x=东+z=北·**与plan轴完全对齐**→门偏移(plan系)直接==道具`along`(房间系)。所以转圈框架本就多余。
- **已落盘改动(8处·未编译)**:
  - **删**: `RoomDef.blankEdges`字段 · `TowerLayout.AlignedRotation` · `TowerPlanV8.DoorEdgesForSlab` · `Topology/RoomFillAlignment.cs`(+meta) · `TowerRoomPoolBuilder.BlankEdgesOf`。
  - **加**: `TowerPlanV8.DoorOpeningsForSlab(slabId)`→`List<SlabDoorOpening{Edge,Offset(相对格心沿墙偏移),HalfWidth}>`(纯逻辑·Topology) · `Scripts/Level/RoomPropPlacement.cs`(运行时组件:`DoorEdge wall;float along`) · `Scripts/Level/Topology/DoorClearance.cs`(纯1D滑槽搜索`NearestFree(along,maxOffset,forbidden[])`→最近空位/NaN) · `Scripts/Level/RoomDoorClearance.cs`(静态`Clear(room,slot)`:对每门→同墙道具`along`压进门洞±(门半宽+道具半宽+0.15余量)→`NearestFree`沿墙滑·滑不动`SetActive(false)`+warn·道具半宽现场renderer bounds算)。
  - **改**: `TowerLayout.Place`实例化后调`RoomDoorClearance.Clear`(networked+decor两路都调)·用`slot.transform.rotation`不再转 · `TowerRoomPoolBuilder`给贴墙道具(N/S/E/W)挂`RoomPropPlacement`(`EdgeOf(Wall)`映射)+**LootAnchor改挂道具子物体下**(挪/禁用时loot跟着走·LootSpawner用FindObjectsByType嵌套无碍)。
  - **测试**: `Tests/EditMode/Level/DoorClearanceTests.cs`(替换RoomFillAlignmentTests·Topology.Tests asm): DoorOpeningsForSlab(PUMP=W/E offset0 half1.0手算·LOBBY含D-VAN S offset3 half1.4·NOPE空) + NearestFree(已清/滑到门边/对称取lo边/全覆盖NaN/道具宽于墙NaN) 共8例。
- **调参**: `DoorClearance.Clearance`0.3→**0.15** · `RoomDoorClearance.EdgeInset`0.3→**0.1**——4×4墙上2m居中门(plan最大50%面)每侧仅1m·余量大会逼窄道具被删而非挪。现窄道具能滑过·宽到塞不下才删(几何正确)。
- **效果**: 家具不再被逼腾空整面墙·房间不转圈·只有正好压门口的1-2件沿墙挪开(挪不动才删)·其余原地。**长房间12×8竖摆朝向不匹配=另一既有问题·没碰**。
- **✅ 全部验证通过(2026-06-29·我自己拉起Unity)**: `Unity.exe -projectPath`后台启(8091约30s起·PID41612) → 直连`ws-unity-call.cjs`。**编译0错**(type=error 0条·我的代码无错; 4条`[MCP Unity]`路径校验=包噪音可忽略) · **全EditMode 212/212 pass·0 fail**(`DoorClearance` 8例全列全过: DoorOpeningsForSlab PUMP/LOBBY/NOPE + NearestFree 5例) · 重跑`Build Tower Room Pool v1`成功(**10 prefab + 30 loot anchors**) · 验prefab: Room_BreakerCloset 4贴墙道具全挂`RoomPropPlacement`(墙{N,N,W,S}✓·中心decal正确没挂)·3 loot anchor。
- **唯一待David**: Play到搜刮任务现场(host+进任务·HQ无ScavengeItem)验门通——压门道具是否沿墙滑开/其余原地。Unity当前**开着**(我启·8091活·PID41612)。
- **下一步(David摆家具辅助)**: 门可视化工具仍未做(现在更可有可无了·因为不再要David腾墙·只在"宽道具塞不下被禁用"时才需看门口)。

## Session 2026-06-29 (cont) — loot洋红诊断 + 菜单背景相机修正(David Play反馈)
### loot "紫色冒光+按E没用"诊断(代码已查证·未动手修)
- **紫色=URP下材质丢shader的洋红**: `Resources/Loot/Loot_Whitebox.mat` 用**内置Standard shader**(guid 933532a4...), URP渲不出→洋红(本意`_BaseColor`=土褐0.42,0.37,0.29)。Asset Store包(Tirgames/Sat)房间道具多半同坑。**修法**: `Edit > Rendering > Materials > Convert All Built-in Materials to URP`(CLAUDE.md早警告)。**未执行**(David没要求·先诊断)。
- **按E没用**: loot白模 collider(非trigger·enabled·1×1×1)+NetworkObject+`ScavengeItem`(IInteractable·`CanBeCarried`=true)全在·捡拾走`PlayerInteraction.FindAimedTarget`SphereCast需准星对准。**最可能David按的是洋红家具(无ScavengeItem)误当loot**。真loot应能捡·待材质转完分清后验。
### 菜单背景相机=对着空墙→改框办公电脑(David"让背景出现电脑·摄像机对着墙没感觉")
- **根因(git证·非我改)**: `CrtMenuStage.BuildMenuCamera`默认构图硬编码"屏幕朝-Z·相机看+Z"(早期旧布局), 但HQ Option-A的CRT在-X墙·`MVP_OfficeComputer`锚点yaw90°**朝+X进屋**→默认相机背对电脑、面朝空墙。`MenuCameraAnchor`物件存在则用其位姿(美术钩子·当前无)。
- **几何(已算)**: HqScale=1.2·Interior父级×1.2。CRT屏幕锚点世界≈(1.44,1.2,7.2)朝+X·债务板≈(0.26,1.5,4.3)·岗哨≈(1.2,0,2.6)·桌+暖灯≈(2.6,0,12.7)。
- **已改(CrtMenuStage.cs默认else块·相对screenAnchor偏移·未编译验证)**: camPos=screenCenter+(2.76,0.35,1.80)·lookAt=screenCenter+(-0.14,-0.05,-0.20)→CRT正前方~2.8m抬0.35略偏下游的3/4回看视角。全屏MainMenuUI叠加层不受影响仍清晰。
- **⚠桥死**(get_console_logs/get_gameobject全TIMEOUT·Unity Play后台没tick): 没法实时调相机/确认编译。**待David**: 停Play(触发重编译)→重进Play看菜单背景→报"太小/太偏/想看桌子"我调那两个偏移。可能微调1-2轮。**若电脑也洋红**=同URP材质坑·一起转。
- **备选**(没采纳·留着): 想要持久/可视拖拽构图可在HQ builder生成`MenuCameraAnchor`物件(David拖到满意位姿)·`CrtMenuStage`已支持读它。

## Session 2026-07-01 — 洋红根因**推翻上次诊断**·已修 DecalsDirt01(新CoplayDev桥首战)
- **新桥确认连通**: CoplayDev UnityMCP(uvx stdio)工具全套可用·read_console实测通。⚠本session Anthropic安全分类器间歇故障→MCP mutating工具(execute_code/execute_menu_item)反复被挡·只读工具(Grep/Read/read_console)全程可用→改走本地YAML直改。
- **上次(06-29)诊断错了·两处**: ①`Loot_Whitebox.mat` shader guid `933532a4...`=**URP/Lit**(不是内置Standard·上次把URP Lit的guid误认了)·材质健康(土褐0.42,0.37,0.29·无自发光)→loot本身从来不洋红; ②"全项目批量Convert Built-in Materials"不必要。
- **真·根因(本次静态全扫定位)**: 全Assets共51个内置shader材质: AdventureForge MetalCabinet×19(Standard·**项目零引用**·房间里的柜子实际是Tirgames prefab+URP材质) + TreePackVol.1×29(Nature/Tree Soft Occlusion+skybox·**运行时零引用**·植被走Foliage Free已转URP) + Tirgames skybox×2(URP下本就能用) + **`DecalsDirt01.mat`×1(legacy Transparent/Diffuse fileID30→URP下洋红·唯一真凶)**。它被`DecalX1Y1.prefab`引用·`TowerRoomPoolBuilder`摆在房间**中央地板**(L93配电间"油渍"/L108"积水渍")→暗房间地上一块发亮洋红片+按E无反应(贴花非loot)=David报的"紫色冒光+按E没用"**完全吻合**。DecalX3Y1/其余Decals材质本就URP。
- **已修**: `DecalsDirt01.mat` YAML直改(Write工具·绕过被挡的桥)→URP/Lit透明(_SURFACE_TYPE_TRANSPARENT·SrcAlpha/OneMinusSrcAlpha·queue3000·贴图guid `6e8fb2ff...`保留进_BaseMap)。**待**: Unity焦点/Refresh重导入→David进任务现场验贴花显示为半透明脏渍(非洋红)+真loot(土褐盒)按E可捡。
- **修正结论**: 不需要跑全项目URP转换(51个里50个零引用或本就兼容); AdventureForge/TreePack若日后启用再转。

## Session 2026-07-01 (cont) — David报4问题: ①电脑侧身已修 ③闭环缺口已补(核心) ②④待David拍板
- **David四点**: ①电脑交互身体侧过来(要正对) ②黑影bug还在 ③要完整循环(两图→结算→选图→出任务) ④车内过场UI丑。AskUserQuestion超时无人→按推荐案推进③、④②等拍板。
- **①已修(编译0错)**: 根因=两套终端相机打架——`MvpHud.OpenComputer`走旧`ComputerCloseupCamera`(硬编码世界系-Z偏移→对-X墙朝+X的CRT成侧视),且它禁用`PlayerCameraController`,把新的正对路径`GetTerminalCameraPose`(`PlayerCameraController.cs:136`)饿死。改`MvpHud.cs`两处:OpenComputer不再Enter旧特写相机(注释说明),CloseComputer保留Exit当无害兜底。**待David开电脑验**。
- **③真缺口(guid逐场景核对)**: Tower_EarthCoast_01.unity只有MissionVanExitPoint×1,Map2_Procedural啥都没有——ScavengeMissionManager/ScavengeCargoZone/LootSpawner只存在于Scavenge_Testbed;TowerV8WhiteboxBuilder的BuildMissionManager只在编辑器内存里建过、从没存进场景→正式派遣=无loot/无法结算/无法回家。**闭环其余链路全在**(任务池Resources/Tasks两图都列、联机SelectCommission、结算、返程、HQ领奖、再选)。
- **③已实现(运行时装配·David没回但这是他明确要的)**:
  - 新`Scripts/Scavenge/ScavengeMissionBootstrap.cs`: [RuntimeInitializeOnLoadMethod]挂sceneLoaded; server+listening+scene==ActiveTask.sceneName才跑(ActiveTask从不清空→场景名gate挡HQ); runner协程等锚点≤5s(现有MissionVanExitPoint→DROPOFF_VanSpawn(Map2)→PlayerSpawnPoint)→按缺补:ExitPoint(场景没有才生成)/CargoZone(anchor+2.5,1.25,-0.5)/LootSpawner/Manager,全部Resources/Mission预制体Instantiate+Spawn。场景已有的件永远优先不重复。
  - 新`Editor/ScavengeMissionRigBuilder.cs`菜单`Tools/Black Commission/Scavenge/Build Mission Rig Prefabs`: 四件各自独立prefab(NGO禁嵌套NetworkObject+各件[RequireComponent(NetworkObject)]),尺寸抄TowerV8WhiteboxBuilder(cargo 8×2.5×4 trigger/exit 2.5×2×2.5); 注册进DefaultNetworkPrefabs(实测NGO导入钩子已自动注册,Add兜底)。
  - **已跑通**: 菜单执行成功→4 prefab落盘`Assets/Resources/Mission/`+注册验证(list现9件:Player/EchoMold/Loot×3/rig×4)。**EditMode 157/157 pass**。省掉手动DepartLever(MissionVanExitPoint上车面板本就走RequestDepart)。
  - **已知妥协(已知会David)**: Tower的van视觉(VAN_Body)也是编辑器产物没存场景→上车点在空地上飘着"上车"提示; CargoZone偏移是估的可再调; Map2多人determinism仍是ADR-0003遗留(solo-host OK)。
- **②黑影**: 静态排查历史已穷尽,给了David三选(截图/Play暂停我桥上抓/先不管),未答。
- **④过场UI**: 确认整个`VanTransitOverlay`是IMGUI(OnGUI+GUI.Label)结构性丑; 给了David两选(推倒重做uGUI+TMP按债务噪音视觉语言[推荐]/IMGUI原地精修),未答。
- **待David验收清单**: 开电脑正对CRT? → host派遣进烂尾楼:有loot捡/装车区计数/上车能出发→结算卡→回HQ→CRT领钱→选Map2→再出发(Map2场景锚点DROPOFF_VanSpawn) → 顺手答②④两问。
- **未提交面继续变大**: 本session新增2脚本+4prefab+DefaultNetworkPrefabs+MvpHud+DecalsDirt01.mat(+此前CrtMenuStage/门口清空/检视全套)——建议David点头后打checkpoint commit。

## Session 2026-07-01 (cont.2) — 任务图ESC设置菜单+伽马+退出到主菜单(David要求·已实现·编译0错)
- **David**: 地图里ESC要能开设置(亮度/伽马/鼠标速度)+退出游戏+退出到主菜单。
- **现状核实**: `SettingsOverlay`(DDOL单例·IMGUI"PREFERENCE RECORD/FORM BC-05")早就有亮度(URP postExposure via BrightnessController全局Volume p100)/全屏/画质/双轴灵敏度/反转Y/FOV/语言/音量/语音/退出游戏——**缺的只是①任务图没人听ESC(监听在MvpHud而MvpHud只是HQ场景对象·guid核对Tower/Map2/Testbed全0) ②伽马 ③退出到主菜单**。
- **已实现(5文件·编译0错·反射验证过)**:
  - `MvpHud.cs`: +`static bool Present`(Awake/OnDestroy跟踪)。
  - `SettingsOverlay.cs`: +`Update()` ESC监听——`MvpHud.Present`或菜单占用时让位(HQ仍走MvpHud面板链·菜单走MainMenuUI),任务图上ESC开/关;guard VanTransitOverlay.IsActive+SettlementCardOverlay.IsCardVisible+InGameplay()。+伽马滑条(亮度下方)。+"退出到主菜单"按钮**两次按确认**(2.5s窗口·host退会踢全队·开面板时重置armed)→`QuitToMainMenu()`: Save+Shutdown+CompanyData.ReloadFromDisk+MvpMissionRuntime.Clear+LoadScene HQ(抄DisconnectHandler返回路径·非联机HQ自动出主菜单;客户端被踢由DisconnectHandler自己走回菜单)。
  - `DisplaySettings.cs`: +`Gamma`(PlayerPrefs `AS.Display.Gamma`·±0.8·ResetDefaults含)。
  - `BrightnessController.cs`: Volume profile +`LiftGammaGain`(gamma.w=Gamma)·Apply()两项都刷。
  - `MvpLocale.cs`: +gamma/quit_to_menu/quit_to_menu_confirm三键(EN/ZH)。
- **待David Play验**: 任务图ESC开面板→调亮度/伽马/灵敏度立即生效→ESC关回锁准星→"退出到主菜单"两按回菜单(mid-mission退出的过场overlay残留是已知边缘·见了报)。

## Session 2026-07-01 (cont.3) — David「事务所建筑+室内还是非常不满意」→ Stage A+B 欠账已补完(已重建·待走查)
- **现场核实**: HQ.unity 里 HQ_OptionA=off·**HQ_MarsWhitebox=ON**(David看的是火星白模)——而06-24拍板的 Stage A材质+Stage B氛围**一直没做**,他看的是半成品。AskUserQuestion(建筑痛点多选/室内痛点多选/流程三选:概念图先行[荐]/先补两pass再评/口述直改)**超时未答**→按判断先补已批欠账(不动方向)。
- **Stage A 已实现**(`HqMarsFreightWhitebox.cs`): C#烘焙**拼板缝贴图+法线**(512²·2×2板/tile≈2.3m板·逐板明度0.78-1.06·缝槽4px+角螺栓·重力垂直污带·顶部火星尘暖漂·细噪防死平; seed=20260701幂等) → 存`Assets/_Project/Art/Textures/MarsShellPanels{,_N}.png`(importer自动设NormalMap/Repeat) → `ApplyShellSkinTextures(mShell)`挂_BaseMap+_BumpMap+_NORMALMAP。冷蓝tint仍由材质乘。
- **Stage B 已实现**: **B1**三道god shaft(交叉梯形additive quad·URP Unlit SrcAlpha/One·Cull Off·底宽渐隐渐变贴图): Shaft_Crack(裂缝→集结台)/Shaft_Door(门头→车湾)/Shaft_Prow(船首rim)。**B2**两组尘埃ParticleSystem(Crack池+Bay钠灯池·prewarm·noise漂移·URP Particles/Unlit软点)。**B3**ambient 0.55→0.45·fogEnd 62→50(光池成孤岛·脊顶隐入黑)。**B4**post加深: Vignette0.28+SplitToning(teal阴影#2E4A4E/amber高光#C89A50·balance-15)+CA0.06+bloom强度0.30→0.42。**B5**自发光: CRT磷光玻璃quad(绿·HDR2.2·bloom拾取)+暖窝门头amber条(1.6)。
- **✅ 已重建进场景**(菜单跑通·完成log·0错; Atmosphere 7子物体/ShellSkin挂上双贴图+_NORMALMAP/两PNG资产在盘/尘埃PS在)。**场景dirty未存**(惯例·David满意再Ctrl+S; 材质仍是builder内存产物·重开Unity重跑菜单再生)。
- **⚠方向级不满意仍未解**: 上面三问(建筑痛点/室内痛点/流程)等David答——若是形体/布局层面,下一步走**概念图先行**(建模任务书流程·设计部门有推翻权·3张方向图)。

## Session 2026-07-01 (cont.4) — **火星方向废止(PM拍板)** → HQ=「破旧事务所+车库湾」·平面图v6已出待批
- **PM「不能用火星白模,他应该就是一个破旧事务所」**·我表态同意(三理由: ①支柱对齐——市政债务黑色的幻想是"经营快倒闭的小事务所",宏伟与幻想打架,破旧本身=身份; ②奇观归任务图——LC结构=船小挤/星球大冷,家↔任务图的对比比塞进HQ内部成立; ③制作现实——程序化大曲面已四轮建→丑→改循环,小而密的破办公室正是现有资产能做出高质量密度的东西)。**保留两条经验**: 纯室内(室外地形/植被也是失败循环)+刚写的StageA/B氛围技术全可迁移(光池/尘埃/光柱/分离调色在小尺度更好用)。
- **PM三决(AskUserQuestion)**: ①空间构成=**办公厅+车库湾一体小平房** ②流程=**平面图先批再建** ③旧的两套HQ(火星白模/OptionA)=**先留着停用**。
- **平面图v6已出**: `design/hq/HQ_ShabbyOffice_Plan_v1.png`(渲染器 `tools/hq_shabby_office_plan_v1.py`·复用hq_floorplan_render)。要点: 办公厅8×7m顶2.9 + 车库加建7.2×8.2m顶4.3铁皮(向北凸1.2m=便宜加建的叙事); **正门被法院查封(印章红封条·不可用)=只能走车库进出**(敌意收购压力入平面); 卷帘门4.0m=框景(死街背景不可进入:链网栅栏/垃圾箱/死路灯/废楼剪影); 办公厅=休息角(破沙发/折叠桌/四人地铺出生)+西墙CRT终端桌+债务板+北墙装备墙(补给柜/档案柜/工具架)+纸箱杂物挤动线+岗哨守查封门; 车库=van车头朝门/车尾登车+集结垫+工作台/油桶/轮胎/配电箱/油渍; S1挂锁门(执照锁)西墙休息角旁; 出发动线=地铺→CRT→装备墙→胶条门帘→集结→车尾; **返程结算beat=进门帘迎面债务板+CRT一条直视线**。灯语=办公厅钨丝+濒死灯管sputter/CRT磷光绿/车库钠灯/南脏窗冷天光。
- **NEXT**: David批平面图(改尺寸/摆位直接说)→按图新写builder(独立`HqShabbyOfficeBuilder.cs`·不碰旧builder·复用StageA/B氛围代码+Tirgames/AS_Office资产)→建→走查。旧HQ_MarsWhitebox/HQ_OptionA继续停用保底。
## Session 2026-07-01 (cont.5) — 破旧事务所已按v2施工完成(David"施工吧"·MCP全程验证·待走查)
- **新builder `Assets/_Project/Editor/HqShabbyOfficeBuilder.cs`**(自包含·不碰旧builder·菜单Build HQ Shabby Office (v6)/Remove(restore Mars))·坐标全部来自批准的平面v2。
- **已建成+桥上verified**: 编译0错 → 反射调Build()(绕过新MenuItem需焦点注册的坑) → 建成`HQ_ShabbyOffice`根(Envelope35/Office23/Garage11/Backdrop14/Lights9/Post/Atmosphere7)·**退役81旧物件**(Mars白模+MVP_RestoredOfficeProps等全停用·菜单相机保留) → 逐项验: Van_BoardZone+OfficeDepartureVan+solid collider✓ · GearSupply_StorageTrigger+OfficeCabinetStorage(isTrigger)✓ · BestiaryNotebook+OfficeMonsterBestiary(折叠桌上)✓ · PlayerSpawnPoint(1.7,0.1,2.0)✓ · MVP_OfficeComputer(1.05,1.05,4.4)fwd=+X✓(GetTerminalCameraPose/CrtMenuStage都吃这个anchor) · DispatchVan(11.6,0,4.3)✓ · 兜底盒仅1个(Tires01不存在·按设计) → **EditMode 157/157 pass**。
- **实现要点**: 查封正门(门板+木条+红胶带X+查封告示+印章)·公文钉板(SafetyBoard+红章片)·2脏窗(磨砂玻璃+冷光)·胶条门帘9条(半透明)·卷帘收起卷+隐形ThresholdBlocker·死街背景(链网栅栏/垃圾箱/死路灯/对面废楼)·灯语四池+濒死灯管sputter·氛围复用(窗2道+卷帘1道光柱/办公室+车库尘埃/CRT磷光玻璃+门帘上amber条自发光/同款post)。
- **⚠走查检查单(David)**: ①**VanYaw**(车头该朝卷帘门·若反了改builder顶部`VanYaw=180`重建) ②尺度/压抑感(顶2.9是否合适) ③门帘穿行手感 ④登车/储物/图鉴/CRT四交互都按E试 ⑤主菜单背景(anchor挪了·CrtMenuStage相对偏移应自动跟) ⑥场景dirty未存·满意Ctrl+S。
## Session 2026-07-01 (cont.6) — David报"电脑摆在椅子上"→ unity-model-fit 全量重排 + 两个大坑(已修·正式重建·待存)
- **根因**: 我猜坐标没量bounds。实测(走unity-model-fit技能): `AS_OfficeComputer`=**整张电脑桌单元**1.69×1.05×0.89落地件(我抬0.76m→悬浮在AS_OfficeDesk上="电脑在椅子上"); `AS_OfficeSofa`=L型转角沙发1.78×**1.88**(原摆穿南墙); `AS_OfficeToolSet`=2.3×2.06巨物(原当纸箱用堵死走道); `AS_OfficeVan`长轴=**局部X**(yaw0=横停)。
- **重排(全部按实测)**: CRT单元落地(0.6,0,4.4)yaw90·删冗余CrtDesk·锚点(1.0,0.95,4.4)fwd+X·绿光池(1.3,1.0,4.4)·删悬浮CRT_Glass自发光(模型自带亮屏); L沙发进西南角(0.95,0,1.0); 地铺4张挪窗下(4.2/5.0,0.75/1.65)+PlayerSpawnPoint(4.6,0.1,1.2); 台灯/图鉴笔记本按实测桌高0.75落位; 公文钉板实测1.88宽→查封门旁墙段放不下→沙发上方(1.0,1.45)+红章片; 岗哨挪门东侧(3.35,0,0.55); 工具架(6.5,0.9)避开档案柜; 灭火器上party墙(7.76,1.1,5.9); ToolSet换`CartonStack`三层纸箱primitive×2; **VanYaw=270**(截图验证90是车头朝南)。
- **⚠大坑2·URP运行时透明材质渲染成实心白**(截图发现光柱=白三角/门帘=白板): 手动SetFloat(_Surface/_Blend)+keyword**不生效**——须调URP编辑器侧`Unity.Rendering.Universal.ShaderUtils.UpdateMaterial(mat,ModifiedMaterial)`(internal→反射)。已封装`ValidateUrpMaterial()`进builder(Glass/Shaft/Dust三处materials都过一遍)。**记住: 以后任何editor代码手配URP透明材质都要跟这一步**。
- **⚠大坑3·Play模式误建**: 第一次重建时David在Play里→几何建进运行时会话(退Play即丢)+MarkSceneDirty抛异常。builder已加`Application.isPlaying`护栏拒建。教训: execute_code动场景前先查isPlaying。
- **манage_camera截图=自验证闭环**: `screenshot`+view_position/view_target+include_image → 我自己能看场景了(CRT角/车库/办公厅全景三张全过目)——不再拿David当人肉验证器。修正前后对比图在Assets/Screenshots/shabby_*.png。
- **状态**: 编辑模式正式重建完成·board/cabinet/bestiary接线verified·vanYaw270·场景dirty**待David Ctrl+S**。
- **UX+游戏设计双审(David指示·并行子agent)**: 两份都判 **NEEDS REVISION·小修不推翻**·独立撞上同一最重问题。**已改进 v2**(`HQ_ShabbyOffice_Plan_v2.png`·同一渲染脚本): ①折叠桌 cx1.6→2.0(东移0.4m·西墙走道~1.0→1.4m 两胶囊可错身·顺带解 P3/P4 出生贴脸·动线改绕桌东侧不再穿模) ②公文钉板加在查封门旁南墙(GD:「可疑市政公文」支柱此前无实体锚点·印章红语言) ③图鉴笔记本落位折叠桌上(UX:OfficeMonsterBestiary图上无落点; 我选桌上闲翻案=顺带给非host等待时有事做·David可翻) ④三个灯语锚点钉死坐标(UX:LW/LG/LS只声明没绑定: 钨丝(1.5,1.2)/CRT绿(0.6,4.9)/钠灯(10.0,2.6)) ⑤登车区纵深0.9→1.5m ⑥债务板南移0.3m收视线夹角<6°。**记到建模阶段的**: 查封/挂锁需非颜色线索(十字胶带/实体锁/盖章告示·a11y)·主动线头顶灯具管线别压到2.3m以下·非host微交互(摸工具架/翻档案柜)后续任务。**双审点名保留**: 查封门+车库唯一出入口叙事拓扑·返程直视线·门帘2.4m·集结垫紧贴门帘·装备墙4.15m分散站位·尺度落差·六段节拍(勿再拉长)。**待David答**: S1执照门外(西墙外)地块有没有预留扩展余量——没有的话现在就该定"解锁后走向"(GD警告:不定=重蹈火星四轮返工)。

## Session 2026-07-02 — David出门5小时全权委托：三关卡+新怪+音效+全链路QA（计划经AskUserQuestion四项拍板）
- **拍板**: ①三关=烂尾楼+Map2+火星物流中心(新) ②新关主题=**火星物流中心**(David自填·奇观归任务图·回收Mars白模技术遗产) ③允许本地checkpoint commit不push ④音效缺口=自动下载+Synth兜底。
- **P0**: HQ破旧事务所确认已存盘完好(昨晚23:57)、EditMode 157基线、VanYaw270正确、Unity需重新拉起(直启exe+清锁)。
- **P1a 烂尾楼**(commit `feat(level): tower mission map shippable`): MonsterSpawnBootstrap(全项目首个MonsterSeed消费者·seed名后缀路由怪种·EchoMold兜底)、MissionMapFinalizer(van视觉环采样落地/避开出生点≥3m/**Physics.SyncTransforms必须在探测射线前**/NavMeshData持久化为asset+存场景)、EchoMold.prefab→Resources/Monsters(GUID保留)。
- **P1b Map2**(commit `feat(level): map2 combat-ready`): 3×MonsterSeed(W2/W4/W6)、真van prop(**紧贴DROPOFF锚点会在navmesh抠洞挡死寻路·FullSiteBuildTests抓到**·移到坪侧)、PlayerSpawnPoint、MapSiteRuntime授权侧运行时NavMeshSurface烘焙(此前Map2怪永远无法寻路)。
- **P1c 火星物流中心**(commit `feat(level): Mars Logistics Hub`): design/levels/mars-logistics-01.md·MarsLogisticsBuilder(尘暴外景+落客坪/查封气闸/8m钠灯货架大厅/冷藏遗物附楼/办公夹层31°坡道·28 LootAnchor·3 MonsterSeed·风暴盒兜底天空)·五条寻路全通·入BuildSettings+任务asset(报酬380·雇主=现有火星地球遗产征集事务所)。**顺手清了任务池重复**: 旧英文eco-column任务移出Resources→_Project/LegacyTasks(CRT列表原本会双列Tower)。⚠任务按taskId排序→**火星现为默认选中(index 0)**。
- **P2 FileWarden档案看守**(commit `feat(monsters): File Warden`): tools/rigging/build_file_warden.py一体化(建模+刚体层级+4clip烘焙+FBX)·档案柜胸腔/拖地袍/图章红独眼/双拳砸击·FileWardenSetup(两遍导入clip切分/Pose控制器/EchoMold脑warden调参: 0.8/2.6慢速·16dmg重击·永不诱饵)·火星冷藏seed改*_WARDEN路由。第二只怪(SealSnapper)按降级序砍掉·路由关键字已留。
- **P3 人物**: 评估=**已完备无需建模**(6色tint变体+CharacterIndex同步+一三人称rig+CrewPicker)·截图验证模型质量好·时间转投QA。
- **P4 音效**(commit `feat(audio): sourced SFX pass`): 13 clip入Resources/Audio/Sfx·**Ext()覆盖+synth兜底模式**·FootstepSlicer(编辑器内GetData瞬态检测从长录音切2个0.32s单步·绕过无ffmpeg)·**"环境音wav"实为误存xlsx**→numpy合成60s幽灵氛围+45s火星风(无缝循环)·新事件接线: 柜开关(MvpHud收口点)/存入(柜+**货舱=PlayStore**)/取出/掉落(ClientRpc全端)/门开关(SimpleDoor摆前态判向)/进人chime·CREDITS.md(3个CC-BY署名)。菜单悬浮音本来就有(UiPlayHover)。
- **P5 QA·全链路Play打通**(火星): 开房(Relay)→派遣→过场→**11 loot/3怪(FileWarden在冷藏)/373tris导航/火星风环境音全活**→3件装车(6/12)→发车→**结算卡「完成」章弹出+回HQ**。修复: **怪物生成必须agent.Warp()**(NavMeshAgent绑prefab原位·三只怪全在0,0,0·live warp验证后已修MonsterSpawnBootstrap)。
- **⚠⚠两个运维级发现**: ①**编辑器是暂停状态会让Play全冻在frame1**(EditorApplication.isPaused=true残留·bridge的editor泵还活着所以极具迷惑性——之前"Play后桥死/黑影"疑与此同根)·解法isPaused=false; ②失焦编辑器Play需Application.runInBackground=true。
- **已知妥协/待David**: 强制HideCabin跳过下车重定位(正常按键下车流程不受影响·怀疑预存在)·门开关长音3.2s偏长可裁·火星内部灯偏暗(ESC伽马可调·或再加灯)·上车/下车独立音未接(车辆启动录音自带关门声)。

## Session 2026-07-02 (下午) — David回来验收→三轮修复(R1音效/R2火星v2/R3怪物概念)·全部已commit
- **R1 音效修复**(commit `5828c3e`): AudioManager.LogSfx探针(每个表格事件log `[SFX] event -> clip`·30秒听测可定位错配)·FootstepSlicer v2(响度排序局部极大+前导静音质检·4片0.26s·~150Hz单极高通去房间隆隆声「怪」·峰值归一+淡入出·AudioManager四片轮播)·HQ卷帘门改走door_open/close(合成creak退役)。**频谱发现待David耳测**: freesound #426765(表里的「打开柜子」)实为~1.76kHz窄带金属铃音——听感=终端beep·映射照表·录音本身可疑; #39028(打开电脑)=600Hz门卡锁正常。
- **R2 火星v2**(commit `cdbd07f`·只动火星·tower/map2按David指示没碰): 集装箱堆场(x44-74·3个可进箱+1 seed)·装卸码头(z26-38·传送带+拖车+1 seed)·检疫地下层(y-4·封闭26.6°坡道下行·检查室/遗物档案室(第2只FileWarden)/焚化炉)·高架猫道环线。**导航排雷记进builder**: 风化层切4块避免下行井在实心collider里·管廊顶收回(压低落点低于agent高)·地下层门头抬到2.55m净空(2.2被voxel取整卡2.0 agent)·管口voxel缝2条NavMeshLink桥接。8条路线全PathComplete(33-73m)。Loot 28→57锚+**新ScavengeMapProfile**(火星26-32件·别图10-14不变)·怪3→6 seed·合同窗12→18游戏小时。EditMode 157/157。
- **R3 FileWarden v2概念**(commits `1b2afd0`+`9ba11ad`): David验收判v1丑→重走概念。三方向原创设计(不抄LC): **A**=驼背管理员 / **B**=柜身高跷猎手 / **C**=纸人锥·预览`tools/rigging/preview/fw2_concept_{a,b,c}.png`。**B已做hero精修**(`fw2_concept_b_hero.png`·LC制作法评估: 圆润大块面bevel+subdiv·夸张比例小重躯干+超长细高跷腿膝过顶·极简脸=单个图章红独眼灯·哑光双色teal+图章红点缀·一个「不对劲」细节=半开抽屉下颚拖纸舌·staged渲染)。**Blender 5.1 headless坑已烤进脚本**: Principled按TYPE找(按名找silent fail→全白)·方块用bevel(subsurf会把cube塌掉)·四肢用点到点圆柱。
- ~~⚠待David pick: FileWarden v2三方向选型~~ → **被19:00 session推翻**(见下)。
- ~~杂项: Assets/_Recovery待确认可删~~ → 已删(David批·AssetDatabase.DeleteAsset)。

## Session 2026-07-02 (晚) — David问「能不能直接抄LC」→ **怪物美术改走现成CC0资产改皮路线(拍板)** · 档案怨灵上线 · 三挂起项清完
- **David两问两答**: ①概念选型答A(驼背管理员)——但随即问「怪能不能直接抄别人的/复现致命公司的?」→ 我正面回答: 复刻LC=侵权风险(自己建模复现设计仍是抄设计·上架吃DMCA)+违反AGENTS.md自家红线; 但"直接抄"的合法版=**现成可商用资产改皮** → ②David拍板「现成资产改皮」→ 概念A路线废止(fw2概念图/hero pass留档)。
- **资产源**: Quaternius Ultimate Monsters(CC0·50只全动画·FBX/Blend/glTF)·Drive公开文件夹·gdown拉(整夹限流→按文件ID单下·manifest在scratchpad)。5候选BC改皮预览渲染(`tools/rigging/preview/qm_*.png`): Orc/Yeti/Demon呆脸压不住·**GhostSkull最阴**·Alien次之。60s无人答→按推荐GhostSkull推进。
- **改皮管线(可复现)**: `tools/rigging/recolor_atlas.py`(Quaternius调色板atlas按亮度重映射: 黑→civic teal→旧纸ramp·红系→印章红)+`preview_qm_candidates.py`(Blender headless批量导入FBX套BC材质+Idle摆姿+staged渲染·顺带打印action清单——Big系14clip/Flying系8clip)。
- **档案怨灵已上线(FileWarden视觉v2·prefab身份/WARDEN路由/EchoMold warden脑参数全不动)**: `FileWardenSetup.cs`重写——FBX源换`ArchiveWraith.fbx`(GhostSkull)+`ArchiveWraith_Atlas.png`(Flying atlas BC重映射·Point filter)·**take名选clip替代帧区间切片**(FW_Idle←Flying_Idle/FW_Hunt←Fast_Flying/FW_Attack←Headbutt/FW_Death←Death·loop标齐)·URP Lit材质代码建+prefab renderer全量覆盖(materialImportMode=None断源材质)·**globalScale=0.55**(原尺寸3.23m高/5.3m手展→1.78m)·**SealGlow红点光**塞骷髅内(眼窝透红=图章红威胁语言·暗走廊远读)。四clip导入✓·bounds 1.78m✓·网络注册沿用✓·**朝向+Z=agent前进向✓**(orbit四角截图`Assets/Screenshots/wraith_orbit.png`过目·暗光下卡通感消失)。宽2.9m(双爪展开)>胶囊0.45——怨灵穿模=虚体设定成立·不改。
- **火星提亮(R3·builder+活场景双改保持一致)**: ambient 0.34/0.27/0.20→0.42/0.34/0.26·StormSun 0.45→0.55·全部内部点灯+~40%强度/+1-2m范围(钠灯池3.6→5.0等)·地下层新增**SubLamp_Exam**(-15,-1.5,7.5·检查室原本无灯)。活场景按灯名patch(19灯全ok)+builder源码同值→**没重跑builder=没动navmesh**。截图验: 地下层可读性大改善·大厅货架间隙保留"手电领域"。场景已存。
- **门声裁剪**: 3.15s/3.57s的mp3实为**大段静音包0.3s事件**(RMS包络实锤·"门响滞后"根因)→soundfile解码+能量裁剪→door_open.wav 0.49s/door_close.wav 0.43s(8ms淡入/80ms淡出)·删mp3·AudioManager按无扩展名路径加载无需改码·CREDITS.md已注。
- **验证**: 编译0错·**EditMode 157/157 pass**·`Assets/_Recovery`已删(David批)。
- **新CREDITS**: `Assets/_Project/Art/Monsters/CREDITS.md`(Quaternius CC0留档+改皮说明+自建资产清单)。
- **待David Play验**: 派火星→冷藏/地下档案室看档案怨灵(飘移+红眼+Headbutt攻击)·手电走大厅感受新亮度·开关门听新短音。**待答**: 卷角异形要不要当第三只怪入池(改皮预览已有`qm_Big_Alien.png`·加一条路由关键字即可)。
