using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Closes the commission loop on REAL mission scenes: the saved map scenes (Tower_EarthCoast_01,
/// Map2_Procedural) ship without the scavenge machinery — it only ever existed in the testbed or
/// as unsaved editor-builder output — so a dispatched run had no loot, no cargo bay and no way to
/// settle or return. On mission-scene load the SERVER spawns whichever pieces are missing from
/// registered network prefabs (Resources/Mission/*, see ScavengeMissionRigBuilder):
/// ScavengeMissionManager, ScavengeCargoZone, LootSpawner, and a MissionVanExitPoint when the
/// scene has none. Scene-authored pieces always win — nothing is duplicated.
///
/// Placement anchors, in priority order: an existing MissionVanExitPoint → the map's named van
/// anchor ("DROPOFF_VanSpawn", Map2) → "PlayerSpawnPoint" → world origin. Runtime-generated maps
/// create their anchors on their own network spawn, so the bootstrap polls a few seconds before
/// giving up (mirrors LootSpawner's deferred fill).
/// </summary>
public static class ScavengeMissionBootstrap
{
    const string ManagerPrefabPath = "Mission/ScavengeMissionManager";
    const string CargoZonePrefabPath = "Mission/ScavengeCargoZone";
    const string LootSpawnerPrefabPath = "Mission/ScavengeLootSpawner";
    const string ExitPointPrefabPath = "Mission/ScavengeVanExitPoint";

    // Cargo bay sits beside the boarding point (mirrors TowerV8WhiteboxBuilder.BuildMissionManager:
    // cargo ≈ exit + (2.5, 0.25, -0.5); the bay trigger centre rides at half its 2.5 m height).
    static readonly Vector3 CargoZoneOffset = new Vector3(2.5f, 1.25f, -0.5f);
    static readonly Vector3 ExitPointLift = new Vector3(0f, 1.0f, 0f);
    const float AnchorPollSeconds = 5f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Server-only, networked sessions only (the solo non-network path never dispatches to a
        // real map today), and only on the active commission's own scene — ActiveTask survives the
        // return to HQ, so the scene-name gate is what keeps the rig out of the office.
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        if (!NetworkManager.Singleton.IsServer) return;
        OfficeTaskDefinition task = MvpMissionRuntime.ActiveTask;
        if (task == null || scene.name != task.sceneName) return;

        var runner = new GameObject("ScavengeMissionBootstrap");
        runner.AddComponent<BootstrapRunner>();
    }

    /// <summary>Waits for the map's van anchor, spawns the missing pieces, then self-destructs.</summary>
    class BootstrapRunner : MonoBehaviour
    {
        IEnumerator Start()
        {
            float deadline = Time.unscaledTime + AnchorPollSeconds;
            Transform anchor;
            while (!TryFindAnchor(out anchor) && Time.unscaledTime < deadline)
                yield return null;

            if (anchor == null)
                Debug.LogWarning("[ScavengeMissionBootstrap] No van anchor (MissionVanExitPoint / " +
                                 "DROPOFF_VanSpawn / PlayerSpawnPoint) appeared — spawning the rig at origin.");

            Vector3 basePos = anchor != null ? anchor.position : Vector3.zero;

            if (Object.FindAnyObjectByType<MissionVanExitPoint>() == null)
                SpawnPiece(ExitPointPrefabPath, basePos + ExitPointLift);

            if (ScavengeCargoZone.Instance == null)
                SpawnPiece(CargoZonePrefabPath, basePos + CargoZoneOffset);

            if (Object.FindAnyObjectByType<LootSpawner>() == null)
                SpawnPiece(LootSpawnerPrefabPath, basePos);

            if (ScavengeMissionManager.Instance == null)
                SpawnPiece(ManagerPrefabPath, basePos);

            Destroy(gameObject);
        }

        static bool TryFindAnchor(out Transform anchor)
        {
            var exit = Object.FindAnyObjectByType<MissionVanExitPoint>();
            if (exit != null) { anchor = exit.transform; return true; }
            var named = GameObject.Find("DROPOFF_VanSpawn") ?? GameObject.Find("PlayerSpawnPoint");
            if (named != null) { anchor = named.transform; return true; }
            anchor = null;
            return false;
        }

        static void SpawnPiece(string resourcePath, Vector3 position)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[ScavengeMissionBootstrap] Missing prefab Resources/{resourcePath} — " +
                               "run Tools > Black Commission > Scavenge > Build Mission Rig Prefabs.");
                return;
            }
            GameObject go = Object.Instantiate(prefab, position, Quaternion.identity);
            go.name = prefab.name;
            go.GetComponent<NetworkObject>().Spawn();
            Debug.Log($"[ScavengeMissionBootstrap] Spawned {prefab.name} at {position}.");
        }
    }
}
