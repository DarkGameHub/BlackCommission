using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ADR-0003 Phase-3 NavMesh pass for the procedural grid preview. Bakes a <see cref="NavMeshSurface"/>
/// scoped to the <c>GRID_PREVIEW</c> root produced by <c>Tools ▸ Black Commission ▸ Map ▸ Preview Grid
/// Layout</c>, then validates that the ENTRY and DEEP anchor markers land on the mesh and that an agent
/// can path ENTRY → DEEP across the stitched modules. This is the visual/real-prefab counterpart to the
/// headless <c>GridNavMeshBakeSpikeTests</c> (which proves the approach on box sources): run the preview,
/// then run this to confirm the actual Corridor_*/Junction_* geometry bakes into one connected, walkable
/// navmesh.
///
/// Bakes into the OPEN scene in memory and NEVER saves — persisting the surface/NavMeshData is the PM's
/// call. Re-runnable; reuses the surface on GRID_PREVIEW if present.
///
/// Run via <c>Tools ▸ Black Commission ▸ Map ▸ Bake Grid NavMesh</c> (after Preview Grid Layout).
/// </summary>
public static class GridNavBaker
{
    const string PreviewRoot = "GRID_PREVIEW";

    [MenuItem("Tools/Black Commission/Map/Bake Grid NavMesh")]
    public static void BakeGridNavMesh()
    {
        var root = GameObject.Find(PreviewRoot);
        if (root == null)
        {
            Debug.LogWarning($"[GridNav] No '{PreviewRoot}' in the scene — run " +
                             "'Tools ▸ Black Commission ▸ Map ▸ Preview Grid Layout' first, then bake.");
            return;
        }

        // Surface lives ON the preview root and collects only its children, so the bake is scoped to the
        // generated layout and ignores any surrounding scene geometry.
        var surface = root.GetComponent<NavMeshSurface>();
        if (surface == null) surface = root.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.layerMask = ~0;

        int tris = BakeWith(surface, NavMeshCollectGeometry.PhysicsColliders);
        string source = "PhysicsColliders";
        if (tris == 0)
        {
            Debug.LogWarning("[GridNav] No walkable area from colliders — retrying with render meshes.");
            tris = BakeWith(surface, NavMeshCollectGeometry.RenderMeshes);
            source = "RenderMeshes";
        }

        if (tris == 0 || surface.navMeshData == null)
        {
            Debug.LogError("[GridNav] Bake produced NO walkable navmesh. Check that the preview modules have " +
                           "colliders/meshes and sit on the floor.");
            return;
        }

        Debug.Log($"[GridNav] Bake OK via {source}. bounds={surface.navMeshData.sourceBounds.size} | " +
                  $"walkable triangles={tris} (UNSAVED — save the scene to keep it).");

        // Validate the ENTRY → DEEP route across the stitched modules.
        Transform entry = root.transform.Find($"ANCHOR_ENTRY");
        Transform deep  = root.transform.Find($"ANCHOR_DEEP");
        if (entry == null || deep == null)
        {
            Debug.LogWarning("[GridNav] ANCHOR_ENTRY / ANCHOR_DEEP markers not found under the preview root — " +
                             "skipped the path check.");
            return;
        }

        if (!NavMesh.SamplePosition(entry.position, out var entryHit, 5f, NavMesh.AllAreas) ||
            !NavMesh.SamplePosition(deep.position, out var deepHit, 5f, NavMesh.AllAreas))
        {
            Debug.LogWarning("[GridNav] ENTRY or DEEP anchor has no navmesh within 5 m — the route is not " +
                             "walkable end-to-end. Check tile alignment / capping at that anchor.");
            return;
        }

        var path = new NavMeshPath();
        bool ok = NavMesh.CalculatePath(entryHit.position, deepHit.position, NavMesh.AllAreas, path);
        if (ok && path.status == NavMeshPathStatus.PathComplete)
            Debug.Log($"[GridNav] ✓ ENTRY → DEEP is walkable: complete path with {path.corners.Length} corners, " +
                      $"length {PathLength(path):0.0} m.");
        else
            Debug.LogError($"[GridNav] ✗ ENTRY → DEEP NOT fully walkable (ok={ok}, status={path.status}). " +
                           "A corridor is gapped or a doorway is capped on the only route.");
    }

    static int BakeWith(NavMeshSurface surface, NavMeshCollectGeometry geometry)
    {
        surface.useGeometry = geometry;
        surface.RemoveData();
        surface.BuildNavMesh();
        var tri = NavMesh.CalculateTriangulation();
        return tri.indices.Length / 3;
    }

    static float PathLength(NavMeshPath path)
    {
        float len = 0f;
        for (int i = 1; i < path.corners.Length; i++)
            len += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        return len;
    }
}
