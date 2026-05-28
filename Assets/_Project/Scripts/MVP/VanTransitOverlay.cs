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

    Texture2D black;
    Texture2D panel;
    Texture2D window;
    Texture2D amber;
    Texture2D green;
    Texture2D red;
    Texture2D paper;
    GUIStyle titleStyle;
    GUIStyle labelStyle;
    GUIStyle mutedStyle;

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
        EnsureGui();
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
        EnsureGui();

        int oldDepth = GUI.depth;
        GUI.depth = -1000;
        float elapsed = Time.unscaledTime - startTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        float fadeIn = Mathf.Clamp01(elapsed / 0.35f);
        float fadeOut = Mathf.Clamp01((hideAt - Time.unscaledTime) / 0.45f);
        float alpha = Mathf.Min(fadeIn, fadeOut);

        Color oldColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, alpha);

        Rect screen = new Rect(0, 0, Screen.width, Screen.height);
        GUI.DrawTexture(screen, black, ScaleMode.StretchToFill);

        float vanWidth = Mathf.Clamp(Screen.width - 64f, 320f, 980f);
        float vanHeight = Mathf.Clamp(Screen.height - 76f, 260f, 520f);
        Rect van = new Rect((Screen.width - vanWidth) * 0.5f, (Screen.height - vanHeight) * 0.5f, vanWidth, vanHeight);
        GUI.DrawTexture(van, panel, ScaleMode.StretchToFill);

        DrawWindow(new Rect(van.x + 42f, van.y + 72f, van.width * 0.38f, van.height * 0.42f), elapsed, 0f);
        DrawWindow(new Rect(van.x + van.width * 0.58f, van.y + 72f, van.width * 0.36f, van.height * 0.42f), elapsed, 0.37f);
        DrawDriver(van);
        DrawRearSeats(van);
        DrawDispatchPlate(van, progress);

        GUI.color = oldColor;
        GUI.depth = oldDepth;
    }

    void DrawWindow(Rect rect, float elapsed, float phase)
    {
        GUI.DrawTexture(rect, window, ScaleMode.StretchToFill);

        float speed = outbound ? 250f : 210f;
        for (int i = 0; i < 8; i++)
        {
            float x = rect.x + Mathf.Repeat((elapsed + phase) * speed + i * 145f, rect.width + 190f) - 160f;
            Rect strip = new Rect(x, rect.y + 18f + (i % 3) * 34f, 86f + (i % 2) * 48f, 12f + (i % 2) * 10f);
            GUI.DrawTexture(strip, i % 3 == 0 ? amber : paper, ScaleMode.StretchToFill);
        }

        for (int i = 0; i < 5; i++)
        {
            float x = rect.x + Mathf.Repeat((elapsed + phase) * (speed * 0.55f) + i * 210f, rect.width + 240f) - 200f;
            Rect building = new Rect(x, rect.y + rect.height - 72f, 120f, 54f);
            GUI.DrawTexture(building, black, ScaleMode.StretchToFill);
        }
    }

    void DrawDriver(Rect van)
    {
        Rect seat = new Rect(van.x + van.width * 0.44f, van.y + 64f, van.width * 0.12f, van.height * 0.34f);
        GUI.DrawTexture(seat, black, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(seat.x + seat.width * 0.3f, seat.y - 18f, seat.width * 0.38f, seat.width * 0.38f), amber, ScaleMode.StretchToFill);
        GUI.Label(new Rect(seat.x - 24f, seat.y + seat.height + 8f, seat.width + 48f, 24f), "外包司机", mutedStyle);
    }

    void DrawRearSeats(Rect van)
    {
        float seatW = van.width * 0.13f;
        float seatH = 82f;
        float startX = van.x + van.width * 0.20f;
        float y = van.y + van.height - 158f;
        for (int i = 0; i < 4; i++)
        {
            Rect seat = new Rect(startX + i * (seatW + 18f), y + (i % 2) * 14f, seatW, seatH);
            GUI.DrawTexture(seat, black, ScaleMode.StretchToFill);
            GUI.Label(new Rect(seat.x, seat.y + seat.height + 8f, seat.width, 22f), $"后座 {i + 1}", mutedStyle);
        }
    }

    void DrawDispatchPlate(Rect van, float progress)
    {
        Rect plate = new Rect(van.x + 42f, van.y + van.height - 72f, van.width - 84f, 44f);
        GUI.DrawTexture(plate, black, ScaleMode.StretchToFill);

        float barWidth = Mathf.Clamp(plate.width * 0.22f, 84f, 126f);
        Rect barBg = new Rect(plate.x + plate.width - barWidth - 24f, plate.y + 15f, barWidth, 10f);
        float titleWidth = Mathf.Clamp(plate.width * 0.32f, 88f, 180f);
        Rect titleRect = new Rect(plate.x + 16f, plate.y + 8f, titleWidth, 28f);
        GUI.Label(titleRect, directionLabel, titleStyle);

        float routeX = titleRect.xMax + 10f;
        float routeWidth = barBg.x - routeX - 12f;
        if (routeWidth > 48f)
        {
            string route = outbound ? $"{taskTitle}  ->  {locationName} 门口" : $"{locationName}  ->  事务所结算";
            GUI.Label(new Rect(routeX, plate.y + 9f, routeWidth, 24f), route, labelStyle);
        }

        GUI.DrawTexture(barBg, paper, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(barBg.x, barBg.y, barBg.width * progress, barBg.height), outbound ? green : red, ScaleMode.StretchToFill);
    }

    void EnsureGui()
    {
        if (black != null) return;

        black = MakeTexture(new Color(0.02f, 0.024f, 0.022f, 0.98f));
        panel = MakeTexture(new Color(0.08f, 0.12f, 0.11f, 0.96f));
        window = MakeTexture(new Color(0.055f, 0.09f, 0.09f, 0.98f));
        amber = MakeTexture(new Color(0.85f, 0.6f, 0.19f, 0.92f));
        green = MakeTexture(new Color(0.48f, 0.81f, 0.54f, 0.92f));
        red = MakeTexture(new Color(0.76f, 0.23f, 0.17f, 0.95f));
        paper = MakeTexture(new Color(0.84f, 0.78f, 0.61f, 0.86f));

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.84f, 0.78f, 0.61f) }
        };
        labelStyle = new GUIStyle(titleStyle)
        {
            fontSize = 15,
            fontStyle = FontStyle.Normal,
            normal = { textColor = new Color(0.88f, 0.9f, 0.84f) }
        };
        mutedStyle = new GUIStyle(labelStyle)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.55f, 0.62f, 0.58f) }
        };
    }

    static Texture2D MakeTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
