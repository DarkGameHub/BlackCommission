using Unity.Netcode;
using UnityEngine;

public class QuickNetworkUI : MonoBehaviour
{
    GUIStyle titleStyle;
    GUIStyle buttonStyle;
    GUIStyle hintStyle;
    GUIStyle statusStyle;
    Texture2D bgTex;
    Texture2D btnNormal;
    Texture2D btnHover;
    Texture2D btnActive;
    bool stylesReady;

    void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        bgTex = MakeTex(1, 1, new Color(0.08f, 0.08f, 0.12f, 0.92f));
        btnNormal = MakeTex(1, 1, new Color(0.18f, 0.45f, 0.72f, 1f));
        btnHover = MakeTex(1, 1, new Color(0.25f, 0.55f, 0.82f, 1f));
        btnActive = MakeTex(1, 1, new Color(0.12f, 0.35f, 0.6f, 1f));

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.95f, 0.85f, 0.4f) },
            padding = new RectOffset(0, 0, 8, 4)
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = btnNormal, textColor = Color.white },
            hover = { background = btnHover, textColor = Color.white },
            active = { background = btnActive, textColor = new Color(0.8f, 0.8f, 0.8f) },
            border = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(12, 12, 8, 8)
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = new Color(0.6f, 0.6f, 0.65f) },
            padding = new RectOffset(8, 8, 4, 4)
        };

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.5f, 0.9f, 0.5f) },
            padding = new RectOffset(10, 10, 6, 6)
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
            GUI.Label(new Rect(8, 8, 320, 28),
                $"[已联机]  {role}  |  玩家: {clients}", statusStyle);
            return;
        }

        float pw = 280, ph = 200;
        float px = (Screen.width - pw) / 2f;
        float py = (Screen.height - ph) / 2f;

        GUI.DrawTexture(new Rect(px, py, pw, ph), bgTex);

        float cx = px + 20;
        float cy = py + 10;
        float bw = pw - 40;

        GUI.Label(new Rect(cx, cy, bw, 36), "外包事故组", titleStyle);
        cy += 42;

        if (GUI.Button(new Rect(cx, cy, bw, 42), "创建房间", buttonStyle))
            NetworkManager.Singleton.StartHost();
        cy += 52;

        if (GUI.Button(new Rect(cx, cy, bw, 42), "加入房间", buttonStyle))
            NetworkManager.Singleton.StartClient();
        cy += 52;

        GUI.Label(new Rect(cx, cy, bw, 36), "创建房间后走到桌上屏幕按 E 接单", hintStyle);
    }
}
