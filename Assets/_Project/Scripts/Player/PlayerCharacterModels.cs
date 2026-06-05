using UnityEngine;

/// <summary>
/// Maps each character slot (0..5) to a generated character mesh prefab living
/// under Resources/GeneratedArt. Today all six slots point at the same base
/// model — once distinct meshes exist, just drop them into
/// Assets/_Project/Art/Generated/Characters_v1 and update the names below
/// (the editor importer turns each FBX into a Resources prefab of the same name).
/// </summary>
public static class PlayerCharacterModels
{
    // Resources-relative paths (no extension), loaded via Resources.Load.
    public static readonly string[] ResourceNames =
    {
        "GeneratedArt/Repairman_Model",
        "GeneratedArt/AS_Character_01",
        "GeneratedArt/AS_Character_02",
        "GeneratedArt/AS_Character_03",
        "GeneratedArt/AS_Character_04",
        "GeneratedArt/AS_Character_05",
    };

    public static string Get(int index)
    {
        if (ResourceNames.Length == 0) return null;
        index = Mathf.Clamp(index, 0, ResourceNames.Length - 1);
        return ResourceNames[index];
    }

    // All six slots share one textured mesh; these tints multiply the (sandy) base
    // texture so each character reads as a different colour without a new model.
    // Kept mid-bright so the camo detail still shows through the multiply.
    static readonly Color[] Tints =
    {
        new(1.00f, 1.00f, 1.00f), // 01 Standard  — original desert sand
        new(0.52f, 0.60f, 0.74f), // 02 Night Shift — cool blue-grey
        new(0.96f, 0.83f, 0.45f), // 03 Maintenance — amber/yellow
        new(0.55f, 0.74f, 0.47f), // 04 Sanitation  — green
        new(0.74f, 0.47f, 0.37f), // 05 Veteran     — rust brown
        new(0.74f, 0.75f, 0.72f), // 06 Rookie      — pale grey
    };

    public static Color TintFor(int index)
    {
        if (Tints.Length == 0) return Color.white;
        return Tints[Mathf.Clamp(index, 0, Tints.Length - 1)];
    }
}
