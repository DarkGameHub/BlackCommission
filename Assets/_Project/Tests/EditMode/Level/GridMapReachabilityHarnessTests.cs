using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using BlackCommission.Level.Generation;

namespace BlackCommission.Tests.Level
{
    /// <summary>
    /// ADR-0003 Phase-5 validation harness for the indoor grid generator. Where
    /// <see cref="GridMapGeneratorTests"/> proves the core properties on a handful of seeds, this sweeps
    /// 1000 seeds to satisfy the ADR's explicit validation criterion — "every seed → objective + exits
    /// reachable (0 unreachable; fallback count tracked)" — plus byte-identical determinism (the co-op
    /// sync guarantee) and the degenerate anchor configurations. Pure logic: no Unity objects, no assets,
    /// no play mode → runs headless / in CI.
    /// </summary>
    public class GridMapReachabilityHarnessTests
    {
        const int Seeds = 1000;   // ADR-0003 validation criterion: 1000-seed harness.
        const int W = 14, H = 10; // matches the editor "Preview Grid Layout" footprint (GridLayoutPreview).

        // The shipping tower's three hero anchors (Entry / Mid / Deep) at fixed grid zones.
        static IReadOnlyList<GridMapGenerator.AnchorSpec> HeroAnchors() => new List<GridMapGenerator.AnchorSpec>
        {
            new GridMapGenerator.AnchorSpec("ENTRY", new GridCoord(1, 1)),
            new GridMapGenerator.AnchorSpec("MID",   new GridCoord(7, 5)),
            new GridMapGenerator.AnchorSpec("DEEP",  new GridCoord(12, 8)),
        };

        [Test]
        public void Every_anchor_reachable_across_1000_seeds()
        {
            var anchors = HeroAnchors();
            var failures = new List<int>();
            for (int seed = 1; seed <= Seeds; seed++)
            {
                var layout = GridMapGenerator.Generate(W, H, anchors, seed);
                if (!GridMapGenerator.AllAnchorsReachable(layout, anchors))
                    failures.Add(seed);
            }
            Assert.IsEmpty(failures,
                $"{failures.Count}/{Seeds} seeds left an anchor unreachable: {Preview(failures)}");
        }

        [Test]
        public void Layout_is_byte_identical_per_seed_across_1000_seeds()
        {
            // Co-op sync depends on every peer rebuilding the identical layout from the one shared int seed.
            var anchors = HeroAnchors();
            var mismatches = new List<int>();
            for (int seed = 1; seed <= Seeds; seed++)
            {
                var a = GridMapGenerator.Generate(W, H, anchors, seed);
                var b = GridMapGenerator.Generate(W, H, anchors, seed);
                if (Signature(a) != Signature(b)) mismatches.Add(seed);
            }
            Assert.IsEmpty(mismatches, $"non-deterministic layout for seeds: {Preview(mismatches)}");
        }

        [Test]
        public void Safety_net_fallback_is_rarely_needed_across_1000_seeds()
        {
            // The primary anchor-to-anchor stitch should connect everything on its own; the L-path safety
            // net is a backstop, not a crutch. ADR-0003 wants the "fallback count tracked".
            var anchors = HeroAnchors();
            int fallbacks = 0, unreachable = 0;
            for (int seed = 1; seed <= Seeds; seed++)
            {
                GridMapGenerator.Generate(W, H, anchors, seed, out var report);
                if (report.SafetyNetFired) fallbacks++;
                if (!report.Reachable) unreachable++;
            }
            TestContext.WriteLine($"safety-net fallbacks: {fallbacks}/{Seeds}; unreachable: {unreachable}/{Seeds}");
            Assert.AreEqual(0, unreachable, "a seed ended unreachable even after the safety net");
            Assert.LessOrEqual(fallbacks, Seeds / 100,
                $"safety net fired on {fallbacks}/{Seeds} seeds — the primary stitcher is too weak");
        }

        [Test]
        public void Seeds_produce_many_distinct_layouts()
        {
            // Per-run replayability is the point of the generator: distinct routes across seeds. The bound
            // is deliberately conservative (a quarter unique) to be a non-flaky regression guard; the actual
            // distinct count is logged so it can be tightened once measured on a real run.
            var anchors = HeroAnchors();
            var signatures = new HashSet<string>();
            for (int seed = 1; seed <= Seeds; seed++)
                signatures.Add(Signature(GridMapGenerator.Generate(W, H, anchors, seed)));
            TestContext.WriteLine($"distinct layouts: {signatures.Count}/{Seeds}");
            Assert.Greater(signatures.Count, Seeds / 4,
                $"only {signatures.Count} distinct layouts from {Seeds} seeds — generator is too repetitive");
        }

        // ---- degenerate / edge anchor configurations -------------------------------------------------

        [Test]
        public void Single_anchor_is_trivially_reachable()
        {
            var anchors = new List<GridMapGenerator.AnchorSpec>
                { new GridMapGenerator.AnchorSpec("ONLY", new GridCoord(3, 3)) };
            var layout = GridMapGenerator.Generate(W, H, anchors, 42);
            Assert.IsTrue(GridMapGenerator.AllAnchorsReachable(layout, anchors));
        }

        [Test]
        public void Coincident_anchors_are_reachable_and_do_not_throw()
        {
            var cell = new GridCoord(5, 5);
            var anchors = new List<GridMapGenerator.AnchorSpec>
            {
                new GridMapGenerator.AnchorSpec("A", cell),
                new GridMapGenerator.AnchorSpec("B", cell),
            };
            GridLayout layout = null;
            Assert.DoesNotThrow(() => layout = GridMapGenerator.Generate(W, H, anchors, 9));
            Assert.IsTrue(GridMapGenerator.AllAnchorsReachable(layout, anchors));
        }

        [Test]
        public void Adjacent_anchors_link_directly()
        {
            var a = new GridCoord(4, 4);
            var b = new GridCoord(5, 4);
            var anchors = new List<GridMapGenerator.AnchorSpec>
            {
                new GridMapGenerator.AnchorSpec("A", a),
                new GridMapGenerator.AnchorSpec("B", b),
            };
            var layout = GridMapGenerator.Generate(W, H, anchors, 3);
            Assert.IsTrue(layout.Linked(a, b), "directly adjacent anchors should share a link");
            Assert.IsTrue(GridMapGenerator.AllAnchorsReachable(layout, anchors));
        }

        [Test]
        public void Collinear_anchors_are_reachable_across_seeds()
        {
            var anchors = new List<GridMapGenerator.AnchorSpec>
            {
                new GridMapGenerator.AnchorSpec("A", new GridCoord(1, 4)),
                new GridMapGenerator.AnchorSpec("B", new GridCoord(6, 4)),
                new GridMapGenerator.AnchorSpec("C", new GridCoord(12, 4)),
            };
            for (int seed = 1; seed <= 50; seed++)
            {
                var layout = GridMapGenerator.Generate(W, H, anchors, seed);
                Assert.IsTrue(GridMapGenerator.AllAnchorsReachable(layout, anchors), $"seed {seed}");
            }
        }

        [Test]
        public void Tiny_grid_still_connects_anchors()
        {
            var anchors = new List<GridMapGenerator.AnchorSpec>
            {
                new GridMapGenerator.AnchorSpec("A", new GridCoord(0, 0)),
                new GridMapGenerator.AnchorSpec("B", new GridCoord(2, 2)),
            };
            var layout = GridMapGenerator.Generate(3, 3, anchors, 11);
            Assert.IsTrue(GridMapGenerator.AllAnchorsReachable(layout, anchors));
        }

        [Test]
        public void Out_of_bounds_anchor_degrades_gracefully()
        {
            // Misconfiguration robustness: an anchor outside the grid must not crash generation (it simply
            // cannot be made reachable). Hero anchors are authored in-bounds by design — this only proves
            // the generator does not throw on a bad config.
            var anchors = new List<GridMapGenerator.AnchorSpec>
            {
                new GridMapGenerator.AnchorSpec("IN",  new GridCoord(1, 1)),
                new GridMapGenerator.AnchorSpec("OOB", new GridCoord(99, 99)),
            };
            Assert.DoesNotThrow(() => GridMapGenerator.Generate(W, H, anchors, 5));
        }

        // ---- helpers ---------------------------------------------------------------------------------

        // Order-stable string encoding of a layout's cell kinds + forward links. Two layouts compare equal
        // iff identical. Delimited so multi-digit coordinates can't alias into a different layout.
        static string Signature(GridLayout layout)
        {
            var sb = new StringBuilder();
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                {
                    var c = new GridCoord(x, y);
                    sb.Append((int)layout.Kind(c)).Append(':');
                    foreach (var d in new[] { new GridCoord(1, 0), new GridCoord(0, 1) })
                    {
                        var n = c + d;
                        if (layout.InBounds(n) && layout.Linked(c, n))
                            sb.Append('|').Append(x).Append(',').Append(y).Append('>').Append(n.X).Append(',').Append(n.Y);
                    }
                    sb.Append(';');
                }
            return sb.ToString();
        }

        static string Preview(List<int> seeds)
        {
            if (seeds.Count == 0) return "(none)";
            int n = System.Math.Min(seeds.Count, 10);
            return string.Join(",", seeds.GetRange(0, n)) + (seeds.Count > n ? ",…" : "");
        }
    }
}
