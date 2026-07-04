using BlackCommission.Monsters;
using NUnit.Framework;

namespace BlackCommission.Monsters.Tests
{
    /// <summary>
    /// Covers the Civic Idol's pure gaze core (<see cref="IdolGazeLogic"/>):
    /// horizontal view-cone membership and the freeze hysteresis window.
    /// GDD: design/gdd/monster-civic-idol.md §Formulas.
    /// </summary>
    public class IdolGazeLogicTests
    {
        const float Range = 45f;
        const float HalfAngle = 50f;

        [Test]
        public void test_target_dead_ahead_is_watched()
        {
            Assert.IsTrue(IdolGazeLogic.IsWithinViewCone(
                0f, 0f, 0f, 1f, 0f, 10f, Range, HalfAngle));
        }

        [Test]
        public void test_target_behind_is_not_watched()
        {
            Assert.IsFalse(IdolGazeLogic.IsWithinViewCone(
                0f, 0f, 0f, 1f, 0f, -10f, Range, HalfAngle));
        }

        [Test]
        public void test_target_beyond_max_range_is_not_watched()
        {
            Assert.IsFalse(IdolGazeLogic.IsWithinViewCone(
                0f, 0f, 0f, 1f, 0f, 45.5f, Range, HalfAngle));
        }

        [Test]
        public void test_target_just_inside_cone_edge_is_watched()
        {
            // 49° off forward with a 50° half-angle: inside.
            float rad = 49f * (float)System.Math.PI / 180f;
            float x = 10f * (float)System.Math.Sin(rad);
            float z = 10f * (float)System.Math.Cos(rad);
            Assert.IsTrue(IdolGazeLogic.IsWithinViewCone(
                0f, 0f, 0f, 1f, x, z, Range, HalfAngle));
        }

        [Test]
        public void test_target_just_outside_cone_edge_is_not_watched()
        {
            // 51° off forward with a 50° half-angle: outside.
            float rad = 51f * (float)System.Math.PI / 180f;
            float x = 10f * (float)System.Math.Sin(rad);
            float z = 10f * (float)System.Math.Cos(rad);
            Assert.IsFalse(IdolGazeLogic.IsWithinViewCone(
                0f, 0f, 0f, 1f, x, z, Range, HalfAngle));
        }

        [Test]
        public void test_unnormalized_forward_behaves_like_normalized()
        {
            Assert.IsTrue(IdolGazeLogic.IsWithinViewCone(
                0f, 0f, 0f, 12.5f, 3f, 10f, Range, HalfAngle));
        }

        [Test]
        public void test_degenerate_forward_is_not_watched()
        {
            // Flattened forward near zero (owner staring straight up/down offline).
            Assert.IsFalse(IdolGazeLogic.IsWithinViewCone(
                0f, 0f, 0f, 0f, 0f, 5f, Range, HalfAngle));
        }

        [Test]
        public void test_coincident_eye_and_target_is_watched()
        {
            Assert.IsTrue(IdolGazeLogic.IsWithinViewCone(
                3f, 4f, 0f, 1f, 3f, 4f, Range, HalfAngle));
        }

        [Test]
        public void test_zero_range_is_never_watched()
        {
            Assert.IsFalse(IdolGazeLogic.IsWithinViewCone(
                0f, 0f, 0f, 1f, 0f, 1f, 0f, HalfAngle));
        }

        [Test]
        public void test_watched_now_freezes_regardless_of_history()
        {
            Assert.IsTrue(IdolGazeLogic.ShouldFreeze(true, -999f, 100f, 0.35f));
        }

        [Test]
        public void test_freeze_holds_during_grace_window()
        {
            // Last watched at t=10.0, now t=10.2, grace 0.35 → still frozen.
            Assert.IsTrue(IdolGazeLogic.ShouldFreeze(false, 10.0f, 10.2f, 0.35f));
        }

        [Test]
        public void test_freeze_releases_after_grace_window()
        {
            // Last watched at t=10.0, now t=10.5, grace 0.35 → free to move.
            Assert.IsFalse(IdolGazeLogic.ShouldFreeze(false, 10.0f, 10.5f, 0.35f));
        }
    }
}
