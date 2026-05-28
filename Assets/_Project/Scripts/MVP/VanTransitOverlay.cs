using Unity.Netcode;
using UnityEngine;

public class VanTransitOverlay : MonoBehaviour
{
    static VanTransitOverlay current;

    string taskTitle;
    string locationName;
    string directionLabel;
    float startTime;
    float duration;
    float hideAt;
    bool outbound;

    Texture2D ink;
    Texture2D blackout;
    Texture2D cabin;
    Texture2D steel;
    Texture2D rubber;
    Texture2D glass;
    Texture2D glassGlow;
    Texture2D paper;
    Texture2D paperDim;
    Texture2D tape;
    Texture2D amber;
    Texture2D red;
    Texture2D coldMark;
    Texture2D shadow;

    GUIStyle brandStyle;
    GUIStyle headingStyle;
    GUIStyle statusStyle;
    GUIStyle labelStyle;
    GUIStyle paperHeadingStyle;
    GUIStyle paperLabelStyle;
    GUIStyle paperSmallStyle;
    GUIStyle smallStyle;
    GUIStyle stampStyle;

    public static bool IsActive => current != null && Time.unscaledTime < current.hideAt;

    public static void ShowOutbound(string taskTitle, string locationName, float durationSeconds)
    {
        Show(taskTitle, locationName, "DISPATCHING", durationSeconds, true);
    }

    public static void ShowReturn(string taskTitle, string locationName, float durationSeconds)
    {
        Show(taskTitle, locationName, "RETURNING", durationSeconds, false);
    }

    static void Show(string taskTitle, string locationName, string directionLabel, float durationSeconds, bool outbound)
    {
        if (current == null)
        {
            var go = new GameObject("MVP_VanTransitOverlay");
            DontDestroyOnLoad(go);
            current = go.AddComponent<VanTransitOverlay>();
        }

        current.Begin(taskTitle, locationName, directionLabel, durationSeconds, outbound);
    }

    void Begin(string newTaskTitle, string newLocationName, string newDirectionLabel, float durationSeconds, bool isOutbound)
    {
        taskTitle = string.IsNullOrWhiteSpace(newTaskTitle) ? "委托" : newTaskTitle;
        locationName = string.IsNullOrWhiteSpace(newLocationName) ? "任务地点" : newLocationName;
        directionLabel = newDirectionLabel;
        duration = Mathf.Max(1.5f, durationSeconds);
        outbound = isOutbound;
        startTime = Time.unscaledTime;
        hideAt = startTime + duration + 0.65f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        EnsureTextures();
    }

    void Update()
    {
        if (Time.unscaledTime <= hideAt) return;
        if (current == this) current = null;
        Destroy(gameObject);
    }

    void OnGUI()
    {
        if (!IsActive) return;
        EnsureTextures();
        EnsureStyles();

        int oldDepth = GUI.depth;
        Color oldColor = GUI.color;
        GUI.depth = -1000;

        float elapsed = Time.unscaledTime - startTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        float fadeIn = Mathf.Clamp01(elapsed / 0.32f);
        float fadeOut = Mathf.Clamp01((hideAt - Time.unscaledTime) / 0.42f);
        float alpha = Mathf.Min(fadeIn, fadeOut);
        GUI.color = new Color(1f, 1f, 1f, alpha);

        DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), blackout);

        float width = Mathf.Clamp(Screen.width - 64f, 360f, 1080f);
        float height = Mathf.Clamp(Screen.height - 64f, 330f, 620f);
        Rect shell = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

        DrawTransitBoard(shell, elapsed, progress);
        DrawScreenWear(shell, elapsed);

        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }

    void DrawTransitBoard(Rect shell, float elapsed, float progress)
    {
        DrawRect(shell, ink);
        DrawRect(new Rect(shell.x + 8f, shell.y + 8f, shell.width - 16f, shell.height - 16f), steel);
        DrawRect(new Rect(shell.x + 18f, shell.y + 18f, shell.width - 36f, shell.height - 36f), cabin);

        Rect header = new Rect(shell.x + 34f, shell.y + 28f, shell.width - 68f, 54f);
        DrawHeader(header);

        float gutter = 22f;
        Rect content = new Rect(shell.x + 34f, header.yMax + 22f, shell.width - 68f, shell.height - 156f);
        float docketWidth = content.width > 620f
            ? Mathf.Clamp(content.width * 0.36f, 230f, 350f)
            : Mathf.Clamp(content.width * 0.46f, 160f, 230f);
        Rect docket = new Rect(content.x, content.y, docketWidth, content.height);
        Rect window = new Rect(docket.xMax + gutter, content.y, content.width - docket.width - gutter, content.height);

        DrawDocket(docket);
        DrawRouteWindow(window, elapsed);
        DrawBottomRoute(new Rect(shell.x + 34f, shell.yMax - 68f, shell.width - 68f, 42f), progress);
    }

    void DrawHeader(Rect header)
    {
        DrawRect(header, rubber);
        DrawRect(new Rect(header.x, header.yMax - 2f, header.width, 2f), outbound ? amber : red);
        GUI.Label(new Rect(header.x + 16f, header.y + 8f, 210f, 24f), "ACCIDENT SQUAD", brandStyle);
        GUI.Label(new Rect(header.x + 16f, header.y + 31f, 280f, 17f), "外包车队 / 临时派遣回执", smallStyle);

        string title = outbound ? "派车去现场" : "返程回事务所";
        GUI.Label(new Rect(header.center.x - 150f, header.y + 10f, 300f, 30f), title, headingStyle);
        GUI.Label(new Rect(header.xMax - 210f, header.y + 17f, 190f, 20f), $"车内人数 {GetPassengerCount()}/4", smallStyle);
    }

    void DrawDocket(Rect rect)
    {
        DrawRect(new Rect(rect.x + 8f, rect.y + 8f, rect.width, rect.height), shadow);
        DrawRect(rect, paper);
        DrawRect(new Rect(rect.x + 18f, rect.y - 7f, rect.width * 0.34f, 18f), tape);
        DrawRect(new Rect(rect.x + rect.width * 0.58f, rect.y - 6f, rect.width * 0.28f, 16f), tape);

        GUI.Label(new Rect(rect.x + 22f, rect.y + 20f, rect.width - 44f, 20f), outbound ? "外勤派车单" : "返程结算单", statusStyle);
        GUI.Label(new Rect(rect.x + 22f, rect.y + 48f, rect.width - 44f, 34f), outbound ? taskTitle : "现场回收单据", paperHeadingStyle);

        DrawDocketLine(rect, 96f, "地点", outbound ? locationName : "事故事务所");
        DrawDocketLine(rect, 134f, "路线", outbound ? $"事务所 -> {locationName}" : $"{locationName} -> 事务所");
        DrawDocketLine(rect, 172f, "车队", "二手外勤车 / 司机外包");
        DrawDocketLine(rect, 210f, "状态", outbound ? "已从车库出发" : "后舱关门，回站结算");

        Rect stamp = new Rect(rect.x + 22f, rect.yMax - 74f, rect.width - 44f, 42f);
        DrawRect(stamp, outbound ? coldMark : red);
        GUI.Label(stamp, directionLabel, stampStyle);
    }

    void DrawDocketLine(Rect rect, float y, string key, string value)
    {
        DrawRect(new Rect(rect.x + 22f, rect.y + y + 25f, rect.width - 44f, 1f), paperDim);
        GUI.Label(new Rect(rect.x + 22f, rect.y + y, 62f, 24f), key, paperSmallStyle);
        GUI.Label(new Rect(rect.x + 88f, rect.y + y, rect.width - 110f, 24f), value, paperLabelStyle);
    }

    void DrawRouteWindow(Rect rect, float elapsed)
    {
        DrawRect(rect, rubber);
        Rect glassRect = new Rect(rect.x + 12f, rect.y + 12f, rect.width - 24f, rect.height - 24f);
        DrawRect(glassRect, glass);
        DrawRect(new Rect(glassRect.x + 14f, glassRect.y + 14f, glassRect.width - 28f, 4f), glassGlow);
        DrawRect(new Rect(glassRect.x + 14f, glassRect.yMax - 18f, glassRect.width - 28f, 4f), shadow);

        DrawExteriorSilhouettes(glassRect, elapsed);

        Rect tag = new Rect(glassRect.x + 18f, glassRect.y + 22f, Mathf.Min(280f, glassRect.width - 36f), 38f);
        DrawRect(tag, shadow);
        GUI.Label(new Rect(tag.x + 12f, tag.y + 6f, tag.width - 24f, 22f),
            outbound ? "车窗外: 旧城边缘 / 废弃学校方向" : "车窗外: 回事务所交账", labelStyle);

        Rect mirror = new Rect(glassRect.xMax - 142f, glassRect.y + 24f, 112f, 38f);
        DrawRect(mirror, ink);
        DrawRect(new Rect(mirror.x + 8f, mirror.y + 8f, mirror.width - 16f, mirror.height - 16f), glassGlow);
        GUI.Label(new Rect(mirror.x, mirror.y + 8f, mirror.width, 18f), outbound ? "司机未回头" : "车尾已关", smallStyle);
    }

    void DrawExteriorSilhouettes(Rect rect, float elapsed)
    {
        float speed = outbound ? 86f : 68f;
        float roadY = rect.yMax - rect.height * 0.24f;
        DrawRect(new Rect(rect.x, roadY, rect.width, rect.height * 0.24f), ink);

        for (int i = 0; i < 7; i++)
        {
            float x = rect.x + Mathf.Repeat((elapsed * speed) + i * 155f, rect.width + 190f) - 170f;
            float h = 42f + (i % 3) * 26f;
            DrawRect(new Rect(x, roadY - h, 92f + i % 2 * 28f, h), shadow);
            if (i % 2 == 0)
                DrawRect(new Rect(x + 12f, roadY - h + 14f, 16f, 4f), amber);
        }

        if (!outbound)
        {
            DrawRect(new Rect(rect.x + rect.width * 0.58f, roadY - 86f, rect.width * 0.22f, 86f), shadow);
            DrawRect(new Rect(rect.x + rect.width * 0.62f, roadY - 118f, rect.width * 0.14f, 32f), shadow);
        }
        else
        {
            DrawRect(new Rect(rect.x + rect.width * 0.62f, roadY - 104f, rect.width * 0.25f, 104f), shadow);
            DrawRect(new Rect(rect.x + rect.width * 0.66f, roadY - 134f, rect.width * 0.17f, 30f), shadow);
            DrawRect(new Rect(rect.x + rect.width * 0.71f, roadY - 82f, 18f, 48f), ink);
        }

        DrawRect(new Rect(rect.x + 26f, roadY + 14f, rect.width - 52f, 3f), amber);
        DrawRect(new Rect(rect.x + 26f, roadY + 34f, rect.width - 52f, 2f), paperDim);
    }

    void DrawBottomRoute(Rect rect, float progress)
    {
        DrawRect(rect, ink);
        DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), steel);

        Rect route = new Rect(rect.x + 18f, rect.y + 8f, rect.width - 220f, 26f);
        GUI.Label(route, outbound ? $"事务所  ->  {locationName}" : $"{locationName}  ->  事务所", labelStyle);

        Rect bar = new Rect(rect.xMax - 188f, rect.y + 15f, 164f, 10f);
        DrawRect(bar, rubber);
        DrawRect(new Rect(bar.x, bar.y, bar.width * progress, bar.height), outbound ? amber : red);

        float markerX = Mathf.Lerp(bar.x + 4f, bar.xMax - 4f, progress);
        DrawRect(new Rect(markerX - 2f, bar.y - 5f, 4f, bar.height + 10f), paper);
    }

    void DrawScreenWear(Rect shell, float elapsed)
    {
        float offset = Mathf.Repeat(elapsed * 14f, 16f);
        for (float y = shell.y + 24f + offset; y < shell.yMax - 24f; y += 16f)
            DrawRect(new Rect(shell.x + 24f, y, shell.width - 48f, 1f), shadow);

        DrawRect(new Rect(shell.x, shell.y, shell.width, 10f), shadow);
        DrawRect(new Rect(shell.x, shell.yMax - 10f, shell.width, 10f), shadow);
    }

    void DrawRect(Rect rect, Texture2D texture)
    {
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
    }

    void EnsureTextures()
    {
        if (ink != null) return;

        ink = MakeTexture(new Color(0.014f, 0.016f, 0.015f, 0.98f));
        blackout = MakeTexture(new Color(0f, 0f, 0f, 0.92f));
        cabin = MakeTexture(new Color(0.055f, 0.064f, 0.058f, 0.98f));
        steel = MakeTexture(new Color(0.14f, 0.16f, 0.145f, 0.98f));
        rubber = MakeTexture(new Color(0.035f, 0.04f, 0.037f, 0.98f));
        glass = MakeTexture(new Color(0.035f, 0.055f, 0.06f, 0.98f));
        glassGlow = MakeTexture(new Color(0.23f, 0.31f, 0.3f, 0.48f));
        paper = MakeTexture(new Color(0.72f, 0.67f, 0.49f, 0.98f));
        paperDim = MakeTexture(new Color(0.44f, 0.39f, 0.27f, 0.72f));
        tape = MakeTexture(new Color(0.82f, 0.68f, 0.36f, 0.76f));
        amber = MakeTexture(new Color(0.78f, 0.54f, 0.2f, 0.95f));
        red = MakeTexture(new Color(0.48f, 0.11f, 0.09f, 0.95f));
        coldMark = MakeTexture(new Color(0.22f, 0.31f, 0.29f, 0.95f));
        shadow = MakeTexture(new Color(0f, 0f, 0f, 0.36f));
    }

    void EnsureStyles()
    {
        if (brandStyle != null) return;
        if (GUI.skin == null || GUI.skin.label == null) return;

        brandStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.84f, 0.78f, 0.61f) }
        };
        headingStyle = new GUIStyle(brandStyle)
        {
            fontSize = 19,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.92f, 0.88f, 0.72f) }
        };
        statusStyle = new GUIStyle(brandStyle)
        {
            fontSize = 15,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.18f, 0.15f, 0.1f) }
        };
        labelStyle = new GUIStyle(brandStyle)
        {
            fontSize = 14,
            fontStyle = FontStyle.Normal,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.86f, 0.86f, 0.78f) }
        };
        paperHeadingStyle = new GUIStyle(brandStyle)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.16f, 0.13f, 0.09f) }
        };
        paperLabelStyle = new GUIStyle(labelStyle)
        {
            normal = { textColor = new Color(0.18f, 0.15f, 0.1f) }
        };
        paperSmallStyle = new GUIStyle(labelStyle)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.34f, 0.28f, 0.18f) }
        };
        smallStyle = new GUIStyle(labelStyle)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.55f, 0.6f, 0.55f) }
        };
        stampStyle = new GUIStyle(brandStyle)
        {
            fontSize = 18,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.91f, 0.85f, 0.64f) }
        };
    }

    static Texture2D MakeTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    static int GetPassengerCount()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network != null && network.IsListening)
            return Mathf.Clamp(network.ConnectedClientsIds.Count, 1, 4);
        return 1;
    }
}
