using System.IO;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

/// <summary>Builds the Blender-backed printer/sheet prefabs and registers both with NGO.</summary>
public static class WorkOrderAssetBuilder
{
    const string ResourceFolder = "Assets/Resources/WorkOrder";
    const string ConfigFolder = "Assets/Resources/Config";
    const string PrinterModelPath = "Assets/_Project/Art/Props/WorkOrder/WorkOrderPrinter.fbx";
    const string SheetModelPath = "Assets/_Project/Art/Props/WorkOrder/WorkOrderSheet.fbx";
    const string NetworkPrefabsListPath = "Assets/DefaultNetworkPrefabs.asset";

    [MenuItem("Tools/Black Commission/Work Orders/Build Assets")]
    public static void Build()
    {
        Directory.CreateDirectory(ResourceFolder);
        Directory.CreateDirectory(ConfigFolder);
        BuildConfig();

        GameObject printer = BuildPrinterPrefab();
        GameObject item = BuildItemPrefab();
        RegisterNetworkPrefabs(printer, item);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[WorkOrderAssetBuilder] Built printer, sheet, config and NGO registrations.");
    }

    static void BuildConfig()
    {
        const string path = ConfigFolder + "/WorkOrderConfig.asset";
        if (AssetDatabase.LoadAssetAtPath<WorkOrderConfig>(path) != null) return;
        WorkOrderConfig config = ScriptableObject.CreateInstance<WorkOrderConfig>();
        AssetDatabase.CreateAsset(config, path);
    }

    static GameObject BuildPrinterPrefab()
    {
        GameObject root = new("WorkOrderPrinter");
        try
        {
            root.AddComponent<NetworkObject>();
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.64f, 0f);
            box.size = new Vector3(0.72f, 1.28f, 0.56f);

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(PrinterModelPath);
            if (model != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = "PrinterModel";
                instance.transform.SetParent(root.transform, false);
            }
            else
            {
                BuildPrinterFallback(root.transform);
                Debug.LogWarning($"[WorkOrderAssetBuilder] Missing {PrinterModelPath}; used low-poly fallback.");
            }

            Transform output = new GameObject("OutputAnchor").transform;
            output.SetParent(root.transform, false);
            output.localPosition = new Vector3(0f, 1.10f, 0.20f);
            output.localRotation = Quaternion.Euler(72f, 0f, 0f);

            GameObject preview = GameObject.CreatePrimitive(PrimitiveType.Cube);
            preview.name = "PaperPreview";
            Object.DestroyImmediate(preview.GetComponent<Collider>());
            preview.transform.SetParent(root.transform, false);
            preview.transform.localPosition = new Vector3(0f, 1.11f, 0.20f);
            preview.transform.localRotation = Quaternion.Euler(18f, 0f, 0f);
            preview.transform.localScale = new Vector3(0.51f, 0.005f, 0.32f);
            preview.GetComponent<Renderer>().sharedMaterial = MakeMaterial("WorkOrderPaper", new Color(0.73f, 0.70f, 0.55f));
            preview.SetActive(false);

            root.AddComponent<WorkOrderPrinter>();
            return PrefabUtility.SaveAsPrefabAsset(root, ResourceFolder + "/WorkOrderPrinter.prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }

    static GameObject BuildItemPrefab()
    {
        GameObject root = new("WorkOrderItem");
        try
        {
            Rigidbody rb = root.AddComponent<Rigidbody>();
            rb.mass = 0.05f;
            rb.linearDamping = 0.7f;
            root.AddComponent<NetworkObject>();
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.006f, 0f);
            box.size = new Vector3(0.43f, 0.012f, 0.34f);

            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(SheetModelPath);
            if (model != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(model);
                instance.name = "SheetModel";
                instance.transform.SetParent(root.transform, false);
            }
            else
            {
                GameObject paper = GameObject.CreatePrimitive(PrimitiveType.Cube);
                paper.name = "SheetModel";
                Object.DestroyImmediate(paper.GetComponent<Collider>());
                paper.transform.SetParent(root.transform, false);
                paper.transform.localScale = new Vector3(0.43f, 0.008f, 0.34f);
                paper.GetComponent<Renderer>().sharedMaterial = MakeMaterial("WorkOrderPaper", new Color(0.73f, 0.70f, 0.55f));
            }

            root.AddComponent<WorkOrderItem>();
            return PrefabUtility.SaveAsPrefabAsset(root, ResourceFolder + "/WorkOrderItem.prefab");
        }
        finally { Object.DestroyImmediate(root); }
    }

    static void BuildPrinterFallback(Transform parent)
    {
        GameObject stand = GameObject.CreatePrimitive(PrimitiveType.Cube);
        stand.name = "Stand";
        Object.DestroyImmediate(stand.GetComponent<Collider>());
        stand.transform.SetParent(parent, false);
        stand.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        stand.transform.localScale = new Vector3(0.62f, 0.9f, 0.48f);
        stand.GetComponent<Renderer>().sharedMaterial = MakeMaterial("PrinterDark", new Color(0.12f, 0.14f, 0.13f));
    }

    static Material MakeMaterial(string name, Color color)
    {
        string path = ResourceFolder + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null) return existing;
        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material material = new(shader) { name = name, color = color };
        material.SetFloat("_Smoothness", 0.08f);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    static void RegisterNetworkPrefabs(params GameObject[] prefabs)
    {
        NetworkPrefabsList list = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsListPath);
        if (list == null) throw new FileNotFoundException("DefaultNetworkPrefabs.asset", NetworkPrefabsListPath);
        foreach (GameObject prefab in prefabs)
            if (prefab != null && !list.Contains(prefab))
                list.Add(new NetworkPrefab { Prefab = prefab });
        EditorUtility.SetDirty(list);
    }
}
