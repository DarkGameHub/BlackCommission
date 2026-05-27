using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.AI;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class ProjectSetup
{
    static Material _baseMat;
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
        EnsureURPPipeline();
        AssetDatabase.Refresh();
        EnsureBaseMaterial();

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

        // Build the MVP school scene in the same run so users only need one click
        MvpProjectSetup.SetupSchoolMvp();

        EditorUtility.DisplayDialog("完成!",
            "配置完成！\n\n MVP 测试流程:\n1. HQ 场景已自动打开\n2. 点 Play → Start Host\n3. 靠近办公室电脑按 [E] 接取「找回作业本」委托\n4. 自动进入学校场景，找到作业本\n5. 躲开怪物，回到绿色出口按 [E] 撤离\n6. 回事务所按 [E] 领取奖励\n\n快捷栏: 1=回血药  2=定身喷  3=诱饵  4=手电", "开始测试!");
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

    // ─────────────────────────────── URP Pipeline ─────────────────────────────

    static void EnsureURPPipeline()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Settings"))
            AssetDatabase.CreateFolder("Assets", "Settings");

        const string rendererPath = "Assets/Settings/URP-Renderer.asset";
        const string pipelinePath = "Assets/Settings/URP-Pipeline.asset";

        var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(rendererPath);
        if (rendererData == null)
        {
            rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, rendererPath);
        }

        var pipelineAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(pipelinePath);
        if (pipelineAsset == null)
        {
            pipelineAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var so = new SerializedObject(pipelineAsset);
            var rendererList = so.FindProperty("m_RendererDataList");
            if (rendererList != null)
            {
                rendererList.arraySize = 1;
                rendererList.GetArrayElementAtIndex(0).objectReferenceValue = rendererData;
            }
            var defaultIdx = so.FindProperty("m_DefaultRendererIndex");
            if (defaultIdx != null) defaultIdx.intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(pipelineAsset, pipelinePath);
        }

        GraphicsSettings.defaultRenderPipeline = pipelineAsset;
        QualitySettings.renderPipeline = pipelineAsset;
        AssetDatabase.SaveAssets();
        Debug.Log("[Setup] URP Pipeline configured.");
    }

    // ─────────────────────────────── Base Material ─────────────────────────────

    static void EnsureBaseMaterial()
    {
        const string matPath = "Assets/Settings/URP-BaseLit.mat";
        _baseMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (_baseMat != null && _baseMat.shader != null && _baseMat.shader.name != "Hidden/InternalErrorShader")
            return;

        // Delete broken material asset if it exists
        if (AssetDatabase.LoadAssetAtPath<Object>(matPath) != null)
            AssetDatabase.DeleteAsset(matPath);

        Shader shader = null;

        // 1) Load directly from URP package path — most reliable in Unity 6
        shader = AssetDatabase.LoadAssetAtPath<Shader>(
            "Packages/com.unity.render-pipelines.universal/Shaders/Lit.shader");
        if (shader != null)
            Debug.Log($"[Setup] Found shader via package path: {shader.name}");

        // 2) Get shader from pipeline's default material
        if (shader == null)
        {
            var pipeline = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (pipeline != null)
            {
                var defMat = pipeline.defaultMaterial;
                if (defMat != null && defMat.shader != null)
                {
                    shader = defMat.shader;
                    Debug.Log($"[Setup] Found shader via pipeline default material: {shader.name}");
                }
            }
        }

        // 3) Try SimpleLit from package
        if (shader == null)
        {
            shader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Packages/com.unity.render-pipelines.universal/Shaders/SimpleLit.shader");
            if (shader != null)
                Debug.Log($"[Setup] Found SimpleLit shader: {shader.name}");
        }

        // 4) Classic Shader.Find as last resort
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Lit");

        if (shader != null)
        {
            _baseMat = new Material(shader);
            AssetDatabase.CreateAsset(_baseMat, matPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Setup] Base URP material created with shader: {shader.name}");
        }
        else
        {
            Debug.LogError("[Setup] FAILED to find any URP shader. All objects will be purple.\n" +
                "Try: Window > Package Manager > Universal RP > Reimport");
        }
    }

    // ─────────────────────────────── Player Prefab ───────────────────────────

    static GameObject CreatePlayerPrefab()
    {
        var player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";

        SetColor(player, new Color(0.3f, 0.5f, 0.7f));
        Object.DestroyImmediate(player.GetComponent<CapsuleCollider>());
        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f; cc.radius = 0.4f; cc.center = new Vector3(0, 1f, 0);

        player.AddComponent<NetworkObject>();
        var netTransform = player.AddComponent<ClientNetworkTransform>();
        netTransform.SyncPositionX = true;
        netTransform.SyncPositionY = true;
        netTransform.SyncPositionZ = true;
        netTransform.SyncRotAngleY = true;
        netTransform.SyncRotAngleX = false;
        netTransform.SyncRotAngleZ = false;
        netTransform.SyncScaleX = false;
        netTransform.SyncScaleY = false;
        netTransform.SyncScaleZ = false;

        var cameraRoot = new GameObject("CameraRoot");
        cameraRoot.transform.SetParent(player.transform);
        cameraRoot.transform.localPosition = new Vector3(0, 0.7f, 0);

        var camGO = new GameObject("PlayerCamera");
        camGO.transform.SetParent(cameraRoot.transform);
        camGO.transform.localPosition = Vector3.zero;
        var cam = camGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f);
        cam.nearClipPlane = 0.1f;
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
        player.AddComponent<PlayerHotbar>();
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
        root.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
        SetColor(root, new Color(1f, 0.9f, 0.1f));
        Object.DestroyImmediate(root.GetComponent<CapsuleCollider>());

        root.AddComponent<NetworkObject>();
        root.AddComponent<NavMeshAgent>();

        var col = root.AddComponent<BoxCollider>();
        col.size   = new Vector3(1.2f, 2f, 1.2f);
        col.center = new Vector3(0f, 1f, 0f);

        var sc = root.AddComponent<SurvivorController>();
        var so = new SerializedObject(sc);
        var injuryProp = so.FindProperty("injuryLevel");
        if (injuryProp != null) { injuryProp.enumValueIndex = (int)SurvivorController.InjuryLevel.Light; so.ApplyModifiedPropertiesWithoutUndo(); }

        var beacon = new GameObject("Beacon");
        beacon.transform.SetParent(root.transform);
        beacon.transform.localPosition = new Vector3(0, 2f, 0);
        var bl = beacon.AddComponent<Light>();
        bl.type = LightType.Point;
        bl.range = 8f;
        bl.intensity = 2f;
        bl.color = new Color(1f, 0.9f, 0.2f);

        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Marker";
        marker.transform.SetParent(root.transform);
        marker.transform.localPosition = new Vector3(0, 2.8f, 0);
        marker.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        marker.transform.localRotation = Quaternion.Euler(45, 45, 0);
        SetColor(marker, new Color(1f, 0.95f, 0.2f));
        Object.DestroyImmediate(marker.GetComponent<BoxCollider>());

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
        root.transform.localScale = new Vector3(0.6f, 1f, 0.6f);
        SetColor(root, new Color(1f, 0.4f, 0.1f));
        Object.DestroyImmediate(root.GetComponent<CapsuleCollider>());

        root.AddComponent<NetworkObject>();
        root.AddComponent<NavMeshAgent>();

        var col = root.AddComponent<BoxCollider>();
        col.size   = new Vector3(1.2f, 2f, 1.2f);
        col.center = new Vector3(0f, 1f, 0f);

        var sc = root.AddComponent<SurvivorController>();
        var so = new SerializedObject(sc);
        var injuryProp = so.FindProperty("injuryLevel");
        if (injuryProp != null) { injuryProp.enumValueIndex = (int)SurvivorController.InjuryLevel.Heavy; so.ApplyModifiedPropertiesWithoutUndo(); }

        var beacon = new GameObject("Beacon");
        beacon.transform.SetParent(root.transform);
        beacon.transform.localPosition = new Vector3(0, 2f, 0);
        var bl = beacon.AddComponent<Light>();
        bl.type = LightType.Point;
        bl.range = 8f;
        bl.intensity = 2f;
        bl.color = new Color(1f, 0.5f, 0.1f);

        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "Marker";
        marker.transform.SetParent(root.transform);
        marker.transform.localPosition = new Vector3(0, 2.8f, 0);
        marker.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);
        marker.transform.localRotation = Quaternion.Euler(45, 45, 0);
        SetColor(marker, new Color(1f, 0.5f, 0.1f));
        Object.DestroyImmediate(marker.GetComponent<BoxCollider>());

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

        var root = new GameObject("Fuse");
        root.AddComponent<NetworkObject>();
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 0.5f;
        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(0.1f, 0.15f, 0.1f);
        col.center = new Vector3(0, 0.075f, 0);
        root.AddComponent<FuseItem>();

        var body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.075f, 0);
        body.transform.localScale = new Vector3(0.08f, 0.075f, 0.08f);
        SetColor(body, new Color(1f, 0.95f, 0.2f));
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());

        var cap1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap1.name = "Cap1";
        cap1.transform.SetParent(root.transform);
        cap1.transform.localPosition = new Vector3(0, 0.155f, 0);
        cap1.transform.localScale = new Vector3(0.04f, 0.02f, 0.04f);
        SetColor(cap1, new Color(0.75f, 0.75f, 0.78f));
        Object.DestroyImmediate(cap1.GetComponent<BoxCollider>());

        var cap2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cap2.name = "Cap2";
        cap2.transform.SetParent(root.transform);
        cap2.transform.localPosition = new Vector3(0, -0.005f, 0);
        cap2.transform.localScale = new Vector3(0.04f, 0.02f, 0.04f);
        SetColor(cap2, new Color(0.75f, 0.75f, 0.78f));
        Object.DestroyImmediate(cap2.GetComponent<BoxCollider>());

        var glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform);
        glow.transform.localPosition = new Vector3(0, 0.1f, 0);
        var gl = glow.AddComponent<Light>();
        gl.type = LightType.Point;
        gl.range = 2f;
        gl.intensity = 0.5f;
        gl.color = new Color(1f, 0.95f, 0.3f);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] Fuse prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── Toolbox Prefab ─────────────────────────────

    static GameObject CreateToolboxPrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/Toolbox.prefab";

        var root = new GameObject("Toolbox");
        root.AddComponent<NetworkObject>();
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 2f;
        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(0.35f, 0.2f, 0.2f);
        col.center = new Vector3(0, 0.1f, 0);
        root.AddComponent<ToolboxItem>();

        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Box";
        box.transform.SetParent(root.transform);
        box.transform.localPosition = new Vector3(0, 0.1f, 0);
        box.transform.localScale = new Vector3(0.35f, 0.2f, 0.2f);
        SetColor(box, new Color(0.9f, 0.15f, 0.1f));
        Object.DestroyImmediate(box.GetComponent<BoxCollider>());

        var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Handle";
        handle.transform.SetParent(root.transform);
        handle.transform.localPosition = new Vector3(0, 0.22f, 0);
        handle.transform.localScale = new Vector3(0.25f, 0.03f, 0.03f);
        SetColor(handle, new Color(0.3f, 0.3f, 0.32f));
        Object.DestroyImmediate(handle.GetComponent<BoxCollider>());

        var glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform);
        glow.transform.localPosition = new Vector3(0, 0.15f, 0);
        var gl = glow.AddComponent<Light>();
        gl.type = LightType.Point;
        gl.range = 2.5f;
        gl.intensity = 0.4f;
        gl.color = new Color(1f, 0.4f, 0.3f);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] Toolbox prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── Battery Prefab ─────────────────────────────

    static GameObject CreateBatteryPrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/TemporaryBattery.prefab";

        var root = new GameObject("TemporaryBattery");
        root.AddComponent<NetworkObject>();
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 5f;
        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(0.45f, 0.3f, 0.3f);
        col.center = new Vector3(0, 0.15f, 0);
        var battery = root.AddComponent<TemporaryBatteryItem>();

        var so = new SerializedObject(battery);
        var heavyProp = so.FindProperty("isHeavy");
        if (heavyProp != null) { heavyProp.boolValue = true; so.ApplyModifiedPropertiesWithoutUndo(); }

        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = new Vector3(0, 0.15f, 0);
        body.transform.localScale = new Vector3(0.45f, 0.3f, 0.3f);
        SetColor(body, new Color(0.15f, 0.35f, 0.7f));
        Object.DestroyImmediate(body.GetComponent<BoxCollider>());

        var term1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        term1.name = "Terminal1";
        term1.transform.SetParent(root.transform);
        term1.transform.localPosition = new Vector3(-0.1f, 0.32f, 0);
        term1.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);
        SetColor(term1, new Color(0.75f, 0.75f, 0.78f));
        Object.DestroyImmediate(term1.GetComponent<CapsuleCollider>());

        var term2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        term2.name = "Terminal2";
        term2.transform.SetParent(root.transform);
        term2.transform.localPosition = new Vector3(0.1f, 0.32f, 0);
        term2.transform.localScale = new Vector3(0.05f, 0.02f, 0.05f);
        SetColor(term2, new Color(0.75f, 0.75f, 0.78f));
        Object.DestroyImmediate(term2.GetComponent<CapsuleCollider>());

        var glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform);
        glow.transform.localPosition = new Vector3(0, 0.2f, 0);
        var gl = glow.AddComponent<Light>();
        gl.type = LightType.Point;
        gl.range = 3f;
        gl.intensity = 0.5f;
        gl.color = new Color(0.3f, 0.5f, 1f);

        var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"[Setup] TemporaryBattery prefab → {path}");
        return prefab;
    }

    // ─────────────────────────── EvidenceBox Prefab ──────────────────────────

    static GameObject CreateEvidenceBoxPrefab()
    {
        const string path = "Assets/_Project/Prefabs/Mission/EvidenceBox.prefab";

        var root = new GameObject("EvidenceBox");
        root.AddComponent<NetworkObject>();
        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 1.5f;
        var col = root.AddComponent<BoxCollider>();
        col.size = new Vector3(0.35f, 0.25f, 0.25f);
        col.center = new Vector3(0, 0.125f, 0);
        root.AddComponent<EvidenceBoxItem>();

        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = "Box";
        box.transform.SetParent(root.transform);
        box.transform.localPosition = new Vector3(0, 0.11f, 0);
        box.transform.localScale = new Vector3(0.35f, 0.22f, 0.25f);
        SetColor(box, new Color(0.55f, 0.35f, 0.15f));
        Object.DestroyImmediate(box.GetComponent<BoxCollider>());

        var lid = GameObject.CreatePrimitive(PrimitiveType.Cube);
        lid.name = "Lid";
        lid.transform.SetParent(root.transform);
        lid.transform.localPosition = new Vector3(0, 0.235f, 0);
        lid.transform.localScale = new Vector3(0.37f, 0.03f, 0.27f);
        SetColor(lid, new Color(0.65f, 0.45f, 0.2f));
        Object.DestroyImmediate(lid.GetComponent<BoxCollider>());

        var glow = new GameObject("Glow");
        glow.transform.SetParent(root.transform);
        glow.transform.localPosition = new Vector3(0, 0.15f, 0);
        var gl = glow.AddComponent<Light>();
        gl.type = LightType.Point;
        gl.range = 2f;
        gl.intensity = 0.4f;
        gl.color = new Color(1f, 0.9f, 0.7f);

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
        var defaultLight = GameObject.Find("Directional Light");
        if (defaultLight != null) Object.DestroyImmediate(defaultLight);

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
        // Desk — MVP_OfficeComputer is placed here by MvpProjectSetup (runs automatically after)
        CreateBoxColored("Desk", new Vector3(0, 0.4f, -1.8f), new Vector3(1.6f, 0.8f, 0.7f), deskColor);

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
        var dirLightGO = new GameObject("DirectionalLight");
        dirLightGO.transform.position = new Vector3(0, 3, 0);
        dirLightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        var dirLight = dirLightGO.AddComponent<Light>();
        dirLight.type = LightType.Directional;
        dirLight.intensity = 0.3f;
        dirLight.color = new Color(0.8f, 0.85f, 1f);

        var lightGO = new GameObject("OfficeLight");
        lightGO.transform.position = new Vector3(0, 2.8f, 0);
        var pointLight = lightGO.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.range = 8f;
        pointLight.intensity = 1.5f;
        pointLight.color = new Color(1f, 0.9f, 0.7f);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.15f, 0.13f, 0.12f);

        // ── NetworkManager ────────────────────────────────────
        var nmGO = new GameObject("NetworkManager");
        var nm = nmGO.AddComponent<NetworkManager>();
        var transport = nmGO.AddComponent<UnityTransport>();
        transport.SetConnectionData("127.0.0.1", 7778);
        nm.NetworkConfig.NetworkTransport = transport;
        nm.NetworkConfig.PlayerPrefab = playerPrefab;
        nm.NetworkConfig.ConnectionApproval = true;
        nmGO.AddComponent<QuickNetworkUI>();
        nmGO.AddComponent<AutoPort>();
        nmGO.AddComponent<MvpConnectionLimiter>();

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
        spawnPoint.transform.position = new Vector3(0, 1.1f, 0);
        spawnPoint.transform.rotation = Quaternion.Euler(0, 180, 0);

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
        var defaultLight = GameObject.Find("Directional Light");
        if (defaultLight != null) Object.DestroyImmediate(defaultLight);

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.04f, 0.04f, 0.06f);

        // ── Geometry ──────────────────────────────────────────────
        CreateBox("Ground",         Vector3.zero,                   new Vector3(30, 0.2f, 30));
        CreateBox("Wall_N",         new Vector3(0,   2.5f,  15),    new Vector3(30, 5, 0.5f));
        CreateBox("Wall_S",         new Vector3(0,   2.5f, -15),    new Vector3(30, 5, 0.5f));
        CreateBox("Wall_E",         new Vector3(15,  2.5f,   0),    new Vector3(0.5f, 5, 30));
        CreateBox("Wall_W",         new Vector3(-15, 2.5f,   0),    new Vector3(0.5f, 5, 30));
        CreateBox("Ceiling",        new Vector3(0,   5.1f,   0),    new Vector3(30, 0.2f, 30));

        // Center divider split into two segments with gap at z=-5 for Shortcut door
        CreateBox("Divider_Upper",  new Vector3(0, 2.5f, 3f),      new Vector3(0.5f, 5, 14));   // z=-4 to z=10
        CreateBox("Divider_Lower",  new Vector3(0, 2.5f, -8f),     new Vector3(0.5f, 5, 4));    // z=-10 to z=-6

        for (int i = -2; i <= 2; i++)
        {
            CreateBox($"Shop_L_{i}", new Vector3(-7, 1.5f, i * 4), new Vector3(4, 3, 3));
            CreateBox($"Shop_R_{i}", new Vector3( 7, 1.5f, i * 4), new Vector3(4, 3, 3));
        }

        // Pump room walls — split south wall for door gap at x=-6
        CreateBox("PumpRoom_Wall_W", new Vector3(-11.5f, 2.5f, -12), new Vector3(5, 5, 0.5f));  // x=-14 to x=-9
        CreateBox("PumpRoom_Wall_E", new Vector3(-4f, 2.5f, -12),    new Vector3(2, 5, 0.5f));  // x=-5 to x=-3
        CreateBox("PumpRoom_Wall_L", new Vector3(-14, 2.5f,  -9),    new Vector3(0.5f, 5, 6));

        // Power control room — split wall for door gap at x=-7
        CreateBox("PowerRoom_Wall_W", new Vector3(-11.5f, 2.5f, -6), new Vector3(5, 5, 0.5f));  // x=-14 to x=-9
        CreateBox("PowerRoom_Wall_E", new Vector3(-4.5f, 2.5f, -6),  new Vector3(3, 5, 0.5f));  // x=-6 to x=-3
        CreateBox("PowerRoom_Wall_R", new Vector3(-7, 2.5f, -9),     new Vector3(0.5f, 5, 6));

        // Corridor walls for WaterBlocked door at (-3, 0, -10)
        CreateBox("Corridor_W",     new Vector3(-5.5f, 2.5f, -10),   new Vector3(3, 5, 0.5f));
        CreateBox("Corridor_E",     new Vector3(-0.5f, 2.5f, -10),   new Vector3(3, 5, 0.5f));

        // Storage room walls for Locked door at (6, 0, -8)
        CreateBox("Storage_S",      new Vector3(6, 2.5f, -10),       new Vector3(8, 5, 0.5f));
        CreateBox("Storage_W",      new Vector3(2.25f, 2.5f, -9),    new Vector3(0.5f, 5, 2));
        CreateBox("Storage_N_W",    new Vector3(3.5f, 2.5f, -8),     new Vector3(2, 5, 0.5f));
        CreateBox("Storage_N_E",    new Vector3(8.5f, 2.5f, -8),     new Vector3(3, 5, 0.5f));

        // Parking pillars for atmosphere
        Color pillarColor = new Color(0.35f, 0.35f, 0.38f);
        Vector3[] pillarPositions = {
            new Vector3(-5, 2.5f, -4), new Vector3(-5, 2.5f, -9),
            new Vector3( 5, 2.5f, -4), new Vector3( 5, 2.5f, -9),
            new Vector3(-5, 2.5f,  4), new Vector3(-5, 2.5f,  9),
            new Vector3( 5, 2.5f,  4), new Vector3( 5, 2.5f,  9),
        };
        for (int i = 0; i < pillarPositions.Length; i++)
            CreateBoxColored($"Pillar_{i}", pillarPositions[i], new Vector3(0.5f, 5, 0.5f), pillarColor);

        // ── Water surface ─────────────────────────────────────────
        var waterPlane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        waterPlane.name = "WaterSurface";
        waterPlane.transform.localScale = new Vector3(3f, 1f, 3f);
        waterPlane.transform.position = new Vector3(0f, -0.1f, 0f);
        SetColor(waterPlane, new Color(0.1f, 0.3f, 0.7f, 0.6f));
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
            fuse.transform.position = new Vector3(-9f, 0.3f, -8f); // Power control room
        }

        if (toolboxPrefab != null)
        {
            var toolbox = (GameObject)PrefabUtility.InstantiatePrefab(toolboxPrefab);
            toolbox.transform.position = new Vector3(3f, 0.3f, -3f); // Shop area
        }

        if (batteryPrefab != null)
        {
            var battery = (GameObject)PrefabUtility.InstantiatePrefab(batteryPrefab);
            battery.transform.position = new Vector3(5f, 0.3f, -9f); // Storage room
        }

        if (evidencePrefab != null)
        {
            var evidence = (GameObject)PrefabUtility.InstantiatePrefab(evidencePrefab);
            evidence.transform.position = new Vector3(-8f, 0.3f, 3f); // Shop area
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

        // ── Door area lights ──────────────────────────────────────
        Vector3[] doorLightPositions = {
            new Vector3(-6f, 3f, -12f),  // PumpRoom
            new Vector3(-7f, 3f, -6f),   // PowerRoom
            new Vector3(0f, 3f, -5f),    // Shortcut
            new Vector3(-3f, 3f, -10f),  // WaterBlocked
            new Vector3(6f, 3f, -8f),    // Locked
        };
        for (int i = 0; i < doorLightPositions.Length; i++)
        {
            var dl = new GameObject($"DoorLight_{i}");
            dl.transform.position = doorLightPositions[i];
            var dLight = dl.AddComponent<Light>();
            dLight.type = LightType.Point;
            dLight.range = 4f;
            dLight.intensity = 0.3f;
            dLight.color = new Color(1f, 0.7f, 0.4f);
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
        nm.NetworkConfig.ConnectionApproval = true;
        nmGO.AddComponent<QuickNetworkUI>();
        nmGO.AddComponent<AutoPort>();
        nmGO.AddComponent<MvpConnectionLimiter>();

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
        sunLight.intensity = 0.15f;
        sunLight.color = new Color(0.4f, 0.5f, 0.7f);

        // Emergency ceiling lights
        for (int ix = -1; ix <= 1; ix++)
        for (int iz = -1; iz <= 1; iz++)
        {
            var emLight = new GameObject($"EmergencyLight_{ix}_{iz}");
            emLight.transform.position = new Vector3(ix * 8f, 4.8f, iz * 8f);
            var pl = emLight.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.range = 10f;
            pl.intensity = 0.6f;
            pl.color = new Color(1f, 0.6f, 0.3f);
        }

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
        SetColor(go, new Color(0.4f, 0.4f, 0.42f));
        GameObjectUtility.SetStaticEditorFlags(go,
            StaticEditorFlags.NavigationStatic | StaticEditorFlags.ContributeGI);
        return go;
    }

    static void SetColor(GameObject go, Color color)
    {
        var rend = go.GetComponent<Renderer>();
        if (rend == null) return;

        if (_baseMat == null || _baseMat.shader == null || _baseMat.shader.name == "Hidden/InternalErrorShader")
        {
            _baseMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Settings/URP-BaseLit.mat");
            if (_baseMat == null || _baseMat.shader == null || _baseMat.shader.name == "Hidden/InternalErrorShader")
                EnsureBaseMaterial();
        }

        Material mat;
        if (_baseMat != null && _baseMat.shader != null && _baseMat.shader.name != "Hidden/InternalErrorShader")
        {
            mat = new Material(_baseMat);
        }
        else
        {
            Debug.LogWarning($"[Setup] No valid URP shader for {go.name}, using URP Unlit fallback.");
            var fallbackShader = Shader.Find("Universal Render Pipeline/Unlit")
                ?? Shader.Find("Unlit/Color")
                ?? rend.sharedMaterial.shader;
            var fallbackMat = new Material(fallbackShader);
            if (fallbackMat.HasProperty("_BaseColor")) fallbackMat.SetColor("_BaseColor", color);
            else if (fallbackMat.HasProperty("_Color")) fallbackMat.color = color;
            rend.sharedMaterial = fallbackMat;
            return;
        }

        mat.SetColor("_BaseColor", color);
        if (color.a < 1f)
        {
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetFloat("_AlphaClip", 0);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
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
