using UnityEngine;

/// <summary>
/// Per-mission fallback reward values used when no OfficeTaskDefinition is active.
/// Mirrors the inspector defaults on LostItemMissionManager so designers can still
/// tune them per-scene; tests use <see cref="Default"/>.
/// </summary>
[System.Serializable]
public struct MissionRewardFallbacks
{
    public int moneyReward;
    public int reputationReward;
    public int experienceReward;
    public int partialMoneyReward;
    public int partialReputationReward;
    public int partialExperienceReward;
    public int failureMoney;
    public int failureReputation;
    public int failureExperience;

    /// <summary>Registry constants: full_job_reward 300G / partial 60G / failure 20G.</summary>
    public static MissionRewardFallbacks Default => new MissionRewardFallbacks
    {
        moneyReward = 300,
        reputationReward = 5,
        experienceReward = 80,
        partialMoneyReward = 60,
        partialReputationReward = 0,
        partialExperienceReward = 15,
        failureMoney = 20,
        failureReputation = -2,
        failureExperience = 0
    };
}

/// <summary>Optional-objective (bonus evidence) reward values.</summary>
[System.Serializable]
public struct MissionRewardBonus
{
    public int money;
    public int reputation;
    public int experience;

    public static MissionRewardBonus Default => new MissionRewardBonus
    {
        money = 90,
        reputation = 1,
        experience = 20
    };
}

/// <summary>Final settlement numbers handed to MvpPendingReward / the settlement UI.</summary>
public struct MissionRewardResult
{
    public int Money;
    public int Reputation;
    public int Experience;
    public float OvertimeGameHours;
    public int OvertimeMoneyPenalty;
    public int OvertimeReputationPenalty;
}

/// <summary>
/// The single source of truth for mission settlement math
/// (registry formula: settlement_reward). Pure and EditMode-testable; the
/// server-authoritative LostItemMissionManager delegates here so the math cannot
/// drift between the van-departure and exit-point settlement paths.
///
/// Order of operations:
///   base(resultKind) [+ bonus evidence unless Failed]
///   − overtime penalties (all result kinds)
///   − wrong-homework penalty (unless Failed)
/// </summary>
public static class MissionRewardCalculator
{
    public const float PartialMoneyFraction = 0.22f;
    public const float PartialExperienceFraction = 0.2f;
    public const int WrongAttemptMoneyPenalty = 30;
    public const int MaxPenalizedWrongAttempts = 3;

    public static MissionRewardResult Calculate(
        OfficeTaskDefinition task,
        MvpMissionResultKind resultKind,
        float missionTimerSeconds,
        bool bonusEvidenceCollected,
        int wrongHomeworkAttempts,
        MissionRewardFallbacks fallbacks,
        MissionRewardBonus bonus)
    {
        var result = new MissionRewardResult
        {
            Money = GetMoneyForResult(task, resultKind, bonusEvidenceCollected, fallbacks, bonus),
            Reputation = GetReputationForResult(task, resultKind, bonusEvidenceCollected, fallbacks, bonus),
            Experience = GetExperienceForResult(task, resultKind, bonusEvidenceCollected, fallbacks, bonus),
            OvertimeGameHours = MvpMissionClock.GetOvertimeGameHours(task, missionTimerSeconds),
            OvertimeMoneyPenalty = MvpMissionClock.GetOvertimeMoneyPenalty(task, missionTimerSeconds),
            OvertimeReputationPenalty = MvpMissionClock.GetOvertimeReputationPenalty(task, missionTimerSeconds)
        };

        result.Money -= result.OvertimeMoneyPenalty;
        result.Reputation -= result.OvertimeReputationPenalty;
        if (resultKind != MvpMissionResultKind.Failed)
            result.Money -= GetWrongHomeworkMoneyPenalty(wrongHomeworkAttempts);

        return result;
    }

    public static int GetWrongHomeworkMoneyPenalty(int wrongAttempts) =>
        Mathf.Clamp(wrongAttempts, 0, MaxPenalizedWrongAttempts) * WrongAttemptMoneyPenalty;

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

    static int GetReputationForResult(OfficeTaskDefinition task, MvpMissionResultKind resultKind,
        bool bonusCollected, MissionRewardFallbacks fallbacks, MissionRewardBonus bonus)
    {
        switch (resultKind)
        {
            case MvpMissionResultKind.Success:
                return (task != null ? task.reputationReward : fallbacks.reputationReward)
                    + (bonusCollected ? bonus.reputation : 0);
            case MvpMissionResultKind.Partial:
                // Partial returns never grant task reputation; only the no-task fallback applies.
                return task != null ? 0 : fallbacks.partialReputationReward;
            default:
                return task != null ? task.failureReputationPenalty : fallbacks.failureReputation;
        }
    }

    static int GetExperienceForResult(OfficeTaskDefinition task, MvpMissionResultKind resultKind,
        bool bonusCollected, MissionRewardFallbacks fallbacks, MissionRewardBonus bonus)
    {
        switch (resultKind)
        {
            case MvpMissionResultKind.Success:
                return (task != null ? task.experienceReward : fallbacks.experienceReward)
                    + (bonusCollected ? bonus.experience : 0);
            case MvpMissionResultKind.Partial:
                int partial = task != null
                    ? Mathf.Max(0, Mathf.RoundToInt(task.experienceReward * PartialExperienceFraction))
                    : fallbacks.partialExperienceReward;
                return partial + (bonusCollected ? bonus.experience : 0);
            default:
                return task != null ? task.failureExperience : fallbacks.failureExperience;
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
