# Quick Design Spec: 工单在手 (Work Order In Hand)

**Type**: New Small System
**Status**: **APPROVED**（David 2026-07-04 拍板"批准，照写"；机制源头 = PM 本人提议"接任务都是接打印纸 + 纸拿在手上随时查看"）
**Scope**: 接单动作物理化（终端接受→打印机吐纸→撕纸拿走），工单成为可携带、可传递、可丢失的手持物品，是任务信息的唯一现场载体。不做：任务目标本身的改动、多委托并行、第三人称读纸姿势同步。
**Date**: 2026-07-04
**Estimated Implementation**: ~2.5–3 天
**风格归属**: 概念板 E2「一台机器·一条时间线」（磷光绿黄 #C8B830 终端 / 针打连页纸 / 章红 #B5372A 只做章），文字英文默认（EN default / ZH 本地化，PM 2026-07-04）
**设计目的**: 把 vs Lethal Company 的差异化从配色层搬到玩法层——LC 的信息锁死在飞船终端，我们的信息被打印出来、带进现场、产生分工（"我拿灯你拿单"）、并成为"怪客户"人格的物理载体。
**相关文档**: `design/gdd/scavenging-core-loop.md`（主 GDD）、`design/quick-specs/inspect-system-2026-06-26.md`（检视交互全继承）、`design/quick-specs/scavenging-item-system-2026-06-16.md`（物品管线）、`design/ux/mockups/style-concepts/concept_e2_timeline.png`（视觉时间线）

---

## Overview

玩家在办公电脑（BC-DOS 终端）接受委托后，任务不再只是屏幕上的状态——办公室的针式打印机当场打出一张工单（连页纸、链轮孔、客户手写批注），玩家走过去撕下来才算"把活接到手上"。工单是网络同步的手持物品：占手持槽（拿它就拿不了手电——**看信息=放下光源**）、举起可读（复用已批准的检视系统）、可以递给队友、会掉、会丢。丢了回派遣车花小钱重打一份。

E2 分配律在本系统的体现：**会变的住屏幕（终端接单页），定格的上纸（打印那一刻），拿走的进手里（工单物品）。纸上永远没有按钮。**

## Core Rules

**接单 → 打印 → 撕纸**
1. 玩家在办公电脑 Commissions 页按 Accept → 终端回显 `PRINTING WORK ORDER…` → 打印机（办公室实体道具，CRT 旁，动线 CRT→打印机→门 ≤5m）开始吐纸：哒哒声 + 卡纸半拍 + 继续（机器必须是坏的），时长 `printDuration`。
2. 打印完成后纸挂在出纸口，任何玩家 [E] 撕纸 → 纸进背包并自动切到手上。**撕纸前任务不进入可出发状态**（派遣车提示 "WORK ORDER NOT TORN"）。
3. 一次派遣一张工单（当前单任务派遣制）。重复 Accept 不重复打印。

**手持与阅读**
4. 工单是标准可捡物（走 ScavengeItem/NetworkObject 管线），重量 0，**CargoZone 不收**（不可入舱、不可结算，结算器忽略）。
5. 手持槽互斥天然成立：手里是工单就不是手电/A镜。举起阅读 = 检视系统既有交互（inspect-system D-I1/D-I2 全继承：减速不定身；检视中弹 Zone-Detail 面板保证字号可读，纸面网格贴图低保真即可）。
6. **纸上永远没有按钮**。工单内容 = 打印体任务清单 + 客户手写批注；目标完成时对应行出现**铅笔勾**（本地视觉，"你自己在纸上做记号"，不违背纸的静态隐喻）。

**传递 / 丢失 / 兜底**
7. 传递 MVP = 丢下+队友捡起（既有管线零新增）；[G] 面对面直接递交 = 后续 polish，不阻塞。
8. 工单掉落在图里就留在原地（会被玩家丢失）。玩家死亡掉落全部持有物时工单一并掉出。
9. **重打**：派遣车内小型车载打印机（关卡评审"移动哑终端"方案，车厢挂钩就是出纸位），[E] REPRINT → 扣 `reprintCost` 金（"纸和色带也是钱"）→ 吐一份副本。副本无数量上限，旧副本继续有效（纯信息物）。资金不足时打印机亮红灯拒绝（穷的羞辱是内容）。

**怪客户载体**
10. `OfficeTaskDefinition` 新增：`clientScrawl`（手写批注文本，手写字体渲染）、`printQuirkSeed`（污渍/歪斜/涂改的确定性随机种子）。批注是"怪委托"支柱的家（writer pass 供文案）。

**服务器权威**
11. 打印触发、撕纸、重打、扣费全部 host 权威（ServerRpc），工单物品位置/持有走既有 NetworkObject 同步。检视阅读是 owner 本地行为（继承 inspect spec §3，无新网络状态）。

## Tuning Knobs

| Knob | Default | Range | Category | Rationale |
|---|---|---|---|---|
| `printDuration` | 3.5s | 2–6s | feel | 够仪式、不烦人；含一次卡纸停顿 |
| `printJamChance` | 100%（固定演出） | 0–100% | feel | MVP 做成固定节拍，不做真随机 |
| `reprintCost` | 5 | 0–50 | gate | 疼一下但不惩罚探索 |
| `tearInteractRange` | 2.0m | 1–3m | feel | 与现有交互 SphereCast 一致 |
| `objectiveTickStyle` | pencil | pencil/stamp | feel | 铅笔勾最像"外勤自己记" |

配置进 `Resources/Config/WorkOrderConfig.asset`（SO，随 ScavengingConfig/DangerConfig 惯例），不硬编码。

## Affected Systems

| System | Impact | Action Required |
|---|---|---|
| `OfficeComputer` / `OfficeTaskDefinition` | Accept 流程接打印；新增 2 字段 | 改代码 + 3 个任务 asset 填批注 |
| `ScavengeMissionManager` | 出发门槛加"工单已撕" | 改代码 |
| `InspectController` | 工单检视内容绑定（清单+勾） | 小改 |
| `CargoZone` / 结算器 | 忽略工单物品 | 一行过滤 |
| HQ/van 场景 | 打印机道具×2（HQ+车载）+ 出纸锚点 | 建模走 Blender 全流程（PM 长期指令 blender-full-pipeline-required）|
| `scavenging-core-loop.md` §loop | 接单一步的描述变化 | **GDD update required（待 PM 单独批准）** |
| 音频 | 针打声/撕纸声/拒绝哔 | 素材渠道已有（"声音先放下"指令解除后接） |

## Acceptance Criteria

- [ ] Accept 后 `printDuration` 内打印机出纸，任何玩家可 [E] 撕纸；未撕纸时派遣车拒绝出发并提示
- [ ] 工单在手时无法持手电；检视键弹出可读清单；目标完成后对应行出现铅笔勾（host/单机；client 勾同步随 ItemId 欠账 follow-up）
- [ ] 工单可丢下、被队友捡起、死亡时掉落；联机双端位置/持有一致
- [ ] 派遣车 [E] REPRINT 扣 `reprintCost` 出副本；资金不足拒绝且有反馈；副本与原件内容一致
- [ ] CargoZone 对工单不入舱不计重；结算不出现工单行
- [ ] 感受验证（playtest）：撕纸→拿单进黑暗的一拍"像接活"，读单时放下手电有紧张感
- [ ] 回归：现有捡物/入舱/结算/检视全部行为不变（EditMode 全量绿）

## Systems Index

无 systems-index.md——本系统低于追踪门槛，quick spec 足够；建议将来建索引时挂在 Scavenging Loop 层下。

## GDD Update Required?

Yes——`design/gdd/scavenging-core-loop.md` 的循环描述（"office computer (accept job)"一步扩为"accept → printer → tear order"），及 Pillar 3（"the contract speaks"）增补"工单是 contract-speaks 的现场载体"。**待 PM 单独批准后再改 GDD。**

---

*评审背景：本 spec 源自 2026-07-04 UI 风格方向讨论（概念板 A–E → E2 定稿）+ 四学科评审（ux/game-design/level/art，判决 3 有条件支持 + 1 有条件反对全部消化进 E2 与本 spec）。详见 `production/session-state/active.md` 2026-07-04 段。*
