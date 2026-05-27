using Unity.Netcode;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class PlayModeCleanup
{
    static PlayModeCleanup()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingPlayMode) return;

        try
        {
            // NetworkManager.Shutdown() already tears down UTP internally.
            // Calling transport.Shutdown() separately triggers a second round of
            // disconnect callbacks after UTP's internal state is gone → NullReferenceException.
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }
        catch (System.Exception e)
        {
            // UTP known bug: GetDisconnectEventMessage NRE during shutdown — safe to swallow.
            UnityEngine.Debug.Log($"[Cleanup] Network shutdown (UTP internal exception suppressed): {e.Message}");
        }

        UnityEngine.Debug.Log("[Cleanup] Network shutdown — port released.");
    }
}
