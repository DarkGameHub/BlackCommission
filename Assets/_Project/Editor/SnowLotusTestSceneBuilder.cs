using System.IO;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SnowLotusTestSceneBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/Snow_Lotus_01.unity";
    const string TaskPath = "Assets/_Project/Resources/Tasks/SnowLotus_01.asset";
    const string ModelPath = "Assets/_Project/Art/Models/Icebound_Rift_100K/Icebound_Rift_100K.fbx";
    const string AlbedoPath = "Assets/_Project/Art/Models/Icebound_Rift_100K/Icebound_Rift_100K_albedo.png";
    const string NormalPath = "Assets/_Project/Art/Models/Icebound_Rift_100K/Icebound_Rift_100K_normal.png";
    const string MetallicPath = "Assets/_Project/Art/Models/Icebound_Rift_100K/Icebound_Rift_100K_metallic.png";
    const string EmissionPath = "Assets/_Project/Art/Models/Icebound_Rift_100K/Icebound_Rift_100K_emit.png";
    const string VanPrefabPath = "Assets/_Project/Prefabs/Art/AS_OfficeVan.prefab";
    const string MissionManagerPrefabPath = "Assets/_Project/Prefabs/Mission/LostItemMissionManager.prefab";
    const string BuildStampKey = "BlackCommission.SnowLotusTestSceneBuilder.Version";
    const int BuildVersion = 15;

    static readonly Vector3 EntryGroundCenter = new Vector3(-6f, -0.05f, -9f);
    static readonly Vector3 EntryGroundSize = new Vector3(8f, 0.1f, 10f);
    static readonly Vector3 EntryTop = new Vector3(0f, 0f, 0f);
    static bool retryBuildAfterPlayMode;

    static SnowLotusTestSceneBuilder()
    {
        EditorApplication.delayCall += EnsureBuilt;
    }

    [MenuItem("Tools/Black Commission/MVP/Build Snow Lotus Test Scene")]
    public static void ForceBuild()
    {
        EditorPrefs.DeleteKey(BuildStampKey);
        EnsureBuilt();
    }

    static void EnsureBuilt()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (!retryBuildAfterPlayMode)
            {
                retryBuildAfterPlayMode = true;
                EditorApplication.playModeStateChanged += RetryBuildWhenEditModeReturns;
            }
            return;
        }
        if (EditorPrefs.GetInt(BuildStampKey, 0) >= BuildVersion && File.Exists(ScenePath)) return;
        if (!File.Exists(ModelPath))
        {
            Debug.LogWarning($"[Snow Lotus] 100K mountain model is missing: {ModelPath}");
            return;
        }
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);

        EnsureFolders();
        OfficeTaskDefinition task = EnsureTask();
        BuildScene();
        UpdateBuildSettings();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorPrefs.SetInt(BuildStampKey, BuildVersion);
        Debug.Log($"[Snow Lotus] Test scene ready: {ScenePath}. HQ default task: {task.title}");
    }

    static void RetryBuildWhenEditModeReturns(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.EnteredEditMode) return;
        retryBuildAfterPlayMode = false;
        EditorApplication.playModeStateChanged -= RetryBuildWhenEditModeReturns;
        EditorApplication.delayCall += EnsureBuilt;
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

    static OfficeTaskDefinition EnsureTask()
    {
        var task = AssetDatabase.LoadAssetAtPath<OfficeTaskDefinition>(TaskPath);
        if (task == null)
        {
            task = ScriptableObject.CreateInstance<OfficeTaskDefinition>();
            AssetDatabase.CreateAsset(task, TaskPath);
        }

        task.taskId = "snow_lotus_01";
        task.title = "白棘雪莲";
        task.category = MvpTaskCategory.LostItemRecovery;
        task.client = "旧疗养站代理人";
        task.description = "前往封山雪线，在废弃检查站后的冰裂谷采回一株白棘雪莲。目标带回委托车才算完成。";
        task.locationName = "封山雪线";
        task.sceneName = "Snow_Lotus_01";
        task.recommendedPlayersMin = 1;
        task.recommendedPlayersMax = 4;
        task.requiredOfficeLevel = 1;
        task.minimumReputation = -100;
        task.missionStartClockHour = 17.5f; // Start just before peak sunset to see transition
        task.contractWindowGameHours = 10f;
        task.realSecondsPerGameHour = 60f;
        task.overtimeMoneyPenaltyPerGameHour = 30;
        task.overtimeReputationPenaltyBlockGameHours = 2f;
        task.overtimeReputationPenaltyPerBlock = 1;
        task.moneyReward = 420;
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
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "Snow_Lotus_01";

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.1f, 0.08f, 0.15f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.12f, 0.1f, 0.18f);
        RenderSettings.fogDensity = 0.012f;

        var root = new GameObject("MVP_Snow_Lotus_01");

        CreateCamera();
        var sun = CreateLight();
        CreatePostProcessing(root.transform);
        CreateWalkableSnow(root.transform);
        PlaceMountain(root.transform);
        PlaceVan(root.transform);
        CreateSpawn(root.transform);
        CreateMissionObjects(root.transform);
        CreateWeatherEffects(root.transform);

        // Add Time Cycle Director to handle the dynamic sunset
        var directorGo = new GameObject("MissionTimeOfDayDirector");
        directorGo.transform.SetParent(root.transform);
        var director = directorGo.AddComponent<MissionTimeOfDayDirector>();
        var so = new SerializedObject(director);
        so.FindProperty("directionalLight").objectReferenceValue = sun.GetComponent<Light>();
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
    }

    static void CreatePostProcessing(Transform parent)
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        string folder = "Assets/_Project/Settings/Profiles";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            EnsureFolder("Assets/_Project", "Settings");
            EnsureFolder("Assets/_Project/Settings", "Profiles");
        }
        string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folder}/SnowLotus_EveningProfile.asset");
        AssetDatabase.CreateAsset(profile, assetPath);

        var tonemapping = profile.Add<Tonemapping>();
        tonemapping.mode.overrideState = true;
        tonemapping.mode.value = TonemappingMode.ACES;

        var bloom = profile.Add<Bloom>();
        bloom.intensity.overrideState = true;
        bloom.intensity.value = 1.5f;
        bloom.threshold.overrideState = true;
        bloom.threshold.value = 0.8f;

        var colorAdjustments = profile.Add<ColorAdjustments>();
        colorAdjustments.contrast.overrideState = true;
        colorAdjustments.contrast.value = 12f;
        colorAdjustments.saturation.overrideState = true;
        colorAdjustments.saturation.value = 20f;

        var volumeObj = new GameObject("PostProcessVolume");
        volumeObj.transform.SetParent(parent);
        var volume = volumeObj.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }

    static void CreateWeatherEffects(Transform parent)
    {
        var particleMat = MakeParticleMaterial();

        // Global Snow
        var globalSnow = new GameObject("GlobalSnow");
        globalSnow.transform.SetParent(parent);
        globalSnow.transform.position = new Vector3(0, 40, 150);
        
        var ps = globalSnow.AddComponent<ParticleSystem>();
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMat;

        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 10f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.2f); // Reverted to smaller size
        main.gravityModifier = 0.1f;
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = 15f;
        
        var emission = ps.emission;
        emission.rateOverTime = 400f;
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(300, 1, 350);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.5f;

        // Localized Mountain Snow 1 (High Density, Small Size)
        CreateLocalizedSnow(parent, "MountainSnow_HighDensity", new Vector3(80, 45, 250), 400f, new Vector2(0.05f, 0.2f), particleMat);
        
        // Localized Mountain Snow 2 (Low Density, Large Size)
        CreateLocalizedSnow(parent, "MountainSnow_LowDensity", new Vector3(-60, 35, 120), 50f, new Vector2(0.1f, 0.4f), particleMat);

        // Blizzard Walls at map edges
        CreateBlizzardWall(parent, "BlizzardWall_Left", new Vector3(-140, 50, 150), new Vector3(20, 100, 400), particleMat);
        CreateBlizzardWall(parent, "BlizzardWall_Right", new Vector3(140, 50, 150), new Vector3(20, 100, 400), particleMat);
        CreateBlizzardWall(parent, "BlizzardWall_Front", new Vector3(0, 50, -15), new Vector3(400, 100, 20), particleMat);
        CreateBlizzardWall(parent, "BlizzardWall_Back", new Vector3(0, 50, 320), new Vector3(400, 100, 20), particleMat);
    }

    static void CreateLocalizedSnow(Transform parent, string name, Vector3 pos, float rate, Vector2 sizeRange, Material mat)
    {
        var snow = new GameObject(name);
        snow.transform.SetParent(parent);
        snow.transform.position = pos;
        
        var ps = snow.AddComponent<ParticleSystem>();
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;

        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.gravityModifier = 0.05f;
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 10f;
        
        var emission = ps.emission;
        emission.rateOverTime = rate;
        
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 40f;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.3f;
    }

    static void CreateBlizzardWall(Transform parent, string name, Vector3 pos, Vector3 scale, Material mat)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = scale;

        var col = wall.AddComponent<BoxCollider>();
        col.size = Vector3.one;

        var ps = wall.AddComponent<ParticleSystem>();
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = mat;

        var main = ps.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(25f, 40f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f); // Reverted to original small size
        main.gravityModifier = 0.05f;
        main.startColor = new Color(1f, 1f, 1f, 0.7f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startLifetime = 10f;

        var emission = ps.emission;
        emission.rateOverTime = 1200f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = Vector3.one;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 1.2f;
        noise.frequency = 0.8f;
    }

    static Material MakeParticleMaterial()
    {
        string folder = "Assets/_Project/Settings/Materials";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            EnsureFolder("Assets/_Project", "Settings");
            EnsureFolder("Assets/_Project/Settings", "Materials");
        }

        string path = $"{folder}/SnowParticle.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        else if (mat.HasProperty("_Color")) mat.SetColor("_Color", Color.white);
        
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1); // Transparent
        if (mat.HasProperty("_Blend")) mat.SetFloat("_Blend", 0); // Alpha
        
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void CreateCamera()
    {
        var go = new GameObject("Preview Camera");
        go.tag = "MainCamera";
        go.transform.SetPositionAndRotation(new Vector3(0f, 8f, -16f), Quaternion.Euler(24f, 0f, 0f));
        var cam = go.AddComponent<Camera>();
        cam.fieldOfView = 60f;
        cam.farClipPlane = 700f;
        go.AddComponent<AudioListener>();

        var camData = go.AddComponent<UniversalAdditionalCameraData>();
        camData.renderPostProcessing = true;
    }

    static GameObject CreateLight()
    {
        var go = new GameObject("Golden Mountain Sun");
        go.transform.rotation = Quaternion.Euler(5f, -45f, 0f);
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.65f, 0.35f);
        light.intensity = 1.6f;
        return go;
    }

    static void CreateWalkableSnow(Transform parent)
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ground.name = "Small_Snow_Entry_Pad";
        ground.transform.SetParent(parent);
        ground.transform.position = EntryGroundCenter;
        ground.transform.localScale = EntryGroundSize;
        ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial(
            "MVP_snow_entry_pad",
            new Color(0.72f, 0.76f, 0.75f));
    }

    static void PlaceMountain(Transform parent)
    {
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        if (model == null)
        {
            AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceSynchronousImport);
            model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        }
        if (model == null) return;

        var mountain = (GameObject)PrefabUtility.InstantiatePrefab(model);
        mountain.name = "Icebound_Rift_100K_Visual";
        mountain.transform.SetParent(parent);
        mountain.transform.localPosition = new Vector3(0f, -2.86f, 118.4f);
        mountain.transform.localEulerAngles = new Vector3(-90f, -180f, 0f);
        mountain.transform.localScale = Vector3.one * 41.32f;
        var transformSo = new SerializedObject(mountain.transform);
        transformSo.FindProperty("m_LocalEulerAnglesHint").vector3Value = new Vector3(-90f, -180f, 0f);
        transformSo.ApplyModifiedPropertiesWithoutUndo();

        mountain.AddComponent<MeshCollider>();

        Material mountainMaterial = MakeMountainMaterial();
        foreach (var renderer in mountain.GetComponentsInChildren<Renderer>(true))
            renderer.sharedMaterial = mountainMaterial;
    }

    static void PlaceVan(Transform parent)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(VanPrefabPath);
        if (prefab == null) return;

        var van = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        van.name = "Snowfield_ReturnVan";
        van.transform.SetParent(parent);
        van.transform.SetPositionAndRotation(new Vector3(-6f, 0f, -9f), Quaternion.Euler(0f, 70f, 0f));
        van.transform.localScale = Vector3.one * 0.7865f;
    }

    static void CreateSpawn(Transform parent)
    {
        var spawn = new GameObject("PlayerSpawnPoint");
        spawn.transform.SetParent(parent);
        spawn.transform.SetPositionAndRotation(new Vector3(0f, 0.5f, -4f), Quaternion.identity);

        var managerGo = new GameObject("SnowSpawnManager");
        managerGo.transform.SetParent(parent);
        var manager = managerGo.AddComponent<HQSpawnManager>();
        var so = new SerializedObject(manager);
        so.FindProperty("spawnPoint").objectReferenceValue = spawn.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void CreateMissionObjects(Transform parent)
    {
        var missionManagerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MissionManagerPrefabPath);
        if (missionManagerPrefab != null)
        {
            var manager = (GameObject)PrefabUtility.InstantiatePrefab(missionManagerPrefab);
            manager.transform.SetParent(parent);
            manager.transform.position = Vector3.zero;
        }

        var flower = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        flower.name = "SnowLotus_Target";
        flower.transform.SetParent(parent);
        flower.transform.position = new Vector3(100f, 40f, 300f);
        flower.transform.localScale = new Vector3(0.45f, 0.75f, 0.45f);
        flower.GetComponent<Renderer>().sharedMaterial = MakeMaterial(
            "MVP_snow_lotus",
            new Color(0.86f, 0.96f, 1f));
        flower.AddComponent<NetworkObject>();
        var item = flower.AddComponent<LostHomeworkItem>();
        var itemSo = new SerializedObject(item);
        itemSo.FindProperty("itemName").stringValue = "白棘雪莲";
        itemSo.ApplyModifiedPropertiesWithoutUndo();

        var glowGo = new GameObject("SnowLotus_Glow");
        glowGo.transform.SetParent(flower.transform);
        glowGo.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        var glow = glowGo.AddComponent<Light>();
        glow.type = LightType.Point;
        glow.range = 5f;
        glow.intensity = 1.15f;
        glow.color = new Color(0.60f, 0.90f, 1f);

        var exit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        exit.name = "SnowReturnPoint";
        exit.transform.SetParent(parent);
        exit.transform.position = new Vector3(-6f, 0.3f, -9f);
        exit.transform.localScale = new Vector3(4.2f, 0.6f, 2.6f);
        exit.GetComponent<Renderer>().enabled = false;
        var box = exit.GetComponent<BoxCollider>();
        box.isTrigger = true;
        exit.AddComponent<NetworkObject>();
        exit.AddComponent<SchoolExitPoint>();
    }

    static Material MakeMaterial(string name, Color color)
    {
        string folder = "Assets/_Project/Settings/Materials";
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

    static Material MakeMountainMaterial()
    {
        EnsureTextureImported(AlbedoPath, TextureImporterType.Default);
        EnsureTextureImported(NormalPath, TextureImporterType.NormalMap);
        EnsureTextureImported(MetallicPath, TextureImporterType.Default);
        EnsureTextureImported(EmissionPath, TextureImporterType.Default);

        string folder = "Assets/_Project/Settings/Materials";
        if (!AssetDatabase.IsValidFolder(folder))
        {
            EnsureFolder("Assets/_Project", "Settings");
            EnsureFolder("Assets/_Project/Settings", "Materials");
        }

        string path = $"{folder}/MVP_icebound_rift_100k.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        var albedo = AssetDatabase.LoadAssetAtPath<Texture2D>(AlbedoPath);
        var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        var metallic = AssetDatabase.LoadAssetAtPath<Texture2D>(MetallicPath);
        var emission = AssetDatabase.LoadAssetAtPath<Texture2D>(EmissionPath);

        if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", albedo);
        if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", albedo);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", Color.white);
        else mat.color = Color.white;

        if (normal != null)
        {
            if (mat.HasProperty("_BumpMap")) mat.SetTexture("_BumpMap", normal);
            if (mat.HasProperty("_BumpScale")) mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }

        if (metallic != null)
        {
            if (mat.HasProperty("_MetallicGlossMap")) mat.SetTexture("_MetallicGlossMap", metallic);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", 0f);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.18f);
            mat.EnableKeyword("_METALLICSPECGLOSSMAP");
        }

        if (emission != null)
        {
            if (mat.HasProperty("_EmissionMap")) mat.SetTexture("_EmissionMap", emission);
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", new Color(0.08f, 0.10f, 0.12f));
            mat.EnableKeyword("_EMISSION");
        }

        EditorUtility.SetDirty(mat);
        return mat;
    }

    static void EnsureTextureImported(string path, TextureImporterType textureType)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            if (File.Exists(path))
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
        }

        if (importer.textureType == textureType) return;
        importer.textureType = textureType;
        importer.SaveAndReimport();
    }

    static void UpdateBuildSettings()
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        EnsureBuildScene(scenes, "Assets/_Project/Scenes/HQ.unity");
        EnsureBuildScene(scenes, ScenePath);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    static void EnsureBuildScene(System.Collections.Generic.List<EditorBuildSettingsScene> scenes, string path)
    {
        foreach (var scene in scenes)
            if (scene.path == path)
                return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
    }
}
