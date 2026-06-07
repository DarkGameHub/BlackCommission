namespace BlackCommission.Level
{
    /// <summary>
    /// The authoritative connectivity graph for the v3 abandoned-tower map
    /// (design/levels/abandoned-tower-redesign-v3.md +
    /// design/levels/abandoned-tower-v3-connectivity.md).
    ///
    /// This is the single source of truth for topology: the generator resolves it per seed
    /// and the scene only supplies geometry keyed by edge id. Because it is plain code it is
    /// fully unit-testable headlessly — connectivity is proven, not assumed.
    ///
    /// Backbone rule: every room hangs off at least one Fixed edge, and the fixed edges form
    /// a connected ring, so the invariants hold even with every toggle closed. Toggles only
    /// add loops/shortcuts.
    /// </summary>
    public static class TowerTopologyV3
    {
        public static TopoGraph BuildCanonical()
        {
            var g = new TopoGraph();

            // ---------------- FLOOR 1 (ground / arrival) ----------------
            g.Node("VAN", 1);
            g.Node("LOBBY", 1, room: true);
            g.Node("HALL", 1, room: true);
            g.Node("PWR", 1, room: true);
            g.Node("TEMP", 1, room: true);
            g.Node("SECUR", 1, room: true);
            g.Node("SAMPLE", 1, room: true);
            g.Node("WORKSHOP", 1, consumable: true);
            g.Node("WAREHOUSE", 1, consumable: true);
            g.Node("DORM", 1, room: true);
            g.Node("CANTEEN", 1, room: true);
            g.Node("FOREMAN", 1, room: true);
            g.Node("DOCK", 1, room: true);
            g.Node("COLLAPSE", 1);   // rubble junction, not a required room
            g.Node("FIRE", 1);       // fire exit node
            g.Node("JW", 1); g.Node("JSW", 1); g.Node("JNW", 1); g.Node("JN", 1); g.Node("JNE", 1);
            g.Node("STAIRA1", 1); g.Node("STAIRB1", 1);

            g.Van = "VAN";
            g.Lobby = "LOBBY";
            g.PowerGate = "PWR";
            g.PowerClue = "TEMP";
            g.FireExit = "FIRE";

            // Backbone — critical path + ring (all fixed).
            g.Fixed("E-VAN", "VAN", "LOBBY", EdgeKind.Corridor, critical: true);
            g.Fixed("E-LH", "LOBBY", "HALL", EdgeKind.Corridor, critical: true);
            // West loop
            g.Fixed("E-HW", "HALL", "JW", EdgeKind.Corridor);
            g.Fixed("E-LSW", "LOBBY", "JSW", EdgeKind.Corridor);
            g.Fixed("E-W-SW", "JW", "JSW", EdgeKind.Corridor);
            g.Fixed("E-JW-WS", "JW", "WORKSHOP", EdgeKind.Corridor);
            g.Fixed("E-WARE", "JW", "WAREHOUSE", EdgeKind.Corridor);
            g.Fixed("E-SW-PWR", "JSW", "PWR", EdgeKind.Corridor, critical: true);
            g.Fixed("E-PWR-TEMP", "PWR", "TEMP", EdgeKind.Door, critical: true);
            g.Fixed("E-SW-SB", "JSW", "STAIRB1", EdgeKind.Corridor, critical: true);
            // North/east ring
            g.Fixed("E-HNW", "HALL", "JNW", EdgeKind.Corridor);
            g.Fixed("E-NW-N", "JNW", "JN", EdgeKind.Corridor);
            g.Fixed("E-N-NE", "JN", "JNE", EdgeKind.Corridor);
            g.Fixed("E-NE-SA", "JNE", "STAIRA1", EdgeKind.Corridor, critical: true);
            g.Fixed("E-NW-DORM", "JNW", "DORM", EdgeKind.Door);
            g.Fixed("E-N-CAN", "JN", "CANTEEN", EdgeKind.Door);
            g.Fixed("E-NE-FORE", "JNE", "FOREMAN", EdgeKind.Door);
            g.Fixed("E-NE-DOCK", "JNE", "DOCK", EdgeKind.Corridor);
            g.Fixed("E-FORE-FIRE", "FOREMAN", "FIRE", EdgeKind.Door, critical: true);
            g.Fixed("E-FORE-COLL", "FOREMAN", "COLLAPSE", EdgeKind.Corridor);
            // Rooms that would otherwise hang off toggles only — give each a fixed anchor.
            g.Fixed("E-HALL-SAMPLE", "HALL", "SAMPLE", EdgeKind.Door);
            g.Fixed("E-LOB-SECUR", "LOBBY", "SECUR", EdgeKind.Door);

            // Toggleable side loops / shortcuts (seed opens/closes; never critical).
            g.Toggle("T1", "TEMP", "SAMPLE", EdgeKind.Door);
            g.Toggle("T2", "WORKSHOP", "SAMPLE", EdgeKind.Door);
            g.Toggle("T3", "DORM", "CANTEEN", EdgeKind.Door);
            g.Toggle("T4", "WAREHOUSE", "STAIRB1", EdgeKind.Door);
            g.Toggle("T5", "WAREHOUSE", "COLLAPSE", EdgeKind.Corridor);
            g.Toggle("T6", "COLLAPSE", "FIRE", EdgeKind.Corridor);

            // ---------------- FLOOR 2 (show-flat / objective) ----------------
            g.Node("TARGET", 2, room: true);
            g.Node("SHOWFLAT", 2, room: true);
            g.Node("EXEC", 2, room: true);
            g.Node("MODEL", 2, room: true);
            g.Node("SALES", 2, room: true);
            g.Node("SHAFTEDGE", 2, room: true);
            g.Node("MAINT", 2, room: true);
            g.Node("DANGER", 2, room: true);
            g.Node("MARKET", 2, room: true);
            g.Node("VIP", 2, room: true);
            g.Node("BALCONY", 2, room: true);
            g.Node("BRIDGEJ", 2); g.Node("JW2", 2); g.Node("JE2", 2);
            g.Node("STAIRA2", 2); g.Node("STAIRB2", 2);

            g.Objective = "TARGET";

            g.Fixed("E-TS", "TARGET", "SHOWFLAT", EdgeKind.Door, critical: true);
            g.Fixed("E-SF-W2", "SHOWFLAT", "JW2", EdgeKind.Corridor, critical: true);
            g.Fixed("E-W2-SB", "JW2", "STAIRB2", EdgeKind.Corridor, critical: true);
            g.Fixed("E-W2-EDGE", "JW2", "SHAFTEDGE", EdgeKind.Corridor);
            g.Fixed("E-BRIDGE", "SHAFTEDGE", "BRIDGEJ", EdgeKind.Corridor); // scaffold bridge
            g.Fixed("E-MID-E2", "BRIDGEJ", "JE2", EdgeKind.Corridor);
            g.Fixed("E-E2-SALES", "JE2", "SALES", EdgeKind.Corridor);
            g.Fixed("E-E2-SA", "JE2", "STAIRA2", EdgeKind.Corridor, critical: true);
            g.Fixed("E-TARG-EXEC", "TARGET", "EXEC", EdgeKind.Corridor);
            g.Fixed("E-MODEL-SALES", "MODEL", "SALES", EdgeKind.Door);
            g.Fixed("E-E2-VIP", "JE2", "VIP", EdgeKind.Door);
            g.Fixed("E-E2-MARK", "JE2", "MARKET", EdgeKind.Door);
            g.Fixed("E-VIP-BAL", "VIP", "BALCONY", EdgeKind.Door);
            g.Fixed("E-EDGE-MAINT", "SHAFTEDGE", "MAINT", EdgeKind.Door);
            g.Fixed("E-EDGE-DANG", "SHAFTEDGE", "DANGER", EdgeKind.Door);

            g.Toggle("T7", "EXEC", "MODEL", EdgeKind.Door);
            g.Toggle("T8", "MAINT", "DANGER", EdgeKind.Door);
            g.Toggle("T9", "SHOWFLAT", "STAIRB2", EdgeKind.Door);

            // ---------------- Inter-floor descents (count toward I7) ----------------
            g.Descent("E-STAIRA", "STAIRA1", "STAIRA2", EdgeKind.Stair);
            g.Descent("E-STAIRB", "STAIRB1", "STAIRB2", EdgeKind.Stair);
            g.Descent("E-DROP", "BALCONY", "DOCK", EdgeKind.ScaffoldDrop, oneWay: true); // F2 -> F1 only

            return g;
        }
    }
}
