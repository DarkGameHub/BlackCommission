using UnityEngine;

public static class MonsterBestiaryProgress
{
    const string HomeworkDebtCollectorEncounteredKey = "AS.Bestiary.HomeworkDebtCollector.Encountered";
    const string HomeworkDebtCollectorTraceKey = "AS.Bestiary.HomeworkDebtCollector.Trace";

    public static bool HasEncounteredHomeworkDebtCollector =>
        PlayerPrefs.GetInt(HomeworkDebtCollectorEncounteredKey, 0) == 1;

    public static bool HasHomeworkDebtCollectorTrace =>
        PlayerPrefs.GetInt(HomeworkDebtCollectorTraceKey, 0) == 1;

    public static bool IsHomeworkDebtCollectorUnlocked =>
        HasEncounteredHomeworkDebtCollector && HasHomeworkDebtCollectorTrace;

    public static void MarkHomeworkDebtCollectorEncountered()
    {
        if (HasEncounteredHomeworkDebtCollector) return;
        PlayerPrefs.SetInt(HomeworkDebtCollectorEncounteredKey, 1);
        PlayerPrefs.Save();
    }

    public static bool TryCollectHomeworkDebtCollectorTrace()
    {
        if (!HasEncounteredHomeworkDebtCollector)
            return false;

        if (!HasHomeworkDebtCollectorTrace)
        {
            PlayerPrefs.SetInt(HomeworkDebtCollectorTraceKey, 1);
            PlayerPrefs.Save();
        }

        return true;
    }
}
