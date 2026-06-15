# Monster — The Echo Mold (回声菌)

> **Status**: In Design
> **Author**: Yan Dai (PM) + Claude (zeno/laplace/banach lens)
> **Last Updated**: 2026-06-14
> **Implements Pillar**: Threat Design (infection ecology — MRC-7 "Noble Guest Plague") · co-op extraction tension
> **Priority / Layer**: MVP / Feature
> **Concept (locked 2026-06-14)**: 感染的人形真菌宿主,会偷听并回放小队对讲机里的人声来欺骗、引散队伍;真·语音捕获 + 预置兜底词;能动(NavMesh);接触扣血,血空 Downed,全员倒 = Failure;反制 = 学破绽 + 语音纪律。变身机制 MVP 不做(留后续)。canon: world-background-2098「会模仿声音的真菌群落」。
> **Review mode**: Lean — `systems-designer`(调参)+ `qa-lead`(验收)passes deferred;flag Full-mode pass pre-production.
> **Related**: `docs/world-background-2098.md`(感染设定)· `design/art/art-bible.md` §5(怪物框架 — **待更新**为感染生态)· `design/game-pillars.md` · `design/gdd/mission-state-machine.md`(Downed→Failure)· `design/gdd/level-map-generation.md`(NavMesh、EDGE 竖井)· `Assets/_Project/Scripts/Network/ProximityVoiceChat.cs`(已有 VOIP)· `Assets/_Project/Scripts/Player/PlayerHealth.cs` · `docs/architecture/ADR-0001-host-authoritative-networking.md`

## Overview

回声菌(Echo Mold)是 MRC-7「贵客瘟疫」在地球海岸壹号烂尾楼里催生的感染生态之一:一个被真菌寄生的人形宿主,浑身菌伞与孢子,蹒跚游荡。它的危险不在力气,而在**它会偷听并回放你们对讲机里的人声**——用你队友自己的声音,从错误的方向喊出"我找到了,过来!",把队伍引散、引向危险,等你落单再近身啃咬掉血。它把"开口说话"从合作工具变成风险源:每一句话都可能被它学走、反过来骗你。主机权威(ADR-0001),在 NavMesh 上行动。canon 锚点:world-background-2098「fungal colonies that mimic sounds」。这是 4 人语音联机/直播里最毒的一类张力——"那真的是你吗?"

## Player Fantasy

情绪锚点不是"被追的恐惧",而是**"不敢相信自己耳朵"的偏执与黑色幽默**(承接 art-bible「焦虑的义务感,不是血腥」的底色,但更喜剧、更社交)。标志瞬间:你听见队友的声音喊"在这边!",你跟过去——空无一人,身后菌伞缓缓张开。

核心幻想是**语音纪律下的合作**:你们被迫少说话、约暗号、互相确认方位;而越交流,越喂它样本、越被它定位。死亡常常是"自找的"——信了假声、为找掉队的人而喊话。黑色幽默来自它**专挑你们说过的蠢话、脏话、甚至客户合同腔**循环回放。直接体验:说不说话的取舍、信不信声音的取舍;间接体验:孢子噗噗声与回放的金属失真感作为氛围。

**支柱对齐**:Threat Design(把"贵客瘟疫"感染生态具象化)+ 合作撤离张力;并放大本作独有的近距离语音系统价值。

## Detailed Design

### Core Rules

1. **主机权威**:感知、寻路、"录哪段 / 在哪放 / 放谁的声音"全部由主机决定并同步;客户端只播放主机指定的空间化音频片段,绝不本地决定(ADR-0001)。
2. **声音是它的主感官**(区别于普通噪声怪):近距离对讲机说话 = 强"注意度"脉冲**且**被采样;奔跑 = 中;走 = 低;蹲走 / 静止 ≈ 0。它朝最近时间窗内"注意度"最强的玩家寻路(NavMesh)。
3. **样本库**:为每名玩家滚动缓存最近 `voiceWindow` 秒、至多 `maxClips` 段语音(FIFO)。**库为空时用预置诱导词兜底**("这边""找到了""快来""有人吗")。
4. **欺骗广播**:每 `lureInterval` 秒,若 ≥1 名玩家"在听"(近期注意度>0)且库里有样本,它从一个**远离本体的诱饵点**(距离 ≥ `decoyMinDist`)用某名玩家的声音回放一段诱导词 / 原话,意图把其他玩家引过去、引散;它本人常在反方向。
5. **破绽 = 可学的反制(无道具,PM 选定)**:回放带可分辨 `tell`——轻微失真 / 金属感、来源方向与该玩家真实位置不符、短时重复。靠耳朵 + 站位识破;并靠**语音纪律**(少说、约定真假暗号)少喂样本、少被定位。
6. **物理威胁 = 掉血(PM 选定)**:它会移动逼近;接触 / 啃咬时每 `dmgTick` 秒扣 `dmgPerTick` 血(走 `PlayerHealth`,**非秒杀**)。血量归零 = Downed(可被队友救起);**全员 Downed = 任务 Failure**(对接 `mission-state-machine` 规则 7)。
7. **自身近乎无声 + 信号色**:本体移动安静;**第一警报是行为 / 听觉**(脚下头顶的孢子噗噗声、回放的失真感),近距离才以 amber-orange `#FF6A00` 孢子眼点作"确认"(沿用 art-bible 信号色规则,即便造型放开为真菌怪)。
8. **在既有光照下活动**:**不**关灯、不制造黑暗事件(art-bible 硬规则保留)。
9. **每张图一只**(MVP);Level-Gen 报告 `Ready` 后由主机生成,在 NavMesh 上巡游。
10. **不破坏可完成性**:寻路与广播不得堵死唯一通路或使目标物不可达(对接 Level 不变量 I1/I2/I6;Hunt 寻路含让位 / 超时回退,避免 softlock)。
11. **变身机制 MVP 不做**(PM 2026-06-14):被抓只掉血;"被感染→变成搅局 / 提示者"留作后续扩展(见 Open Questions)。

### States and Transitions

| 状态 | 含义 | 有效转移(触发) |
|---|---|---|
| `Roam` 游荡 | 沿 NavMesh 慢巡 + 监听采样 | → `Lure`(有样本且有人在听)· → `Hunt`(强注意度源很近) |
| `Lure` 诱骗 | 选一名玩家声音,从诱饵点广播假报点 | → `Hunt`(某玩家靠近本体 / 落单)· → `Roam`(无人上钩,冷却) |
| `Hunt` 追近 | 朝最强声音源寻路逼近 | → `Attack`(进入 `attackRange`)· → `Lure`(目标静默且拉开)· → `Roam`(彻底丢失) |
| `Attack` 啃咬 | 近身,周期性扣血 | → `Hunt`(目标逃开)· →(目标 Downed)`Roam` / `Lure` |

*注*:`Failure`(全员 Downed)由 Mission 状态机判定,不是本系统的状态。`Roam` 是默认归位态。

### Interactions with Other Systems

| 系统 | 输入 | 输出 / 契约 | 归属 |
|---|---|---|---|
| **Networking**(ADR-0001) | — | 主机决定录 / 放 / 位置 / 谁的声音,NetworkVariable + RPC 同步;客户端只读播放 | 网络管传输;本系统持权威 |
| **ProximityVoiceChat**(已有) | 每名玩家的近距离语音音频流 | 复用其采集与**空间化回放**管线来录样本、从诱饵点放声 | VOIP 拥有音频管线;本系统消费 + 触发回放 |
| **Player** | `PlayerHealth`(扣血 / Downed)、移动状态(注意度) | 读这些算感知;造成伤害 | Player 持有;本系统读 / 施加 |
| **Level / Map-Gen** | 每个种子的连通 NavMesh + 房间 / 路口节点 | 在其上寻路;诱饵点选自节点;**继承 carve-leak 风险**(Level Open Q#1);不堵唯一通路 | Level 管网格;本系统消费 |
| **Mission State Machine** | — | 放倒玩家 → 计入"全员倒 = Failure";不得破坏可完成性 | Mission 判结局;本系统只扣血 / 放倒 |
| **Monster System / 危险等级** | 站点 `danger_level`(主机) | 活跃度(诱骗频率 / 移速 / 攻击)随危险等级 `lerp(idle, frenzied)` 提升;饱和 = 强制撤离 | Monster System 拥有升级;本怪消费 |
| **Audio** | — | 点名需要音效的事件:孢子噗噗(预警)、回放失真滤镜、啃咬、丢失目标 | 本系统点名;Audio 出规格 |

## Formulas

> Lean 模式:数值为初始建议,**`systems-designer` 调参 pending**。变量在 Tuning Knobs 可调。

**1. `attention`(它朝谁去)**

`attention(p) = w_voice·voiceActive(p) + w_run·isRunning(p) + w_walk·isWalking(p)`(蹲走 / 静止 = 0)

| 变量 | 类型 | 范围 | 说明 |
|---|---|---|---|
| `voiceActive(p)` | bool→0/1 | — | 该玩家此刻是否在对讲机说话 |
| `isRunning / isWalking(p)` | bool→0/1 | — | 移动态 |
| `w_voice` | float | def **1.0** | 说话权重(主感官,最高) |
| `w_run` | float | def **0.5** | 奔跑权重 |
| `w_walk` | float | def **0.15** | 走路权重 |

**输出**:每名玩家一个注意度标量;它向最近 `attentionWindow` 秒内 argmax 的玩家寻路。*例*:A 在说话(1.0)、B 在跑(0.5)→ 它先奔 A。

**2. `sample_capture`**:每名玩家保留最近 `voiceWindow`(def **20s**)、至多 `maxClips`(def **5**)段;FIFO 覆盖。无样本→预置词库 `fallbackLines`。

**3. `lure_broadcast`**:每 `lureInterval`(def **25s**)秒,若 ∃ 玩家近期 attention>0 且库非空 → 选一名"听众"附近的玩家声音,从距本体 ≥ `decoyMinDist`(def **12m**)的 NavMesh 节点回放。

**4. `damage`**:接触持续期间每 `dmgTick`(def **0.6s**)造成 `dmgPerTick`(def **8 HP**);`PlayerHealth` 归零 → Downed。*伤害与 down 的真值归 `PlayerHealth`,此处只触发。*

**输出范围**:典型一次被缠住未脱身 ≈ 数秒内掉数十血;给队友"听到啃咬声→去救"的反应窗口,而非秒杀。

## Edge Cases

- **全队从不说话**:它采不到真样本 → 退化为**预置诱导词**,欺骗力下降;但仍靠走 / 跑声 `Hunt`。**纯沉默不是免死**——任务要求移动(搬运、拉闸)。
- **单人 / Solo**:没有"队友声音"可借 → 它只回放**该玩家自己的声音** + 预置词(自己骗自己,仍诡异);solo 可玩。
- **它把人引向 EDGE 竖井 / 迷路**:允许(环境危险被武器化),但目标物 / 唯一通路不受影响(Level 不变量保证可完成)。
- **它逼停在唯一门口**:不得永久堵死 `criticalPath`——`Hunt` 寻路含让位 / 超时回退,避免 softlock。
- **回放与真人同时说话**:两者都播,空间化区分;`tell`(失真)仍可辨。
- **玩家在被采样时阵亡**:停止其新采样;已有库可继续使用(诡异但允许)。
- **外部语音(Discord 等)**:它只能采到游戏内 `ProximityVoiceChat` 的音频;若全程用 Discord,欺骗失效。**已知局限** → Open Q#2(是否提示 / 激励用游戏内语音,或加非语音感官补偿)。
- **隐私**:回放真人语音在直播 / 录屏下可能涉及录音同意 → Open Q#5(需要一次性提示 / 设置开关)。
- **主机迁移**:同 Mission GDD,MVP 不支持 → 会话结束,无静默损失。
- **客户端晚加入**:接收当前同步状态;不重建历史样本,新样本正常采集。

## Dependencies

**上游(本系统依赖):**

| 系统 | 硬 / 软 | 接口 |
|---|---|---|
| **Networking**(ADR-0001) | **硬** | 主机权威写 / 同步;客户端只读播放 |
| **ProximityVoiceChat** | **硬** | 提供每名玩家语音流 + 空间化回放管线(采样与放声的技术底座) |
| **Player**(`PlayerHealth`) | **硬** | 扣血 / Downed 真值;读移动态算注意度 |
| **Level / Map-Gen** | **硬** | 连通 NavMesh + 节点(寻路 / 诱饵点);依赖 I1/I2/I6 可完成性 |
| **Scene Flow / Game State** | **硬** | Level `Ready` 后生成;返程时清理 |

**下游(依赖本系统):**

| 系统 | 硬 / 软 | 消费 |
|---|---|---|
| **Audio** | **硬** | 它点名的音效事件 + 回放失真滤镜规格 |
| **HUD**(可选) | **软** | 被啃咬时的极简威胁态(若做) |

**双向一致性**:`ProximityVoiceChat` / `PlayerHealth` / `Mission` / `Level` 若有 / 新增文档,需回标"被 Echo Mold 依赖"。flag `/consistency-check`。

## Tuning Knobs

| 旋钮 | 默认 | 安全范围 | 过低 → | 过高 → | 交互 |
|---|---|---|---|---|---|
| `w_voice` | 1.0 | 0.5–2 | 说话不再危险,失去核心张力 | 一开口必被锁,过严 | attention |
| `w_run` / `w_walk` | 0.5 / 0.15 | — | 移动太安全 | 走两步就被追 | attention |
| `voiceWindow` | 20s | 8–40 | 样本太旧 / 太少 | 老样本太多,诡异度↑算力↑ | maxClips |
| `maxClips` | 5 | 2–10 | 回放太重复 | 内存 / 带宽↑ | voiceWindow |
| `lureInterval` | 25s | 10–60 | 假报点太频,识破后烦 | 太稀,欺骗存在感弱 | tellDistortion |
| `decoyMinDist` | 12m | 6–25 | 诱饵离本体太近,易识破 | 太远,引不动 | 地图尺度 |
| `tellDistortion` | 中 | — | 太易识破 = 不吓人 | 太难辨 = 不公平 | 玩家学习曲线 |
| `moveSpeed` | 略低于玩家走速 | — | 永远追不上 | 碾压全队 | attackRange |
| `attackRange` / `dmgPerTick` / `dmgTick` | 1.5m / 8 / 0.6s | — | 不痛 | 秒人 | **PlayerHealth(引用,不在此重定义)** |
| `fallbackLines` | 一组预置诱导词 | — | 沉默局太无聊 | — | 样本库 |

**source-of-truth 注**:伤害数值的最终归属是 `PlayerHealth` / 战斗参数;`mission_clock` 归 Hazard;此处引用不重定义。

## Visual/Audio Requirements

- **造型**:感染的人形宿主,菌伞 / 菌褶 / 孢子囊覆盖躯干,比例略失常(承接 art-bible「wrong proportion」,但**允许真菌怪造型**——§5 待更新)。可借既有第三方资源做基底再长菌。
- **信号色**:全身唯一暖色 = amber-orange `#FF6A00` 孢子眼点(确认信号);其余只用既有调色板(死黑橡胶 / 做旧 / 军绿 / 孢子灰)。无红色身体、无奇幻荧光。
- **行为先于视觉**:孢子噗噗声、回放的金属 / 比特失真感是第一信号;眼点只近距离"确认"。
- **回放音频处理**:统一过一层"失真滤镜"(轻比特压缩 / 混响 / 方向偏移)= 可学的 `tell`;真样本与预置词都过同一滤镜,避免真假音质差异穿帮。
- **光照**:在既有(困难)光照下活动;不关灯。
- 📌 **Asset Spec**:art-bible §5 更新后跑 `/asset-spec system:monster-echo-mold`(宿主造型、孢子眼点、菌巢 / 诱饵点 dressing)。

## UI Requirements

- **无专属屏**。HUD 仅在被啃咬时用极简工单态(art-bible HUD 语言:盖章块,不闪不炫);**不画血条式威胁仪表**(anti-pillar:威胁不做 0–100 条)。
- 可选:一次性"游戏内语音被监听"提示 / 设置开关(见 Open Q#5)。
- 📌 **UX Flag**:若要做威胁态 HUD 提示,Pre-Production 跑 `/ux-design`。

## Acceptance Criteria

> Lean:`qa-lead` 独立复核 pending。

- **GIVEN** 玩家近距离对讲机说话,**WHEN** 回声菌在监听范围,**THEN** 主机记录一段该玩家语音样本,库按 FIFO 不超 `maxClips`。
- **GIVEN** 库非空且 ≥1 玩家在听,**WHEN** `lureInterval` 到,**THEN** 从距本体 ≥ `decoyMinDist` 的节点回放一段该玩家声音,且**所有客户端在同一空间位置听到同一片段**(主机权威同步)。
- **GIVEN** 一段回放,**WHEN** 玩家近距离听,**THEN** 存在可分辨 `tell`(失真 / 方向不符),与真人语音可区分。
- **GIVEN** 回声菌接触玩家,**WHEN** 每 `dmgTick`,**THEN** 该玩家 `PlayerHealth` 扣 `dmgPerTick`;归零 → Downed(可救)。
- **GIVEN** 全队 Downed,**WHEN** 结算,**THEN** 任务 Failure(Mission 判定)。
- **GIVEN** 任意种子地图,**WHEN** 它寻路 / 广播,**THEN** 目标物与唯一通路始终可达(不 softlock)。
- **GIVEN** 2–4 人联机,**WHEN** 录制 / 回放,**THEN** 所有客户端听到一致回放;客户端不自行决定放什么。
- **GIVEN** 玩家全程沉默,**WHEN** 一段时间,**THEN** 它退化为预置词 + 靠移动声 `Hunt`(沉默非免死)。

## Open Questions

| # | 问题 | 负责 | 目标 |
|---|---|---|---|
| 1 | **真 VOIP 捕获 / 空间化回放技术 spike**:缓存结构、带宽、延迟、与 NGO 同步。**MEDIUM 风险**,先做技术验证再大改。 | laplace | 进生产前 |
| 2 | **外部语音(Discord)绕过**:是否提示 / 激励游戏内语音,或加非语音感官补偿。 | zeno + laplace | 设计 |
| 3 | **`tell` 强度调参**:太易=不吓人,太难=不公平。 | systems-designer | 平衡 pass |
| 4 | **移速 vs 玩家**:能追上掉队者,但不碾压全队。 | systems-designer + QA | playtest |
| 5 | **录音隐私 / 同意**:回放真人语音(尤其直播 / 录屏)是否需一次性提示 / 开关。**重要**。 | PM + 法务视角 | 进生产前 |
| 6 | **art-bible §5 更新**:把"不准是怪物"改为"感染生态(infected 人 / 动物 / 植物 / 真菌)+ 信号色 / 行为先于视觉仍适用"。本 GDD 已按此假设写。 | banach + PM | GDD 批准后 |
| 7 | **变身机制(deferred)**:被感染→搅局 / 提示者,后续扩展。 | zeno | post-MVP |
