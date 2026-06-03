using UnityEngine;

public static class PlayerCharacterPalette
{
    public struct CharacterColors
    {
        public Color uniform;
        public Color vest;
        public Color helmet;
        public string label;
    }

    public static readonly Color Skin = new(0.56f, 0.40f, 0.30f);
    public static readonly Color Boots = new(0.067f, 0.078f, 0.075f);

    public const int Count = 6;

    public static readonly CharacterColors[] Variants =
    {
        new() { uniform = new Color(0.17f, 0.20f, 0.17f), vest = new Color(0.85f, 0.60f, 0.19f), helmet = new Color(0.79f, 0.76f, 0.67f), label = "Standard" },
        new() { uniform = new Color(0.14f, 0.16f, 0.22f), vest = new Color(0.82f, 0.38f, 0.15f), helmet = new Color(0.79f, 0.76f, 0.67f), label = "Night Shift" },
        new() { uniform = new Color(0.22f, 0.16f, 0.11f), vest = new Color(0.88f, 0.82f, 0.25f), helmet = new Color(0.22f, 0.30f, 0.22f), label = "Maintenance" },
        new() { uniform = new Color(0.16f, 0.17f, 0.16f), vest = new Color(0.48f, 0.81f, 0.35f), helmet = new Color(0.72f, 0.72f, 0.70f), label = "Sanitation" },
        new() { uniform = new Color(0.24f, 0.12f, 0.11f), vest = new Color(0.85f, 0.60f, 0.19f), helmet = new Color(0.35f, 0.25f, 0.16f), label = "Veteran" },
        new() { uniform = new Color(0.12f, 0.20f, 0.15f), vest = new Color(0.68f, 0.70f, 0.68f), helmet = new Color(0.79f, 0.76f, 0.67f), label = "Rookie" },
    };

    public static CharacterColors Get(int index)
    {
        return Variants[Mathf.Clamp(index, 0, Count - 1)];
    }

    public static int SavedIndex
    {
        get => Mathf.Clamp(PlayerPrefs.GetInt("AS.Character.Index", 0), 0, Count - 1);
        set => PlayerPrefs.SetInt("AS.Character.Index", Mathf.Clamp(value, 0, Count - 1));
    }
}
