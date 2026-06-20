using System.Collections.Generic;
using NUnit.Framework;
using BlackCommission.Level.Generation;

namespace BlackCommission.Tests.Level
{
    /// <summary>
    /// Pure-logic tests for the ADR-0003 indoor generator core. No Unity objects, no assets, no
    /// play mode — runs headless / in CI. Determinism and reachability are the make-or-break
    /// properties (host/client co-op sync + solvable maps); variation proves the point of it.
    /// </summary>
    public class GridMapGeneratorTests
    {
        const int W = 12, H = 10;

        static IReadOnlyList<GridMapGenerator.AnchorSpec> Anchors() => new List<GridMapGenerator.AnchorSpec>
        {
            new GridMapGenerator.AnchorSpec("ENTRY", new GridCoord(1, 1)),
            new GridMapGenerator.AnchorSpec("MID",   new GridCoord(6, 5)),
            new GridMapGenerator.AnchorSpec("DEEP",  new GridCoord(10, 8)),
        };

        [Test]
        public void Generate_is_deterministic_for_a_seed()
        {
            var anchors = Anchors();
            var a = GridMapGenerator.Generate(W, H, anchors, 1234);
            var b = GridMapGenerator.Generate(W, H, anchors, 1234);

            Assert.AreEqual(a.LinkCount, b.LinkCount, "link counts differ");
            for (int x = 0; x < W; x++)
                for (int y = 0; y < H; y++)
                {
                    var c = new GridCoord(x, y);
                    Assert.AreEqual(a.Kind(c), b.Kind(c), $"cell kind differs at {c}");
                    Assert.AreEqual(a.Owner(c), b.Owner(c), $"cell owner differs at {c}");
                    // Check the two unique forward edges per cell (covers every undirected link once).
                    foreach (var d in new[] { new GridCoord(1, 0), new GridCoord(0, 1) })
                    {
                        var n = c + d;
                        if (a.InBounds(n))
                            Assert.AreEqual(a.Linked(c, n), b.Linked(c, n), $"link differs {c}-{n}");
                    }
                }
        }

        [Test]
        public void Every_anchor_is_reachable_across_many_seeds()
        {
            var anchors = Anchors();
            for (int seed = 1; seed <= 30; seed++)
            {
                var layout = GridMapGenerator.Generate(W, H, anchors, seed);
                Assert.IsTrue(GridMapGenerator.AllAnchorsReachable(layout, anchors),
                    $"an anchor was unreachable for seed {seed}");
            }
        }

        [Test]
        public void Anchors_are_placed_at_their_cells()
        {
            var anchors = Anchors();
            var layout = GridMapGenerator.Generate(W, H, anchors, 7);
            foreach (var a in anchors)
            {
                Assert.AreEqual(CellKind.Anchor, layout.Kind(a.Cell), $"{a.Id} is not an Anchor cell");
                Assert.AreEqual(a.Id, layout.Owner(a.Cell), $"{a.Id} owner wrong");
            }
        }

        [Test]
        public void Different_seeds_produce_varied_layouts()
        {
            var anchors = Anchors();
            var signatures = new HashSet<string>();
            for (int seed = 1; seed <= 16; seed++)
                signatures.Add(Signature(GridMapGenerator.Generate(W, H, anchors, seed)));
            Assert.GreaterOrEqual(signatures.Count, 2, "expected route variation across seeds");
        }

        static string Signature(GridLayout layout)
        {
            var sb = new System.Text.StringBuilder();
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                    if (layout.Kind(new GridCoord(x, y)) == CellKind.Corridor)
                        sb.Append(x).Append(',').Append(y).Append(';');
            return sb.ToString();
        }
    }
}
