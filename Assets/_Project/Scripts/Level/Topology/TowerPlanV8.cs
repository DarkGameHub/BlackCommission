using System.Collections.Generic;

namespace BlackCommission.Level
{
    /// <summary>Geometric category of a slab (drives realization).</summary>
    public enum SlabKind { Room, Corr, Stair, Void, Bridge, Open, Van }

    /// <summary>
    /// Gameplay descriptor, DECOUPLED from geometry/size (V8 rule: a Room-kind slab with
    /// two or more critical doors must be a Hub — transit space, zero loot, high headroom cue).
    /// </summary>
    public enum SlabFunction { Dead, Hub, Corr, Node, Stair, Void, Bridge, Open, Van }

    /// <summary>The three locked room size classes (plus None for non-rooms).</summary>
    public enum PlanSize { None, S, M, L }

    /// <summary>
    /// Door classification. Critical/Fixed are always open; Toggle is seed-rolled;
    /// Junction is a full-span corridor merge (no wall, no door leaf).
    /// </summary>
    public enum PlanDoorType { Critical, Fixed, Toggle, Junction }

    /// <summary>One axis-aligned slab. Units are metres on the 4 m grid; origin SW, x=east, z=north.</summary>
    public readonly struct PlanSlab
    {
        public readonly string Id;
        public readonly int Floor;
        public readonly float X, Z, W, D;
        public readonly SlabKind Kind;
        public readonly PlanSize Size;
        public readonly SlabFunction Function;
        public readonly string LabelCn;

        public PlanSlab(string id, int floor, float x, float z, float w, float d,
            SlabKind kind, PlanSize size, SlabFunction function, string labelCn)
        {
            Id = id; Floor = floor; X = x; Z = z; W = w; D = d;
            Kind = kind; Size = size; Function = function; LabelCn = labelCn;
        }

        public float CenterX => X + W * 0.5f;
        public float CenterZ => Z + D * 0.5f;
    }

    /// <summary>
    /// One door: a hole punched in the shared wall face between two slabs.
    /// <see cref="OffsetM"/> shifts the door centre along the face from the face midpoint
    /// (anti-enfilade rule V8-C2). Perimeter doors (van entrance, plate shutter/threshold,
    /// fire exit) have no shared face and are placed specially by the builder.
    /// </summary>
    public readonly struct PlanDoor
    {
        public readonly string Id;
        public readonly string A, B;
        public readonly PlanDoorType Type;
        public readonly float WidthM;
        public readonly float OffsetM;
        public readonly bool IsPerimeter;

        public PlanDoor(string id, string a, string b, PlanDoorType type,
            float widthM, float offsetM = 0f, bool isPerimeter = false)
        {
            Id = id; A = a; B = b; Type = type;
            WidthM = widthM; OffsetM = offsetM; IsPerimeter = isPerimeter;
        }
    }

    /// <summary>A shared wall face between two slabs: a line at <see cref="At"/> spanning [Lo..Hi].</summary>
    public readonly struct PlanFace
    {
        /// <summary>'x' = the face is a wall at constant x (positions run along z); 'z' = constant z.</summary>
        public readonly char Axis;
        public readonly float At, Lo, Hi;
        public PlanFace(char axis, float at, float lo, float hi) { Axis = axis; At = at; Lo = lo; Hi = hi; }
        public float Mid => (Lo + Hi) * 0.5f;
        public float Span => Hi - Lo;
    }

    /// <summary>
    /// The PM-approved V8 plan for 地球海岸壹号·烂尾预售楼 (slab-partition model, section grammar):
    /// the single source of truth for both the connectivity graph
    /// (<see cref="BuildCanonicalGraph"/> — consumed by <see cref="TowerTopology"/>) and the
    /// whitebox geometry (TowerV8WhiteboxBuilder). Mirrors tools/generate_tower_floorplans_v8.js;
    /// <see cref="ValidatePlan"/> enforces the V8 rules (V8-C1..C6) headlessly in EditMode tests.
    ///
    /// DEVIATIONS from the approved v8 SVGs (flagged for PM, Yan Dai, 2026-06-10):
    ///   * T3 (DORM-CANTEEN) and T12 (DOCK-SHANTY) are promoted to FIXED doors D20/D21.
    ///     CANTEEN and SHANTY were reachable only through toggles, which violates the project's
    ///     backbone rule (I8: every required room reachable with all toggles closed) — the same
    ///     fix V3 applied via added anchors. Toggle count is therefore 9 (7 F1 + 2 F2).
    ///   * VIP (T7) and BALCONY (T17) stay toggle-gated BY DESIGN (seed-gated bonus room /
    ///     jump-drop gate) and are deliberately NOT in the required-reachable room set.
    ///   * The F2 plate has NO floor over COLLAPSE (x 0..12, z 28..40): the collapsed corner
    ///     goes through both floors, which is what makes the F1 skylight (塌角天光) possible.
    /// </summary>
    public static class TowerPlanV8
    {
        // Building slab outline (both floors). The van forecourt sits outside, south.
        public const float OutlineX = 0f, OutlineZ = 0f, OutlineW = 44f, OutlineD = 40f;
        public const float GridCell = 4f;

        /// <summary>Corridor straight runs longer than this need a declared mid break node (V8-C4).</summary>
        public const float MaxCorridorRun = 16f;
        /// <summary>A door may use at most this fraction of its shared wall face (V8-C1).</summary>
        public const float MaxDoorFaceFraction = 0.5f;
        /// <summary>Minimum centre-to-centre spacing of critical doors on opposite parallel hub faces (V8-C2).</summary>
        public const float MinHubDoorOffset = 2f;
        /// <summary>Clearance kept between a door edge and the wall corner.</summary>
        public const float DoorCornerClearance = 0.45f;

        public static readonly PlanSlab[] Slabs =
        {
            // ---------- F1 ----------
            new PlanSlab("VAN",      1, 14,-10, 12, 8, SlabKind.Van,  PlanSize.None, SlabFunction.Van,  "委托车/前院"),
            new PlanSlab("WAREHOUSE",1,  0,  0, 12, 8, SlabKind.Room, PlanSize.L,    SlabFunction.Dead, "西仓库"),
            new PlanSlab("LOBBY",    1, 12,  0, 12, 8, SlabKind.Room, PlanSize.L,    SlabFunction.Hub,  "大堂·售楼处"),
            new PlanSlab("PUMP",     1, 24,  0,  4, 4, SlabKind.Room, PlanSize.S,    SlabFunction.Dead, "水泵机电"),
            new PlanSlab("SECUR",    1, 24,  4,  4, 4, SlabKind.Room, PlanSize.S,    SlabFunction.Dead, "保安室"),
            new PlanSlab("WORKSHOP", 1, 28,  0,  8, 8, SlabKind.Room, PlanSize.M,    SlabFunction.Dead, "工坊"),
            new PlanSlab("C1",       1,  0,  8, 16, 4, SlabKind.Corr, PlanSize.None, SlabFunction.Corr, "南廊·西"),
            new PlanSlab("C2",       1, 16,  8,  4, 8, SlabKind.Corr, PlanSize.None, SlabFunction.Node, "门厅路口"),
            new PlanSlab("C3",       1, 20,  8, 24, 4, SlabKind.Corr, PlanSize.None, SlabFunction.Corr, "南廊·东"),
            new PlanSlab("POWER",    1,  0, 12,  4, 4, SlabKind.Room, PlanSize.S,    SlabFunction.Dead, "配电房 P-01"),
            new PlanSlab("C4",       1,  4, 12,  4,12, SlabKind.Corr, PlanSize.None, SlabFunction.Corr, "西廊"),
            new PlanSlab("SAMPLE",   1,  8, 12,  4, 4, SlabKind.Room, PlanSize.S,    SlabFunction.Dead, "样品间"),
            new PlanSlab("STAIRB1",  1,  0, 16,  4, 8, SlabKind.Stair,PlanSize.None, SlabFunction.Stair,"B梯(暗/稳)"),
            new PlanSlab("HALL",     1, 12, 16, 12, 8, SlabKind.Room, PlanSize.L,    SlabFunction.Hub,  "中央施工厅(中庭挑空)"),
            new PlanSlab("C5",       1, 24, 12,  4,12, SlabKind.Corr, PlanSize.None, SlabFunction.Corr, "东廊"),
            new PlanSlab("REBAR",    1, 28, 12,  8, 8, SlabKind.Room, PlanSize.M,    SlabFunction.Dead, "钢筋堆场"),
            new PlanSlab("DOCK",     1, 36, 12,  8, 8, SlabKind.Room, PlanSize.M,    SlabFunction.Dead, "装卸坞·跳降着陆"),
            new PlanSlab("SHANTY",   1, 40, 20,  4, 4, SlabKind.Room, PlanSize.S,    SlabFunction.Dead, "民工棚"),
            new PlanSlab("TEMP",     1,  4, 24,  4, 4, SlabKind.Room, PlanSize.S,    SlabFunction.Dead, "临时办公(线索)"),
            new PlanSlab("C6",       1, 24, 24,  6, 4, SlabKind.Corr, PlanSize.None, SlabFunction.Node, "A梯前厅"),
            new PlanSlab("DORM",     1, 12, 24,  8, 8, SlabKind.Room, PlanSize.M,    SlabFunction.Dead, "宿舍(证据)"),
            new PlanSlab("COLLAPSE", 1,  0, 28, 12,12, SlabKind.Void, PlanSize.None, SlabFunction.Void, "塌角·露天(天光)"),
            new PlanSlab("STAIRA1",  1, 26, 28,  4, 8, SlabKind.Stair,PlanSize.None, SlabFunction.Stair,"A梯(快/暴露)"),
            new PlanSlab("CANTEEN",  1, 12, 32,  8, 8, SlabKind.Room, PlanSize.M,    SlabFunction.Dead, "食堂"),
            new PlanSlab("FOREMAN",  1, 30, 32,  8, 8, SlabKind.Room, PlanSize.M,    SlabFunction.Dead, "工头办公·消防口"),
            // ---------- F2 ----------
            new PlanSlab("PLATE",    2,  0,  0, 44,40, SlabKind.Open, PlanSize.None, SlabFunction.Open, "毛坯开放板"),
            new PlanSlab("ATRIUM",   2, 12, 16, 12, 8, SlabKind.Void, PlanSize.None, SlabFunction.Void, "中庭挑空(下方=施工厅)"),
            new PlanSlab("BRIDGE2",  2, 12, 18, 12, 4, SlabKind.Bridge,PlanSize.None,SlabFunction.Bridge,"脚手桥(唯一跨越)"),
            new PlanSlab("STAIRB2",  2,  0, 16,  4, 8, SlabKind.Stair,PlanSize.None, SlabFunction.Stair,"B梯+欠款卷帘①"),
            new PlanSlab("STAIRA2",  2, 26, 28,  4, 8, SlabKind.Stair,PlanSize.None, SlabFunction.Stair,"A梯+卷帘②"),
            new PlanSlab("C7",       2, 26, 24,  4, 4, SlabKind.Corr, PlanSize.None, SlabFunction.Node, "A梯前厅"),
            new PlanSlab("SALES",    2, 24, 16,  8, 8, SlabKind.Room, PlanSize.M,    SlabFunction.Hub,  "销售办公(沙盘陈设)"),
            new PlanSlab("SHOWFLAT", 2, 32, 16,  8, 8, SlabKind.Room, PlanSize.M,    SlabFunction.Hub,  "样板间·精装"),
            new PlanSlab("TARGET",   2, 32,  8, 12, 8, SlabKind.Room, PlanSize.L,    SlabFunction.Dead, "「真实海岸」生态柱展厅"),
            new PlanSlab("VIP",      2, 32, 24,  4, 4, SlabKind.Room, PlanSize.S,    SlabFunction.Dead, "VIP休息室(随机奖励)"),
            new PlanSlab("BALCONY",  2, 40, 16,  4, 4, SlabKind.Room, PlanSize.S,    SlabFunction.Dead, "阳台·跳降口"),
        };

        public static readonly PlanDoor[] Doors =
        {
            // ---- F1 critical (4 m-face doors capped at 2.0 m; D-VAN offset +3 breaks the
            //      D-VAN/D1 enfilade through the LOBBY hub)
            new PlanDoor("D-VAN", "VAN", "LOBBY", PlanDoorType.Critical, 2.8f, 3f, isPerimeter: true),
            new PlanDoor("D1", "LOBBY", "C2", PlanDoorType.Critical, 2.0f),
            new PlanDoor("J1", "C1", "C2", PlanDoorType.Junction, 0f),
            new PlanDoor("J2", "C2", "C3", PlanDoorType.Junction, 0f),
            new PlanDoor("J3", "C1", "C4", PlanDoorType.Junction, 0f),
            new PlanDoor("D4", "C4", "POWER", PlanDoorType.Critical, 2.0f),
            new PlanDoor("D5", "C4", "STAIRB1", PlanDoorType.Critical, 2.0f, -2f),
            new PlanDoor("D7", "C2", "HALL", PlanDoorType.Critical, 2.0f),
            new PlanDoor("D8", "HALL", "C5", PlanDoorType.Critical, 2.8f),
            new PlanDoor("J4", "C3", "C5", PlanDoorType.Junction, 0f),
            new PlanDoor("J5", "C5", "C6", PlanDoorType.Junction, 0f),
            new PlanDoor("D10", "C6", "STAIRA1", PlanDoorType.Critical, 2.0f, -0.55f),
            // ---- F1 fixed
            new PlanDoor("D6", "C4", "TEMP", PlanDoorType.Fixed, 2.0f),
            new PlanDoor("D11", "C1", "WAREHOUSE", PlanDoorType.Fixed, 2.8f),
            new PlanDoor("D12", "C1", "SAMPLE", PlanDoorType.Fixed, 2.0f),
            new PlanDoor("D13", "C3", "SECUR", PlanDoorType.Fixed, 2.0f),
            new PlanDoor("D14", "C3", "WORKSHOP", PlanDoorType.Fixed, 2.8f),
            new PlanDoor("D15", "C3", "DOCK", PlanDoorType.Fixed, 2.8f),
            new PlanDoor("D16", "C3", "REBAR", PlanDoorType.Fixed, 2.8f),
            new PlanDoor("D17", "LOBBY", "PUMP", PlanDoorType.Fixed, 2.0f),
            new PlanDoor("D18", "HALL", "DORM", PlanDoorType.Fixed, 2.0f),
            new PlanDoor("D19", "STAIRA1", "FOREMAN", PlanDoorType.Fixed, 2.0f),
            // Backbone anchors promoted from toggles T3/T12 (see class summary).
            new PlanDoor("D20", "DORM", "CANTEEN", PlanDoorType.Fixed, 2.0f),
            new PlanDoor("D21", "DOCK", "SHANTY", PlanDoorType.Fixed, 2.0f),
            // Fire exit: perimeter gap in FOREMAN's north (outline) wall.
            new PlanDoor("E-FIRE", "FOREMAN", "FIRE", PlanDoorType.Critical, 2.0f, -0.6f, isPerimeter: true),
            // ---- F1 toggles (7) — T1/T4 rewired off the LOBBY hub (V8-C5)
            new PlanDoor("T1", "SECUR", "WORKSHOP", PlanDoorType.Toggle, 2.0f),
            new PlanDoor("T2", "C4", "SAMPLE", PlanDoorType.Toggle, 2.0f),
            new PlanDoor("T4", "C1", "WAREHOUSE", PlanDoorType.Toggle, 2.0f, -4f),
            new PlanDoor("T5", "TEMP", "COLLAPSE", PlanDoorType.Toggle, 2.0f),
            new PlanDoor("T6", "COLLAPSE", "CANTEEN", PlanDoorType.Toggle, 2.0f),
            new PlanDoor("T10", "C5", "REBAR", PlanDoorType.Toggle, 2.0f),
            new PlanDoor("T11", "PUMP", "WORKSHOP", PlanDoorType.Toggle, 2.0f),
            // ---- F2 critical (D32 capped 2.0 on its 4 m face; D33 offset -2 breaks the
            //      bridge->sales->showflat straight axis)
            new PlanDoor("D30", "STAIRB2", "PLATE", PlanDoorType.Critical, 2.8f, 2f, isPerimeter: true),
            new PlanDoor("D31", "PLATE", "BRIDGE2", PlanDoorType.Critical, 2.8f, 0f, isPerimeter: true),
            new PlanDoor("D32", "BRIDGE2", "SALES", PlanDoorType.Critical, 2.0f),
            new PlanDoor("D33", "SALES", "SHOWFLAT", PlanDoorType.Critical, 2.8f, -2f),
            new PlanDoor("D34", "SHOWFLAT", "TARGET", PlanDoorType.Critical, 2.8f),
            new PlanDoor("D35", "STAIRA2", "C7", PlanDoorType.Critical, 2.0f, 0.55f),
            new PlanDoor("D36", "C7", "SALES", PlanDoorType.Critical, 2.0f),
            // ---- F2 toggles (2) — both ends deliberately seed-gated bonus/escape spaces
            new PlanDoor("T7", "SHOWFLAT", "VIP", PlanDoorType.Toggle, 2.0f),
            new PlanDoor("T17", "TARGET", "BALCONY", PlanDoorType.Toggle, 2.0f),
        };

        /// <summary>Inter-floor descents: (edge id, upper, lower, oneWay).</summary>
        public static readonly (string Id, string A, string B, bool OneWay)[] Descents =
        {
            ("E-STAIRA", "STAIRA1", "STAIRA2", false),
            ("E-STAIRB", "STAIRB1", "STAIRB2", false),
            ("E-DROP", "BALCONY", "DOCK", true), // F2 -> F1 only (jump drop)
        };

        /// <summary>Corridor mid-break nodes (V8-C4): (slab id, x, z, label).</summary>
        public static readonly (string SlabId, float X, float Z, string LabelCn)[] CorridorBreaks =
        {
            ("C3", 30f, 10f, "廊中节点:指向牌+工程灯"),
        };

        /// <summary>
        /// F2 material stacks (cover props; fixed positions, 1.6×1.2 m each). Kept clear of the
        /// sightline corridor (V8-C6) and of the open COLLAPSE/ATRIUM/stair holes.
        /// </summary>
        public static readonly (float X, float Z)[] PlateStacks =
        {
            (3,3),(8,6),(16,4),(7,26),(14,35),(16,30),(20,34),(33,34),(39,30),(21,8),(14,11),
        };

        /// <summary>Stack-free sightline corridor: B-shutter exit -> show-flat beacon (V8-C6).</summary>
        public const float SightX = 4f, SightZ = 18f, SightW = 28f, SightD = 4f;

        /// <summary>Light anchors per the spatial-language spec: (id, floor, x, z, hex colour, label).</summary>
        public static readonly (string Id, int Floor, float X, float Z, string Hex, string LabelCn)[] LightAnchors =
        {
            ("LA-DUTY",    1, 17f,   6.5f,  "#E8A33D", "值班台残灯(暖·自D1可见)"),
            ("LA-P01",     1, 1f,    15.2f, "#C43A2A", "P-01悬挂红灯(门外可见)"),
            // B-stair sodium pair per docs/design/tower-stair-ux-notes.md: main lamp lights
            // the flight side wall; spill lamp throws the sodium pool onto the corridor
            // floor outside D5 so the stair is perceivable 3-5 m before the door.
            ("LA-SODIUM",  1, 2.0f,  18.0f, "#D9B25A", "B梯钠灯频闪(主·梯段侧壁)"),
            ("LA-SODIUM2", 1, 3.8f,  18.0f, "#D9B25A", "B梯钠灯溢出(门外地面光斑)"),
            ("LA-SKY",     1, 6f,    36f,   "#EEF0F4", "塌角天光(指北)"),
            ("LA-BEACON",  2, 36f,   22f,   "#E8F0F4", "样板岛冷光(全图灯塔)"),
            ("LA-ECO",     2, 35f,   13.5f, "#7FD4C0", "生态柱微光(变异藻荧光)"),
            ("LA-SALES",   2, 27.5f, 21.5f, "#E8A33D", "销售台暖灯(北墙)"),
        };

        /// <summary>Seed-randomized monster start markers (感染监理): (id, x, z). All on F2.</summary>
        public static readonly (string Id, float X, float Z)[] MonsterSeeds =
        {
            ("MS-NEST", 40f, 10f),       // nest: 生态柱展厅
            ("MS-ALT-SALES", 26f, 17f),
            ("MS-ALT-BRIDGE", 23.2f, 21f),
        };

        /// <summary>Toggle-gated bonus rooms: fillable, but NOT in the required-reachable set (I8).</summary>
        public static readonly HashSet<string> SeedGatedBonusRooms = new HashSet<string> { "VIP", "BALCONY" };

        /// <summary>Junction merges that carry the critical path (B-route C2→C1→C4; A-route C5→C6).</summary>
        static readonly HashSet<string> CriticalJunctions = new HashSet<string> { "J1", "J3", "J5" };

        static Dictionary<string, PlanSlab> cache;
        public static IReadOnlyDictionary<string, PlanSlab> ById
        {
            get
            {
                if (cache == null)
                {
                    cache = new Dictionary<string, PlanSlab>();
                    foreach (PlanSlab s in Slabs) cache[s.Id] = s;
                }
                return cache;
            }
        }

        /// <summary>The shared wall face between two touching slabs, or false if they do not touch.</summary>
        public static bool TryGetSharedFace(in PlanSlab a, in PlanSlab b, out PlanFace face)
        {
            const float eps = 0.01f;
            // a's east face against b's west face (or vice versa) -> wall at constant x.
            if (System.Math.Abs(a.X + a.W - b.X) < eps || System.Math.Abs(b.X + b.W - a.X) < eps)
            {
                float at = System.Math.Abs(a.X + a.W - b.X) < eps ? a.X + a.W : b.X + b.W;
                float lo = System.Math.Max(a.Z, b.Z), hi = System.Math.Min(a.Z + a.D, b.Z + b.D);
                if (hi - lo > eps) { face = new PlanFace('x', at, lo, hi); return true; }
            }
            if (System.Math.Abs(a.Z + a.D - b.Z) < eps || System.Math.Abs(b.Z + b.D - a.Z) < eps)
            {
                float at = System.Math.Abs(a.Z + a.D - b.Z) < eps ? a.Z + a.D : b.Z + b.D;
                float lo = System.Math.Max(a.X, b.X), hi = System.Math.Min(a.X + a.W, b.X + b.W);
                if (hi - lo > eps) { face = new PlanFace('z', at, lo, hi); return true; }
            }
            face = default;
            return false;
        }

        /// <summary>
        /// The door's centre on its wall: (axis, wall position, centre along the face).
        /// Mirrors the builder's placement so validation and geometry can't drift.
        /// Returns false for junctions and the unplaceable perimeter cases it doesn't model.
        /// </summary>
        public static bool TryGetDoorCenter(in PlanDoor d, out char axis, out float at, out float pos)
        {
            axis = 'x'; at = 0f; pos = 0f;
            if (d.Type == PlanDoorType.Junction) return false;
            if (d.IsPerimeter)
            {
                switch (d.Id)
                {
                    case "D-VAN": // LOBBY south outline wall; face mid x=18 + offset
                        axis = 'z'; at = 0f; pos = 18f + d.OffsetM; return true;
                    case "E-FIRE": // FOREMAN north outline wall; face mid x=34 + offset
                        axis = 'z'; at = OutlineD; pos = 34f + d.OffsetM; return true;
                    case "D30": // STAIRB2 east face shutter onto the plate
                        axis = 'x'; at = 4f; pos = 20f + d.OffsetM; return true;
                    case "D31": // BRIDGE2 west threshold from the plate
                        axis = 'x'; at = 12f; pos = 20f + d.OffsetM; return true;
                    default: return false;
                }
            }
            if (!ById.TryGetValue(d.A, out PlanSlab a) || !ById.TryGetValue(d.B, out PlanSlab b)) return false;
            if (!TryGetSharedFace(a, b, out PlanFace f)) return false;
            axis = f.Axis; at = f.At; pos = f.Mid + d.OffsetM;
            return true;
        }

        static bool Overlap(in PlanSlab a, in PlanSlab b)
        {
            float ix = System.Math.Min(a.X + a.W, b.X + b.W) - System.Math.Max(a.X, b.X);
            float iz = System.Math.Min(a.Z + a.D, b.Z + b.D) - System.Math.Max(a.Z, b.Z);
            return ix > 0.01f && iz > 0.01f;
        }

        /// <summary>
        /// All V8 plan rules, headless. Empty list = plan valid. Mirrors the checks in
        /// tools/generate_tower_floorplans_v8.js plus backbone reachability.
        /// </summary>
        public static List<string> ValidatePlan()
        {
            var errors = new List<string>();

            // V8-C0a: no overlap among same-floor slabs (PLATE is background; BRIDGE2 lives in ATRIUM).
            for (int i = 0; i < Slabs.Length; i++)
                for (int j = i + 1; j < Slabs.Length; j++)
                {
                    PlanSlab a = Slabs[i], b = Slabs[j];
                    if (a.Floor != b.Floor || a.Id == "PLATE" || b.Id == "PLATE") continue;
                    if ((a.Id == "ATRIUM" && b.Id == "BRIDGE2") || (a.Id == "BRIDGE2" && b.Id == "ATRIUM")) continue;
                    if (Overlap(a, b)) errors.Add($"overlap: {a.Id} / {b.Id}");
                }

            // V8-C0b: inside the outline (except the van forecourt).
            foreach (PlanSlab s in Slabs)
            {
                if (s.Id == "VAN") continue;
                if (s.X < OutlineX - 0.01f || s.Z < OutlineZ - 0.01f ||
                    s.X + s.W > OutlineX + OutlineW + 0.01f || s.Z + s.D > OutlineZ + OutlineD + 0.01f)
                    errors.Add($"outside outline: {s.Id}");
            }

            // V8-C1: door fits its face, respects offset, and uses <= 50% of the face.
            foreach (PlanDoor d in Doors)
            {
                if (d.Type == PlanDoorType.Junction || d.IsPerimeter) continue;
                if (!ById.TryGetValue(d.A, out PlanSlab a) || !ById.TryGetValue(d.B, out PlanSlab b))
                { errors.Add($"unknown slab in door {d.Id}"); continue; }
                if (!TryGetSharedFace(a, b, out PlanFace f))
                { errors.Add($"no shared face: {d.Id} {d.A}<->{d.B}"); continue; }
                if (d.WidthM > f.Span * MaxDoorFaceFraction + 0.01f)
                    errors.Add($"V8-C1 door > {MaxDoorFaceFraction:P0} of face: {d.Id} ({d.WidthM}m on {f.Span}m)");
                float c = f.Mid + d.OffsetM;
                if (c - d.WidthM * 0.5f < f.Lo + DoorCornerClearance - 0.01f ||
                    c + d.WidthM * 0.5f > f.Hi - DoorCornerClearance + 0.01f)
                    errors.Add($"door off face: {d.Id}");
            }

            // V8-C2: no enfilade through a Hub (critical doors on opposite parallel faces >= 2 m apart).
            foreach (PlanSlab s in Slabs)
            {
                if (s.Function != SlabFunction.Hub) continue;
                var centers = new List<(char Axis, float At, float Pos)>();
                foreach (PlanDoor d in Doors)
                {
                    if (d.Type != PlanDoorType.Critical || (d.A != s.Id && d.B != s.Id)) continue;
                    if (TryGetDoorCenter(d, out char axis, out float at, out float pos))
                        centers.Add((axis, at, pos));
                }
                for (int i = 0; i < centers.Count; i++)
                    for (int j = i + 1; j < centers.Count; j++)
                    {
                        if (centers[i].Axis != centers[j].Axis ||
                            System.Math.Abs(centers[i].At - centers[j].At) < 0.01f) continue;
                        if (System.Math.Abs(centers[i].Pos - centers[j].Pos) < MinHubDoorOffset - 0.01f)
                            errors.Add($"V8-C2 enfilade through hub {s.Id}");
                    }
            }

            // V8-C3: a Room-kind slab with >= 2 critical doors must be a Hub.
            foreach (PlanSlab s in Slabs)
            {
                if (s.Kind != SlabKind.Room) continue;
                int critical = 0;
                foreach (PlanDoor d in Doors)
                    if (d.Type == PlanDoorType.Critical && (d.A == s.Id || d.B == s.Id)) critical++;
                if (critical >= 2 && s.Function != SlabFunction.Hub)
                    errors.Add($"V8-C3 pass-through room not Hub: {s.Id}");
            }

            // V8-C4: corridor straight runs > 16 m need a declared break node.
            foreach (PlanSlab s in Slabs)
            {
                if (s.Function != SlabFunction.Corr) continue;
                if (System.Math.Max(s.W, s.D) > MaxCorridorRun)
                {
                    bool hasBreak = false;
                    foreach (var b in CorridorBreaks) if (b.SlabId == s.Id) hasBreak = true;
                    if (!hasBreak) errors.Add($"V8-C4 corridor run > {MaxCorridorRun}m without break: {s.Id}");
                }
            }

            // V8-C5: every toggle has at least one dead-end (<= 1 non-toggle door) Dead/Void end.
            var nonToggleCount = new Dictionary<string, int>();
            foreach (PlanDoor d in Doors)
            {
                if (d.Type == PlanDoorType.Toggle) continue;
                nonToggleCount.TryGetValue(d.A, out int ca); nonToggleCount[d.A] = ca + 1;
                nonToggleCount.TryGetValue(d.B, out int cb); nonToggleCount[d.B] = cb + 1;
            }
            foreach (PlanDoor d in Doors)
            {
                if (d.Type != PlanDoorType.Toggle) continue;
                bool DeadEnd(string id) =>
                    ById.TryGetValue(id, out PlanSlab s) &&
                    (s.Function == SlabFunction.Dead || s.Function == SlabFunction.Void) &&
                    (!nonToggleCount.TryGetValue(id, out int c) || c <= 1);
                if (!DeadEnd(d.A) && !DeadEnd(d.B))
                    errors.Add($"V8-C5 toggle between non-dead ends: {d.Id} {d.A}<->{d.B}");
            }

            // V8-C6: the sightline corridor (B-shutter -> beacon) is free of material stacks.
            foreach (var (x, z) in PlateStacks)
                if (x + 1.6f > SightX && x < SightX + SightW && z + 1.2f > SightZ && z < SightZ + SightD)
                    errors.Add($"V8-C6 stack blocks sightline corridor at ({x},{z})");

            // V8-C7: backbone reachability — every required room reachable with all toggles closed.
            var adj = new Dictionary<string, List<string>>();
            void Link(string a, string b)
            {
                if (!adj.TryGetValue(a, out List<string> l)) { l = new List<string>(); adj[a] = l; }
                l.Add(b);
            }
            foreach (PlanDoor d in Doors)
                if (d.Type != PlanDoorType.Toggle && d.B != "FIRE") { Link(d.A, d.B); Link(d.B, d.A); }
            foreach (var (_, a, b, oneWay) in Descents) { Link(a, b); if (!oneWay) Link(b, a); }
            var seen = new HashSet<string> { "VAN" };
            var queue = new Queue<string>(); queue.Enqueue("VAN");
            while (queue.Count > 0)
            {
                string n = queue.Dequeue();
                if (!adj.TryGetValue(n, out List<string> nb)) continue;
                foreach (string m in nb) if (seen.Add(m)) queue.Enqueue(m);
            }
            foreach (PlanSlab s in Slabs)
            {
                bool required = (s.Function == SlabFunction.Dead || s.Function == SlabFunction.Hub)
                                && !SeedGatedBonusRooms.Contains(s.Id);
                if (required && !seen.Contains(s.Id))
                    errors.Add($"V8-C7 required room not backbone-reachable: {s.Id}");
            }

            return errors;
        }

        /// <summary>
        /// Build the canonical connectivity graph for the resolver/validator
        /// (<see cref="TowerTopology"/>) directly from the plan tables, so geometry and
        /// connectivity share one source of truth. Replaces TowerTopologyV3.BuildCanonical().
        /// </summary>
        public static TopoGraph BuildCanonicalGraph()
        {
            var g = new TopoGraph();

            foreach (PlanSlab s in Slabs)
            {
                bool required = (s.Function == SlabFunction.Dead || s.Function == SlabFunction.Hub)
                                && !SeedGatedBonusRooms.Contains(s.Id);
                bool consumable = s.Id == "WAREHOUSE" || s.Id == "WORKSHOP" || s.Id == "REBAR";
                g.Node(s.Id, s.Floor, room: required, consumable: consumable);
            }
            g.Node("FIRE", 1); // exterior fire exit, not a slab / loot room

            g.Van = "VAN";
            g.Lobby = "LOBBY";
            g.PowerGate = "POWER";
            g.PowerClue = "TEMP";
            g.FireExit = "FIRE";
            g.Objective = "TARGET";

            foreach (PlanDoor d in Doors)
            {
                EdgeKind kind = d.Type == PlanDoorType.Junction || d.WidthM >= 2.8f
                    ? EdgeKind.Corridor : EdgeKind.Door;
                switch (d.Type)
                {
                    case PlanDoorType.Critical:
                        g.Fixed(d.Id, d.A, d.B, kind, critical: true);
                        break;
                    case PlanDoorType.Junction:
                        g.Fixed(d.Id, d.A, d.B, EdgeKind.Corridor, critical: CriticalJunctions.Contains(d.Id));
                        break;
                    case PlanDoorType.Fixed:
                        g.Fixed(d.Id, d.A, d.B, kind);
                        break;
                    case PlanDoorType.Toggle:
                        g.Toggle(d.Id, d.A, d.B, kind);
                        break;
                }
            }

            foreach (var (id, a, b, oneWay) in Descents)
                g.Descent(id, a, b, id == "E-DROP" ? EdgeKind.ScaffoldDrop : EdgeKind.Stair, oneWay);

            return g;
        }
    }
}
