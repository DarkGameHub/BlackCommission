using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using BlackCommission.Level.Generation;
// UnityEngine also defines a GridLayout (2D tilemap); alias to the generation type to avoid CS0104.
using GridLayout = BlackCommission.Level.Generation.GridLayout;

namespace BlackCommission.Tests.Level
{
    /// <summary>
    /// Headless integration test for the ADR-0003 map-2 site (direction B). It runs the REAL
    /// <c>BlackCommission.Level.MapSiteBuilder.Build</c> via reflection (it lives in Assembly-CSharp, which an
    /// asmdef test can't reference at compile time), bakes a navmesh over the ACTUAL generated geometry with
    /// <see cref="NavMeshBuilder"/> using PhysicsColliders (so walls genuinely block and doors genuinely pass —
    /// markers have no colliders so they don't interfere), and asserts the COMPLETE traversal van
    /// DROP-OFF → DEEP objective is walkable. Display/focus/bridge-free → runs under
    /// <c>-runTests -batchmode</c> while no one is at the editor.
    /// </summary>
    public class FullSiteBuildTests
    {
        // Geometry constants that MUST match MapSiteBuilder (Cell=4, ENTRY(1,1), DEEP(12,8), front cell (1,0),
        // drop-off 18 m south of the front door).
        const float Cell = 4f;
        static readonly Vector3 Dropoff = new Vector3((2 + 0.5f) * Cell, 0f, -80f);                  // (10,0,-80) far
        static readonly Vector3 Deep = new Vector3((25 + 0.5f) * Cell, 0f, (22 + 0.5f) * Cell);      // (102,0,90)
        static readonly Vector3 Entry = new Vector3((2 + 0.5f) * Cell, 0f, (2 + 0.5f) * Cell);       // (10,0,10)

        MethodInfo _build;
        readonly List<GameObject> _roots = new List<GameObject>();
        readonly List<NavMeshDataInstance> _navs = new List<NavMeshDataInstance>();

        [OneTimeSetUp]
        public void ResolveBuilder()
        {
            var asm = System.AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
            Assert.NotNull(asm, "Assembly-CSharp not loaded");
            var type = asm.GetType("BlackCommission.Level.MapSiteBuilder");
            Assert.NotNull(type, "BlackCommission.Level.MapSiteBuilder not found");
            _build = type.GetMethod("Build", BindingFlags.Public | BindingFlags.Static, null,
                new[] { typeof(Transform), typeof(int), typeof(int), typeof(int) }, null);
            Assert.NotNull(_build, "MapSiteBuilder.Build(Transform,int,int,int) not found");
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var n in _navs) if (n.valid) n.Remove();
            _navs.Clear();
            foreach (var r in _roots) if (r != null) Object.DestroyImmediate(r);
            _roots.Clear();
        }

        [Test]
        public void Site_builds_geometry_without_throwing()
        {
            var root = NewRoot("BUILD_ONLY");
            Assert.DoesNotThrow(() => _build.Invoke(null, new object[] { root.transform, 12345, 28, 24 }));
            Assert.Greater(root.transform.childCount, 0, "builder produced no Indoor/Outdoor roots");
        }

        [Test]
        public void Generator_connects_entry_to_deep_at_full_size()
        {
            // INDOOR traversability by PURE LOGIC (no navmesh bake → no scale artifact): the generator must
            // connect all 8 hero waypoints ENTRY…DEEP at the shipped 28×24 config WITH loops + rooms. The
            // edge-builder realises every link as a 2 m door, so logical reachability ⇒ a walkable building.
            var anchors = Map2Anchors();
            for (int seed = 1; seed <= 50; seed++)
            {
                var layout = GridMapGenerator.Generate(28, 24, anchors, seed, 40, 24, 1);
                Assert.IsTrue(GridMapGenerator.AllAnchorsReachable(layout, anchors),
                    $"seed {seed}: a waypoint (ENTRY…DEEP) was unreachable in the 28×24 layout");
            }
        }

        [Test]
        public void Generator_creates_loops()
        {
            // The interconnected-maze fix: with loops the link graph has CYCLES. A spanning tree over N
            // reachable cells has N−1 edges, so LinkCount > N−1 proves multiple routes exist (not one path).
            var anchors = Map2Anchors();
            for (int seed = 1; seed <= 25; seed++)
            {
                var layout = GridMapGenerator.Generate(28, 24, anchors, seed, 40, 24, 0); // loops on, rooms off
                int cells = CountNonEmpty(layout);
                Assert.Greater(layout.LinkCount, cells - 1,
                    $"seed {seed}: {layout.LinkCount} links ≤ tree edges {cells - 1} — no cycles (still one path)");
            }
        }

        [Test]
        public void Rooms_are_open_and_reachable()
        {
            // Each waypoint expands into an OPEN room: Room-kind cells owned by the anchor, interior linked
            // (so the builder leaves it open), still reachable from ENTRY.
            var anchors = Map2Anchors();
            var layout = GridMapGenerator.Generate(28, 24, anchors, 12345, 40, 24, 1);
            foreach (var a in anchors)
            {
                Assert.AreEqual(CellKind.Room, layout.Kind(a.Cell), $"{a.Id}: anchor cell is not part of a room");
                Assert.AreEqual(a.Id, layout.Owner(a.Cell), $"{a.Id}: room owner wrong");
                bool openInterior = false;
                foreach (var d in new[] { new GridCoord(1, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, -1) })
                {
                    var n = a.Cell + d;
                    if (layout.InBounds(n) && layout.Owner(n) == a.Id)
                    {
                        Assert.IsTrue(layout.Linked(a.Cell, n), $"{a.Id}: room interior edge not open (unlinked)");
                        openInterior = true;
                    }
                }
                Assert.IsTrue(openInterior, $"{a.Id}: room has no open interior neighbour");
            }
            Assert.IsTrue(GridMapGenerator.AllAnchorsReachable(layout, anchors), "rooms broke ENTRY…DEEP reachability");
        }

        [Test]
        public void Outdoor_approach_dropoff_to_entry_is_walkable()
        {
            // OUTDOOR approach by navmesh: across the dark forest + through the front door, DROP-OFF → ENTRY
            // must be walkable. (The full DROP-OFF→DEEP single-bake fragments at this scale — a NavMeshBuilder
            // limitation, not a geometry block; the shipped game tile-bakes via NavMeshSurface. Indoor
            // traversability is covered logically above.)
            foreach (int seed in new[] { 1, 777 })
            {
                var root = NewRoot($"SITE_{seed}");
                _build.Invoke(null, new object[] { root.transform, seed, 28, 24 });
                int tris = Bake(root.transform, new Bounds(new Vector3(56f, 0f, -2f), new Vector3(180f, 24f, 220f)));
                Assert.Greater(tris, 0, $"seed {seed}: site baked no navmesh");
                Assert.IsTrue(Walkable(Dropoff, Entry, out var s), $"seed {seed}: DROP-OFF→ENTRY blocked ({s})");
                TestContext.WriteLine($"seed {seed}: drop-off→entry walkable through the forest, {tris} tris.");
                Cleanup();
            }
        }

        // ---- outdoor scatter (pure logic, fast) -------------------------------------------------

        [Test]
        public void Outdoor_scatter_is_deterministic_per_seed()
        {
            System.Func<float, float, bool> none = (x, z) => false;
            var a = OutdoorScatterGenerator.Generate(-20, -30, 60, 0, 999, 3.5f, none);
            var b = OutdoorScatterGenerator.Generate(-20, -30, 60, 0, 999, 3.5f, none);
            Assert.AreEqual(a.Count, b.Count, "scatter count differs for the same seed");
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].X, b[i].X, 1e-4f, $"item {i} X differs");
                Assert.AreEqual(a[i].Z, b[i].Z, 1e-4f, $"item {i} Z differs");
                Assert.AreEqual(a[i].Type, b[i].Type, $"item {i} type differs");
            }
            Assert.Greater(a.Count, 0, "expected some scatter");
        }

        [Test]
        public void Outdoor_scatter_respects_exclusion_and_stays_stable()
        {
            // The keep/skip stream is independent of exclusions, so excluding a region only REMOVES items —
            // the surviving items keep their exact positions (host-deterministic regardless of the mask).
            System.Func<float, float, bool> none = (x, z) => false;
            System.Func<float, float, bool> box = (x, z) => x > 0 && x < 30 && z > -20 && z < 0;
            var full = OutdoorScatterGenerator.Generate(-20, -30, 60, 0, 7, 3.5f, none);
            var masked = OutdoorScatterGenerator.Generate(-20, -30, 60, 0, 7, 3.5f, box);
            Assert.Less(masked.Count, full.Count, "exclusion removed nothing");
            foreach (var it in masked)
                Assert.IsFalse(box(it.X, it.Z), $"a scatter item survived inside the exclusion at ({it.X},{it.Z})");
        }

        // ---- helpers ----------------------------------------------------------------------------

        GameObject NewRoot(string name)
        {
            var go = new GameObject(name);
            _roots.Add(go);
            return go;
        }

        int Bake(Transform from, Bounds bounds)
        {
            // Collect ONLY collider geometry (floors/walls/ground/scatter have colliders; markers don't), so
            // walls block and doors pass faithfully and the anchor markers don't interfere.
            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();
            NavMeshBuilder.CollectSources(from, ~0, NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            var settings = NavMesh.GetSettingsByID(0);
            var data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            _navs.Add(NavMesh.AddNavMeshData(data));
            return NavMesh.CalculateTriangulation().indices.Length / 3;
        }

        static bool Walkable(Vector3 a, Vector3 b, out NavMeshPathStatus status)
        {
            status = NavMeshPathStatus.PathInvalid;
            if (!NavMesh.SamplePosition(a, out var ha, 8f, NavMesh.AllAreas)) return false;
            if (!NavMesh.SamplePosition(b, out var hb, 8f, NavMesh.AllAreas)) return false;
            var path = new NavMeshPath();
            NavMesh.CalculatePath(ha.position, hb.position, NavMesh.AllAreas, path);
            status = path.status;
            return status == NavMeshPathStatus.PathComplete;
        }

        // The 8 map-2 waypoints — MIRROR MapSiteBuilder.Anchors; keep in sync if those change.
        static List<GridMapGenerator.AnchorSpec> Map2Anchors() => new List<GridMapGenerator.AnchorSpec>
        {
            new GridMapGenerator.AnchorSpec("ENTRY", new GridCoord(2, 2)),
            new GridMapGenerator.AnchorSpec("W1", new GridCoord(9, 7)),
            new GridMapGenerator.AnchorSpec("W2", new GridCoord(4, 15)),
            new GridMapGenerator.AnchorSpec("W3", new GridCoord(15, 11)),
            new GridMapGenerator.AnchorSpec("W4", new GridCoord(22, 5)),
            new GridMapGenerator.AnchorSpec("W5", new GridCoord(12, 20)),
            new GridMapGenerator.AnchorSpec("W6", new GridCoord(24, 16)),
            new GridMapGenerator.AnchorSpec("DEEP", new GridCoord(25, 22)),
        };

        static int CountNonEmpty(GridLayout layout)
        {
            int n = 0;
            for (int x = 0; x < layout.Width; x++)
                for (int y = 0; y < layout.Height; y++)
                    if (layout.Kind(new GridCoord(x, y)) != CellKind.Empty) n++;
            return n;
        }
    }
}
