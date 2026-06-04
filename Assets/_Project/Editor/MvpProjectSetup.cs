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
    const string LakeScenePath = "Assets/_Project/Scenes/Lake_DiveKey_01.unity";
    const string LakeTaskAssetPath = "Assets/_Project/Settings/Tasks/LakeDiveKeyRetrieval.asset";
    const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";
    const string MaterialFolder = "Assets/_Project/Settings/Materials";

    static readonly Dictionary<string, Material> MaterialCache = new Dictionary<string, Material>();

    [MenuItem("Tools/Black Commission/MVP/Setup Lake MVP")]
    public static void SetupLakeMvpMenu() => SetupLakeMvp();

    public static void SetupLakeMvp()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("请先停止运行", "请先退出 Play 模式，再运行 MVP Setup。", "OK");
            return;
        }

        EnsureFolders();
        OfficeTaskDefinition task = EnsureLakeDiveTask();
        PatchPlayerPrefab();
        SetupHq(task);            // points the HQ computer's task at the lake dive (replaces School as the active mission)
        SetupLakeScene();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(HqScenePath);

        EditorUtility.DisplayDialog(
            "湖底潜水 MVP 已配置",
            "已生成湖边潜水取钥匙 MVP（HQ 电脑现在派遣的是这关）。\n\n测试流程:\n1. 打开 HQ 场景并 Play\n2. 点 Start Host\n3. 靠近电脑按 E 接任务，上车按 Space 出发\n4. 走下斜坡进湖，潜水（Space 上浮 / Ctrl 下潜）\n5. 注意氧气条，到湖底拾取钥匙\n6. 游回岸边走上斜坡，靠近车按 E 上车、Space 返程\n7. 回事务所按 E 领取奖励",
            "开始测试");
    }

    static OfficeTaskDefinition EnsureLakeDiveTask()
    {
        var task = AssetDatabase.LoadAssetAtPath<OfficeTaskDefinition>(LakeTaskAssetPath);
        if (task == null)
        {
            task = ScriptableObject.CreateInstance<OfficeTaskDefinition>();
            AssetDatabase.CreateAsset(task, LakeTaskAssetPath);
        }

        task.taskId = "lake_dive_key_01";
        task.title = "打捞湖底钥匙";
        task.category = MvpTaskCategory.LostItemRecovery;
        task.client = "守口如瓶的委托人";
        task.description = "委托车会停在山间湖泊岸边。下水潜到湖底找回一把钥匙并带回车上；注意氧气，憋不住就上浮换气。";
        task.locationName = "山间湖泊";
        task.sceneName = "Lake_DiveKey_01";
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
        task.moneyReward = 400;
        task.reputationReward = 8;
        task.experienceReward = 120;
        task.failureConsolationMoney = 20;
        task.failureReputationPenalty = -2;
        task.failureExperience = 0;
        EditorUtility.SetDirty(task);
        return task;
    }

    static void SetupLakeScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.10f, 0.14f, 0.17f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.10f, 0.13f, 0.16f);
        RenderSettings.fogDensity = 0.012f;

        var root = new GameObject("MVP_Lake_DiveKey_01");

        CreateDirectionalLight();
        CreateLakeGeometry(root.transform);
        CreateLakeProps(root.transform);
        CreateLakeMissionObjects(root.transform);
        EnsureMvpHud();

        try
        {
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[MVP Setup] NavMesh bake skipped: {ex.Message}");
        }

        EditorSceneManager.SaveScene(scene, LakeScenePath);
        Debug.Log($"[MVP Setup] Lake scene saved: {LakeScenePath}");
    }

    static void CreateLakeGeometry(Transform parent)
    {
        // SHELL ONLY — placeholder lake bowl: a south shore (van/spawn/exit), a ramp down into
        // the water, and a sunken lakebed to the north. Surface sits at y = 0. Import real art
        // and align it to these anchors later.
        CreateBox("Shore_Ground", new Vector3(0f, -0.1f, -14f), new Vector3(30f, 0.2f, 16f),
            new Color(0.20f, 0.22f, 0.20f), parent);

        // Ramp bridging shore (y≈0) down to the lakebed (y≈-4). Overlaps both ends so there is
        // no gap for the CharacterController; ~28° slope is within the default 45° slope limit.
        var ramp = CreateBox("Lake_Ramp", new Vector3(0f, -2f, -1f), new Vector3(11f, 0.4f, 15f),
            new Color(0.18f, 0.20f, 0.19f), parent, false);
        ramp.transform.rotation = Quaternion.Euler(28f, 0f, 0f);

        CreateBox("Lakebed", new Vector3(0f, -4f, 9f), new Vector3(28f, 0.4f, 26f),
            new Color(0.13f, 0.16f, 0.15f), parent);

        // Low containment lips around the lake so the water box reads as a basin.
        CreateBox("Lake_Lip_N", new Vector3(0f, -1.6f, 22.2f), new Vector3(28f, 5f, 0.6f),
            new Color(0.16f, 0.18f, 0.17f), parent);
        CreateBox("Lake_Lip_E", new Vector3(14.2f, -1.6f, 9f), new Vector3(0.6f, 5f, 26f),
            new Color(0.16f, 0.18f, 0.17f), parent);
        CreateBox("Lake_Lip_W", new Vector3(-14.2f, -1.6f, 9f), new Vector3(0.6f, 5f, 26f),
            new Color(0.16f, 0.18f, 0.17f), parent);

        // Transparent surface plane (cheap placeholder — no water shader).
        var surface = GameObject.CreatePrimitive(PrimitiveType.Plane);
        surface.name = "Lake_WaterSurface";
        surface.transform.SetParent(parent);
        surface.transform.position = new Vector3(0f, -0.05f, 7f);
        surface.transform.localScale = new Vector3(3.0f, 1f, 3.2f);   // Plane is 10x10 units
        if (surface.TryGetComponent<Collider>(out var surfCol))
            Object.DestroyImmediate(surfCol);                         // don't block diving
        ApplyWaterMaterial(surface);
    }

    static void CreateLakeProps(Transform parent)
    {
        var moonGlow = new GameObject("ShoreLight");
        moonGlow.transform.SetParent(parent);
        moonGlow.transform.position = new Vector3(0f, 3.2f, -14f);
        var shore = moonGlow.AddComponent<Light>();
        shore.type = LightType.Point;
        shore.range = 16f;
        shore.intensity = 0.9f;
        shore.color = new Color(0.62f, 0.74f, 0.82f);

        // Cyan glow near the lakebed so divers can orient in the murk.
        var deepGlow = new GameObject("DeepWaterGlow");
        deepGlow.transform.SetParent(parent);
        deepGlow.transform.position = new Vector3(0f, -3f, 9f);
        var deep = deepGlow.AddComponent<Light>();
        deep.type = LightType.Point;
        deep.range = 18f;
        deep.intensity = 1.1f;
        deep.color = new Color(0.20f, 0.55f, 0.70f);
    }

    static void CreateLakeMissionObjects(Transform parent)
    {
        var spawn = new GameObject("PlayerSpawnPoint");
        spawn.transform.SetParent(parent);
        spawn.transform.SetPositionAndRotation(new Vector3(0f, 0.1f, -16f), Quaternion.identity);

        var spawnManager = new GameObject("LakeSpawnManager");
        spawnManager.transform.SetParent(parent);
        var manager = spawnManager.AddComponent<HQSpawnManager>();
        SetObjectReference(manager, "spawnPoint", spawn.transform);

        // Mission manager (reused as-is — the key is its "lost item").
        var missionManagerSrc = new GameObject("LostItemMissionManager");
        missionManagerSrc.AddComponent<NetworkObject>();
        missionManagerSrc.AddComponent<LostItemMissionManager>();
        MakePrefabInstance("LostItemMissionManager", missionManagerSrc, Vector3.zero, parent);

        // The key on the lakebed.
        var keyPos = new Vector3(4f, -3.4f, 12f);
        var keySrc = CreateBox("LakeKey", keyPos, new Vector3(0.32f, 0.1f, 0.5f),
            new Color(0.95f, 0.82f, 0.25f), parent, false);
        keySrc.AddComponent<NetworkObject>();
        keySrc.AddComponent<LakeKeyItem>();
        var keyGlow = new GameObject("KeyGlow");
        keyGlow.transform.SetParent(keySrc.transform);
        keyGlow.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        var glow = keyGlow.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 4f;
        glow.intensity = 1.4f;
        glow.color = new Color(1f, 0.86f, 0.3f);
        MakePrefabInstance("LakeKey", keySrc, keyPos, parent);

        // Boarding / return point at the van on the shore (reuse SchoolExitPoint).
        var exitPos = new Vector3(0f, 0.08f, -13f);
        var exitSrc = CreateBox("LakeExitPoint", exitPos, new Vector3(4.4f, 0.16f, 2.2f),
            new Color(0.1f, 0.7f, 0.42f), parent, false);
        exitSrc.AddComponent<NetworkObject>();
        if (exitSrc.TryGetComponent<BoxCollider>(out var exitCollider))
            exitCollider.isTrigger = true;
        exitSrc.AddComponent<SchoolExitPoint>();
        MakePrefabInstance("LakeExitPoint", exitSrc, exitPos, parent);

        // Optional van visual on the shore (flavor only — no networking).
        var vanPrefab = Resources.Load<GameObject>("GeneratedArt/ASV4_SecondHandDispatchVan");
        if (vanPrefab != null)
        {
            var van = (GameObject)PrefabUtility.InstantiatePrefab(vanPrefab);
            van.name = "ShoreVan";
            van.transform.SetParent(parent);
            van.transform.SetPositionAndRotation(new Vector3(0f, 0f, -17f), Quaternion.Euler(0f, 90f, 0f));
        }

        // Water body + underwater effects. Trigger top is the surface (y = 0).
        var water = new GameObject("WaterVolume");
        water.transform.SetParent(parent);
        water.transform.position = new Vector3(0f, -2f, 7f);
        var waterBox = water.AddComponent<BoxCollider>();
        waterBox.isTrigger = true;
        waterBox.size = new Vector3(30f, 4f, 32f);   // top at y=0, bottom at y=-4; covers the ramp mouth
        water.AddComponent<WaterVolume>();
    }

    static void ApplyWaterMaterial(GameObject go)
    {
        if (!go.TryGetComponent<Renderer>(out var renderer)) return;

        string path = $"{MaterialFolder}/MVP_lake_water.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        // URP Lit transparent setup.
        material.SetFloat("_Surface", 1f);          // 0 = opaque, 1 = transparent
        material.SetFloat("_Blend", 0f);            // alpha blend
        material.SetFloat("_ZWrite", 0f);
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        Color water = new Color(0.10f, 0.36f, 0.46f, 0.55f);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", water);
        else material.color = water;

        EditorUtility.SetDirty(material);
        renderer.sharedMaterial = material;
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/_Project", "Settings");
        EnsureFolder("Assets/_Project/Settings", "Tasks");
        EnsureFolder("Assets/_Project/Settings", "Materials");
        EnsureFolder("Assets/_Project", "Scenes");
        EnsureFolder("Assets/_Project/Prefabs", "Mission");
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

        if (root.GetComponent<PlayerOxygen>() == null)
            root.AddComponent<PlayerOxygen>();

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
        var orderedPaths = new List<string> { HqScenePath, LakeScenePath };
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
            if (!System.IO.File.Exists(existing.path)) continue;   // drop deleted scenes (e.g. old School)
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
