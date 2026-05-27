using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class QuickNetworkUI : MonoBehaviour
{
    GUIStyle titleStyle;
    GUIStyle buttonStyle;
    GUIStyle hintStyle;
    GUIStyle statusStyle;
    GUIStyle fieldStyle;
    Texture2D bgTex;
    Texture2D screenTex;
    Texture2D fieldTex;
    Texture2D btnNormal;
    Texture2D btnHover;
    Texture2D btnActive;
    bool stylesReady;
    string connectAddress = "127.0.0.1";
    string connectPort = "7778";
    string networkMessage = "";
    ushort lastHostPort = 7778;

    void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        screenTex = MakeTex(1, 1, new Color(0.012f, 0.016f, 0.014f, 1f));
        bgTex = MakeTex(1, 1, new Color(0.035f, 0.045f, 0.04f, 0.96f));
        fieldTex = MakeTex(1, 1, new Color(0.012f, 0.018f, 0.016f, 1f));
        btnNormal = MakeTex(1, 1, new Color(0.05f, 0.42f, 0.22f, 1f));
        btnHover = MakeTex(1, 1, new Color(0.09f, 0.55f, 0.30f, 1f));
        btnActive = MakeTex(1, 1, new Color(0.03f, 0.28f, 0.16f, 1f));


        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.58f, 1f, 0.72f) },
            padding = new RectOffset(0, 0, 8, 4)
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = btnNormal, textColor = Color.white },
            hover = { background = btnHover, textColor = Color.white },
            active = { background = btnActive, textColor = new Color(0.8f, 0.8f, 0.8f) },
            border = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(12, 12, 8, 8)
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(0.62f, 0.70f, 0.64f) },
            padding = new RectOffset(8, 8, 4, 4)
        };

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.52f, 1f, 0.68f) },
            padding = new RectOffset(10, 10, 6, 6)
        };

        fieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            normal = { background = fieldTex, textColor = new Color(0.78f, 0.9f, 0.78f) },
            focused = { background = fieldTex, textColor = Color.white },
            padding = new RectOffset(8, 8, 5, 5)
        };
    }

    static Texture2D MakeTex(int w, int h, Color col)
    {
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        var tex = new Texture2D(w, h);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;
        InitStyles();

        if (NetworkManager.Singleton.IsListening)
        {
            int clients = NetworkManager.Singleton.ConnectedClientsIds.Count;
            string role = NetworkManager.Singleton.IsHost ? "房主" : "客户端";
            string portText = NetworkManager.Singleton.IsHost ? $"  端口 {lastHostPort}" : "";
            GUI.Label(new Rect(8, 8, 320, 28),
                $"已联机  {role}  玩家 {clients}  {portText}", statusStyle);
            return;
        }

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), screenTex);

        float pw = 360, ph = 280;
        float px = (Screen.width - pw) / 2f;
        float py = (Screen.height - ph) / 2f;

        GUI.DrawTexture(new Rect(px, py, pw, ph), bgTex);

        float cx = px + 20;
        float cy = py + 10;
        float bw = pw - 40;

        GUI.Label(new Rect(cx, cy, bw, 36), "Accident Squad", titleStyle);
        cy += 42;

        GUI.Label(new Rect(cx, cy, bw, 20), "事务所连接终端", hintStyle);
        cy += 24;
        connectAddress = GUI.TextField(new Rect(cx, cy, bw - 86, 30), connectAddress, fieldStyle);
        connectPort = GUI.TextField(new Rect(cx + bw - 78, cy, 78, 30), connectPort, fieldStyle);
        cy += 40;

        if (GUI.Button(new Rect(cx, cy, bw, 42), "创建事务所", buttonStyle))
            StartHostWithFreshPort();
        cy += 52;

        if (GUI.Button(new Rect(cx, cy, bw, 42), "接入事务所", buttonStyle))
            StartClientWithEndpoint();
        cy += 52;

        string hint = string.IsNullOrEmpty(networkMessage)
            ? "创建后进入事务所，通过办公室电脑查看委托。"
            : networkMessage;
        GUI.Label(new Rect(cx, cy, bw, 42), hint, hintStyle);
    }

    void StartHostWithFreshPort()
    {
        if (!NetworkManager.Singleton.TryGetComponent<UnityTransport>(out var transport))
        {
            bool startedWithoutTransport = NetworkManager.Singleton.StartHost();
            networkMessage = startedWithoutTransport ? "主机已启动。" : "创建主机失败: 缺少 UnityTransport。";
            return;
        }

        ushort nextBasePort = 7778;
        for (int attempt = 0; attempt < 4; attempt++)
        {
            ushort port = AutoPort.AssignFreePort(transport, nextBasePort);
            lastHostPort = port;
            connectPort = port.ToString();

            if (NetworkManager.Singleton.StartHost())
            {
                connectAddress = "127.0.0.1";
                networkMessage = $"主机已启动: {connectAddress}:{port}";
                return;
            }

            NetworkManager.Singleton.Shutdown();
            nextBasePort = (ushort)Mathf.Min(ushort.MaxValue - 1, port + 1);
        }

        networkMessage = "创建主机失败: 端口仍被占用，请关闭旧 Play/Build 后重试。";
    }

    void StartClientWithEndpoint()
    {
        if (!ushort.TryParse(connectPort, out ushort port))
        {
            networkMessage = "加入失败: 端口必须是 0-65535 的数字。";
            return;
        }

        if (NetworkManager.Singleton.TryGetComponent<UnityTransport>(out var transport))
            transport.SetConnectionData(connectAddress, port);

        bool started = NetworkManager.Singleton.StartClient();
        networkMessage = started ? $"正在加入 {connectAddress}:{port}..." : "加入失败: NetworkManager 未能启动客户端。";
    }
}
