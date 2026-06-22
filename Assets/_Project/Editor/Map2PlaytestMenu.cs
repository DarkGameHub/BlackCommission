using UnityEngine;
using UnityEditor;
using BlackCommission.Level;

/// <summary>
/// One-click FIRST-PERSON playtest of Map 2. Builds the full procedural site (outdoor approach +
/// buried interior + haul-up route) into the OPEN scene, adds a sun + ambient so it is not pitch
/// black, and drops a standalone walk-rig (<see cref="Map2PlaytestController"/>) at the van drop-off.
/// The PM then presses Play (▶) and walks the whole loop to find geometry problems.
///
/// Builds into the open scene and NEVER saves (matches "Build Full Map 2"). This is a SOLO, non-network
/// geometry check — the real networked player needs an NGO host and is out of scope for a quick walk.
/// A fixed seed keeps a reported problem reproducible (change <see cref="Seed"/> to roll a new layout).
///
/// Menu: Tools ▸ Black Commission ▸ Map ▸ Playtest Map 2 (Walk It)
/// </summary>
public static class Map2PlaytestMenu
{
    const string RootName = "PLAYTEST_MAP2";
    const int Seed = 1;

    [MenuItem("Tools/Black Commission/Map/Playtest Map 2 (Walk It)")]
    public static void Setup()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject(RootName);
        MapSiteBuilder.Result res = MapSiteBuilder.Build(root.transform, Seed);

        EnsureLighting(root.transform);
        SpawnPlayer(root.transform, res.Dropoff);

        Debug.Log($"[PlaytestMap2] READY (seed {Seed}). Press Play (▶), then WASD + mouse to walk. " +
                  $"You spawn at the van drop-off {res.Dropoff}. Front door {res.FrontDoor}, " +
                  $"fire-exit (haul-up) {res.FireExit}. Controls: Shift sprint · Ctrl crouch · Space jump · " +
                  $"F flythrough · Esc release cursor.");

        Selection.activeGameObject = GameObject.Find(RootName);
        SceneView.FrameLastActiveSceneView();
    }

    static void EnsureLighting(Transform parent)
    {
        var sun = new GameObject("Playtest Sun");
        sun.transform.SetParent(parent, false);
        sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        var l = sun.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.1f;
        l.shadows = LightShadows.Soft;

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.30f, 0.32f, 0.36f);
    }

    static void SpawnPlayer(Transform parent, Vector3 dropoff)
    {
        // Only the playtest camera should render / hear — disable any other cameras + listeners.
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None)) c.gameObject.SetActive(false);
        foreach (var a in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None)) a.enabled = false;

        var player = new GameObject("PLAYTEST_PLAYER");
        player.transform.SetParent(parent, false);
        player.transform.position = dropoff + Vector3.up * 3f; // drop onto the ground from just above

        var cc = player.AddComponent<CharacterController>();
        cc.height = 2f;
        cc.radius = 0.4f;
        cc.stepOffset = 0.5f;
        cc.slopeLimit = 75f;
        cc.center = new Vector3(0f, 1f, 0f);

        var camGo = new GameObject("PlaytestCam");
        camGo.transform.SetParent(player.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 1.7f, 0f); // eye height
        var cam = camGo.AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.nearClipPlane = 0.05f;
        camGo.AddComponent<AudioListener>();

        player.AddComponent<Map2PlaytestController>();
    }
}
