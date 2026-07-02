using System.IO;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// One-shot "make this mission map shippable" pass for a SAVED map scene. Complements the
/// per-map builders and <c>TowerNavBaker</c> (which bakes in memory only): this tool persists
/// everything so a dispatched run works from a cold scene load.
///
/// Steps, in order:
/// <list type="number">
///   <item>Van visual — every map's <see cref="MissionVanExitPoint"/> is a bare trigger, so the
///   "board" prompt floats in mid-air. Adds the office van model (Resources/GeneratedArt/
///   AS_OfficeVan) + a solid body collider under the exit point, grounded via raycast, nose
///   pointed away from the map centre. Skipped if the exit point already has a renderer child.</item>
///   <item>NavMesh — ensures a scene-wide <see cref="NavMeshSurface"/>, bakes (colliders first,
///   render-mesh fallback), and SAVES the NavMeshData asset next to the scenes folder so
///   <see cref="EchoMold"/>'s NavMeshAgent paths after a plain scene load.</item>
///   <item>Saves the scene.</item>
/// </list>
///
/// Run via <c>Tools ▸ Black Commission ▸ Map ▸ Finalize Mission Map (Van + NavMesh + Save)</c>
/// with the target map open, or call <see cref="FinalizeActiveScene"/> from editor code.
/// </summary>
public static class MissionMapFinalizer
{
    const string VanResourcePath = "GeneratedArt/AS_OfficeVan";
    const string VanVisualName = "VAN_Visual";
    const string NavDataFolder = "Assets/_Project/Scenes/NavMeshData";

    [MenuItem("Tools/Black Commission/Map/Finalize Mission Map (Van + NavMesh + Save)")]
    public static void FinalizeActiveScene()
    {
        if (Application.isPlaying)
        {
            Debug.LogError("[MapFinalizer] Refusing to run in Play mode — geometry would be lost on exit.");
            return;
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            Debug.LogError("[MapFinalizer] Active scene has never been saved — save it first so NavMeshData has a home.");
            return;
        }

        EnsureVanVisual();
        BakeAndPersistNavMesh(scene.name);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[MapFinalizer] '{scene.name}' finalized and saved.");
    }

    static void EnsureVanVisual()
    {
        var exit = Object.FindAnyObjectByType<MissionVanExitPoint>(FindObjectsInactive.Include);
        if (exit == null)
        {
            Debug.Log("[MapFinalizer] No MissionVanExitPoint in scene (runtime-rig map) — van visual skipped.");
            return;
        }
        // Re-runs rebuild our own visual; a hand-authored visual (any other renderer) is respected.
        Transform stale = exit.transform.Find(VanVisualName);
        if (stale != null) Object.DestroyImmediate(stale.gameObject);
        if (exit.GetComponentInChildren<Renderer>(true) != null)
        {
            Debug.Log("[MapFinalizer] Exit point already has a visual — skipped.");
            return;
        }

        var prefab = Resources.Load<GameObject>(VanResourcePath);
        if (prefab == null)
        {
            Debug.LogWarning($"[MapFinalizer] Resources/{VanResourcePath} missing — van visual skipped.");
            return;
        }

        // The trigger can sit against (or on) walls, so a straight-down ray may land on a wall
        // top. Probe a ring of candidates around the exit, keep only hits genuinely BELOW the
        // lifted trigger, and prefer the one nearest the player spawn — that side is the yard
        // the crew actually walks on. Falls back to trigger height - 1.
        Vector3 exitPos = exit.transform.position;
        Vector3 groundPos = new Vector3(exitPos.x, exitPos.y - 1f, exitPos.z);
        var spawn = GameObject.Find("PlayerSpawnPoint");
        Vector3 preferTo = spawn != null ? spawn.transform.position : exitPos;
        float bestScore = float.MaxValue;
        bool found = false;
        for (int i = -1; i < 8; i++)
        {
            Vector3 probe = exitPos;
            if (i >= 0)
            {
                float a = i * Mathf.PI * 2f / 8f;
                probe += new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3f;
            }
            if (!Physics.Raycast(probe + Vector3.up * 4f, Vector3.down, out RaycastHit hitInfo, 30f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
            if (hitInfo.point.y > exitPos.y - 0.5f) continue;   // wall top / platform — not ground
            // Near the spawn side, but never ON the spawn — a van body over the spawn point
            // traps every player inside its collider on mission start.
            float dist = Vector3.Distance(hitInfo.point, preferTo);
            if (dist < 3f) continue;
            if (dist < bestScore) { bestScore = dist; groundPos = hitInfo.point; found = true; }
        }
        if (!found)
            Debug.LogWarning("[MapFinalizer] No ground below the exit trigger found — van uses fallback height.");

        // AS_OfficeVan's nose is its local +X (measured 2026-07-01); world nose after yaw θ is
        // (cos θ, 0, -sin θ). Point the nose away from the map so it reads as "parked, ready to leave".
        Vector3 centre = SceneRenderCentre();
        Vector3 away = exitPos - centre; away.y = 0f;
        if (away.sqrMagnitude < 0.01f) away = Vector3.back;
        away.Normalize();
        float yaw = Mathf.Atan2(-away.z, away.x) * Mathf.Rad2Deg;

        var van = (GameObject)Object.Instantiate(prefab, exit.transform);
        van.name = VanVisualName;
        van.transform.position = groundPos;
        van.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        van.transform.localScale = Vector3.one;

        // Model colliders off (mirrors the HQ prop idiom); one solid body box so players can't
        // walk through the van. Sized from the rendered bounds, slightly shrunk.
        foreach (var col in van.GetComponentsInChildren<Collider>()) col.enabled = false;
        Bounds b = RenderBounds(van);
        if (b.size != Vector3.zero)
        {
            var solid = new GameObject("VAN_BodyCollider");
            solid.transform.SetParent(van.transform, false);
            solid.transform.position = b.center;
            var box = solid.AddComponent<BoxCollider>();
            box.size = Vector3.Scale(b.size, new Vector3(0.9f, 0.95f, 0.9f));
        }

        Debug.Log($"[MapFinalizer] Van visual added at {groundPos} (yaw {yaw:0}).");
    }

    static void BakeAndPersistNavMesh(string sceneName)
    {
        var surface = Object.FindAnyObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            var go = new GameObject("NavMeshSurface");
            surface = go.AddComponent<NavMeshSurface>();
        }
        surface.collectObjects = CollectObjects.All;
        surface.layerMask = ~0;

        int tris = BakeWith(surface, NavMeshCollectGeometry.PhysicsColliders);
        string source = "PhysicsColliders";
        if (tris == 0)
        {
            tris = BakeWith(surface, NavMeshCollectGeometry.RenderMeshes);
            source = "RenderMeshes";
        }
        if (tris == 0 || surface.navMeshData == null)
        {
            Debug.LogError("[MapFinalizer] Bake produced NO walkable navmesh — check active geometry/colliders.");
            return;
        }

        // Persist the baked data as an asset; without this the surface references in-memory
        // data that dies with the editor session and the saved scene ships mesh-less.
        if (!Directory.Exists(NavDataFolder)) Directory.CreateDirectory(NavDataFolder);
        string assetPath = $"{NavDataFolder}/{sceneName}_NavMesh.asset";
        var existing = AssetDatabase.LoadAssetAtPath<NavMeshData>(assetPath);
        if (existing != null) AssetDatabase.DeleteAsset(assetPath);
        AssetDatabase.CreateAsset(surface.navMeshData, assetPath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[MapFinalizer] NavMesh baked via {source}: {tris} tris → {assetPath}.");
    }

    static int BakeWith(NavMeshSurface surface, NavMeshCollectGeometry geometry)
    {
        surface.useGeometry = geometry;
        surface.RemoveData();
        surface.BuildNavMesh();
        var tri = NavMesh.CalculateTriangulation();
        return tri.indices.Length / 3;
    }

    static Vector3 SceneRenderCentre()
    {
        var rs = Object.FindObjectsByType<Renderer>();
        if (rs.Length == 0) return Vector3.zero;
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b.center;
    }

    static Bounds RenderBounds(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = rs[0].bounds;
        for (int i = 1; i < rs.Length; i++) b.Encapsulate(rs[i].bounds);
        return b;
    }
}
