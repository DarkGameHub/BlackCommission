using System.Collections.Generic;
using BlackCommission.Level;
using NUnit.Framework;

namespace BlackCommission.Level.Tests
{
    /// <summary>
    /// Headless connectivity + plan-rule guarantees for the seed-randomized tower topology,
    /// now driven by the PM-approved V8 slab plan (<see cref="TowerPlanV8"/>, 2026-06-10).
    /// The graph is BUILT FROM the plan's door table, so these tests cover both the
    /// connectivity invariants (I1–I9) and the V8 spatial-language rules (V8-C1..C7).
    /// Pure C# — no scene required.
    /// </summary>
    public class TowerTopologyTests
    {
        const int SeedSampleCount = 1000;

        // ---- The V8 plan itself: every spatial-language rule holds ----

        [Test]
        public void PlanV8_Validate_ReportsNoErrors()
        {
            List<string> errors = TowerPlanV8.ValidatePlan();
            Assert.IsEmpty(errors, "V8 plan rule violations:\n" + string.Join("\n", errors));
        }

        [Test]
        public void PlanV8_PassThroughRooms_AreClassifiedAsHubs()
        {
            // The V7 critique root cause: rooms acting as corridors. V8 decouples
            // functionClass from geometry — these four sit on the critical path.
            foreach (string id in new[] { "LOBBY", "HALL", "SALES", "SHOWFLAT" })
                Assert.AreEqual(SlabFunction.Hub, TowerPlanV8.ById[id].Function,
                    $"{id} carries >=2 critical doors and must be a Hub (zero loot, transit).");
        }

        [Test]
        public void PlanV8_RoomSizes_UseOnlyTheThreeLockedClasses()
        {
            foreach (PlanSlab s in TowerPlanV8.Slabs)
            {
                if (s.Kind != SlabKind.Room) continue;
                Assert.AreNotEqual(PlanSize.None, s.Size, $"{s.Id} is a room but has no size class.");
                bool sizeMatches = s.Size switch
                {
                    PlanSize.S => s.W == 4f && s.D == 4f,
                    PlanSize.M => s.W == 8f && s.D == 8f,
                    PlanSize.L => s.W == 12f && s.D == 8f,
                    _ => false
                };
                Assert.IsTrue(sizeMatches, $"{s.Id} ({s.W}x{s.D}) does not match its class {s.Size}.");
            }
        }

        [Test]
        public void PlanV8_StairAnchors_AreLockedAndVerticallyAligned()
        {
            // Locked constraint from the level-map-generation GDD: per-floor stair
            // landings sit exactly over the floor below.
            PlanSlab b1 = TowerPlanV8.ById["STAIRB1"], b2 = TowerPlanV8.ById["STAIRB2"];
            PlanSlab a1 = TowerPlanV8.ById["STAIRA1"], a2 = TowerPlanV8.ById["STAIRA2"];
            Assert.AreEqual((b1.X, b1.Z, b1.W, b1.D), (b2.X, b2.Z, b2.W, b2.D), "B stairs misaligned");
            Assert.AreEqual((a1.X, a1.Z, a1.W, a1.D), (a2.X, a2.Z, a2.W, a2.D), "A stairs misaligned");
            Assert.AreEqual((0f, 16f), (b1.X, b1.Z), "STAIRB anchor moved");
            Assert.AreEqual((26f, 28f), (a1.X, a1.Z), "STAIRA anchor moved");
        }

        [Test]
        public void PlanV8_HasLockedRoomDensityAndToggleCount()
        {
            TopoGraph g = TowerPlanV8.BuildCanonicalGraph();
            var roomsByFloor = new Dictionary<int, int>();
            foreach (string room in g.RoomNodes)
            {
                int floor = g.NodeFloor[room];
                roomsByFloor.TryGetValue(floor, out int count);
                roomsByFloor[floor] = count + 1;
            }
            Assert.AreEqual(15, roomsByFloor[1], "F1 keeps the LC-density target (15 required rooms).");
            Assert.AreEqual(3, roomsByFloor[2],
                "F2 show-flat island has 3 required rooms (SALES/SHOWFLAT/TARGET); VIP/BALCONY are seed-gated bonus.");

            int toggles = 0;
            foreach (Edge e in g.Edges) if (e.Toggleable) toggles++;
            Assert.AreEqual(9, toggles, "V8 has 7 F1 + 2 F2 toggles (T3/T12 were promoted to fixed anchors).");
        }

        [Test]
        public void PlanV8_SeedGatedBonusRooms_AreNotRequiredButStillFillable()
        {
            TopoGraph g = TowerPlanV8.BuildCanonicalGraph();
            foreach (string id in TowerPlanV8.SeedGatedBonusRooms)
            {
                Assert.IsFalse(g.RoomNodes.Contains(id),
                    $"{id} is toggle-gated by design and must not be in the I8 required set.");
                Assert.AreEqual(SlabKind.Room, TowerPlanV8.ById[id].Kind,
                    $"{id} is still a fillable room slab.");
            }
        }

        [Test]
        public void PlanV8_CriticalPath_CrossesPlateBridgeAndIsland()
        {
            TopoGraph g = TowerPlanV8.BuildCanonicalGraph();
            AssertCriticalEdge(g, "D30", "STAIRB2", "PLATE");
            AssertCriticalEdge(g, "D31", "PLATE", "BRIDGE2");
            AssertCriticalEdge(g, "D32", "BRIDGE2", "SALES");
            AssertCriticalEdge(g, "D33", "SALES", "SHOWFLAT");
            AssertCriticalEdge(g, "D34", "SHOWFLAT", "TARGET");
        }

        [Test]
        public void PlanV8_CarryReturn_BiasesTowardAStair()
        {
            TopoGraph g = TowerPlanV8.BuildCanonicalGraph();
            AssertCriticalEdge(g, "D35", "STAIRA2", "C7");
            AssertCriticalEdge(g, "D36", "C7", "SALES");
        }

        // ---- Connectivity invariants: always solvable, every seed ----

        [Test]
        public void Canonical_BackboneOnly_PassesAllInvariants()
        {
            TopoGraph g = TowerPlanV8.BuildCanonicalGraph();

            // Open ONLY the fixed backbone (every toggle closed) — the worst case for connectivity.
            var backbone = new HashSet<string>();
            foreach (Edge e in g.Edges)
                if (e.FixedOpen) backbone.Add(e.Id);

            Verdict v = TowerTopology.Validate(g, backbone);
            Assert.IsTrue(v.Ok, $"Backbone alone must satisfy every invariant, but: {v}");
        }

        [Test]
        public void Canonical_EverySeed_ResolvesValidWithoutFallback()
        {
            TopoGraph g = TowerPlanV8.BuildCanonicalGraph();
            for (int seed = 1; seed <= SeedSampleCount; seed++)
            {
                TopoResult r = TowerTopology.Resolve(g, seed);
                Assert.IsTrue(r.Ok, $"seed {seed}: resolved topology invalid: {r.Verdict}");
                Assert.IsFalse(r.UsedFallback, $"seed {seed}: needed all-open fallback (backbone should cover it)");
            }
        }

        [Test]
        public void Canonical_EverySeed_NoIslandRooms()
        {
            TopoGraph g = TowerPlanV8.BuildCanonicalGraph();
            for (int seed = 1; seed <= SeedSampleCount; seed++)
            {
                TopoResult r = TowerTopology.Resolve(g, seed);
                Verdict v = TowerTopology.Validate(g, r.OpenEdgeIds);
                Assert.AreNotEqual("I8", v.FailedInvariant, $"seed {seed}: island room — {v.Reason}");
                Assert.IsTrue(v.Ok, $"seed {seed}: {v}");
            }
        }

        [Test]
        public void Canonical_EverySeed_AtLeastTwoDescentsOpen()
        {
            TopoGraph g = TowerPlanV8.BuildCanonicalGraph();
            for (int seed = 1; seed <= SeedSampleCount; seed++)
            {
                TopoResult r = TowerTopology.Resolve(g, seed);
                int descents = 0;
                foreach (string id in g.DescentEdgeIds)
                    if (r.OpenEdgeIds.Contains(id)) descents++;
                Assert.GreaterOrEqual(descents, 2, $"seed {seed}: only {descents} descent(s) open");
            }
        }

        [Test]
        public void Resolve_IsDeterministic_ForTheSameSeed()
        {
            TopoGraph g = TowerPlanV8.BuildCanonicalGraph();
            for (int seed = 1; seed <= 50; seed++)
            {
                TopoResult a = TowerTopology.Resolve(g, seed);
                TopoResult b = TowerTopology.Resolve(g, seed);
                Assert.IsTrue(a.OpenEdgeIds.SetEquals(b.OpenEdgeIds),
                    $"seed {seed}: same seed produced different open sets (would desync host/clients)");
            }
        }

        [Test]
        public void Resolve_DifferentSeeds_ProduceDifferentMazes()
        {
            TopoGraph g = TowerPlanV8.BuildCanonicalGraph();
            var signatures = new HashSet<string>();
            for (int seed = 1; seed <= 200; seed++)
            {
                var open = new List<string>(TowerTopology.Resolve(g, seed).OpenEdgeIds);
                open.Sort(System.StringComparer.Ordinal);
                signatures.Add(string.Join(",", open));
            }
            Assert.Greater(signatures.Count, 1, "topology never varied across 200 seeds");
        }

        // ---- The validator itself catches broken graphs ----

        [Test]
        public void Validator_CatchesIslandRoom()
        {
            var g = new TopoGraph();
            g.Node("VAN", 1).Node("A", 1, room: true).Node("ISL", 1, room: true);
            g.Van = "VAN";
            g.Fixed("e1", "VAN", "A", EdgeKind.Corridor, critical: true);
            g.Toggle("t1", "A", "ISL", EdgeKind.Door); // ISL only reachable via this toggle

            var open = new HashSet<string> { "e1" };
            Verdict v = TowerTopology.Validate(g, open);
            Assert.IsFalse(v.Ok);
            Assert.AreEqual("I8", v.FailedInvariant, v.ToString());
        }

        [Test]
        public void Validator_CatchesBrokenCriticalPath()
        {
            var g = new TopoGraph();
            g.Node("VAN", 1).Node("OBJ", 2, room: true);
            g.Van = "VAN";
            g.Objective = "OBJ";
            g.Fixed("e1", "VAN", "OBJ", EdgeKind.Corridor, critical: true);

            Verdict v = TowerTopology.Validate(g, new HashSet<string>()); // nothing open
            Assert.IsFalse(v.Ok);
            Assert.AreEqual("I1", v.FailedInvariant, v.ToString());
        }

        [Test]
        public void Resolve_FallsBackToAllOpen_WhenRollsCannotSatisfy()
        {
            var g = new TopoGraph();
            g.Node("VAN", 1).Node("OBJ", 1, room: true);
            g.Van = "VAN";
            g.Objective = "OBJ";
            g.Toggle("t1", "VAN", "OBJ", EdgeKind.Door, openChancePermille: 0);

            TopoResult r = TowerTopology.Resolve(g, seed: 12345);
            Assert.IsTrue(r.UsedFallback, "should have fallen back when no roll could connect the objective");
            Assert.IsTrue(r.Ok, "all-open fallback must be valid by construction");
            Assert.IsTrue(r.OpenEdgeIds.Contains("t1"), "fallback opens every edge");
        }

        static void AssertCriticalEdge(TopoGraph g, string id, string a, string b)
        {
            foreach (Edge e in g.Edges)
            {
                if (e.Id != id) continue;
                Assert.IsTrue(e.FixedOpen, $"{id} must be fixed-open.");
                Assert.IsTrue(e.CriticalPath, $"{id} must be on the critical backbone.");
                bool endpointsMatch = (e.A == a && e.B == b) || (e.A == b && e.B == a);
                Assert.IsTrue(endpointsMatch, $"{id} endpoints were {e.A}<->{e.B}, expected {a}<->{b}.");
                return;
            }

            Assert.Fail($"Missing edge {id}.");
        }
    }
}
