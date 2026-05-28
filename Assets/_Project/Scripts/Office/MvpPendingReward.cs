public enum MvpMissionResultKind
{
    Success,
    Partial,
    Failed
}

public static class MvpPendingReward
{
    public static bool HasPending { get; private set; }
    public static int Money { get; private set; }
    public static int Reputation { get; private set; }
    public static int Experience { get; private set; }
    public static bool Success { get; private set; }
    public static float ElapsedSeconds { get; private set; }
    public static bool CountsTowardLostItemProgress { get; private set; }
    public static MvpMissionResultKind ResultKind { get; private set; } = MvpMissionResultKind.Failed;
    public static string ResultLabel
    {
        get
        {
            switch (ResultKind)
            {
                case MvpMissionResultKind.Success:
                    return "完成";
                case MvpMissionResultKind.Partial:
                    return "部分结算";
                default:
                    return "失败";
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
        HasPending = true;
        Money = money;
        Reputation = reputation;
        Experience = experience;
        Success = success;
        ElapsedSeconds = elapsedSeconds;
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
        CountsTowardLostItemProgress = false;
        ResultKind = MvpMissionResultKind.Failed;
    }
}
