# Tower Room Content Pool — v1 (First Series)

**Status**: Draft for PM review (NOT built)
**Date**: 2026-06-18
**PM**: David
**Map**: 地球海岸壹号·烂尾预售楼 (Map 1)
**Related**: `design/gdd/map-sequence-and-modular-system.md` (§Modular Room System, §Implementation Status),
`Assets/_Project/Scripts/Level/RoomDef.cs`, `RoomSlot.cs`, `TowerRoomCatalog.cs`

---

> **Dressing direction (PM, 2026-06-18): 整图压成「未完工毛坯 + 工业」。** Rooms keep their identities/fiction
> (the sales lie, the show flat, the squatters) but are dressed from the real Asset Store packs —
> `TirgamesAssets/Factory` (power boxes, metal cabinets, barrels, debris ×13, industrial doors/gates, fire
> extinguishers, ceiling lamps, floor decals) and `Sat Productions/Concrete Props Pack` (concrete blocks,
> brick stacks, barriers, pipes) — with residential furniture de-emphasised. A few whitebox pieces (desk /
> notice board / CRT / shelving) are kept where they carry meaning. **Live dressing source of truth =
> `Assets/_Project/Editor/TowerRoomPoolBuilder.cs`**; the per-room prop tables below are the original
> whitebox intent, the builder is the industrial realisation. Pack materials may need a one-time URP
> material conversion if they render magenta.

---

## 1. What this is

The tower's geometry **shell** (walls / corridors / stairs) is fixed and authored. `TowerLayoutGenerator`
drops a **content prefab** at each room slot's anchor per seed. This doc specs the first batch of those
content prefabs — i.e. the **dressing** (furniture + props + loot anchors) for a room footprint, **not**
walls. Each spec'd room becomes one `RoomDef` (size + role + content prefab + tuning) added to a
`TowerRoomCatalog`.

**What the tower's Random slots need** (from the verified scene audit):

| Size | Footprint | Random slots in tower | Rooms in this series |
|------|-----------|-----------------------|----------------------|
| Small (S)  | 4×4   | 6 (VIP·TEMP·PUMP·SAMPLE·SECUR·SHANTY) | 4 |
| Medium (M) | 8×8   | 6 (DORM·CANTEEN·FOREMAN·WORKSHOP·REBAR·DOCK) | 4 |
| Large (L)  | 12×8  | 1 (WAREHOUSE) | 2 |

> Slot names are just IDs — a Random slot accepts **any** RoomDef of matching size; content is pool-driven,
> not name-driven. 10 rooms cover all 13 slots because the high-frequency generic rooms set
> `allowDuplicates` (see §6); variety deepens when the pool expands past this first series.

**Reuse scope** (answer to "will other maps use these too?"): the modular kit (Kit A) is shared across
Maps 1 / 5 / 6. At the content level this batch splits into **🔄 Shared** (generic institutional rooms that
drop into any map's catalog) and **🏢 Tower-only** (pre-sale fiction, this map only). 6 Shared / 4 Tower-only.

## 2. Shared design rules (apply to every room)

1. **Door-agnostic.** The same RoomDef lands in slots whose corridor opening is on different walls, so
   props hug the walls and leave a **clear central circulation path**; never block all sides.
2. **Blockers are deliberate.** At most **1–2** props per room carry `centralBlocker = true` (carving
   `NavMeshObstacle`); everything else is edge dressing the agent routes past.
3. **Loot anchors are forward-looking.** Anchor counts/surfaces are authored now but stay **dormant** —
   the tower is currently the objective-retrieval mission and `LootSpawner` is not wired into it (see GDD
   §Implementation Status). They activate only if/when the scavenge loop is integrated into the tower.
4. **No hand-tuned transforms.** Props are placed against the footprint's anchor frame per the
   `unity-model-fit` methodology, not by eyeballed coordinates.
5. **Palette.** Municipal Debt Noir — civic teal, dead-rubber black, aged paper, sodium amber, dispatch
   green, stamp red. Aged-earth neutral base; value is never signalled by colour.

**Legend** (zone boxes below): `▣` central blocker · `▢` wall/edge prop · `✦` loot anchor · `·` open floor.

---

## 3. Small rooms (4×4)

### S1 · 配电间 Breaker Closet — 🔄 Shared
```
┌───────────┐
│ ▢breaker ✦│  N: breaker panel + open cabinet
│ ·       · │
│ ✦      ▢  │  toolbox on floor / stool
└────[门]───┘
```
| Prop | Surface | Blocker | Loot anchor |
|------|---------|---------|-------------|
| 锈蚀闸刀面板 (wall) | Wall | no | — |
| 开着的配电柜 | Cabinet | no | ✦ Cabinet |
| 地上工具箱 | Floor | no | ✦ Floor |
| 单脚凳 | — | no | — |

**Identity / fiction**: 半数闸刀被拉下,每路贴着户号标签——"12-04 已售"。锈与潮气。
**Loot categories** (dormant): 专业工具 / 住宅固定件.

### S2 · 保安岗亭 Security Booth — 🔄 Shared
```
┌───────────┐
│▢CRT墙   ✦ │  dead monitor bank + logbook desk
│ ·       · │
│ ✦钥匙板   │  key-hook board on wall
└──[门]─────┘
```
| Prop | Surface | Blocker | Loot anchor |
|------|---------|---------|-------------|
| 死掉的监控 CRT 墙 + 值班桌 | DeskSurface | no | ✦ DeskSurface (登记簿/钥匙) |
| 钥匙挂板 | Wall | no | ✦ Wall |
| 旋转椅 | — | no | — |

**Identity / fiction**: 监控墙全灭,登记簿最后一页是封楼通告;civic-teal 漆皮翘起。
**Loot categories** (dormant): 公文 / 钥匙类.

### S3 · 样板卫浴 Show-Flat Bathroom — 🏢 Tower-only
```
┌───────────┐
│ ▢浴缸    ✦│  display tub (rainwater) + fixture
│ ·       · │
│ ✦广告牌▢台盆│ "火星海景 SPA" placard + pedestal sink
└────[门]───┘
```
| Prop | Surface | Blocker | Loot anchor |
|------|---------|---------|-------------|
| 展示浴缸 (积雨水/落叶) | Floor | no | ✦ Floor (固定件) |
| "火星海景 SPA" 广告牌 | Wall | no | ✦ Wall |
| 立柱台盆 + 半贴瓷砖墙 | — | no | — |

**Identity / fiction**: 只贴了一面墙的瓷砖,广告牌承诺"看得见火星的浴缸"。
**Loot categories** (dormant): 住宅固定件.

### S4 · 杂物储藏 Janitor Store — 🔄 Shared
```
┌───────────┐
│▢货架    ✦ │  steel shelving
│ ·       · │
│ ✦   ▢拖把池│ mop sink + paint cans
└──[门]─────┘
```
| Prop | Surface | Blocker | Loot anchor |
|------|---------|---------|-------------|
| 钢制货架 | ShelfSlot | no | ✦ ShelfSlot |
| 拖把池 + 水桶 | Floor | no | ✦ Floor |
| 干涸乳胶漆桶堆 | — | no | — |

**Identity / fiction**: 货架上半是清洁剂半是建材样品;一桶干透的乳胶漆。
**Loot categories** (dormant): 家用科技 / 私人物品.

---

## 4. Medium rooms (8×8)

### M1 · 工人宿舍 Worker Bunkroom — 🔄 Shared
```
┌─────────────────┐
│ ▢双层床  ▢双层床 │  bunks along N & E walls
│ ·             ✦ │  locker base
│ ✦窝    ·        │  squatter nest (blankets/kettle)
│ ▢储物柜 ✦   [门] │
└─────────────────┘
```
| Prop | Surface | Blocker | Loot anchor |
|------|---------|---------|-------------|
| 双层床 ×2 (靠墙) | — | no | — |
| 占屋者的窝 (毯子/搪瓷缸/旧鞋) | Floor | no | ✦ Floor |
| 储物柜排 | ShelfSlot | no | ✦ ShelfSlot |
| 翻倒的奶箱当桌 | Floor | no | ✦ Floor |

**Identity / fiction**: 上铺挂着褪色全家福;有人封楼后还在这住过——一只搪瓷缸,一双没主的鞋。
**Loot categories** (dormant): 私人物品 / 信件.

### M2 · 工地食堂 Site Canteen — 🔄 Shared
```
┌─────────────────┐
│ ▣打饭台═════════ │  serving counter (blocker, runs 2/3)
│ ·             ✦ │
│ ▢长桌 ▢长桌(翻)  │  tables; one bench overturned
│ ✦  ✦布告栏 [门]  │
└─────────────────┘
```
| Prop | Surface | Blocker | Loot anchor |
|------|---------|---------|-------------|
| 打饭台/窗口 | DeskSurface | **yes** | ✦ DeskSurface |
| 长桌 + 长凳 (一张翻倒) | Floor | no | ✦ Floor |
| 布告栏 | Wall | no | ✦ Wall |

**Identity / fiction**: 打饭窗口菜单还写着"今日:红烧";一张长凳翻倒,像走得很急。
**Loot categories** (dormant): 家用 / 私人物品.

### M3 · 销售办公区 Sales Bullpen — 🏢 Tower-only
```
┌─────────────────┐
│ ▣隔间格 ▣隔间格   │  2×2 cubicle block (blocker)
│ ·             ✦ │
│ ✦   ·   玻璃经理间│  glass manager nook (corner)
│ ✦户型墙"已售" [门]│  wall of fake floorplans
└─────────────────┘
```
| Prop | Surface | Blocker | Loot anchor |
|------|---------|---------|-------------|
| 工位隔间组 (死终端) | DeskSurface | **yes** | ✦ DeskSurface ×2 |
| 玻璃墙经理间 | — | no | — |
| "已售/SOLD" 假户型墙 | Wall | no | ✦ Wall |
| "本月销冠"奖杯 | — | no | — |

**Identity / fiction**: 墙上整栋楼都标了"已售",可这楼根本没盖完——这层是这栋楼的谎言。
**Loot categories** (dormant): 公文 / 专业工具.

### M4 · 施工材料间 Construction Stores — 🏢 Tower-only
```
┌─────────────────┐
│ ▢脚手架塔        │  scaffold tower (wall)
│ ·    ▣钢筋捆  ✦  │  rebar bundles (blocker)
│ ✦水泥袋堆 ·      │  cement bag stacks
│ ▢线缆盘     [门] │
└─────────────────┘
```
| Prop | Surface | Blocker | Loot anchor |
|------|---------|---------|-------------|
| 钢筋捆 (按楼层编号) | Floor | **yes** | ✦ Floor |
| 水泥袋堆 | CrateTop | no | ✦ CrateTop |
| 脚手架塔 | — | no | — |
| 线缆盘 | — | no | — |

**Identity / fiction**: 钢筋按楼层编了号,有几捆永远等不到它们的楼层。
**Loot categories** (dormant): 专业工具 / 固定件.

---

## 5. Large rooms (12×8)

### L1 · 毛坯样板层 Unfinished Show-Flat Floor — 🏢 Tower-only
```
┌───────────────────────────┐
│ ▢"您的真实海岸"横幅          │  marketing banner (wall)
│ ·        ▣塑料布沙发    ✦   │  lone display sofa under sheeting
│ ✦样板控制台 ·   ·    ▢隔断墩 │  model-home console / partition stub
│ ✦       ·   树根撑裂的外墙▢✦ │  root-cracked exterior (biome ingress)
└───────────────────[门]──────┘
```
| Prop | Surface | Blocker | Loot anchor |
|------|---------|---------|-------------|
| 塑料膜下的展示沙发 | Floor | **yes** | ✦ Floor |
| 样板间控制台 | DeskSurface | no | ✦ DeskSurface |
| "您的真实海岸"营销横幅 | Wall | no | ✦ Wall |
| 树根撑裂的清水混凝土外墙 | Floor | no | ✦ Floor |
| 矮隔断墩 (毛坯) | — | no | — |

**Identity / fiction**: 样板间永远停在交付前一周——沙发还裹着膜,墙上是"实景"效果图,墙根却被树根撑裂:真实的海岸正从外面长进来。
**Loot categories** (dormant): 住宅固定件 (高价值).

### L2 · 地下卸货区 Loading Bay — 🔄 Shared · floor1Only
```
┌───────────────────────────┐
│ ▢卷帘门(半开)               │  roll-up shutter (wall)
│ ·   ▣托盘堆  ▣托盘堆   ✦    │  pallet stacks (blockers)
│ ✦      ▣抛锚叉车    ·       │  stranded forklift (blocker)
│ ·   积水    ✦       [门]    │  puddle (decal, non-block)
└───────────────────────────┘
```
| Prop | Surface | Blocker | Loot anchor |
|------|---------|---------|-------------|
| 托盘堆 ×2 | CrateTop | **yes** | ✦ CrateTop ×2 |
| 抛锚叉车 (钥匙还插着) | Floor | **yes** | ✦ Floor |
| 半开卷帘门 | Wall | no | — |
| 积水 (地面贴花) | — | no | — |

**Identity / fiction**: 叉车钥匙还插着,电量早没了;卷帘门卡在半空,外面是海风和锈。
**Loot categories** (dormant): 混合 + 重型固定件.

---

## 6. RoomDef metadata summary

`roleFilter = Random` for all (these fill Random slots). `floor1Only/floor2Only = false` (any floor)
unless noted. `weight` = relative odds vs other same-size eligible rooms (tunable).

| RoomDef | Size | Reuse | Floor | weight | allowDuplicates |
|---------|------|-------|-------|--------|-----------------|
| Room_BreakerCloset      | S | 🔄 Shared | any | 2 | **yes** |
| Room_SecurityBooth      | S | 🔄 Shared | any | 1 | no |
| Room_ShowFlatBath       | S | 🏢 Tower  | any | 1 | no |
| Room_JanitorStore       | S | 🔄 Shared | any | 2 | **yes** |
| Room_WorkerBunkroom     | M | 🔄 Shared | any | 2 | no |
| Room_SiteCanteen        | M | 🔄 Shared | any | 1 | **yes** |
| Room_SalesBullpen       | M | 🏢 Tower  | any | 2 | no |
| Room_ConstructionStores | M | 🏢 Tower  | any | 1 | **yes** |
| Room_ShowFlatFloor      | L | 🏢 Tower  | any | 2 | no |
| Room_LoadingBay         | L | 🔄 Shared | **floor1** | 1 | no |

**Slot coverage**: S = 4 defs (2 dup-capable) → fills 6 S slots. M = 4 defs (2 dup-capable) → fills 6 M
slots. L = 2 defs → the 1 L slot draws one per seed (the other is the alternate). All 13 Random slots fill.

## 7. Build plan (after PM approval — NOT yet done)

1. **Content prefab per room** — assemble from the existing whitebox furniture set (same dress-pass
   convention as `RoomDresser`), parented under a single root placed at the slot anchor; tag blockers with
   `NavMeshObstacle (carve)`; drop `LootAnchor` markers at the ✦ spots with the listed `DressingSurface`.
2. **`RoomDef` asset per room** — set size / roleFilter=Random / floor / weight / allowDuplicates /
   contentPrefab per §6.
3. **`TowerRoomCatalog` asset** — list all 10 RoomDefs; assign it to `TowerLayoutGenerator.catalog` in
   `Tower_EarthCoast_01.unity` (currently null).
4. **Verify** — run a few seeds (host): every Random slot fills, no errors, central path navigable;
   optional EditMode test on `TowerRoomCatalog.Candidates` per size/role.
5. **Shared pool split** — move the 6 🔄 Shared RoomDefs into a shared folder so Maps 5/6 catalogs can
   reuse them (material reskin per map is later polish).

## 8. Acceptance criteria

- [ ] Each room reads as its identity within ~2 s (silhouette + 1 signature prop).
- [ ] Door-agnostic: works regardless of which wall the corridor opens on; central path stays clear.
- [ ] ≤ 2 `centralBlocker` props per room; agent can traverse.
- [ ] Loot anchors sit on valid surfaces (authored, dormant until scavenge integration).
- [ ] Catalog fills all 13 Random slots across seeds (via the duplicate-capable rooms).
- [ ] 6 rooms tagged 🔄 Shared are free of pre-sale-specific props (safe to reuse in Maps 5/6).

## 9. Open questions

- **Floor spread**: all Random slots are floor 1 except VIP (S, floor 2). Keeping rooms floor-agnostic
  covers this; no dedicated upper-floor room needed in v1.
- **Tower↔scavenge integration**: whether loot anchors ever go live in the tower is a separate PM decision
  (objective map vs scavenge loop) — does not block this room batch.
- **Pool depth**: v1 leans on duplicates to fill 13 slots. A v2 expansion (≈9 S / 9 M / 3 L) would let
  every slot be distinct per run — author after v1 style is approved.
