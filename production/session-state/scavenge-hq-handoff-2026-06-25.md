# Autonomous Handoff — Scavenge Loop + HQ (2026-06-25)

PM left mid-session with an autonomous mandate: improve the HQ (LC-referenced), then do the
level/map design and **connect both missions end-to-end ("打通两关")**. Later authorized me to
restart Unity to revive the dead bridge + granted full machine access.

## Bridge status: REVIVED ✅
The mcp-unity bridge had wedged (server stopped on a domain reload). Per PM authorization I:
`taskkill /F /IM Unity.exe` (3 instances) → cleared `Temp/UnityLockfile` → **headless batch compile
gate** (`-batchmode -nographics -quit`) which confirmed **0 `error CS` / no Safe Mode** (so my
unverified batch compiles) → relaunched the full editor → bridge answered. Editor is currently
running with HQ open. Driver = `node tools/ws-unity-call.cjs <method> '<json>' <ms>`
(execute_menu_item / get_gameobject{idOrName} / get_scene_info / save_scene). The build can drop the
WS reply at ~30s on first-pass material compile — the menu run is synchronous so it still completes;
verify via get_gameobject regardless.

## State of work

### ✅ DONE + verified
- **HUD**: resolution-adaptive (`MvpHud` global `GUI.matrix`@1080; `ScavengeCargoZone` strip matches);
  LC-style always-on inventory bar (48px, empty=outline, selected=amber corner brackets, centered). PM confirmed look.
- **Compiles clean** (headless gate): client-preference wiring + two-commission terminal + commission builder.
- **Client-preference (Option 2)**: `OfficeTaskDefinition.clientType/favouredCategoryIds/FavoursCategoryId`;
  `ScavengeCargoZone.TryStow` flags `IsFavoured` → calculator pays favoured categories ×1.3.
- **Two-commission terminal**: `OfficeComputer` pool (Resources/Tasks, networked SelectedCommissionIndex,
  host clicks to switch) + `MvpHud.DrawTabCommissions` selectable rows. Back-compat with 1 task.
- **Commissions + item library BUILT** (`ScavengeCommissionBuilder`, ran via bridge, verified on disk):
  Resources/Tasks = `TowerEarthCoast_01` (Commissioned, favours effects/civic/fixtures) + `FreeSalvage_Map2`
  (Free Salvage, scene `Map2_Procedural`); Resources/Scavenge/Items = 15 items across the 12 categories.
- **HQ → Mars Freight BUILT + SAVED + verified**: ran `HqMarsFreightWhitebox`; `HQ_MarsWhitebox` active
  (104-obj hierarchy), `MVP_OfficeComputer` active + repositioned into the nest, old `HQ_OptionA`
  deactivated. `HQ.unity` saved (isDirty=false).

### 🔴 NEXT — "打通两关" (bridge is UP, do verified)
1. **⚠ Map2 commission will ERROR on dispatch until wired**: `Map2_Procedural` is (a) NOT in Build
   Settings → `NetworkManager.SceneManager.LoadScene("Map2_Procedural")` fails; (b) has no mission rig
   (no ScavengeMissionManager/CargoZone/LootSpawner/depart); (c) uses single-player `MapSiteRuntime`
   (PM chose deterministic `GridMapNetworkBuilder`). **Until wired, don't dispatch commission #2.**
2. **Tower → scavenge**: scene still runs `TowerMissionManager` (eco-column) — mismatch with its now-scavenge
   commission. Deactivate eco-column mission; add ScavengeMissionManager + ScavengeCargoZone (reuse the
   existing cargo BoxCollider) + LootSpawner + depart trigger; scatter `LootAnchor`s (raycast-grid onto
   floors is the robust generic approach). Eco-column prop → Heavy fixture item (already in the library as `fix_eco_column`).
3. Plan: write one reusable `ScavengeSceneWiring` editor builder (rig-injector + raycast anchor scatter,
   modeled on `ScavengePlaytestSceneBuilder.AddMission/AddCargoZone/AddLootSpawner`), + "Wire Tower" /
   "Wire Map2" menu entries (Map2 also: GridMapNetworkBuilder swap + AddToBuildSettings). Run each via
   bridge → verify (get_gameobject) → save_scene.
4. **Smoke test** (Play → Host): terminal lists 2 commissions → select each → board → mission scene →
   pick up + deposit (weight-gated) → depart → settlement total (favoured ×1.3 on Tower) → return → HQ → claim.

## Locked decisions
- Option 2 client-preference (Tower Commissioned favours effects/civic/fixtures; Map2 Free Salvage).
- HUD bar centered, 48px, adaptive. HQ = Mars Freight (concept LOCKED — iterate execution only).
- Map2 multiplayer = server-seeded deterministic `GridMapNetworkBuilder`.
