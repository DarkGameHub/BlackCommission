using System.Collections.Generic;

namespace BlackCommission.Level.Generation
{
    /// <summary>
    /// Deterministic, seed-driven grid map generator (ADR-0003, constrained model). Places fixed
    /// hero anchors, stitches connecting corridors between them, adds a few seeded branch dead-ends,
    /// and guarantees every anchor is reachable. Same (width, height, anchors, seed) ⇒ identical
    /// layout on every machine, so only the int seed need cross the network (ADR-0001).
    ///
    /// Pure logic: no UnityEngine, no Time, no UnityEngine.Random, no hash-set iteration in the
    /// generation path — ALL randomness comes from one seeded System.Random and iteration order is
    /// explicit/stable (registry forbidden pattern: nondeterministic_generation_input).
    /// </summary>
    public static class GridMapGenerator
    {
        /// <summary>A fixed hero anchor occupying a single cell (Entry / Mid / Deep / objective).</summary>
        public sealed class AnchorSpec
        {
            public readonly string Id;
            public readonly GridCoord Cell;
            public AnchorSpec(string id, GridCoord cell) { Id = id; Cell = cell; }
        }

        /// <summary>Diagnostics from one <see cref="Generate"/> run, for the ADR-0003 validation harness.</summary>
        public struct GenerationReport
        {
            /// <summary>True if the primary anchor-to-anchor stitch left an anchor unreachable and the
            /// guaranteed L-path safety net had to run. Expected to be rare → a high rate means the
            /// primary stitcher is too weak.</summary>
            public bool SafetyNetFired;
            /// <summary>True if every anchor is reachable in the final layout (must always hold).</summary>
            public bool Reachable;
            /// <summary>Corridor cells carved — layout "texture" / size metric.</summary>
            public int CorridorCells;
            /// <summary>Undirected walkable links in the final layout.</summary>
            public int LinkCount;
        }

        static readonly GridCoord[] Dirs =
        {
            new GridCoord(0, 1), new GridCoord(1, 0), new GridCoord(0, -1), new GridCoord(-1, 0)
        };

        /// <summary>
        /// Generate a layout. Anchors are placed in the given order; consecutive anchors are
        /// connected (Entry → … → Deep). <paramref name="branches"/> seeded dead-end corridors add
        /// exploration. Reachability is guaranteed by construction and re-verified.
        /// </summary>
        public static GridLayout Generate(int width, int height,
            IReadOnlyList<AnchorSpec> anchors, int seed, int branches = 4, int loops = 0, int roomRadius = 0)
            => Generate(width, height, anchors, seed, out _, branches, loops, roomRadius);

        /// <summary>
        /// Generation overload that also returns a <see cref="GenerationReport"/> (corridor / link counts,
        /// whether the reachability safety net had to fire, final reachability). Used by the ADR-0003
        /// 1000-seed validation harness to track the fallback rate. The layout produced is byte-identical
        /// to the report-less overload — the report only observes, it never changes an RNG draw.
        /// </summary>
        public static GridLayout Generate(int width, int height,
            IReadOnlyList<AnchorSpec> anchors, int seed, out GenerationReport report,
            int branches = 4, int loops = 0, int roomRadius = 0)
        {
            report = default;
            var layout = new GridLayout(width, height);
            if (anchors == null || anchors.Count == 0) { report.Reachable = true; return layout; }

            var rng = new System.Random(seed);

            // 1. Place anchors (deterministic = given order).
            foreach (var a in anchors)
                if (layout.InBounds(a.Cell)) layout.Set(a.Cell, CellKind.Anchor, a.Id);

            // 2. Connect consecutive anchors with seeded randomized-Manhattan corridors.
            for (int i = 0; i + 1 < anchors.Count; i++)
                CarvePath(layout, anchors[i].Cell, anchors[i + 1].Cell, rng);

            // 3. Seeded branch dead-ends off existing corridors (exploration / texture).
            AddBranches(layout, rng, branches);

            // 3b. Loops: link adjacent corridors that aren't yet connected → cycles → an INTERCONNECTED maze
            //     with multiple routes (vs. the single-path tree AddBranches alone produces).
            AddLoops(layout, rng, loops);

            // 3c. Rooms: expand each anchor into an open multi-cell room (LC-style varied open spaces, not
            //     just 1-wide corridors). Done last so rooms absorb/connect the corridors already laid.
            if (roomRadius > 0) ExpandRooms(layout, anchors, rng, roomRadius);

            // 4. Safety net: if any anchor is somehow unreachable, carve guaranteed L-paths.
            //    Record whether it fired BEFORE it mutates the layout (AllAnchorsReachable is a pure read,
            //    consumes no RNG, so observing it keeps the layout byte-identical to the report-less path).
            bool reachableBeforeSafetyNet = AllAnchorsReachable(layout, anchors);
            if (!reachableBeforeSafetyNet)
                for (int i = 0; i + 1 < anchors.Count; i++)
                    CarveLPath(layout, anchors[i].Cell, anchors[i + 1].Cell);

            report.SafetyNetFired = !reachableBeforeSafetyNet;
            report.Reachable = reachableBeforeSafetyNet || AllAnchorsReachable(layout, anchors);
            report.CorridorCells = CountKind(layout, CellKind.Corridor);
            report.LinkCount = layout.LinkCount;
            return layout;
        }

        // Walk from -> to, always reducing Manhattan distance (so it always arrives), linking each
        // step. A 1-in-4 seeded detour swaps which axis advances first, for route variation.
        static void CarvePath(GridLayout layout, GridCoord from, GridCoord to, System.Random rng)
        {
            GridCoord cur = from;
            int guard = (layout.Width + layout.Height) * 4;
            while (!cur.Equals(to) && guard-- > 0)
            {
                GridCoord next = StepToward(cur, to, rng);
                if (!layout.InBounds(next)) break;
                if (layout.Kind(next) == CellKind.Empty) layout.Set(next, CellKind.Corridor);
                layout.Link(cur, next);
                cur = next;
            }
            if (!cur.Equals(to)) CarveLPath(layout, cur, to); // guarantee the connection
        }

        static GridCoord StepToward(GridCoord cur, GridCoord to, System.Random rng)
        {
            int dx = to.X - cur.X, dy = to.Y - cur.Y;
            GridCoord stepX = new GridCoord(cur.X + System.Math.Sign(dx), cur.Y);
            GridCoord stepY = new GridCoord(cur.X, cur.Y + System.Math.Sign(dy));

            bool xPrimary = System.Math.Abs(dx) >= System.Math.Abs(dy);
            if (rng.Next(4) == 0) xPrimary = !xPrimary; // seeded detour (consumes RNG every step → deterministic)

            if (dx != 0 && dy != 0) return xPrimary ? stepX : stepY;
            if (dx != 0) return stepX;
            if (dy != 0) return stepY;
            return cur;
        }

        static void CarveLPath(GridLayout layout, GridCoord from, GridCoord to)
        {
            GridCoord cur = from;
            while (cur.X != to.X)
            {
                var n = new GridCoord(cur.X + System.Math.Sign(to.X - cur.X), cur.Y);
                Carve(layout, cur, n); cur = n;
            }
            while (cur.Y != to.Y)
            {
                var n = new GridCoord(cur.X, cur.Y + System.Math.Sign(to.Y - cur.Y));
                Carve(layout, cur, n); cur = n;
            }
        }

        static void Carve(GridLayout layout, GridCoord a, GridCoord b)
        {
            if (layout.InBounds(b) && layout.Kind(b) == CellKind.Empty) layout.Set(b, CellKind.Corridor);
            layout.Link(a, b);
        }

        static void AddBranches(GridLayout layout, System.Random rng, int count)
        {
            // Gather corridor cells in a stable (x, y) order so the seeded picks are deterministic.
            var corridors = new List<GridCoord>();
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                {
                    var c = new GridCoord(x, y);
                    if (layout.Kind(c) == CellKind.Corridor) corridors.Add(c);
                }
            if (corridors.Count == 0) return;

            for (int b = 0; b < count; b++)
            {
                GridCoord cur = corridors[rng.Next(corridors.Count)];
                int len = 1 + rng.Next(3);
                for (int s = 0; s < len; s++)
                {
                    GridCoord next = cur + Dirs[rng.Next(Dirs.Length)];
                    if (!layout.InBounds(next) || layout.Kind(next) != CellKind.Empty) break;
                    layout.Set(next, CellKind.Corridor);
                    layout.Link(cur, next);
                    cur = next;
                }
            }
        }

        // Link adjacent corridor/anchor cells that are NOT yet connected → cycles. The inverse of
        // AddBranches (which stops on collision); this is what turns the single-path tree into a looped,
        // multi-route maze. Stable iteration order + the shared seeded RNG keep it deterministic.
        static void AddLoops(GridLayout layout, System.Random rng, int count)
        {
            if (count <= 0) return;

            var cells = new List<GridCoord>();
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                {
                    var k = layout.Kind(new GridCoord(x, y));
                    if (k == CellKind.Corridor || k == CellKind.Anchor) cells.Add(new GridCoord(x, y));
                }
            if (cells.Count == 0) return;

            int added = 0, attempts = 0, cap = count * 30;
            while (added < count && attempts++ < cap)
            {
                GridCoord c = cells[rng.Next(cells.Count)];
                GridCoord n = c + Dirs[rng.Next(Dirs.Length)];
                if (!layout.InBounds(n) || layout.Linked(c, n)) continue;
                CellKind nk = layout.Kind(n);
                if (nk == CellKind.Corridor || nk == CellKind.Anchor)
                {
                    layout.Link(c, n);
                    added++;
                }
            }
        }

        // The three LOCKED room-size classes — identical to the tower system (TowerPlanV8 / PlanSize):
        // S = 4×4 m, M = 8×8 m, L = 12×8 m. On the 4 m grid that is 1×1, 2×2 and 3×2 cells. Rooms are placed
        // ONLY at these footprints (PM rule 2026-06-20: room sizes = these 3 classes only — no free-form blocks).
        static readonly (int W, int H)[] RoomFootprints = { (1, 1), (2, 2), (3, 2) }; // S, M, L (cells)

        // Expand each anchor into an OPEN room whose footprint is one of the 3 LOCKED size classes (hero anchors —
        // first = ENTRY, last = DEEP/objective — are always L; the rest pick a seeded class + orientation). All
        // internal adjacencies are linked (open interior — the builder skips walls between same-owner cells) and
        // every perimeter cell links to any adjacent corridor (a door). The anchor cell is guaranteed to belong to
        // its own room and another anchor's room is never overwritten. <paramref name="radius"/> is unused — the
        // caller's roomRadius only gates rooms on/off (in Generate); the footprints are fixed.
        static void ExpandRooms(GridLayout layout, IReadOnlyList<AnchorSpec> anchors, System.Random rng, int radius)
        {
            for (int ai = 0; ai < anchors.Count; ai++)
            {
                var a = anchors[ai];
                if (!layout.InBounds(a.Cell)) continue;

                bool hero = ai == 0 || ai == anchors.Count - 1;        // ENTRY / DEEP → always Large
                var fp = hero ? RoomFootprints[2] : RoomFootprints[rng.Next(RoomFootprints.Length)];
                int sw = fp.W, sh = fp.H;
                if (rng.Next(2) == 0) { int t = sw; sw = sh; sh = t; } // seeded orientation (matters for L: 3×2/2×3)

                layout.Set(a.Cell, CellKind.Room, a.Id);               // anchor cell always belongs to its own room
                if (sw > layout.Width || sh > layout.Height) continue; // footprint can't fit (degenerate grid)

                // Place so the anchor sits inside the footprint; clamp so the block stays fully on the grid.
                int minX = Clamp(a.Cell.X - sw / 2, 0, layout.Width - sw);
                int minY = Clamp(a.Cell.Y - sh / 2, 0, layout.Height - sh);

                for (int x = minX; x < minX + sw; x++)
                    for (int y = minY; y < minY + sh; y++)
                    {
                        var c = new GridCoord(x, y);
                        if (!layout.InBounds(c)) continue;
                        var owner = layout.Owner(c);
                        if (owner != null && owner != a.Id) continue;  // never steal another anchor's room cell
                        layout.Set(c, CellKind.Room, a.Id);
                    }

                for (int x = minX; x < minX + sw; x++)
                    for (int y = minY; y < minY + sh; y++)
                    {
                        var c = new GridCoord(x, y);
                        if (!layout.InBounds(c) || layout.Owner(c) != a.Id) continue;
                        foreach (var d in Dirs)
                        {
                            var n = c + d;
                            if (!layout.InBounds(n)) continue;
                            if (layout.Owner(n) == a.Id || layout.Kind(n) == CellKind.Corridor)
                                layout.Link(c, n); // open interior, or a door into an adjacent corridor
                        }
                    }
            }
        }

        static int Clamp(int v, int lo, int hi) => v < lo ? lo : (v > hi ? hi : v);

        static int CountKind(GridLayout layout, CellKind kind)
        {
            int n = 0;
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                    if (layout.Kind(new GridCoord(x, y)) == kind) n++;
            return n;
        }

        /// <summary>BFS over the corridor link graph from the first anchor; true if every anchor is reached.</summary>
        public static bool AllAnchorsReachable(GridLayout layout, IReadOnlyList<AnchorSpec> anchors)
        {
            if (anchors == null || anchors.Count == 0) return true;

            var seen = new HashSet<GridCoord>();
            var queue = new Queue<GridCoord>();
            seen.Add(anchors[0].Cell);
            queue.Enqueue(anchors[0].Cell);
            while (queue.Count > 0)
            {
                GridCoord c = queue.Dequeue();
                foreach (var d in Dirs)
                {
                    GridCoord n = c + d;
                    if (layout.InBounds(n) && !seen.Contains(n) && layout.Linked(c, n))
                    {
                        seen.Add(n);
                        queue.Enqueue(n);
                    }
                }
            }
            foreach (var a in anchors)
                if (!seen.Contains(a.Cell)) return false;
            return true;
        }
    }
}
