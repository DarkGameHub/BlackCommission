using UnityEngine;

/// <summary>
/// Per-mission fallback reward values used when no OfficeTaskDefinition is active.
/// Tests use <see cref="Default"/>.
/// </summary>
[System.Serializable]
public struct MissionRewardFallbacks
{
    public int moneyReward;
    public int partialMoneyReward;
    public int failureMoney;

    /// <summary>Registry constants: full_job_reward 300G / partial 60G / failure 20G.</summary>
    public static MissionRewardFallbacks Default => new MissionRewardFallbacks
    {
        moneyReward = 300,
        partialMoneyReward = 60,
        failureMoney = 20
    };
}

/// <summary>Optional-objective (bonus evidence) reward values.</summary>
[System.Serializable]
public struct MissionRewardBonus
{
    public int money;

    public static MissionRewardBonus Default => new MissionRewardBonus
    {
        money = 90
    };
}

/// <summary>Final settlement numbers handed to MvpPendingReward / the settlement UI.</summary>
public struct MissionRewardResult
{
    public int Money;
    public float OvertimeGameHours;
    public int OvertimeMoneyPenalty;
}

/// <summary>
/// The single source of truth for mission settlement math
/// (registry formula: settlement_reward). Pure and EditMode-testable.
///
/// Order of operations:
///   base(resultKind) [+ bonus money unless Failed]
///   − overtime money penalty (all result kinds)
/// </summary>
public static class MissionRewardCalculator
{
    public const float PartialMoneyFraction = 0.22f;

    public static MissionRewardResult Calculate(
        OfficeTaskDefinition task,
        MvpMissionResultKind resultKind,
        float missionTimerSeconds,
        bool bonusEvidenceCollected,
        MissionRewardFallbacks fallbacks,
        MissionRewardBonus bonus)
    {
        var result = new MissionRewardResult
        {
            Money = GetMoneyForResult(task, resultKind, bonusEvidenceCollected, fallbacks, bonus),
            OvertimeGameHours = MvpMissionClock.GetOvertimeGameHours(task, missionTimerSeconds),
            OvertimeMoneyPenalty = MvpMissionClock.GetOvertimeMoneyPenalty(task, missionTimerSeconds)
        };

        result.Money -= result.OvertimeMoneyPenalty;

        return result;
    }

    static int GetMoneyForResult(OfficeTaskDefinition task, MvpMissionResultKind resultKind,
        bool bonusCollected, MissionRewardFallbacks fallbacks, MissionRewardBonus bonus)
    {
        switch (resultKind)
        {
            case MvpMissionResultKind.Success:
                return GetFullMoney(task, fallbacks) + GetBonusMoney(resultKind, bonusCollected, bonus);
            case MvpMissionResultKind.Partial:
                return GetPartialMoney(task, fallbacks) + GetBonusMoney(resultKind, bonusCollected, bonus);
            default:
                return task != null ? task.failureConsolationMoney : fallbacks.failureMoney;
        }
    }

    static int GetFullMoney(OfficeTaskDefinition task, MissionRewardFallbacks fallbacks) =>
        task != null ? task.moneyReward : fallbacks.moneyReward;

    static int GetPartialMoney(OfficeTaskDefinition task, MissionRewardFallbacks fallbacks) =>
        task != null
            ? Mathf.Max(task.failureConsolationMoney, Mathf.RoundToInt(task.moneyReward * PartialMoneyFraction))
            : fallbacks.partialMoneyReward;

    static int GetBonusMoney(MvpMissionResultKind resultKind, bool bonusCollected, MissionRewardBonus bonus) =>
        bonusCollected && resultKind != MvpMissionResultKind.Failed ? bonus.money : 0;
}
