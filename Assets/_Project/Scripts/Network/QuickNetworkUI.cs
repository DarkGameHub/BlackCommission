using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Simple in-game Start Host / Start Client buttons for local testing.
/// Remove this before shipping.
/// </summary>
public class QuickNetworkUI : MonoBehaviour
{
    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;
        if (NetworkManager.Singleton.IsListening)
        {
            GUI.Label(new Rect(10, 10, 200, 30), $"Connected (IsHost={NetworkManager.Singleton.IsHost})");
            return;
        }

        GUILayout.BeginArea(new Rect(10, 10, 220, 110));
        GUI.skin.button.fontSize = 16;
        if (GUILayout.Button("Start Host", GUILayout.Height(45)))
            NetworkManager.Singleton.StartHost();
        if (GUILayout.Button("Start Client", GUILayout.Height(45)))
            NetworkManager.Singleton.StartClient();
        GUILayout.EndArea();
    }
}
