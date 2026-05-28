# AccidentSquad MVP Core Loop

## Status

Code-level MVP slice is implemented. The Unity editor must run `Tools > Accident Squad > MVP > Setup School MVP` once to generate and wire the playable school scene, task asset, office computer, HUD, and Build Settings.

## Core Premise

Players run a nearly bankrupt "accident handling" office in a city where every uncomfortable problem has been outsourced. Parents, schools, companies, landlords, and executives all post jobs they do not want to handle themselves.

The office starts at level 1: broken furniture, debt notices, a second-hand computer, and almost no clients. The first available category is simple lost-item recovery. The demo job is from a parent who wants the team to recover a child's homework notebook from a school after hours.

The joke is that even a tiny job can become absurdly dangerous. The long-term satire is that the company can grow by staying reputable and taking slower, cleaner work, or by chasing money and reputation collapse until only darker clients remain.

## Visual Direction

The MVP style is "outsourced civic horror": a cheap debt-soaked office connected to cold public-service spaces. Both the office and school should use the same language:

- Green terminal glow for anything controlled by the company computer.
- Red takeover / overdue-warning signage for pressure, danger, and hostile acquisition.
- Cheap gray-green institutional surfaces, second-hand furniture, paper notices, exposed utility lighting, and cluttered shelves.
- Equipment should look like improvised office purchases, not fantasy loot: pharmacy box, cheap flashlight, aerosol spray, noisy decoy.
- Monsters read as bureaucratic threats made physical. The first school monster is a homework debt collector: red coat, ledger, long arms, glowing warning eyes.

Avoid clean sci-fi, cozy office decor, or generic school grayboxes. The world should feel like a city has outsourced responsibility until the paperwork started hunting people.

## MVP Player Flow

```mermaid
flowchart TD
    A["主菜单"] --> B{"选择模式"}
    B --> B1["单人游玩 / 创建主机"]
    B --> B2["加入主机"]
    B1 --> C["破旧事务所"]
    B2 --> C

    C --> D["办公室电脑"]
    D --> D1["电脑商店采购道具\nF1 回血药 / F2 诱饵 / F3 喷雾 / F4 手电"]
    D1 --> E["锁定任务: 找回作业本"]
    E --> E2["全员进入事务所面包车"]
    E2 --> F["车内等待转场\n到达学校门口"]

    F --> G["寻找作业本 / 可选登记簿"]
    G --> H{"任务中发生什么?"}
    H -->|找到作业本| I["作业本携带者前往校门事故车"]
    H -->|找到登记簿| H2["拍照留证\n增加外快"]
    H -->|被怪物追击| J["用走位 / 喷雾 / 诱饵 / 回血药求生"]
    H -->|主动止损| M["事故车提前返程\n部分结算"]
    H2 --> G
    J --> G
    H -->|全员倒地| K["任务失败"]

    I --> L["事故车完整返程\n任务成功"]
    K --> M["返回事务所"]
    L --> M
    M --> N["办公室电脑领取结算"]

    N --> O{"结算结果"}
    O -->|成功| P["获得金钱 / 声望 / 经验"]
    O -->|失败| Q["少量安慰金 / 声望下降 / 吞并压力上升"]

    P --> R{"完成 2 个失物任务\n且资金 >= 150G\n且吞并压力 < 70?"}
    R -->|是| S["吞并 0 级事务所\n事务所升到 2 级"]
    R -->|否| D
    S --> D

    Q --> T{"吞并压力 100\n且资金/声望仍为负?"}
    T -->|第一次| U["收到竞对最后通牒"]
    T -->|再次失败| V["被竞对吞并重组\n债务增加，进度回退"]
    T -->|否| D
    U --> D
    V --> D
```

The MVP now uses the dispatch van as a physical mission gate. The office
computer locks the commission, but the team must gather in the van before the
host can drive. A short in-van transit overlay covers scene loading: players see
the back seats, driver silhouette, and passing civic scenery before arriving at
the mission entrance.

```text
Main Menu
  -> Solo Host / Create Host / Join Host
  -> Rundown Office
  -> Office Computer
  -> Buy Gear From Computer
  -> Drop Spare Gear On HQ Storage Mat With G
  -> Accept "Missing Homework Notebook"
  -> Gather In Dispatch Van
  -> Host Drives / Facing-Seat Van Transit
  -> School Gate Van / Exterior Forecourt
  -> Open School Entrance Door
  -> Find Homework Notebook
  -> Avoid Monster
  -> Return To Van
  -> Return to Office
  -> Claim Pending Rewards From Office Computer
  -> Buy Gear / Handle Acquisition Hooks
```

The return van is not only a success gate. Players may return early even before
all objectives are complete. A full objective return grants the normal reward;
an early return grants a partial settlement and raises takeover pressure only
lightly. Total failure, such as everyone being downed, still uses the failure
settlement path.

## Office Computer UX

The office computer is the MVP hub. It should behave like a compact diegetic terminal rather than a full management screen: one interaction point, clear keyboard commands, no nested economy menus, and no mouse-only flow. In the office, pressing `E` on the computer performs the highest-priority action in this order: claim pending reward, accept the available lost-item job, or accept the tutorial acquisition when it is unlocked and affordable. Gear purchasing is available only while near the computer and only when no pending reward is waiting: `F1` medkit, `F2` decoy, `F3` stun spray, `F4` flashlight. In HQ, `G` drops one item from the selected hotbar slot onto the office floor storage mat; the dropped counts live on the office computer so the team can retrieve spare gear before a later run.

The computer panel must always show funds/debt, reputation, office level/XP, hostile takeover pressure, current lost-item progress, the next available computer action, and shop commands. Purchased gear must immediately appear in the five-slot hotbar with an icon and quantity; unpurchased gear must not appear. The mission item never occupies a hotbar slot.

Acceptance criteria: after returning from a mission, the computer clearly blocks shopping until the pending settlement is claimed; pressing `E` applies reward or failure results exactly once; insufficient funds produces readable feedback and does not change the hotbar; after two successful lost-item jobs the acquisition prompt appears only when pressure is below `70`, costs `150G`, and raises the office to level 2.

## HQ And Map Design Framework

HQ is a dense physical menu, not an exploration map. It should stay small and immediately readable.

| HQ Zone | Purpose |
|---|---|
| Entry / rally area | Player spawn, join flow, team gathering, "broke company clocking in" tone |
| Office computer | Task start, reward claim, gear shop, acquisition prompt, company state |
| Equipment shelf / floor mat | Physical reminder that gear is bought before jobs; spare hotbar gear can be dropped with `G` and picked back up |
| Dispatch van | Team rally point, drive confirmation, loading ritual, return/partial settlement anchor |
| Company status wall | Debt, reputation, hostile takeover pressure, locked categories, acquisition hints |
| Upgrade display area | Small visible progression hooks: repaired furniture, new shelf space, better lighting |

School and future maps should use three reusable spatial rules:

1. Main spine plus side loops: players understand the route quickly, but have chase choices.
2. Risk gradient rooms: low-risk rooms teach layout, medium-risk rooms hold clues/consumables, high-risk rooms hold the real objective.
3. Objective and exit separation: finding the item starts the second pressure phase rather than ending the job.

Future map archetypes should be defined by one spatial gimmick before content production:

| Map | Spatial Gimmick |
|---|---|
| Abandoned school | Corridor loops, classrooms, lockers, objective-to-exit retreat |
| Underground mall | Shutters and flooded service routes change paths |
| Old apartment block | Vertical stairs/elevator risk and floor-by-floor searching |
| Night office floor | Cubicle maze, access cards, temporary power cuts |
| Hospital back-of-house | Curtains, carts, beds, light/noise management |
| Logistics warehouse | Long shelf aisles, roll-up doors, large carried objectives |

## First Mission

| Field | Value |
|---|---|
| Category | Lost Item Recovery |
| Job | Missing Homework Notebook |
| Client | Worried Parent |
| Location | School |
| Objective | Find the notebook and return to the van; optional: photograph the overdue ledger in the records room |
| Threat | One school anomaly that patrols and chases players within range |
| Reward | Money, reputation, experience |
| Player count | 1-4 |

The mission item does not occupy a hotbar slot. The optional overdue ledger is a quick evidence interaction, not an inventory item. The hotbar is reserved for equipment and consumables.

Notebook pickup, optional evidence pickup, and exit completion are server-validated. The server checks the acting player, distance, alive/downed state, and notebook carrier identity so a client cannot remotely pick up the objective or let a non-carrier finish the job.

## Progression Model

| System | MVP Rule |
|---|---|
| Money | Used for equipment, recovery items, office decoration, debt, and acquisitions |
| Reputation | Ranges from -100 to 100; changes which clients and job categories appear |
| Office Level | Ranges from 1 to 8; represents company scale, not combat power |
| Experience | Earned from successful jobs; failed jobs do not level the company |
| Acquisition | Tutorial acquisition becomes available after two successful lost-item jobs; hostile acquisition pressure rises after failed jobs |

For MVP, acquisition is a tutorial action rather than a full company market. The office starts at `-300G` funds / `300G` debt, and each successful homework job pays `300G`, so two clean jobs make the first acquisition affordable without removing the pressure of debt. After two successful lost-item jobs, the office computer offers a one-time level 0 office acquisition. The level 0 office is valued at `100G`; acquiring it costs `150G` and requires hostile takeover pressure below `70`. Accepting it raises the office to level 2 and marks the second job category as unlocked future content.

Failure pressure is the counterweight. If all players are downed, the mission fails and the team returns to the office. Failed jobs add hostile takeover pressure, especially when funds and reputation are below zero. At `100/100` pressure, if the office is still broke and disliked, the first trigger issues a final warning. The next failed job under the same bad conditions lets a competitor forcibly acquire/restructure the office: debt increases, office level drops by one, experience and lost-item progress reset, and the player keeps operating under worse terms rather than hitting a hard game-over screen.

## Planned Job Categories

1. Lost Item Recovery
2. Scene Cleanup
3. Personnel Rescue
4. Anomaly Handling
5. Debt and Dispute Mediation
6. Corporate Crisis Management
7. Security and Escort
8. Black Commissions

Only Lost Item Recovery is playable in the first MVP. Other categories appear as locked office-computer entries.

## Agent Team Setup

The current virtual team is:

| Agent | Role | Responsibility |
|---|---|---|
| Zeno | Creative / Game Design Director | Story, core loop, progression, category logic, mechanic sanity checks |
| Laplace | Unity Multiplayer Technical Agent | Host/join flow, mission scene sync, network state, Steam/Relay transport path |
| Hilbert | UI/UX Agent | Office computer, task selection, hotbar, settlement, shop, acquisition UI |
| Banach | Art Direction Agent | Rundown office, school scene, monster, notebook, visual readability |
| Sagan | QA Agent | Multiplayer smoke tests, mission completion/failure, rewards, hotbar, acquisition validation |

PM owner: Yan Dai.

Project-specific Codex custom agents live in `.codex/agents/`. Longer-term product direction lives in [AccidentSquad Long-Term Roadmap](accidentsquad-long-term-roadmap.md).

## First Implementation Slice

The first code slice should avoid heavy scene-YAML edits and establish reusable gameplay scripts:

1. Task definition data for office jobs.
2. Office computer entry point that starts the selected mission.
3. School lost-item mission manager.
4. Notebook pickup objective.
5. School exit point.
6. Basic server-authoritative school monster.
7. Five-slot player hotbar.
8. Reward application through the existing company state.

Current script slice:

| Script | Purpose |
|---|---|
| `Assets/_Project/Scripts/Office/OfficeTaskDefinition.cs` | ScriptableObject definition for office jobs |
| `Assets/_Project/Scripts/Office/OfficeComputer.cs` | Interactable office computer that selects jobs and dispatches the van |
| `Assets/_Project/Scripts/Office/MvpMissionRuntime.cs` | Runtime handoff from office scene to mission scene |
| `Assets/_Project/Scripts/Office/MvpPendingReward.cs` | One-shot pending reward handoff for office-computer claiming |
| `Assets/_Project/Scripts/MVP/LostItemMissionManager.cs` | School lost-item mission state, reward grant, return-to-office flow |
| `Assets/_Project/Scripts/MVP/LostHomeworkItem.cs` | Interactable homework notebook objective |
| `Assets/_Project/Scripts/MVP/SchoolExitPoint.cs` | Mission van rear cabin, shared locker, and return-to-office point |
| `Assets/_Project/Scripts/MVP/VanTransitOverlay.cs` | In-van outbound/return transition overlay during scene loads |
| `Assets/_Project/Scripts/MVP/SchoolMonsterAI.cs` | Server-authoritative school monster chase behavior |
| `Assets/_Project/Scripts/MVP/PlayerHotbar.cs` | Five-slot player equipment/consumable hotbar |
| `Assets/_Project/Scripts/MVP/PlayerFirstPersonRig.cs` | Local first-person hands and held-item models |
| `Assets/_Project/Scripts/MVP/MvpSceneStyleDirector.cs` | Runtime office/school style pass for MVP visual cohesion |
| `Assets/_Project/Scripts/MVP/MvpHud.cs` | Temporary MVP HUD for office state, mission objective, monster status, and hotbar |
| `Assets/_Project/Scripts/Network/MvpConnectionLimiter.cs` | Connection approval guard that caps MVP sessions at 4 players |
| `Assets/_Project/Editor/MvpProjectSetup.cs` | One-click Unity editor setup for the school MVP scene and office hookup |
| `Assets/_Project/Editor/MvpProjectValidator.cs` | One-click Unity editor validation for MVP assets, scenes, and network hookups |

## Unity Hookup Checklist

Run `Tools > Accident Squad > MVP > Setup School MVP` after the base project setup. The menu action performs the hookup below:

1. Creates `Assets/_Project/Settings/Tasks/MissingHomeworkNotebook.asset`.
2. Adds a new `MVP_OfficeComputer` to `HQ.unity` and assigns the task asset.
3. Adds `MvpHud` to HQ and the school scene.
4. Adds `PlayerHotbar`, `PlayerFirstPersonRig`, and `ClientNetworkTransform` to the player prefab if missing.
5. Enables Netcode connection approval and adds `MvpConnectionLimiter` to cap sessions at 4 players.
6. Generates `Assets/_Project/Scenes/School_LostItem_01.unity`.
7. Builds a school graybox with exterior forecourt, entrance door, classroom, side record office, shelf obstacles, lockers, lighting, notebook, optional overdue ledger evidence, exit van marker, and monster.
8. Adds `LostItemMissionManager`, `LostHomeworkItem`, `SchoolExitPoint`, and `SchoolMonsterAI` scene objects.
9. Bakes NavMesh for the school scene when Unity's NavMesh builder is available.
10. Updates Build Settings to `HQ -> School_LostItem_01 -> Mall_B2`.

Then run `Tools > Accident Squad > MVP > Validate School MVP` before Play Mode.

MVP single-player should still use host mode. Non-network offline mode is intentionally not the target for the first playable build.

## MVP QA Gates

The first playable MVP is not accepted until these pass:

1. Solo host can start in office, accept school task, find notebook, return, and claim rewards.
2. Four players can join one host and load into the school together.
3. All players see the same notebook pickup state.
4. The monster chases host and clients consistently.
5. Returning to the office applies rewards once, not repeatedly.
6. Each player has five hotbar slots and item use does not affect other players incorrectly.
7. Purchased gear appears as hotbar icons and as first-person held-item models; unpurchased gear stays absent.
8. Host-only actions are enforced for starting missions and acquisition decisions.

## MVP Test Procedure

Run these after `Setup School MVP` and `Validate School MVP`:

1. Solo host happy path: `HQ -> Start Host -> Office Computer -> Board Van -> School Gate -> Open Entrance Door -> Optional Ledger -> Notebook -> Return Van -> HQ -> Claim Reward`.
2. Solo host failure path: enter school, let the monster down the player, confirm return to HQ with failure reward/penalty and hostile takeover pressure.
3. Shop and hotbar path: in HQ, stand near the computer and buy gear with `F1-F4`; confirm only purchased gear appears in the icon hotbar and first-person hand model, then use `1-5` to select slots. Press `G` near the office storage mat to drop one selected hotbar item, then press `E` on the floor item to pick it back up. Use a medkit after taking damage, use stun spray near the monster and confirm chase pauses, use a decoy and confirm the monster is distracted, then select the flashlight slot and confirm it toggles without being consumed.
4. Two-client smoke: host starts mission, second player joins before mission start, both board the dispatch van, both load into school, one collects notebook, and only the notebook carrier can trigger full-success return.
5. Partial-return smoke: host starts mission, opens the school return van before collecting the notebook, clicks once to arm the warning, clicks again to return, and confirms partial settlement.
6. Reward idempotency: after returning to HQ, press `E` on the computer repeatedly and confirm the pending reward is applied only once.
7. Progression hook: complete two successful lost-item jobs, press `E` on the office computer, and confirm the level 0 acquisition costs `150G` and raises the office to level 2.
8. Build Settings check: `HQ` is first, `School_LostItem_01` is second, and `Mall_B2` remains available for the older rescue prototype.

## Scope Cuts For MVP

Do not build these in the first slice:

- Full drivable vehicle simulation. The MVP keeps the dispatch van as a boarding, transit, supply, and return ritual.
- Full eight-category content.
- Full acquisition strategy layer.
- Full decoration catalog.
- Steam achievements, Steam Cloud, public matchmaking.
- Complex backpack inventory.
- Multiple monster types.

The MVP should prove the office-to-mission-to-office loop first.

The mission exit is the parked dispatch van rear cabin: players can take limited supplies, return early for partial settlement, or return with the objective for full settlement.
