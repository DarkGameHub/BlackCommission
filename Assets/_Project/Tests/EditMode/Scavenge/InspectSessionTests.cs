using NUnit.Framework;

namespace BlackCommission.Scavenge.Tests
{
    /// <summary>
    /// Pure state-core contract for first-person relic inspection
    /// (design/ux/item-inspection.md, AC1–AC4). <see cref="InspectSession"/> holds no Unity,
    /// scene, or value reference, so these pin the enter/exit transitions and the precedence of
    /// the vulnerable escape valve (decision ②) deterministically. AC4 ("inspection never
    /// changes value") is guaranteed structurally — the core has no economy field to touch —
    /// and is verified at the InspectController layer in Play.
    /// </summary>
    public class InspectSessionTests
    {
        [Test]
        public void Tick_HoldWithTarget_EntersInspect()
        {
            var s = new InspectSession();

            var cmd = s.Tick(new InspectInput(holdInspect: true, interrupt: false, downed: false, hasTarget: true));

            Assert.AreEqual(InspectCommand.Enter, cmd);
            Assert.IsTrue(s.IsActive);
        }

        [Test]
        public void Tick_HoldWithoutTarget_DoesNotEnter()
        {
            var s = new InspectSession();

            var cmd = s.Tick(new InspectInput(holdInspect: true, interrupt: false, downed: false, hasTarget: false));

            Assert.AreEqual(InspectCommand.None, cmd);
            Assert.IsFalse(s.IsActive);
        }

        [Test]
        public void Tick_CannotEnterWhileDowned()
        {
            var s = new InspectSession();

            var cmd = s.Tick(new InspectInput(holdInspect: true, interrupt: false, downed: true, hasTarget: true));

            Assert.AreEqual(InspectCommand.None, cmd);
            Assert.IsFalse(s.IsActive);
        }

        [Test]
        public void Tick_ReleaseWhileActive_ExitsWithRelease()
        {
            var s = new InspectSession();
            s.Tick(new InspectInput(true, false, false, true)); // enter

            var cmd = s.Tick(new InspectInput(holdInspect: false, interrupt: false, downed: false, hasTarget: true));

            Assert.AreEqual(InspectCommand.Exit, cmd);
            Assert.IsFalse(s.IsActive);
            Assert.AreEqual(InspectExitReason.Release, s.LastExit);
        }

        [Test]
        public void Tick_MoveOrCombatInputWhileActive_InterruptsInstantly()
        {
            var s = new InspectSession();
            s.Tick(new InspectInput(true, false, false, true)); // enter

            var cmd = s.Tick(new InspectInput(holdInspect: true, interrupt: true, downed: false, hasTarget: true));

            Assert.AreEqual(InspectCommand.Exit, cmd);
            Assert.IsFalse(s.IsActive);
            Assert.AreEqual(InspectExitReason.Interrupt, s.LastExit,
                "Any move/combat/light/hotbar input is the vulnerable escape valve (decision ②).");
        }

        [Test]
        public void Tick_DownedWhileActive_ExitsAsDowned()
        {
            var s = new InspectSession();
            s.Tick(new InspectInput(true, false, false, true)); // enter

            var cmd = s.Tick(new InspectInput(holdInspect: true, interrupt: false, downed: true, hasTarget: true));

            Assert.AreEqual(InspectCommand.Exit, cmd);
            Assert.AreEqual(InspectExitReason.Downed, s.LastExit);
        }

        [Test]
        public void Tick_DownedTakesPrecedenceOverInterruptAndRelease()
        {
            var s = new InspectSession();
            s.Tick(new InspectInput(true, false, false, true)); // enter

            var cmd = s.Tick(new InspectInput(holdInspect: false, interrupt: true, downed: true, hasTarget: true));

            Assert.AreEqual(InspectCommand.Exit, cmd);
            Assert.AreEqual(InspectExitReason.Downed, s.LastExit, "Downed wins regardless of other inputs.");
        }

        [Test]
        public void Tick_HoldSteadyWhileActive_StaysInspectingNoEdge()
        {
            var s = new InspectSession();
            s.Tick(new InspectInput(true, false, false, true)); // enter

            var cmd = s.Tick(new InspectInput(true, false, false, true)); // hold steady

            Assert.AreEqual(InspectCommand.None, cmd);
            Assert.IsTrue(s.IsActive, "Holding maintains the pose; only edges return a command.");
        }

        [Test]
        public void Tick_LosingAimWhileActive_DoesNotDropInspection()
        {
            // Target only gates ENTRY; once raised you are already holding it, so losing aim
            // does not force-exit. Only release / interrupt / downed exit.
            var s = new InspectSession();
            s.Tick(new InspectInput(true, false, false, true)); // enter

            var cmd = s.Tick(new InspectInput(holdInspect: true, interrupt: false, downed: false, hasTarget: false));

            Assert.AreEqual(InspectCommand.None, cmd);
            Assert.IsTrue(s.IsActive);
        }

        [Test]
        public void Tick_InterruptThenStillHolding_DoesNotReEnterUntilReleased()
        {
            var s = new InspectSession();
            s.Tick(new InspectInput(true, false, false, true)); // enter
            s.Tick(new InspectInput(holdInspect: true, interrupt: true, downed: false, hasTarget: true)); // interrupt-exit

            // Inspect key still held + target present — must NOT re-enter (you tapped move to leave).
            var stillHeld = s.Tick(new InspectInput(holdInspect: true, interrupt: false, downed: false, hasTarget: true));
            Assert.AreEqual(InspectCommand.None, stillHeld);
            Assert.IsFalse(s.IsActive);

            // Releasing re-arms, then a fresh hold re-enters.
            s.Tick(new InspectInput(holdInspect: false, interrupt: false, downed: false, hasTarget: true)); // release → re-arm
            var reenter = s.Tick(new InspectInput(holdInspect: true, interrupt: false, downed: false, hasTarget: true));
            Assert.AreEqual(InspectCommand.Enter, reenter);
            Assert.IsTrue(s.IsActive);
        }
    }
}
