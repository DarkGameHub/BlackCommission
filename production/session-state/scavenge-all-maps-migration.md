# 迁移：所有任务图统一改 Scavenge（删生态柱）

**发起**：PM David，2026-06-26。分支 `scavenge-settlement-reveal`。
**目标**：两张任务图（`Tower_EarthCoast_01` + `Map2_Procedural`）统一走"捡垃圾"采集循环；生态柱(EcoColumn/TowerMission)整套删除。`Scavenge_Testbed` 已接好，作配方参考。

---
## ▶ 进度 2026-06-26（第一批：删除+集成+Tower builder 代码全改完，未编译验证）
**已完成（代码层，静态自检通过：0 悬空类引用 / 0 悬空成员引用）：**
- 删除 5 文件：TowerMissionManager / TowerMissionLogic / EcoColumnCarriable / TowerVanDepartLever / TowerMissionLogicTests（+.meta）。
- 改指向完成：EchoMold(脱 OnObjectiveSecured 钩子+去 aggroBoost) / PlayerHotbar(4 gate) / MissionVanExitPoint(去完整度+部分,return→Scavenge) / MvpHud(任务面板→舱位/件数) / VanTransitOverlay(整张拆 EarlyReturn 卡,发车→Scavenge.RequestDepart)。
- Tower builder 重写：删 BuildEcoColumn+plinth；BuildMissionManager → LootSpawner + ScavengeCargoZone + ScavengeMissionManager + ScavengeVanDepartTrigger（单例自解析 + SerializedObject 双保险）。
- LootSpawner 加 SpawnLootDeferred()：等 LootAnchor 出现再撒（解决与 TowerLayoutGenerator.Fill 的 OnNetworkSpawn 顺序坑）。
- 注释清理：ScavengeItem 去 `<see cref="EcoColumnCarriable"/>`（免 CS1574）。

**⚠ 未验证**：Unity 编辑器开着(PID 14588+锁) → 不能另起 headless 编译；桥 8091 在听但 pump 停(不应答) → 不能远程触发。**下次：PM 聚焦 Unity 让其重编译 → 看控制台 0 错误**，再继续。

## ▶ 进度 2026-06-26（第二批：Map2 采集接线，单机优先；未编译/未烤验证）
- 批1 已验证编译干净（Editor.log，0 error）。
- `MapSiteBuilder.ScatterLootAnchors`：每个房间格(Owner!=null)撒一个 LootAnchor（纯标记/确定性，挂 sunk Indoor 下）；Build 里 DressInterior 后调用。
- `Map2SceneBuilder.BuildScavengeRig`：把 LootSpawner+ScavengeCargoZone+ScavengeMissionManager+ScavengeVanDepartTrigger+MissionVanExitPoint+PlayerSpawnPoint **烤进场景固定 dropoff (10,*,-80)**（ENTRY 锚恒为 (2,2) → dropoff 与种子无关）；场景 NetworkObject 自动 spawn。
- `Map2SceneBuilder.EnsureInBuildSettings`：BuildAndSave 时自动把 `Map2_Procedural` 加进 Build Settings。
- OfficeComputer 注释更新（Map2 现已接线，单机主机）。MP 确定性记入 `production/backlog.md`（ADR-0003 seed-sync 跟进）。

## ▶ 进度 2026-06-26（第三批：Map2 多人 seed-sync，已写；未验证）
- `MapSiteRuntime` MonoBehaviour → **NetworkBehaviour**：server 掷一个种子 → `NetworkVariable<int> netSeed`(-1哨兵) → 各 peer 同种子本地重建同一张图。镜像 `GridMapNetworkBuilder`/`TowerLayoutGenerator` 范式。**保留离线路径**（无 NGO 时 Start 用本地种子建，单机走查不受影响——走查菜单本就直接调 `MapSiteBuilder.Build`，不经 MapSiteRuntime）。
- `Map2SceneBuilder`：MapSite 上加 `NetworkObject`（baked scene 对象 → NGO 自动 spawn）。
- 确定性已核：建图链全 `System.Random(seed)`，无非种子随机；loot 由 host-only `LootSpawner` 撒+复制，天然多人一致。
- → **Map2 现在多人也正确**（不再只是单机主机）。backlog 该项标记 IMPLEMENTED。

**⚠ 待验证（PM 聚焦 Unity 后）：**
1. 重编译 0 error（批1+批2 一起；批2 新引用：Map2SceneBuilder `using Unity.Netcode`、各 scavenge 全局类、LootAnchor 同命名空间——静态已核）。
2. 重跑 `Tools ▸ Black Commission ▸ Map ▸ Create Map2 Scene`（重烤 Map2 场景带 rig + 自注册 build settings）。
3. 重跑 Tower builder（`Build Production HQ`/`Tower V8 Whitebox` 对应菜单）→ 旧 missing-script 物件被新采集 rig 取代。
4. 联机(单机主机)走查：HQ 接 Tower 或 FreeSalvage_Map2 → 上车 → 到图捡垃圾(满地 ScavengeItem) → 货舱称重 → 发车杆结算 → 返程 → HQ。
---

## PM 决定（2026-06-26）
- **怪物**：先把 `EchoMold` 移出两张图、解除其对 `TowerMissionManager` 的依赖；**不删** EchoMold 脚本/资产（先留）。
- **结算模型**：发车即结算车上现有货物（`ScavengeCargoZone.SettleCargo`，按客户偏好/市价）；**砍掉**塔楼"部分返程/完整度/目标登车"。
- **Tower 几何**：保留手搭 V8，只加采集接线（loot anchor 由 `TowerLayoutGenerator.Fill` 填的 dressed Room 预制现成提供）。

## 删除（生态柱整套）
- [ ] `Scripts/Mission/TowerMissionManager.cs`
- [ ] `Scripts/Mission/Core/TowerMissionLogic.cs`
- [ ] `Scripts/Mission/EcoColumnCarriable.cs`
- [ ] `Scripts/Mission/TowerVanDepartLever.cs`
- [ ] `Tests/EditMode/Mission/TowerMissionLogicTests.cs`
- [ ] builder 里 `BuildEcoColumn` / `TARGET_EcoColumnPlinth` / `V8_EcoColumn_Glass` 材质

## 改指向（TowerMissionManager → ScavengeMissionManager / 移除塔楼概念）
- [ ] `Monsters/EchoMold.cs`(3) — 解 `OnObjectiveSecured` 钩子；怪保留 roam/hunt/近身
- [ ] `Player/PlayerHotbar.cs`(4) — `TowerMissionManager.Instance` → `ScavengeMissionManager.Instance`（任务期 gate）
- [ ] `Mission/PowerGateBreaker.cs`(1) — 仅注释
- [ ] `Mission/MissionVanExitPoint.cs`(6) — 留登车+储物柜；删完整度/部分；return → `ScavengeMissionManager.RequestDepart()`
- [ ] `UI/MvpHud.cs`(6) — 任务面板改读 `ScavengeMissionManager`（目标=已收金额/舱位/件数）
- [ ] `Van/VanTransitOverlay.cs`(5) — 删提前收工/部分预览；终态 guard + depart → Scavenge
- [ ] `UI/SettlementCardOverlay.cs`(1) — 核查（多半 guard/注释）
- [ ] `Editor/WhiteboxFurnitureBuilder.cs`(1) — 核查

## 加（两张图各自）
- [ ] Tower builder：`LootSpawner` + `ScavengeCargoZone`(货舱 BoxCollider) + `ScavengeMissionManager` + `ScavengeVanDepartTrigger`（替 lever）；LootSpawner 须等 `TowerLayoutGenerator.Fill` 跑完再找 anchor（OnNetworkSpawn 顺序坑）
- [ ] Map2：给生成器补 LootAnchor（GridMapGenerator 房间格裸几何，无 anchor）+ 同上采集接线
- [ ] `Map2_Procedural` 加进 Build Settings（NGO 加载前提）

## 验证（桥断时走 headless batch）
- [ ] `Unity.exe -runTests -batchmode -nographics` → 0 编译错误 + EditMode 通过（删了 TowerMissionLogicTests，基线随之变）
- [ ] `-executeMethod` 重跑 Tower builder + Map2 生成 → 存场景
- [ ] 联机走查：HQ 接单 → 两张图各自 捡→称重→发车结算→返程

## 参考配方
`Editor/ScavengeTestbedBuilder.cs`：LootSpawner(NetworkObject) + ScavengeCargoZone(BoxCollider+NetworkObject) + ScavengeMissionManager(cargoZone 引用)。`ScavengeCargoZone.SettleCargo()` 出 `SettlementResult`；偏好取 `MvpMissionRuntime.ActiveTask.FavoursCategoryId`。
