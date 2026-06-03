using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DisconnectHandler : MonoBehaviour
{
    // Persist across scene loads so a host-drop DURING a mission still routes clients back
    // to HQ. Scenes may each carry a copy; the first one wins and later duplicates remove
    // themselves. Subscription is lazy because NetworkManager may not exist yet at Awake.
    static DisconnectHandler instance;

    float returnTimer = -1f;
    bool showingUI;
    string disconnectMessage = "";
    GUIStyle messageStyle;
    GUIStyle toastStyle;

    NetworkManager subscribedNetwork;

    // Transient "teammate joined / left" toasts, shown on every peer.
    readonly List<(string text, float until)> toasts = new();

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        Unsubscribe();
        if (instance == this) instance = null;
    }

    // Subscribe once NetworkManager is available; re-subscribe if the instance is replaced
    // (host shutdown + new session within the same process).
    void EnsureSubscribed()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (subscribedNetwork == network) return;

        Unsubscribe();
        if (network == null) return;

        network.OnClientDisconnectCallback += OnClientDisconnect;
        network.OnConnectionEvent += OnConnectionEvent;
        subscribedNetwork = network;
    }

    void Unsubscribe()
    {
        if (subscribedNetwork == null) return;
        subscribedNetwork.OnClientDisconnectCallback -= OnClientDisconnect;
        subscribedNetwork.OnConnectionEvent -= OnConnectionEvent;
        subscribedNetwork = null;
    }

    void OnConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        // Fires on every peer: announce other players coming and going.
        switch (data.EventType)
        {
            case ConnectionEvent.PeerConnected:
                AddToast(MvpLocale.T("player_joined"));
                break;
            case ConnectionEvent.PeerDisconnected:
                AddToast(MvpLocale.T("player_left"));
                break;
        }
    }

    void AddToast(string text)
    {
        toasts.Add((text, Time.unscaledTime + 4f));
        if (toasts.Count > 6) toasts.RemoveAt(0);
    }

    void OnClientDisconnect(ulong clientId)
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[Disconnect] 客户端 {clientId} 断开连接");
            return;
        }

        // On a client, a disconnect of ourselves or the server means the session is over.
        if (clientId == NetworkManager.Singleton.LocalClientId ||
            clientId == NetworkManager.ServerClientId)
        {
            // If the host kicked us it provides a reason; otherwise the host just dropped.
            string reason = NetworkManager.Singleton.DisconnectReason;
            disconnectMessage = !string.IsNullOrEmpty(reason) ? reason : MvpLocale.T("host_disconnected");
            showingUI = true;
            returnTimer = 3f;
        }
    }

    void Update()
    {
        EnsureSubscribed();

        if (returnTimer < 0) return;

        returnTimer -= Time.deltaTime;
        if (returnTimer <= 0)
        {
            showingUI = false;
            NetworkManager.Singleton?.Shutdown();
            // We may have been a guest mirroring the host's company; restore our own save.
            CompanyData.ReloadFromDisk();
            SceneManager.LoadScene("HQ");
        }
    }

    void OnGUI()
    {
        EnsureStyles();
        DrawToasts();

        if (!showingUI) return;

        float w = 500, h = 80;
        GUI.Box(new Rect((Screen.width - w) / 2f, (Screen.height - h) / 2f, w, h),
            disconnectMessage, messageStyle);
    }

    void DrawToasts()
    {
        if (toasts.Count == 0) return;

        float now = Time.unscaledTime;
        for (int i = toasts.Count - 1; i >= 0; i--)
            if (toasts[i].until <= now) toasts.RemoveAt(i);

        float y = 88f;
        foreach (var (text, _) in toasts)
        {
            GUI.Label(new Rect(0, y, Screen.width, 24), text, toastStyle);
            y += 26f;
        }
    }

    void EnsureStyles()
    {
        if (messageStyle == null)
        {
            messageStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.red }
            };
        }

        if (toastStyle == null)
        {
            toastStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 15,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.86f, 0.83f, 0.70f) }
            };
        }
    }
}
