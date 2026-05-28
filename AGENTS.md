# AccidentSquad Agent Rules

Yan Dai is the PM and final design decision maker.

AccidentSquad is a 1-4 player co-op commission-running game about a nearly bankrupt accident-handling office taking strange outsourced jobs. The current signature loop is:

`HQ office -> office computer -> gear/shop -> team boards dispatch van -> in-van transit -> mission site -> objective / partial return choice -> van return -> HQ settlement`.

## Long-Term Project Rules

- Treat Lethal Company as a production-method reference only: strong repeatable rituals, low-cost readable staging, co-op extraction tension, and darkness with clear navigation. Do not copy its assets, UI, monsters, ship, quota fiction, item list, or map layouts.
- AccidentSquad's own identity is Municipal Debt Noir: civic teal, dead rubber black, aged paper, sodium amber, restrained dispatch green, and stamp red.
- The memorable pillars are the broke office, the dispatch van ritual, suspicious civic paperwork, partial settlement choices, hostile takeover pressure, and weird local-client jobs.
- Prioritize playability and logical scale before visual decoration. Furniture, characters, monsters, vehicles, doors, props, and colliders must make physical sense in Unity.
- The playable HQ is currently runtime-generated in `MvpSceneStyleDirector`; Blender assets are supporting/imported assets unless the runtime flow explicitly uses them.
- Keep multiplayer state server-authoritative. Mission selection, notebook pickup, van lockers, completion, partial return, failure, and rewards must sync to clients.
- This is a Unity game project, not a backend project. The MVP should stay in Unity + Netcode for GameObjects with host-authored state; Steam/Relay can be considered as transport/services later, not as a custom backend.
- Do not push, commit, delete large assets, or rewrite scene YAML unless the user explicitly asks.

## Custom Agents

Project-specific agents live in `.codex/agents/`:

- `zeno`: creative and game design direction.
- `laplace`: Unity multiplayer, Steam/Relay transport, Netcode, scene-flow architecture.
- `hilbert`: UI/UX, office computer, hotbar, settlement, mission HUD.
- `banach`: art direction, modeling, lighting, Blender-to-Unity pipeline.
- `sagan`: QA, smoke tests, go/no-go, regression risks.

Use them explicitly, for example:

`Use banach to critique the HQ and van style, then use sagan to list playability blockers.`

`Spawn zeno, laplace, and hilbert in parallel to review the next mission design.`

## Current Validation Baseline

When possible, run:

- `git diff --check`
- `Tools > Accident Squad > MVP > Validate School MVP`
- `Tools > Accident Squad > MVP > Run Smoke Test`

If Unity is unavailable, state that clearly and run local static checks only.
