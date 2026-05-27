---
name: accidentsquad-agent-team
description: Use when coordinating AccidentSquad's virtual development team for MVP planning, feature design, implementation review, or milestone planning. Covers the PM-led agent setup, role responsibilities, reusable prompts for each specialist agent, and the office-to-school-mission MVP direction.
---

# AccidentSquad Agent Team

## When To Use

Use this skill when the user wants to plan, review, or implement AccidentSquad work with a virtual team of specialist agents.

The default team is:

| Agent | Role | Main Responsibility |
|---|---|---|
| Zeno | Creative / Game Design Director | Story, core loop, progression, task categories, mechanic sanity checks |
| Laplace | Steam / Multiplayer Technical Agent | Host/join flow, network state, mission scene sync, Steam/Relay path |
| Hilbert | UI/UX Agent | Office computer, task selection, hotbar, settlement, shop, acquisition UI |
| Banach | Art Direction Agent | Rundown office, school scene, monster, notebook, visual readability |
| Sagan | QA Agent | Multiplayer smoke tests, mission completion/failure, rewards, hotbar, acquisition validation |

PM owner: Yan Dai.

## Current MVP Frame

AccidentSquad is a 1-4 player co-op commission-running game about a broke office that survives by taking increasingly strange outsourced jobs.

First playable loop:

```text
Solo Host / Create Host / Join Host
-> Rundown Office (HQ scene)
-> Office Computer [E]
-> Accept "找回被遗忘的作业本"
-> School Mission (School_LostItem_01 scene)
-> Find Homework Notebook [E]
-> Avoid Monster (HomeworkDebtCollector)
-> Green Exit [E]  ← only notebook carrier can complete this
-> Return to Office (HQ scene)
-> Claim Pending Rewards from Office Computer [E]
-> Buy Gear / Complete Tutorial Acquisition Hook
```

MVP job:

| Field | Value |
|---|---|
| Category | Lost Item Recovery |
| Job | 找回被遗忘的作业本 |
| Client | 焦急的家长 |
| Location | 废弃学校 |
| Objective | Find the notebook and return to the exit |
| Threat | One school anomaly (HomeworkDebtCollector) that patrols and chases players within range |
| Reward | +300G, +5 rep, +80 XP |
| Failure | +20G consolation, -2 rep, +35 hostile pressure |
| Player count | 1-4 |

The office starts at `-300G` funds / `300G` debt. After two successful lost-item jobs, the office computer offers a one-time tutorial acquisition of a level 0 office (costs `150G`, requires hostile takeover pressure below `70`), raising the office to level 2.

If all players are downed, the mission fails. Failed jobs add hostile takeover pressure. At `100/100` pressure with funds and reputation both negative: first trigger issues a final warning; the next failed job under the same bad conditions forces a hostile restructure (debt +500G, office level drops, progress resets) without a hard game-over.

## Implementation Status

All MVP scripts are implemented and wired. Run `Tools > Accident Squad > Setup All` — it now automatically runs the school MVP setup as part of the same pass.

### Scripts — all present

| Script | Path | Status |
|---|---|---|
| `OfficeTaskDefinition` | `Scripts/Office/` | ✅ ScriptableObject, task reward fields |
| `OfficeComputer` | `Scripts/Office/` | ✅ Pending reward, acquisition hook, NGO sync |
| `MvpMissionRuntime` | `Scripts/Office/` | ✅ Static handoff for active task + return scene |
| `MvpPendingReward` | `Scripts/Office/` | ✅ One-shot apply, idempotent claim |
| `LostItemMissionManager` | `Scripts/MVP/` | ✅ Searching→ReturnToExit→Completed/Failed state machine |
| `LostHomeworkItem` | `Scripts/MVP/` | ✅ IInteractable, server-validated pickup radius |
| `SchoolExitPoint` | `Scripts/MVP/` | ✅ IInteractable, carrier-only exit enforcement |
| `SchoolMonsterAI` | `Scripts/MVP/` | ✅ Patrol / Chase / Stunned / Distracted states |
| `PlayerHotbar` | `Scripts/MVP/` | ✅ 5 slots, medkit/stun spray/decoy/flashlight |
| `MvpHud` | `Scripts/MVP/` | ✅ Office panel + mission panel + bottom hotbar |
| `MvpConnectionLimiter` | `Scripts/Network/` | ✅ Caps at 4 players via connection approval |
| `CompanyData` / `CompanyState` | `Scripts/Settlement/SettlementManager.cs` | ✅ Full economy: funds, debt, rep, XP, pressure |
| `MvpProjectSetup` | `Editor/` | ✅ Generates School_LostItem_01 scene, patches player prefab |
| `MvpProjectValidator` | `Editor/` | ✅ Validates scene assets and network hookups |

### Economy rules (implemented)

| Rule | Value |
|---|---|
| Starting funds | -300G |
| Starting debt | 300G |
| Starting reputation | 0 |
| Office level | 1 |
| Homework job reward | +300G, +5 rep, +80 XP |
| Failure consolation | +20G, -2 rep |
| Hostile pressure per failure | +35 (more if funds/rep negative) |
| Tutorial acquisition cost | 150G (100G valuation × 1.5) |
| Tutorial acquisition requirement | pressure < 70, completed ≥ 2 lost-item jobs |
| Forced restructure trigger | pressure = 100 AND funds < 0 AND rep < 0, after ultimatum |

### Player hotbar (5 slots, starter loadout)

| Slot | Item | Key | Effect |
|---|---|---|---|
| 1 | 回血药 (Medkit) | 1 + LMB | Heal 30 HP, consumed |
| 2 | 定身喷雾 (Stun Spray) | 2 + LMB | Stun nearest monster 2.5s within 6m, consumed |
| 3 | 诱饵 (Decoy) | 3 + LMB | Distract nearest monster 4s within 12m, consumed |
| 4 | 手电 (Flashlight) | 4 + LMB | Toggle — not consumed |
| 5 | (empty) | 5 | — |

## One-Click Setup

```
Tools > Accident Squad > Setup All
```

This creates all prefabs, HQ scene, Mall_B2 prototype, AND School_LostItem_01 MVP scene in a single pass. No separate MVP setup step needed.

After running: open **HQ.unity** → Play → Start Host → approach the office computer → press E.

To validate before Play: `Tools > Accident Squad > MVP > Validate School MVP`

## Test Procedure

### Solo happy path
`HQ` → Start Host → Office Computer [E] → School → Notebook [E] → Avoid monster → Green exit [E] → HQ → Computer [E] → Reward claimed.

### Solo failure path
Enter school, let the monster kill you, confirm all-downed triggers failure, confirm return to HQ with failure penalty and pressure increase.

### Hotbar path
- Slot 1 (Medkit): take damage, use → HP recovers, slot empties
- Slot 2 (Stun Spray): use near monster → monster enters Stunned state for ~2.5s
- Slot 3 (Decoy): use near monster → monster moves toward decoy position for ~4s
- Slot 4 (Flashlight): use → toggles light, slot NOT consumed

### Two-client smoke
Host starts mission, second player joins before launch, both load school, one collects notebook, only notebook carrier can complete exit.

### Reward idempotency
Return to HQ, press E on computer repeatedly → reward applies exactly once.

### Progression hook
Complete 2 successful lost-item jobs, open computer → acquisition offer appears at 150G. Accept → office level becomes 2, second job category marked as future content.

## QA Go/No-Go Criteria

1. Solo host can complete the full loop: office → school → notebook → exit → claim reward.
2. Four players can join one host and load into school together.
3. All players see the same notebook pickup state (it disappears for everyone when collected).
4. Monster chases host and clients consistently (server-authoritative NavMesh AI).
5. Only the notebook carrier can complete the exit interaction.
6. Returning to the office applies rewards exactly once.
7. Each player has five hotbar slots; consumable use does not affect other players.
8. All-downed triggers mission failure and returns to HQ.
9. Hostile takeover pressure increases after failures; ultimatum and restructure fire at the right thresholds.
10. Tutorial acquisition correctly costs 150G and raises office to level 2.

## Coordination Workflow

1. Treat the user as PM. Ask for PM decisions only when a choice materially changes scope, tone, or schedule.
2. Keep one main coordinator voice. Specialist agents produce recommendations; the coordinator resolves conflicts and gives the PM a clear plan.
3. For new features, consult only the agents needed for that decision.
4. Convert agent output into concrete backlog items, acceptance criteria, and risks.
5. Keep MVP scope small: prove the office-to-mission-to-office loop before adding more task categories, deep acquisition logic, Steam features, or advanced UI.

## Role Prompts

### Zeno: Creative / Game Design Director

```text
You are Zeno, the Creative / Game Design Director for AccidentSquad.
PM: Yan Dai. Coordinator will consolidate your output.

Current MVP:
- 1-4 players run a nearly bankrupt commission office.
- Office computer accepts jobs; demo job: Lost Item Recovery, school scene, worried parent asks for homework notebook.
- One school anomaly (HomeworkDebtCollector) patrols and chases players.
- Players return to office and claim money, reputation, and experience.
- After two successful lost-item jobs, a tutorial level 0 office acquisition becomes available (150G, pressure < 70).
- Reputation affects which clients and job categories appear; office level ranges from 1 to 8.
- Hostile takeover pressure rises with failures; at 100 with funds/rep negative, forced restructure.

Output: story/tone, mechanic strengths, risks, scope cuts, task category ideas, economy rule suggestions.
Do not edit files unless explicitly asked.
```

### Laplace: Steam / Multiplayer Technical Agent

```text
You are Laplace, the Steam / Multiplayer Technical Agent for AccidentSquad.
PM: Yan Dai. Coordinator will consolidate your output.

Stack: Unity 6000.4.7f1, NGO 2.11.2, Unity Relay, CharacterController first-person.
Key pattern: NetworkManager.Singleton.IsServer for non-NetworkBehaviour scripts.
Scenes: HQ (lobby) -> School_LostItem_01 (mission) -> HQ (return), loaded via NGO SceneManager.

Focus: host/join flow, server-authoritative mission state, notebook/exit/reward sync, 4-player cap (MvpConnectionLimiter).

Output: architecture notes, reuse opportunities, P0 risks, two-week tasks, UI/QA interfaces.
Do not edit files unless explicitly asked.
```

### Hilbert: UI/UX Agent

```text
You are Hilbert, the UI/UX Agent for AccidentSquad.
PM: Yan Dai. Coordinator will consolidate your output.

All UI is OnGUI (no Canvas/TMPro). MvpHud shows: office panel (company state) or mission panel (objective, carrier, monster status) + bottom hotbar. OfficeComputer is IInteractable with E.

Focus: office computer IA (Tasks/Pending Rewards/Shop/Equipment/Office/Acquisition tabs future), hotbar rules (5 slots, flashlight non-consumable), settlement flow, acquisition hook UX.

Output: MVP UI flow, information architecture, mission HUD layout, hotbar rules, settlement flow, acceptance criteria.
Do not edit files unless explicitly asked.
```

### Banach: Art Direction Agent

```text
You are Banach, the Art Direction Agent for AccidentSquad.
PM: Yan Dai. Coordinator will consolidate your output.

Current visuals: primitive geometry with URP Lit materials. School scene has hallway, classrooms, desks, lockers, flickering lamps. Monster (HomeworkDebtCollector) is a red capsule. Notebook is a yellow flat box with glow light.

Focus: rundown commission office aesthetic, school horror atmosphere, monster silhouette readability, notebook visibility.
Style: cheap-office comedy plus light school horror — not pure horror, not cartoonish.

Output: visual direction, minimal asset list, office asset list, school asset list, monster/notebook direction, two-week priorities.
Do not edit files unless explicitly asked.
```

### Sagan: QA Agent

```text
You are Sagan, the QA Agent for AccidentSquad.
PM: Yan Dai. Coordinator will consolidate your output.

Core loop to prove:
Create/join room → office → computer [E] → school → find notebook [E] → monster chase → exit [E] → return office → claim reward [E] → money/rep/XP update → after two jobs, tutorial acquisition available.

Key server invariants: only notebook carrier completes exit; rewards apply once; all-downed = failure; 4-player cap enforced.

Output: P0 smoke checklist, 1/2/4 player matrix, success/failure cases, reward anti-duplication, hotbar tests, acquisition hook tests, go/no-go criteria.
Do not edit files unless explicitly asked.
```

## Default Backlog Shape

```text
Title:
Owner Agent:
Priority: P0/P1/P2
Goal:
Implementation Notes:
Acceptance Criteria:
Dependencies:
Risks:
```

## MVP Guardrails

- Single-player uses host mode. No offline non-network mode.
- Homework notebook is a mission objective; it does NOT occupy a hotbar slot.
- Notebook pickup and mission exit validated on server: player identity, distance, alive/downed state.
- Only the notebook carrier can complete the mission exit.
- Hotbar has exactly five slots for equipment/consumables.
- Flashlight slot is never consumed on use.
- Every visible hotbar item has a real gameplay effect.
- Only the host starts missions and performs office acquisition decisions.
- Rewards are pending after mission return; claimed from the office computer (idempotent).
- Do not expand all eight job categories before the first school mission is stable.
- No vehicle system for MVP; mission exit is a simple return-to-office trigger.
- No Steam achievements, Steam Cloud, or public matchmaking in MVP.

## Scope Cuts For MVP

Not built in the first slice:
- Full vehicle system
- Full eight-category content
- Full acquisition strategy layer
- Full decoration catalog
- Steam achievements / Cloud / public matchmaking
- Complex backpack inventory
- Multiple monster types
