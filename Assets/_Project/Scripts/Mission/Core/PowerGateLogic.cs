/// <summary>
/// Pure hold-to-restore logic for the power gate (design:
/// design/levels/abandoned-tower-earth-coast-01.md P-01 — "restore power to unlock F2",
/// tuning knob: 3.0 s hold). Engine-free so it is EditMode-testable; the
/// server-authoritative PowerGateBreaker component owns networking and ticks this on
/// the host only.
///
/// Rules:
/// - Progress accumulates only while held, and resets to zero the moment the hold is
///   released early — the restore is one committed beat, not a chore you can chip at.
/// - Completion latches: once restored, the gate stays restored and further ticks are
///   no-ops (idempotent under repeated RPCs / replays).
/// </summary>
public class PowerGateLogic
{
    public float RequiredHoldSeconds { get; }
    public float Progress { get; private set; }
    public bool IsRestored { get; private set; }

    public float NormalizedProgress =>
        IsRestored ? 1f : Progress / RequiredHoldSeconds;

    public PowerGateLogic(float requiredHoldSeconds)
    {
        RequiredHoldSeconds = requiredHoldSeconds > 0.01f ? requiredHoldSeconds : 0.01f;
    }

    /// <summary>
    /// Advance the hold. Returns true exactly once: on the tick that completes the
    /// restoration. All later calls return false regardless of input.
    /// </summary>
    public bool Tick(float deltaSeconds, bool isHeld)
    {
        if (IsRestored) return false;
        if (deltaSeconds < 0f) deltaSeconds = 0f;

        if (!isHeld)
        {
            Progress = 0f;
            return false;
        }

        Progress += deltaSeconds;
        if (Progress < RequiredHoldSeconds) return false;

        Progress = RequiredHoldSeconds;
        IsRestored = true;
        return true;
    }

    /// <summary>Force-apply an externally synced restored state (late joiner / load).</summary>
    public void ApplyRestored()
    {
        IsRestored = true;
        Progress = RequiredHoldSeconds;
    }
}
