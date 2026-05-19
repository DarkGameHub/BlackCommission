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
            int clients = NetworkManager.Singleton.ConnectedClientsIds.Count;
            GUI.Label(new Rect(10, 10, 300, 30),
                $"已联机 | {(NetworkManager.Singleton.IsHost ? "房主" : "客户端")} | 玩家: {clients}");
            return;
        }

        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(5, 5, 250, 160), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(15, 10, 230, 150));
        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14, fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };
        GUILayout.Label("点击下方按钮开始", titleStyle);
        GUILayout.Space(4);

        GUI.skin.button.fontSize = 16;
        if (GUILayout.Button("创建房间 (Start Host)", GUILayout.Height(40)))
            NetworkManager.Singleton.StartHost();
        GUILayout.Space(4);
        if (GUILayout.Button("加入房间 (Start Client)", GUILayout.Height(40)))
            NetworkManager.Singleton.StartClient();
        GUILayout.Space(4);
        var hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11, normal = { textColor = Color.gray }
        };
        GUILayout.Label("创建房间后走到桌上屏幕按E接单", hintStyle);
        GUILayout.EndArea();
    }
}
