using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Creates the ready-to-Play map-2 scene (ADR-0003, direction B): a light + an overview camera + a
/// <c>MapSite</c> object carrying <c>MapSiteRuntime</c>, which on Play generates a FRESH RANDOM site
/// (outdoor approach + van drop-off + edge-based interior) from a new seed each run. Kept as a runtime scene
/// (not a baked one) because the whitebox geometry uses runtime-created materials that wouldn't persist if
/// saved into the scene — and because per-run randomness is the point.
///
/// Headless: <c>Unity.exe -batchmode -nographics -projectPath . -executeMethod Map2SceneBuilder.BuildAndSave
/// -quit -logFile &lt;log&gt;</c>. Also available interactively: Tools ▸ Black Commission ▸ Map ▸ Create Map2 Scene.
/// </summary>
public static class Map2SceneBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/Map2_Procedural.unity";

    [MenuItem("Tools/Black Commission/Map/Create Map2 Scene")]
    public static void BuildAndSave()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var lightGo = new GameObject("Directional Light");
        var light = lightGo.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 0.32f;                       // moonlit-dark (LC-style night)
        light.color = new Color(0.62f, 0.68f, 0.82f);  // cold moonlight, less saturated blue (noir, not movie-blue)
        light.shadows = LightShadows.Soft;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Dark + foggy so the building is NOT visible from the far drop-off — you navigate the woods to find it.
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.06f, 0.065f, 0.075f); // dim, near-neutral cold (desaturated)
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.045f, 0.05f, 0.06f);      // dark concrete-gray haze, still cold
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.018f;                            // keeps the building hidden until found

        // Reuse the project's owned Tirgames industrial NIGHT skybox (no Asset Store import). Set editor-side so
        // it serialises into the scene's RenderSettings — the runtime MapSiteRuntime build never touches assets.
        var sky = AssetDatabase.LoadAssetAtPath<Material>(
            "Assets/TirgamesAssets/Factory/Environment/SkyBoxes/Materials/SkyBoxIndustrial01Night.mat");
        if (sky != null) RenderSettings.skybox = sky;
        else Debug.LogWarning("[Map2Scene] night skybox material not found — leaving default skybox.");

        var camGo = new GameObject("Overview Camera");
        var cam = camGo.AddComponent<Camera>();
        camGo.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.Skybox;
        camGo.transform.position = new Vector3(28f, 46f, -46f); // looks north+down over outdoor→building
        camGo.transform.rotation = Quaternion.Euler(45f, 0f, 0f);

        var site = new GameObject("MapSite");
        site.AddComponent<NetworkObject>();  // MapSiteRuntime is a NetworkBehaviour now (server-synced seed)
        site.AddComponent<MapSiteRuntime>(); // Assembly-CSharp-Editor references Assembly-CSharp, so this resolves

        BuildScavengeRig();

        bool ok = EditorSceneManager.SaveScene(scene, ScenePath);
        EnsureInBuildSettings(ScenePath);
        Debug.Log($"[Map2Scene] {(ok ? "saved" : "FAILED to save")} {ScenePath} " +
                  $"(light + overview camera + MapSite[MapSiteRuntime] + ScavengeRig). Hit Play → fresh random site each run.");
    }

    // Scavenge rig baked at the FIXED van drop-off. The ENTRY anchor is constant (grid 2,2) so the front
    // door resolves to world (10,0,0) and the drop-off to (10,0,-80) on every seed — only the interior maze
    // + loot positions vary. These are SCENE NetworkObjects so NGO auto-spawns them when the host loads the
    // map; the runtime MapSiteRuntime build adds the (non-networked) loot anchors + geometry around them.
    // Solo-host runs the whole loop; multiplayer determinism (synced seed) = ADR-0003 follow-up.
    static void BuildScavengeRig()
    {
        const float dropX = 10f, dropZ = -80f;
        var rig = new GameObject("ScavengeRig");

        var spawnerGo = new GameObject("LootSpawner");
        spawnerGo.transform.SetParent(rig.transform);
        spawnerGo.AddComponent<NetworkObject>();
        spawnerGo.AddComponent<LootSpawner>();

        var zoneGo = new GameObject("VAN_CargoZone");
        zoneGo.transform.SetParent(rig.transform);
        zoneGo.transform.position = new Vector3(dropX, 1.25f, dropZ);
        var zoneBox = zoneGo.AddComponent<BoxCollider>();
        zoneBox.isTrigger = true;
        zoneBox.size = new Vector3(8f, 2.5f, 4f);
        zoneGo.AddComponent<NetworkObject>();
        var cargoZone = zoneGo.AddComponent<ScavengeCargoZone>();

        var managerGo = new GameObject("ScavengeMissionManager");
        managerGo.transform.SetParent(rig.transform);
        managerGo.AddComponent<NetworkObject>();
        var manager = managerGo.AddComponent<ScavengeMissionManager>();
        var so = new SerializedObject(manager);
        so.FindProperty("cargoZone").objectReferenceValue = cargoZone;
        so.ApplyModifiedPropertiesWithoutUndo();

        var leverGo = new GameObject("VAN_DepartLever");
        leverGo.transform.SetParent(rig.transform);
        leverGo.transform.position = new Vector3(dropX + 2f, 0.7f, dropZ + 1f);
        var leverBox = leverGo.AddComponent<BoxCollider>();
        leverBox.size = new Vector3(0.5f, 1.4f, 0.5f); // solid, aimable post
        var lever = leverGo.AddComponent<ScavengeVanDepartTrigger>();
        var leverSo = new SerializedObject(lever);
        leverSo.FindProperty("manager").objectReferenceValue = manager;
        leverSo.ApplyModifiedPropertiesWithoutUndo();

        var exitGo = new GameObject("VAN_ExitPoint");
        exitGo.transform.SetParent(rig.transform);
        exitGo.transform.position = new Vector3(dropX - 2.5f, 1.0f, dropZ + 1f);
        var exitBox = exitGo.AddComponent<BoxCollider>();
        exitBox.isTrigger = true;
        exitBox.size = new Vector3(2.5f, 2f, 2.5f);
        exitGo.AddComponent<NetworkObject>();
        exitGo.AddComponent<MissionVanExitPoint>();

        var spawn = new GameObject("PlayerSpawnPoint");
        spawn.transform.SetParent(rig.transform);
        spawn.transform.position = new Vector3(dropX, 0.1f, dropZ + 2f); // just off the drop-off, facing the building (+z)
    }

    static void EnsureInBuildSettings(string path)
    {
        var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        for (int i = 0; i < scenes.Count; i++)
            if (scenes[i].path == path)
            {
                if (!scenes[i].enabled)
                {
                    scenes[i] = new EditorBuildSettingsScene(path, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
                return;
            }
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[Map2Scene] Added {path} to Build Settings.");
    }
}
