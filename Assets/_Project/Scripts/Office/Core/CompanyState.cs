using UnityEngine;

/// <summary>
/// The host-authoritative company progression state and its settlement math.
/// Pure data + logic (no I/O, no networking) so it is EditMode-testable; persistence
/// and host/client snapshot sync live in the static CompanyData wrapper.
/// Player-facing surface is money only — Reputation, OfficeLevel, Experience and
/// HostileTakeoverPressure have been removed (PM decision 2026-06-17; see AGENTS.md).
/// Job gating now uses license stages — TODO: gate by license stage (game-pillars.md).
/// </summary>
[System.Serializable]
public class CompanyState
{
    public int Funds;
    public int Debt;
    public int CompletedLostItemJobs;
    public int FailedJobs;
    public bool HasAcquiredTutorialOffice;
    public bool WristwatchPurchased;
    public bool LastMissionSucceeded;
    public float LastMissionTimeSeconds;

    const int TutorialOfficeValuation = 100;
    const int TutorialAcquisitionMultiplierNumerator = 3;
    const int TutorialAcquisitionMultiplierDenominator = 2;

    public bool IsInDebt => Funds < 0;
    public int TutorialAcquisitionCost => TutorialOfficeValuation * TutorialAcquisitionMultiplierNumerator / TutorialAcquisitionMultiplierDenominator;
    public bool CanShowTutorialAcquisition => CompletedLostItemJobs >= 2 && !HasAcquiredTutorialOffice;
    public bool CanAffordTutorialAcquisition => CanShowTutorialAcquisition && Funds >= TutorialAcquisitionCost;

    /// <summary>
    /// Money-only mission settlement (PM decision 2026-06-17).
    /// Implements spec: Funds += money; LastMissionSucceeded = success;
    /// success + countsTowardLostItemProgress increments CompletedLostItemJobs.
    /// </summary>
    public void ApplyMissionResult(bool success, int money, float elapsedSeconds,
        bool countsTowardLostItemProgress = true)
    {
        ApplyMissionResult(success, money, elapsedSeconds, countsTowardLostItemProgress,
            success ? MvpMissionResultKind.Success : MvpMissionResultKind.Failed);
    }

    public void ApplyMissionResult(bool success, int money, float elapsedSeconds,
        bool countsTowardLostItemProgress, MvpMissionResultKind resultKind)
    {
        Funds += money;
        LastMissionSucceeded = success;
        LastMissionTimeSeconds = elapsedSeconds;

        if (resultKind == MvpMissionResultKind.Success)
        {
            if (countsTowardLostItemProgress)
                CompletedLostItemJobs++;
        }
        else if (resultKind == MvpMissionResultKind.Failed)
        {
            FailedJobs++;
        }
    }

    public bool TryAcquireTutorialOffice()
    {
        if (!CanAffordTutorialAcquisition) return false;

        Funds -= TutorialAcquisitionCost;
        HasAcquiredTutorialOffice = true;
        return true;
    }
}
