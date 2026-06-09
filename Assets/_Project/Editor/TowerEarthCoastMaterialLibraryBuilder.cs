using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TowerEarthCoastMaterialLibraryBuilder
{
    const string Root = "Assets/_Project/Art/Maps/Tower_EarthCoast_01";

    [MenuItem("Tools/Black Commission/Art/Create Tower EarthCoast 01 Material Library")]
    public static void CreateMaterialLibrary()
    {
        EnsureFolders();

        CreateMaterial("Materials/Architecture/M_T01_Concrete_Slab.mat", new Color(0.52f, 0.54f, 0.52f), 0f, 0.28f);
        CreateMaterial("Materials/Architecture/M_T01_Concrete_WallRaw.mat", new Color(0.70f, 0.70f, 0.66f), 0f, 0.22f);
        CreateMaterial("Materials/Architecture/M_T01_Concrete_DarkVoid.mat", new Color(0.20f, 0.22f, 0.22f), 0f, 0.18f);
        CreateMaterial("Materials/Architecture/M_T01_Plaster_OffWhite.mat", new Color(0.78f, 0.76f, 0.69f), 0f, 0.30f);
        CreateMaterial("Materials/Architecture/M_T01_Tile_LobbyDusty.mat", new Color(0.62f, 0.64f, 0.60f), 0f, 0.35f);
        CreateMaterial("Materials/Architecture/M_T01_Metal_Rust.mat", new Color(0.44f, 0.27f, 0.17f), 0.55f, 0.18f);
        CreateMaterial("Materials/Architecture/M_T01_Rebar_Dark.mat", new Color(0.16f, 0.15f, 0.14f), 0.70f, 0.20f);
        CreateMaterial("Materials/Architecture/M_T01_Wood_Formwork.mat", new Color(0.45f, 0.34f, 0.22f), 0f, 0.24f);
        CreateMaterial("Materials/Architecture/M_T01_Asphalt_Muddy.mat", new Color(0.22f, 0.22f, 0.20f), 0f, 0.16f);
        CreateMaterial("Materials/Architecture/M_T01_Glass_Dirty.mat", new Color(0.52f, 0.68f, 0.68f, 0.55f), 0f, 0.72f);
        CreateMaterial("Materials/Architecture/M_T01_Metal_MilGreen.mat", new Color(0.33f, 0.38f, 0.29f), 0f, 0.25f);
        CreateMaterial("Materials/Architecture/M_T01_Metal_MilGreenFaded.mat", new Color(0.41f, 0.45f, 0.36f), 0f, 0.22f);

        CreateMaterial("Materials/Props/M_T01_Tarp_Khaki.mat", new Color(0.32f, 0.30f, 0.22f), 0f, 0.25f);
        CreateMaterial("Materials/Props/M_T01_Rubble.mat", new Color(0.38f, 0.34f, 0.29f), 0f, 0.20f);
        CreateMaterial("Materials/Props/M_T01_Objective_SandPan.mat", new Color(0.83f, 0.63f, 0.13f), 0f, 0.22f);

        CreateMaterial("Materials/Decals/M_T01_Decal_LeakDark.mat", new Color(0.08f, 0.08f, 0.07f, 0.70f), 0f, 0.12f);
        CreateMaterial("Materials/Decals/M_T01_Decal_WarningYellow.mat", new Color(0.72f, 0.56f, 0.08f, 1f), 0f, 0.35f);
        CreateMaterial("Materials/Decals/M_T01_Decal_StampRed.mat", new Color(0.76f, 0.23f, 0.17f, 1f), 0f, 0.20f);
        CreateMaterial("Materials/Paper/M_T01_Paper_BCForm.mat", new Color(0.84f, 0.80f, 0.68f), 0f, 0.18f);

        CreateAmbientCgMaterials();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TowerEarthCoast01] Material library created/verified.");
    }

    static void EnsureFolders()
    {
        string[] folders =
        {
            "Materials/Architecture",
            "Materials/Props",
            "Materials/Decals",
            "Materials/Paper",
            "Materials/Downloaded",
            "Textures/_Inbox",
            "Textures/Architecture/Concrete",
            "Textures/Architecture/Plaster",
            "Textures/Architecture/Tile",
            "Textures/Architecture/Metal",
            "Textures/Architecture/Glass",
            "Textures/Architecture/Wood",
            "Textures/Exterior",
            "Textures/Props",
            "Textures/Decals",
            "Textures/Utility/TrimSheets",
            "Textures/Utility/Masks",
            "References"
        };

        foreach (string folder in folders)
            Directory.CreateDirectory(Path.Combine(Root, folder));
    }

    static void CreateAmbientCgMaterials()
    {
        var defs = new[]
        {
            new AmbientCgMaterial("Concrete048", "Materials/Downloaded/M_ACG_Concrete048.mat", 0f, 0.28f),
            new AmbientCgMaterial("Concrete034", "Materials/Downloaded/M_ACG_Concrete034.mat", 0f, 0.22f),
            new AmbientCgMaterial("Plaster001", "Materials/Downloaded/M_ACG_Plaster001.mat", 0f, 0.30f),
            new AmbientCgMaterial("PaintedPlaster017", "Materials/Downloaded/M_ACG_PaintedPlaster017.mat", 0f, 0.34f),
            new AmbientCgMaterial("Tiles133D", "Materials/Downloaded/M_ACG_Tiles133D.mat", 0f, 0.40f),
            new AmbientCgMaterial("Metal063", "Materials/Downloaded/M_ACG_Metal063.mat", 0.75f, 0.18f),
            new AmbientCgMaterial("MetalWalkway014", "Materials/Downloaded/M_ACG_MetalWalkway014.mat", 0.65f, 0.20f),
            new AmbientCgMaterial("CorrugatedSteel007A", "Materials/Downloaded/M_ACG_CorrugatedSteel007A.mat", 0.70f, 0.22f),
            new AmbientCgMaterial("Planks037A", "Materials/Downloaded/M_ACG_Planks037A.mat", 0f, 0.24f),
            new AmbientCgMaterial("Asphalt031", "Materials/Downloaded/M_ACG_Asphalt031.mat", 0f, 0.16f),
            new AmbientCgMaterial("Gravel043", "Materials/Downloaded/M_ACG_Gravel043.mat", 0f, 0.18f),
            new AmbientCgMaterial("Facade001", "Materials/Downloaded/M_ACG_Facade001.mat", 0f, 0.25f)
        };

        AssetDatabase.Refresh();
        foreach (var def in defs)
            CreateAmbientCgMaterial(def);
    }

    static void CreateAmbientCgMaterial(AmbientCgMaterial def)
    {
        string textureFolder = FindAssetFolder(def.id);
        if (string.IsNullOrEmpty(textureFolder))
        {
            Debug.LogWarning($"[TowerEarthCoast01] Missing downloaded texture folder for {def.id}.");
            return;
        }

        string assetPath = $"{Root}/{def.materialPath}";
        var material = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader)
            {
                name = Path.GetFileNameWithoutExtension(assetPath)
            };
            AssetDatabase.CreateAsset(material, assetPath);
        }

        Texture2D color = LoadTexture(textureFolder, "Color", colorData: true);
        Texture2D normal = LoadTexture(textureFolder, "NormalGL", colorData: false, normalMap: true);
        Texture2D occlusion = LoadTexture(textureFolder, "AmbientOcclusion", colorData: false);
        Texture2D metallic = LoadTexture(textureFolder, "Metalness", colorData: false);

        if (color != null)
        {
            SetTexture(material, "_BaseMap", color);
            SetTexture(material, "_MainTex", color);
        }

        if (normal != null)
        {
            SetTexture(material, "_BumpMap", normal);
            material.EnableKeyword("_NORMALMAP");
        }

        if (occlusion != null)
            SetTexture(material, "_OcclusionMap", occlusion);

        if (metallic != null)
            SetTexture(material, "_MetallicGlossMap", metallic);

        SetFloat(material, "_Metallic", def.metallic);
        SetFloat(material, "_Smoothness", def.smoothness);
        EditorUtility.SetDirty(material);
    }

    static string FindAssetFolder(string assetId)
    {
        string fullRoot = Path.GetFullPath(Root);
        string[] matches = Directory.GetDirectories(fullRoot, assetId, SearchOption.AllDirectories);
        return matches.Length == 0 ? null : ToAssetPath(matches[0]);
    }

    static Texture2D LoadTexture(string assetFolder, string token, bool colorData, bool normalMap = false)
    {
        string[] files = Directory.GetFiles(assetFolder, "*.jpg", SearchOption.TopDirectoryOnly)
            .Where(path => Path.GetFileNameWithoutExtension(path).Contains(token))
            .ToArray();
        if (files.Length == 0) return null;

        string assetPath = ToAssetPath(files[0]);
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            bool dirty = false;
            if (normalMap && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                dirty = true;
            }
            if (!normalMap && importer.sRGBTexture != colorData)
            {
                importer.sRGBTexture = colorData;
                dirty = true;
            }
            if (dirty)
                importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
    }

    static void CreateMaterial(string relativePath, Color baseColor, float metallic, float smoothness)
    {
        string assetPath = $"{Root}/{relativePath}";
        if (AssetDatabase.LoadAssetAtPath<Material>(assetPath) != null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var material = new Material(shader)
        {
            name = Path.GetFileNameWithoutExtension(assetPath)
        };

        SetColor(material, "_BaseColor", baseColor);
        SetColor(material, "_Color", baseColor);
        SetFloat(material, "_Metallic", metallic);
        SetFloat(material, "_Smoothness", smoothness);

        if (baseColor.a < 0.99f)
        {
            SetFloat(material, "_Surface", 1f);
            SetFloat(material, "_Blend", 0f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.SetOverrideTag("RenderType", "Transparent");
        }

        AssetDatabase.CreateAsset(material, assetPath);
    }

    static void SetColor(Material material, string property, Color value)
    {
        if (material.HasProperty(property))
            material.SetColor(property, value);
    }

    static void SetFloat(Material material, string property, float value)
    {
        if (material.HasProperty(property))
            material.SetFloat(property, value);
    }

    static void SetTexture(Material material, string property, Texture texture)
    {
        if (material.HasProperty(property))
            material.SetTexture(property, texture);
    }

    static string ToAssetPath(string path) =>
        path.Replace("\\", "/").Replace(Path.GetFullPath(".").Replace("\\", "/") + "/", "");

    readonly struct AmbientCgMaterial
    {
        public readonly string id;
        public readonly string materialPath;
        public readonly float metallic;
        public readonly float smoothness;

        public AmbientCgMaterial(string id, string materialPath, float metallic, float smoothness)
        {
            this.id = id;
            this.materialPath = materialPath;
            this.metallic = metallic;
            this.smoothness = smoothness;
        }
    }
}
