public static class MvpPendingReward
{
    public static bool HasPending { get; private set; }
    public static int Money { get; private set; }
    public static int Reputation { get; private set; }
    public static int Experience { get; private set; }
    public static bool Success { get; private set; }
    public static float ElapsedSeconds { get; private set; }
    public static bool CountsTowardLostItemProgress { get; private set; }

    public static void Set(int money, int reputation, int experience, bool success, float elapsedSeconds,
        bool countsTowardLostItemProgress = true)
    {
        HasPending = true;
        Money = money;
        Reputation = reputation;
        Experience = experience;
        Success = success;
        ElapsedSeconds = elapsedSeconds;
        CountsTowardLostItemProgress = countsTowardLostItemProgress;
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
            CountsTowardLostItemProgress);

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
    }
}
