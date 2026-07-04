using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Local (per-player) record of which monster species this player has encountered, for the
/// office bestiary dossiers. File-backed under the save folder. Not networked/shared — each
/// player tracks their own discoveries (host/solo marks fire from the server-side monster
/// brains, so guests unlock only what their own machine witnesses; a true shared codex needs
/// a ClientRpc pass later).
///
/// Species ids: "echo_mold" / "file_warden" / "civic_idol" (see <see cref="SpeciesIds"/>).
/// </summary>
public static class MonsterBestiaryProgress
{
    const string SaveFileName = "bestiary.json";
    const string LegacyEncounteredKey = "AS.Bestiary.EchoMold.Encountered";
    const string LegacyTraceKey = "AS.Bestiary.EchoMold.Trace";

    public const string EchoMold = "echo_mold";
    public const string FileWarden = "file_warden";
    public const string CivicIdol = "civic_idol";

    /// <summary>Dossier order in the bestiary UI.</summary>
    public static readonly string[] SpeciesIds = { EchoMold, FileWarden, CivicIdol };

    [System.Serializable]
    class BestiaryData
    {
        public List<string> encounteredSpecies = new();
        // Legacy single-monster fields — kept so pre-generic saves migrate on first read.
        public bool echoMoldEncountered;
        public bool echoMoldTrace;
    }

    static BestiaryData cached;

    static BestiaryData Data
    {
        get
        {
            if (cached != null) return cached;

            cached = SaveIO.ReadJson<BestiaryData>(SaveFileName) ?? new BestiaryData();
            cached.encounteredSpecies ??= new List<string>();

            // Migrate the legacy EchoMold flags (json field or the even older PlayerPrefs pair).
            bool legacy = cached.echoMoldEncountered
                          || PlayerPrefs.GetInt(LegacyEncounteredKey, 0) == 1
                          || PlayerPrefs.GetInt(LegacyTraceKey, 0) == 1;
            if (legacy && !cached.encounteredSpecies.Contains(EchoMold))
            {
                cached.encounteredSpecies.Add(EchoMold);
                PlayerPrefs.DeleteKey(LegacyEncounteredKey);
                PlayerPrefs.DeleteKey(LegacyTraceKey);
                PlayerPrefs.Save();
                SaveIO.WriteJson(SaveFileName, cached);
            }
            return cached;
        }
    }

    /// <summary>True once this player's machine has witnessed the species hunting.</summary>
    public static bool HasEncountered(string speciesId)
        => Data.encounteredSpecies.Contains(speciesId);

    /// <summary>Idempotent; called by the monster brains the moment they start hunting.</summary>
    public static void MarkEncountered(string speciesId)
    {
        if (string.IsNullOrEmpty(speciesId) || Data.encounteredSpecies.Contains(speciesId)) return;
        Data.encounteredSpecies.Add(speciesId);
        SaveIO.WriteJson(SaveFileName, Data);
    }
}
