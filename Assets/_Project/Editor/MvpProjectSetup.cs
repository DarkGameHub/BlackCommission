using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

public static class MvpProjectSetup
{
    const string HqScenePath = "Assets/_Project/Scenes/HQ.unity";
    const string SchoolScenePath = "Assets/_Project/Scenes/School_LostItem_01.unity";
    const string TaskAssetPath = "Assets/_Project/Settings/Tasks/MissingHomeworkNotebook.asset";
    const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
    const string MaterialFolder = "Assets/_Project/Settings/Materials";

    static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

    [MenuItem("Tools/Black Commission/MVP/Setup School MVP")]
    public static void SetupSchoolMvpMenu() => SetupSchoolMvp();

    public static void SetupSchoolMvp()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("请先停止运行", "请先退出 Play 模式，再运行 MVP Setup。", "OK");
            return;
        }

        EnsureFolders();
        OfficeTaskDefinition task = EnsureMissingHomeworkTask();
        PatchPlayerPrefab();
        SetupHq(task);
        SetupSchoolScene();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(HqScenePath);

        EditorUtility.DisplayDialog(
            "MVP 场景已配置",
            "已生成学校找回作业本 MVP。\n\n测试流程:\n1. 打开 HQ 场景并 Play\n2. 点 Start Host\n3. 靠近电脑按 E 接任务\n4. 在学校找到作业本\n5. 躲开怪物回到校门\n6. 回事务所按 E 领取奖励",
            "开始测试");
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/_Project", "Settings");
        EnsureFolder("Assets/_Project/Settings", "Tasks");
        EnsureFolder("Assets/_Project/Settings", "Materials");
        EnsureFolder("Assets/_Project", "Scenes");
        EnsureFolder("Assets/_Project/Prefabs", "Mission");
    }

    static OfficeTaskDefinition EnsureMissingHomeworkTask()
    {
        var task = AssetDatabase.LoadAssetAtPath<OfficeTaskDefinition>(TaskAssetPath);
        if (task == null)
        {
            task = ScriptableObject.CreateInstance<OfficeTaskDefinition>();
            AssetDatabase.CreateAsset(task, TaskAssetPath);
        }

        task.taskId = "lost_homework_01";
        task.title = "找回被遗忘的作业本";
        task.category = MvpTaskCategory.LostItemRecovery;
        task.client = "焦急的家长";
        task.description = "委托车会停在旧校舍门外。推门进校后找回作业本并安全撤离；记录室里的逾期登记簿可拍照留证，能多拿一点外快。";
        task.locationName = "废弃学校";
        task.sceneName = "School_LostItem_01";
        task.recommendedPlayersMin = 1;
        task.recommendedPlayersMax = 4;
        task.requiredOfficeLevel = 1;
        task.minimumReputation = -100;
        task.missionStartClockHour = 8f;
        task.contractWindowGameHours = 12f;
        task.realSecondsPerGameHour = 60f;
        task.overtimeMoneyPenaltyPerGameHour = 30;
        task.overtimeReputationPenaltyBlockGameHours = 2f;
        task.overtimeReputationPenaltyPerBlock = 1;
        task.moneyReward = 300;
        task.reputationReward = 5;
        task.experienceReward = 80;
        task.failureConsolationMoney = 20;
        task.failureReputationPenalty = -2;
        task.failureExperience = 0;
        EditorUtility.SetDirty(task);
        return task;
    }

    static void PatchPlayerPrefab()
    {
        var root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
        if (root == null)
        {
            Debug.LogWarning($"[MVP Setup] Player prefab not found at {PlayerPrefabPath}. Run Setup All first.");
            return;
        }

        if (root.GetComponent<ClientNetworkTransform>() == null)
        {
            var netTransform = root.AddComponent<ClientNetworkTransform>();
            netTransform.SyncPositionX = true;
            netTransform.SyncPositionY = true;
            netTransform.SyncPositionZ = true;
            netTransform.SyncRotAngleY = true;
            netTransform.SyncRotAngleX = false;
            netTransform.SyncRotAngleZ = false;
            netTransform.SyncScaleX = false;
            netTransform.SyncScaleY = false;
            netTransform.SyncScaleZ = false;
        }

        var hotbar = root.GetComponent<PlayerHotbar>();
        if (hotbar == null)
            hotbar = root.AddComponent<PlayerHotbar>();

        if (root.GetComponent<PlayerFirstPersonRig>() == null)
            root.AddComponent<PlayerFirstPersonRig>();

        PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
        PrefabUtility.UnloadPrefabContents(root);
        Debug.Log("[MVP Setup] Player prefab patched with MVP hotbar, first-person rig, and network transform.");
    }

    static void SetupHq(OfficeTaskDefinition task)
    {
        var scene = EditorSceneManager.OpenScene(HqScenePath, OpenSceneMode.Single);
        EnsureMvpHud();
        EnsureMainMenuUi();

        GameObject computer = GameObject.Find("MVP_OfficeComputer");
        if (computer == null)
        {
            computer = GameObject.CreatePrimitive(PrimitiveType.Cube);
            computer.name = "MVP_OfficeComputer";
            computer.transform.position = new Vector3(-1.55f, 1.085f, 1.704f);
            computer.transform.rotation = Quaternion.identity;
            computer.transform.localScale = Vector3.one;

            // No renderer/material/light: the Blender HQ already models a CRT, this object is collider-only.
            if (computer.TryGetComponent<Renderer>(out var renderer)) renderer.enabled = false;
        }
        else
        {
            // Re-align an existing computer to the Blender CRT so a re-run picks up the layout fix.
            computer.transform.position = new Vector3(-1.55f, 1.085f, 1.704f);
            computer.transform.rotation = Quaternion.identity;
            computer.transform.localScale = Vector3.one;
            foreach (var r in computer.GetComponentsInChildren<Renderer>()) r.enabled = false;
            foreach (var l in computer.GetComponentsInChildren<Light>()) l.enabled = false;
        }

        var computerTrigger = computer.GetComponent<BoxCollider>();
        if (computerTrigger == null)
            computerTrigger = computer.AddComponent<BoxCollider>();
        computerTrigger.isTrigger = true;
        computerTrigger.center = new Vector3(0f, -0.035f, -0.754f);
        computerTrigger.size = new Vector3(2.20f, 1.75f, 2.15f);

        if (computer.GetComponent<NetworkObject>() == null)
            computer.AddComponent<NetworkObject>();

        var officeComputer = computer.GetComponent<OfficeComputer>();
        if (officeComputer == null)
            officeComputer = computer.AddComponent<OfficeComputer>();

        SetObjectReference(officeComputer, "demoTask", task);
        SetString(officeComputer, "returnOfficeScene", "HQ");
        SetBool(officeComputer, "allowNonNetworkSoloStart", false);

        PatchNetworkManagerPlayerPrefab();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void SetupSchoolScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.16f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.05f, 0.08f, 0.09f);
        RenderSettings.fogDensity = 0.025f;

        var root = new GameObject("MVP_School_LostItem_01");

        CreateDirectionalLight();
        CreateSchoolGeometry(root.transform);
        CreateSchoolProps(root.transform);
        CreateMissionObjects(root.transform);
        EnsureMvpHud();

        try
        {
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MVP Setup] NavMesh bake skipped: {ex.Message}");
        }

        EditorSceneManager.SaveScene(scene, SchoolScenePath);
        Debug.Log($"[MVP Setup] School scene saved: {SchoolScenePath}");
    }

    static void CreateSchoolGeometry(Transform parent)
    {
        // SHELL ONLY — the placeholder rooms/walls/shelves were removed so you can import
        // your own level models. What stays is the minimum a playable shell needs: a walkable
        // floor (for NavMesh + testing) and the functional entrance door. Everything mission-
        // related (spawn, notebook, exit, monster, patrol points, evidence) is built in
        // CreateMissionObjects(); import your geometry and align it to those anchors.
        CreateBox("Floor_MainHall", new Vector3(0f, -0.05f, 0f), new Vector3(24f, 0.1f, 18f),
            new Color(0.22f, 0.24f, 0.25f), parent);
        CreateBox("Exterior_Forecourt", new Vector3(0f, -0.05f, -12.1f), new Vector3(10.5f, 0.1f, 7.0f),
            new Color(0.12f, 0.13f, 0.13f), parent);

        var entranceDoor = CreateBox("SchoolEntranceDoor", new Vector3(0f, 1.18f, -9.1f),
            new Vector3(1.7f, 2.25f, 0.12f), new Color(0.08f, 0.09f, 0.08f), parent, false);
        GameObject entranceHandle = CreateBox("SchoolEntranceDoorHandle", new Vector3(0.62f, 1.12f, -9.19f),
            new Vector3(0.12f, 0.12f, 0.055f), new Color(0.1f, 0.75f, 0.38f), parent, false);
        entranceHandle.transform.SetParent(entranceDoor.transform, true);
        entranceDoor.AddComponent<SchoolEntranceDoor>();
    }

    static void CreateSchoolProps(Transform parent)
    {
        // SHELL ONLY — desk/locker/blackboard placeholders removed. Just basic lighting so the
        // empty shell stays navigable; replace with your own fixtures when you import art.
        var lampGlow = new GameObject("ColdSchoolLights");
        lampGlow.transform.SetParent(parent);
        lampGlow.transform.position = new Vector3(0f, 2.7f, 0f);
        var light = lampGlow.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 10.5f;
        light.intensity = 1.15f;
        light.color = new Color(0.58f, 0.74f, 0.68f);

        var redGlow = new GameObject("MonsterWarningLight");
        redGlow.transform.SetParent(parent);
        redGlow.transform.position = new Vector3(8f, 1.8f, 4f);
        var red = redGlow.AddComponent<Light>();
        red.type = LightType.Point;
        red.range = 4.5f;
        red.intensity = 0.55f;
        red.color = new Color(0.82f, 0.18f, 0.12f);
    }

    static void CreateMissionObjects(Transform parent)
    {
        var spawn = new GameObject("PlayerSpawnPoint");
        spawn.transform.SetParent(parent);
        spawn.transform.SetPositionAndRotation(new Vector3(0f, 0.1f, -11.45f), Quaternion.identity);

        var spawnManager = new GameObject("SchoolSpawnManager");
        spawnManager.transform.SetParent(parent);
        var manager = spawnManager.AddComponent<HQSpawnManager>();
        SetObjectReference(manager, "spawnPoint", spawn.transform);

        // Each scene-placed NetworkObject must be saved as a unique prefab asset.
        // NGO derives GlobalObjectIdHash from the prefab GUID — plain GameObjects all hash to 0.
        var missionManagerSrc = new GameObject("LostItemMissionManager");
        missionManagerSrc.AddComponent<NetworkObject>();
        missionManagerSrc.AddComponent<LostItemMissionManager>();
        MakePrefabInstance("LostItemMissionManager", missionManagerSrc, Vector3.zero, parent);

        var notebookSrc = CreateBox("LostHomeworkNotebook", new Vector3(3.6f, 0.72f, 5.05f),
            new Vector3(0.6f, 0.08f, 0.42f), new Color(1f, 0.88f, 0.28f), parent, false);
        notebookSrc.AddComponent<NetworkObject>();
        notebookSrc.AddComponent<LostHomeworkItem>();
        var notebookGlow = new GameObject("NotebookGlow");
        notebookGlow.transform.SetParent(notebookSrc.transform);
        notebookGlow.transform.localPosition = new Vector3(0f, 0.35f, 0f);
        var glow = notebookGlow.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 3f;
        glow.intensity = 1.2f;
        glow.color = new Color(1f, 0.85f, 0.2f);
        MakePrefabInstance("LostHomeworkNotebook", notebookSrc, new Vector3(3.6f, 0.72f, 5.05f), parent);

        var bonusEvidence = CreateBox("OverdueLedgerEvidence", new Vector3(-7.55f, 0.96f, 1.38f),
            new Vector3(0.7f, 0.08f, 0.48f), new Color(0.86f, 0.78f, 0.55f), parent, false);
        var bonusCollider = bonusEvidence.GetComponent<BoxCollider>();
        if (bonusCollider != null)
        {
            bonusCollider.isTrigger = true;
            bonusCollider.size = new Vector3(2.6f, 16f, 3f);
            bonusCollider.center = new Vector3(0f, 7f, 0f);
        }
        bonusEvidence.AddComponent<SchoolBonusEvidenceItem>();
        CreateBox("OverdueLedgerStamp", new Vector3(-7.39f, 1.03f, 1.33f),
            new Vector3(0.24f, 0.025f, 0.16f), new Color(0.8f, 0.08f, 0.04f), parent, false);

        var exitSrc = CreateBox("SchoolExitPoint", new Vector3(0f, 0.08f, -12.2f),
            new Vector3(4.4f, 0.16f, 1.8f), new Color(0.1f, 0.75f, 0.38f), parent, false);
        exitSrc.AddComponent<NetworkObject>();
        var exitCollider = exitSrc.GetComponent<BoxCollider>();
        if (exitCollider != null) exitCollider.isTrigger = true;
        exitSrc.AddComponent<SchoolExitPoint>();
        MakePrefabInstance("SchoolExitPoint", exitSrc, new Vector3(0f, 0.08f, -12.2f), parent);
        Transform[] patrol = CreatePatrolPoints(parent);
        var monsterSrc = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        monsterSrc.name = "HomeworkDebtCollector";
        monsterSrc.transform.position = new Vector3(8.5f, 1f, 4f);
        monsterSrc.transform.localScale = new Vector3(0.85f, 1.4f, 0.85f);
        ApplyMaterial(monsterSrc, "monster_redcoat", new Color(0.55f, 0.08f, 0.07f));
        monsterSrc.AddComponent<NetworkObject>();
        monsterSrc.AddComponent<NetworkTransform>();
        var agent = monsterSrc.AddComponent<NavMeshAgent>();
        agent.speed = 3.8f;
        agent.angularSpeed = 240f;
        agent.acceleration = 12f;
        agent.radius = 0.45f;
        agent.height = 2.2f;
        monsterSrc.AddComponent<SchoolMonsterAI>();
        // patrolPoints reference scene objects — set on the instance after prefab round-trip
        var monsterInstance = MakePrefabInstance("HomeworkDebtCollector", monsterSrc,
            new Vector3(8.5f, 1f, 4f), parent);
        SetObjectArray(monsterInstance.GetComponent<SchoolMonsterAI>(), "patrolPoints", patrol);
    }

    // Saves `source` as a prefab asset (giving it a unique GUID for NGO hash),
    // destroys the source, and returns the scene instance parented under `sceneParent`.
    static GameObject MakePrefabInstance(string prefabName, GameObject source,
        Vector3 worldPos, Transform sceneParent)
    {
        source.transform.SetParent(null, true);
        source.transform.SetPositionAndRotation(worldPos, source.transform.rotation);
        string path = $"Assets/_Project/Prefabs/Mission/{prefabName}.prefab";
        var asset = PrefabUtility.SaveAsPrefabAsset(source, path);
        Object.DestroyImmediate(source);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.transform.SetParent(sceneParent, true);
        instance.transform.position = worldPos;
        return instance;
    }

    static Transform[] CreatePatrolPoints(Transform parent)
    {
        Vector3[] positions =
        {
            new Vector3(8.5f, 0.05f, 4.2f),
            new Vector3(8.8f, 0.05f, -3.8f),
            new Vector3(-8.2f, 0.05f, -3.6f),
            new Vector3(-2.8f, 0.05f, 4.7f)
        };

        Transform[] points = new Transform[positions.Length];
        for (int i = 0; i < positions.Length; i++)
        {
            var point = new GameObject($"MonsterPatrolPoint_{i + 1}");
            point.transform.SetParent(parent);
            point.transform.position = positions[i];
            points[i] = point.transform;
        }

        return points;
    }

    static GameObject CreateBox(string name, Vector3 position, Vector3 scale, Color color, Transform parent, bool navStatic = true)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = position;
        go.transform.localScale = scale;
        ApplyMaterial(go, name.ToLowerInvariant(), color);
        if (navStatic)
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.ContributeGI);
        return go;
    }

    static void CreateDirectionalLight()
    {
        var lightGo = new GameObject("MoonlitDirectionalLight");
        lightGo.transform.rotation = Quaternion.Euler(50f, -25f, 0f);
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.35f;
        light.color = new Color(0.65f, 0.82f, 1f);
    }

    static void EnsureMvpHud()
    {
        if (Object.FindFirstObjectByType<MvpHud>() != null) return;
        var hud = new GameObject("MVP_HUD");
        hud.AddComponent<MvpHud>();
    }

    static void EnsureMainMenuUi()
    {
        if (Object.FindFirstObjectByType<MainMenuUI>() != null) return;
        var menu = new GameObject("MainMenu_UGUI");
        menu.AddComponent<MainMenuUI>();
    }

    static void PatchNetworkManagerPlayerPrefab()
    {
        var networkManager = Object.FindFirstObjectByType<NetworkManager>();
        var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (networkManager == null || playerPrefab == null) return;

        networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
        networkManager.NetworkConfig.ConnectionApproval = true;
        if (networkManager.GetComponent<MvpConnectionLimiter>() == null)
            networkManager.gameObject.AddComponent<MvpConnectionLimiter>();
        EditorUtility.SetDirty(networkManager);
    }

    static void UpdateBuildSettings()
    {
        var orderedPaths = new List<string> { HqScenePath, SchoolScenePath };
        var scenes = new List<EditorBuildSettingsScene>();

        foreach (string path in orderedPaths)
        {
            if (!System.IO.File.Exists(path)) continue;
            scenes.Add(new EditorBuildSettingsScene(path, true));
        }

        foreach (var existing in EditorBuildSettings.scenes)
        {
            if (existing == null || string.IsNullOrEmpty(existing.path)) continue;
            if (orderedPaths.Contains(existing.path)) continue;
            scenes.Add(existing);
        }

        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void EnsureFolder(string parent, string child)
    {
        string path = $"{parent}/{child}";
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    static void ApplyMaterial(GameObject go, string materialKey, Color color)
    {
        if (!go.TryGetComponent<Renderer>(out var renderer)) return;
        renderer.sharedMaterial = GetMaterial(materialKey, color);
    }

    static Material GetMaterial(string key, Color color)
    {
        if (MaterialCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        string safeKey = key.Replace(" ", "_").Replace("/", "_");
        string path = $"{MaterialFolder}/MVP_{safeKey}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                ?? Shader.Find("Universal Render Pipeline/Simple Lit")
                ?? Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color");
            material = new Material(shader != null ? shader : Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        else
            material.color = color;

        EditorUtility.SetDirty(material);
        MaterialCache[key] = material;
        return material;
    }

    static void SetObjectReference(Object target, string propertyName, Object value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void SetObjectArray(Object target, string propertyName, Object[] values)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null && prop.isArray)
        {
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void SetString(Object target, string propertyName, string value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.stringValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    static void SetBool(Object target, string propertyName, bool value)
    {
        var so = new SerializedObject(target);
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop != null)
        {
            prop.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
