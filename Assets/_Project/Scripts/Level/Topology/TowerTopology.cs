using System.Collections.Generic;

namespace BlackCommission.Level
{
    /// <summary>
    /// Pure, deterministic topology resolver + connectivity validator. Engine-free so it
    /// runs headlessly in EditMode tests and CI. Given the same graph and seed it produces
    /// the same open-edge set on every peer — only the seed crosses the wire, exactly like
    /// the existing content fill (<see cref="TowerLayout"/>).
    ///
    /// Guarantee: a valid layout ALWAYS exists, because the backbone (all FixedOpen edges)
    /// is connected on its own and satisfies every invariant; toggleable edges only ADD
    /// loops. So the all-open fallback can never be disconnected. This is DunGen's
    /// "main path always valid, branches vary," enforced by construction.
    /// </summary>
    public static class TowerTopology
    {
        public const int DefaultRerollCap = 16;

        /// <summary>
        /// Resolve a seed into an open-edge set that satisfies all invariants. Re-rolls
        /// deterministically up to <paramref name="rerollCap"/> times, then falls back to
        /// all-open (provably valid). The result carries which attempt won and the verdict.
        /// </summary>
        public static TopoResult Resolve(TopoGraph graph, int seed, int rerollCap = DefaultRerollCap)
        {
            for (int attempt = 0; attempt <= rerollCap; attempt++)
            {
                HashSet<string> open = RollOpenEdges(graph, TopoSeed(seed, attempt));
                Verdict v = Validate(graph, open);
                if (v.Ok) return new TopoResult(open, attempt, false, v);
            }

            // Fallback: every edge open. Valid by construction (backbone alone passes).
            var all = new HashSet<string>();
            foreach (Edge e in graph.Edges) all.Add(e.Id);
            return new TopoResult(all, rerollCap + 1, true, Validate(graph, all));
        }

        /// <summary>
        /// Deterministically decide which edges are open for a given (already-combined) seed.
        /// FixedOpen edges are always open and never consume the RNG; toggleable edges roll
        /// in stable id order so every peer agrees.
        /// </summary>
        public static HashSet<string> RollOpenEdges(TopoGraph graph, int topoSeed)
        {
            var ordered = new List<Edge>(graph.Edges);
            ordered.Sort((x, y) => string.CompareOrdinal(x.Id, y.Id));

            var open = new HashSet<string>();
            var rng = new System.Random(topoSeed);
            foreach (Edge e in ordered)
            {
                if (e.Toggleable && !e.FixedOpen)
                {
                    if (rng.Next(0, 1000) < e.OpenChancePermille) open.Add(e.Id);
                }
                else
                {
                    open.Add(e.Id); // FixedOpen (or, defensively, anything not toggleable)
                }
            }
            return open;
        }

        /// <summary>
        /// Check every connectivity invariant against an open-edge set. Returns the first
        /// failure (with its invariant id) or <see cref="Verdict.Pass"/>.
        /// </summary>
        public static Verdict Validate(TopoGraph g, HashSet<string> open)
        {
            HashSet<string> reachAll = Reach(g, open, g.Van, floor1Only: false);

            // I1 — objective reachable from the van.
            if (!string.IsNullOrEmpty(g.Objective) && !reachAll.Contains(g.Objective))
                return Verdict.Fail("I1", "objective unreachable from van");

            // I2 — can carry the objective back to the van.
            if (!string.IsNullOrEmpty(g.Objective))
            {
                HashSet<string> fromObjective = Reach(g, open, g.Objective, floor1Only: false);
                if (!fromObjective.Contains(g.Van))
                    return Verdict.Fail("I2", "cannot return from objective to van");
            }

            HashSet<string> reachF1 = Reach(g, open, g.Van, floor1Only: true);

            // I3 — power gate reachable before crossing to floor 2.
            if (!string.IsNullOrEmpty(g.PowerGate) && !reachF1.Contains(g.PowerGate))
                return Verdict.Fail("I3", "power gate not reachable before a stair");

            // I4 — the power clue reachable before a stair.
            if (!string.IsNullOrEmpty(g.PowerClue) && !reachF1.Contains(g.PowerClue))
                return Verdict.Fail("I4", "power clue not reachable before a stair");

            // I5 — at least one consumable room reachable before a stair.
            if (g.ConsumableRooms.Count > 0)
            {
                bool any = false;
                foreach (string c in g.ConsumableRooms)
                    if (reachF1.Contains(c)) { any = true; break; }
                if (!any) return Verdict.Fail("I5", "no consumable room reachable before a stair");
            }

            // I6 — the fire exit is reachable.
            if (!string.IsNullOrEmpty(g.FireExit) && !reachAll.Contains(g.FireExit))
                return Verdict.Fail("I6", "fire exit unreachable");

            // I7 — at least two descents open (no single campable chokepoint).
            // Only enforced when the map declares descents (a single-floor graph declares none).
            if (g.DescentEdgeIds.Count > 0)
            {
                int descents = 0;
                foreach (string id in g.DescentEdgeIds)
                    if (open.Contains(id)) descents++;
                if (descents < 2)
                    return Verdict.Fail("I7", $"only {descents} descent(s) open, need >= 2");
            }

            // I8 — every room reachable (no island rooms / dead pockets).
            foreach (string room in g.RoomNodes)
                if (!reachAll.Contains(room))
                    return Verdict.Fail("I8", $"island room: {room}");

            // I9 — shaft fall-gaps are never modeled as edges, so falling is never mandatory by construction.
            return Verdict.Pass();
        }

        /// <summary>
        /// Forward BFS over open edges. Respects one-way edges. When
        /// <paramref name="floor1Only"/> is true, only floor-1 nodes/edges are traversed
        /// (used to test "reachable before any stair").
        /// </summary>
        static HashSet<string> Reach(TopoGraph g, HashSet<string> open, string start, bool floor1Only)
        {
            var adj = new Dictionary<string, List<string>>();
            void Link(string a, string b)
            {
                if (!adj.TryGetValue(a, out List<string> list)) { list = new List<string>(); adj[a] = list; }
                list.Add(b);
            }

            foreach (Edge e in g.Edges)
            {
                if (!open.Contains(e.Id)) continue;
                if (floor1Only && (g.FloorOf(e.A) != 1 || g.FloorOf(e.B) != 1)) continue;
                Link(e.A, e.B);
                if (!e.OneWay) Link(e.B, e.A);
            }

            var seen = new HashSet<string>();
            if (string.IsNullOrEmpty(start)) return seen;
            seen.Add(start);
            var queue = new Queue<string>();
            queue.Enqueue(start);
            while (queue.Count > 0)
            {
                string n = queue.Dequeue();
                if (!adj.TryGetValue(n, out List<string> neighbours)) continue;
                foreach (string m in neighbours)
                    if (seen.Add(m)) queue.Enqueue(m);
            }
            return seen;
        }

        /// <summary>Deterministically combine a base seed with a re-roll attempt index.</summary>
        static int TopoSeed(int seed, int attempt)
        {
            unchecked
            {
                int h = seed * 73856093;
                h ^= (attempt + 1) * 19349663;
                return h;
            }
        }
    }
}
