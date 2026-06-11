# Quick Design Spec: TowerMissionManager（烂尾楼任务管理器）

**Type**: New Small System
**Scope**: 烂尾楼委托的主机权威任务状态机 + 生态柱完整度跟踪 + 三路结算接线。不含怪物 aggro、不含 HQ 任务选择 UI 改动。
**Date**: 2026-06-10
**Estimated Implementation**: 1-2 sessions
**GDD Reference**: `design/levels/abandoned-tower-earth-coast-01.md`、`design/gdd/office-economy-progression.md`
**参考实现模式**: `LostItemMissionManager`（主机权威 NetworkVariable + 客户端意图 RPC）

## Overview

跟踪生态柱的拾取/掉落/交付，维护密封完整度，在委托车出发时按任务状态走
`MissionRewardCalculator` 三条结算路径（全额/部分/失败）入账 `CompanyState`。
复用现有网络模式，不修改雪莲学校任务代码。

## Core Rules

状态机（host 权威，`NetworkVariable<TowerMissionState>`）：

| 转换 | 触发 |
|---|---|
| InProgress → ObjectiveSecured | 生态柱首次被任何玩家抱起；广播 `OnObjectiveSecured`（未来怪物 aggro 钩子） |
| → Delivered | 车发车时生态柱在货舱 trigger 内 **且完整度 ≥ rejectThreshold** |
| → PartialReturn | 车发车时无生态柱在舱，或完整度 < rejectThreshold（合同：密封罩破损不予接收） |
| → Failed | 全员倒地 |

完整度（`NetworkVariable<float>` 初始 1.0）：
- 每次硬着陆（脱手后落地冲击速度 ≥ `Carriable.dropDamageThreshold` = 3.5 m/s）扣 dropPenalty
- 轻放/接力换手（冲击低于阈值）不扣——鼓励合作传递
- 跳降抱柱直落 4.2 m 必超阈值 → 必扣（跳降捷径的代价）

结算：全额 = `300G × 完整度`（如 91% → 273G）+ 既有声望/经验；部分 = 既有 60G 路径；
失败 = 20G。结算屏显示完整度行（对应关卡文档既有文案"生态柱密封完整度：91%"）。

## Tuning Knobs

| Knob | Default | Range | Category | Rationale |
|------|---------|-------|----------|-----------|
| dropPenalty | 0.03 | 0.01–0.10 | 经济 | PM 拍板 2026-06-10；结算样例 91% ≈ 3 次掉落 |
| rejectThreshold | 0.50 | 0–0.7 | 门槛 | PM 拍板 2026-06-10；低于一半客户拒收 |
| 硬着陆判定速度 | 3.5 m/s | — | 复用 | 复用 Carriable.dropDamageThreshold，单一来源 |

## Acceptance Criteria

- [ ] 纯逻辑类 `TowerMissionLogic` EditMode 测试：全部状态转换、完整度公式
      （3 次掉落 = 91% → 273G）、rejectThreshold 拒收降级、全员倒地失败
- [ ] 车发车三条路径各自触发正确结算并入账 CompanyState（host 权威）
- [ ] 晚加入客户端通过 NetworkVariable 收到当前状态与完整度
- [ ] 无回归：LostItemMissionManager 及其既有测试全绿

## Systems Index

属任务系统层；建议后续在 systems-index 的 Mission 行补条目（quick-spec 先行）。

## Deferred（PM 决定暂不加，2026-06-10）

- 取柱跳闸第二幕事件（OnObjectiveSecured → 全楼断电+卷帘复落，须摸黑回配电房）
- TEMP 线索房副目标（接 MissionRewardCalculator 既有 bonus-evidence 路径）
怪物接上后再评估是否启用。
