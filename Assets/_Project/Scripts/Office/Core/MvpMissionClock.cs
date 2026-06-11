using UnityEngine;

public static class MvpMissionClock
{
    public const float DefaultMissionStartClockHour = 8f;
    public const float DefaultContractWindowGameHours = 12f;
    public const float DefaultRealSecondsPerGameHour = 60f;
    public const int DefaultOvertimeMoneyPenaltyPerGameHour = 30;
    public const float DefaultOvertimeReputationPenaltyBlockGameHours = 2f;
    public const int DefaultOvertimeReputationPenaltyPerBlock = 1;

    public static float GetStartClockHour(OfficeTaskDefinition task) =>
        task != null ? task.missionStartClockHour : DefaultMissionStartClockHour;

    public static float GetContractWindowGameHours(OfficeTaskDefinition task) =>
        Mathf.Max(0.25f, task != null ? task.contractWindowGameHours : DefaultContractWindowGameHours);

    public static float GetRealSecondsPerGameHour(OfficeTaskDefinition task) =>
        Mathf.Max(1f, task != null ? task.realSecondsPerGameHour : DefaultRealSecondsPerGameHour);

    public static float GetElapsedGameHours(OfficeTaskDefinition task, float elapsedRealSeconds) =>
        Mathf.Max(0f, elapsedRealSeconds) / GetRealSecondsPerGameHour(task);

    public static float GetCurrentClockHour(OfficeTaskDefinition task, float elapsedRealSeconds) =>
        GetStartClockHour(task) + GetElapsedGameHours(task, elapsedRealSeconds);

    public static float GetDeadlineClockHour(OfficeTaskDefinition task) =>
        GetStartClockHour(task) + GetContractWindowGameHours(task);

    public static float GetRemainingGameHours(OfficeTaskDefinition task, float elapsedRealSeconds) =>
        Mathf.Max(0f, GetContractWindowGameHours(task) - GetElapsedGameHours(task, elapsedRealSeconds));

    public static float GetOvertimeGameHours(OfficeTaskDefinition task, float elapsedRealSeconds) =>
        Mathf.Max(0f, GetElapsedGameHours(task, elapsedRealSeconds) - GetContractWindowGameHours(task));

    public static int GetOvertimeMoneyPenalty(OfficeTaskDefinition task, float elapsedRealSeconds)
    {
        float overtime = GetOvertimeGameHours(task, elapsedRealSeconds);
        if (overtime <= 0f) return 0;

        int rate = task != null
            ? Mathf.Max(0, task.overtimeMoneyPenaltyPerGameHour)
            : DefaultOvertimeMoneyPenaltyPerGameHour;
        return Mathf.CeilToInt(overtime) * rate;
    }

    public static int GetOvertimeReputationPenalty(OfficeTaskDefinition task, float elapsedRealSeconds)
    {
        float overtime = GetOvertimeGameHours(task, elapsedRealSeconds);
        if (overtime <= 0f) return 0;

        float blockHours = task != null
            ? Mathf.Max(0.25f, task.overtimeReputationPenaltyBlockGameHours)
            : DefaultOvertimeReputationPenaltyBlockGameHours;
        int penalty = task != null
            ? Mathf.Max(0, task.overtimeReputationPenaltyPerBlock)
            : DefaultOvertimeReputationPenaltyPerBlock;

        return Mathf.FloorToInt(overtime / blockHours) * penalty;
    }

    public static string FormatClock(float absoluteHour)
    {
        int totalMinutes = Mathf.FloorToInt(Mathf.Max(0f, absoluteHour) * 60f + 0.5f);
        int day = totalMinutes / (24 * 60);
        int minutesOfDay = totalMinutes % (24 * 60);
        int hour = minutesOfDay / 60;
        int minute = minutesOfDay % 60;

        if (day == 0)
            return $"{hour:00}:{minute:00}";
        if (day == 1)
            return $"Next day {hour:00}:{minute:00}";
        return $"Day {day + 1} {hour:00}:{minute:00}";
    }

    public static string FormatGameHours(float gameHours)
    {
        if (gameHours <= 0f) return "0h";
        int wholeHours = Mathf.FloorToInt(gameHours);
        int minutes = Mathf.RoundToInt((gameHours - wholeHours) * 60f);
        if (minutes >= 60)
        {
            wholeHours++;
            minutes -= 60;
        }

        return minutes == 0 ? $"{wholeHours}h" : $"{wholeHours}h{minutes:00}m";
    }

    public static string FormatRealDurationForWindow(OfficeTaskDefinition task)
    {
        float realMinutes = GetContractWindowGameHours(task) * GetRealSecondsPerGameHour(task) / 60f;
        if (realMinutes < 1f)
            return $"{Mathf.RoundToInt(realMinutes * 60f)}s";
        if (Mathf.Abs(realMinutes - Mathf.Round(realMinutes)) < 0.05f)
            return $"{Mathf.RoundToInt(realMinutes)}min";
        return $"{realMinutes:0.0}min";
    }

    public static string GetScheduleSummary(OfficeTaskDefinition task)
    {
        return $"{FormatClock(GetStartClockHour(task))} - {FormatClock(GetDeadlineClockHour(task))} " +
            $"({FormatGameHours(GetContractWindowGameHours(task))} / ~{FormatRealDurationForWindow(task)})";
    }

    public static string GetOvertimeRuleSummary(OfficeTaskDefinition task)
    {
        int money = task != null
            ? Mathf.Max(0, task.overtimeMoneyPenaltyPerGameHour)
            : DefaultOvertimeMoneyPenaltyPerGameHour;
        float repBlock = task != null
            ? Mathf.Max(0.25f, task.overtimeReputationPenaltyBlockGameHours)
            : DefaultOvertimeReputationPenaltyBlockGameHours;
        int rep = task != null
            ? Mathf.Max(0, task.overtimeReputationPenaltyPerBlock)
            : DefaultOvertimeReputationPenaltyPerBlock;

        return $"Overtime: -{money}G per overtime task hour, -{rep} reputation every {FormatGameHours(repBlock)}";
    }

    public static string GetDaylightLabel(float absoluteClockHour)
    {
        float hour = Mathf.Repeat(absoluteClockHour, 24f);
        if (hour < 5.5f) return "Late Night";
        if (hour < 8f) return "Early Morning";
        if (hour < 12f) return "Morning";
        if (hour < 16.5f) return "Afternoon";
        if (hour < 19.5f) return "Dusk";
        if (hour < 22f) return "Evening";
        return "Late Night";
    }
}
