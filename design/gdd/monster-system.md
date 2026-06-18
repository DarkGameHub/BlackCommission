# Monster System (怪物系统)

> **Status**: In Design
> **Author**: Yan Dai (PM) + Claude (zeno/laplace/banach lens)
> **Last Updated**: 2026-06-14
> **Implements Pillar**: Threat Design(MRC-7 感染生态)· co-op extraction tension · 支柱4「要不要现在走」
> **Priority / Layer**: MVP / Core(框架)
> **Concept (locked 2026-06-14)**: 所有威胁的父框架。核心定律「**一只怪 = 一个主感官 + 一个破解法**」(LC 式,可在死亡中学会)。共享:主机权威 NavMesh AI;统一被抓规则(接触=掉血→倒地→全员倒=Failure);**危险等级**随停留上涨,饱和=强制撤离(替代固定倒计时)。MVP 只发一只样板——回声菌(声音感官);框架预留热感/振动/视线等槽位,后加不返工。
> **Review mode**: Lean — `systems-designer`/`qa-lead` passes deferred.
> **Related**: `design/gdd/monster-echo-mold.md`(roster 首条)· `docs/world-background-2098.md`(感染设定)· `design/art/art-bible.md` §5(已修订允许感染怪)· `design/gdd/mission-state-machine.md`(Downed→Failure;**其固定时钟模型待按本系统改**)· `design/gdd/level-map-generation.md`(NavMesh + 不变量)· `Assets/_Project/Scripts/Player/PlayerHealth.cs`、`PlayerOxygen.cs`、`Network/ProximityVoiceChat.cs`、`Office/OfficeMonsterBestiary.cs` · ADR-0001

## Overview

怪物系统是《Black Commission》所有威胁的统一框架。每只怪都是 MRC-7「贵客瘟疫」催生的**感染生态生物**(感染的人/兽/植物/真菌),且严格遵循一条定律:**它只靠一种主感官狩猎,也只被一种对应行为破解**(致命公司式——靠死亡和试错学会规则,而不是靠武器)。所有怪共享一套主机权威的 NavMesh AI、一个统一的"被抓后果"(接触掉血→倒地→全员倒=任务 Failure),以及一个**站点级"危险等级"**:你们在污染站点里待得越久,站点越"苏醒",怪越活跃,到饱和就**强制撤离**——它替代了死板的固定倒计时,让节奏由你们的玩法快慢决定。MVP 只上线一只样板怪(**回声菌**,声音感官),但框架是按"随时加入新感官(热感、振动、视线……)而不重写"来设计的。没有它,怪物就是一堆各写各的散件;有了它,每只新怪只需填"感官 + 破解 + 表现"三栏。

## Player Fantasy

(系统级:大部分是间接基础设施,但有一层玩家直接学习的规则)

核心幻想是 **"每只怪是一道用'行为'解的谜题,不是用枪解的"**。你们几乎没有真正的武器——活下去靠的是**搞清楚每只怪'听什么/感什么',然后剥夺它那一种感官**:对回声菌闭嘴、对热感怪变冷、对振动怪慢走。一局之内,**危险等级**把这栋楼从"可以慢慢搜"逼成"主动与你为敌",而队伍的纪律(安静、变冷、慢、高效)就是唯一的生存技术。合作层面:每只怪的破解法都是**全队必须协调的共同行为**——一个人喊话,全队暴露。register 是 art-bible §5 修订后的**黑色幽默感染恐怖**,不是纯压抑。

**支柱对齐**:Threat Design(感染生态具象化)、合作撤离张力、以及支柱4"要不要现在走"(危险等级越高越逼你做这个决定)。

## Detailed Design

### Core Rules

1. **主机权威**:所有怪的感知、寻路、状态在主机运行并同步;客户端只渲染同步状态(ADR-0001)。
2. **一只怪 = 一个主感官 + 一个破解法(核心定律)**:每只怪恰好一种主感官(声音/热感/振动/视线/气味/光…)+ 一种匹配的行为破解;**第一警报是行为/听觉,不是视觉**(art-bible §5 保留),感官靠玩法学会。
3. **共享 AI 状态机**:`Roam`(巡逻)→ `Investigate`(去最后探测点)→ `Hunt`(committed 追)→ `Attack`(接触);丢失探测 → 回 `Search`/`Roam`。各怪可微调阈值或特化某一态(如回声菌把 Investigate 特化成"诱骗"),但共享这套骨架。
4. **统一被抓后果(PM 锁定)**:接触/攻击 = 持续掉血(走 `PlayerHealth`);血归零 = Downed(队友可救);**全员 Downed = 任务 Failure**(对接 `mission-state-machine` 规则7)。MVP 无秒杀。
5. **危险等级(PM 锁定,升级属性)**:主机维护一个 0→饱和 的计量器,随**停留时间**上涨,并被**关键动作**(拉电闸、拿走目标物)催出尖峰。等级越高,**所有怪越活跃**(更快、更频、感官范围更大)。**饱和 = 强制撤离**(派遣车撤离 / 强制终局)——**替代固定倒计时**,节奏由玩法决定。虚构:感染站点随活人长时间在场而逐步苏醒。
6. **破解 = 玩家能动性(PM 锁定)**:每种感官有对应行为破解,能拖慢/剥夺探测(声音→安静;热感→变冷/静止/进冷区;振动→慢走/蹲;视线→断视线/掩体)。MVP 不强制道具依赖(道具为后续可选深度)。
7. **生成 / 数量**:Level-Gen `Ready` 后由主机生成;**MVP 每图一只**;框架支持每图 N 只(多怪共存见 Open Q)。
8. **站点绑定的感染虚构**:所有怪是 MRC-7 感染生态(世界设定 + art-bible §5 修订);名字源于世界/职务;唯一暖信号色 amber-orange `#FF6A00`;在既有光照下活动,**不关灯**。
9. **图鉴集成**:每遭遇一只怪,记入办公室图鉴(`OfficeMonsterBestiary`),把"靠死亡学规则"做成**跨局持久**的认知积累。
10. **不得破坏可完成性**:怪的寻路 / 危险表现绝不能 softlock(目标物 + 唯一通路始终可达;Level 不变量 I1/I2/I6)。

### States and Transitions

**共享状态模板**(每只怪继承):

| 状态 | 含义 | 转移 |
|---|---|---|
| `Roam` | 巡逻 + 用其感官监听 | → `Investigate`(感官探测到玩家) |
| `Investigate` | 移向探测点(可被特化,如回声菌的"诱骗") | → `Hunt`(确认/逼近)· → `Roam`(扑空) |
| `Hunt` | committed 追近 | → `Attack`(进攻击距)· → `Investigate`(目标用破解法甩开)· → `Roam`(彻底丢失) |
| `Attack` | 接触,周期掉血 | → `Hunt`(目标逃)· →(目标 Downed)`Roam` |

**危险等级相位**(站点级,独立于单怪状态)。**相位定义权威 = `design/quick-specs/danger-infection-system-2026-06-18.md`**;本表只描述对怪物行为的映射。4 个相位由**进站时间**驱动(纯时间、无动作脉冲 — PM 锁 2026-06-18),re-entry 每次起始相位 +1:

| 相位 | 时间触发 | 对怪物行为的映射 |
|---|---|---|
| `Survey` 勘查 | 进站(0–8 min) | 固定巡逻、感官窄;给搜索窗口 |
| `Active` 活跃 | 8 min | 巡逻范围扩大、反应更快、感官更广 |
| `Pursuit` 追猎 | 18 min | 主动搜索、多 spawn 点激活(相位名用 Pursuit,避开 AI 状态 `Hunt`) |
| `Saturation` 饱和 | 28 min | 全站点敌对;触发强制撤离终局(60s) |

相位是 `danger_level` 连续值上的**离散标记**(管 spawn 激活 / 环境信号 / 感染倍率 / 撤离);怪物活跃度在相位内仍由 `monster_activity` 连续插值(Formula 2)平滑变化。

### Interactions with Other Systems

| 系统 | 输入 | 输出 / 契约 | 归属 |
|---|---|---|---|
| **Networking**(ADR-0001) | — | 主机权威写/同步;客户端只读 | 网络管传输;本系统持权威 |
| **Level / Map-Gen** | 连通 NavMesh + 节点 | 怪寻路 / 危险表现;不堵唯一通路;继承 carve-leak 风险 | Level 管网格 |
| **Mission State Machine** | — | 放倒玩家→计 Failure;**危险等级饱和→触发强制撤离终局**(需把 Mission 的固定时钟改为本系统驱动) | Mission 判终局;本系统提供升级 + 放倒 |
| **Player** | `PlayerHealth`、`PlayerOxygen`、移动/语音(感官输入) | 掉血/倒地;部分感官/危险接掉氧 | Player 持有;本系统读/施加 |
| **ProximityVoiceChat** | 语音流 | 供声音类怪(回声菌)采样/回放 | VOIP 拥有管线 |
| **OfficeMonsterBestiary** | 遭遇事件 | 记录已知怪 + 其规则,跨局学习 | Bestiary 持有 UI/数据 |
| **Audio** | — | 各怪 + 危险等级的环境音(孢子雾加重等) | 本系统点名;Audio 出规格 |
| **回声菌(roster 实例)** | 本框架 | 实现声音感官 + 共享规则 | 子 GDD `monster-echo-mold.md` |

## Formulas

> Lean:初值待 `systems-designer` 调参。

**1. `danger_level`(站点升级,纯时间 — PM 锁 2026-06-18)**

`danger_level(t) = min(SAT, base_rate·t)`   // 无动作脉冲;玩家关键动作不再顶高 danger

| 变量 | 类型 | 范围 | 说明 |
|---|---|---|---|
| `t` | float(s) | 0+ | 进站点后经过的时间(re-entry 按相位 +1 折算起点) |
| `base_rate` | float | 调到 ~28 min 自然饱和 | 每秒基线上涨;相位边界 8/18/28 min 见 danger spec |
| `SAT` | float | def **100** | 饱和阈值 → 强制撤离 |

**输出**:0→100,随时间单调上涨到 `SAT` 触发 Mission 强制撤离终局。相位边界(Survey/Active/Pursuit/Saturation @ 0/8/18/28 min)是这条连续曲线上的离散标记。

**2. `monster_activity`**:`activity = lerp(idleProfile, frenziedProfile, danger_level/SAT)`——把每只怪的移速/感官范围/出手频率在"平静→狂暴"间插值。

**3. `detection`(每怪特化)**:由各怪自己的 GDD 定义(回声菌=声音;热感怪=体温…);本框架只规定"一只怪一种主感官 + 一个破解"。

**4. `catch_damage`(统一)**:接触每 `dmgTick` 扣 `dmgPerTick`(回声菌默认 8/0.6s);真值归 `PlayerHealth`。

## Edge Cases

- **全队又静又冷又慢(完美龟缩)**:单怪探测被剥夺,但 `danger_level` **仍随时间上涨** → 不能无限龟,迟早饱和强制撤离。**纪律降低被抓概率,但买不到无限时间**。
- **危险等级在持有目标物时饱和**:触发强制撤离;按 Mission 的 `forcedTimeoutOutcome` 决定算 Failure 还是强制 PartialReturn(见 Open Q;Mission GDD 待改)。
- **怪把人逼进唯一门口 / 竖井**:不得永久堵死 `criticalPath`(寻路含让位/超时回退);竖井等环境危险可被武器化但目标物始终可达。
- **Solo**:每图一只怪 + 危险等级照常;声音类怪退化(无队友语音可借,见回声菌 GDD)。
- **多怪共存(后续)**:>1 只时,危险等级是共享的;它们不协同(各自感官),但同图叠加难度——MVP 不涉及,框架预留。
- **首次遭遇某怪**:图鉴此前无条目→玩家无提示,纯靠现场学(art-bible「行为先于视觉」);事后入库。
- **主机迁移**:同 Mission GDD,MVP 不支持→会话结束。

## Dependencies

**上游(硬)**:Networking/ADR-0001;Level/Map-Gen(NavMesh+节点);Player(`PlayerHealth`/`PlayerOxygen`/移动语音);Scene Flow(生成时机)。
**上游(软)**:ProximityVoiceChat(仅声音类怪需要)。
**下游**:各单怪 GDD(`monster-echo-mold.md` 等)实现本框架;Mission(消费 Downed + 危险饱和终局);OfficeMonsterBestiary(消费遭遇记录);Audio;HUD。
**双向注**:Mission/Level/Player/Bestiary 文档需回标"被 Monster System 依赖";Mission GDD 的固定时钟需改为"危险等级驱动"。flag `/consistency-check`。

## Tuning Knobs

| 旋钮 | 默认 | 安全范围 | 过低→ | 过高→ | 交互 |
|---|---|---|---|---|---|
| `base_rate`(危险/秒) | 调到 ~28 min 饱和 | — | 永远不饱和,无撤离压力 | 没时间搜就被赶走 | 局时长;相位边界 8/18/28 见 danger spec |
| `SAT` 饱和阈 | 100 | — | — | — | 强制撤离触发 |
| 相位时长(Survey/Active/Pursuit) | 8/10/10 min | 见 danger spec | 太早变凶 | 太晚没张力 | 相位边界;权威值在 danger-infection spec |
| `activity` 插值曲线 | 线性 | — | 升级无感 | 突然爆难 | 每怪 profile |
| 每图怪数 | 1(MVP) | 1–N | — | 过载 | 危险等级共享 |
| `catch_damage` | 见回声菌 | — | 不痛 | 秒人 | **PlayerHealth(引用不重定义)** |
| 每怪感官/破解参数 | 各 GDD | — | — | — | 指向各子 GDD |

## Visual/Audio Requirements

- **统一设计语言**(art-bible §5 修订):感染生态生物;唯一暖色 = `#FF6A00` 信号眼点;只用既有调色板;**行为/音先于视觉**;不关灯。
- **危险等级的环境表达**(关键):随等级上涨 → **孢子雾渐浓**(能见度↓,可接 PlayerOxygen 缓慢掉氧)、环境音床渐紧、远处怪声渐密。**不靠黑暗、不画数字条**(anti-pillar)——玩家从"空气变稠、声音变多"读出"该走了"。
- 每只怪的具体造型/音效在其子 GDD;本框架只定共享规则。
- 📌 **Asset Spec**:art-bible §5 已修订;各怪 GDD 完成后跑 `/asset-spec system:<monster>`。

## UI Requirements

- **怪物图鉴(办公室)**:展示已遭遇怪 + 其感官/破解(学习闭环);接 `OfficeMonsterBestiary`。
- **无威胁血条 / 无危险等级数字条**(anti-pillar):危险等级走环境(孢子雾/音),不走仪表;被啃咬时 HUD 仅极简工单态(art-bible HUD 语言)。
- 📌 **UX Flag**:图鉴屏在 Pre-Production 跑 `/ux-design`。

## Acceptance Criteria

> Lean:`qa-lead` 复核 pending。

- **GIVEN** 任一怪,**WHEN** 玩家未触发其主感官(用对破解法),**THEN** 该怪不进入 Hunt(感官-破解 成对生效)。
- **GIVEN** 任一怪接触玩家,**WHEN** 每 `dmgTick`,**THEN** `PlayerHealth` 扣血;归零→Downed;全员 Downed→Mission Failure。
- **GIVEN** 进入站点,**WHEN** 时间推进 + 触发关键动作,**THEN** `danger_level` 单调上涨,到 `SAT` 触发强制撤离终局。
- **GIVEN** 危险等级上涨,**WHEN** 跨相位阈,**THEN** 怪的活跃度(移速/感官/频率)随之提升,且孢子雾/音床加重(无数字条)。
- **GIVEN** 全队完美执行破解(静/冷/慢),**WHEN** 时间足够,**THEN** 仍会因 `danger_level` 饱和被强制撤离(龟缩买不到无限时间)。
- **GIVEN** 2–4 人联机,**WHEN** 怪 AI 运行,**THEN** 所有客户端看到一致状态(主机权威)。
- **GIVEN** 任意种子地图,**WHEN** 怪寻路/危险表现,**THEN** 目标物与唯一通路始终可达(不 softlock)。
- **GIVEN** 首次遭遇某怪并存活,**WHEN** 回到办公室,**THEN** 图鉴新增该怪及其规则。

## Open Questions

| # | 问题 | 负责 | 目标 |
|---|---|---|---|
| 1 | **危险等级驱动项**:纯时间 vs 时间+在场/动作的权重。 | systems-designer + playtest | 平衡 pass |
| 2 | **饱和终局映射**:危险饱和 → Failure 还是强制 PartialReturn?需与 Mission `forcedTimeoutOutcome` 对齐(Mission GDD 待改)。 | PM + zeno | Mission 修订时 |
| 3 | **Mission 固定时钟 → 危险等级**:把 mission-state-machine 的固定倒计时改为本系统驱动(它已标 Needs Revision)。 | laplace + PM | Mission 修订 |
| 4 | **roster 扩张顺序**:下一只做哪个感官?(热感怪=你的体温点子,首选候选) | PM + zeno | 下一系统 |
| 5 | **多怪共存规则**:>1 只/图 的难度与交互。 | systems-designer | post-MVP |
| 6 | **图鉴揭示深度**:首遇后揭示多少规则(全揭 vs 渐进)。 | hilbert + zeno | UX pass |
| 7 | **周目级回声**(越搬空地球越糟)——deferred。 | zeno | post-MVP |
