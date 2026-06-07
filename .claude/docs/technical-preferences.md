# Technical Preferences

## Engine & Language

- **Engine**: Unity 6 (6000.4.7f1)
- **Language**: C#
- **Rendering**: URP (verify in ProjectSettings if a system depends on it)
- **Physics**: Unity built-in (PhysX)

## Input & Platform

- **Target Platforms**: PC (Windows)
- **Input Methods**: Keyboard/Mouse (gamepad TBD)
- **Primary Input**: Keyboard/Mouse
- **Gamepad Support**: TBD
- **Touch Support**: None
- **Platform Notes**: 1–4 player online co-op; first-person.

## Networking

- **Stack**: Netcode for GameObjects (NGO)
- **Authority**: Host/server-authoritative. Mission selection, notebook pickup,
  van lockers, completion, partial return, failure, and rewards must sync to clients.
- **Transport**: Steam/Relay considered later as transport/services, NOT a custom backend.

## Naming Conventions

- **Classes**: PascalCase
- **Variables**: camelCase (private fields per existing code conventions)
- **Events**: PascalCase (C# events / UnityEvents)
- **Files**: One MonoBehaviour per file, filename matches class
- **Scenes/Prefabs**: Match existing `Assets/_Project` conventions
- **Constants**: PascalCase or UPPER_CASE per existing code

## Performance Budgets

- **Target Framerate**: 60 FPS
- **Frame Budget**: ~16.6 ms
- **Draw Calls**: TBD
- **Memory Ceiling**: TBD

## Testing

- **Framework**: Unity Test Framework (EditMode/PlayMode)
- **Minimum Coverage**: Not enforced; prioritize playability per `@AGENTS.md`
- **Required Tests**: Networking sync, mission state machine, settlement/reward formulas
- **Project validation**: `Tools > Black Commission > MVP > Validate School MVP`
  and `Tools > Black Commission > MVP > Run Smoke Test` (run in Unity when available)

## Forbidden Patterns

- Do not push, commit, delete large assets, or rewrite scene YAML unless explicitly asked.
- Do not copy Lethal Company assets, UI, monsters, ship, quota fiction, item list, or maps.
- No custom backend — stay in Unity + NGO with host-authored state.

## Allowed Libraries / Addons

- Netcode for GameObjects
- TextMesh Pro
- mcp-unity (editor bridge, port 8090)

## Architecture Decisions Log

- [No ADRs yet — use /architecture-decision to create one]

## Engine Specialists

- **Primary**: unity-engine specialist
- **Language/Code Specialist**: C# / gameplay programmer
- **Shader Specialist**: shader specialist (URP)
- **UI Specialist**: UI specialist (uGUI / TMP)
- **Routing Notes**: Runtime HQ scene generation lives in `MvpSceneStyleDirector`.
  Note: project also has Codex-side agents in `.codex/agents/` (zeno, laplace,
  hilbert, banach, sagan) — see `@AGENTS.md`.

### File Extension Routing

| File Extension / Type | Specialist to Spawn |
|-----------------------|---------------------|
| `.cs` (game code) | C# / gameplay programmer |
| `.shader` / `.shadergraph` / material | Shader specialist |
| UI prefabs / screens (uGUI/TMP) | UI specialist |
| `.unity` scene / `.prefab` | Unity engine specialist |
| General architecture review | technical-director |
