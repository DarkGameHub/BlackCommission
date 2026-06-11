using UnityEngine;

/// <summary>
/// The host-authoritative company progression state and its settlement math.
/// Pure data + logic (no I/O, no networking) so it is EditMode-testable; persistence
/// and host/client snapshot sync live in the static CompanyData wrapper.
/// Player-facing surface is money + license stages only — Reputation, OfficeLevel and
/// HostileTakeoverPressure are INTERNAL drivers (see design/gdd/office-economy-progression.md).
/// </summary>
[System.Serializable]
public class CompanyState
{
    public int Funds;
    public int Reputation;
    public int OfficeLevel = 1;
    public int Experience;
    public int Debt;
    public int CompletedLostItemJobs;
    public int FailedJobs;
    public int HostileTakeoverPressure;
    public bool HasAcquiredTutorialOffice;
    public bool WristwatchPurchased;
    public bool LastMissionSucceeded;
    public bool WasRecentlyHostileAcquired;
    public bool HasHostileTakeoverUltimatum;
    public bool WasRecentlyIssuedTakeoverUltimatum;
    public float LastMissionTimeSeconds;

    const int TutorialOfficeValuation = 100;
    const int TutorialAcquisitionMultiplierNumerator = 3;
    const int TutorialAcquisitionMultiplierDenominator = 2;

    public bool IsInDebt => Funds < 0;
    public int ExperienceForNextLevel => Mathf.Max(100, OfficeLevel * 300);
    public bool CanShowTutorialAcquisition => OfficeLevel == 1 && CompletedLostItemJobs >= 2 && !HasAcquiredTutorialOffice;
    public int TutorialAcquisitionCost => TutorialOfficeValuation * TutorialAcquisitionMultiplierNumerator / TutorialAcquisitionMultiplierDenominator;
    public bool CanAffordTutorialAcquisition => CanShowTutorialAcquisition && Funds >= TutorialAcquisitionCost && HostileTakeoverPressure < 70;
    public int UnlockedCategoryCount => Mathf.Clamp(OfficeLevel, 1, 8);
    public bool IsHostileTakeoverRisk => HasHostileTakeoverUltimatum || (HostileTakeoverPressure >= 70 && Funds < 0);

    public void ApplyMissionResult(bool success, int money, int reputation, int experience, float elapsedSeconds,
        bool countsTowardLostItemProgress = true)
    {
        ApplyMissionResult(success, money, reputation, experience, elapsedSeconds, countsTowardLostItemProgress,
            success ? MvpMissionResultKind.Success : MvpMissionResultKind.Failed);
    }

    public void ApplyMissionResult(bool success, int money, int reputation, int experience, float elapsedSeconds,
        bool countsTowardLostItemProgress, MvpMissionResultKind resultKind)
    {
        Funds += money;
        Reputation += reputation;
        Experience += resultKind == MvpMissionResultKind.Failed ? 0 : experience;
        LastMissionSucceeded = success;
        LastMissionTimeSeconds = elapsedSeconds;
        WasRecentlyHostileAcquired = false;
        WasRecentlyIssuedTakeoverUltimatum = false;

        if (resultKind == MvpMissionResultKind.Success)
        {
            if (countsTowardLostItemProgress)
                CompletedLostItemJobs++;
            HostileTakeoverPressure = Mathf.Max(0, HostileTakeoverPressure - 25);
            if (HostileTakeoverPressure < 70)
                HasHostileTakeoverUltimatum = false;
        }
        else if (resultKind == MvpMissionResultKind.Partial)
        {
            int pressureGain = 12;
            if (Funds < 0) pressureGain += 5;
            if (Reputation < 0) pressureGain += 5;
            HostileTakeoverPressure = Mathf.Min(100, HostileTakeoverPressure + pressureGain);
            if (HostileTakeoverPressure < 100)
                HasHostileTakeoverUltimatum = false;
        }
        else
        {
            FailedJobs++;
            int pressureGain = 35;
            if (Funds < 0) pressureGain += 15;
            if (Reputation < 0) pressureGain += 10;
            HostileTakeoverPressure = Mathf.Min(100, HostileTakeoverPressure + pressureGain);
            TryApplyHostileTakeover();
        }

        TryApplyLevelUps();
    }

    public void TryApplyLevelUps()
    {
        while (OfficeLevel < 8 && Experience >= ExperienceForNextLevel)
        {
            Experience -= ExperienceForNextLevel;
            OfficeLevel++;
        }
    }

    public bool TryAcquireTutorialOffice()
    {
        if (!CanAffordTutorialAcquisition) return false;

        Funds -= TutorialAcquisitionCost;
        HasAcquiredTutorialOffice = true;
        OfficeLevel = Mathf.Max(OfficeLevel, 2);
        Reputation += 1;
        HostileTakeoverPressure = Mathf.Max(0, HostileTakeoverPressure - 20);
        HasHostileTakeoverUltimatum = false;
        WasRecentlyIssuedTakeoverUltimatum = false;
        WasRecentlyHostileAcquired = false;
        return true;
    }

    void TryApplyHostileTakeover()
    {
        if (HostileTakeoverPressure < 100) return;
        if (Funds >= 0 || Reputation >= 0) return;
        if (!HasHostileTakeoverUltimatum)
        {
            HasHostileTakeoverUltimatum = true;
            WasRecentlyIssuedTakeoverUltimatum = true;
            return;
        }

        WasRecentlyHostileAcquired = true;
        HasHostileTakeoverUltimatum = false;
        WasRecentlyIssuedTakeoverUltimatum = false;
        OfficeLevel = Mathf.Max(1, OfficeLevel - 1);
        Debt += 500;
        Funds = Mathf.Min(Funds, -500);
        Reputation = Mathf.Min(Reputation, -5);
        Experience = 0;
        CompletedLostItemJobs = 0;
        HasAcquiredTutorialOffice = false;
        HostileTakeoverPressure = 35;
    }
}
