using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click setup for walking the abandoned-tower blockout: removes the leftover placeholder
/// objects (Door / Cone / Plane), quiets any stray cameras/audio listeners, and drops a
/// <see cref="PreviewWalker"/> at the building entrance so you can press Play and feel the space.
/// Re-runnable (it clears its own previous walker first). The networked PlayerController is NOT
/// used here — see PreviewWalker for why.
/// </summary>
public static class BlockoutPreviewTool
{
    const string ScenePath = "Assets/Scene/AbandonedBuilding_Blockout.unity";

    // Entrance: a few metres south of the van/lobby door (x15, z0), facing north into the building.
    static readonly Vector3 EntrancePos = new Vector3(15f, 0.2f, -3f);

    [MenuItem("Tools/Black Commission/MVP/Tower/Setup Blockout Walkthrough")]
    public static void Setup()
    {
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 1) Delete the explicit placeholders (and any previous walker so re-running is clean).
        int deleted = DestroyAllNamed("Door") + DestroyAllNamed("Cone") + DestroyAllNamed("Plane")
                      + DestroyAllNamed("PreviewWalker");

        // 2) Deactivate other leftover experiments rather than delete (you didn't name these).
        int deactivated = DeactivateAllNamed("player") + DeactivateAllNamed("pb_Mesh");

        // 3) Quiet every existing camera + audio listener so Play has exactly one of each (ours).
        foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            c.enabled = false;
        foreach (var l in Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None))
            l.enabled = false;

        // 4) Build the preview walker at the entrance.
        var walker = new GameObject("PreviewWalker");
        walker.transform.SetPositionAndRotation(EntrancePos, Quaternion.identity);
        var cc = walker.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        var camGo = new GameObject("PreviewCamera");
        camGo.transform.SetParent(walker.transform);
        camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        camGo.tag = "MainCamera";
        camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();

        var walk = walker.AddComponent<PreviewWalker>();
        var so = new SerializedObject(walk);
        var camProp = so.FindProperty("cam");
        if (camProp != null)
        {
            camProp.objectReferenceValue = camGo.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Walkthrough] Deleted {deleted} placeholder object(s); deactivated {deactivated} " +
                  $"leftover(s). PreviewWalker placed at {EntrancePos}. Press Play — WASD move, " +
                  "mouse look, Shift sprint, Space jump, Esc toggles the cursor.");
    }

    static int DestroyAllNamed(string name)
    {
        int n = 0;
        for (var go = GameObject.Find(name); go != null; go = GameObject.Find(name))
        {
            Object.DestroyImmediate(go);
            n++;
        }
        return n;
    }

    static int DeactivateAllNamed(string name)
    {
        int n = 0;
        for (var go = GameObject.Find(name); go != null; go = GameObject.Find(name))
        {
            go.SetActive(false);   // SetActive(false) so Find() skips it next iteration
            n++;
        }
        return n;
    }
}
