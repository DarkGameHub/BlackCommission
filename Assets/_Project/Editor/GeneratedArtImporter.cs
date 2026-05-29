using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GeneratedArtImporter
{
    const string PrefabFolder = "Assets/_Project/Prefabs/Art";
    const string ResourcesGeneratedArtFolder = "Assets/Resources/GeneratedArt";
    const string PlayableVanPrefabPath = "Assets/_Project/Prefabs/Art/ASV4_PlayableDepartureVan.prefab";
    const string PlayableVanResourcesPath = "Assets/Resources/GeneratedArt/ASV4_PlayableDepartureVan.prefab";
    const string VanModelPath = "Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4/ASV4_Second_Hand_Dispatch_Van.fbx";
    const string AutoSetupDoneKey = "AccidentSquad.GeneratedArt.ASV4AutoSetupDone.v2";

    struct AssetSpec
    {
        public readonly string ModelPath;
        public readonly string PrefabName;
        public readonly Vector3 GalleryPosition;
        public readonly Vector3 PrefabScale;

        public AssetSpec(string modelPath, string prefabName, Vector3 galleryPosition)
            : this(modelPath, prefabName, galleryPosition, Vector3.one)
        {
        }

        public AssetSpec(string modelPath, string prefabName, Vector3 galleryPosition, Vector3 prefabScale)
        {
            ModelPath = modelPath;
            PrefabName = prefabName;
            GalleryPosition = galleryPosition;
            PrefabScale = prefabScale;
        }

        public string PrefabPath => $"{PrefabFolder}/{PrefabName}.prefab";
    }

    static readonly AssetSpec[] Specs =
    {
        new AssetSpec(
            "Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4/ASV4_HQ_Rundown_Commission_Office.fbx",
            "ASV4_HQ_RundownCommissionOffice",
            new Vector3(0f, 0f, 0f)),
        new AssetSpec(
            "Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4/ASV4_School_Lost_Item_Map.fbx",
            "ASV4_SchoolLostItemMap",
            new Vector3(0f, 0f, 9f)),
        new AssetSpec(
            "Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4/ASV4_Worker_Cheap_Outsourced_Uniform.fbx",
            "ASV4_WorkerCheapOutsourcedUniform",
            new Vector3(-5f, 0f, -4f),
            Vector3.one * 1.08f),
        new AssetSpec(
            "Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4/ASV4_Monster_Homework_Debt_Collector.fbx",
            "ASV4_MonsterHomeworkDebtCollector",
            new Vector3(-3f, 0f, -4f)),
        new AssetSpec(
            "Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4/ASV4_Missing_Homework_Notebook.fbx",
            "ASV4_MissingHomeworkNotebook",
            new Vector3(-1f, 0f, -4f)),
        new AssetSpec(
            "Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4/ASV4_Second_Hand_Dispatch_Van.fbx",
            "ASV4_SecondHandDispatchVan",
            new Vector3(6f, 0f, -4f)),
        new AssetSpec(
            "Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4/ASV4_Van_Transit_Interior.fbx",
            "ASV4_VanTransitInterior",
            new Vector3(3f, 0f, -9f)),
        new AssetSpec(
            "Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4/ASV4_FirstPerson_Gloves.fbx",
            "ASV4_FirstPerson_Gloves",
            new Vector3(-6f, 0f, -8f)),
        new AssetSpec(
            "Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4/ASV4_Item_Flashlight.fbx",
            "ASV4_Item_Flashlight",
            new Vector3(-4f, 0f, -8f)),
        new AssetSpec(
            "Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4/ASV4_Item_Battery.fbx",
            "ASV4_Item_Battery",
            new Vector3(-2f, 0f, -8f)),
    };

    static bool isRunningSetup;

    static GeneratedArtImporter()
    {
        EditorApplication.delayCall += AutoSetupGeneratedArtForPlay;
    }

    // Listens for FBX re-imports during the editor session (e.g. after Blender
    // re-exports while Unity is open). When any ASV4 FBX is touched, regenerate
    // the prefab wrappers so Resources/GeneratedArt stays in sync.
    //
    // Re-entry guard: SetupGeneratedArtForPlay itself calls SaveAndReimport on
    // each FBX, which fires OnPostprocessAllAssets again. Without the isRunningSetup
    // flag we would recurse infinitely and Unity would hang at "loading".
    class GeneratedArtFbxPostprocessor : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] imported, string[] deleted,
            string[] moved, string[] movedFrom)
        {
            if (isRunningSetup) return;

            bool touchedAsv4 = false;
            foreach (string path in imported)
            {
                if (path != null && path.Contains("OutsourcedCivicCommercial_v4") && path.EndsWith(".fbx"))
                {
                    touchedAsv4 = true;
                    break;
                }
            }
            if (!touchedAsv4) return;

            EditorApplication.delayCall += () =>
            {
                if (isRunningSetup) return;
                SetupGeneratedArtForPlay(showDialogs: false);
            };
        }
    }

    [MenuItem("Tools/Accident Squad/Art/Setup ASV4 Art For Play")]
    public static void SetupGeneratedArtForPlayMenu()
    {
        SetupGeneratedArtForPlay(showDialogs: true);
    }

    public static void SetupGeneratedArtForPlayBatch()
    {
        bool ok = SetupGeneratedArtForPlay(showDialogs: false);
        if (!ok)
            throw new System.InvalidOperationException("ASV4 generated art setup failed. Check missing FBX paths in Assets/_Project/Art/Generated/OutsourcedCivicCommercial_v4.");
    }

    [MenuItem("Tools/Accident Squad/Art/Import Generated Blender Kit")]
    public static void ImportGeneratedBlenderKit()
    {
        EnsureFolder(PrefabFolder);

        var missing = new List<string>();
        foreach (AssetSpec spec in Specs)
        {
            ConfigureModelImporter(spec.ModelPath, missing);
        }

        AssetDatabase.Refresh();

        int created = 0;
        foreach (AssetSpec spec in Specs)
        {
            if (CreatePrefabWrapper(spec))
                created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string message = missing.Count == 0
            ? $"Imported {created} ASV4 commercial AccidentSquad art prefabs into {PrefabFolder}."
            : $"Imported {created} prefabs. Missing FBX files:\n{string.Join("\n", missing)}";
        EditorUtility.DisplayDialog("Generated art import complete", message, "OK");
    }

    [MenuItem("Tools/Accident Squad/Art/Create Generated Art Gallery In Open Scene")]
    public static void CreateGeneratedArtGallery()
    {
        ImportGeneratedBlenderKit();

        GameObject existing = GameObject.Find("GeneratedArtGallery");
        if (existing != null)
            Object.DestroyImmediate(existing);

        var root = new GameObject("GeneratedArtGallery");
        foreach (AssetSpec spec in Specs)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
            if (prefab == null) continue;

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (instance == null) continue;

            instance.transform.SetParent(root.transform, false);
            instance.transform.localPosition = spec.GalleryPosition;
        }

        Selection.activeGameObject = root;
    }

    [MenuItem("Tools/Accident Squad/Art/Create Playable ASV4 Departure Van Prefab")]
    public static void CreatePlayableDepartureVanPrefab()
    {
        bool created = CreatePlayableDepartureVanPrefabAssets();

        EditorUtility.DisplayDialog(
            "Playable van prefab",
            created
                ? $"Created:\n{PlayableVanPrefabPath}\n{PlayableVanResourcesPath}\n\nHQ Play Mode will auto-load the Resources copy."
                : $"Could not create playable van prefab. Missing model:\n{VanModelPath}",
            "OK");
    }

    static void AutoSetupGeneratedArtForPlay()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (AssetImporter.GetAtPath(VanModelPath) == null) return;

        // Re-run setup whenever any FBX is newer than its corresponding prefab.
        // Without this check the old logic only ran once per machine (gated by
        // EditorPrefs), so re-exporting from Blender did not propagate to the
        // Resources/GeneratedArt prefabs that the runtime loads.
        if (!EditorPrefs.GetBool(AutoSetupDoneKey, false) || AnyFbxNewerThanPrefab())
        {
            bool ok = SetupGeneratedArtForPlay(showDialogs: false);
            if (ok) EditorPrefs.SetBool(AutoSetupDoneKey, true);
        }
    }

    static bool AnyFbxNewerThanPrefab()
    {
        foreach (AssetSpec spec in Specs)
        {
            string fbxFull = System.IO.Path.GetFullPath(spec.ModelPath);
            string prefabFull = System.IO.Path.GetFullPath(spec.PrefabPath);
            string resourcesFull = System.IO.Path.GetFullPath(
                $"{ResourcesGeneratedArtFolder}/{spec.PrefabName}.prefab");

            if (!System.IO.File.Exists(fbxFull)) continue;
            var fbxTime = System.IO.File.GetLastWriteTimeUtc(fbxFull);
            if (System.IO.File.Exists(prefabFull) &&
                fbxTime > System.IO.File.GetLastWriteTimeUtc(prefabFull)) return true;
            if (System.IO.File.Exists(resourcesFull) &&
                fbxTime > System.IO.File.GetLastWriteTimeUtc(resourcesFull)) return true;
            if (!System.IO.File.Exists(prefabFull) || !System.IO.File.Exists(resourcesFull)) return true;
        }

        if (System.IO.File.Exists(VanModelPath))
        {
            var vanTime = System.IO.File.GetLastWriteTimeUtc(VanModelPath);
            if (!System.IO.File.Exists(PlayableVanResourcesPath) ||
                vanTime > System.IO.File.GetLastWriteTimeUtc(PlayableVanResourcesPath)) return true;
        }

        return false;
    }

    static bool SetupGeneratedArtForPlay(bool showDialogs)
    {
        // Re-entry guard so the AssetPostprocessor that watches FBX changes does not
        // recurse into us while SaveAndReimport / AssetDatabase.Refresh fire below.
        if (isRunningSetup) return true;
        isRunningSetup = true;
        try
        {
            EnsureFolder(PrefabFolder);
            EnsureFolder(ResourcesGeneratedArtFolder);

            var missing = new List<string>();
            foreach (AssetSpec spec in Specs)
                ConfigureModelImporter(spec.ModelPath, missing);

            AssetDatabase.Refresh();

            int created = 0;
            foreach (AssetSpec spec in Specs)
            {
                if (CreatePrefabWrapper(spec))
                    created++;
            }

            bool playableVanCreated = CreatePlayableDepartureVanPrefabAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (showDialogs)
            {
                string message = missing.Count == 0
                    ? $"Ready for Play Mode.\nCreated/updated {created} art prefabs and playable departure van resources."
                    : $"Created/updated {created} art prefabs. Missing FBX files:\n{string.Join("\n", missing)}";
                if (!playableVanCreated)
                    message += $"\n\nPlayable van was not created. Missing:\n{VanModelPath}";
                EditorUtility.DisplayDialog("ASV4 art setup", message, "OK");
            }

            return missing.Count == 0 && playableVanCreated;
        }
        finally
        {
            isRunningSetup = false;
        }
    }

    static void ConfigureModelImporter(string modelPath, List<string> missing)
    {
        var importer = AssetImporter.GetAtPath(modelPath) as ModelImporter;
        if (importer == null)
        {
            missing.Add(modelPath);
            return;
        }

        importer.importAnimation = false;
        importer.animationType = ModelImporterAnimationType.None;
        importer.addCollider = false;
        importer.isReadable = false;
        importer.importBlendShapes = false;
        importer.importCameras = false;
        importer.importLights = false;
        importer.importVisibility = false;
        importer.generateSecondaryUV = false;
        importer.meshCompression = ModelImporterMeshCompression.Off;
        importer.weldVertices = true;
        importer.globalScale = 1f;
        importer.useFileScale = true;
        importer.SaveAndReimport();
    }

    static bool CreatePrefabWrapper(AssetSpec spec)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(spec.ModelPath);
        if (model == null) return false;

        var wrapper = new GameObject(spec.PrefabName);
        var modelInstance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (modelInstance == null)
            modelInstance = Object.Instantiate(model);

        modelInstance.name = $"{spec.PrefabName}_Model";
        modelInstance.transform.SetParent(wrapper.transform, false);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = spec.PrefabScale;

        foreach (var renderer in wrapper.GetComponentsInChildren<Renderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        PrefabUtility.SaveAsPrefabAsset(wrapper, spec.PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(wrapper, $"{ResourcesGeneratedArtFolder}/{spec.PrefabName}.prefab");
        Object.DestroyImmediate(wrapper);
        return true;
    }

    static bool CreatePlayableDepartureVanPrefabAssets()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(ResourcesGeneratedArtFolder);

        var missing = new List<string>();
        ConfigureModelImporter(VanModelPath, missing);
        AssetDatabase.Refresh();

        var model = AssetDatabase.LoadAssetAtPath<GameObject>(VanModelPath);
        if (model == null) return false;

        var wrapper = new GameObject("ASV4_PlayableDepartureVan");
        var modelInstance = PrefabUtility.InstantiatePrefab(model) as GameObject;
        if (modelInstance == null)
            modelInstance = Object.Instantiate(model);

        modelInstance.name = "ASV4_PlayableDepartureVan_Model";
        modelInstance.transform.SetParent(wrapper.transform, false);
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        var trigger = wrapper.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 0.78f, 1.42f);
        trigger.size = new Vector3(4.35f, 1.85f, 0.95f);

        AddVanBlockingCollider(wrapper.transform, "ASV4DepartureVanSolidBodyCollider",
            new Vector3(0f, 0.86f, 0f), new Vector3(3.55f, 1.45f, 1.7f));
        AddVanBlockingCollider(wrapper.transform, "ASV4DepartureVanFrontBulkCollider",
            new Vector3(-1.65f, 0.68f, 0f), new Vector3(0.55f, 0.92f, 1.35f));
        AddVanBlockingCollider(wrapper.transform, "ASV4DepartureVanRearBulkCollider",
            new Vector3(1.55f, 0.66f, 0f), new Vector3(0.36f, 0.82f, 1.28f));

        wrapper.AddComponent<OfficeDepartureVan>();

        foreach (var renderer in wrapper.GetComponentsInChildren<Renderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
        }

        PrefabUtility.SaveAsPrefabAsset(wrapper, PlayableVanPrefabPath);
        PrefabUtility.SaveAsPrefabAsset(wrapper, PlayableVanResourcesPath);
        Object.DestroyImmediate(wrapper);
        return true;
    }

    static void AddVanBlockingCollider(Transform parent, string name, Vector3 localCenter, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localCenter;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;
        var collider = go.AddComponent<BoxCollider>();
        collider.isTrigger = false;
        collider.center = Vector3.zero;
        collider.size = size;
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
