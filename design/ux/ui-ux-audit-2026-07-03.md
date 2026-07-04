# UI/UX 全面审计 — 2026-07-03（静态半场）

> **性质**: David 拍板「先做全面 UI/UX 审计」后的第一交付。
> **方法**: 8 份 `design/ux/*.md` spec 逐一对照代码实现（grep 关键特征 + 定点读文件）。
> **范围限制**: 本文是**静态半场**——不含运行时截图走查；各 spec 的视觉/可读性验收标准
> （720p 可读、纸卡不裁切、推镜无黑屏、时序）留给动态半场（需 Play 模式 + 截图）。
> **实现底座现状**: 全部游戏内 UI 为 IMGUI（`MvpHud` 1932 行 / `SettlementCardOverlay` /
> `VanTransitOverlay` / `QuickNetworkUI`）；主菜单为 uGUI（`MainMenuUI` 2901 行）。

---

## 逐屏对照

| # | Spec（状态） | 实现载体 | 判定 |
|---|---|---|---|
| 1 | 主菜单（Approved 06-11） | `MainMenuUI` + `CrtMenuStage`（uGUI+3D 办公室 CRT） | **大头已实现**，验收标准待截图走查 |
| 2 | 大厅·派工名单（Drafted 待终审） | `MainMenuUI` WaitingTerminal + `QuickNetworkUI` IMGUI | **未按 spec 实现**（见 §2） |
| 3 | 办公电脑终端（Approved 06-11） | `MvpHud.DrawOfficePanel`（IMGUI） | **部分实现**：无页签、无账本（见 §3） |
| 4 | HUD（Approved 06-11） | `MvpHud`（IMGUI） | 核心已实现；**spec 已过时**（塔楼时代，见 §4） |
| 5 | 结算单（Approved 06-11） | `SettlementCardOverlay`（IMGUI） | 塔楼路径已实现；**搜刮路径只显示总额**（见 §5） |
| 6 | 上车+在途（Drafted） | `VanTransitOverlay`（969 行 IMGUI） | **大头已实现**；申请单已删=spec 过时（见 §6） |
| 7 | 任务遭遇 HUD（In Design） | `VoiceMicIndicator` | **spec 13/15 节空白**；实现反而超前（见 §7） |
| 8 | 物品检视（In Design·dev-ready） | `InspectController` + `InspectSession` | MVP 占位已实现；**数据字段全缺**（见 §8） |

### §2 大厅 — spec 与实现互相脱节
- spec 明令退役的 940×620 绿色 WaitingTerminal 面板（`MainMenuUI.cs:1722`）与
  `QuickNetworkUI` 的 IMGUI HostWaiting 覆盖层**仍是现役实现**；「派工名单」盖章公文卡未建。
- 已有的部分：花名册+静音钮（`MainMenuUI.cs:1985`）；**「更换工装」监控室选人场景已实现**
  （`CrewPickerScreen.cs`，mockup 14_crew_picker v3）。
- spec 标注的两个架构旗标未落实：色板独占 ServerRpc 校验；NGO disconnect reason 带「除名」标记。

### §3 办公电脑终端 — 页签制与账本缺失
- 现实现：CRT 风格 section 流（`DrawTerminalHeader`/`DrawTerminalSection`、商店 F1-F4、
  待结算块、旧货采购）——终端「气质」在，**结构不在**。
- spec 锁定的 Z3 页签行 `[1]委托文件 [2]采购目录 [3]公司账本` 全项目零命中；
  「公司账本」页（逐单流水+讽刺备注回看）完全没有。
- 账本前置 = **逐单结算历史存储 ADR**（spec Open Q#1，归 technical-director）至今未出。

### §4 HUD — 实现健康，spec 过时
- 已实现：VITALS 体检监视器（`DrawVitalsBlock`）、准星/琥珀交互提示、热栏、受击闪、
  目标行（搜刮文案 `MvpHud.cs:1127`）。
- **spec 整份按塔楼生态柱任务写成**（SyncedCompleteness 密封完整度 / 工单行 A2 / 重物锁定 /
  TowerMissionManager 5 态）——核心循环已转搜刮双层 loot，spec 需修订对齐（信息清单重排）。
- spec 实施备注「统一批次迁移 uGUI/TMP」未做（全部仍 IMGUI）。

### §5 结算单 — 塔楼路径完整，搜刮路径是占位
- 塔楼路径完整：条款式账目行（C-7/B-2/D-1）+ 客户使用备注 + 章（`SettlementCardOverlay`）。
- **搜刮路径只给总额**：`ScavengeMissionManager.cs:14` 明注
  "Per-item settlement reveal (quick-spec §4 P2) is deferred/PM-owned; this shows the run
  total on the existing settlement card"。逐件揭示、relic 倍率呈现 = 本 branch 的本名功能，未实现。
- spec 数据缺口未补：`MissionRewardResult` 行项拆解、客户备注字段、单号规则、账本 ADR。

### §6 上车+在途 — 已实现，减一块
- 票据条/派车单/待签发→已签发章/在途墨条均已实现（`VanTransitOverlay`）。
- **提前收工申请单已整体移除**（`VanTransitOverlay.cs:524`：搜刮发车即结算现有货物，
  无部分返程概念）→ spec 对应章节过时。
- spec 最大实施项「签发即异步加载、双门控开门」是否已做**未验证**（动态半场确认）。

### §7 任务遭遇 HUD — spec 欠账最大的一份
- spec 15 节只写了 §1 Purpose，13 节 [To be designed]（2026-06-15 停笔）。
- 实现反而超前：`VoiceMicIndicator`（常驻开麦指示 + 首次语音同意告知）正是 §1 的两个硬需求。
- **怪物阵容已从 1 只变 3 只**（回声菌/档案怨灵/市政圣像）——圣像「盯住就停」的
  HUD 语法（要不要任何提示？纯 diegetic？）完全没设计。spec 需按新阵容重写。

### §8 检视 — 交互壳已立，数据与呈现层全空
- 已实现（MVP 占位）：hold F / 鼠标旋转 / 视角锁 / 全打断键 / armed-latch /
  `IsInspecting` 联机同步 / 占位文字面板。EditMode 测试在。
- 未实现：`inspectDetail` 文本面板 + 撕边档案标签底衬（WCAG AA）、低头俯仰 -8~-12°、
  Toggle a11y、世界压暗层。
- **数据字段全缺**：`ScavengeItemDefinition` 无 `tier` / `isInspectable` /
  `targetPersonId` / `inspectDetail` —— 双层 loot 数据结构完全未实现。

---

## 横切缺口（比单屏更重要）

- **A. 双层 loot + 逐件结算揭示 + relic 倍率**：机制 APPROVED
  （`design/quick-specs/scavenging-two-tier-revision-2026-06-26.md`），代码零实现。
  §5 和 §8 的缺口同根于此。branch `scavenge-settlement-reveal` 的本名欠账。
- **B. spec 过时带**：hud.md / settlement.md / boarding-transit.md 均写于塔楼时代，
  搜刮转向后未修订；mission-encounter-hud.md 停在 1 只怪时代。
- **C. 队列尾三屏**（电脑→HUD→结算→上车→大厅→图谱→设置）：大厅未做；
  图谱无 spec（`OfficeMonsterBestiary` 23 行占位）；**设置已 ad-hoc 实现**
  （`SettingsOverlay`，07-01 ESC 菜单）**但无 spec**——各 spec 挂靠设置屏的无障碍开关
  （减弱动效 / 检视 Toggle / 减弱屏闪 / 按住改双击）全部无家可归。
- **D. 底层债**：IMGUI→uGUI/TMP 迁移未启动；`interaction-patterns.md` 未建
  （累计 9+ 种子模式）；`accessibility-requirements.md` 未建；`player-journey.md` 未建。

## 优先级建议（待 David 拍板）

| 级 | 项 | 理由 |
|---|---|---|
| P0 | 搜刮结算揭示链路（横切 A）：数据字段 → 逐件揭示序列 → relic 倍率 → 检视 detail 面板 | 批准已齐、纯实施；核心循环的情感终点；branch 本名 |
| P1 | 遭遇 HUD spec 补写（3 怪已上线）+ 塔楼时代 spec 修订带（横切 B） | spec 债不还，后面每屏实施都在错图纸上干 |
| P1 | 大厅派工名单卡（退役 WaitingTerminal / QuickNetworkUI IMGUI） | 队列既定；现状是 spec 明令退役物 |
| P2 | 终端页签化 + 公司账本（前置：逐单历史 ADR） | 讽刺文本回看的正式家 |
| P2 | 设置 spec 补写（收编全部 a11y 开关）+ 图谱 | 无障碍承诺全挂在这屏 |
| P3 | IMGUI→uGUI 迁移、模式库、a11y 基线文档 | 结构性，不阻塞玩法 |

## 动态半场（待做）

Play 模式截图走查：各屏验收标准逐条打钩（主菜单推镜无黑屏、720p 可读、纸卡不裁切、
派车单时序、到站渗光、结算章砸落对齐 StampThunk、检视姿态）。等编辑器空闲后进行。
