using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
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
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            // Shutdown NetworkManager regardless of IsListening state
            if (NetworkManager.Singleton != null)
                NetworkManager.Singleton.Shutdown();

            // Also directly stop the transport socket
            var transport = Object.FindObjectOfType<UnityTransport>();
            if (transport != null)
                transport.Shutdown();

            UnityEngine.Debug.Log("[Cleanup] Network shutdown — port released.");
        }
    }
}
