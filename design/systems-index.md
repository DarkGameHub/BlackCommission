# Systems Index — Black Commission

> Bridges the framework to the real Unity project. Game code lives in
> `Assets/_Project/Scripts/<System>/`, NOT `src/`. Status reflects 2026-06-06 scan.
> **Test status is project-wide weak** — only 1 test file exists (see Risk below).

| System | Code Location | Files | Status | Design Doc | Tests |
|--------|---------------|-------|--------|------------|-------|
| **Networking** | `Assets/_Project/Scripts/Network/` | 10 | Implemented | `docs/mvp-core-loop.md` | ❌ none |
| **Mission (school/lost-item)** | `Assets/_Project/Scripts/Mission/` | 12 | Implemented | `docs/mvp-core-loop.md` | ❌ none |
| **Office / HQ economy** | `Assets/_Project/Scripts/Office/` | 16 | Implemented | `docs/mvp-core-loop.md` | ❌ none |
| **Player** | `Assets/_Project/Scripts/Player/` | 15 | Implemented | — | ❌ none |
| **UI / HUD / Settlement** | `Assets/_Project/Scripts/UI/` | 11 | Implemented | — | ❌ none |
| **Van transit** | `Assets/_Project/Scripts/Van/` | 2 | Partial | `@AGENTS.md` (van ritual) | ❌ none |
| **Equipment** | `Assets/_Project/Scripts/Equipment/` | 1 | Stub (flashlight only) | — | ❌ none |
| **Environment** | `Assets/_Project/Scripts/Environment/` | 1 | Stub (billboard) | — | ❌ none |
| **Audio** | `Assets/_Project/Scripts/Audio/` + `Network/AudioManager` | 2 | Partial | — | ❌ none |

## Key Components by System

- **Networking**: ConnectionManager, DisconnectHandler, HQController, HQSpawnManager,
  MvpConnectionLimiter, ProximityVoiceChat, AutoPort, QuickNetworkUI
- **Mission**: LostItemMissionManager, MvpMissionClock, SchoolMonsterAI, SchoolEntranceDoor,
  SchoolExitPoint, HidingSpot, evidence/homework items, MissionTimeOfDayDirector
- **Office**: OfficeComputer, OfficeTaskDefinition, MvpMissionRuntime, MvpPendingReward,
  CompanyData, SaveIO, OfficeDepartureVan, OfficeMonsterBestiary, OfficeCabinetStorage
- **Player**: PlayerController, PlayerInteraction, PlayerHotbar, CarrySystem, PlayerHealth,
  PlayerOxygen, PlayerCameraController, PlayerFirstPersonRig, PlayerCharacterModels/Palette
- **UI**: MvpHud, SettlementUIController, MainMenuUI, SettingsOverlay, BlackCommissionUiTheme

## Scenes

- `Assets/_Project/Scenes/HQ.unity` — HQ (also runtime-generated via `MvpSceneStyleDirector`)
- `Assets/_Project/Scenes/Snow_Lotus_01.unity` — current playable mission (白棘雪莲)
- `Assets/Scene/AbandonedBuilding_Blockout.unity` — blockout

## ⚠️ Top Risk: Test Coverage

This is a **host-authoritative 4-player co-op** game with **basically no automated tests**
(1 test file project-wide). The highest-risk, hardest-to-debug-manually areas:

1. **Mission state machine** (`LostItemMissionManager`, `MvpMissionRuntime`) — completion,
   partial return, failure transitions must sync to all clients.
2. **Settlement / reward math** (`MvpPendingReward`, `SettlementUIController`, `CompanyData`).
3. **Network sync** (`ConnectionManager`, `DisconnectHandler`, spawn/ownership).

Manual smoke testing (`Tools > Black Commission > MVP > Run Smoke Test`) is the current
safety net. Logic-level EditMode tests for the three areas above would catch the silent
desync/economy bugs that single-player playtesting cannot.
