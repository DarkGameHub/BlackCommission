using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>
/// Server-side monster populator for dispatched mission scenes. Mission maps author their
/// threat layout as "MonsterSeed*" marker transforms (the tower whitebox bakes three; new maps
/// place their own); the SERVER spawns creatures from <c>Resources/Monsters/&lt;type&gt;</c>
/// (registered network prefabs), snapped onto the baked NavMesh so the
/// <see cref="NavMeshAgent"/> can actually path.
///
/// Seeds are CANDIDATE points, not guarantees: activation is gated by the site danger track
/// (<see cref="BlackCommission.Monsters.DangerClock"/>, danger-infection quick-spec 2026-06-18).
/// Survey opens with a sparse third of the seeds, Active brings in two thirds, Pursuit lights
/// them all — "the building wakes up" instead of the full roster camping the entrance at t=0.
/// Seeds activate in name order (sorted), so level design can stage priority by naming.
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

    /// <summary>
    /// Waits for seeds + NavMesh, then activates seeds as the danger phase advances
    /// (Survey ⌈N/3⌉ → Active ⌈2N/3⌉ → Pursuit all); self-destructs once every seed is live.
    /// </summary>
    class SpawnRunner : MonoBehaviour
    {
        const float PhasePollSeconds = 5f;

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

            // Deterministic activation order; scene-authored monsters already parked on a seed
            // count as that seed being live.
            System.Array.Sort(seeds, (a, b) => string.CompareOrdinal(a.name, b.name));
            var live = new bool[seeds.Length];
            int liveCount = 0;
            for (int i = 0; i < seeds.Length; i++)
                if (SeedOccupied(seeds[i].position)) { live[i] = true; liveCount++; }

            var clock = BlackCommission.Monsters.DangerConfig.LoadClockOrDefaults();
            float missionStart = Time.time;
            var lastPhase = (BlackCommission.Monsters.DangerPhase)(-1);

            while (liveCount < seeds.Length)
            {
                var phase = clock.PhaseAt(Time.time - missionStart);
                int target = BlackCommission.Monsters.DangerClock.ActiveSeedCount(seeds.Length, phase);
                if (phase != lastPhase)
                {
                    lastPhase = phase;
                    Debug.Log($"[MonsterSpawnBootstrap] Danger phase → {phase}: {target}/{seeds.Length} seed(s) live.");
                }

                for (int i = 0; i < seeds.Length && liveCount < target; i++)
                {
                    if (live[i]) continue;
                    if (PlayerNear(seeds[i].position)) continue;  // never spawn in a player's face — retry next poll
                    live[i] = true;           // consumed even when spawn fails — don't retry a bad seed forever
                    liveCount++;
                    TrySpawnAt(seeds[i]);
                }

                yield return new WaitForSeconds(PhasePollSeconds);
            }

            Debug.Log($"[MonsterSpawnBootstrap] All {seeds.Length} seed(s) live — runner done.");
            Destroy(gameObject);
        }

        static void TrySpawnAt(Transform seed)
        {
            if (!NavMesh.SamplePosition(seed.position, out NavMeshHit hit, NavSnapRadius, NavMesh.AllAreas))
            {
                Debug.LogWarning($"[MonsterSpawnBootstrap] Seed '{seed.name}' at {seed.position} has no " +
                                 $"NavMesh within {NavSnapRadius} m — skipped (bake the map's NavMesh).");
                return;
            }

            string path = PrefabPathForSeed(seed.name);
            GameObject prefab = Resources.Load<GameObject>(path) ?? Resources.Load<GameObject>(FallbackPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[MonsterSpawnBootstrap] Missing prefab Resources/{path} (and fallback) — skipped.");
                return;
            }

            GameObject go = Object.Instantiate(prefab, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            go.name = prefab.name;
            // A NavMeshAgent instantiated at a position binds to the navmesh at the PREFAB
            // pose and yanks the transform back to the origin on its first update — Warp is
            // the documented fix (verified live 2026-07-02: all three monsters sat at 0,0,0).
            var agent = go.GetComponent<NavMeshAgent>();
            if (agent != null) agent.Warp(hit.position);
            go.GetComponent<NetworkObject>().Spawn();
            Debug.Log($"[MonsterSpawnBootstrap] Seed '{seed.name}' activated → {prefab.name}.");
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
            if (upper.Contains("IDOL") || upper.Contains("STATUE")) return "Monsters/CivicIdol";
            return FallbackPrefabPath;
        }

        static bool PlayerNear(Vector3 position)
        {
            foreach (var player in FindObjectsByType<PlayerController>())
                if (Vector3.Distance(player.transform.position, position) < 12f)
                    return true;
            return false;
        }

        static bool SeedOccupied(Vector3 position)
        {
            foreach (var mold in FindObjectsByType<EchoMold>())
                if (Vector3.Distance(mold.transform.position, position) < OccupiedRadius)
                    return true;
            foreach (var idol in FindObjectsByType<CivicIdol>())
                if (Vector3.Distance(idol.transform.position, position) < OccupiedRadius)
                    return true;
            return false;
        }
    }
}
