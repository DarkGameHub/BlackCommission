using UnityEngine;

/// <summary>
/// Local (per-player) record of which monsters this player has encountered, for the office
/// bestiary. File-backed under the save folder. Not networked/shared — each player tracks
/// their own discoveries (true shared-codex would need network sync, a later step).
/// </summary>
public static class MonsterBestiaryProgress
{
    const string SaveFileName = "bestiary.json";
    const string LegacyEncounteredKey = "AS.Bestiary.EchoMold.Encountered";
    const string LegacyTraceKey = "AS.Bestiary.EchoMold.Trace";

    [System.Serializable]
    class BestiaryData
    {
        public bool echoMoldEncountered;
        public bool echoMoldTrace;
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
                bool enc = PlayerPrefs.GetInt(LegacyEncounteredKey, 0) == 1;
                bool trace = PlayerPrefs.GetInt(LegacyTraceKey, 0) == 1;
                if (enc || trace)
                {
                    cached.echoMoldEncountered = enc;
                    cached.echoMoldTrace = trace;
                    PlayerPrefs.DeleteKey(LegacyEncounteredKey);
                    PlayerPrefs.DeleteKey(LegacyTraceKey);
                    PlayerPrefs.Save();
                    SaveIO.WriteJson(SaveFileName, cached);
                }
            }
            return cached;
        }
    }

    public static bool HasEncounteredEchoMold => Data.echoMoldEncountered;
    public static bool HasEchoMoldTrace => Data.echoMoldTrace;
    public static bool IsEchoMoldUnlocked => HasEncounteredEchoMold && HasEchoMoldTrace;

    public static void MarkEchoMoldEncountered()
    {
        if (Data.echoMoldEncountered) return;
        Data.echoMoldEncountered = true;
        SaveIO.WriteJson(SaveFileName, Data);
    }

    public static bool TryCollectEchoMoldTrace()
    {
        if (!Data.echoMoldEncountered) return false;

        if (!Data.echoMoldTrace)
        {
            Data.echoMoldTrace = true;
            SaveIO.WriteJson(SaveFileName, Data);
        }
        return true;
    }
}
