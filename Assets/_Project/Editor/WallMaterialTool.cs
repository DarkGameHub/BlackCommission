using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Builds URP/Lit concrete material assets from the downloaded AmbientCG map sets
/// (Color / NormalGL / AO) into a Resources folder so office surfaces can load them via
/// Resources.Load (key "Office/MVP_office_wall_concrete"). Materials under Resources/ pull
/// their referenced textures into the build as dependencies.
/// </summary>
public static class WallMaterialTool
{
    struct ConcreteMat
    {
        public string TextureFolder;  // folder holding the loose AmbientCG jpgs
        public string Prefix;         // "<Prefix>_Color.jpg" etc.
        public string MaterialPath;   // under a Resources folder
        public Vector2 Tiling;        // repeats per surface box
    }

    const string MatRoot = "Assets/_Project/Resources/Office";

    static readonly ConcreteMat[] Mats =
    {
        new ConcreteMat
        {
            TextureFolder = "Assets/_Project/Materials/事务所/Concrete044C_2K-JPG",
            Prefix = "Concrete044C_2K-JPG",
            MaterialPath = MatRoot + "/MVP_office_wall_concrete.mat",
            Tiling = new Vector2(1.67f, 2f),
        },
    };

    [InitializeOnLoadMethod]
    static void AutoBuildOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (ConcreteMat m in Mats)
            {
                if (AssetImporter.GetAtPath($"{m.TextureFolder}/{m.Prefix}_Color.jpg") == null) continue;
                // Only build when missing, so hand-tuned tiling on an existing asset is preserved.
                if (AssetDatabase.LoadAssetAtPath<Material>(m.MaterialPath) == null) Build(m);
            }
        };
    }

    [MenuItem("Tools/Accident Squad/Art/Rebuild Concrete Material (wall+floor+ceiling)")]
    public static void RebuildMenu()
    {
        int built = 0;
        foreach (ConcreteMat m in Mats)
        {
            if (AssetImporter.GetAtPath($"{m.TextureFolder}/{m.Prefix}_Color.jpg") == null)
            {
                Debug.LogWarning($"[WallMaterialTool] Missing textures, skipped: {m.TextureFolder}");
                continue;
            }
            if (Build(m) != null) built++;
        }
        EditorUtility.DisplayDialog("Concrete materials",
            $"Rebuilt {built} material(s) under {MatRoot}.\n" +
            "Enter/exit Play (or Tools ▸ Black Commission ▸ Refresh HQ) to see the surfaces update.\n\n" +
            "Note: this resets tiling to the tool defaults.", "OK");
    }

    static Material Build(ConcreteMat m)
    {
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null) { Debug.LogError("[WallMaterialTool] URP/Lit shader not found."); return null; }

        ConfigureTextureImports(m);
        EnsureFolder(Path.GetDirectoryName(m.MaterialPath).Replace('\\', '/'));

        var mat = AssetDatabase.LoadAssetAtPath<Material>(m.MaterialPath);
        if (mat == null)
        {
            mat = new Material(urp);
            AssetDatabase.CreateAsset(mat, m.MaterialPath);
        }
        else mat.shader = urp;

        var color = AssetDatabase.LoadAssetAtPath<Texture2D>($"{m.TextureFolder}/{m.Prefix}_Color.jpg");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{m.TextureFolder}/{m.Prefix}_NormalGL.jpg");
        var ao = AssetDatabase.LoadAssetAtPath<Texture2D>($"{m.TextureFolder}/{m.Prefix}_AmbientOcclusion.jpg");

        mat.SetColor("_BaseColor", Color.white);
        if (color != null) mat.SetTexture("_BaseMap", color);
        if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
        if (ao != null) { mat.SetTexture("_OcclusionMap", ao); mat.EnableKeyword("_OCCLUSIONMAP"); }

        mat.SetFloat("_Metallic", 0f);    // concrete: non-metallic, rough
        mat.SetFloat("_Smoothness", 0.1f);

        mat.SetTextureScale("_BaseMap", m.Tiling);
        mat.SetTextureScale("_BumpMap", m.Tiling);
        mat.SetTextureScale("_OcclusionMap", m.Tiling);

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        Debug.Log($"[WallMaterialTool] Built {m.MaterialPath}.");
        return mat;
    }

    static void ConfigureTextureImports(ConcreteMat m)
    {
        SetTexture($"{m.TextureFolder}/{m.Prefix}_Color.jpg", isNormal: false, sRGB: true);
        SetTexture($"{m.TextureFolder}/{m.Prefix}_NormalGL.jpg", isNormal: true, sRGB: false);
        SetTexture($"{m.TextureFolder}/{m.Prefix}_AmbientOcclusion.jpg", isNormal: false, sRGB: false);
    }

    static void SetTexture(string path, bool isNormal, bool sRGB)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        bool changed = false;
        var wantType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (ti.textureType != wantType) { ti.textureType = wantType; changed = true; }
        if (!isNormal && ti.sRGBTexture != sRGB) { ti.sRGBTexture = sRGB; changed = true; }
        if (changed) ti.SaveAndReimport();
    }

    static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
