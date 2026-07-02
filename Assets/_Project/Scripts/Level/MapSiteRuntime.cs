using System.Collections;
using Unity.AI.Navigation;
using Unity.Netcode;
using UnityEngine;
using BlackCommission.Level;

/// <summary>
/// Runtime driver for the map-2 procedural site (ADR-0003). The server rolls ONE seed, replicates it via a
/// <see cref="NetworkVariable{T}"/>, and every peer (host + clients) rebuilds the IDENTICAL site locally from
/// that seed — <see cref="MapSiteBuilder.Build"/> is proven deterministic (GridMapReachabilityHarnessTests:
/// 1000-seed byte-identical, 0 unreachable). Only the int seed crosses the wire; the geometry + loot anchors
/// are generated, not networked. The host's <c>LootSpawner</c> then fills the (now identical) anchors and
/// replicates the spawned items, so loot lands correctly on every peer. Mirrors
/// <see cref="GridMapNetworkBuilder"/>'s seed-sync exactly.
///
/// Offline (direct Play / PreviewWalker walk-test, no NetworkManager listening) it builds immediately from a
/// local seed, so the scene stays walkable solo without a host. <c>fixedSeed != 0</c> forces a repeatable layout.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class MapSiteRuntime : NetworkBehaviour
{
    [Tooltip("Non-zero forces a repeatable layout; 0 = a fresh random seed each session (server-chosen).")]
    [SerializeField] int fixedSeed = 0;
    [SerializeField] int width = 28;
    [SerializeField] int height = 24;

    // Replicated seed: server writes, everyone reads. -1 = not chosen yet.
    readonly NetworkVariable<int> netSeed =
        new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone,
                                     NetworkVariableWritePermission.Server);

    bool built;

    // Offline / direct-Play path: no NGO session → build now with a local seed (single peer, determinism moot).
    void Start()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            BuildOnce(fixedSeed != 0 ? fixedSeed : new System.Random().Next(1, int.MaxValue));
    }

    public override void OnNetworkSpawn()
    {
        netSeed.OnValueChanged += OnSeedChanged;
        if (IsServer)
        {
            // Host chooses the one seed for the whole session and replicates it.
            netSeed.Value = fixedSeed != 0 ? fixedSeed : new System.Random().Next(1, int.MaxValue);
            BuildIfReady(); // server can build immediately
        }
        else
        {
            BuildIfReady(); // late joiner: the value may already be synced with the spawn
        }
    }

    public override void OnNetworkDespawn() => netSeed.OnValueChanged -= OnSeedChanged;

    void OnSeedChanged(int previous, int current) => BuildIfReady();

    void BuildIfReady()
    {
        if (built || netSeed.Value < 0) return;
        BuildOnce(netSeed.Value);
    }

    void BuildOnce(int seed)
    {
        if (built) return;
        built = true;
        MapSiteBuilder.Result res = MapSiteBuilder.Build(transform, seed, width, height);
        Debug.Log($"[MapSiteRuntime] seed {seed}: {res.Floors} floors / {res.Walls} walls / {res.Scatter} scatter; " +
                  $"drop-off at {res.Dropoff}, deep at {res.Deep}.");

        // Monsters (NavMeshAgents) tick only on the authority, so only it pays for the runtime
        // bake; clients never query this navmesh. Offline preview counts as authority too.
        bool isAuthority = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || IsServer;
        if (isAuthority) StartCoroutine(BakeNavMeshDeferred());
    }

    // One frame after the build so the freshly created colliders/terrain are registered with
    // physics before NavMeshSurface collects them.
    IEnumerator BakeNavMeshDeferred()
    {
        yield return null;
        var surface = GetComponent<NavMeshSurface>();
        if (surface == null) surface = gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children; // the whole site lives under this transform
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;
        surface.layerMask = ~0;
        surface.BuildNavMesh();
        int tris = UnityEngine.AI.NavMesh.CalculateTriangulation().indices.Length / 3;
        Debug.Log($"[MapSiteRuntime] Runtime NavMesh baked: {tris} tris (monsters can path).");
    }
}
