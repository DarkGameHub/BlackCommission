using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// QoL toggle: when enabled, pressing Play ALWAYS boots from the HQ scene (main
/// menu + full game flow) no matter which scene is open in the editor — and on
/// stop you land back in the scene you were editing. Unity otherwise plays the
/// open scene, which made tower-editing sessions confusing ("Play 只有塔楼").
/// Menu: Tools > Black Commission > Play From HQ (toggle, persisted per-user).
/// </summary>
[InitializeOnLoad]
public static class PlayFromHqTool
{
    const string MenuPath = "Tools/Black Commission/Play From HQ";
    const string PrefKey = "BC.PlayFromHq";
    const string HqScenePath = "Assets/_Project/Scenes/HQ.unity";

    static PlayFromHqTool()
    {
        // Re-apply on every domain reload so the setting survives recompiles.
        EditorApplication.delayCall += Apply;
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        EditorPrefs.SetBool(PrefKey, !EditorPrefs.GetBool(PrefKey, false));
        Apply();
        Debug.Log($"[PlayFromHQ] {(EditorPrefs.GetBool(PrefKey, false) ? "ON — Play 永远从 HQ（主菜单）启动" : "OFF — Play 运行当前打开的场景")}");
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, EditorPrefs.GetBool(PrefKey, false));
        return true;
    }

    static void Apply()
    {
        if (EditorPrefs.GetBool(PrefKey, false))
        {
            var hq = AssetDatabase.LoadAssetAtPath<SceneAsset>(HqScenePath);
            if (hq == null)
            {
                Debug.LogWarning($"[PlayFromHQ] HQ scene not found at {HqScenePath} — toggle ignored.");
                return;
            }
            EditorSceneManager.playModeStartScene = hq;
        }
        else
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }
}
