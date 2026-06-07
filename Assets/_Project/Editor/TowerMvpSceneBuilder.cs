using System.Collections.Generic;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Promotes the abandoned-tower whitebox into a PLAYABLE co-op mission, mirroring how
/// <see cref="SnowLotusTestSceneBuilder"/> wires Snow Lotus. It copies the blockout scene to
/// <c>Tower_EarthCoast_01.unity</c>, strips the walkthrough/preview objects, and lays the mission
/// layer on top of the existing geometry: a spawn point + <see cref="HQSpawnManager"/> at the van,
/// the <c>LostItemMissionManager</c> prefab, the 楼盘沙盘 objective in F2_L5, and a
/// <see cref="SchoolExitPoint"/> return trigger at the van. It then marks the blockout
/// Navigation Static and bakes a NavMesh, registers the scene in Build Settings, and points the
/// HQ office computer's commission at the new tower task so Play-from-HQ dispatches here.
///
/// Reuses existing systems only — the heavy 沙盘 carry and power gate are a later pass, so for now
/// the objective is a normal <see cref="LostHomeworkItem"/> (grab → return → settle).
/// </summary>
public static class TowerMvpSceneBuilder
{
    const string BlockoutPath = "Assets/Scene/AbandonedBuilding_Blockout.unity";
    const string ScenePath = "Assets/_Project/Scenes/Tower_EarthCoast_01.unity";
    const string TaskPath = "Assets/_Project/Resources/Tasks/Tower_EarthCoast_01.asset";
    const string HqScenePath = "Assets/_Project/Scenes/HQ.unity";
    const string MissionManagerPrefabPath = "Assets/_Project/Prefabs/Mission/LostItemMissionManager.prefab";
    const string BlockoutRoot = "AB_FloorPlan_Blockout";

    // Tower grid landmarks (match AbandonedBuildingFloorPlanBuilder): van forecourt is x12–24, z<0;
    // F2 floor is at y=4.2; F2_L5_DeepTargetArea is x4–16, z12–20.
    static readonly Vector3 SpawnPos = new Vector3(15f, 0.3f, -3f);
    static readonly Vector3 ExitPos = new Vector3(15f, 0.3f, -4f);
    static readonly Vector3 ObjectivePos = new Vector3(9f, 4.7f, 16f);

    [MenuItem("Tools/Black Commission/MVP/Tower/Setup Playable Tower Mission")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("先退出 Play", "请先退出 Play 模式再运行。", "OK");
            return;
        }

        EnsureFolders();
        OfficeTaskDefinition task = EnsureTask();
        BuildScene();
        WireHqComputer(task);
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(HqScenePath);
        Debug.Log($"[Tower MVP] Playable mission ready: {ScenePath}. HQ commission now dispatches to " +
                  $"'{task.title}'. Play from HQ → Start Host → accept at the computer → board the van.");
    }

    static OfficeTaskDefinition EnsureTask()
    {
        var task = AssetDatabase.LoadAssetAtPath<OfficeTaskDefinition>(TaskPath);
        if (task == null)
        {
            task = ScriptableObject.CreateInstance<OfficeTaskDefinition>();
            AssetDatabase.CreateAsset(task, TaskPath);
        }

        task.taskId = "tower_earth_coast_01";
        task.title = "地球海岸沙盘回收";
        task.category = MvpTaskCategory.LostItemRecovery;
        task.client = "火星「地球未竟之梦」主题展";
        task.description = "前往烂尾的「阿瑞斯预售·地球海岸壹号」，从二层售楼区取回楼盘沙盘并带回委托车。" +
                           "二层断电、卷帘锁定，需先在一层配电室恢复供电。";
        task.locationName = "地球海岸壹号·烂尾预售楼";
        task.sceneName = "Tower_EarthCoast_01";
        task.recommendedPlayersMin = 1;
        task.recommendedPlayersMax = 4;
        task.requiredOfficeLevel = 1;
        task.minimumReputation = -100;
        task.missionStartClockHour = 16f;
        task.contractWindowGameHours = 12f;
        task.realSecondsPerGameHour = 60f;
        task.overtimeMoneyPenaltyPerGameHour = 30;
        task.overtimeReputationPenaltyBlockGameHours = 2f;
        task.overtimeReputationPenaltyPerBlock = 1;
        task.moneyReward = 400;
        task.reputationReward = 8;
        task.experienceReward = 120;
        task.failureConsolationMoney = 20;
        task.failureReputationPenalty = -2;
        task.failureExperience = 0;
        EditorUtility.SetDirty(task);
        return task;
    }

    static void BuildScene()
    {
        // Copy the blockout to the playable scene path (blockout file stays untouched), then edit the copy.
        Scene blockout = EditorSceneManager.OpenScene(BlockoutPath, OpenSceneMode.Single);
        EditorSceneManager.SaveScene(blockout, ScenePath, saveAsCopy: true);
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // Strip preview/walkthrough leftovers — the networked player brings its own camera.
        foreach (var name in new[] { "PreviewWalker", "PreviewCamera", "Preview Camera",
                                     "player", "pb_Mesh", "Main Camera", "Directional Light" })
            DestroyAllNamed(name);

        // Dim, pre-power mood (flashlight-dependent). A full lighting pass is a later step.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.06f, 0.06f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.07f);
        RenderSettings.fogDensity = 0.02f;

        var root = new GameObject("MVP_Tower_EarthCoast_01");

        CreateCamera(root.transform);
        CreateLight(root.transform);
        CreateSpawn(root.transform);
        CreateMissionObjects(root.transform);
        BakeNavMesh();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        Debug.Log($"[Tower MVP] Scene built from blockout copy: {ScenePath}");
    }

    static void CreateCamera(Transform parent)
    {
        var go = new GameObject("Preview Camera");
        go.transform.SetParent(parent);
        go.tag = "MainCamera";
        go.transform.SetPositionAndRotation(new Vector3(15f, 2.2f, -6f), Quaternion.Euler(6f, 0f, 0f));
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.farClipPlane = 300f;
        go.AddComponent<AudioListener>();
        var camData = go.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
    }

    static void CreateLight(Transform parent)
    {
        var go = new GameObject("Overcast Sun");
        go.transform.SetParent(parent);
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(0.62f, 0.66f, 0.74f);
        light.intensity = 0.45f;   // dim; the show-flat warm light + worklights come in the lighting pass
    }

    static void CreateSpawn(Transform parent)
    {
        var spawn = new GameObject("PlayerSpawnPoint");
        spawn.transform.SetParent(parent);
        spawn.transform.SetPositionAndRotation(SpawnPos, Quaternion.identity);   // face north into the lobby

        var managerGo = new GameObject("TowerSpawnManager");
        managerGo.transform.SetParent(parent);
        var manager = managerGo.AddComponent<HQSpawnManager>();
        var so = new SerializedObject(manager);
        so.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateMissionObjects(Transform parent)
    {
        var managerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MissionManagerPrefabPath);
        if (managerPrefab != null)
        {
            var manager = (GameObject)PrefabUtility.InstantiatePrefab(managerPrefab);
            manager.transform.SetParent(parent);
            manager.transform.position = Vector3.zero;
        }
        else
        {
            Debug.LogWarning($"[Tower MVP] Mission manager prefab missing: {MissionManagerPrefabPath}");
        }

        // Objective: the 楼盘沙盘 on its pedestal in F2_L5. Reuses LostHomeworkItem for the
        // grab→return loop; the heavy two-hand carry is a later mechanic.
        var pedestal = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pedestal.name = "SandTablePedestal";
        pedestal.transform.SetParent(parent);
        pedestal.transform.position = ObjectivePos + new Vector3(0f, -0.45f, 0f);
        pedestal.transform.localScale = new Vector3(1.6f, 0.9f, 1.0f);
        pedestal.GetComponent<Renderer>().sharedMaterial = MakeMaterial("MVP_tower_pedestal", new Color(0.30f, 0.30f, 0.33f));

        var model = GameObject.CreatePrimitive(PrimitiveType.Cube);
        model.name = "TowerSandTable";
        model.transform.SetParent(parent);
        model.transform.position = ObjectivePos;
        model.transform.localScale = new Vector3(1.4f, 0.35f, 0.9f);
        model.GetComponent<Renderer>().sharedMaterial = MakeMaterial("MVP_tower_sandtable", new Color(0.85f, 0.78f, 0.45f));
        model.AddComponent<NetworkObject>();
        var item = model.AddComponent<LostHomeworkItem>();
        var itemSo = new SerializedObject(item);
        var nameProp = itemSo.FindProperty("itemName");
        if (nameProp != null) nameProp.stringValue = "楼盘沙盘";
        itemSo.ApplyModifiedPropertiesWithoutUndo();

        var glowGo = new GameObject("SandTableGlow");
        glowGo.transform.SetParent(model.transform);
        glowGo.transform.localPosition = new Vector3(0f, 1.0f, 0f);
        var glow = glowGo.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 7f;
        glow.intensity = 1.6f;
        glow.color = new Color(1f, 0.86f, 0.45f);   // warm "wrong luxury" beacon

        // Return point at the van (reuse SchoolExitPoint).
        var exit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exit.name = "TowerReturnPoint";
        exit.transform.SetParent(parent);
        exit.transform.position = ExitPos;
        exit.transform.localScale = new Vector3(6f, 1.2f, 4f);
        exit.GetComponent<Renderer>().enabled = false;
        var box = exit.GetComponent<BoxCollider>();
        box.isTrigger = true;
        exit.AddComponent<NetworkObject>();
        exit.AddComponent<SchoolExitPoint>();
    }

    static void BakeNavMesh()
    {
        var blockout = GameObject.Find(BlockoutRoot);
        if (blockout != null)
        {
            // Mark the shell Navigation Static so the bake includes floors/stairs/walls.
            foreach (var r in blockout.GetComponentsInChildren<Renderer>(true))
                GameObjectUtility.SetStaticEditorFlags(r.gameObject,
                    StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
        }

        try
        {
            NavMeshBuilder.BuildNavMesh();
            Debug.Log("[Tower MVP] NavMesh baked. NOTE: the discrete stair steps may need NavMesh " +
                      "Links to connect floors for AI; players use the CharacterController regardless.");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[Tower MVP] NavMesh bake skipped: {ex.Message}. Bake manually via Window > AI > Navigation.");
        }
    }

    static void WireHqComputer(OfficeTaskDefinition task)
    {
        Scene scene = EditorSceneManager.OpenScene(HqScenePath, OpenSceneMode.Single);
        var computer = GameObject.Find("MVP_OfficeComputer");
        if (computer == null || !computer.TryGetComponent<OfficeComputer>(out var office))
        {
            Debug.LogWarning("[Tower MVP] HQ MVP_OfficeComputer not found — point its demoTask at the " +
                             "tower task manually to dispatch here.");
            return;
        }

        var so = new SerializedObject(office);
        var prop = so.FindProperty("demoTask");
        if (prop != null)
        {
            prop.objectReferenceValue = task;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Tower MVP] HQ computer commission set to the tower task (replaces Snow Lotus). " +
                      "Revert by setting demoTask back to SnowLotus_01 if you want Snow Lotus again.");
        }
    }

    static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        EnsureBuildScene(scenes, HqScenePath);
        EnsureBuildScene(scenes, ScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void EnsureBuildScene(List<EditorBuildSettingsScene> scenes, string path)
    {
        foreach (var s in scenes)
            if (s.path == path) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
    }

    static Material MakeMaterial(string name, Color color)
    {
        const string folder = "Assets/_Project/Settings/Materials";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            EnsureFolder("Assets/_Project", "Settings");
            EnsureFolder("Assets/_Project/Settings", "Materials");
        }

        string path = $"{folder}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        else mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/_Project", "Resources");
        EnsureFolder("Assets/_Project/Resources", "Tasks");
        EnsureFolder("Assets/_Project", "Scenes");
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    static void DestroyAllNamed(string name)
    {
        for (var go = GameObject.Find(name); go != null; go = GameObject.Find(name))
            Object.DestroyImmediate(go);
    }
}
