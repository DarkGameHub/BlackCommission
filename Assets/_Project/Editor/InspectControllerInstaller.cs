using UnityEditor;
using UnityEngine;

/// <summary>
/// MVP wiring: ensures the Player prefab carries <see cref="InspectController"/> so relic
/// inspection (design/ux/item-inspection.md) is live in Play without hand-editing prefab YAML.
/// Runs on editor load, idempotent (adds the component once, skips if already present).
/// Remove this installer once InspectController is authored into Player.prefab properly.
/// </summary>
[InitializeOnLoad]
public static class InspectControllerInstaller
{
    const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player/Player.prefab";

    static InspectControllerInstaller()
    {
        // Run synchronously on load — this fires during the domain reload regardless of editor
        // focus. delayCall alone does NOT run while the editor is backgrounded/unfocused, which
        // left the prefab unwired. Keep a delayCall as a fallback. EnsureInstalled is idempotent,
        // so the double-invocation is harmless.
        TryInstall();
        EditorApplication.delayCall += TryInstall;
    }

    static void TryInstall()
    {
        try { EnsureInstalled(); }
        catch (System.Exception e) { Debug.LogWarning($"[InspectControllerInstaller] deferred: {e.Message}"); }
    }

    static void EnsureInstalled()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        if (prefab == null)
        {
            Debug.LogWarning($"[InspectControllerInstaller] Player prefab not found at {PlayerPrefabPath}");
            return;
        }
        if (prefab.GetComponent<InspectController>() != null)
            return; // already wired

        prefab.AddComponent<InspectController>();
        PrefabUtility.SavePrefabAsset(prefab);
        Debug.Log("[InspectControllerInstaller] Added InspectController to Player.prefab");
    }
}
