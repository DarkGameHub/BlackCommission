using System.Collections.Generic;
using BlackCommission.Level;
using NUnit.Framework;

namespace BlackCommission.Level.Tests
{
    /// <summary>
    /// Headless connectivity guarantees for the seed-randomized tower topology. These are the
    /// project's first automated tests and cover the high-risk, server-authoritative,
    /// seed-synced area called out in design/systems-index.md and ADR-0001's validation
    /// criteria. Pure C# — no scene required.
    /// </summary>
    public class TowerTopologyTests
    {
        const int SeedSampleCount = 1000;

        // ---- The canonical v3 map: always solvable, every seed ----

        [Test]
        public void Canonical_BackboneOnly_PassesAllInvariants()
        {
            TopoGraph g = TowerTopologyV3.BuildCanonical();

            // Open ONLY the fixed backbone (every toggle closed) — the worst case for connectivity.
            var backbone = new HashSet<string>();
            foreach (Edge e in g.Edges)
                if (e.FixedOpen) backbone.Add(e.Id);

            Verdict v = TowerTopology.Validate(g, backbone);
            Assert.IsTrue(v.Ok, $"Backbone alone must satisfy every invariant, but: {v}");
        }

        [Test]
        public void Canonical_HasLockedRoomDensity()
        {
            TopoGraph g = TowerTopologyV3.BuildCanonical();
            var roomsByFloor = new Dictionary<int, int>();
            foreach (string room in g.RoomNodes)
            {
                int floor = g.NodeFloor[room];
                roomsByFloor.TryGetValue(floor, out int count);
                roomsByFloor[floor] = count + 1;
            }

            Assert.AreEqual(15, roomsByFloor[1], "F1 should stay at the LC-density target.");
            Assert.AreEqual(15, roomsByFloor[2], "F2 should stay at the LC-density target.");
        }

        [Test]
        public void Canonical_HasExpandedToggleSet()
        {
            TopoGraph g = TowerTopologyV3.BuildCanonical();
            int toggles = 0;
            foreach (Edge e in g.Edges)
                if (e.Toggleable) toggles++;

            Assert.AreEqual(14, toggles, "Densified v3 should keep the expanded seed-switch set.");
        }

        [Test]
        public void Canonical_EverySeed_ResolvesValidWithoutFallback()
        {
            TopoGraph g = TowerTopologyV3.BuildCanonical();
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
            TopoGraph g = TowerTopologyV3.BuildCanonical();
            for (int seed = 1; seed <= SeedSampleCount; seed++)
            {
                TopoResult r = TowerTopology.Resolve(g, seed);
                // I8 is inside Validate, but assert explicitly so a failure names this guarantee.
                Verdict v = TowerTopology.Validate(g, r.OpenEdgeIds);
                Assert.AreNotEqual("I8", v.FailedInvariant, $"seed {seed}: island room — {v.Reason}");
                Assert.IsTrue(v.Ok, $"seed {seed}: {v}");
            }
        }

        [Test]
        public void Canonical_EverySeed_AtLeastTwoDescentsOpen()
        {
            TopoGraph g = TowerTopologyV3.BuildCanonical();
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
            TopoGraph g = TowerTopologyV3.BuildCanonical();
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
            TopoGraph g = TowerTopologyV3.BuildCanonical();
            // Topology should actually vary run to run (otherwise we're back to "memorizable").
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

            // Open everything EXCEPT the toggle -> ISL is an island.
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
            // The only link to the objective is a toggle that NEVER opens by roll (chance 0).
            g.Toggle("t1", "VAN", "OBJ", EdgeKind.Door, openChancePermille: 0);

            TopoResult r = TowerTopology.Resolve(g, seed: 12345);
            Assert.IsTrue(r.UsedFallback, "should have fallen back when no roll could connect the objective");
            Assert.IsTrue(r.Ok, "all-open fallback must be valid by construction");
            Assert.IsTrue(r.OpenEdgeIds.Contains("t1"), "fallback opens every edge");
        }
    }
}
