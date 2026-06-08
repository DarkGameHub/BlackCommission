using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class TowerColliderUtility
{
    const string TowerRootName = "Tower_v3_Whitebox";
    const string RampPrefix = "WalkableRampCollider";

    [MenuItem("Tools/Black Commission/MVP/Tower/Colliders/Add Missing Mesh Colliders To Selection")]
    public static void AddMissingMeshCollidersToSelection()
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("[TowerColliders] Select the stairs/floors/walls you want to make walkable first.");
            return;
        }

        AddMissingMeshColliders(Selection.gameObjects, "selection");
    }

    [MenuItem("Tools/Black Commission/MVP/Tower/Colliders/Add Missing Mesh Colliders To Tower v3 Whitebox")]
    public static void AddMissingMeshCollidersToTower()
    {
        GameObject root = GameObject.Find(TowerRootName);
        if (root == null)
        {
            Debug.LogWarning($"[TowerColliders] Could not find '{TowerRootName}' in the current scene.");
            return;
        }

        AddMissingMeshColliders(new[] { root }, TowerRootName);
    }

    [MenuItem("Tools/Black Commission/MVP/Tower/Colliders/Report Missing Mesh Colliders In Tower v3 Whitebox")]
    public static void ReportMissingMeshCollidersInTower()
    {
        GameObject root = GameObject.Find(TowerRootName);
        if (root == null)
        {
            Debug.LogWarning($"[TowerColliders] Could not find '{TowerRootName}' in the current scene.");
            return;
        }

        var missing = FindMissingMeshColliders(new[] { root });
        Debug.Log($"[TowerColliders] Missing MeshCollider count under '{TowerRootName}': {missing.Count}");
        for (int i = 0; i < Mathf.Min(missing.Count, 60); i++)
            Debug.Log("  - " + HierarchyPath(missing[i].transform));
    }

    [MenuItem("Tools/Black Commission/MVP/Tower/Colliders/Add Smooth Stair Ramp To Selection/+X Low-to-High")]
    public static void AddRampPosX() => AddSmoothRampCollidersToSelection(Vector3.right, "+X");

    [MenuItem("Tools/Black Commission/MVP/Tower/Colliders/Add Smooth Stair Ramp To Selection/-X Low-to-High")]
    public static void AddRampNegX() => AddSmoothRampCollidersToSelection(Vector3.left, "-X");

    [MenuItem("Tools/Black Commission/MVP/Tower/Colliders/Add Smooth Stair Ramp To Selection/+Z Low-to-High")]
    public static void AddRampPosZ() => AddSmoothRampCollidersToSelection(Vector3.forward, "+Z");

    [MenuItem("Tools/Black Commission/MVP/Tower/Colliders/Add Smooth Stair Ramp To Selection/-Z Low-to-High")]
    public static void AddRampNegZ() => AddSmoothRampCollidersToSelection(Vector3.back, "-Z");

    static void AddMissingMeshColliders(IReadOnlyList<GameObject> roots, string label)
    {
        var missing = FindMissingMeshColliders(roots);
        if (missing.Count == 0)
        {
            Debug.Log($"[TowerColliders] No missing MeshColliders found in {label}.");
            return;
        }

        Undo.SetCurrentGroupName("Add Tower Mesh Colliders");
        int group = Undo.GetCurrentGroup();
        int added = 0;

        foreach (MeshFilter filter in missing)
        {
            MeshCollider collider = Undo.AddComponent<MeshCollider>(filter.gameObject);
            collider.sharedMesh = filter.sharedMesh;
            collider.convex = ShouldUseConvexCollider(filter.gameObject);
            added++;
        }

        Undo.CollapseUndoOperations(group);
        MarkActiveSceneDirty();
        Debug.Log($"[TowerColliders] Added {added} MeshCollider(s) in {label}. Save the scene to keep them.");
    }

    static List<MeshFilter> FindMissingMeshColliders(IReadOnlyList<GameObject> roots)
    {
        var result = new List<MeshFilter>();
        var seen = new HashSet<MeshFilter>();

        foreach (GameObject root in roots)
        {
            if (root == null || !root.scene.IsValid()) continue;
            foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter == null || filter.sharedMesh == null) continue;
                if (seen.Contains(filter)) continue;
                seen.Add(filter);

                GameObject go = filter.gameObject;
                if (EditorUtility.IsPersistent(go)) continue;
                if (go.GetComponent<Collider>() != null) continue;
                if (go.GetComponent<MeshRenderer>() == null) continue;
                if (go.name.EndsWith("_Slot")) continue;
                result.Add(filter);
            }
        }

        return result;
    }

    static bool ShouldUseConvexCollider(GameObject go)
    {
        Rigidbody body = go.GetComponentInParent<Rigidbody>();
        return body != null && !body.isKinematic;
    }

    static void AddSmoothRampCollidersToSelection(Vector3 lowToHighDirection, string label)
    {
        if (Selection.gameObjects.Length == 0)
        {
            Debug.LogWarning("[TowerColliders] Select one or more stair meshes first.");
            return;
        }

        Undo.SetCurrentGroupName("Add Smooth Stair Ramp Colliders");
        int group = Undo.GetCurrentGroup();
        int added = 0;

        foreach (GameObject selected in Selection.gameObjects)
        {
            if (selected == null || !selected.scene.IsValid()) continue;
            if (!TryGetRendererBounds(selected, out Bounds bounds)) continue;

            string rampName = $"{RampPrefix}_{label}";
            if (selected.transform.Find(rampName) != null) continue;

            GameObject ramp = new GameObject(rampName);
            Undo.RegisterCreatedObjectUndo(ramp, "Add Smooth Stair Ramp Collider");
            ramp.transform.SetParent(selected.transform, true);

            Vector3 dir = new Vector3(lowToHighDirection.x, 0f, lowToHighDirection.z).normalized;
            bool alongX = Mathf.Abs(dir.x) > Mathf.Abs(dir.z);
            float length = Mathf.Max(alongX ? bounds.size.x : bounds.size.z, 0.5f);
            float width = Mathf.Max(alongX ? bounds.size.z : bounds.size.x, 0.5f);
            float rise = Mathf.Clamp(bounds.size.y, 0.15f, length * Mathf.Tan(35f * Mathf.Deg2Rad));
            float angle = Mathf.Atan2(rise, length) * Mathf.Rad2Deg;

            ramp.transform.position = new Vector3(bounds.center.x, bounds.min.y + rise * 0.5f, bounds.center.z);
            ramp.transform.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(-angle, 0f, 0f);

            BoxCollider collider = Undo.AddComponent<BoxCollider>(ramp);
            collider.size = new Vector3(width, 0.18f, Mathf.Sqrt(length * length + rise * rise));
            collider.center = Vector3.zero;
            added++;
        }

        Undo.CollapseUndoOperations(group);
        MarkActiveSceneDirty();
        Debug.Log($"[TowerColliders] Added {added} smooth stair ramp collider(s). Direction means low side to high side: {label}.");
    }

    static bool TryGetRendererBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || !renderer.enabled) continue;
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }
        return hasBounds;
    }

    static void MarkActiveSceneDirty()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.IsValid())
            EditorSceneManager.MarkSceneDirty(scene);
    }

    static string HierarchyPath(Transform transform)
    {
        var parts = new Stack<string>();
        for (Transform t = transform; t != null; t = t.parent)
            parts.Push(t.name);
        return string.Join("/", parts);
    }
}
