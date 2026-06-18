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
///
/// Schema v2 (2026-06-17): Reputation, OfficeLevel, Experience, HostileTakeoverPressure and
/// the hostile-takeover FSM booleans have been removed. Old v1 saves load safely — the
/// removed fields are simply ignored by JsonUtility (extra JSON keys are silently skipped).
/// </summary>
public static class CompanyData
{
    const int SchemaVersion = 2;
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
        new CompanyState { Funds = -300, Debt = 300 };

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
        // v1 saves had Reputation/OfficeLevel/Experience/HostileTakeoverPressure fields.
        // JsonUtility ignores unknown JSON keys when deserialising into the new CompanyState,
        // so old saves load cleanly — no explicit field clearing needed. The only action here
        // is bumping to v2 so future loads skip this path.
        if (fromVersion < 2)
        {
            // No data mutation required; removed fields were simply not populated.
            // Re-saving at v2 schema ensures the file stays clean going forward.
        }
        return state;
    }

    static void WriteToDisk(CompanyState state)
    {
        SaveIO.WriteJson(SaveFileName, new Envelope { schemaVersion = SchemaVersion, state = state });
    }

    // NOTE: CompanyState (the pure progression/settlement logic) lives in
    // Office/Core/CompanyState.cs (BlackCommission.Office.Core) so it is EditMode-testable.
    // This wrapper owns only persistence + host-authority + snapshot sync.
    public static void ApplySnapshot(
        int funds,
        int debt,
        int completedLostItemJobs,
        int failedJobs,
        bool hasAcquiredTutorialOffice,
        bool lastMissionSucceeded,
        float lastMissionTimeSeconds)
    {
        Current.Funds = funds;
        Current.Debt = debt;
        Current.CompletedLostItemJobs = completedLostItemJobs;
        Current.FailedJobs = failedJobs;
        Current.HasAcquiredTutorialOffice = hasAcquiredTutorialOffice;
        Current.LastMissionSucceeded = lastMissionSucceeded;
        Current.LastMissionTimeSeconds = lastMissionTimeSeconds;
    }
}
