using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Uniformly scales the hand-authored HQ scene about the world origin (floor plane
/// stays at y≈0, all relative layout preserved). Needed because the HQ was authored
/// against the old 0.7 m camera-height bug: with the correct 1.7 m eye the 2.72 m
/// ceiling / 2.2 m garage opening read cramped (PM 2026-06-11: 整体拉大事务所).
///
/// Scales: every root's localPosition + localScale (so props grow and stay in
/// place), plus Light.range on all point/spot lights (range ignores transform
/// scale). Skips RectTransform roots (screen-space UI) and infrastructure objects.
/// Does NOT save — inspect the result, then Ctrl+S to keep or Ctrl+Z to revert.
/// </summary>
public static class HqScaleTool
{
    const string ScenePath = "Assets/_Project/Scenes/HQ.unity";

    static readonly string[] SkipNames =
    {
        "NetworkManager", "EventSystem", "AudioManager", "AudioManager (Auto)",
        "MainMenu", "Directional Light",
    };

    [MenuItem("Tools/Black Commission/MVP/HQ/Scale HQ Up x1.25")]
    static void ScaleUp() => Apply(1.25f);

    [MenuItem("Tools/Black Commission/MVP/HQ/Scale HQ Up x1.1")]
    static void ScaleUpSmall() => Apply(1.1f);

    [MenuItem("Tools/Black Commission/MVP/HQ/Scale HQ Down x0.8 (undo x1.25)")]
    static void ScaleDown() => Apply(0.8f);

    static void Apply(float k)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "HQ")
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        int scaled = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root.transform is RectTransform) continue;             // screen-space UI
            if (System.Array.IndexOf(SkipNames, root.name) >= 0) continue;

            Undo.RecordObject(root.transform, "Scale HQ");
            root.transform.localPosition *= k;
            root.transform.localScale *= k;
            scaled++;
        }

        // Light.range is independent of transform scale — keep light pools proportional.
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional) continue;
            Undo.RecordObject(light, "Scale HQ");
            light.range *= k;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log($"[HqScale] Scaled {scaled} roots by x{k} (lights' ranges too). " +
                  "Walk it in Play mode; Ctrl+S to keep, Ctrl+Z (or the Down menu) to revert.");
    }
}
