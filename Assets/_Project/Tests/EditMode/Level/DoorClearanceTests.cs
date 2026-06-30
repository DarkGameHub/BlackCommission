using System.Collections.Generic;
using NUnit.Framework;

namespace BlackCommission.Level.Tests
{
    /// <summary>
    /// Door-fill clearance logic: plan-derived door openings (TowerPlanV8.DoorOpeningsForSlab) and the
    /// pure 1-D slide search (DoorClearance.NearestFree). No scene/transform — the "slide only the prop
    /// that sits across the door, else remove it" contract is pinned deterministically.
    /// </summary>
    public class DoorClearanceTests
    {
        const float Eps = 1e-3f;

        [Test]
        public void DoorOpeningsForSlab_Pump_WestAndEast_Centred()
        {
            // PUMP (24,0,4,4), centre (26,2): D17 from LOBBY on its west face, T11 to WORKSHOP on east.
            // Both faces overlap z[0,4] → mid z=2 → offset = 2 - 2 = 0; widths 2.0 → half 1.0.
            List<SlabDoorOpening> o = TowerPlanV8.DoorOpeningsForSlab("PUMP");
            Assert.AreEqual(2, o.Count);
            Assert.AreEqual(DoorEdge.W, o[0].Edge);
            Assert.AreEqual(0f, o[0].Offset, Eps);
            Assert.AreEqual(1.0f, o[0].HalfWidth, Eps);
            Assert.AreEqual(DoorEdge.E, o[1].Edge);
            Assert.AreEqual(0f, o[1].Offset, Eps);
            Assert.AreEqual(1.0f, o[1].HalfWidth, Eps);
        }

        [Test]
        public void DoorOpeningsForSlab_Lobby_HasOffsetVanDoorOnSouth()
        {
            // D-VAN: LOBBY (12,0,12,8) south face, offset +3 → centre x = 18+3 = 21; LOBBY centre x=18
            // → offset = 3; width 2.8 → half 1.4. Exercises the S/N branch and a non-zero offset.
            List<SlabDoorOpening> o = TowerPlanV8.DoorOpeningsForSlab("LOBBY");
            bool found = false;
            foreach (SlabDoorOpening d in o)
                if (d.Edge == DoorEdge.S && System.Math.Abs(d.Offset - 3f) < Eps &&
                    System.Math.Abs(d.HalfWidth - 1.4f) < Eps) found = true;
            Assert.IsTrue(found, "expected a south opening at offset +3, half 1.4 (D-VAN)");
        }

        [Test]
        public void DoorOpeningsForSlab_UnknownSlab_Empty()
            => Assert.AreEqual(0, TowerPlanV8.DoorOpeningsForSlab("NOPE").Count);

        [Test]
        public void NearestFree_AlreadyClear_StaysPut()
            => Assert.AreEqual(0.5f, DoorClearance.NearestFree(0.5f, 2.0f, Bands()), Eps);

        [Test]
        public void NearestFree_Blocked_SlidesToNearestDoorEdge()
        {
            // Door band [-1, 1], prop centred at 0.5, wall room ±2 → nearest free is the right edge 1.0.
            Assert.AreEqual(1.0f, DoorClearance.NearestFree(0.5f, 2.0f, Bands((-1f, 1f))), Eps);
        }

        [Test]
        public void NearestFree_CentredProp_SlidesToFirstEqualEdge()
        {
            // Symmetric: prop at 0, band [-1.3,1.3], wall ±1.7 → both edges tie at dist 1.3; lo edge wins.
            Assert.AreEqual(-1.3f, DoorClearance.NearestFree(0f, 1.7f, Bands((-1.3f, 1.3f))), Eps);
        }

        [Test]
        public void NearestFree_WallFullyCovered_ReturnsNaN()
        {
            // Band [-1.3,1.3] swallows the whole reachable range ±1.0 → nowhere to go.
            Assert.IsNaN(DoorClearance.NearestFree(0f, 1.0f, Bands((-1.3f, 1.3f))));
        }

        [Test]
        public void NearestFree_PropWiderThanWall_ReturnsNaN()
            => Assert.IsNaN(DoorClearance.NearestFree(0f, -0.2f, Bands()));

        static IReadOnlyList<(float lo, float hi)> Bands(params (float lo, float hi)[] b)
            => new List<(float, float)>(b);
    }
}
