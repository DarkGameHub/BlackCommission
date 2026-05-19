using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisconnectHandler : MonoBehaviour
{
    float returnTimer = -1f;
    bool showingUI;
    string disconnectMessage = "";
    GUIStyle messageStyle;

    void OnEnable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
    }

    void OnDisable()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    void OnClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[Disconnect] 客户端 {clientId} 断开连接");
            return;
        }

        if (clientId == NetworkManager.Singleton.LocalClientId ||
            clientId == NetworkManager.ServerClientId)
        {
            disconnectMessage = "主机已断开连接，即将返回...";
            showingUI = true;
            returnTimer = 3f;
        }
    }

    void Update()
    {
        if (returnTimer < 0) return;

        returnTimer -= Time.deltaTime;
        if (returnTimer <= 0)
        {
            showingUI = false;
            NetworkManager.Singleton?.Shutdown();
            SceneManager.LoadScene("HQ");
        }
    }

    void OnGUI()
    {
        if (!showingUI) return;

        if (messageStyle == null)
        {
            messageStyle = new GUIStyle(GUI.skin.box)
            {
                font = UIFont.Get(), fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.red }
            };
        }

        float w = 500, h = 80;
        GUI.Box(new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h),
            disconnectMessage, messageStyle);
    }
}
