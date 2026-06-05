using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Forces the editor Play button to always boot the HQ scene (which owns the main menu and
/// the office), regardless of which scene is currently open. Without this, pressing Play
/// while a mission scene is open drops you straight into that scene
/// with no menu — the editor plays the OPEN scene, not build-settings index 0.
///
/// This is intentionally NOT a persistent toggle (an earlier toggle version got stuck OFF).
/// It always re-applies on every compile/domain reload. If you want to test the open scene
/// in isolation just once, use Tools > Black Commission > MVP > Play Current Scene Once —
/// it clears the override for the next Play only, then HQ-start comes back on the next reload.
/// </summary>
[InitializeOnLoad]
public static class MvpPlayModeStartScene
{
    const string HqScenePath = "Assets/_Project/Scenes/HQ.unity";

    static MvpPlayModeStartScene()
    {
        Apply();                               // set immediately on domain reload
        EditorApplication.delayCall += Apply;  // and again once the AssetDatabase is fully ready
    }

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

    // Run the currently-open scene for the next Play only; HQ-start returns on the next reload.
    [MenuItem("Tools/Black Commission/MVP/Play Current Scene Once")]
    static void PlayCurrentOnce()
    {
        EditorSceneManager.playModeStartScene = null;
        Debug.Log("[MVP] 本次 Play 跑当前打开的场景；下次重新编译/重载后会恢复从 HQ 启动。");
    }
}
