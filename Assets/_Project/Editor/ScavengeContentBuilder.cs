using UnityEngine;
using UnityEditor;
using BlackCommission.Scavenge;

/// <summary>
/// Creates the scavenging config asset (spec-locked knobs) plus a couple of clearly
/// labelled PLACEHOLDER sample items, so the data layer has something concrete to load.
/// The real 12-category roster and balanced values are deferred to the economy/balance
/// pass (PM-owned) — this tool intentionally does not invent economy numbers.
///
/// Menu: Tools > Black Commission > MVP > Scavenge > Build Config + Sample Items
/// Idempotent: leaves any existing config / sample assets untouched.
/// </summary>
public static class ScavengeContentBuilder
{
    const string ConfigDir = "Assets/Resources/Config";
    const string ItemDir   = "Assets/Resources/Scavenge/Items"; // Resources so LootSpawner can load at runtime

    [MenuItem("Tools/Black Commission/MVP/Scavenge/Build Config + Sample Items")]
    public static void Build()
    {
        EnsureFolders();

        // ScavengingConfig — spec-locked defaults.
        string cfgPath = ConfigDir + "/ScavengingConfig.asset";
        if (AssetDatabase.LoadAssetAtPath<ScavengingConfig>(cfgPath) == null)
            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<ScavengingConfig>(), cfgPath);

        // Two clearly-placeholder sample items (structure demo only; values are PLACEHOLDER).
        CreateSample("SAMPLE_civic_notice", "催缴通知（占位）", ScavengeCategory.CivicDocuments, WeightClass.Light,
            new[] { LootSurface.DeskSurface, LootSurface.ShelfSlot, LootSurface.Cabinet }, 30);
        CreateSample("SAMPLE_old_terminal", "废旧终端（占位）", ScavengeCategory.HouseholdTechnology, WeightClass.Medium,
            new[] { LootSurface.Floor, LootSurface.ShelfSlot, LootSurface.CrateTop }, 80);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[ScavengeContentBuilder] Wrote ScavengingConfig.asset + 2 PLACEHOLDER sample items. " +
                  "Real 12-category roster + balanced values = later economy pass (PM-owned).");
    }

    static void CreateSample(string id, string displayName, ScavengeCategory cat, WeightClass weight,
        LootSurface[] surfaces, int placeholderValue)
    {
        string path = $"{ItemDir}/{id}.asset";
        if (AssetDatabase.LoadAssetAtPath<ScavengeItemDefinition>(path) != null) return;

        var d = ScriptableObject.CreateInstance<ScavengeItemDefinition>();
        d.id = id;
        d.displayName = displayName;
        d.category = cat;
        d.weight = weight;
        d.allowedSurfaces = surfaces;
        d.baseValue = placeholderValue; // PLACEHOLDER — not a balance decision
        AssetDatabase.CreateAsset(d, path);
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Config");
        EnsureFolder("Assets/Resources", "Scavenge");
        EnsureFolder("Assets/Resources/Scavenge", "Items");
    }

    static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
            AssetDatabase.CreateFolder(parent, child);
    }
}
