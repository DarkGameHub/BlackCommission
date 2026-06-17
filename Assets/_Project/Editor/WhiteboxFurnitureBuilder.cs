using UnityEngine;
using UnityEditor;

/// <summary>
/// Generates whitebox furniture prefabs (cube/box assemblies) for the modular room
/// system, reusing the tower's V8 whitebox materials. No 3D modeling required — these
/// are readable placeholder silhouettes. Swapping to art assets later means replacing
/// the prefab refs in the RoomDressingSet ScriptableObjects, not changing this code.
///
/// Menu: Tools > Black Commission > MVP > Modules > Build Whitebox Furniture
/// Output: Assets/_Project/Art/Maps/Shared/Furniture/
/// Run BEFORE 'Dress Room Modules'.
/// </summary>
public static class WhiteboxFurnitureBuilder
{
    const string OutDir  = "Assets/_Project/Art/Maps/Shared/Furniture";
    const string MatBase = "Assets/_Project/Art/Maps/Tower_EarthCoast_01/Materials/Whitebox/";

    static Material wood, steel, rust, paper, glass, offwhite;

    [MenuItem("Tools/Black Commission/MVP/Modules/Build Whitebox Furniture")]
    public static void BuildAll()
    {
        LoadMaterials();
        EnsureOutputDir();

        Save(Desk());
        Save(Cabinet());
        Save(Chair());
        Save(CrtTerminal());
        Save(NoticeBoard());
        Save(Shelving());
        Save(ElectricalPanel());
        Save(PipeRun());
        Save(Crate());
        Save(CableTray());

        AssetDatabase.Refresh();
        Debug.Log($"[WhiteboxFurnitureBuilder] 10 furniture prefabs built → {OutDir}/");
    }

    // ── Pieces (origin at floor unless noted; wall items centred for placement) ──────

    static GameObject Desk()
    {
        var r = Root("Furniture_Desk");
        Box(r, wood,  0f, 0.74f, 0f,    1.40f, 0.05f, 0.70f, "Top");
        Box(r, steel, -0.65f, 0.37f, 0.25f, 0.05f, 0.72f, 0.05f, "Leg_BL");
        Box(r, steel,  0.65f, 0.37f, 0.25f, 0.05f, 0.72f, 0.05f, "Leg_BR");
        Box(r, steel, -0.65f, 0.37f, -0.25f, 0.05f, 0.72f, 0.05f, "Leg_FL");
        Box(r, steel,  0.65f, 0.37f, -0.25f, 0.05f, 0.72f, 0.05f, "Leg_FR");
        Box(r, steel, 0f, 0.45f, 0.32f, 1.30f, 0.45f, 0.03f, "Modesty");
        return r;
    }

    static GameObject Cabinet()
    {
        var r = Root("Furniture_Cabinet");
        Box(r, steel, 0f, 0.66f, 0f, 0.50f, 1.32f, 0.60f, "Body");
        Box(r, rust,  0f, 0.33f, 0.31f, 0.46f, 0.02f, 0.02f, "Seam_1");
        Box(r, rust,  0f, 0.66f, 0.31f, 0.46f, 0.02f, 0.02f, "Seam_2");
        Box(r, rust,  0f, 0.99f, 0.31f, 0.46f, 0.02f, 0.02f, "Seam_3");
        return r;
    }

    static GameObject Chair()
    {
        var r = Root("Furniture_Chair");
        Box(r, steel, 0f, 0.45f, 0f,   0.45f, 0.05f, 0.45f, "Seat");
        Box(r, steel, 0f, 0.70f, -0.20f, 0.45f, 0.45f, 0.05f, "Back");
        Box(r, steel, 0f, 0.22f, 0f,   0.08f, 0.45f, 0.08f, "Post");
        Box(r, steel, 0f, 0.03f, 0f,   0.40f, 0.05f, 0.40f, "Base");
        return r;
    }

    static GameObject CrtTerminal()
    {
        var r = Root("Furniture_CRT");
        Box(r, steel, 0f, 0.19f, 0f,   0.40f, 0.38f, 0.42f, "Body");
        Box(r, glass, 0f, 0.21f, 0.21f, 0.30f, 0.26f, 0.03f, "Screen");
        Box(r, steel, 0f, 0.02f, 0f,   0.34f, 0.04f, 0.34f, "Base");
        return r;
    }

    /// Wall item — centred at origin; placement sets mount height + wall offset.
    static GameObject NoticeBoard()
    {
        var r = Root("Furniture_NoticeBoard");
        Box(r, steel, 0f, 0f, 0f,    1.00f, 0.72f, 0.04f, "Frame");
        Box(r, paper, 0f, 0f, -0.03f, 0.92f, 0.64f, 0.02f, "Paper");
        return r;
    }

    static GameObject Shelving()
    {
        var r = Root("Furniture_Shelving");
        Box(r, steel, -0.48f, 0.90f, 0f, 0.04f, 1.80f, 0.40f, "Up_L");
        Box(r, steel,  0.48f, 0.90f, 0f, 0.04f, 1.80f, 0.40f, "Up_R");
        for (int i = 0; i < 4; i++)
            Box(r, steel, 0f, 0.30f + i * 0.50f, 0f, 0.96f, 0.03f, 0.38f, "Shelf_" + i);
        return r;
    }

    /// Wall item — centred at origin; placement sets mount height + wall offset.
    static GameObject ElectricalPanel()
    {
        var r = Root("Furniture_ElectricalPanel");
        Box(r, steel, 0f, 0f, 0f,    0.60f, 0.90f, 0.18f, "Box");
        Box(r, rust,  0f, 0f, 0.10f, 0.50f, 0.80f, 0.02f, "Door");
        Box(r, steel, 0f, -0.60f, 0f, 0.06f, 0.40f, 0.06f, "Conduit");
        return r;
    }

    /// Wall run — centred at origin; placement sets mount height + wall offset. Spans 2m in X.
    static GameObject PipeRun()
    {
        var r = Root("Furniture_PipeRun");
        Box(r, rust,  0f, 0.06f, 0f,  2.00f, 0.12f, 0.12f, "Pipe_A");
        Box(r, rust,  0f, -0.10f, 0f, 2.00f, 0.10f, 0.10f, "Pipe_B");
        Box(r, steel, -0.80f, 0f, 0f, 0.04f, 0.30f, 0.04f, "Bracket_L");
        Box(r, steel,  0.80f, 0f, 0f, 0.04f, 0.30f, 0.04f, "Bracket_R");
        return r;
    }

    static GameObject Crate()
    {
        var r = Root("Furniture_Crate");
        Box(r, wood, 0f, 0.30f, 0f,    0.60f, 0.60f, 0.60f, "Body");
        Box(r, rust, 0f, 0.30f, 0.31f, 0.62f, 0.05f, 0.02f, "Strap_F");
        Box(r, rust, 0f, 0.30f, -0.31f, 0.62f, 0.05f, 0.02f, "Strap_B");
        return r;
    }

    /// Ceiling run — centred at origin; placement sets ceiling height. Spans 3.6m in X.
    static GameObject CableTray()
    {
        var r = Root("Furniture_CableTray");
        Box(r, steel, 0f, 0f, 0f,     3.60f, 0.04f, 0.30f, "Base");
        Box(r, steel, 0f, 0.06f, 0.14f, 3.60f, 0.12f, 0.03f, "Side_A");
        Box(r, steel, 0f, 0.06f, -0.14f, 3.60f, 0.12f, 0.03f, "Side_B");
        return r;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────────

    static GameObject Root(string name) => new GameObject(name);

    static void Box(GameObject parent, Material mat,
        float cx, float cy, float cz, float w, float h, float d, string name)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.localPosition = new Vector3(cx, cy, cz);
        go.transform.localScale    = new Vector3(w, h, d);
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.sharedMaterial = mat;
    }

    static void Save(GameObject go)
    {
        string path = $"{OutDir}/{go.name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    static void LoadMaterials()
    {
        wood     = L("V8_Wood_Formwork");
        steel    = L("V8_Steel_Dark");
        rust     = L("V8_Steel_Rust");
        paper    = L("V8_Paper_Aged");
        glass    = L("V8_EcoColumn_Glass");
        offwhite = L("V8_Finish_OffWhite");

        var fallback = L("V8_Concrete_Poured");
        if (wood == null)     wood = fallback;
        if (steel == null)    steel = fallback;
        if (rust == null)     rust = steel;
        if (offwhite == null) offwhite = fallback;
        if (paper == null)    paper = offwhite;
        if (glass == null)    glass = steel;

        if (fallback == null && steel == null)
            Debug.LogWarning("[WhiteboxFurnitureBuilder] No V8 whitebox materials found — furniture will use default material. " +
                "Run 'Rebuild v8 Whitebox' first for correct look.");
    }

    static Material L(string filename) =>
        AssetDatabase.LoadAssetAtPath<Material>(MatBase + filename + ".mat");

    static void EnsureOutputDir()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Maps/Shared"))
            AssetDatabase.CreateFolder("Assets/_Project/Art/Maps", "Shared");
        if (!AssetDatabase.IsValidFolder(OutDir))
            AssetDatabase.CreateFolder("Assets/_Project/Art/Maps/Shared", "Furniture");
    }
}
