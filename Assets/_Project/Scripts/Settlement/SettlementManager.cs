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
    [SerializeField] int playerInjuryPenalty = 100;
    [SerializeField] int playerSeriousInjuryPenalty = 300;
    [SerializeField] int timeoutPenalty = 200;
    [SerializeField] int propertyDamagePenalty = 150;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void BeginSettlement(int survivorsRescued, int evidenceCount)
    {
        if (!IsServer) return;

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
        if (gm.CurrentPhase.Value == GameManager.MissionPhase.ForcedEvac)
            expenses += timeoutPenalty;

        int netResult = income - expenses;
        float elapsed = gm.MissionTimer.Value;

        SendSettlementClientRpc(income, expenses, netResult, survivorsRescued,
            evidenceCount, gm.PumpRepaired.Value, elapsed);
    }

    [ClientRpc]
    void SendSettlementClientRpc(int income, int expenses, int net,
        int survivors, int evidence, bool pumpFixed, float timeElapsed)
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

        // Update persistent company funds (local for MVP, server-sync later)
        CompanyData.Current.Funds += net;
        CompanyData.Current.Reputation += net > 0 ? 1 : -1;
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
    public static CompanyState Current = new CompanyState { Funds = 1000, Reputation = 0 };
}

[System.Serializable]
public class CompanyState
{
    public int Funds;
    public int Reputation;
    public bool IsInDebt => Funds < 0;
}
