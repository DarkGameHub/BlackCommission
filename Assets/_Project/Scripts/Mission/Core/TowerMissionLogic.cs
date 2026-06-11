using System;

/// <summary>Tower mission lifecycle states (quick-spec tower-mission-manager-2026-06-10).</summary>
public enum TowerMissionState
{
    InProgress = 0,
    ObjectiveSecured = 1,
    Delivered = 2,
    PartialReturn = 3,
    Failed = 4
}

/// <summary>
/// Pure state machine + seal-completeness math for the abandoned-tower commission
/// (design/quick-specs/tower-mission-manager-2026-06-10.md). Engine-free so it is
/// EditMode-testable; the server-authoritative TowerMissionManager owns networking
/// and forwards events here on the authority only.
///
/// Rules:
/// - ObjectiveSecured latches on the first pickup and survives later drops.
/// - Every HARD drop (impact at/over the Carriable damage threshold) costs
///   <see cref="dropPenalty"/> completeness; gentle hand-offs cost nothing.
/// - Departure with the column aboard delivers ONLY at/above
///   <see cref="rejectThreshold"/> completeness (contract: 密封罩破损不予接收);
///   otherwise the run downgrades to PartialReturn.
/// - Terminal states (Delivered/PartialReturn/Failed) latch; later events are no-ops.
/// </summary>
public class TowerMissionLogic
{
    const float ThresholdEpsilon = 1e-4f;

    readonly float dropPenalty;
    readonly float rejectThreshold;

    public TowerMissionState State { get; private set; } = TowerMissionState.InProgress;
    public float Completeness { get; private set; } = 1f;

    public bool IsTerminal =>
        State == TowerMissionState.Delivered ||
        State == TowerMissionState.PartialReturn ||
        State == TowerMissionState.Failed;

    public TowerMissionLogic(float dropPenalty = 0.03f, float rejectThreshold = 0.5f)
    {
        this.dropPenalty = dropPenalty;
        this.rejectThreshold = rejectThreshold;
    }

    /// <summary>First pickup of the eco column. Returns true only on the latching transition.</summary>
    public bool NotifySecured()
    {
        if (IsTerminal || State == TowerMissionState.ObjectiveSecured) return false;
        State = TowerMissionState.ObjectiveSecured;
        return true;
    }

    /// <summary>A hard landing (impact at/over the damage threshold). Returns true if completeness changed.</summary>
    public bool NotifyHardDrop()
    {
        if (IsTerminal) return false;
        Completeness = Math.Max(0f, Completeness - dropPenalty);
        return true;
    }

    /// <summary>Van departs. Resolves the terminal outcome (idempotent once terminal).</summary>
    public TowerMissionState ResolveDeparture(bool columnAboard)
    {
        if (IsTerminal) return State;
        State = columnAboard && Completeness >= rejectThreshold - ThresholdEpsilon
            ? TowerMissionState.Delivered
            : TowerMissionState.PartialReturn;
        return State;
    }

    /// <summary>Whole crew downed. Returns true on the latching transition to Failed.</summary>
    public bool NotifyAllDowned()
    {
        if (IsTerminal) return false;
        State = TowerMissionState.Failed;
        return true;
    }

    /// <summary>Full-delivery payout scaled by seal completeness (300G × 0.91 → 273G).</summary>
    public int ScaleDeliveredMoney(int fullMoney) =>
        (int)Math.Round(fullMoney * Completeness, MidpointRounding.AwayFromZero);
}
