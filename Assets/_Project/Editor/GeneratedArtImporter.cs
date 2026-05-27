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
    const string AutoSetupDoneKey = "AccidentSquad.GeneratedArt.ASV4AutoSetupDone.v1";

    struct AssetSpec
    {
        public readonly string ModelPath;
        public readonly string PrefabName;
        public readonly Vector3 GalleryPosition;

        public AssetSpec(string modelPath, string prefabName, Vector3 galleryPosition)
        {
            ModelPath = modelPath;
            PrefabName = prefabName;
            GalleryPosition = galleryPosition;
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
            new Vector3(-5f, 0f, -4f)),
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
    };

    static GeneratedArtImporter()
    {
        EditorApplication.delayCall += AutoSetupGeneratedArtForPlay;
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
        if (EditorPrefs.GetBool(AutoSetupDoneKey, false)) return;
        if (AssetImporter.GetAtPath(VanModelPath) == null) return;

        bool ok = SetupGeneratedArtForPlay(showDialogs: false);
        if (ok)
            EditorPrefs.SetBool(AutoSetupDoneKey, true);
    }

    static bool SetupGeneratedArtForPlay(bool showDialogs)
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
        modelInstance.transform.localScale = Vector3.one;

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
        trigger.center = new Vector3(0f, 0.9f, 0f);
        trigger.size = new Vector3(4.4f, 2.0f, 2.4f);

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
