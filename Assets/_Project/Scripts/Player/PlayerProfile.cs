using UnityEngine;

/// <summary>
/// Local-only player identity (display name). Stored in PlayerPrefs and read once at
/// spawn into PlayerController.DisplayName, which syncs it to every peer.
/// </summary>
public static class PlayerProfile
{
    const string NameKey = "AS.Player.Name";

    // Synced as FixedString64Bytes (~61 UTF-8 bytes). 10 chars fits even for CJK
    // (3 bytes each = 30 bytes) with room to spare.
    public const int MaxLength = 10;

    public static string Name
    {
        get
        {
            string stored = PlayerPrefs.GetString(NameKey, "");
            if (!string.IsNullOrWhiteSpace(stored)) return stored;

            // First run: assign a stable random default so teammates are distinguishable.
            string generated = $"Agent-{Random.Range(100, 1000)}";
            PlayerPrefs.SetString(NameKey, generated);
            PlayerPrefs.Save();
            return generated;
        }
        set
        {
            PlayerPrefs.SetString(NameKey, Sanitize(value));
            PlayerPrefs.Save();
        }
    }

    /// <summary>Trim, collapse, and clamp a raw name to the safe stored form.</summary>
    public static string Sanitize(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return "";
        raw = raw.Trim();
        if (raw.Length > MaxLength) raw = raw.Substring(0, MaxLength);
        return raw;
    }
}
