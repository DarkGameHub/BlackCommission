namespace BlackCommission.Scavenge
{
    /// <summary>Edge command emitted by <see cref="InspectSession.Tick"/>.</summary>
    public enum InspectCommand { None, Enter, Exit }

    /// <summary>Why an active inspection ended.</summary>
    public enum InspectExitReason { None, Release, Interrupt, Downed }

    /// <summary>Input snapshot for one tick of <see cref="InspectSession"/>.</summary>
    public readonly struct InspectInput
    {
        public readonly bool HoldInspect;
        public readonly bool Interrupt;
        public readonly bool Downed;
        public readonly bool HasTarget;

        public InspectInput(bool holdInspect, bool interrupt, bool downed, bool hasTarget)
        {
            HoldInspect = holdInspect;
            Interrupt = interrupt;
            Downed = downed;
            HasTarget = hasTarget;
        }
    }

    /// <summary>
    /// Pure, Unity-free state core for relic inspection (design/ux/item-inspection.md, AC1–AC3).
    /// Deterministic — identical inputs yield identical transitions, so it is fully EditMode
    /// testable without a scene, camera, or netcode. It holds NO item or value reference, which
    /// is how the "inspection never changes value" contract (AC4) is guaranteed structurally:
    /// there is nothing here that could touch an economy field.
    /// </summary>
    public class InspectSession
    {
        public bool IsActive { get; private set; }
        public InspectExitReason LastExit { get; private set; }

        // Must release the inspect key before a fresh hold can re-enter. Without this, an
        // interrupt (e.g. tapping a move key) while the inspect key is still held would
        // instantly re-enter the next tick — you could never actually walk away.
        bool armed = true;

        /// <summary>Advance one tick; returns the Enter/Exit edge (or None).</summary>
        public InspectCommand Tick(in InspectInput input)
        {
            if (!IsActive)
            {
                if (!input.HoldInspect) armed = true; // re-arm on release

                // Enter only on a fresh hold + a valid aimed target, and not while downed.
                if (armed && input.HoldInspect && input.HasTarget && !input.Downed)
                {
                    IsActive = true;
                    armed = false;
                    LastExit = InspectExitReason.None;
                    return InspectCommand.Enter;
                }
                return InspectCommand.None;
            }

            // Active — exit precedence: downed > interrupt (move/combat/light/hotbar) > release.
            if (input.Downed) return Exit(InspectExitReason.Downed);
            if (input.Interrupt) return Exit(InspectExitReason.Interrupt);
            if (!input.HoldInspect) return Exit(InspectExitReason.Release);
            return InspectCommand.None;
        }

        InspectCommand Exit(InspectExitReason reason)
        {
            IsActive = false;
            armed = false; // require a fresh hold press to re-enter
            LastExit = reason;
            return InspectCommand.Exit;
        }
    }
}
