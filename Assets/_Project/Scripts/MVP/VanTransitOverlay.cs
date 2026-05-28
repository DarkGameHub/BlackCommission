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

    Texture2D black;
    Texture2D panel;
    Texture2D window;
    Texture2D floor;
    Texture2D ceiling;
    Texture2D bench;
    Texture2D skin;
    Texture2D coat;
    Texture2D shadow;
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

        float vanWidth = Mathf.Clamp(Screen.width - 56f, 340f, 1080f);
        float vanHeight = Mathf.Clamp(Screen.height - 64f, 280f, 600f);
        Rect van = new Rect((Screen.width - vanWidth) * 0.5f, (Screen.height - vanHeight) * 0.5f, vanWidth, vanHeight);
        DrawCabin(van, elapsed);
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

    void DrawCabin(Rect van, float elapsed)
    {
        GUI.DrawTexture(van, panel, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(van.x + 20f, van.y + 20f, van.width - 40f, 48f), ceiling, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(van.x + 30f, van.y + van.height * 0.63f, van.width - 60f, van.height * 0.22f), floor, ScaleMode.StretchToFill);

        DrawWindow(new Rect(van.x + 42f, van.y + 84f, van.width * 0.25f, van.height * 0.34f), elapsed, 0f);
        DrawWindow(new Rect(van.x + van.width * 0.73f, van.y + 84f, van.width * 0.22f, van.height * 0.34f), elapsed, 0.37f);
        DrawDriver(van);
        DrawFacingPassengerSeats(van);

        GUI.DrawTexture(new Rect(van.x + van.width * 0.48f, van.y + 80f, van.width * 0.04f, van.height * 0.48f),
            shadow, ScaleMode.StretchToFill);
        GUI.Label(new Rect(van.x + van.width * 0.36f, van.y + 28f, van.width * 0.28f, 26f),
            outbound ? "事故外勤车厢" : "返程车厢", titleStyle);
    }

    void DrawDriver(Rect van)
    {
        Rect partition = new Rect(van.x + van.width * 0.35f, van.y + 78f, van.width * 0.3f, van.height * 0.26f);
        GUI.DrawTexture(partition, shadow, ScaleMode.StretchToFill);
        Rect head = new Rect(partition.center.x - 16f, partition.y + 18f, 32f, 32f);
        GUI.DrawTexture(head, amber, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(partition.center.x - 28f, partition.y + 50f, 56f, 60f), coat, ScaleMode.StretchToFill);
        GUI.Label(new Rect(partition.x, partition.yMax + 6f, partition.width, 22f), "外包司机", mutedStyle);
    }

    void DrawFacingPassengerSeats(Rect van)
    {
        int passengerCount = GetPassengerCount();
        Rect leftBench = new Rect(van.x + van.width * 0.15f, van.y + van.height * 0.42f, van.width * 0.23f, van.height * 0.28f);
        Rect rightBench = new Rect(van.x + van.width * 0.62f, van.y + van.height * 0.42f, van.width * 0.23f, van.height * 0.28f);
        GUI.DrawTexture(leftBench, bench, ScaleMode.StretchToFill);
        GUI.DrawTexture(rightBench, bench, ScaleMode.StretchToFill);

        DrawPassenger(new Rect(leftBench.x + leftBench.width * 0.08f, leftBench.y - 20f, leftBench.width * 0.34f, leftBench.height + 12f),
            0, passengerCount, false);
        DrawPassenger(new Rect(leftBench.x + leftBench.width * 0.56f, leftBench.y - 20f, leftBench.width * 0.34f, leftBench.height + 12f),
            1, passengerCount, false);
        DrawPassenger(new Rect(rightBench.x + rightBench.width * 0.08f, rightBench.y - 20f, rightBench.width * 0.34f, rightBench.height + 12f),
            2, passengerCount, true);
        DrawPassenger(new Rect(rightBench.x + rightBench.width * 0.56f, rightBench.y - 20f, rightBench.width * 0.34f, rightBench.height + 12f),
            3, passengerCount, true);
    }

    void DrawPassenger(Rect rect, int seatIndex, int passengerCount, bool facingLeft)
    {
        bool occupied = seatIndex < passengerCount;
        Texture2D body = occupied ? coat : shadow;
        Texture2D head = occupied ? skin : black;
        float lean = facingLeft ? -rect.width * 0.1f : rect.width * 0.1f;
        GUI.DrawTexture(new Rect(rect.x + rect.width * 0.18f + lean, rect.y + rect.height * 0.28f, rect.width * 0.64f, rect.height * 0.48f),
            body, ScaleMode.StretchToFill);
        GUI.DrawTexture(new Rect(rect.x + rect.width * 0.33f + lean, rect.y + rect.height * 0.08f, rect.width * 0.34f, rect.width * 0.34f),
            head, ScaleMode.StretchToFill);

        string label = occupied ? GetSeatLabel(seatIndex) : "空座";
        GUI.Label(new Rect(rect.x - 8f, rect.y + rect.height - 8f, rect.width + 16f, 22f), label, mutedStyle);
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
        panel = MakeTexture(new Color(0.07f, 0.085f, 0.078f, 0.98f));
        window = MakeTexture(new Color(0.055f, 0.09f, 0.09f, 0.98f));
        floor = MakeTexture(new Color(0.12f, 0.125f, 0.11f, 0.96f));
        ceiling = MakeTexture(new Color(0.11f, 0.13f, 0.115f, 0.96f));
        bench = MakeTexture(new Color(0.16f, 0.18f, 0.15f, 0.98f));
        skin = MakeTexture(new Color(0.72f, 0.58f, 0.42f, 0.98f));
        coat = MakeTexture(new Color(0.23f, 0.3f, 0.27f, 0.98f));
        shadow = MakeTexture(new Color(0.025f, 0.03f, 0.028f, 0.94f));
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

    static int GetPassengerCount()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network != null && network.IsListening)
            return Mathf.Clamp(network.ConnectedClientsIds.Count, 1, 4);
        return 1;
    }

    static string GetSeatLabel(int seatIndex)
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening)
            return seatIndex == 0 ? "你" : $"队员 {seatIndex + 1}";

        if (seatIndex < network.ConnectedClientsIds.Count)
        {
            ulong clientId = GetConnectedClientIdAt(network, seatIndex);
            return clientId == network.LocalClientId ? "你" : $"队员 {seatIndex + 1}";
        }

        return $"队员 {seatIndex + 1}";
    }

    static ulong GetConnectedClientIdAt(NetworkManager network, int index)
    {
        int current = 0;
        foreach (ulong clientId in network.ConnectedClientsIds)
        {
            if (current == index)
                return clientId;
            current++;
        }

        return ulong.MaxValue;
    }
}
