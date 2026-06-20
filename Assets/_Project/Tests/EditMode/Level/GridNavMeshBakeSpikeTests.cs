using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using BlackCommission.Level.Generation;
// UnityEngine also defines a GridLayout (2D tilemap); alias to the generation type to avoid CS0104.
using GridLayout = BlackCommission.Level.Generation.GridLayout;

namespace BlackCommission.Tests.Level
{
    /// <summary>
    /// ADR-0003 Phase-3 NavMesh spike (runtime-bake approach). Proves that a generated grid layout, floored
    /// with one walkable tile per occupied cell, bakes via the runtime NavMesh API into a SINGLE connected
    /// navmesh on which an agent can path ENTRY → DEEP — i.e. a host-side runtime bake at mission load is
    /// viable, and cheap enough to hide behind the van-transit/load screen (ADR-0003 §Performance).
    ///
    /// Uses <see cref="NavMeshBuilder"/> (UnityEngine.AIModule) with explicit box sources, so it needs no
    /// scene objects and no Unity.AI.Navigation package reference. This deliberately isolates the navmesh
    /// APPROACH question (does a runtime bake over the generated cell grid connect Entry→Deep?) from
    /// real-prefab fidelity (whether the actual Corridor_*/Junction_* meshes bake cleanly), which the PM
    /// verifies visually with the editor baker. Edit-time bake + query is the same path TowerNavBaker uses.
    /// </summary>
    public class GridNavMeshBakeSpikeTests
    {
        const int W = 14, H = 10;
        const float Cell = 4f; // GridLayoutInstantiator.CellSize

        static readonly GridMapGenerator.AnchorSpec Entry = new GridMapGenerator.AnchorSpec("ENTRY", new GridCoord(1, 1));
        static readonly GridMapGenerator.AnchorSpec Mid   = new GridMapGenerator.AnchorSpec("MID",   new GridCoord(7, 5));
        static readonly GridMapGenerator.AnchorSpec Deep  = new GridMapGenerator.AnchorSpec("DEEP",  new GridCoord(12, 8));

        static IReadOnlyList<GridMapGenerator.AnchorSpec> Anchors() =>
            new List<GridMapGenerator.AnchorSpec> { Entry, Mid, Deep };

        readonly List<NavMeshDataInstance> _instances = new List<NavMeshDataInstance>();

        [TearDown]
        public void TearDown()
        {
            foreach (var inst in _instances)
                if (inst.valid) inst.Remove();
            _instances.Clear();
        }

        [Test]
        public void Generated_layout_bakes_a_navmesh_that_paths_entry_to_deep()
        {
            const int seed = 12345;
            GridLayout layout = GridMapGenerator.Generate(W, H, Anchors(), seed);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var (inst, tris) = BakeFloors(layout);
            sw.Stop();
            _instances.Add(inst);

            TestContext.WriteLine($"seed {seed}: baked {tris} navmesh tris in {sw.Elapsed.TotalMilliseconds:0.0} ms");
            Assert.Greater(tris, 0, "runtime bake produced no walkable navmesh over the generated floors");

            Vector3 from = OnMesh(World(Entry.Cell));
            Vector3 to   = OnMesh(World(Deep.Cell));
            var path = new NavMeshPath();
            bool ok = NavMesh.CalculatePath(from, to, NavMesh.AllAreas, path);

            Assert.IsTrue(ok && path.status == NavMeshPathStatus.PathComplete,
                $"no complete navmesh path ENTRY→DEEP (ok={ok}, status={path.status})");
        }

        [Test]
        public void Bake_time_stays_within_a_load_budget_across_several_seeds()
        {
            // The bake is a one-time mission-load cost hidden behind the van-transit screen; it must be cheap.
            double worst = 0;
            for (int seed = 1; seed <= 8; seed++)
            {
                var layout = GridMapGenerator.Generate(W, H, Anchors(), seed);
                var sw = System.Diagnostics.Stopwatch.StartNew();
                var (inst, tris) = BakeFloors(layout);
                sw.Stop();
                worst = System.Math.Max(worst, sw.Elapsed.TotalMilliseconds);
                Assert.Greater(tris, 0, $"seed {seed} baked nothing");
                if (inst.valid) inst.Remove(); // immediate cleanup → no overlapping navmesh between seeds
            }
            TestContext.WriteLine($"worst single-grid bake over 8 seeds: {worst:0.0} ms");
            Assert.Less(worst, 2000.0, "a single 14×10 grid bake should be well under 2 s (load-screen budget)");
        }

        // Floor every occupied cell with a walkable box source, bake one connected navmesh, return its tri count.
        (NavMeshDataInstance instance, int tris) BakeFloors(GridLayout layout)
        {
            var sources = new List<NavMeshBuildSource>();
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                {
                    var c = new GridCoord(x, y);
                    if (layout.Kind(c) == CellKind.Empty) continue;
                    sources.Add(new NavMeshBuildSource
                    {
                        shape = NavMeshBuildSourceShape.Box,
                        // floor slab just below y=0 so the walkable surface sits at y=0; +0.2 so touching cells fuse.
                        transform = Matrix4x4.TRS(World(c) + Vector3.down * 0.05f, Quaternion.identity, Vector3.one),
                        size = new Vector3(Cell + 0.2f, 0.1f, Cell + 0.2f),
                        area = 0 // walkable
                    });
                }

            NavMeshBuildSettings settings = NavMesh.GetSettingsByID(0); // default Humanoid agent
            var bounds = new Bounds(
                new Vector3(layout.Width * Cell * 0.5f, 0f, layout.Height * Cell * 0.5f),
                new Vector3(layout.Width * Cell + 8f, 4f, layout.Height * Cell + 8f));

            NavMeshData data = NavMeshBuilder.BuildNavMeshData(
                settings, sources, bounds, Vector3.zero, Quaternion.identity);
            NavMeshDataInstance inst = NavMesh.AddNavMeshData(data);
            var tri = NavMesh.CalculateTriangulation();
            return (inst, tri.indices.Length / 3);
        }

        static Vector3 World(GridCoord c) => new Vector3(c.X * Cell, 0f, c.Y * Cell);

        static Vector3 OnMesh(Vector3 p) =>
            NavMesh.SamplePosition(p, out var hit, 5f, NavMesh.AllAreas) ? hit.position : p;
    }
}
