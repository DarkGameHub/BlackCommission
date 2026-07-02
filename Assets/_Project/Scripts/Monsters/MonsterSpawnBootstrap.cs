using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Server-side monster populator for dispatched mission scenes. Mission maps author their
/// threat layout as "MonsterSeed*" marker transforms (the tower whitebox bakes three; new maps
/// place their own), but until now nothing consumed those seeds — a dispatched run shipped with
/// zero monsters. On mission-scene load the SERVER spawns one creature per seed from
/// <c>Resources/Monsters/&lt;type&gt;</c> (registered network prefabs), snapped onto the baked
/// NavMesh so the <see cref="NavMeshAgent"/> can actually path.
///
/// Seed → prefab routing: a seed named <c>MonsterSeed_XX-SUFFIX</c> can pin a species by
/// suffix keyword (see <see cref="PrefabPathForSeed"/>); anything unrecognised falls back to
/// the Echo Mold so existing maps keep working as new species are added.
///
/// Mirrors <see cref="ScavengeMissionBootstrap"/>: <c>[RuntimeInitializeOnLoadMethod]</c> +
/// sceneLoaded, gated to server + listening + scene == ActiveTask.sceneName (ActiveTask is
/// never cleared, so the scene-name gate keeps monsters out of the HQ). Waits a few seconds
/// for late-created seeds/NavMesh (runtime-generated maps bake after network spawn).
/// Idempotent: a seed that already has a monster within 2 m (scene-authored) is skipped.
/// </summary>
public static class MonsterSpawnBootstrap
{
    const string FallbackPrefabPath = "Monsters/EchoMold";
    const float SeedPollSeconds = 6f;
    const float NavSnapRadius = 6f;
    const float OccupiedRadius = 2f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        if (!NetworkManager.Singleton.IsServer) return;
        OfficeTaskDefinition task = MvpMissionRuntime.ActiveTask;
        if (task == null || scene.name != task.sceneName) return;

        var runner = new GameObject("MonsterSpawnBootstrap");
        runner.AddComponent<SpawnRunner>();
    }

    /// <summary>Waits for seeds + NavMesh, spawns per-seed creatures, then self-destructs.</summary>
    class SpawnRunner : MonoBehaviour
    {
        IEnumerator Start()
        {
            float deadline = Time.unscaledTime + SeedPollSeconds;
            Transform[] seeds;
            while ((seeds = FindSeeds()).Length == 0 && Time.unscaledTime < deadline)
                yield return null;

            if (seeds.Length == 0)
            {
                Debug.Log("[MonsterSpawnBootstrap] No MonsterSeed* markers in this map — no monsters this run.");
                Destroy(gameObject);
                yield break;
            }

            // NavMesh may be baked at runtime on procedural maps — wait for it near the first seed.
            while (!NavMesh.SamplePosition(seeds[0].position, out _, NavSnapRadius, NavMesh.AllAreas)
                   && Time.unscaledTime < deadline)
                yield return null;

            int spawned = 0;
            foreach (Transform seed in seeds)
            {
                if (SeedOccupied(seed.position)) continue;
                if (!NavMesh.SamplePosition(seed.position, out NavMeshHit hit, NavSnapRadius, NavMesh.AllAreas))
                {
                    Debug.LogWarning($"[MonsterSpawnBootstrap] Seed '{seed.name}' at {seed.position} has no " +
                                     $"NavMesh within {NavSnapRadius} m — skipped (bake the map's NavMesh).");
                    continue;
                }

                string path = PrefabPathForSeed(seed.name);
                GameObject prefab = Resources.Load<GameObject>(path) ?? Resources.Load<GameObject>(FallbackPrefabPath);
                if (prefab == null)
                {
                    Debug.LogError($"[MonsterSpawnBootstrap] Missing prefab Resources/{path} (and fallback) — aborting.");
                    break;
                }

                GameObject go = Object.Instantiate(prefab, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
                go.name = prefab.name;
                go.GetComponent<NetworkObject>().Spawn();
                spawned++;
            }

            Debug.Log($"[MonsterSpawnBootstrap] Spawned {spawned} monster(s) across {seeds.Length} seed(s).");
            Destroy(gameObject);
        }

        static Transform[] FindSeeds()
        {
            var found = new System.Collections.Generic.List<Transform>();
            foreach (var t in FindObjectsByType<Transform>())
                if (t.name.StartsWith("MonsterSeed")) found.Add(t);
            return found.ToArray();
        }

        /// <summary>Species routing by seed-name keyword; unrecognised names → Echo Mold.</summary>
        static string PrefabPathForSeed(string seedName)
        {
            string upper = seedName.ToUpperInvariant();
            if (upper.Contains("WARDEN")) return "Monsters/FileWarden";
            if (upper.Contains("SNAPPER")) return "Monsters/SealSnapper";
            return FallbackPrefabPath;
        }

        static bool SeedOccupied(Vector3 position)
        {
            foreach (var mold in FindObjectsByType<EchoMold>())
                if (Vector3.Distance(mold.transform.position, position) < OccupiedRadius)
                    return true;
            return false;
        }
    }
}
