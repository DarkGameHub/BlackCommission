using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Imports Meshy-generated office props (FBX + loose PBR PNGs) into URP-ready,
/// normalized prefabs under Resources/GeneratedArt. Same approach as
/// CharacterModelImporter: build a URP/Lit material by hand from the 5 maps, then
/// scale/centre the mesh to a target height with its base on the origin.
///
/// Auto-rebuilds on editor load when the source files are newer than the prefab,
/// so no menu click is needed. First prop: the dispatch computer terminal.
/// </summary>
public static class OfficePropImporter
{
    struct Prop
    {
        public string Fbx;
        public string TextureFolder;
        public string Name;        // albedo etc. prefixed with this
        public string PrefabName;  // Resources/GeneratedArt/<PrefabName>
        public float TargetHeight; // metres, tallest dimension fitted to this
        public Vector3 Euler;      // facing tweak applied before normalize
    }

    const string PrefabFolder = "Assets/_Project/Prefabs/Art";
    const string ResourcesFolder = "Assets/Resources/GeneratedArt";

    static readonly Prop[] Props =
    {
        new Prop
        {
            Fbx = "Assets/_Project/Art/Generated/Office_v1/AS_OfficeComputer.fbx",
            TextureFolder = "Assets/_Project/Art/Generated/Office_v1/Textures",
            Name = "AS_OfficeComputer",
            PrefabName = "AS_OfficeComputer",
            TargetHeight = 1.05f,                // whole desk-with-terminal prop (incl. its own desk)
            Euler = new Vector3(-90f, 0f, 0f),   // Meshy FBX imports lying on its back; stand it up
        },
        new Prop
        {
            Fbx = "Assets/_Project/Art/Generated/Office_v1/AS_OfficeVan.fbx",
            TextureFolder = "Assets/_Project/Art/Generated/Office_v1/Textures",
            Name = "AS_OfficeVan",
            PrefabName = "AS_OfficeVan",
            TargetHeight = 2.0f,                 // van height; length scales proportionally
            Euler = new Vector3(-90f, 0f, 0f),   // same lying-down fix as the other Meshy props
        },
    };

    static bool isBuilding;

    [InitializeOnLoadMethod]
    static void AutoBuildOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (isBuilding || EditorApplication.isPlayingOrWillChangePlaymode) return;
            foreach (Prop p in Props)
            {
                if (AssetImporter.GetAtPath(p.Fbx) == null) continue;
                if (PrefabIsStale(p)) BuildProp(p, showDialog: false);
            }
        };
    }

    [MenuItem("Tools/Accident Squad/Art/Build Office Props")]
    public static void BuildOfficeProps()
    {
        foreach (Prop p in Props) BuildProp(p, showDialog: true);
    }

    static void BuildProp(Prop p, bool showDialog)
    {
        if (isBuilding) return;
        if (AssetImporter.GetAtPath(p.Fbx) == null)
        {
            if (showDialog)
                EditorUtility.DisplayDialog("Office prop import", $"FBX not found:\n{p.Fbx}", "OK");
            return;
        }

        isBuilding = true;
        try
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(ResourcesFolder);

            ConfigureModelImport(p.Fbx);
            ConfigureTextureImports(p);
            Material material = BuildUrpMaterial(p);

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(p.Fbx);
            if (model == null) return;

            var wrapper = new GameObject(p.PrefabName);
            try
            {
                var inst = PrefabUtility.InstantiatePrefab(model) as GameObject ?? Object.Instantiate(model);
                inst.name = $"{p.PrefabName}_Model";
                inst.transform.SetParent(wrapper.transform, false);
                inst.transform.localRotation = Quaternion.Euler(p.Euler);
                inst.transform.localScale = Vector3.one;
                inst.transform.localPosition = Vector3.zero;

                NormalizeToBaseOrigin(inst, p.TargetHeight);

                foreach (var r in wrapper.GetComponentsInChildren<Renderer>())
                {
                    if (material != null)
                    {
                        var mats = r.sharedMaterials;
                        for (int i = 0; i < mats.Length; i++) mats[i] = material;
                        r.sharedMaterials = mats;
                    }
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    r.receiveShadows = true;
                }

                PrefabUtility.SaveAsPrefabAsset(wrapper, $"{PrefabFolder}/{p.PrefabName}.prefab");
                PrefabUtility.SaveAsPrefabAsset(wrapper, $"{ResourcesFolder}/{p.PrefabName}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(wrapper);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[OfficePropImporter] Built prop prefab '{p.PrefabName}'.");

            if (showDialog)
                EditorUtility.DisplayDialog("Office prop import",
                    $"Built {p.PrefabName} into {ResourcesFolder}.", "OK");
        }
        finally
        {
            isBuilding = false;
        }
    }

    static void ConfigureModelImport(string fbx)
    {
        var importer = AssetImporter.GetAtPath(fbx) as ModelImporter;
        if (importer == null) return;
        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.addCollider = false;
        importer.isReadable = true;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.generateSecondaryUV = false;
        importer.weldVertices = true;
        importer.useFileScale = true;
        importer.materialImportMode = ModelImporterMaterialImportMode.None;
        importer.SaveAndReimport();
    }

    static void ConfigureTextureImports(Prop p)
    {
        SetTexture($"{p.TextureFolder}/{p.Name}_albedo.png", isNormal: false, sRGB: true);
        SetTexture($"{p.TextureFolder}/{p.Name}_normal.png", isNormal: true, sRGB: false);
        SetTexture($"{p.TextureFolder}/{p.Name}_metallic.png", isNormal: false, sRGB: false);
        SetTexture($"{p.TextureFolder}/{p.Name}_emission.png", isNormal: false, sRGB: true);
    }

    static void SetTexture(string path, bool isNormal, bool sRGB)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        ti.textureType = isNormal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (!isNormal) ti.sRGBTexture = sRGB;
        ti.SaveAndReimport();
    }

    static Material BuildUrpMaterial(Prop p)
    {
        Shader urp = Shader.Find("Universal Render Pipeline/Lit");
        if (urp == null) return null;

        string matPath = $"Assets/_Project/Art/Generated/Office_v1/{p.Name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(urp);
            AssetDatabase.CreateAsset(mat, matPath);
        }
        else mat.shader = urp;

        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>($"{p.TextureFolder}/{p.Name}_albedo.png");
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>($"{p.TextureFolder}/{p.Name}_normal.png");
        var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>($"{p.TextureFolder}/{p.Name}_metallic.png");
        var emission = AssetDatabase.LoadAssetAtPath<Texture2D>($"{p.TextureFolder}/{p.Name}_emission.png");

        mat.SetColor("_BaseColor", Color.white);
        if (albedo != null) mat.SetTexture("_BaseMap", albedo);
        if (normal != null) { mat.SetTexture("_BumpMap", normal); mat.EnableKeyword("_NORMALMAP"); }
        if (metallic != null)
        {
            mat.SetTexture("_MetallicGlossMap", metallic);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            mat.SetFloat("_Metallic", 1f);
        }
        mat.SetFloat("_Smoothness", 0.4f);
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

    // Fit the tallest dimension to targetHeight, drop the base to y=0, centre x/z.
    static void NormalizeToBaseOrigin(GameObject inst, float targetHeight)
    {
        if (!TryGetBounds(inst, out Bounds b) || b.size.y <= 0.0001f) return;
        float scale = targetHeight / b.size.y;
        inst.transform.localScale = Vector3.one * scale;

        TryGetBounds(inst, out b);
        inst.transform.position += new Vector3(-b.center.x, -b.min.y, -b.center.z);
    }

    static bool TryGetBounds(GameObject root, out Bounds bounds)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        bounds = default;
        if (renderers.Length == 0) return false;
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    static bool PrefabIsStale(Prop p)
    {
        string prefabFull = Path.GetFullPath($"{ResourcesFolder}/{p.PrefabName}.prefab");
        if (!File.Exists(prefabFull)) return true;
        var prefabTime = File.GetLastWriteTimeUtc(prefabFull);

        string[] sources =
        {
            p.Fbx,
            $"{p.TextureFolder}/{p.Name}_albedo.png",
            $"{p.TextureFolder}/{p.Name}_normal.png",
            $"{p.TextureFolder}/{p.Name}_metallic.png",
            $"{p.TextureFolder}/{p.Name}_emission.png",
        };
        foreach (string s in sources)
        {
            string full = Path.GetFullPath(s);
            if (File.Exists(full) && File.GetLastWriteTimeUtc(full) > prefabTime) return true;
        }
        return false;
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
