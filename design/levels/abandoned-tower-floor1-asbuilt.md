# 烂尾楼 · 一层 As-Built 核对(真实场景几何)

> 数据源:`Assets/Scene/AbandonedBuilding_Blockout.unity`(2026-06-07 19:19 版本)。
> 全部坐标由只读解析脚本从场景 YAML 还原(世界坐标,x→东,z↑北,单位米)。
> 配套精确图:`tower_floor1_accurate.svg`。**这是 as-built,不是设计意图**——
> 设计文档(`abandoned-tower-redesign-v3.md`)与旧图 `tower_floor1_zh.svg` 均已过时。

---

## 1. 一层真实平面(ASCII,4m/格,北↑)

```
  40   .    .    .    .    .  FOREMFOREM FIRE FIRE  .
  36 COLLACOLLACOLLACANTECANTEFOREMFOREM FIRE FIRE  .
  32 COLLACOLLACOLLACANTECANTE  .  A梯   .    .    .
  28 COLLACOLLACOLLA DORM DORM  .  A梯   .    .    .
  24 COLLACOLLACOLLA DORM DORM  .    .    .  SHANT  .
  20 B梯   .    .   HALL HALL HALL  .    .   DOCK DOCK
  16 B梯  TEMP  .   HALL HALL HALL  .    .   DOCK DOCK
  12 POWER  .  SECURSAMPL  .    .  WORKSWORKSREBARREBAR
   8   .    .    .    .    .    .  WORKSWORKSREBARREBAR
   4   .    .    .  LOBBYLOBBYLOBBY  .    .    .    .
   0   .    .    .  LOBBYLOBBYLOBBY PUMP  .    .    .
  -4 WAREHWAREHWAREH VAN  VAN  VAN   .    .    .    .
  -8 WAREHWAREHWAREH VAN  VAN  VAN   .    .    .    .
     x=0   4    8   12   16   20   24   28   32   36  40
```

## 2. 精确房间矩形 `[x0,z0]-[x1,z1]`(米)

| 房间 | 矩形 | 尺寸 | 角色 | 备注 |
|---|---|---|---|---|
| WAREHOUSE 西仓库 | [0,-8]-[12,0] | L 12×8 | 随机 | 与货车同排(最南) |
| LOBBY 大堂 | [12,0]-[24,8] | L 12×8 | 固定·接货车 | ★地标◆主入口;地板=铁质stairs材质 |
| PUMP 水泵房 | [26,0]-[30,4] | S 4×4 | densify | x 偏 2m 网格 |
| POWER 配电房 | [0,10]-[4,14] | S 4×4 | ⚡闸门 | z 偏 2m 网格 |
| SECUR 保安室 | [8,10]-[12,14] | S 4×4 | 随机·单30 | z 偏 2m |
| SAMPLE 样品间 | [12,10]-[16,14] | S 4×4 | 随机·单55 | z 偏 2m |
| TEMP 临时办公 | [4,14]-[8,18] | S 4×4 | !线索 | z 偏 2m |
| WORKSHOP 工坊 | [24,8]-[32,16] | M 8×8 | 随机·耗材 | |
| REBAR 钢筋堆场 | [34,8]-[42,16] | M 8×8 | densify | x 偏 2m |
| HALL 中央施工厅 | [12,16]-[24,24] | L 12×8 | 固定·▒竖井 | |
| DOCK 装卸坞 | [34,16]-[42,24] | M 8×8 | 随机·双110 | x 偏 2m |
| STAIRB1 B梯 | [0,16]-[4,24] | 4×8 | 楼梯·暗/安全 | |
| DORM 宿舍 | [12,24]-[20,32] | M 8×8 | !证据 | |
| SHANTY 民工棚 | [34,24]-[38,28] | S 4×4 | densify | x 偏 2m |
| STAIRA1 A梯 | [26,**26.41**]-[30,**37.59**] | 4×**11.18** | 楼梯·快/暴露 | ⚠ 被缩放拉歪,脱离网格 |
| CANTEEN 食堂 | [12,32]-[20,40] | M 8×8 | 随机·单40 | |
| FOREMAN 工头办公 | [22,36]-[30,44] | M 8×8 | 随机·单90·接消防 | |
| VAN 货车 | (14,-8)-(26,0) | — | 撤离(室外) | 无地板(室外) |
| FIRE 消防出口 | (30,36)-(38,44) | — | ▲唯一出口 | |
| COLLAPSE 塌角 | (0,24)-(12,40) | — | ░开天·消防侧 | 无地板(空洞) |

走廊/门(Connector Run,F1 共 29 条边全部已有几何):
`E-VAN E-LH E-H-SA E-LPWR E-PWR-SB E-PWR-TEMP E-FIRE E-LSAMP E-SAMP-H E-HW E-WD
E-LW E-HN E-N-FORE E-FORE-SA E-SECUR-TEMP E-CANTEEN-FORE E-WS-REBAR E-LOBBY-PUMP
E-DOCK-SHANTY` + 种子开关 `T1 T2 T3 T4 T5 T6 T10 T11 T12`。

---

## 3. 问题清单(按严重度)

### 🔴 阻断级
1. **二层被抬到 y≈28.2**,但爬梯 prop(`Factory1Stairs04`)在 **y=4.2**(原设计层高)。
   说明二层是被临时抬高、真实层高应为 4.2。游玩前必须把二层放回 4.2,楼梯才接得上;
   否则上下层断开。
   - (注:PM 已**有意删除**重叠的 `Plate_F1_*` 旧地面板——原来地面是一堆重叠模型。
     现以"房间地板 + 走廊条"铺地,经核对**可行走面完全连通**,见 §3 ✅,不是问题。)

### 🟠 高(违反铁律/影响生成)
3. **楼梯上下不对齐**(强制规则:楼梯井必须同 x,z)。
   - A 梯:F1 中心 (28,32) vs F2 (26,28) → 错位 (2,4)
   - B 梯:F1 (2,20) vs F2 (2,16) → 错位 (0,4)
   连接坡道会变成悬空斜梯,且违反"仅楼梯+竖井对齐"的设计前提。
4. **STAIRA1 地板被缩放拉歪**:z 26.41→37.59(11.18m,非 8m,脱离网格)。编辑器误拖 scale,需复位为 4×8。
5. **两套锚点根并存**:`TOWER_SLOTS`(19 个旧锚点,slotId 仍是 `F1_L1_CentralConstructionHall` 老命名、坐标不同)
   与 `Tower_v3_Whitebox`(34 个当前锚点)。生成器/RoomSlot 扫描会同时拿到两套 → 重复/歧义。旧根应删。

### 🟡 中(网格/材质/一致性)
6. **2m 网格漂移**:POWER/SECUR/SAMPLE/TEMP(z=10/14/18)与 PUMP/DOCK/REBAR/SHANTY(x=26/30/34/38/42)
   坐落在 2m 偏移上,而非设计的 4m 网格。后果:这些房间与 4m 网格邻居之间的墙/门对不齐。
7. **材质半成品且语义错位**:铁质 stairs 材质(`Factory1Stairs01`)贴在 **LOBBY 大堂地板**(不是楼梯);
   仅个别墙/地板换皮(`Factory2Wall05`/`Concrete044C`/`Factory2Floor02`),**走廊 Run 全未贴**。
   材质是逐物体手贴,无"按房型/材质规范"的统一约定。
8. **竖井上下不通透**:F2 竖井空洞(x12–28,z12–16)不在 F1 HALL(z16–24)正上方,垂直视线噱头失效。
9. **文档/图与场景全部漂移**:`abandoned-tower-redesign-v3.md` 的 ASCII 与旧图 `tower_floor1_zh.svg`
   都与真实场景不符(西仓库位置、S 房簇排布、缺 REBAR/PUMP/SHANTY 三房);代码注释引用的
   `tower_layout_v3.json`(号称真相源)**在仓库中不存在**。

### ⚪ 待 PM 判定(可能就是你说的"别的问题")
10. **轮廓不够"烂尾"**:设计要"风车形错落翼 + 塌角 + 脚手架",现状偏紧凑方块团,翼之间错落感弱。
11. **densify 房间(REBAR/PUMP/SHANTY)** 是否保留?它们把每层房数推到 ~17,且都落在 2m 漂移网格上。
12. **建筑外壳**:F1 只有房间盒 + 围挡(Fence_N/S/W,缺 E?),无真正立面/外墙,室内外边界模糊。

### ✅ 已核对正常(避免误报)
- 第一层 29 条连通边几何**全部存在**;无孤岛房间。
- 第一层房间之间**无重叠**。
- **可行走面完全连通**:16 个房间地板全部能从 LOBBY 走到(0.6m 容差);
  每条走廊条都至少接一个房间地板,无悬空走廊。PM 删重叠地面板后地面仍走得通。

### 楼梯游戏元素(PM 手放的 prefab 实例,8 个)
- `Factory1Stairs03` ×6(y≈0,地面层):大堂(12,2.5)、保安/样品(9,14)、TEMP 旁(4.8,18)、
  HALL/B梯(12,20.5)、装卸坞(34,24)。
- `Factory1Stairs04` ×2(**y=4.2**,层高线索):B梯顶(1.2,24.2)、A梯顶(27.4,35.6)。
- ⚠ **重复**:两个 `Factory1Stairs03 (1)` 落在完全相同坐标 (4.79, 0, 18.0),删其一。
- `Factory1Stairs01` 是**材质**,贴在 LOBBY 12×8 地板;真正楼梯元素是上面的 Stairs03/04。

---

## 4. 建议下一步(待批)
- A. 修复方案落地:先补 **F1 连续地面**(阻断级 1)。
- B. 结构清理:删 `TOWER_SLOTS` 旧根;二层复位 4.2;对齐 A/B 梯;复位 STAIRA1 4×8。
- C. 网格归位:把 2m 漂移房间拉回 4m 网格(会牵动相邻墙/门,需重核连接)。
- D. 材质规范:定义"地面/墙/走廊/楼梯"各自的材质,再批量贴(铁质 stairs 应归楼梯)。
- E. 真相源:以本 as-built 文档为 F1 单一真相源,回写 v3 设计文档,弃用 `tower_layout_v3.json` 提法。

> 以上 B/C 涉及改场景 YAML,按 `@AGENTS.md` 需 PM 显式授权;未授权前我只出"编辑器手改清单"。
</content>
</invoke>
