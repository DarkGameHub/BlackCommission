using UnityEngine;

/// <summary>
/// Local (per-player) record of which monsters this player has encountered, for the office
/// bestiary. File-backed under the save folder. Not networked/shared — each player tracks
/// their own discoveries (true shared-codex would need network sync, a later step).
/// </summary>
public static class MonsterBestiaryProgress
{
    const string SaveFileName = "bestiary.json";
    const string LegacyEncounteredKey = "AS.Bestiary.HomeworkDebtCollector.Encountered";
    const string LegacyTraceKey = "AS.Bestiary.HomeworkDebtCollector.Trace";

    [System.Serializable]
    class BestiaryData
    {
        public bool homeworkDebtCollectorEncountered;
        public bool homeworkDebtCollectorTrace;
    }

    static BestiaryData cached;

    static BestiaryData Data
    {
        get
        {
            if (cached != null) return cached;

            cached = SaveIO.ReadJson<BestiaryData>(SaveFileName);
            if (cached == null)
            {
                cached = new BestiaryData();
                // One-time import of the old PlayerPrefs flags.
                bool enc = PlayerPrefs.GetInt(LegacyEncounteredKey, 0) == 1;
                bool trace = PlayerPrefs.GetInt(LegacyTraceKey, 0) == 1;
                if (enc || trace)
                {
                    cached.homeworkDebtCollectorEncountered = enc;
                    cached.homeworkDebtCollectorTrace = trace;
                    PlayerPrefs.DeleteKey(LegacyEncounteredKey);
                    PlayerPrefs.DeleteKey(LegacyTraceKey);
                    PlayerPrefs.Save();
                    SaveIO.WriteJson(SaveFileName, cached);
                }
            }
            return cached;
        }
    }

    public static bool HasEncounteredHomeworkDebtCollector => Data.homeworkDebtCollectorEncountered;
    public static bool HasHomeworkDebtCollectorTrace => Data.homeworkDebtCollectorTrace;
    public static bool IsHomeworkDebtCollectorUnlocked =>
        HasEncounteredHomeworkDebtCollector && HasHomeworkDebtCollectorTrace;

    public static void MarkHomeworkDebtCollectorEncountered()
    {
        if (Data.homeworkDebtCollectorEncountered) return;
        Data.homeworkDebtCollectorEncountered = true;
        SaveIO.WriteJson(SaveFileName, Data);
    }

    public static bool TryCollectHomeworkDebtCollectorTrace()
    {
        if (!Data.homeworkDebtCollectorEncountered) return false;

        if (!Data.homeworkDebtCollectorTrace)
        {
            Data.homeworkDebtCollectorTrace = true;
            SaveIO.WriteJson(SaveFileName, Data);
        }
        return true;
    }
}
