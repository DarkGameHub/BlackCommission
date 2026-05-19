using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

public static class ProjectSetup
{
    [MenuItem("Tools/Accident Squad/Setup All (Run This First!)")]
    static void SetupAll()
    {
        if (UnityEditor.EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("请先停止运行",
                "请先退出 Play 模式（点 ▶ 按钮），再运行 Setup All。", "OK");
            return;
        }

        var inputActionsAsset = AssetDatabase.LoadAssetAtPath<Object>(
            "Assets/_Project/Scripts/Player/PlayerInputActions.inputactions");
        if (inputActionsAsset == null)
        {
            EditorUtility.DisplayDialog("缺少文件",
                "找不到 PlayerInputActions.inputactions。\n请确认文件在 Assets/_Project/Scripts/Player/ 里。", "OK");
            return;
        }

        EnsureFolders();

        var playerPrefab   = CreatePlayerPrefab();
        if (playerPrefab == null) { Debug.LogError("Player prefab creation failed."); return; }

        var pumpPrefab     = CreatePumpPrefab();
        var evacPrefab     = CreateEvacPrefab();
        var survivorLight  = CreateSurvivorLightPrefab();
        var survivorHeavy  = CreateSurvivorHeavyPrefab();
        var robotPrefab    = CreateRobotPrefab();
        var fusePrefab     = CreateFusePrefab();
        var toolboxPrefab  = CreateToolboxPrefab();
        var batteryPrefab  = CreateBatteryPrefab();
        var evidencePrefab = CreateEvidenceBoxPrefab();
        var doorPumpRoomPrefab    = CreateDoorPrefabNamed("Door_PumpRoom",    DoorController.DoorType.Normal,       new Color(0.45f, 0.3f, 0.15f));
        var doorPowerRoomPrefab   = CreateDoorPrefabNamed("Door_PowerRoom",   DoorController.DoorType.Normal,       new Color(0.45f, 0.3f, 0.15f));
        var doorLockedPrefab      = CreateDoorPrefabNamed("Door_Locked",      DoorController.DoorType.Locked,       new Color(0.6f,  0.2f, 0.1f));
        var doorShortcutPrefab    = CreateDoorPrefabNamed("Door_Shortcut",    DoorController.DoorType.Shortcut,     new Color(0.2f,  0.5f, 0.2f));
        var doorWaterBlockedPrefab= CreateDoorPrefabNamed("Door_WaterBlocked",DoorController.DoorType.WaterBlocked, new Color(0.1f,  0.3f, 0.6f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        SetupHQScene(playerPrefab);
        SetupMallScene(playerPrefab, pumpPrefab, evacPrefab, survivorLight, survivorHeavy, robotPrefab,
            fusePrefab, toolboxPrefab, batteryPrefab, evidencePrefab,
            doorPumpRoomPrefab, doorPowerRoomPrefab, doorLockedPrefab, doorShortcutPrefab, doorWaterBlockedPrefab);

        AddSceneToBuildSettings("Assets/_Project/Scenes/HQ.unity");
        AddSceneToBuildSettings("Assets/_Project/Scenes/Mall_B2.unity");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("完成!",
            "配置完成!\n所有 Prefabs 和场景已更新。\n\n打开 Mall_B2 场景开始测试!", "开始!");
    }

    // ─────────────────────────────── Folders ─────────────────────────────────

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs"))
            AssetDatabase.CreateFolder("Assets/_Project", "Prefabs");
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Player"))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Player");
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Prefabs/Mission"))
            AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "Mission");
        if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes"))
            AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
    }

    // ─────────────────────────────── Player Prefab ───────────────────────────

    static GameObject CreatePlayerPrefab()
    {
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";

        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = new Vector3(0, 1f, 0);

        player.AddComponent<NetworkObject>();

        var cameraRoot = new GameObject("CameraRoot");
        cameraRoot.transform.SetParent(player.transform);
        cameraRoot.transform.localPosition = new Vector3(0, 0.7f, 0);

        var camGO = new GameObject("PlayerCamera");
        camGO.transform.SetParent(cameraRoot.transform);
        camGO.transform.localPosition = Vector3.zero;
        camGO.AddComponent<Camera>();
        camGO.AddComponent<AudioListener>();

        var holdPoint = new GameObject("HoldPoint");
        holdPoint.transform.SetParent(camGO.transform);
        holdPoint.transform.localPosition = new Vector3(0, -0.2f, 0.6f);

        // Flashlight (SpotLight child of camera)
        var flashlightGO = new GameObject("Flashlight");
        flashlightGO.transform.SetParent(camGO.transform);
        flashlightGO.transform.localPosition = Vector3.zero;
        var spotLight = flashlightGO.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.range = 15f;
        spotLight.spotAngle = 60f;
        spotLight.intensity = 1f;
        spotLight.enabled = false;

        var pc = player.AddComponent<PlayerController>();
        SetField(pc, "cameraRoot", cameraRoot.transform);

        var pcc = cameraRoot.AddComponent<PlayerCameraController>();
        SetField(pcc, "playerBody", player.transform);

        var carry = player.AddComponent<CarrySystem>();
        SetField(carry, "holdPoint", holdPoint.transform);

        player.AddComponent<PlayerInteraction>();
        player.AddComponent<PlayerHealth>();
        player.AddComponent<FlashlightController>();

        const string path = "Assets/_Project/Prefabs/Player/Player.prefab";
        var prefab = PrefabUtility.SaveAsPrefabAsset(player, path);
        Object.DestroyImmediate(player);
        Debug.Log($"[Setup] Player prefab → {path}");
        return prefab;
    }

    // ─────────────────────────────── Pump Prefab ─────────────────────────────

    static GameObject CreatePumpPrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/Pump.prefab";

        var root = new GameObject("Pump");
        root.AddComponent<NetworkObject>();
        root.AddComponent<PumpRepairInteraction>();
        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(1f, 3f, 1f);
        col.center = new Vector3(0f, 1.5f, 0f);

        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "PumpBody";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        SetColor(body, new Color(0.55f, 0.35f, 0.1f));
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] Pump prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── EvacuationPoint Prefab ──────────────────────

    static GameObject CreateEvacPrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/EvacuationPoint.prefab";

        var root = new GameObject("EvacuationPoint");
        root.AddComponent<NetworkObject>();
        root.AddComponent<EvacuationPoint>();
        var col = root.AddComponent<BoxCollider>();
        col.size   = new Vector3(4f, 3f, 4f);
        col.center = new Vector3(0f, 1.5f, 0f);

        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "EvacMarker";
        marker.transform.SetParent(root.transform);
        marker.transform.localPosition = Vector3.zero;
        marker.transform.localScale = new Vector3(4f, 0.05f, 4f);
        SetColor(marker, new Color(0.1f, 0.9f, 0.2f));
        Object.DestroyImmediate(marker.GetComponent<BoxCollider>());

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] EvacuationPoint prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── SurvivorLight Prefab ────────────────────────

    static GameObject CreateSurvivorLightPrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/SurvivorLight.prefab";

        var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "SurvivorLight";
        SetColor(root, new Color(1f, 0.9f, 0.1f));
        Object.DestroyImmediate(root.GetComponent<CapsuleCollider>());

        root.AddComponent<NetworkObject>();
        root.AddComponent<NavMeshAgent>();

        var col = root.AddComponent<BoxCollider>();
        col.size   = new Vector3(0.8f, 2f, 0.8f);
        col.center = new Vector3(0f, 1f, 0f);

        var sc = root.AddComponent<SurvivorController>();
        var so = new SerializedObject(sc);
        var injuryProp = so.FindProperty("injuryLevel");
        if (injuryProp != null) { injuryProp.enumValueIndex = (int)SurvivorController.InjuryLevel.Light; so.ApplyModifiedPropertiesWithoutUndo(); }

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] SurvivorLight prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── SurvivorHeavy Prefab ────────────────────────

    static GameObject CreateSurvivorHeavyPrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/SurvivorHeavy.prefab";

        var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        root.name = "SurvivorHeavy";
        SetColor(root, new Color(1f, 0.4f, 0.1f));
        Object.DestroyImmediate(root.GetComponent<CapsuleCollider>());

        root.AddComponent<NetworkObject>();
        root.AddComponent<NavMeshAgent>();

        var col = root.AddComponent<BoxCollider>();
        col.size   = new Vector3(0.8f, 2f, 0.8f);
        col.center = new Vector3(0f, 1f, 0f);

        var sc = root.AddComponent<SurvivorController>();
        var so = new SerializedObject(sc);
        var injuryProp = so.FindProperty("injuryLevel");
        if (injuryProp != null) { injuryProp.enumValueIndex = (int)SurvivorController.InjuryLevel.Heavy; so.ApplyModifiedPropertiesWithoutUndo(); }

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] SurvivorHeavy prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── CleaningRobot Prefab ────────────────────────

    static GameObject CreateRobotPrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/CleaningRobot.prefab";

        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "CleaningRobot";
        root.transform.localScale = new Vector3(0.8f, 0.6f, 1.2f);
        SetColor(root, new Color(0.7f, 0.7f, 0.7f));
        Object.DestroyImmediate(root.GetComponent<BoxCollider>());

        root.AddComponent<NetworkObject>();
        root.AddComponent<NavMeshAgent>();

        var col = root.AddComponent<BoxCollider>();
        col.size   = new Vector3(1f, 1f, 1f);
        col.center = Vector3.zero;

        root.AddComponent<CleaningRobot>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] CleaningRobot prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── Fuse Prefab ────────────────────────────────

    static GameObject CreateFusePrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/Fuse.prefab";

        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Fuse";
        root.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
        SetColor(root, new Color(1f, 0.95f, 0.2f));
        Object.DestroyImmediate(root.GetComponent<BoxCollider>());

        root.AddComponent<NetworkObject>();
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        var col = root.AddComponent<BoxCollider>();
        col.size = Vector3.one;
        root.AddComponent<FuseItem>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] Fuse prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── Toolbox Prefab ─────────────────────────────

    static GameObject CreateToolboxPrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/Toolbox.prefab";

        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "Toolbox";
        root.transform.localScale = new Vector3(0.3f, 0.2f, 0.2f);
        SetColor(root, new Color(0.9f, 0.15f, 0.1f));
        Object.DestroyImmediate(root.GetComponent<BoxCollider>());

        root.AddComponent<NetworkObject>();
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 2f;
        var col = root.AddComponent<BoxCollider>();
        col.size = Vector3.one;
        root.AddComponent<ToolboxItem>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] Toolbox prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── Battery Prefab ─────────────────────────────

    static GameObject CreateBatteryPrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/TemporaryBattery.prefab";

        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "TemporaryBattery";
        root.transform.localScale = new Vector3(0.4f, 0.3f, 0.3f);
        SetColor(root, new Color(0.2f, 0.5f, 0.9f));
        Object.DestroyImmediate(root.GetComponent<BoxCollider>());

        root.AddComponent<NetworkObject>();
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 5f;
        var col = root.AddComponent<BoxCollider>();
        col.size = Vector3.one;
        var battery = root.AddComponent<TemporaryBatteryItem>();

        var so = new SerializedObject(battery);
        var heavyProp = so.FindProperty("isHeavy");
        if (heavyProp != null) { heavyProp.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] TemporaryBattery prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── EvidenceBox Prefab ──────────────────────────

    static GameObject CreateEvidenceBoxPrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/EvidenceBox.prefab";

        var root = GameObject.CreatePrimitive(PrimitiveType.Cube);
        root.name = "EvidenceBox";
        root.transform.localScale = new Vector3(0.3f, 0.25f, 0.25f);
        SetColor(root, new Color(0.55f, 0.35f, 0.15f));
        Object.DestroyImmediate(root.GetComponent<BoxCollider>());

        root.AddComponent<NetworkObject>();
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 1.5f;
        var col = root.AddComponent<BoxCollider>();
        col.size = Vector3.one;
        root.AddComponent<EvidenceBoxItem>();

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] EvidenceBox prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── Door Prefabs ───────────────────────────────────
    // Each door in the scene MUST come from its own unique prefab file.
    // NGO derives GlobalObjectIdHash from the prefab asset GUID, so two scene-placed
    // instances of the same prefab always collide.

    static GameObject CreateDoorPrefabNamed(string doorName, DoorController.DoorType type, Color color)
    {
        string path = $"Assets/_Project/Prefabs/Mission/{doorName}.prefab";

        var root = new GameObject(doorName);
        root.AddComponent<NetworkObject>();
        var dc = root.AddComponent<DoorController>();

        var so = new SerializedObject(dc);
        var typeProp = so.FindProperty("doorType");
        if (typeProp != null) { typeProp.enumValueIndex = (int)type; so.ApplyModifiedPropertiesWithoutUndo(); }

        var pivot = new GameObject("Pivot");
        pivot.transform.SetParent(root.transform);
        pivot.transform.localPosition = new Vector3(-0.5f, 0, 0);

        var doorBody = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorBody.name = "DoorBody";
        doorBody.transform.SetParent(pivot.transform);
        doorBody.transform.localPosition = new Vector3(0.5f, 1.25f, 0);
        doorBody.transform.localScale = new Vector3(1f, 2.5f, 0.1f);
        SetColor(doorBody, color);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] {doorName} prefab → {path}");
        return prefab;
    }

    // ─────────────────────────────── HQ Scene ────────────────────────────────

    static void SetupHQScene(GameObject playerPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var defaultCam = GameObject.Find("Main Camera");
        if (defaultCam != null) Object.DestroyImmediate(defaultCam);

        Color wallColor  = new Color(0.6f, 0.55f, 0.45f);
        Color floorColor = new Color(0.35f, 0.3f, 0.25f);
        Color deskColor  = new Color(0.45f, 0.3f, 0.15f);
        Color shelfColor = new Color(0.3f, 0.3f, 0.35f);

        // ── Office room (6x3x5 meters) ────────────────────────
        var floor = CreateBoxColored("Floor", Vector3.zero, new Vector3(6, 0.1f, 5), floorColor);
        CreateBoxColored("Ceiling", new Vector3(0, 3, 0), new Vector3(6, 0.1f, 5), wallColor);
        CreateBoxColored("Wall_Back",  new Vector3(0, 1.5f, -2.5f), new Vector3(6, 3, 0.15f), wallColor);
        CreateBoxColored("Wall_Left",  new Vector3(-3, 1.5f, 0), new Vector3(0.15f, 3, 5), wallColor);
        CreateBoxColored("Wall_Right", new Vector3(3, 1.5f, 0), new Vector3(0.15f, 3, 5), wallColor);
        CreateBoxColored("Wall_Front", new Vector3(0, 1.5f, 2.5f), new Vector3(6, 3, 0.15f), wallColor);

        // ── Furniture ─────────────────────────────────────────
        // Desk (job board computer sits on this)
        CreateBoxColored("Desk", new Vector3(0, 0.4f, -1.8f), new Vector3(1.6f, 0.8f, 0.7f), deskColor);
        // Small monitor on desk (the job board)
        var jobBoardGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
        jobBoardGO.name = "JobBoard_Screen";
        jobBoardGO.transform.position = new Vector3(0, 1.1f, -2f);
        jobBoardGO.transform.localScale = new Vector3(0.6f, 0.45f, 0.05f);
        SetColor(jobBoardGO, new Color(0.1f, 0.8f, 0.3f));
        Object.DestroyImmediate(jobBoardGO.GetComponent<BoxCollider>());
        var jbCol = jobBoardGO.AddComponent<BoxCollider>();
        jbCol.size = new Vector3(1f, 1f, 3f);
        jbCol.center = Vector3.zero;
        jobBoardGO.AddComponent<JobBoard>();

        // Equipment shelf (right side)
        CreateBoxColored("Shelf", new Vector3(2.2f, 0.5f, -1.5f), new Vector3(1.2f, 1f, 0.5f), shelfColor);
        CreateBoxColored("Shelf_Top", new Vector3(2.2f, 1.05f, -1.5f), new Vector3(1.2f, 0.05f, 0.5f), shelfColor);

        // Whiteboard on back wall
        CreateBoxColored("Whiteboard", new Vector3(-1.5f, 1.6f, -2.35f), new Vector3(1.2f, 0.8f, 0.03f), Color.white);

        // "零事故 0 天" sign
        CreateBoxColored("Sign_ZeroAccident", new Vector3(1.5f, 2.2f, -2.35f), new Vector3(0.8f, 0.3f, 0.02f), new Color(1f, 0.3f, 0.1f));

        // Old fan on floor (atmosphere)
        CreateBoxColored("OldFan", new Vector3(-2.2f, 0.3f, 1f), new Vector3(0.3f, 0.6f, 0.3f), new Color(0.5f, 0.5f, 0.5f));

        // Exit door frame (atmosphere, south wall)
        CreateBoxColored("DoorFrame", new Vector3(0, 1.25f, 2.42f), new Vector3(0.9f, 2.5f, 0.1f), new Color(0.35f, 0.25f, 0.15f));

        // ── Lighting ──────────────────────────────────────────
        var lightGO = new GameObject("OfficeLight");
        lightGO.transform.position = new Vector3(0, 2.8f, 0);
        var pointLight = lightGO.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 8f;
        pointLight.intensity = 1.5f;
        pointLight.color = new Color(1f, 0.9f, 0.7f);

        // ── NetworkManager ────────────────────────────────────
        var nmGO = new GameObject("NetworkManager");
        var nm = nmGO.AddComponent<NetworkManager>();
        var transport = nmGO.AddComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", 7778);
        nm.NetworkConfig.NetworkTransport = transport;
        nm.NetworkConfig.PlayerPrefab = playerPrefab;
        nmGO.AddComponent<QuickNetworkUI>();
        nmGO.AddComponent<AutoPort>();

        var cmGO = new GameObject("ConnectionManager");
        cmGO.AddComponent<ConnectionManager>();

        var amGO = new GameObject("AudioManager");
        amGO.AddComponent<AudioSource>();
        amGO.AddComponent<AudioManager>();

        new GameObject("HQUI").AddComponent<HQController>();

        var settlementUI = new GameObject("SettlementUI");
        settlementUI.AddComponent<SettlementUIController>();

        new GameObject("DisconnectHandler").AddComponent<DisconnectHandler>();

        // ── Spawn ─────────────────────────────────────────────
        var spawnPoint = new GameObject("PlayerSpawnPoint");
        spawnPoint.transform.position = new Vector3(0, 1.1f, 1);

        var spawnMgr = new GameObject("HQSpawnManager");
        var mgr = spawnMgr.AddComponent<HQSpawnManager>();
        SetField(mgr, "spawnPoint", spawnPoint.transform);

        const string path = "Assets/_Project/Scenes/HQ.unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[Setup] HQ scene → {path}");
    }

    static GameObject CreateBoxColored(string name, Vector3 pos, Vector3 scale, Color color)
    {
        var go = CreateBox(name, pos, scale);
        SetColor(go, color);
        return go;
    }

    // ──────────────────────────── Mall_B2 Scene ──────────────────────────────

    static void SetupMallScene(GameObject playerPrefab, GameObject pumpPrefab, GameObject evacPrefab,
        GameObject survivorLight, GameObject survivorHeavy, GameObject robotPrefab,
        GameObject fusePrefab, GameObject toolboxPrefab, GameObject batteryPrefab,
        GameObject evidencePrefab,
        GameObject doorPumpRoomPrefab, GameObject doorPowerRoomPrefab,
        GameObject doorLockedPrefab, GameObject doorShortcutPrefab, GameObject doorWaterBlockedPrefab)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        var defaultCam = GameObject.Find("Main Camera");
        if (defaultCam != null) Object.DestroyImmediate(defaultCam);

        // ── Geometry ──────────────────────────────────────────────
        CreateBox("Ground",         Vector3.zero,                   new Vector3(30, 0.2f, 30));
        CreateBox("Wall_N",         new Vector3(0,   2.5f,  15),    new Vector3(30, 5, 0.5f));
        CreateBox("Wall_S",         new Vector3(0,   2.5f, -15),    new Vector3(30, 5, 0.5f));
        CreateBox("Wall_E",         new Vector3(15,  2.5f,   0),    new Vector3(0.5f, 5, 30));
        CreateBox("Wall_W",         new Vector3(-15, 2.5f,   0),    new Vector3(0.5f, 5, 30));
        CreateBox("Ceiling",        new Vector3(0,   5.1f,   0),    new Vector3(30, 0.2f, 30));
        CreateBox("Divider_Center", new Vector3(0,   2.5f,   0),    new Vector3(0.5f, 5, 20));

        for (int i = -2; i <= 2; i++)
        {
            CreateBox($"Shop_L_{i}", new Vector3(-7, 1.5f, i * 4), new Vector3(4, 3, 3));
            CreateBox($"Shop_R_{i}", new Vector3( 7, 1.5f, i * 4), new Vector3(4, 3, 3));
        }

        CreateBox("PumpRoom_Wall",   new Vector3(-10, 2.5f, -12), new Vector3(8, 5, 0.5f));
        CreateBox("PumpRoom_Wall_L", new Vector3(-14, 2.5f,  -9), new Vector3(0.5f, 5, 6));

        // Power control room area
        CreateBox("PowerRoom_Wall",   new Vector3(-10, 2.5f, -6), new Vector3(6, 5, 0.5f));
        CreateBox("PowerRoom_Wall_R", new Vector3(-7, 2.5f, -9), new Vector3(0.5f, 5, 6));

        // ── Water surface ─────────────────────────────────────────
        var waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "WaterSurface";
        waterPlane.transform.localScale = new Vector3(3f, 1f, 3f);
        waterPlane.transform.position = new Vector3(0f, -0.1f, 0f);
        SetColor(waterPlane, new Color(0.15f, 0.4f, 0.9f));
        Object.DestroyImmediate(waterPlane.GetComponent<MeshCollider>());
        waterPlane.AddComponent<WaterVisual>();

        // ── Mission prefab instances ───────────────────────────────
        if (pumpPrefab != null)
        {
            var pump = (GameObject)PrefabUtility.InstantiatePrefab(pumpPrefab);
            pump.transform.position = new Vector3(-11f, 0f, -13f);
        }

        if (evacPrefab != null)
        {
            var evac = (GameObject)PrefabUtility.InstantiatePrefab(evacPrefab);
            evac.transform.position = new Vector3(12f, 0.1f, 12f);
        }

        if (survivorLight != null)
        {
            var sl = (GameObject)PrefabUtility.InstantiatePrefab(survivorLight);
            sl.transform.position = new Vector3(-5f, 1f, -5f);
        }

        if (survivorHeavy != null)
        {
            var sh = (GameObject)PrefabUtility.InstantiatePrefab(survivorHeavy);
            sh.transform.position = new Vector3(-11f, 1f, -8f);
        }

        // ── Equipment items ───────────────────────────────────────
        if (fusePrefab != null)
        {
            var fuse = (GameObject)PrefabUtility.InstantiatePrefab(fusePrefab);
            fuse.transform.position = new Vector3(-9f, 0.5f, -8f); // Power control room
        }

        if (toolboxPrefab != null)
        {
            var toolbox = (GameObject)PrefabUtility.InstantiatePrefab(toolboxPrefab);
            toolbox.transform.position = new Vector3(3f, 0.5f, -3f); // Shop area
        }

        if (batteryPrefab != null)
        {
            var battery = (GameObject)PrefabUtility.InstantiatePrefab(batteryPrefab);
            battery.transform.position = new Vector3(5f, 0.5f, -8f); // Storage area
        }

        if (evidencePrefab != null)
        {
            var evidence = (GameObject)PrefabUtility.InstantiatePrefab(evidencePrefab);
            evidence.transform.position = new Vector3(-8f, 0.5f, 3f); // Shop area
        }

        // ── Doors (each door is its own prefab asset — NGO requires unique GlobalObjectIdHash per scene instance) ──
        if (doorPumpRoomPrefab != null)
        {
            var door1 = (GameObject)PrefabUtility.InstantiatePrefab(doorPumpRoomPrefab);
            door1.name = "Door_PumpRoom";
            door1.transform.position = new Vector3(-6f, 0f, -12f);
        }
        if (doorPowerRoomPrefab != null)
        {
            var door2 = (GameObject)PrefabUtility.InstantiatePrefab(doorPowerRoomPrefab);
            door2.name = "Door_PowerRoom";
            door2.transform.position = new Vector3(-7f, 0f, -6f);
        }
        if (doorShortcutPrefab != null)
        {
            var door3 = (GameObject)PrefabUtility.InstantiatePrefab(doorShortcutPrefab);
            door3.name = "Door_Shortcut";
            door3.transform.position = new Vector3(0f, 0f, -5f);
        }
        if (doorWaterBlockedPrefab != null)
        {
            var door4 = (GameObject)PrefabUtility.InstantiatePrefab(doorWaterBlockedPrefab);
            door4.name = "Door_WaterBlocked";
            door4.transform.position = new Vector3(-3f, 0f, -10f);
        }
        if (doorLockedPrefab != null)
        {
            var door5 = (GameObject)PrefabUtility.InstantiatePrefab(doorLockedPrefab);
            door5.name = "Door_Locked";
            door5.transform.position = new Vector3(6f, 0f, -8f);
        }

        // ── Patrol waypoints ──────────────────────────────────────
        Vector3[] waypointPositions =
        {
            new Vector3( 3f, 0.5f,  8f),
            new Vector3(-3f, 0.5f,  5f),
            new Vector3( 5f, 0.5f, -5f),
            new Vector3(-5f, 0.5f, -8f),
        };

        var waypoints = new GameObject[waypointPositions.Length];
        for (int i = 0; i < waypointPositions.Length; i++)
        {
            waypoints[i] = new GameObject($"WaypointRobot_{i}");
            waypoints[i].transform.position = waypointPositions[i];
        }

        if (robotPrefab != null)
        {
            var robot = (GameObject)PrefabUtility.InstantiatePrefab(robotPrefab);
            robot.transform.position = new Vector3(0f, 0.5f, 0f);

            var robotSo = new SerializedObject(robot.GetComponent<CleaningRobot>());

            var patrolProp = robotSo.FindProperty("patrolPoints");
            if (patrolProp != null)
            {
                patrolProp.arraySize = waypoints.Length;
                for (int i = 0; i < waypoints.Length; i++)
                    patrolProp.GetArrayElementAtIndex(i).objectReferenceValue = waypoints[i].transform;
                robotSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var layerProp = robotSo.FindProperty("playerLayer");
            if (layerProp != null)
            {
                layerProp.intValue = -1;
                robotSo.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ── Managers ──────────────────────────────────────────────
        var nmGO = new GameObject("NetworkManager");
        var nm = nmGO.AddComponent<NetworkManager>();
        var transport = nmGO.AddComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", 7778);
        nm.NetworkConfig.NetworkTransport = transport;
        if (playerPrefab) nm.NetworkConfig.PlayerPrefab = playerPrefab;
        nmGO.AddComponent<QuickNetworkUI>();
        nmGO.AddComponent<AutoPort>();

        new GameObject("GameManager").AddComponent<GameManager>();
        new GameObject("WaterLevelManager").AddComponent<WaterLevelManager>();
        new GameObject("SettlementManager").AddComponent<SettlementManager>();
        new GameObject("SimpleHUD").AddComponent<SimpleHUD>();
        new GameObject("PhaseBroadcaster").AddComponent<PhaseBroadcaster>();
        new GameObject("DisconnectHandler").AddComponent<DisconnectHandler>();

        var settlementUI = new GameObject("SettlementUI");
        settlementUI.AddComponent<SettlementUIController>();

        var amGO = new GameObject("AudioManager");
        amGO.AddComponent<AudioSource>();
        amGO.AddComponent<AudioManager>();

        // ── Lighting ──────────────────────────────────────────────
        var sunGO = new GameObject("DirectionalLight");
        sunGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        var sunLight = sunGO.AddComponent<Light>();
        sunLight.type = LightType.Directional;
        sunLight.intensity = 0.3f;
        sunLight.color = new Color(0.5f, 0.7f, 1f);

        // ── Spawn points + spawn manager ─────────────────────────
        var missionSpawn = new GameObject("MissionSpawnPoint");
        missionSpawn.transform.position = new Vector3(-2f, 1.1f, 10f);

        var missionSpawnMgr = new GameObject("MissionSpawnManager");
        var msm = missionSpawnMgr.AddComponent<HQSpawnManager>();
        SetField(msm, "spawnPoint", missionSpawn.transform);

        const string path = "Assets/_Project/Scenes/Mall_B2.unity";
        EditorSceneManager.SaveScene(scene, path);
        Debug.Log($"[Setup] Mall_B2 scene → {path}");

        // ── NavMesh bake ──────────────────────────────────────────
        try
        {
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            EditorSceneManager.SaveScene(scene, path);
            Debug.Log("[Setup] NavMesh baked for Mall_B2.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[Setup] NavMesh bake skipped: {e.Message}");
        }
    }

    // ─────────────────────────────── Helpers ─────────────────────────────────

    static GameObject CreateBox(string name, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.position = pos; go.transform.localScale = scale;
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
        return go;
    }

    static void SetColor(GameObject go, Color color)
    {
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;
        var mat = new Material(rend.sharedMaterial) { color = color };
        rend.sharedMaterial = mat;
    }

    static void SetField(Object target, string fieldName, Object value)
    {
        var so = new SerializedObject(target);
        var prop = so.FindProperty(fieldName);
        if (prop != null) { prop.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        else Debug.LogWarning($"[Setup] Field '{fieldName}' not found on {target.GetType().Name}");
    }

    static void AddSceneToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == scenePath) return;

        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        EditorBuildSettings.scenes = list.ToArray();
        Debug.Log($"[Setup] Added to Build Settings: {scenePath}");
    }
}
