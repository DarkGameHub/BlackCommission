using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>Per-mission settlement breakdown (display only).</summary>
public class SettlementData
{
    public int Income;
    public int Expenses;
    public int Net;
    public int SurvivorsRescued;
    public int EvidenceCollected;
    public bool PumpRepaired;
    public float TimeElapsed;
}

/// <summary>
/// Persistent company progression. Saved to a versioned JSON file under persistentDataPath
/// (Steam Cloud-friendly), NOT PlayerPrefs. In networked play the company is the HOST's:
/// only the server writes to disk, so a client joining someone else's game never overwrites
/// its own save (clients receive the host's state via ApplySnapshot for display).
/// Device-local settings (volume, name, sensitivity, voice) stay in PlayerPrefs by design.
/// </summary>
public static class CompanyData
{
    const int SchemaVersion = 1;
    const string SaveFileName = "company.json";
    const string LegacyPrefsKey = "AS.CompanyData.v1"; // pre-file saves; imported once

    public static CompanyState Current = Load();

    [Serializable]
    class Envelope
    {
        public int schemaVersion;
        public CompanyState state;
    }

    public static void Save()
    {
        // Host-authoritative: in a networked session only the server owns/persists the company.
        NetworkManager nm = NetworkManager.Singleton;
        if (nm != null && nm.IsListening && !nm.IsServer) return;

        WriteToDisk(Current);
    }

    public static void ResetToNew()
    {
        Current = NewState();
        Save();
    }

    static CompanyState NewState() =>
        new CompanyState { Funds = -300, Reputation = 0, OfficeLevel = 1, Experience = 0, Debt = 300 };

    static CompanyState Load()
    {
        var env = SaveIO.ReadJson<Envelope>(SaveFileName);
        if (env != null && env.state != null)
            return Migrate(env.schemaVersion, env.state);

        // One-time import of the old PlayerPrefs save so existing players keep progress.
        string legacy = PlayerPrefs.GetString(LegacyPrefsKey, "");
        if (!string.IsNullOrEmpty(legacy))
        {
            try
            {
                CompanyState imported = JsonUtility.FromJson<CompanyState>(legacy);
                if (imported != null)
                {
                    WriteToDisk(imported);
                    PlayerPrefs.DeleteKey(LegacyPrefsKey);
                    PlayerPrefs.Save();
                    return imported;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[CompanyData] Legacy import failed: {e.Message}");
            }
        }
        return NewState();
    }

    // Reloads the local save from disk. Call when returning to solo/host control so a company
    // synced from a remote host (while you were a guest) doesn't linger as your own.
    public static void ReloadFromDisk() => Current = Load();

    static CompanyState Migrate(int fromVersion, CompanyState state)
    {
        // Future schema bumps handle field changes here; v1 needs no migration.
        return state;
    }

    static void WriteToDisk(CompanyState state)
    {
        SaveIO.WriteJson(SaveFileName, new Envelope { schemaVersion = SchemaVersion, state = state });
    }

    public static void ApplySnapshot(
        int funds,
        int reputation,
        int officeLevel,
        int experience,
        int debt,
        int completedLostItemJobs,
        int failedJobs,
        int hostileTakeoverPressure,
        bool hasAcquiredTutorialOffice,
        bool lastMissionSucceeded,
        bool wasRecentlyHostileAcquired,
        bool hasHostileTakeoverUltimatum,
        bool wasRecentlyIssuedTakeoverUltimatum,
        float lastMissionTimeSeconds)
    {
        Current.Funds = funds;
        Current.Reputation = reputation;
        Current.OfficeLevel = officeLevel;
        Current.Experience = experience;
        Current.Debt = debt;
        Current.CompletedLostItemJobs = completedLostItemJobs;
        Current.FailedJobs = failedJobs;
        Current.HostileTakeoverPressure = hostileTakeoverPressure;
        Current.HasAcquiredTutorialOffice = hasAcquiredTutorialOffice;
        Current.LastMissionSucceeded = lastMissionSucceeded;
        Current.WasRecentlyHostileAcquired = wasRecentlyHostileAcquired;
        Current.HasHostileTakeoverUltimatum = hasHostileTakeoverUltimatum;
        Current.WasRecentlyIssuedTakeoverUltimatum = wasRecentlyIssuedTakeoverUltimatum;
        Current.LastMissionTimeSeconds = lastMissionTimeSeconds;
    }
}

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
