# 下一步建议 — 烂尾楼玩法推进

**日期**: 2026-06-10
**背景**: 地图内容循环已完整（van→F1摸黑→配电闸3s→卷帘升→折返楼梯→F2→脚手桥→样板岛→生态柱重搬→回程→van）。EditMode 134/134 绿。PM 正在白盒试玩。

---

## 一、无怪物白盒试玩能验证什么

**能验证：**
- 空间可读性：F1暗→找电闸→上楼→发现样板岛的认知动线（三秒判读标准）
- 配电闸 3s hold 节奏感——是"仪式感停顿"还是"等待感烦躁"
- 重搬生态柱的行动代价（0.55× 速度 + 热键锁）：单人扛柱回程距离感
- 楼梯 dog-leg 可用性（方向感、进出门视线）
- 脚手桥坠落风险感知（携柱慢走过桥的压迫感纯靠视觉，不依赖怪物）
- E-DROP 跳降是否流畅、高度感是否合理——**这是纯调参，与怪物完全无关，可以现在就调定**

**不能验证：**
- 怪物未动时，"拿到目标→压力激活"这个心理转折点是否有效
- 多人分工：一人扛柱 + 其余人阻拦怪物的协作张力
- 配电闸多人合作 hold（1人 hold + 他人掩护）的分工感
- 任务管理器网络接线：完成/部分返还/失败的结算流是否正确同步

---

## 二、任务管理器先行 vs 怪物先行：哪个更早暴露设计风险

### 任务管理器先行

任务管理器是**最高优先级未解决的技术+设计双风险**：

- **技术风险**：LostItemMissionManager 是雪莲学校专用——字段（LostItemCollected、CarrierClientId、SchoolEntranceOpened）全部是学校语义，直接复用会引入错误的状态逻辑。塔楼需要一个新的 `TowerMissionManager`，覆盖：objective picked up（生态柱 NetworkObject 作为 carriable）→ 怪物 aggro 触发 → 部分返还（扛柱到van但未完整）→ 完整结算 → 失败 → 完整度扣减（drop completeness penalty）。
- **设计风险**：现有结算公式（20% partial、300G full）是雪莲/学校数值。生态柱委托有额外合同扣款项（密封完整度、坠落扣减、轨道检疫费）——这些是否接入结算系统，或只是叙事文本，需要明确。
- **可测性**：任务管理器写完后，无需怪物即可完整测试从"拿柱→van→结算"的全链路。**这是当前无法做到的事情**——PM 现在玩白盒看到的是一个没有结算终点的循环。

### 怪物先行

感染监理 AI 的阻力相对较低：

- `SchoolMonsterAI` 已实现完整的 Patrol/Chase/Stunned/Distracted 状态机和 NavMeshAgent，NavMesh 已 bake（56.5×7.6×62.5 两层）。怪物出生点（nest=TARGET，备用 SALES/bridge-east）已在 V8 平面图中标注。
- 所需工作：在 TARGET 房间放置一个 `SchoolMonsterAI` 预制件，设置巡逻路点，接线"生态柱 pickup→aggro"触发（需要任务管理器中的对应 NetworkVariable 才能可靠触发）。

**关键依赖**：怪物 aggro 触发的"生态柱被拿起"事件，是任务管理器的 NetworkVariable，不是怪物 AI 本身能独立监听的事件。**先放怪物但任务管理器未完成，aggro 触发逻辑就没有可靠的服务端信号。** 可以用临时 hack（本地 proximity trigger），但那验证不了多人同步。

**结论：任务管理器先行。** 它是完整可测循环的卡点，怪物的 aggro 也依赖它的信号。

---

## 三、多人测试必须何时进入循环

**现在单人验证不了的内容：**

1. 配电闸 3s hold 的多人掩护分工——一人 hold 被打断的体验只在多人下出现
2. 生态柱的接力传递（drop + teammate picks up）——单人只能是"我扛到van"，接力节奏是多人专属
3. 怪物追单个携柱者时，其他人的分散/阻拦决策——这是核心合作张力，无法单人模拟
4. 网络同步的视觉一致性：门开/关、shutter 状态、生态柱位置在多客户端的呈现

**多人测试时机建议**：

- **任务管理器接线完成后立即进入 2 人测试**，不必等怪物。目的：验证任务状态同步（拿柱/partial return/settlement）在两客户端一致，同时顺便验证配电闸 hold 的多人打断行为。
- 不建议等怪物就绪再做第一次多人测试，因为 session state 显示 sync 是当前第一高技术风险。

---

## 四、最小可玩验证（能完整跑一单委托并看到钱进账）——缺口盘点

| 缺口 | 量级 | 说明 |
|------|------|------|
| **塔楼任务管理器** | M（2-3 sessions） | 新建 `TowerMissionManager` NetworkBehaviour：生态柱 pickup NetworkVar、完整度变量（drop 扣减）、partial/complete/fail 三路结算接线到 `MissionRewardCalculator`、van exit point 适配。这是当前卡死"看到钱进账"的唯一硬性前置。 |
| **感染监理怪物预制件+spawn** | S（1 session） | 在 TARGET 放 `SchoolMonsterAI` 预制件，设巡逻路点，接线 aggro 触发（依赖任务管理器 NetworkVar）。`SchoolMonsterAI` 本身已有完整逻辑，工作量主要是场景接线，不是写新 AI。 |
| **E-DROP 调参** | S（半 session） | 跳降高度/伤害门槛/生态柱坠落完整度扣减比例——纯调参，地图几何已就绪，可随时测。**建议今天的白盒试玩期间一并测**。 |
| **办公室电脑——塔楼委托条目** | S（1 session） | `OfficeTaskDefinition` 中增加"地球海岸壹号·生态柱采回"任务条目，接入 job pool；这是 HQ→mission 全链路的入口，没有它就必须靠 hardcode 直接进场景。 |

> **注**：怪物模型/外观重设是非必须项，`SchoolMonsterAI` 用占位外形就能完成 MVP 玩法验证。

---

## 五、最终建议：接下来 2-3 个工作块的顺序

### 工作块 A（今天）— E-DROP 调参 + 白盒试玩记录

**一句话理由**：PM 已在场地里，白盒地图已就绪，调参不依赖任何未完成系统，现在测是成本最低的一次窗口；同时记录空间感/楼梯/桥体验的具体反馈，给工作块 B 的任务管理器接线提供明确目标数值（completeness penalty per drop）。

具体行动：
- 测试 E-DROP 跳降（伤害门槛、高度感）
- 记录生态柱携带速度 0.55× 在实际动线中的感受（是否可以调整）
- 记录桥上坠落风险的视觉感知是否足够
- 确认 completeness penalty per drop 的直觉值（0%? 5%? 10%?）

### 工作块 B（接下来 1-2 sessions）— 塔楼任务管理器

**一句话理由**：这是唯一卡死"完整跑一单并看到钱进账"的硬性前置，同时是当前最高技术风险（sync），越早测越早暴露问题，等怪物就绪再测会让 bug 归因变困难。

具体行动：
- 新建 `TowerMissionManager`（不改 `LostItemMissionManager`）
- NetworkVar：`EcoColumnPickedUp`、`EcoColumnCompleteness`、`EcoColumnCarrierClientId`
- 接入 `MissionRewardCalculator` 和 `SchoolExitPoint`（复用 van 返回逻辑）
- `OfficeTaskDefinition` 增加委托条目
- **任务管理器完成后立即做 2 人 PlayMode 测试**（这是不可推迟的 sync 验证）

### 工作块 C（任务管理器完成后）— 感染监理放置 + aggro 接线

**一句话理由**：任务管理器就绪后，怪物 aggro 触发有了可靠的服务端信号，接线工作量只有 1 session，做完即可体验完整的"拿柱→怪物追→跑回van→结算"全循环；进入这一步才算真正的 playtest loop。

具体行动：
- TARGET 放 `SchoolMonsterAI` 预制件，设 F2 巡逻路点
- 接线 `EcoColumnPickedUp` NetworkVar → 触发 aggro（`AggroTarget(player)`）
- 验证 NavMesh 在两层楼中的怪物寻路（已 bake，主要确认楼梯 offMeshLink）
- 怪物速度 vs 携柱速度的调参（初始值：怪物略快于 0.55× 携柱速度）

---

## 附：设计风险提示（PM 定夺）

1. **生态柱完整度扣减**：坠落每次扣多少？是否有最低保底（比如扣完只剩20%而不是0）？这个数值直接影响"失误恢复感"，建议白盒测试期间确认直觉值。

2. **部分返还条件**：生态柱未携带回van，但有人成功出场 = partial？还是一定要带着柱子才算 partial？`LostItemMissionManager` 里的逻辑是"携带目标物到 exit point"，塔楼需要明确这个定义。

3. **单人可解性**：文档标注"Solo 应该可以通过使用诱饵/喷雾"，但目前 Equipment & Consumables 系统是 Stub（只有手电筒）。如果这次 MVP 目标不包含 solo，需要现在明确，避免调参时拿单人体验作为基准。
