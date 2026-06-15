# Systems Index — Black Commission

> Bridges the framework to the real Unity project. Game code lives in
> `Assets/_Project/Scripts/<System>/`, NOT `src/`. **Rescanned 2026-06-14**
> (post tower migration). ~85 runtime scripts + 7 EditMode test files.
>
> Note: `CLAUDE.md` / `AGENTS.md` still reference `MvpSceneStyleDirector` as the
> runtime HQ generator — **that script no longer exists**; HQ now lives in
> `HQ.unity`, dressed at play by `HqOfficePropRestorer` + `HqOfficeLightingPass`.
> (Those project-rule files are PM-owned; flagged here, not edited.)

| System | Code Location | Files | Status | EditMode Tests |
|--------|---------------|-------|--------|----------------|
| **Networking** | `Scripts/Network/` | 10 | Implemented | ❌ none (sync untested) |
| **Level / Tower generation** | `Scripts/Level/` (+ `Topology/`) | 9 | Implemented | ✅ `TowerTopologyTests` |
| **Mission (tower)** | `Scripts/Mission/` (+ `Core/`) | 8 | Implemented | ✅ `TowerMissionLogicTests`, `PowerGateLogicTests` |
| **Office / HQ economy** | `Scripts/Office/` (+ `Core/`) | 20 | Implemented | ✅ `CompanyStateTests`, `MissionRewardCalculatorTests`, `MvpMissionClockTests`, `OfficeTaskSettlementNoteTests` |
| **Player** | `Scripts/Player/` | 16 | Implemented | ❌ none |
| **UI / HUD / Settlement / Menus** | `Scripts/UI/` | 14 | Implemented | ❌ none |
| **Van transit** | `Scripts/Van/` | 2 | Implemented | ❌ none |
| **Environment (FX)** | `Scripts/Environment/` | 4 | Implemented | ❌ none |
| **Audio** | `Scripts/Audio/` + `Network/AudioManager` | 2 | Partial (synth SFX) | ❌ none |
| **Equipment** | `Scripts/Equipment/` | 1 | Stub (flashlight only) | ❌ none |

## Key Components by System

- **Networking**: ConnectionManager, DisconnectHandler, HQController, HQSpawnManager,
  MvpConnectionLimiter, ProximityVoiceChat, AutoPort, GameBuild, AudioManager, QuickNetworkUI
- **Level**: `Core` plan/topology = TowerPlanV8 → TowerTopology / TopoGraph → TowerLayout /
  TowerLayoutGenerator; TowerRoomCatalog, RoomDef, RoomSlot, Connector. Build + verify in
  Editor: `Editor/TowerV8WhiteboxBuilder`, `Editor/TowerWalkValidator`. NavMesh = separate asset.
- **Mission**: TowerMissionManager + `Core/TowerMissionLogic`; PowerGateBreaker + `Core/PowerGateLogic`
  (power gate to Floor 2); EcoColumnCarriable (the heavy two-hand objective carry); TowerVanDepartLever,
  MissionVanExitPoint, IInteractable
- **Office**: OfficeComputer, `Core/OfficeTaskDefinition`, `Core/CompanyState`,
  `Core/MissionRewardCalculator`, `Core/MvpMissionClock`, `Core/MvpTaskCategory`,
  `Core/MvpMissionResultKind`, CompanyData, SaveIO, MvpPendingReward, MvpMissionRuntime,
  OfficeCabinetStorage, OfficeDepartureVan, OfficeGroundItemPickup, OfficeMonsterBestiary,
  MonsterBestiaryProgress, HqOfficePropRestorer, HqOfficeLightingPass
- **Player**: PlayerController, PlayerInteraction, PlayerHotbar, CarrySystem + Carriable,
  PlayerHealth, PlayerOxygen, PlayerCameraController, PlayerFirstPersonRig,
  PlayerCharacterModels / PlayerCharacterPalette, PlayerProfile, PlayerGestures,
  PreviewWalker, ClientNetworkTransform, PlayerInputActions
- **UI**: MvpHud, SettlementUIController + SettlementCardOverlay, MainMenuUI, CrewPickerScreen,
  SettingsOverlay, BlackCommissionUiTheme, MvpLocale (localization keys), MvpTmpFontProvider,
  CRT effects (CrtMenuStage, CrtScreenSkew), DisplaySettings, BrightnessController
- **Van**: VanCabin, VanTransitOverlay

## Scenes

- `Assets/_Project/Scenes/HQ.unity` — HQ (office dressed at play via HqOffice* passes)
- `Assets/_Project/Scenes/Tower_EarthCoast_01.unity` — current playable mission (Earth Coast No.1)

## Design Docs

| GDD | Status |
|---|---|
| `design/gdd/level-map-generation.md` | In Design |
| `design/gdd/mission-state-machine.md` | Needs Revision |
| `design/gdd/office-economy-progression.md` | Needs Revision |
| `design/gdd/monster-echo-mold.md` | Designed |
| `design/gdd/monster-system.md` | Designed |

Cross-review (2026-06-14): `design/gdd/gdd-cross-review-2026-06-14.md` — flagged the two "Needs Revision".
Monster System (2026-06-14): framework + Danger-Level escalation; resolves cross-review C4 (clock ownership) — mission-state-machine fixed clock to be reworked to danger_level.
Monster GDD added 2026-06-14 (Echo Mold) — replaces the abandoned "Auditor" concept; art-bible §5 amended to allow infection creatures.

- Pillars/progression: `design/game-pillars.md` · Art: `design/art/art-bible.md`

## ⚠️ Top Gaps (reframed)

Test coverage is no longer the headline risk (7 EditMode files now cover Level/Mission/Office
*pure logic*). The real open gaps:

1. **Host-authoritative SYNC paths are untested.** EditMode tests cover deterministic logic;
   the actual replication of pickup/carry/power-gate/completion/partial-return to clients is
   validated only by manual smoke tests — the likeliest place for silent desync.
2. **The signature monster (Auditor / 核查专员) is unbuilt and undocumented.** No AI script in
   `Scripts/` (bestiary *data* only), and **no monster GDD**. Designed on paper in
   `art-bible.md §5` + session notes; gated on multiplayer.
3. **No formal concept/vision GDD.** Concept lives in `game-pillars.md` + `@AGENTS.md`, not a GDD.
4. **Equipment** is a single flashlight stub.
