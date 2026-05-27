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

        // Let Netcode's own OnApplicationQuit/OnDestroy path perform shutdown.
        // Calling Shutdown() here can dispose the scene manager, then Unity calls
        // OnApplicationQuit and Netcode disposes it a second time.
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            UnityEngine.Debug.Log("[Cleanup] Play mode exiting; Netcode will release the port during its own shutdown.");
    }
}
