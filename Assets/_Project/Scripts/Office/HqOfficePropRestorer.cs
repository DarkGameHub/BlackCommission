using UnityEngine;
using UnityEngine.SceneManagement;

public static class HqOfficePropRestorer
{
    const string SceneName = "HQ";
    const string RootName = "MVP_RestoredOfficeProps";

    static readonly Color DeadRubberSoft = Rgb(0x23, 0x28, 0x25);
    static readonly Color AgedPaperDark = Rgb(0x86, 0x7A, 0x58);
    static readonly Color IncandescentWhite = new(1.0f, 0.95f, 0.86f);
    static readonly Color IncandescentWarm = new(1.0f, 0.86f, 0.66f);

    enum OfficePattern
    {
        Grime,
        Scratched
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void RuntimeBootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RestoreIfHq(SceneManager.GetActiveScene());
    }

#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoadMethod]
    static void EditorBootstrap()
    {
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            Scene scene = SceneManager.GetActiveScene();
            if (RestoreIfHq(scene))
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        };
    }

    [UnityEditor.MenuItem("Tools/Black Commission/Art/Restore Missing HQ Office Props")]
    static void RestoreHqPropsMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (RestoreIfHq(scene))
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    }
#endif

    static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RestoreIfHq(scene);
    }

    static bool RestoreIfHq(Scene scene)
    {
        if (!scene.IsValid() || scene.name != SceneName)
            return false;

        Transform root = EnsureRoot().transform;
        bool changed = false;

        changed |= PlacePropBaked("GeneratedArt/AS_OfficeSofa", "ShellSofa_Generated", root,
            new Vector3(-3.90f, 0f, -2.80f), new Vector3(0f, 90f, 0f),
            new Vector3(-3.674f, 0.388f, 3.221f), new Vector3(-90f, -180f, 0f), new Vector3(79.2f, 64.3f, 84.5f));

        changed |= PlacePropBaked("GeneratedArt/AS_OfficeFilingCabinet", "ShellFilingCabinet_Generated", root,
            new Vector3(-4.45f, 0f, 1.70f), new Vector3(0f, 90f, 0f),
            new Vector3(0.395f, 0.66f, -0.23f), new Vector3(-90f, 0f, 0f), Vector3.one * 65.80643f);

        bool supplyCabinetAdded = PlacePropBaked("GeneratedArt/AS_OfficeSupplyCabinet", "ShellSupplyCabinet_Generated", root,
            new Vector3(-4.45f, 0f, 0.20f), new Vector3(0f, 90f, 0f),
            new Vector3(0.215f, 0.652f, -0.159f), new Vector3(-90f, 0f, 0f), new Vector3(81.3f, 81.3f, 78.3f));
        changed |= supplyCabinetAdded;
        if (!SceneObjectExists("ShellSupplyCabinetStorageTrigger"))
        {
            CreateInteractionTrigger("ShellSupplyCabinetStorageTrigger", root,
                new Vector3(-4.18f, 1.02f, 0.20f), new Vector3(1.45f, 1.95f, 1.25f))
                .AddComponent<OfficeCabinetStorage>();
            changed = true;
        }

        changed |= PlacePropBaked("GeneratedArt/AS_OfficeDebtBoard", "ShellDebtBoard_Generated", root,
            new Vector3(-0.45f, 1.55f, 3.05f), new Vector3(0f, 180f, 0f),
            new Vector3(0.442f, -0.176f, -0.001f), new Vector3(-90f, 0f, 0f), Vector3.one * 81.64722f);

        changed |= PlacePropBaked("GeneratedArt/AS_OfficeToolSet", "ShellToolSet_Generated", root,
            new Vector3(-2.60f, 0f, -2.85f), Vector3.zero,
            new Vector3(-1.514f, 0.277f, 1.001f), new Vector3(-90f, 0f, 82.44f), Vector3.one * 104f);

        changed |= PlaceProp("GeneratedArt/AS_OfficeToolRack", "ShellToolRack_Generated", root,
            new Vector3(2.70f, 0.85f, 2.95f), Quaternion.Euler(0f, 180f, 0f));

        changed |= PlacePropBaked("GeneratedArt/AS_GarageWorkshopCorner", "ShellGarageWorkshopCorner_Generated", root,
            new Vector3(4.70f, 0f, 2.50f), new Vector3(0f, -90f, 0f),
            new Vector3(-4.628f, 0.717f, 0.004f), new Vector3(-90f, 0f, 0f), Vector3.one * 101.8262f);

        changed |= PlacePropBaked("GeneratedArt/AS_LampFluorescent", "ShellLampFluorescent_Computer", root,
            new Vector3(-1.55f, 2.60f, 1.80f), Vector3.zero,
            new Vector3(-0.988f, -0.619f, 1.195f), new Vector3(-90f, 180f, 0f), Vector3.one * 65.48044f);
        changed |= CreatePointLightIfMissing("HQ_LampFluorescent_Computer_Light", root,
            new Vector3(2.70f, 2.45f, 3.063f), IncandescentWhite, 1.8f, 5.5f);

        changed |= PlacePropBaked("GeneratedArt/AS_LampFluorescent", "ShellLampFluorescent_ToolRack", root,
            new Vector3(2.70f, 2.60f, 2.60f), Vector3.zero,
            new Vector3(-0.000632885f, -0.166f, 0.422f), new Vector3(-90f, 180f, 0f), Vector3.one * 65.48044f);
        changed |= CreatePointLightIfMissing("HQ_LampFluorescent_ToolRack_Light", root,
            new Vector3(-2.526f, 2.00f, 2.724f), IncandescentWhite, 1.8f, 5.5f);

        changed |= PlacePropBaked("GeneratedArt/AS_LampDesk", "ShellLampDesk_A", root,
            new Vector3(-1.55f, 0.80f, 1.60f), new Vector3(0f, 180f, 0f),
            new Vector3(2.01f, 1.727f, 1.295f), new Vector3(-90f, 0f, 0f), Vector3.one * 23.69977f);
        changed |= CreatePointLightIfMissing("HQ_LampDesk_A_Light", root,
            new Vector3(-1.55f, 1.85f, 1.60f), IncandescentWarm, 1.2f, 3.0f);

        changed |= PlacePropBaked("GeneratedArt/AS_LampDesk", "ShellLampDesk_B", root,
            new Vector3(-3.80f, 0.75f, 2.00f), new Vector3(0f, 90f, 0f),
            new Vector3(-4.317f, 1.672f, 4.71f), new Vector3(-90f, 0f, 0f), Vector3.one * 23.69977f);
        changed |= CreatePointLightIfMissing("HQ_LampDesk_B_Light", root,
            new Vector3(-3.80f, 1.75f, 2.00f), IncandescentWarm, 1.2f, 3.0f);

        changed |= PlacePropBaked("GeneratedArt/AS_OfficeSafetyBoard", "ShellSafetyBoard_Generated", root,
            new Vector3(-2.80f, 1.50f, 3.05f), new Vector3(0f, 180f, 0f),
            new Vector3(2.05f, -0.169f, 5.053f), new Vector3(-90f, -90f, 0f), Vector3.one * 98.74973f);

        changed |= PlacePropBaked("GeneratedArt/AS_OfficeDesk", "ShellDesk_Generated", root,
            new Vector3(-1.20f, 0f, 2.20f), new Vector3(0f, 180f, 0f),
            new Vector3(-0.357f, 0.402f, -0.503f), new Vector3(-90f, 0f, 0f), Vector3.one * 58.47131f);

        changed |= CreateOfficeBestiaryNotebook(root);

        changed |= PlacePropBaked("GeneratedArt/AS_OfficeFireExtinguisher", "ShellFireExtinguisher_Generated", root,
            new Vector3(-4.50f, 0f, -2.90f), new Vector3(0f, 90f, 0f),
            new Vector3(-2.511f, 0.412f, 4.335f), new Vector3(-90f, -180f, 0f), Vector3.one * 40.83926f);

        changed |= PlacePropBaked("GeneratedArt/AS_OfficeGasMaskSentinel", "ShellGasMaskSentinel_Generated", root,
            new Vector3(-0.60f, 0f, -2.90f), new Vector3(0f, 180f, 0f),
            new Vector3(4.113f, 0.923f, -5.063f), new Vector3(-90f, -90f, 0f), Vector3.one * 89.5156f);

        return changed;
    }

    static GameObject EnsureRoot()
    {
        GameObject root = FindSceneObject(RootName);
        if (root != null)
            return root;

        root = new GameObject(RootName);
        return root;
    }

    static bool PlaceProp(string resourcePath, string name, Transform root, Vector3 position, Quaternion rotation)
    {
        if (SceneObjectExists(name))
            return false;

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[HqOfficePropRestorer] Missing prefab: {resourcePath}");
            return false;
        }

        GameObject go = Object.Instantiate(prefab, root);
        go.name = name;
        go.transform.localPosition = position;
        go.transform.localRotation = rotation;
        go.transform.localScale = Vector3.one;
        DisableImportedColliders(go);
        return true;
    }

    static bool PlacePropBaked(string resourcePath, string name, Transform root,
        Vector3 wrapperPos, Vector3 wrapperEuler, Vector3 childLocalPos, Vector3 childEuler, Vector3 childScale)
    {
        if (SceneObjectExists(name))
            return false;

        GameObject prefab = Resources.Load<GameObject>(resourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[HqOfficePropRestorer] Missing prefab: {resourcePath}");
            return false;
        }

        GameObject go = Object.Instantiate(prefab, root);
        go.name = name;
        go.transform.localPosition = wrapperPos;
        go.transform.localRotation = Quaternion.Euler(wrapperEuler);
        go.transform.localScale = Vector3.one;

        if (go.transform.childCount > 0)
        {
            Transform model = go.transform.GetChild(0);
            model.localPosition = childLocalPos;
            model.localEulerAngles = childEuler;
            model.localScale = childScale;
        }

        DisableImportedColliders(go);
        return true;
    }

    static void DisableImportedColliders(GameObject go)
    {
        foreach (Collider collider in go.GetComponentsInChildren<Collider>())
            collider.enabled = false;
    }

    static bool CreatePointLightIfMissing(string name, Transform root, Vector3 position, Color color, float intensity, float range)
    {
        if (SceneObjectExists(name))
            return false;

        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        go.transform.position = position;

        Light light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
        return true;
    }

    static GameObject CreateInteractionTrigger(string name, Transform parent, Vector3 position, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        BoxCollider collider = go.AddComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = size;
        return go;
    }

    static bool CreateOfficeBestiaryNotebook(Transform root)
    {
        if (SceneObjectExists("ShellMonsterBestiaryNotebook"))
            return false;

        Material cover = MakeOfficeMaterial("Office_BestiaryNotebookCover", Rgb(0x47, 0x50, 0x40), DeadRubberSoft, OfficePattern.Scratched);
        Material paper = MakeOfficeMaterial("Office_BestiaryNotebookPages", AgedPaperDark, DeadRubberSoft, OfficePattern.Grime);

        GameObject notebook = CreateBox("ShellMonsterBestiaryNotebook", root,
            new Vector3(-1.05f, 0.86f, 1.95f), new Vector3(0.46f, 0.035f, 0.34f), cover);
        notebook.transform.rotation = Quaternion.Euler(0f, -12f, 0f);

        CreateBoxLocal("ShellMonsterBestiaryNotebookPages", notebook.transform,
            new Vector3(0.03f, 0.034f, 0f), new Vector3(0.36f, 0.012f, 0.27f), Quaternion.identity, paper);

        CreateInteractionTrigger("ShellMonsterBestiaryNotebookTrigger", root,
            new Vector3(-1.05f, 0.98f, 1.95f), new Vector3(1.05f, 0.72f, 0.95f))
            .AddComponent<OfficeMonsterBestiary>();
        return true;
    }

    static GameObject CreateBox(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        go.transform.localScale = scale;
        if (go.TryGetComponent(out Collider collider))
            DestroySceneObject(collider);
        if (go.TryGetComponent(out Renderer renderer))
            renderer.sharedMaterial = material;
        return go;
    }

    static void CreateBoxLocal(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Quaternion localRotation, Material material)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = localRotation;
        go.transform.localScale = localScale;
        if (go.TryGetComponent(out Collider collider))
            DestroySceneObject(collider);
        if (go.TryGetComponent(out Renderer renderer))
            renderer.sharedMaterial = material;
    }

    static bool SceneObjectExists(string name)
    {
        return FindSceneObject(name) != null;
    }

    static GameObject FindSceneObject(string name)
    {
        GameObject[] objects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (GameObject obj in objects)
        {
            if (obj != null && obj.name == name)
                return obj;
        }
        return null;
    }

    static void DestroySceneObject(Object obj)
    {
        if (obj == null)
            return;

        if (Application.isPlaying)
            Object.Destroy(obj);
        else
            Object.DestroyImmediate(obj);
    }

    static Material MakeOfficeMaterial(string name, Color baseColor, Color accentColor, OfficePattern pattern)
    {
        Material material = MakeMaterial(baseColor);
        material.name = name;
        if (pattern == OfficePattern.Grime)
            material.color = Color.Lerp(baseColor, accentColor, 0.08f);
        return material;
    }

    static Material MakeMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit")
            ?? Shader.Find("Universal Render Pipeline/Simple Lit")
            ?? Shader.Find("Standard");
        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;
        if (material.HasProperty("_Smoothness"))
            material.SetFloat("_Smoothness", 0.08f);
        if (material.HasProperty("_Metallic"))
            material.SetFloat("_Metallic", 0f);
        return material;
    }

    static Color Rgb(int r, int g, int b)
    {
        return new Color(r / 255f, g / 255f, b / 255f);
    }
}
