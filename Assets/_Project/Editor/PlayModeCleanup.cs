using System.Net.Sockets;
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
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            AssignFreePort();
            return;
        }

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

    // Called at the start of every play session. Finds the first free UDP port
    // starting from 7778 and updates the scene's UnityTransport before the host binds.
    static void AssignFreePort()
    {
        var networkManager = Object.FindFirstObjectByType<NetworkManager>();
        if (networkManager == null) return;

        var transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null) return;

        ushort port = FindFreeUdpPort(7778);
        transport.SetConnectionData("127.0.0.1", port);
        UnityEngine.Debug.Log($"[Cleanup] UTP bound to port {port}.");
    }

    static ushort FindFreeUdpPort(ushort startPort)
    {
        for (ushort port = startPort; port < startPort + 50; port++)
        {
            try
            {
                using var udp = new UdpClient(port);
                return port;
            }
            catch { }
        }
        return startPort;
    }
}
