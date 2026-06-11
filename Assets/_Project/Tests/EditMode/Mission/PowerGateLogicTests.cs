using NUnit.Framework;

namespace BlackCommission.Mission.Tests
{
    /// <summary>
    /// Hold-to-restore behavior for the tower power gate — one of the two new
    /// server-authoritative mechanics flagged as untested-sync risk in
    /// design/levels/abandoned-tower-earth-coast-01.md. The PowerGateBreaker component
    /// ticks this logic on the host only; these tests pin the contract it relies on:
    /// release-resets, single completion event, and idempotent latching.
    /// </summary>
    public class PowerGateLogicTests
    {
        const float HoldSeconds = 3f;   // GDD tuning knob: power-gate hold time

        [Test]
        public void Tick_WhileHeld_AccumulatesProgress()
        {
            var gate = new PowerGateLogic(HoldSeconds);

            bool completed = gate.Tick(1.0f, isHeld: true);

            Assert.IsFalse(completed);
            Assert.AreEqual(1.0f, gate.Progress, 0.0001f);
            Assert.IsFalse(gate.IsRestored);
        }

        [Test]
        public void Tick_ReleasedEarly_ResetsProgressToZero()
        {
            var gate = new PowerGateLogic(HoldSeconds);
            gate.Tick(2.9f, isHeld: true);

            gate.Tick(0.016f, isHeld: false);

            Assert.AreEqual(0f, gate.Progress, "The restore is one committed beat — release wipes it.");
            Assert.IsFalse(gate.IsRestored);
        }

        [Test]
        public void Tick_ReachingThreshold_CompletesExactlyOnce()
        {
            var gate = new PowerGateLogic(HoldSeconds);
            gate.Tick(2.5f, isHeld: true);

            bool completedNow = gate.Tick(0.6f, isHeld: true);
            bool completedAgain = gate.Tick(1.0f, isHeld: true);

            Assert.IsTrue(completedNow, "Crossing the threshold must report completion.");
            Assert.IsFalse(completedAgain, "Completion fires exactly once — RPC replays must be no-ops.");
            Assert.IsTrue(gate.IsRestored);
        }

        [Test]
        public void Tick_AfterRestore_IgnoresReleaseAndHold()
        {
            var gate = new PowerGateLogic(HoldSeconds);
            gate.Tick(HoldSeconds, isHeld: true);

            gate.Tick(1f, isHeld: false);
            gate.Tick(1f, isHeld: true);

            Assert.IsTrue(gate.IsRestored, "Restoration latches — power never goes back out.");
            Assert.AreEqual(HoldSeconds, gate.Progress, 0.0001f);
            Assert.AreEqual(1f, gate.NormalizedProgress, 0.0001f);
        }

        [Test]
        public void Tick_AccumulatesAcrossManySmallFrames()
        {
            var gate = new PowerGateLogic(HoldSeconds);

            bool completed = false;
            for (int frame = 0; frame < 200 && !completed; frame++)
                completed = gate.Tick(1f / 60f, isHeld: true);

            Assert.IsTrue(completed, "3s at 60fps should complete within 180 frames.");
        }

        [Test]
        public void Tick_NegativeDeltaTime_DoesNotRegressOrComplete()
        {
            var gate = new PowerGateLogic(HoldSeconds);
            gate.Tick(2f, isHeld: true);

            bool completed = gate.Tick(-100f, isHeld: true);

            Assert.IsFalse(completed);
            Assert.AreEqual(2f, gate.Progress, 0.0001f, "Negative dt is clamped, not applied.");
        }

        [Test]
        public void NormalizedProgress_TracksFraction()
        {
            var gate = new PowerGateLogic(HoldSeconds);
            gate.Tick(1.5f, isHeld: true);

            Assert.AreEqual(0.5f, gate.NormalizedProgress, 0.0001f);
        }

        [Test]
        public void ApplyRestored_ForLateJoiner_LatchesWithoutTicking()
        {
            var gate = new PowerGateLogic(HoldSeconds);

            gate.ApplyRestored();

            Assert.IsTrue(gate.IsRestored);
            Assert.AreEqual(1f, gate.NormalizedProgress, 0.0001f);
            Assert.IsFalse(gate.Tick(5f, isHeld: true), "Synced state must not re-fire completion.");
        }

        [Test]
        public void Constructor_TinyOrZeroHoldTime_IsClampedNotBroken()
        {
            var gate = new PowerGateLogic(0f);

            bool completed = gate.Tick(0.02f, isHeld: true);

            Assert.IsTrue(completed, "A zero-second gate clamps to a minimal hold and still works.");
        }
    }
}
