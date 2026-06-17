using UnityEngine;
using UnityEditor;
using Unity.Netcode;
using BlackCommission.Scavenge;

/// <summary>
/// Builds the 3 whitebox loot prefabs (Light / Medium / Heavy) the LootSpawner instantiates.
/// PM 2026-06-17: minimal visuals — one neutral aged-earth block per WEIGHT class (not per
/// category). Each prefab carries the networked carry stack: Rigidbody + NetworkObject +
/// ScavengeItem (a Carriable subclass). Heavy = two-hand carry (Carriable.isHeavy) + bigger/heavier.
/// Output: Assets/Resources/Loot/ so LootSpawner can Resources.Load them at runtime.
///
/// Menu: Tools > Black Commission > MVP > Scavenge > Build Whitebox Loot Props
/// Run again to rebuild (overwrites the 3 prefabs). After building, confirm the prefabs are
/// registered in DefaultNetworkPrefabs (NGO auto-adds NetworkObject prefabs on import).
/// </summary>
public static class WhiteboxLootBuilder
{
    const string OutDir  = "Assets/Resources/Loot";
    const string MatPath = OutDir + "/Loot_Whitebox.mat";

    [MenuItem("Tools/Black Commission/MVP/Scavenge/Build Whitebox Loot Props")]
    public static void Build()
    {
        EnsureFolders();
        Material mat = EnsureMaterial();

        //        name           weight              heavy  size (m)                          mass
        BuildProp("Loot_Light",  WeightClass.Light,  false, new Vector3(0.25f, 0.18f, 0.25f), 2f,  mat);
        BuildProp("Loot_Medium", WeightClass.Medium, false, new Vector3(0.40f, 0.34f, 0.40f), 8f,  mat);
        BuildProp("Loot_Heavy",  WeightClass.Heavy,  true,  new Vector3(0.60f, 0.55f, 0.60f), 30f, mat);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WhiteboxLootBuilder] Built 3 loot prefabs (Light/Medium/Heavy) → " + OutDir +
                  "/. Verify they appear in DefaultNetworkPrefabs (NGO auto-add) before a hosted play test.");
    }

    static void BuildProp(string name, WeightClass weight, bool heavy, Vector3 size, float mass, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube); // ships with a BoxCollider
        go.name = name;
        go.transform.localScale = size;
        go.GetComponent<Renderer>().sharedMaterial = mat;

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = mass;

        go.AddComponent<NetworkObject>();
        var item = go.AddComponent<ScavengeItem>();

        // Set the private serialized fields: weightClass (on ScavengeItem) and isHeavy (on the
        // Carriable base). Both are serialized on the same component and found by name.
        var so = new SerializedObject(item);
        so.FindProperty("weightClass").enumValueIndex = WeightIndex(weight);
        so.FindProperty("isHeavy").boolValue = heavy;
        so.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(go, $"{OutDir}/{name}.prefab");
        Object.DestroyImmediate(go);
    }

    // enumValueIndex is the enum's DECLARATION order (Light=0, Medium=1, Heavy=2), not its int value.
    static int WeightIndex(WeightClass w)
    {
        switch (w)
        {
            case WeightClass.Light:  return 0;
            case WeightClass.Medium: return 1;
            case WeightClass.Heavy:  return 2;
            default:                 return 0;
        }
    }

    static Material EnsureMaterial()
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader) { color = new Color(0.42f, 0.37f, 0.29f) }; // neutral aged earth
            AssetDatabase.CreateAsset(mat, MatPath);
        }
        return mat;
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(OutDir))
            AssetDatabase.CreateFolder("Assets/Resources", "Loot");
    }
}
