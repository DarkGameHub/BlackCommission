using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Bakes the abandoned-tower NavMesh (感染监理 pathfinding prerequisite).
/// Adds a NavMeshSurface to the V8 whitebox root (children-only, physics colliders),
/// bakes, and saves the scene. Power shutters / toggle rubble plugs carry carving
/// NavMeshObstacles, so gated routes stay blocked until opened at runtime.
/// </summary>
public static class TowerNavMeshBaker
{
    const string ScenePath = "Assets/_Project/Scenes/Tower_EarthCoast_01.unity";
    const string RootName = "Tower_v8_Whitebox";

    [MenuItem("Tools/Black Commission/MVP/Tower/Bake Tower NavMesh")]
    public static void Bake()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        GameObject root = null;
        foreach (GameObject go in scene.GetRootGameObjects())
            if (go.name == RootName) { root = go; break; }
        if (root == null)
        {
            Debug.LogError($"[TowerNav] '{RootName}' not found — run the v8 rebuild first.");
            return;
        }

        var surface = root.GetComponent<NavMeshSurface>();
        if (surface == null) surface = root.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.useGeometry = UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;

        surface.BuildNavMesh();

        // Persist NavMeshData as a standalone asset. Left embedded in the scene it
        // forces Unity to save the WHOLE scene as binary (even under ForceText),
        // which kills git diffs and every text-based scene check.
        const string navAssetPath = "Assets/_Project/Scenes/Tower_EarthCoast_01_NavMesh.asset";
        var data = surface.navMeshData;
        if (data != null && !AssetDatabase.Contains(data))
        {
            AssetDatabase.DeleteAsset(navAssetPath);
            data.name = "Tower_NavMesh";
            AssetDatabase.CreateAsset(data, navAssetPath);
            surface.navMeshData = AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(navAssetPath);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        var nav = surface.navMeshData;
        Debug.Log($"[TowerNav] NavMesh baked: {(nav != null ? $"bounds {nav.sourceBounds.size}" : "NO DATA")}. " +
                  "Scene saved. Carving obstacles (power shutters, toggle plugs) cut holes at runtime.");
    }
}
