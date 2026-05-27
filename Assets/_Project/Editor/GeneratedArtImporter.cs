using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GeneratedArtImporter
{
    const string PrefabFolder = "Assets/_Project/Prefabs/Art";

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
        importer.generateSecondaryUV = true;
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
