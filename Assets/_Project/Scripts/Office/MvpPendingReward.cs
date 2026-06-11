// MvpMissionResultKind lives in Office/Core/MvpMissionResultKind.cs (BlackCommission.Office.Core).
public static class MvpPendingReward
{
    public static bool HasPending { get; private set; }
    public static int Money { get; private set; }
    public static int Reputation { get; private set; }
    public static int Experience { get; private set; }
    public static bool Success { get; private set; }
    public static float ElapsedSeconds { get; private set; }
    public static float OvertimeGameHours { get; private set; }
    public static int OvertimeMoneyPenalty { get; private set; }
    public static int OvertimeReputationPenalty { get; private set; }
    public static bool CountsTowardLostItemProgress { get; private set; }
    public static MvpMissionResultKind ResultKind { get; private set; } = MvpMissionResultKind.Failed;
    public static bool HasOvertimePenalty => OvertimeMoneyPenalty > 0 || OvertimeReputationPenalty > 0;
    public static string ResultLabel
    {
        get
        {
            switch (ResultKind)
            {
                case MvpMissionResultKind.Success:
                    return "Complete";
                case MvpMissionResultKind.Partial:
                    return "Partial Settlement";
                default:
                    return "Failed";
            }
        }
    }

    public static void Set(int money, int reputation, int experience, bool success, float elapsedSeconds,
        bool countsTowardLostItemProgress = true)
    {
        Set(money, reputation, experience, success, elapsedSeconds, countsTowardLostItemProgress,
            success ? MvpMissionResultKind.Success : MvpMissionResultKind.Failed);
    }

    public static void Set(int money, int reputation, int experience, bool success, float elapsedSeconds,
        bool countsTowardLostItemProgress, MvpMissionResultKind resultKind)
    {
        Set(money, reputation, experience, success, elapsedSeconds, countsTowardLostItemProgress,
            resultKind, 0f, 0, 0);
    }

    public static void Set(
        int money,
        int reputation,
        int experience,
        bool success,
        float elapsedSeconds,
        bool countsTowardLostItemProgress,
        MvpMissionResultKind resultKind,
        float overtimeGameHours,
        int overtimeMoneyPenalty,
        int overtimeReputationPenalty)
    {
        HasPending = true;
        Money = money;
        Reputation = reputation;
        Experience = experience;
        Success = success;
        ElapsedSeconds = elapsedSeconds;
        OvertimeGameHours = overtimeGameHours;
        OvertimeMoneyPenalty = overtimeMoneyPenalty;
        OvertimeReputationPenalty = overtimeReputationPenalty;
        CountsTowardLostItemProgress = countsTowardLostItemProgress;
        ResultKind = resultKind;
    }

    public static bool Claim()
    {
        if (!HasPending) return false;

        CompanyData.Current.ApplyMissionResult(
            Success,
            Money,
            Reputation,
            Experience,
            ElapsedSeconds,
            CountsTowardLostItemProgress,
            ResultKind);

        CompanyData.Save();
        Clear();
        return true;
    }

    public static void Clear()
    {
        HasPending = false;
        Money = 0;
        Reputation = 0;
        Experience = 0;
        Success = false;
        ElapsedSeconds = 0f;
        OvertimeGameHours = 0f;
        OvertimeMoneyPenalty = 0;
        OvertimeReputationPenalty = 0;
        CountsTowardLostItemProgress = false;
        ResultKind = MvpMissionResultKind.Failed;
    }
}
