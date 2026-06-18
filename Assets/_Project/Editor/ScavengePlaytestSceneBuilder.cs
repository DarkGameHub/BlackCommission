using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Unity.Netcode;

/// <summary>
/// Builds a SELF-CONTAINED, hostable scavenging play-test scene so the full P1b loop
/// (spawn → carry → deposit → van weight gate) can be exercised without the HQ → dispatch flow.
/// Unlike a normal mission scene (which borrows HQ's persistent NetworkManager), this dev scene
/// carries its own NetworkManager — copied verbatim from HQ.unity so the NetworkConfig, player
/// prefab and DefaultNetworkPrefabs list match exactly. Open the scene, press Play, click Host
/// (QuickNetworkUI), and the player spawns in a dressed room; loot spawns at its anchors; carry an
/// item into the teal van bay and it loads if the weight fits, else stays behind.
///
/// Menu: Tools > Black Commission > MVP > Scavenge > Build Scavenging Play-Test Scene
/// Writes Assets/_Project/Scenes/Scavenge_Testbed.unity and registers it in Build Settings.
/// Re-run to regenerate from scratch. Dev/test artifact — not part of the shipping mission set.
/// </summary>
public static class ScavengePlaytestSceneBuilder
{
    const string ScenePath = "Assets/_Project/Scenes/Scavenge_Testbed.unity";
    const string HQPath    = "Assets/_Project/Scenes/HQ.unity";
    const string RoomPrefabPath = "Assets/_Project/Art/Maps/Shared/Modules/Room_Office_4x8.prefab";

    [MenuItem("Tools/Black Commission/MVP/Scavenge/Build Scavenging Playtest Scene")]
    public static void Build()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("[ScavengePlaytest] Exit Play mode before building the scene.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        AddDirectionalLight();
        AddSafetyGround();
        bool roomOk = AddDressedRoom(scene, out int anchorCount);
        bool nmOk = CopyNetworkManagerFromHQ(scene);
        AddPlayerSpawn();
        AddLootSpawner();
        var cargoZone = AddCargoZone();
        AddMission(cargoZone);

        if (!EnsureSceneFolder())
        {
            Debug.LogError("[ScavengePlaytest] Could not ensure Assets/_Project/Scenes folder.");
            return;
        }
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuildSettings(ScenePath);

        Debug.Log($"[ScavengePlaytest] Built {ScenePath} " +
                  $"(room: {(roomOk ? "ok" : "MISSING prefab")}, {anchorCount} loot anchor(s); " +
                  $"NetworkManager: {(nmOk ? "copied from HQ" : "MISSING — host won't start")}). " +
                  "Open it, press Play, click Host: loot spawns → carry into the teal bay (weight-gated) " +
                  "→ press E on the DepartLever post to settle (pays the cargo total → return → HQ).");
    }

    static void AddDirectionalLight()
    {
        var go = new GameObject("Directional Light");
        var light = go.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.0f;
        light.color = new Color(1f, 0.96f, 0.9f);
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    // A generous floor so nothing falls through gaps around/beside the room module.
    static void AddSafetyGround()
    {
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.position = new Vector3(2f, 0f, 0f);
        ground.transform.localScale = new Vector3(4f, 1f, 4f); // 40 x 40 m
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        var mat = new Material(shader) { color = new Color(0.22f, 0.22f, 0.21f) };
        ground.GetComponent<Renderer>().sharedMaterial = mat;
    }

    // The dressed module already carries LootAnchors in its Dressing group (RoomDresser output).
    static bool AddDressedRoom(Scene scene, out int anchorCount)
    {
        anchorCount = 0;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(RoomPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[ScavengePlaytest] Room module not found at {RoomPrefabPath}; " +
                             "scene will have no loot anchors — place a dressed module or add LootAnchors manually.");
            return false;
        }
        var room = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        room.name = "ScavengeRoom";
        room.transform.position = Vector3.zero;
        anchorCount = room.GetComponentsInChildren<BlackCommission.Level.LootAnchor>(true).Length;
        return true;
    }

    // Borrow HQ's NetworkManager wholesale (NetworkConfig, player prefab, prefab list, transport,
    // AutoPort, MvpConnectionLimiter, QuickNetworkUI) so the dev scene hosts identically to HQ.
    static bool CopyNetworkManagerFromHQ(Scene target)
    {
        var hq = AssetDatabase.LoadAssetAtPath<SceneAsset>(HQPath);
        if (hq == null)
        {
            Debug.LogWarning($"[ScavengePlaytest] HQ scene not found at {HQPath}; cannot copy NetworkManager.");
            return false;
        }

        Scene hqScene = EditorSceneManager.OpenScene(HQPath, OpenSceneMode.Additive);
        GameObject source = null;
        foreach (var go in hqScene.GetRootGameObjects())
            if (go.GetComponent<NetworkManager>() != null) { source = go; break; }

        bool ok = false;
        if (source != null)
        {
            var copy = Object.Instantiate(source);
            copy.name = "NetworkManager";
            SceneManager.MoveGameObjectToScene(copy, target);
            ok = true;
        }
        else
        {
            Debug.LogWarning("[ScavengePlaytest] No NetworkManager found among HQ root objects.");
        }

        EditorSceneManager.CloseScene(hqScene, true);
        return ok;
    }

    static void AddPlayerSpawn()
    {
        var spawn = new GameObject("PlayerSpawnPoint");
        spawn.transform.position = new Vector3(0f, 0.2f, -2.5f);
        var sm = spawn.AddComponent<HQSpawnManager>();
        var so = new SerializedObject(sm);
        var prop = so.FindProperty("spawnPoint");
        if (prop != null)
        {
            prop.objectReferenceValue = spawn.transform;
            so.ApplyModifiedProperties();
        }
    }

    static void AddLootSpawner()
    {
        var go = new GameObject("LootSpawner");
        go.transform.position = Vector3.zero;
        go.AddComponent<NetworkObject>();
        go.AddComponent<LootSpawner>();
    }

    // The van bay: a trigger volume (gate) + a translucent teal marker so it's visible. Placed a
    // few metres from the room so loot must be carried to it.
    static ScavengeCargoZone AddCargoZone()
    {
        var zone = new GameObject("VanCargoZone");
        zone.transform.position = new Vector3(6f, 0f, 0f);

        var box = zone.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, 1f, 0f);
        box.size = new Vector3(3f, 2f, 3f);

        zone.AddComponent<NetworkObject>();
        var cargo = zone.AddComponent<ScavengeCargoZone>();

        var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
        marker.name = "BayMarker";
        marker.transform.SetParent(zone.transform, false);
        marker.transform.localPosition = box.center;
        marker.transform.localScale = box.size;
        var col = marker.GetComponent<Collider>();
        if (col != null) Object.DestroyImmediate(col);

        var shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
        var mat = new Material(shader);
        if (mat.HasProperty("_Surface")) mat.SetFloat("_Surface", 1f);
        mat.renderQueue = 3000;
        var teal = new Color(0.247f, 0.373f, 0.361f, 0.18f);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", teal);
        mat.color = teal;
        marker.GetComponent<Renderer>().sharedMaterial = mat;

        return cargo;
    }

    // The mission manager (settles the cargo on departure) + a depart lever post beside the bay.
    // Pressing E on the post settles the run: pays the cargo total → return transit → HQ.
    static void AddMission(ScavengeCargoZone cargoZone)
    {
        var mgrGo = new GameObject("ScavengeMissionManager");
        mgrGo.AddComponent<NetworkObject>();
        var mgr = mgrGo.AddComponent<ScavengeMissionManager>();
        if (cargoZone != null)
        {
            var so = new SerializedObject(mgr);
            var prop = so.FindProperty("cargoZone");
            if (prop != null) { prop.objectReferenceValue = cargoZone; so.ApplyModifiedProperties(); }
        }

        // Lever post beside the van bay — solid collider so the interaction raycast hits it.
        var post = GameObject.CreatePrimitive(PrimitiveType.Cube);
        post.name = "DepartLever";
        post.transform.position = new Vector3(4.2f, 0.6f, 0f);
        post.transform.localScale = new Vector3(0.28f, 1.2f, 0.28f);
        var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        post.GetComponent<Renderer>().sharedMaterial = new Material(shader) { color = new Color(0.247f, 0.373f, 0.361f) };
        var trigger = post.AddComponent<ScavengeVanDepartTrigger>();
        var tso = new SerializedObject(trigger);
        var tprop = tso.FindProperty("manager");
        if (tprop != null) { tprop.objectReferenceValue = mgr; tso.ApplyModifiedProperties(); }
    }

    static bool EnsureSceneFolder()
    {
        if (AssetDatabase.IsValidFolder("Assets/_Project/Scenes")) return true;
        if (!AssetDatabase.IsValidFolder("Assets/_Project")) return false;
        AssetDatabase.CreateFolder("Assets/_Project", "Scenes");
        return AssetDatabase.IsValidFolder("Assets/_Project/Scenes");
    }

    static void AddToBuildSettings(string scenePath)
    {
        var scenes = EditorBuildSettings.scenes;
        foreach (var s in scenes)
            if (s.path == scenePath) return; // already registered
        var grown = new EditorBuildSettingsScene[scenes.Length + 1];
        System.Array.Copy(scenes, grown, scenes.Length);
        grown[scenes.Length] = new EditorBuildSettingsScene(scenePath, true);
        EditorBuildSettings.scenes = grown;
    }
}
