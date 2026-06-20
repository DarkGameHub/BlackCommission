using UnityEngine;
using BlackCommission.Level;

/// <summary>
/// Runtime driver for the map-2 procedural site (ADR-0003, direction B — single-player path; multiplayer
/// determinism is backlogged). On <see cref="Start"/> it builds the full site (outdoor approach + van
/// drop-off + edge-based interior) from a seed via <see cref="MapSiteBuilder"/>, so entering Play in the map-2
/// scene generates a fresh random outdoor→indoor site you can walk as a player (the whitebox geometry carries
/// colliders, so player physics works without a navmesh). <c>fixedSeed != 0</c> forces a repeatable layout.
///
/// Navmesh (for AI agents) is baked + verified by the editor tool "Build Full Map 2"; a runtime navmesh bake
/// is a follow-up once the monster is wired into this map (see production/backlog.md).
/// </summary>
public class MapSiteRuntime : MonoBehaviour
{
    [Tooltip("Non-zero forces a repeatable layout; 0 = a fresh random seed each run.")]
    [SerializeField] int fixedSeed = 0;
    [SerializeField] int width = 28;
    [SerializeField] int height = 24;

    void Start()
    {
        int seed = fixedSeed != 0 ? fixedSeed : new System.Random().Next(1, int.MaxValue);
        MapSiteBuilder.Result res = MapSiteBuilder.Build(transform, seed, width, height);
        Debug.Log($"[MapSiteRuntime] seed {seed}: {res.Floors} floors / {res.Walls} walls / {res.Scatter} scatter; " +
                  $"drop-off at {res.Dropoff}, deep at {res.Deep}.");
    }
}
