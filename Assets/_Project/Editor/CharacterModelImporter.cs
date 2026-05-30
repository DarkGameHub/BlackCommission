using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Turns the Meshy-generated character FBX + its PBR texture set into normalized,
/// URP-ready player prefabs under Resources/GeneratedArt (AS_Character_01..06).
///
/// Meshy exports the maps as loose PNGs (albedo / normal / metallic / roughness /
/// emission) rather than embedding them, so we build a URP/Lit material by hand and
/// assign it to the mesh — far more reliable than letting the FBX importer guess.
///
/// The mesh also tends to import lying down (Z-up vs Y-up) at an arbitrary scale, so
/// we auto-stand it and fit it to ~1.8m with the feet on the origin.
///
/// Today all six slots share this one model/material. Give each its own look later by
/// dropping more FBX+texture sets in and pointing each spec at its own files.
/// </summary>
public static class CharacterModelImporter
{
    const string Folder = "Assets/_Project/Art/Generated/Characters_v1";
    const string SourceFbx = Folder + "/AS_Character_Base.fbx";
    const string TextureFolder = Folder + "/Textures";
    const string MaterialPath = Folder + "/AS_Character.mat";

    const string AlbedoTex = TextureFolder + "/AS_Character_albedo.png";
    const string NormalTex = TextureFolder + "/AS_Character_normal.png";
    const string MetallicTex = TextureFolder + "/AS_Character_metallic.png";
    const string EmissionTex = TextureFolder + "/AS_Character_emission.png";

    const string PrefabFolder = "Assets/_Project/Prefabs/Art";
    const string ResourcesFolder = "Assets/Resources/GeneratedArt";

    // Player root pivot is at the feet; a ~2m CharacterController wants a body a touch
    // under 2m so the head clears.
    const float TargetHeight = 1.8f;

    // Front/back facing tweak applied on top of the auto-upright pass.
    // If the character faces away from its walk direction, set this to (0,180,0).
    static readonly Vector3 ModelEuler = Vector3.zero;

    static readonly string[] SlotNames =
    {
        "AS_Character_01", "AS_Character_02", "AS_Character_03",
        "AS_Character_04", "AS_Character_05", "AS_Character_06",
    };

    static bool isBuilding;

    // Auto-rebuild on editor load / script recompile so the prefabs never go stale
    // behind a manual menu click — if the FBX, textures, or material are newer than
    // the generated prefabs (or any prefab is missing), regenerate them silently.
    [InitializeOnLoadMethod]
    static void AutoBuildOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (isBuilding) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (AssetImporter.GetAtPath(SourceFbx) == null) return;
            if (PrefabsAreStale())
                Build(showDialog: false);
        };
    }

    [MenuItem("Tools/Accident Squad/Art/Build Player Character Prefabs")]
    public static void BuildPlayerCharacterPrefabs()
    {
        Build(showDialog: true);
    }

    static void Build(bool showDialog)
    {
        if (isBuilding) return;
        if (AssetImporter.GetAtPath(SourceFbx) == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Character import",
                    $"Source FBX not found:\n{SourceFbx}\n\nDrop the model there and run again.", "OK");
            return;
        }

        isBuilding = true;
        try
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(ResourcesFolder);

            ConfigureModelImport();
            ConfigureTextureImports();
            Material material = BuildUrpMaterial();

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFbx);
            if (model == null)
            {
                if (showDialog)
                    EditorUtility.DisplayDialog("Character import", "Failed to load model after import.", "OK");
                return;
            }

            int built = 0;
            foreach (string slot in SlotNames)
            {
                if (BuildNormalizedPrefab(model, material, slot))
                    built++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CharacterModelImporter] Rebuilt {built}/{SlotNames.Length} textured character prefabs.");

            if (showDialog)
                EditorUtility.DisplayDialog("Character import",
                    $"Built {built}/{SlotNames.Length} textured character prefabs into:\n{ResourcesFolder}\n\n" +
                    "Enter the main menu to preview.", "OK");
        }
        finally
        {
            isBuilding = false;
        }
    }

    static bool PrefabsAreStale()
    {
        string materialFull = Path.GetFullPath(MaterialPath);
        if (!File.Exists(materialFull)) return true;

        string[] sources = { SourceFbx, AlbedoTex, NormalTex, MetallicTex, EmissionTex, MaterialPath };
        System.DateTime newestSource = System.DateTime.MinValue;
        foreach (string src in sources)
        {
            string full = Path.GetFullPath(src);
            if (File.Exists(full))
            {
                var t = File.GetLastWriteTimeUtc(full);
                if (t > newestSource) newestSource = t;
            }
        }

        foreach (string slot in SlotNames)
        {
            string prefabFull = Path.GetFullPath($"{ResourcesFolder}/{slot}.prefab");
            if (!File.Exists(prefabFull)) return true;
            if (File.GetLastWriteTimeUtc(prefabFull) < newestSource) return true;
        }
        return false;
    }

    static void ConfigureModelImport()
    {
        var importer = AssetImporter.GetAtPath(SourceFbx) as ModelImporter;
        if (importer == null) return;

        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.addCollider = false;
        importer.isReadable = true;            // needed to read bounds reliably
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.generateSecondaryUV = false;
        importer.weldVertices = true;
        importer.useFileScale = true;
        // We supply our own URP material, so don't let the FBX create magenta ones.
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
    }

    static void ConfigureTextureImports()
    {
        // Albedo & emission are colour data (sRGB); the rest are data maps (linear).
        SetTexture(AlbedoTex, isNormal: false, sRGB: true);
        SetTexture(NormalTex, isNormal: true, sRGB: false);
        SetTexture(MetallicTex, isNormal: false, sRGB: false);
        SetTexture(EmissionTex, isNormal: false, sRGB: true);
    }

    static void SetTexture(string path, bool isNormal, bool sRGB)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (!isNormal) ti.sRGBTexture = sRGB;
        ti.SaveAndReimport();
    }

    static Material BuildUrpMaterial()
    {
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null)
        {
            Debug.LogError("[CharacterModelImporter] URP/Lit shader not found.");
            return null;
        }

        var mat = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (mat == null)
        {
            mat = new Material(urp);
            AssetDatabase.CreateAsset(mat, MaterialPath);
        }
        else
        {
            mat.shader = urp;
        }

        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoTex);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalTex);
        var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicTex);
        var emission = AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionTex);

        mat.SetColor("_BaseColor", Color.white);
        if (albedo != null) mat.SetTexture("_BaseMap", albedo);

        if (normal != null)
        {
            mat.SetTexture("_BumpMap", normal);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (metallic != null)
        {
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Metallic", 1f);
        }
        // URP/Lit can't take a standalone roughness map; use a modest fixed smoothness.
        mat.SetFloat("_Smoothness", 0.35f);

        if (emission != null)
        {
            mat.SetTexture("_EmissionMap", emission);
            mat.SetColor("_EmissionColor", Color.white);
            mat.EnableKeyword("_EMISSION");
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        }

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static bool BuildNormalizedPrefab(GameObject model, Material material, string slotName)
    {
        var wrapper = new GameObject(slotName);
        try
        {
            var modelInstance = PrefabUtility.InstantiatePrefab(model) as GameObject;
            if (modelInstance == null) modelInstance = Object.Instantiate(model);

            modelInstance.name = $"{slotName}_Model";
            modelInstance.transform.SetParent(wrapper.transform, false);
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;
            modelInstance.transform.localPosition = Vector3.zero;

            AutoStandUpright(modelInstance);
            modelInstance.transform.localRotation = Quaternion.Euler(ModelEuler) * modelInstance.transform.localRotation;
            NormalizeToFeetOrigin(modelInstance);

            foreach (var renderer in wrapper.GetComponentsInChildren<Renderer>())
            {
                if (material != null)
                {
                    var mats = renderer.sharedMaterials;
                    for (int i = 0; i < mats.Length; i++) mats[i] = material;
                    renderer.sharedMaterials = mats;
                }
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }

            PrefabUtility.SaveAsPrefabAsset(wrapper, $"{PrefabFolder}/{slotName}.prefab");
            PrefabUtility.SaveAsPrefabAsset(wrapper, $"{ResourcesFolder}/{slotName}.prefab");
            return true;
        }
        finally
        {
            Object.DestroyImmediate(wrapper);
        }
    }

    // Meshy FBX often imports lying down. A standing humanoid is far taller than wide,
    // so rotate whichever axis is currently longest to point up.
    static void AutoStandUpright(GameObject modelInstance)
    {
        if (!TryGetBounds(modelInstance, out Bounds b)) return;
        Vector3 s = b.size;
        if (s.y >= s.x && s.y >= s.z) return;        // already tallest in Y
        if (s.z >= s.x && s.z >= s.y)                // lying along Z → tip up around X
            modelInstance.transform.Rotate(-90f, 0f, 0f, Space.World);
        else                                          // lying along X → tip up around Z
            modelInstance.transform.Rotate(0f, 0f, 90f, Space.World);
    }

    // Scale to TargetHeight, drop feet to y=0, center on x/z.
    static void NormalizeToFeetOrigin(GameObject modelInstance)
    {
        if (!TryGetBounds(modelInstance, out Bounds bounds)) return;
        if (bounds.size.y <= 0.0001f) return;

        float scale = TargetHeight / bounds.size.y;
        modelInstance.transform.localScale = Vector3.one * scale;

        TryGetBounds(modelInstance, out bounds); // recompute after scaling
        Vector3 worldOffset = new(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        modelInstance.transform.position += worldOffset;
    }

    static bool TryGetBounds(GameObject root, out Bounds bounds)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        bounds = default;
        if (renderers.Length == 0) return false;
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    static void EnsureFolder(string folderPath)
    {
        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
