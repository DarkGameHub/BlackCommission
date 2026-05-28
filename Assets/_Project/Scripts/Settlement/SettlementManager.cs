using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Calculates mission payout/penalties after evacuation and sends results to all clients.
/// </summary>
public class SettlementManager : NetworkBehaviour
{
    public static SettlementManager Instance { get; private set; }

    [Header("Rewards")]
    [SerializeField] int mainObjectiveBonus = 800;
    [SerializeField] int survivor1Reward = 400;
    [SerializeField] int survivor2Reward = 600;
    [SerializeField] int pumpRepairReward = 500;
    [SerializeField] int evidenceReward = 300;
    [SerializeField] int fastClearBonus = 200;   // under 10 minutes
    [SerializeField] int fastClearThreshold = 600;

    [Header("Penalties")]
    [SerializeField] int survivorDeathPenalty = 600;
#pragma warning disable 0414
    [SerializeField] int playerInjuryPenalty = 100;          // used in M2 injury tracking
    [SerializeField] int playerSeriousInjuryPenalty = 300;   // used in M2 injury tracking
    [SerializeField] int propertyDamagePenalty = 150;        // used in M2 damage tracking
#pragma warning restore 0414
    [SerializeField] int timeoutPenalty = 200;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void BeginSettlement(int survivorsRescued, int evidenceCount)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;

        var gm = GameManager.Instance;
        int income = 0;
        int expenses = 0;

        // Income
        if (gm.CanComplete)
            income += mainObjectiveBonus;
        if (survivorsRescued >= 1) income += survivor1Reward;
        if (survivorsRescued >= 2) income += survivor2Reward;
        if (gm.PumpRepaired.Value) income += pumpRepairReward;
        income += evidenceCount * evidenceReward;
        if (gm.MissionTimer.Value < fastClearThreshold) income += fastClearBonus;

        // Penalties (simplified for MVP — expand with actual tracking later)
        int deadSurvivors = 2 - survivorsRescued;
        expenses += deadSurvivors * survivorDeathPenalty;
        if (gm.TimeRemainingToForcedEvac <= 0f)
            expenses += timeoutPenalty;

        int netResult = income - expenses;
        float elapsed = gm.MissionTimer.Value;

        SendSettlementClientRpc(income, expenses, netResult, survivorsRescued,
            evidenceCount, gm.PumpRepaired.Value, gm.CanComplete, elapsed);
    }

    [ClientRpc]
    void SendSettlementClientRpc(int income, int expenses, int net,
        int survivors, int evidence, bool pumpFixed, bool success, float timeElapsed)
    {
        SettlementUIController.Instance?.ShowSettlement(new SettlementData
        {
            Income = income,
            Expenses = expenses,
            Net = net,
            SurvivorsRescued = survivors,
            EvidenceCollected = evidence,
            PumpRepaired = pumpFixed,
            TimeElapsed = timeElapsed
        });

        // Old prototype settlements now follow the MVP rule: rewards are pending
        // until the team returns to the office computer.
        MvpPendingReward.Set(net, net >= 0 ? 1 : -1, 0, success, timeElapsed,
            countsTowardLostItemProgress: false);
    }
}

[System.Serializable]
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

/// <summary>Persistent company state saved between missions.</summary>
public static class CompanyData
{
    const string SaveKey = "AS.CompanyData.v1";

    public static CompanyState Current = Load();

    public static void Save()
    {
        string json = JsonUtility.ToJson(Current);
        PlayerPrefs.SetString(SaveKey, json);
        PlayerPrefs.Save();
    }

    public static void ResetToNew()
    {
        Current = new CompanyState { Funds = -300, Reputation = 0, OfficeLevel = 1, Experience = 0, Debt = 300 };
        Save();
    }

    static CompanyState Load()
    {
        string json = PlayerPrefs.GetString(SaveKey, "");
        if (string.IsNullOrEmpty(json))
            return new CompanyState { Funds = -300, Reputation = 0, OfficeLevel = 1, Experience = 0, Debt = 300 };
        try { return JsonUtility.FromJson<CompanyState>(json); }
        catch { return new CompanyState { Funds = -300, Reputation = 0, OfficeLevel = 1, Experience = 0, Debt = 300 }; }
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
