using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Previously forced the editor Play button to always boot the HQ scene.
/// Disabled so Play Mode starts from the currently open scene.
/// </summary>
[InitializeOnLoad]
public static class MvpPlayModeStartScene
{
    const string HqScenePath = "Assets/_Project/Scenes/HQ.unity";

    static MvpPlayModeStartScene()
    {
        // Apply();                               // set immediately on domain reload
        // EditorApplication.delayCall += Apply;  // and again once the AssetDatabase is fully ready
        EditorApplication.delayCall += ClearHqOverride;
    }

    /*
    static void Apply()
    {
        var hq = AssetDatabase.LoadAssetAtPath<SceneAsset>(HqScenePath);
        if (hq == null) return;   // AssetDatabase may not be ready on the first call; delayCall retries

        if (EditorSceneManager.playModeStartScene != hq)
        {
            EditorSceneManager.playModeStartScene = hq;
            Debug.Log("[MVP] Play 会先从事务所 (HQ) 启动。要只跑当前场景一次: Tools > Black Commission > MVP > Play Current Scene Once。");
        }
    }
    */

    static void ClearHqOverride()
    {
        var hq = AssetDatabase.LoadAssetAtPath<SceneAsset>(HqScenePath);
        if (hq != null && EditorSceneManager.playModeStartScene == hq)
        {
            EditorSceneManager.playModeStartScene = null;
            Debug.Log("[MVP] 已关闭自动从 HQ 启动；Play 会运行当前打开的场景。");
        }
    }

    // Run the currently-open scene.
    [MenuItem("Tools/Black Commission/MVP/Play Current Scene Once")]
    static void PlayCurrentOnce()
    {
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("[MVP] Play 会运行当前打开的场景。");
    }
}
