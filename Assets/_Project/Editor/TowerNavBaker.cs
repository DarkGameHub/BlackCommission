using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// First-pass NavMesh bake for the runtime-staged tower mission scene and the
/// "NavMesh pass" step of the tower map workflow. Ensures a single scene-wide
/// <see cref="NavMeshSurface"/> exists, bakes it for the default Humanoid agent,
/// then validates that key actors (the Echo Mold) and candidate lurk points land
/// on the mesh so the monster can actually path.
///
/// Bakes into the OPEN scene in memory. The surface component and resulting
/// NavMeshData only persist if the scene is saved — that is the PM's call, so this
/// tool never saves. Re-runnable; reuses an existing surface if one is present.
///
/// Run via <c>Tools ▸ Black Commission ▸ Map ▸ Bake Tower NavMesh</c>.
/// </summary>
public static class TowerNavBaker
{
    /// <summary>Candidate upper-level lurk/approach points to probe for walkability (world space).</summary>
    static readonly Vector3[] Candidates =
    {
        new Vector3(18f, 4.3f, 16f), // atrium center
        new Vector3(16f, 4.3f, 18f), // atrium, deeper / further from objective
        new Vector3(20f, 4.3f, 14f), // atrium -> objective approach
        new Vector3(22f, 4.3f, 14f), // mid approach
        new Vector3(24f, 4.3f, 14f), // approach, nearer objective
        new Vector3(28f, 4.3f, 12f), // corridor mouth before objective room
    };

    [MenuItem("Tools/Black Commission/Map/Bake Tower NavMesh")]
    public static void BakeTowerNavMesh()
    {
        // 1. Ensure a single scene-wide surface.
        var surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            var go = new GameObject("NavMeshSurface");
            go.transform.position = Vector3.zero;
            surface = go.AddComponent<NavMeshSurface>();
            Debug.Log("[TowerNav] Created scene NavMeshSurface (UNSAVED — save the scene to keep it).");
        }
        else
        {
            Debug.Log($"[TowerNav] Reusing existing NavMeshSurface on '{surface.gameObject.name}'.");
        }

        surface.collectObjects = CollectObjects.All;
        surface.layerMask = ~0;

        // 2. Bake. Prefer physics colliders (where players can actually stand); if that
        //    yields nothing, fall back to render meshes so a geometry-only scene still bakes.
        int tris = BakeWith(surface, NavMeshCollectGeometry.PhysicsColliders);
        string source = "PhysicsColliders";
        if (tris == 0)
        {
            Debug.LogWarning("[TowerNav] No walkable area from colliders — retrying with render meshes.");
            tris = BakeWith(surface, NavMeshCollectGeometry.RenderMeshes);
            source = "RenderMeshes";
        }

        if (tris == 0 || surface.navMeshData == null)
        {
            Debug.LogError("[TowerNav] Bake produced NO walkable navmesh. Check that tower geometry is active and has colliders/meshes.");
            return;
        }

        Debug.Log($"[TowerNav] Bake OK via {source}. bounds={surface.navMeshData.sourceBounds.size} | walkable triangles={tris}.");

        // 3. Validate the live actor + report which candidate lurk points are usable.
        ValidateOnMesh("EchoMold");
        Debug.Log("[TowerNav] Candidate lurk points (✓ = on mesh / will snap, ✗ = no nav within 4 m):");
        foreach (var c in Candidates)
        {
            if (NavMesh.SamplePosition(c, out var hit, 4f, NavMesh.AllAreas))
                Debug.Log($"[TowerNav]   ✓ {c} -> {hit.position} (Δ={Vector3.Distance(c, hit.position):0.00} m)");
            else
                Debug.Log($"[TowerNav]   ✗ {c} (no navmesh within 4 m)");
        }
    }

    static int BakeWith(NavMeshSurface surface, NavMeshCollectGeometry geometry)
    {
        surface.useGeometry = geometry;
        surface.RemoveData();
        surface.BuildNavMesh();
        var tri = NavMesh.CalculateTriangulation();
        return tri.indices.Length / 3;
    }

    static void ValidateOnMesh(string name)
    {
        var go = GameObject.Find(name);
        if (go == null) { Debug.LogWarning($"[TowerNav] '{name}' not found in scene — skipped on-mesh check."); return; }

        // Ground-truth component dump (the MCP bridge's get_gameobject under-reports these).
        string Has<T>() where T : Component => go.GetComponent<T>() != null ? "✓" : "✗ MISSING";
        Debug.Log($"[TowerNav] '{name}' real components: " +
                  $"Animator {Has<Animator>()}, NavMeshAgent {Has<NavMeshAgent>()}, " +
                  $"CapsuleCollider {Has<CapsuleCollider>()}, NetworkObject {Has<Unity.Netcode.NetworkObject>()}, " +
                  $"EchoMold {Has<EchoMold>()} | childRig={go.GetComponentsInChildren<MeshRenderer>(true).Length} meshes.");

        Vector3 p = go.transform.position;
        if (NavMesh.SamplePosition(p, out var hit, 5f, NavMesh.AllAreas))
        {
            float d = Vector3.Distance(p, hit.position);
            string verdict = d < 1.0f ? "ON MESH" : "NEAR (agent will snap on enable)";
            Debug.Log($"[TowerNav] '{name}' at {p} -> nearest navmesh {hit.position} (Δ={d:0.00} m) — {verdict}.");
        }
        else
        {
            Debug.LogWarning($"[TowerNav] '{name}' at {p} has NO navmesh within 5 m — it cannot move. Reposition onto walkable floor.");
        }
    }
}
