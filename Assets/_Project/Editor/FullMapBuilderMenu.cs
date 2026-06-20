using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using BlackCommission.Level;

/// <summary>
/// ADR-0003 map-2 editor driver. Builds the COMPLETE procedural site (edge-based interior + seeded outdoor
/// approach + van drop-off) via <see cref="MapSiteBuilder"/>, bakes a NavMeshSurface over it, and verifies the
/// full traversal DROP-OFF → ENTRY → DEEP is walkable end-to-end. Each click re-rolls a fresh seed so the
/// random variation is visible. Builds into the open scene and never saves (the PM keeps/saves if wanted).
///
/// Menu: Tools ▸ Black Commission ▸ Map ▸ Build Full Map 2
/// </summary>
public static class FullMapBuilderMenu
{
    const string RootName = "FULLMAP_2";

    [MenuItem("Tools/Black Commission/Map/Build Full Map 2")]
    public static void BuildFullMap()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null) Object.DestroyImmediate(existing);

        var root = new GameObject(RootName);
        int seed = (int)(System.DateTime.Now.Ticks & 0x7fffffff);
        MapSiteBuilder.Result res = MapSiteBuilder.Build(root.transform, seed);
        Debug.Log($"[FullMap2] seed {seed}: indoor {res.Floors} floors / {res.Walls} walls; outdoor {res.Scatter} " +
                  $"scatter. dropoff={res.Dropoff} frontDoor={res.FrontDoor} entry={res.Entry} deep={res.Deep}");

        BakeAndVerify(root, res);

        Selection.activeGameObject = root;
        SceneView.FrameLastActiveSceneView();
    }

    static void BakeAndVerify(GameObject root, MapSiteBuilder.Result res)
    {
        var surface = root.GetComponent<NavMeshSurface>();
        if (surface == null) surface = root.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.layerMask = ~0;
        surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        surface.RemoveData();
        surface.BuildNavMesh();

        int tris = NavMesh.CalculateTriangulation().indices.Length / 3;
        if (tris == 0 || surface.navMeshData == null)
        {
            Debug.LogError("[FullMap2] Bake produced NO walkable navmesh — check geometry/colliders.");
            return;
        }
        Debug.Log($"[FullMap2] navmesh baked: {tris} walkable tris, bounds={surface.navMeshData.sourceBounds.size}.");

        VerifyPath("DROP-OFF → ENTRY", res.Dropoff, res.Entry);
        VerifyPath("DROP-OFF → DEEP", res.Dropoff, res.Deep);
    }

    static void VerifyPath(string label, Vector3 a, Vector3 b)
    {
        if (!NavMesh.SamplePosition(a, out var ha, 6f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(b, out var hb, 6f, NavMesh.AllAreas))
        {
            Debug.LogError($"[FullMap2] {label}: an endpoint has no navmesh within 6 m — site NOT connected there.");
            return;
        }
        var path = new NavMeshPath();
        bool ok = NavMesh.CalculatePath(ha.position, hb.position, NavMesh.AllAreas, path);
        if (ok && path.status == NavMeshPathStatus.PathComplete)
        {
            float len = 0f;
            for (int i = 1; i < path.corners.Length; i++) len += Vector3.Distance(path.corners[i - 1], path.corners[i]);
            Debug.Log($"[FullMap2] ✓ {label}: walkable — {path.corners.Length} corners, {len:0.0} m.");
        }
        else
        {
            Debug.LogError($"[FullMap2] ✗ {label}: NOT fully walkable (status={path.status}). Front door/corridor gapped.");
        }
    }
}
