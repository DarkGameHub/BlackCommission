using System.Collections.Generic;

namespace BlackCommission.Level
{
    /// <summary>
    /// What a connector physically is. Affects authoring/geometry, not the reachability
    /// math (the validator treats all open edges the same except for <see cref="Edge.OneWay"/>).
    /// </summary>
    public enum EdgeKind
    {
        Corridor,     // 4 m walk-through
        Door,         // 2 m opening
        Stair,        // inter-floor, two-way
        ScaffoldDrop  // inter-floor, one-way DOWN (F2 -> F1)
    }

    /// <summary>
    /// One connector in the tower graph. Immutable. The graph is defined in code
    /// (<see cref="TowerPlanV8"/>) so it is the single, testable source of truth;
    /// the scene only supplies the matching geometry keyed by <see cref="Id"/>.
    /// </summary>
    public readonly struct Edge
    {
        /// <summary>Stable id — used for deterministic ordering, scene matching, and tests.</summary>
        public readonly string Id;
        /// <summary>Endpoint node ids (room slot ids or junction ids).</summary>
        public readonly string A, B;
        public readonly EdgeKind Kind;
        /// <summary>Always open; never toggled. The backbone/critical path is all fixedOpen.</summary>
        public readonly bool FixedOpen;
        /// <summary>Seed may open or close this edge. Mutually exclusive with FixedOpen.</summary>
        public readonly bool Toggleable;
        /// <summary>Part of VAN-&gt;objective-&gt;VAN. Must always stay open (so must be FixedOpen).</summary>
        public readonly bool CriticalPath;
        /// <summary>If true, traversal is A-&gt;B only (e.g. a scaffold drop).</summary>
        public readonly bool OneWay;
        /// <summary>Open probability in per-mille (0..1000). Only used when Toggleable.</summary>
        public readonly int OpenChancePermille;

        public Edge(string id, string a, string b, EdgeKind kind,
            bool fixedOpen, bool toggleable, bool criticalPath, bool oneWay, int openChancePermille)
        {
            Id = id; A = a; B = b; Kind = kind;
            FixedOpen = fixedOpen; Toggleable = toggleable;
            CriticalPath = criticalPath; OneWay = oneWay;
            OpenChancePermille = openChancePermille;
        }
    }

    /// <summary>Result of validating one resolved topology against the invariants.</summary>
    public readonly struct Verdict
    {
        public readonly bool Ok;
        /// <summary>Invariant id that failed (e.g. "I8"), or empty when Ok.</summary>
        public readonly string FailedInvariant;
        public readonly string Reason;

        Verdict(bool ok, string inv, string reason) { Ok = ok; FailedInvariant = inv; Reason = reason; }
        public static Verdict Pass() => new Verdict(true, "", "");
        public static Verdict Fail(string inv, string reason) => new Verdict(false, inv, reason);
        public override string ToString() => Ok ? "OK" : $"FAIL {FailedInvariant}: {Reason}";
    }

    /// <summary>Outcome of resolving a seed into an open-edge set.</summary>
    public readonly struct TopoResult
    {
        /// <summary>Ids of every connector that is OPEN this run.</summary>
        public readonly HashSet<string> OpenEdgeIds;
        /// <summary>Re-roll attempt index that succeeded (0 = first try).</summary>
        public readonly int Attempt;
        /// <summary>True if no seeded roll validated and we fell back to all-open.</summary>
        public readonly bool UsedFallback;
        public readonly Verdict Verdict;

        public TopoResult(HashSet<string> open, int attempt, bool usedFallback, Verdict verdict)
        {
            OpenEdgeIds = open; Attempt = attempt; UsedFallback = usedFallback; Verdict = verdict;
        }
        public bool Ok => Verdict.Ok;
    }

    /// <summary>
    /// Pure, engine-free description of a tower's connectivity: nodes (with floor),
    /// edges (connectors), and the role tags the invariants check against. Built in code
    /// by <see cref="TowerPlanV8"/>; consumed by <see cref="TowerTopology"/>.
    /// </summary>
    public sealed class TopoGraph
    {
        public readonly List<Edge> Edges = new List<Edge>();
        public readonly Dictionary<string, int> NodeFloor = new Dictionary<string, int>();

        /// <summary>Rooms that must ALL be reachable from the van (invariant I8). Excludes junctions/exits.</summary>
        public readonly HashSet<string> RoomNodes = new HashSet<string>();
        /// <summary>Gear/consumable rooms; at least one must be reachable before a stair (I5).</summary>
        public readonly HashSet<string> ConsumableRooms = new HashSet<string>();
        /// <summary>Edge ids that count as a "descent"; at least two must be open (I7).</summary>
        public readonly HashSet<string> DescentEdgeIds = new HashSet<string>();

        public string Van, Lobby, PowerGate, PowerClue, Objective, FireExit;

        // ---- authoring helpers (keep the V3 factory readable) ----

        public TopoGraph Node(string id, int floor, bool room = false, bool consumable = false)
        {
            NodeFloor[id] = floor;
            if (room) RoomNodes.Add(id);
            if (consumable) { RoomNodes.Add(id); ConsumableRooms.Add(id); }
            return this;
        }

        /// <summary>Add a fixed-open edge (never toggled).</summary>
        public TopoGraph Fixed(string id, string a, string b, EdgeKind kind,
            bool critical = false, bool oneWay = false)
        {
            Edges.Add(new Edge(id, a, b, kind, true, false, critical, oneWay, 1000));
            return this;
        }

        /// <summary>Add a seed-toggleable edge (a side loop or shortcut; never the critical path).</summary>
        public TopoGraph Toggle(string id, string a, string b, EdgeKind kind, int openChancePermille = 500)
        {
            Edges.Add(new Edge(id, a, b, kind, false, true, false, false, openChancePermille));
            return this;
        }

        /// <summary>Add a fixed inter-floor descent (counts toward invariant I7).</summary>
        public TopoGraph Descent(string id, string a, string b, EdgeKind kind, bool oneWay = false)
        {
            Edges.Add(new Edge(id, a, b, kind, true, false, true, oneWay, 1000));
            DescentEdgeIds.Add(id);
            return this;
        }

        public int FloorOf(string id) => NodeFloor.TryGetValue(id, out int f) ? f : 1;
    }
}
