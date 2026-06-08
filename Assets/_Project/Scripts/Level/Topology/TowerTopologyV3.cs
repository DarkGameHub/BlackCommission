namespace BlackCommission.Level
{
    /// <summary>
    /// The authoritative connectivity graph for the v3 abandoned-tower map
    /// ("地球海岸壹号 烂尾楼" — design/levels/abandoned-tower-redesign-v3.md +
    /// abandoned-tower-v3-connectivity.md, transcribed from tower_layout_v3.json).
    ///
    /// This is the single source of truth for topology: the generator resolves it per seed
    /// and the scene only supplies geometry keyed by edge id (TowerV3WhiteboxBuilder). Because
    /// it is plain code it is fully unit-testable headlessly — connectivity is proven, not assumed.
    ///
    /// Backbone rule: every ROOM hangs off at least one Fixed edge and the fixed edges form a
    /// connected graph, so I1–I9 hold even with every toggle closed. Toggles only add loops.
    ///
    /// DEVIATIONS from tower_layout_v3.json (flagged for PM, Yan Dai):
    ///   * E-SECUR-TEMP and E-CANTEEN-FORE are ADDED fixed anchors. In the raw JSON, SECUR is
    ///     reachable only via toggle T1 and CANTEEN only via toggle T3, which makes them island
    ///     rooms when those toggles roll closed (violating I8 / the "backbone alone is valid"
    ///     guarantee the project's tests enforce). Each anchor links to a physically adjacent
    ///     room; the original T1/T3 are kept as loop shortcuts.
    ///   * SHAFT (the void) and BRIDGE (decorative scaffold strip) are not graph nodes — no edge
    ///     references them. The shaft is an absence of floor; the scaffold bridge is realised by
    ///     the EDGE↔VIP connector (E-BRIDGE).
    ///   * Per-floor stairs use distinct ids (STAIRA1/STAIRA2, STAIRB1/STAIRB2) because TopoGraph
    ///     keys nodes by a single flat id; the JSON's "F1.STAIRA" maps to STAIRA1, etc.
    /// </summary>
    public static class TowerTopologyV3
    {
        public static TopoGraph BuildCanonical()
        {
            var g = new TopoGraph();

            // ---------------- FLOOR 1 (ground / arrival) ----------------
            g.Node("VAN", 1);                                   // exterior extraction, not a loot room
            g.Node("LOBBY", 1, room: true);                    // 大堂/售楼处 (L) — landmark
            g.Node("WAREHOUSE", 1, room: true, consumable: true); // 西仓库 (L) — 双手(140)·电池
            g.Node("POWER", 1, room: true);                    // 配电房 (S) — power gate
            g.Node("TEMP", 1, room: true);                     // 临时办公 (S) — power clue
            g.Node("SECUR", 1, room: true);                    // 保安室 (S) — 单手(30)
            g.Node("SAMPLE", 1, room: true);                   // 样品间 (S) — 单手(55)
            g.Node("HALL", 1, room: true);                     // 中央施工厅 (L) — atrium + shaft
            g.Node("WORKSHOP", 1, room: true, consumable: true); // 工坊 (M) — 耗材
            g.Node("DOCK", 1, room: true);                     // 装卸坞 (M) — 双手(110) + scaffold-drop landing
            g.Node("DORM", 1, room: true);                     // 宿舍 (M) — evidence
            g.Node("CANTEEN", 1, room: true);                  // 食堂 (M) — 单手(40)
            g.Node("FOREMAN", 1, room: true);                  // 工头办公 (M) — 单手(90)
            g.Node("REBAR", 1, room: true, consumable: true);  // 钢筋堆场 (M) — densify(LC ~15/floor)
            g.Node("PUMP", 1, room: true);                     // 水泵机电房 (S) — densify
            g.Node("SHANTY", 1, room: true);                   // 民工临时棚 (S) — densify
            g.Node("FIRE", 1);                                  // 消防出口 — only exit, not a loot room
            g.Node("COLLAPSE", 1);                             // 塌角 — rubble junction, not a required room
            g.Node("STAIRA1", 1);                              // A梯 (fast/exposed)
            g.Node("STAIRB1", 1);                              // B梯 (dark/safe)

            g.Van = "VAN";
            g.Lobby = "LOBBY";
            g.PowerGate = "POWER";
            g.PowerClue = "TEMP";
            g.FireExit = "FIRE";

            // Backbone — critical path + ring (all fixed).
            g.Fixed("E-VAN", "VAN", "LOBBY", EdgeKind.Corridor, critical: true);
            g.Fixed("E-LH", "LOBBY", "HALL", EdgeKind.Corridor, critical: true);
            g.Fixed("E-H-SA", "HALL", "STAIRA1", EdgeKind.Corridor, critical: true);
            g.Fixed("E-LPWR", "LOBBY", "POWER", EdgeKind.Corridor, critical: true);
            g.Fixed("E-PWR-SB", "POWER", "STAIRB1", EdgeKind.Corridor, critical: true);
            g.Fixed("E-PWR-TEMP", "POWER", "TEMP", EdgeKind.Door, critical: true);
            g.Fixed("E-FIRE", "FOREMAN", "FIRE", EdgeKind.Door, critical: true);
            // Room anchors (fixed, non-critical).
            g.Fixed("E-LSAMP", "LOBBY", "SAMPLE", EdgeKind.Door);
            g.Fixed("E-SAMP-H", "SAMPLE", "HALL", EdgeKind.Corridor);
            g.Fixed("E-HW", "HALL", "WORKSHOP", EdgeKind.Corridor);
            g.Fixed("E-WD", "WORKSHOP", "DOCK", EdgeKind.Door);
            g.Fixed("E-LW", "LOBBY", "WAREHOUSE", EdgeKind.Corridor);
            g.Fixed("E-HN", "HALL", "DORM", EdgeKind.Corridor);
            g.Fixed("E-N-FORE", "DORM", "FOREMAN", EdgeKind.Corridor);
            g.Fixed("E-FORE-SA", "FOREMAN", "STAIRA1", EdgeKind.Corridor);
            // ADDED backbone anchors (see class summary) — keep SECUR/CANTEEN off the island list.
            g.Fixed("E-SECUR-TEMP", "SECUR", "TEMP", EdgeKind.Door);
            g.Fixed("E-CANTEEN-FORE", "CANTEEN", "FOREMAN", EdgeKind.Corridor);
            // Densify anchors (new rooms each get one fixed backbone edge).
            g.Fixed("E-WS-REBAR", "WORKSHOP", "REBAR", EdgeKind.Corridor);
            g.Fixed("E-LOBBY-PUMP", "LOBBY", "PUMP", EdgeKind.Door);
            g.Fixed("E-DOCK-SHANTY", "DOCK", "SHANTY", EdgeKind.Door);

            // Toggleable side loops / shortcuts (seed opens/closes; never critical).
            g.Toggle("T1", "SECUR", "SAMPLE", EdgeKind.Door);
            g.Toggle("T2", "SAMPLE", "WORKSHOP", EdgeKind.Corridor);
            g.Toggle("T3", "DORM", "CANTEEN", EdgeKind.Corridor);
            g.Toggle("T4", "WAREHOUSE", "POWER", EdgeKind.Door);
            g.Toggle("T5", "COLLAPSE", "STAIRB1", EdgeKind.Corridor);
            g.Toggle("T6", "COLLAPSE", "FOREMAN", EdgeKind.Corridor);
            g.Toggle("T10", "REBAR", "DOCK", EdgeKind.Door);
            g.Toggle("T11", "PUMP", "HALL", EdgeKind.Door);
            g.Toggle("T12", "SHANTY", "FOREMAN", EdgeKind.Corridor);

            // ---------------- FLOOR 2 (show-flat / objective) ----------------
            g.Node("TARGET", 2, room: true);    // 纵深目标 (L) — 沙盘 (objective + nest)
            g.Node("SHOWFLAT", 2, room: true);  // 样板间 (M) — warm beacon
            g.Node("EXEC", 2, room: true);      // 行政套间 (M)
            g.Node("MODEL", 2, room: true);     // 模型展厅 (M)
            g.Node("SALES", 2, room: true);     // 销售办公 (M) — 双手(130)
            g.Node("VIP", 2, room: true);       // VIP休息室 (M)
            g.Node("EDGE", 2, room: true);      // 竖井边缘 (L) — unfinished, fall hazard
            g.Node("DANGER", 2, room: true);    // 危险间 (S) — 双手(95) high-risk
            g.Node("MAINT", 2, room: true);     // 检修间 (S)
            g.Node("MARKET", 2, room: true);    // 营销/储物 (S)
            g.Node("BALCONY", 2, room: true);   // 阳台 (S) — scaffold drop to F1
            g.Node("NEGOT", 2, room: true);     // 洽谈区 (M) — densify(LC ~15/floor)
            g.Node("FIN", 2, room: true);       // 财务室 (S) — densify
            g.Node("ARCHIVE", 2, room: true);   // 资料档案室 (S) — densify
            g.Node("TANK", 2, room: true);      // 水箱机房 (S) — densify
            g.Node("STAIRA2", 2);
            g.Node("STAIRB2", 2);

            g.Objective = "TARGET";

            g.Fixed("E-TS", "TARGET", "SHOWFLAT", EdgeKind.Door, critical: true);
            g.Fixed("E-SF-SB", "SHOWFLAT", "STAIRB2", EdgeKind.Corridor, critical: true);
            g.Fixed("E-SALES-SA", "SALES", "STAIRA2", EdgeKind.Corridor, critical: true);
            g.Fixed("E-VIP-BALC", "VIP", "BALCONY", EdgeKind.Door, critical: true);
            g.Fixed("E-TARG-EXEC", "TARGET", "EXEC", EdgeKind.Corridor);
            g.Fixed("E-EXEC-SALES", "EXEC", "SALES", EdgeKind.Corridor);
            g.Fixed("E-SF-MODEL", "SHOWFLAT", "MODEL", EdgeKind.Corridor);
            g.Fixed("E-SF-EDGE", "SHOWFLAT", "EDGE", EdgeKind.Corridor);
            g.Fixed("E-BRIDGE", "EDGE", "VIP", EdgeKind.Corridor);   // scaffold bridge — only direct E↔W, no rails
            g.Fixed("E-VIP-SALES", "VIP", "SALES", EdgeKind.Corridor);
            g.Fixed("E-EDGE-DANGER", "EDGE", "DANGER", EdgeKind.Door);
            g.Fixed("E-EDGE-MAINT", "EDGE", "MAINT", EdgeKind.Door);
            g.Fixed("E-EDGE-MARKET", "EDGE", "MARKET", EdgeKind.Door);
            // Densify anchors.
            g.Fixed("E-SALES-NEGOT", "SALES", "NEGOT", EdgeKind.Corridor);
            g.Fixed("E-EDGE-FIN", "EDGE", "FIN", EdgeKind.Door);
            g.Fixed("E-EDGE-ARCH", "EDGE", "ARCHIVE", EdgeKind.Door);
            g.Fixed("E-MARKET-TANK", "MARKET", "TANK", EdgeKind.Door);

            g.Toggle("T7", "MODEL", "SALES", EdgeKind.Door);
            g.Toggle("T8", "MAINT", "MARKET", EdgeKind.Door);
            g.Toggle("T9", "MARKET", "SHOWFLAT", EdgeKind.Door);
            g.Toggle("T13", "NEGOT", "EXEC", EdgeKind.Door);
            g.Toggle("T14", "ARCHIVE", "FIN", EdgeKind.Door);

            // ---------------- Inter-floor descents (count toward I7) ----------------
            g.Descent("E-STAIRA", "STAIRA1", "STAIRA2", EdgeKind.Stair);
            g.Descent("E-STAIRB", "STAIRB1", "STAIRB2", EdgeKind.Stair);
            g.Descent("E-DROP", "BALCONY", "DOCK", EdgeKind.ScaffoldDrop, oneWay: true); // F2 -> F1 only

            return g;
        }
    }
}
