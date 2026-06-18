namespace BlackCommission.Scavenge
{
    /// <summary>Lifecycle of one scavenging run. Terminal once it leaves <see cref="InProgress"/>.</summary>
    public enum ScavengeMissionState
    {
        InProgress,
        Settled,   // voluntary departure — the crew chose to head back
        Failed,    // forced return (whole crew downed) — the banked cargo still pays out
    }

    /// <summary>
    /// Pure state machine for a scavenging run (the connective tissue mirrored on
    /// <c>ScavengeMissionManager</c>, the networked layer). Scavenging has no full/partial split
    /// like the tower — whatever is in the van comes home — so the value is the cargo manifest's
    /// settlement total (<see cref="ScavengeSettlementCalculator"/>); this only owns the single-fire
    /// terminal transition so the run can't settle twice. No Unity, no networking → unit-testable.
    /// </summary>
    public sealed class ScavengeMissionLogic
    {
        public ScavengeMissionState State { get; private set; } = ScavengeMissionState.InProgress;

        public bool IsTerminal => State != ScavengeMissionState.InProgress;

        /// <summary>
        /// Voluntary departure (depart lever / boarding): settles the banked cargo. The first call
        /// from <see cref="ScavengeMissionState.InProgress"/> transitions to <see cref="ScavengeMissionState.Settled"/>
        /// and returns true; any later call is a no-op returning false.
        /// </summary>
        public bool ResolveDeparture()
        {
            if (IsTerminal) return false;
            State = ScavengeMissionState.Settled;
            return true;
        }

        /// <summary>
        /// Whole crew downed: forced return marked <see cref="ScavengeMissionState.Failed"/> (the cargo
        /// already loaded into the van still settles). Single-fire, same as <see cref="ResolveDeparture"/>.
        /// </summary>
        public bool NotifyAllDowned()
        {
            if (IsTerminal) return false;
            State = ScavengeMissionState.Failed;
            return true;
        }
    }
}
