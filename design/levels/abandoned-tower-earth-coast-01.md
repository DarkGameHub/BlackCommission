# Level: 地球海岸壹号 · 烂尾预售楼 (Earth Coast No.1 — Abandoned Pre-Sale Tower)

> First designated-commission map after the school lost-item demo. Builds directly
> on the existing `Assets/Scene/AbandonedBuilding_Blockout.unity` blockout — room
> names below match that scene's GameObject names so this doc is buildable as-is.
> Decisions locked 2026-06-06 (PM Yan Dai): objective = sales scale model;
> floor-2 gate = restore power; objective carry = heavy two-hand carry.

## Quick Reference

- **Area/Region**: Coastal infection zone, Earth surface, 2098
- **Type**: Mixed — vertical exploration + extraction under chase
- **Estimated Play Time**: 15–20 min (coordinating 1–4p group); ~10–12 min solo rush
- **Difficulty**: 5/10 (vs school demo 3/10)
- **Prerequisite**: Office computer accepts the "楼盘沙盘采回" commission; team boards dispatch van
- **Status**: Graybox exists (blockout) → this doc drives the Layout→Graybox content pass

## Narrative Context

- **Story Moment**: Early designated-commission tier. The player is still "a broke
  office chasing bills," beginning to notice how strange Mars clients' wants are.
- **Building identity**: A pre-exodus luxury pre-sale tower, **「阿瑞斯预售·地球海岸壹号」**,
  marketed as "owning the last *real* Earth coastline home." Buyers emigrated to Mars;
  the project went **烂尾 (abandoned mid-construction) in 2071**. Now inside a 贵客瘟
  lockdown zone. The whole tower is raw concrete, rebar, scaffold and tarps —
  **except one fully finished, pristine luxury show-flat floor**: a clean rich island
  rotting inside a concrete skeleton.
- **The commission (satire core)**: A Mars client (a "Earth's Unbuilt Dream" themed
  exhibition / private collector) pays well to retrieve the **sales scale model (楼盘沙盘)**
  — a miniature of the Earth future that was never built. They want the *unfinished*
  dream as a collectible: 烂尾 itself is the luxury.
- **Emotional target**: curiosity → unease. "This place was sold to people who left,
  and now their descendants want the leftovers."
- **Lore discoveries**: A 临时隔离公告 dated *before* the official infection
  announcement (optional evidence → ties to the 贵客瘟 truth thread, like the school ledger).

### Settlement Text (satire payoff — draft)

```text
楼盘沙盘完整度：91%        项目状态：2071 年停工
车厢防尘：通过             结构粉尘清洁费：扣款
轨道检疫费：扣款           人员坠落/感染补贴：无
客户用途：火星「地球未竟之梦」主题展 · 私人收藏
客户评价：未完成的样子最真实，比建成更值得收藏。
```

## Layout

> **Modular floor plan + room-size kit**: see `abandoned-tower-floorplan.md` for the
> grid (G=4m), the 4 standard room sizes (S/M/L/XL), the slot-based skeleton, and the
> random room-assignment rules. The narrative beats below map onto those slots.

Two floors connected by **two stairwells** (A = main, fast/exposed; B = side,
slower/safer) — this is the map's spatial gimmick: **vertical, floor-by-floor risk
with a chase choice**, per the apartment-block archetype in `docs/mvp-core-loop.md`.

The level uses the project's three spatial rules:
1. **Main spine + side loops** — M-branch is the spine; S-branch are side rooms;
   L-branch is the deep objective line.
2. **Risk gradient** — Lobby (low, teaches) → Warehouse/Workshop (med, loot) →
   DeepTargetArea/DangerousShaft (high, objective + nest).
3. **Objective ↔ exit separation** — grabbing the sales model on F2 starts the
   pressure phase; the carrier must descend all the way back to the van.

### Overview Map (logical flow — verify exact adjacency against scene transforms)

```
FLOOR 2 (Show-flat / Sales floor — HIGH RISK)
  [F2_L5_DeepTargetArea] (R!B)  <-- 沙盘 objective + monster nest
        |  (LeftLowerConnector)
  [F2_L4_SampleOffice_HalfFinished] (!)   [F2_S5_DangerousShaftRoom] (?R)
        |                                        |
  [F2_M5_ScaffoldBridge] (C>) --- TopCorridor --- [F2_S4_MaintenanceRoom] (?)
        |
  [F2_M4_SalesOffice_RichLoot] (R!)
        |                         \
   [F2_A_MainStair] (=)            [F2_B_SideStair] (=)   --MainReturnCorridor-->
        |                                |
========================================================= (stairs)
        |                                |
FLOOR 1 (Ground / Arrival — LOW->MED RISK)
   [F1_A_MainStair_ToFloor2]      [F1_B_SideStair_ToFloor2]
        |                                |
  [F1_M2_EastAssistantWorkshop](R) [F1_M3_MainWorkerDorm](?!)  <-- evidence
        |  EastMainCorridor             | NorthCorridor
  [F1_L1_CentralConstructionHall] (hub) --WestConnector-- [F1_L2_WestMaterialWarehouse](R)
        |  StairLobbyConnector
  [F1_M1_LobbySecurityPassage] (!)  --- [F1_S2_TemporaryOffice](?) -- [F1_S3_PowerRoom](P!)
        |                                                                  ^ POWER GATE
  [F1_S1_StartVanArea] (S/E)  <-- dispatch van: spawn, return, partial-settlement
```

Legend: S=start E=exit C=combat/chase P=puzzle/gate R=loot !=story beat ?=optional B=boss/nest >=one-way =two-way

### Critical Path

1. **[S] F1_S1_StartVanArea** — van drops team outside the 工地围挡. Banners:
   "拥有真正的地球海岸" / "地球最后的稀缺住宅". Route signage points to lobby.
2. **F1_M1_LobbySecurityPassage** — dead reception, sales hall. Players learn the
   show-flat floor is **powered down / security shutter sealed** → must restore power.
   (Diegetic readout on the security desk: "二层售楼区：断电 / 卷帘锁定".)
3. **F1_S3_PowerRoom (POWER GATE, step 1)** — reach via F1_S2_TemporaryOffice (which
   holds the clue: a note/keytag pointing to the power room). Restore power: a short
   hold-interaction at the breaker → audible building-wide power-on, shutter to F2 lifts.
4. **F1_A_MainStair / F1_B_SideStair → Floor 2** — vertical risk choice.
5. **F2_M4_SalesOffice_RichLoot → F2_M5_ScaffoldBridge → F2_L4_SampleOffice →
   F2_L5_DeepTargetArea** — the signature traversal: cross the exposed scaffold
   bridge (fall risk) into the pristine show-flat, then the deep target room.
6. **Grab the 沙盘 (objective, step 2 / pressure trigger)** — heavy two-hand carry.
   Pickup wakes/aggros the **infected site inspector** (monster nest in F2_L5).
7. **Descend under pressure** — F2_MainReturnCorridor → stairs down → Floor 1.
8. **[E] Return to F1_S1 van** — full return with 沙盘 = full settlement; early
   return without it (drop/leave) = partial settlement; all downed = failure.

### Optional Paths

| Path | Access Requirement | Reward | Discovery Hint |
|------|-------------------|--------|---------------|
| F1_M3_MainWorkerDorm | none | **Evidence**: 临时隔离公告 (photo → bonus 外快) | NorthCorridor signage |
| F1_L2_WestMaterialWarehouse | none | Consumables (medkit/decoy/spray/batteries) | Open hub off WestConnector |
| F2_S5_DangerousShaftRoom | accept fall risk | Extra rich loot + possible quick descent | Visible from TopCorridor |
| F2_S4_MaintenanceRoom | none | Spare consumables, monster patrol-gap shortcut | Off TopCorridor |

### Points of Interest

| Location | Type | Description | Purpose |
|----------|------|-------------|---------|
| F1_M1_LobbySecurityPassage | Story/gate signpost | Sales hall, "断电" readout | Teaches the power gate + tone |
| F1_S3_PowerRoom | Gate | Restore power, hold-interaction | 2-step objective gate; paces the run |
| F2_M4_SalesOffice_RichLoot | Wrong detail | Pristine materials (clean surfaces, intact furniture) vs concrete shell — wrongness from condition, not color | Map's memorable detail + loot |
| F2_M5_ScaffoldBridge | Traversal risk | Exposed scaffold crossing | Signature vertical tension |
| F2_L5_DeepTargetArea | Objective + nest | 沙盘 on a pedestal; monster nest | Climax / pressure trigger |

## Encounters

### Threat Encounters

| ID | Position | Threat | Difficulty | Arena Notes |
|----|----------|--------|-----------|-------------|
| E-01 | F2 (patrol M4↔L4↔L5) | 1× Infected Site Inspector (reskinned `SchoolMonsterAI`) | 5/10 | Dormant/patrolling until objective grabbed; then hunts |
| E-02 | F2_M5_ScaffoldBridge | Environmental: fall/gap (no rail) | — | Carrier with 沙盘 is slow → tense crossing |

**Monster — Infected Site Inspector (感染监理/安全员)**: hard hat + hi-vis vest +
acceptance clipboard, long limbs, glowing warning-light eyes. "Still doing inspection
rounds on a tower that will never be accepted." Same design language as the
HomeworkDebtCollector (bureaucratic threat made physical). Reuses `SchoolMonsterAI`
chase logic + `HidingSpot` interactions; reskin + nest placement only for MVP.

### Non-Combat Encounters

| ID | Position | Type | Description | Solution |
|----|----------|------|-------------|----------|
| P-01 | F1_S3_PowerRoom | Gate | Restore power to unlock F2 | Hold-interact breaker |
| EV-01 | F1_M3_MainWorkerDorm | Evidence | Pre-dated 隔离公告 | Photo interaction (bonus) |

## Pacing Chart

```
Intensity
10 |                                   *  (carry-out chase)
 8 |                              *  * * *
 6 |                    *   *   *        *
 4 |      *   *  *  * *   * *              *
 2 | * *    *           *                   *
 0 |S--------------------------------------------E
   [Van] [Lobby] [Power gate+loot] [Ascend] [Objective] [Descent/Exit]
```

- **Valley**: lobby + ground exploration (learn layout, gather, find power). No monster yet.
- **First rise**: ascending — monster patrol audible/visible, scaffold crossing.
- **Climax**: grab 沙盘 → monster active → heavy carry descent. Peak tension at scaffold/stairs.
- **Rest/decision**: at the van — full vs partial vs push-back-in.

## Visual Direction

- **Lighting**: pre-power = flashlight-dependent dark (one or two desk lamps still
  plugged in — warm tungsten residual, signaling recent presence); post-power = cold
  industrial (4500K–5000K) worklights flicker on across most floors; the show-flat
  restores its cold overheads cleanly (the only fully functioning lighting zone), with
  a single warm tungsten desk lamp on the sales desk marking human presence.
- **Color palette**: Municipal Debt Noir — dead-rubber black, concrete gray, warm
  tungsten amber (inhabitation signal only); the show-flat reads as wrong through
  its material condition (clean linoleum, unscratched surfaces, intact furniture)
  against the raw-concrete base — same warm desk lamp light as the HQ office, which
  is precisely what makes it disturbing. No fake-luxury gold; the wrongness is in
  the pristine materials, not a distinct hue.
- **Landmarks**: the two stair towers (A cold/exposed, B dim/enclosed); the show-flat
  visible across the unfinished shaft as a navigation beacon — the only zone where
  cold overheads function cleanly, creating a bright contrast against the failing
  floors around it.
- **Sight lines**: from F2_L3 unfinished shaft, players can see down into F1 hub
  (vertical readability) and across to the lit objective room.

## BC Identity Injection

Per art-bible Section 6: the commission's paperwork must appear as a distinct administrative
layer imposed on the tower's own institutional signage.

| Location | BC Marker | Description |
|----------|-----------|-------------|
| F1_S1_StartVanArea (van) | Route arrow + job number | "COMMISSION BC-12 / 外包施工场地 → 目标：沙盘" stenciled on the fence |
| F1_M1_LobbySecurityPassage | BC-12 form pinned to reception | Commission intake form with job reference and "委托人：火星私人收藏者" |
| F2 security shutter (post-power) | "欠款通道 / DEBT ACCESS ONLY" | Signage applied to the shutter frame — the commission office's access authorization |
| F1_S3_PowerRoom entrance | BC-12 equipment tag | Tagged breaker panel: "危险作业 / 外包事故组 BC-12 / 断路器" |
| F2_L5_DeepTargetArea | Overdue payment notice | Stamp-red "INVOICE OVERDUE" / "欠款未结" notice posted near the objective pedestal |

All BC text uses monospaced typeface, uppercase, stamp-red ink for status marks.
Institutional host text (sales banners, warning signs) uses the tower's own voice — not BC's.

## Collectibles and Secrets

| Item | Location | Visibility | Hint | Required For |
|------|----------|-----------|------|-------------|
| 楼盘沙盘 (objective) | F2_L5_DeepTargetArea | High (lit pedestal) | Lit show-flat beacon | Full settlement |
| 临时隔离公告 (evidence) | F1_M3_MainWorkerDorm | Medium | Pinned to dorm board | Bonus payout |
| Consumables | F1_L2 / F2_S4 | Medium | Open shelves | Survival |

## Tuning Knobs (formulas / values to balance — see /balance-check)

| Knob | Default | Notes |
|------|---------|-------|
| Power-gate hold time | 3.0 s | Long enough to feel like a beat, not a chore |
| 沙盘 carry move-speed multiplier | 0.55× | Heavy; carrier vulnerable, others must protect |
| Carrier hotbar lock while carrying | true | Can't use items while two-hand carrying |
| 沙盘 can be dropped/handed off | true | Co-op relay; drop near van = partial; re-grab allowed |
| Monster aggro trigger | on objective pickup | Dormant patrol before; hunts after |
| Monster speed vs carrier | slightly > carrier carry speed | Forces escorts/distraction, not impossible solo |
| Evidence photo bonus | +(small) 外快 | Mirror school ledger value |
| Partial settlement (evidence only, no 沙盘) | partial % | Reuse early-return partial path |

## Edge Cases

- **Carrier downed mid-carry**: 沙盘 drops in place; any teammate can pick it up.
- **沙盘 dropped off the scaffold/shaft**: define a recovery rule (respawn at last
  safe floor vs. lost → forces partial). Recommend: lands on floor below, recoverable.
- **Power restored, then host migration/late joiner**: gate state must be server-authoritative.
- **Early return with 沙盘 not yet grabbed**: partial settlement (existing path).
- **Solo player**: heavy carry + chase must be *survivable* solo (use decoy/spray);
  tune monster speed so a careful solo can extract.

## Dependencies

**Reuses (no new system needed):**
- Mission state/return/partial/fail: pattern from `LostItemMissionManager` +
  `SchoolExitPoint` (van rear cabin = return/partial anchor).
- Monster: `SchoolMonsterAI` (reskin) + `HidingSpot`.
- Mission clock / time-of-day: `MvpMissionClock`, `MissionTimeOfDayDirector`.
- Evidence photo: pattern from `SchoolBonusEvidenceItem`.
- Van transit + settlement + office computer commission: existing flow.

**NEW for this map (flag for scoping — see systems-index test-gap note):**
1. **Heavy two-hand carry objective** — extends the notebook-carrier rule: slowed
   carrier, hotbar lock, droppable/relay, server-authoritative carrier identity.
2. **Power gate** — server-authoritative building state that unlocks F2 access
   (shutter/door). Single hold-interaction.

> ⚠️ Both new mechanics are **server-authoritative state that must sync to all
> clients** (carrier identity, drop/handoff, gate open/closed). This is exactly the
> high-risk, untested area called out in `design/systems-index.md`. Add EditMode
> tests for: carrier-identity validation, gate state sync, drop/recover.

## Acceptance Criteria

1. Team boards van, arrives at tower; lobby clearly signals F2 is powered-down.
2. Restoring power in F1_S3 (server-authoritative) audibly/visibly unlocks F2 for all clients.
3. 沙盘 in F2_L5 can be grabbed only by an alive player at range; pickup syncs to all
   and aggros the monster.
4. Carrying 沙盘 slows the carrier and locks their hotbar; it can be dropped/handed off;
   carrier identity is server-validated (a client cannot remotely "finish").
5. Full return with 沙盘 to the van = full settlement; early return without it = partial;
   all-downed = failure — all three reuse existing settlement paths.
6. Optional dorm evidence grants a bonus, mirroring the school ledger.
7. Target play time 15–20 min for a first-time 2–4p group (measure in playtest).
8. Runs at target framerate; two stairwells + scaffold give readable navigation and chase choices.

## Technical Notes

- **Build on**: `Assets/Scene/AbandonedBuilding_Blockout.unity` (rooms already named).
  Promote to a playable scene (e.g. `Assets/_Project/Scenes/Tower_EarthCoast_01.unity`)
  and wire into Build Settings after HQ, like the school/snow-lotus scenes.
- **NavMesh**: bake across both floors + stair links; verify agents traverse stairs A/B
  and do NOT path across the scaffold gap unintentionally.
- **Required systems**: NGO host-authoritative, mission manager, monster AI, van/exit,
  office computer commission, HUD objective + carry indicator.
- **Performance**: two-floor sightline down the shaft — watch overdraw; the lit show-flat
  is the only warm-lit zone, keep dynamic lights bounded.
- **Next steps**: `/design-review` this doc → (optional) `/asset-spec` for 沙盘 + inspector
  monster → `/create-stories` for the 2 new mechanics → `/qa-plan` for the sync tests.
```
