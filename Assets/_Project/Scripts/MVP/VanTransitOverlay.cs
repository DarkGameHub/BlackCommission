using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Builds and shows the enclosed, windowless van cabin that players actually sit inside
/// during transit. Unlike the old passive cutscene, this NO LONGER disables the player
/// camera or spawns cosmetic dummies — real seated player bodies (with their own cameras,
/// gestures, held items and nameplates) occupy the cabin. The cabin is shown on the LOCAL
/// client whenever the local player is seated (PlayerController.EnterSeat/ExitSeat call
/// EnsureCabin/HideCabin). Geometry/seat math lives in <see cref="VanCabin"/>.
/// </summary>
public class VanTransitOverlay : MonoBehaviour
{
    public enum Phase { None, Boarding, Transit }

    static VanTransitOverlay current;

    GameObject interiorRoot;
    bool cabinShown;
    Phase phase;

    string taskTitle = "";
    string locationName = "";
    int boardedCount;

    GUIStyle headingStyle;
    GUIStyle smallStyle;

    public static bool IsActive => current != null && current.cabinShown;
    public static Phase CurrentPhase => current != null ? current.phase : Phase.None;

    // ─── Public API ───

    public static void EnsureCabin()
    {
        EnsureInstance();
        current.ShowCabin();
    }

    public static void HideCabin()
    {
        if (current != null) current.TeardownCabin();
    }

    public static void ShowBoarding(string title, string location, bool host)
    {
        EnsureInstance();
        current.taskTitle = Resolve(title, "commission");
        current.locationName = Resolve(location, "mission_location");
        if (current.phase == Phase.None) current.phase = Phase.Boarding;
        current.ShowCabin();
    }

    public static void ShowOutbound(string title, string location, float durationSeconds)
    {
        EnsureInstance();
        current.taskTitle = Resolve(title, "commission");
        current.locationName = Resolve(location, "mission_location");
        current.BeginDrive();
    }

    public static void ShowReturn(string title, string location, float durationSeconds)
    {
        EnsureInstance();
        current.taskTitle = Resolve(title, "office");
        current.locationName = Resolve(location, "office");
        current.BeginDrive();
    }

    public static void StartDeparture(float transitSeconds)
    {
        if (current != null) current.BeginDrive();
    }

    public static void NotifyPlayerBoarded(int totalBoarded)
    {
        if (current != null) current.boardedCount = totalBoarded;
    }

    static string Resolve(string value, string fallbackKey)
        => string.IsNullOrWhiteSpace(value) ? MvpLocale.T(fallbackKey) : value;

    static void EnsureInstance()
    {
        if (current != null) return;
        var go = new GameObject("MVP_VanTransitOverlay");
        DontDestroyOnLoad(go);
        current = go.AddComponent<VanTransitOverlay>();
    }

    // ─── Cabin lifecycle ───

    void ShowCabin()
    {
        if (interiorRoot == null)
        {
            interiorRoot = CreateProceduralInterior();
            interiorRoot.transform.position = VanCabin.Origin;
            interiorRoot.transform.rotation = Quaternion.identity;
            interiorRoot.transform.localScale = Vector3.one * VanCabin.Scale;
            DontDestroyOnLoad(interiorRoot);

            foreach (Collider c in interiorRoot.GetComponentsInChildren<Collider>())
                c.enabled = false;

            AddTransitInteriorLights();
        }
        cabinShown = true;
    }

    void BeginDrive()
    {
        ShowCabin();
        if (phase != Phase.Transit)
        {
            phase = Phase.Transit;
            AudioManager.Instance?.PlayEngineStart(Vector3.zero);
            AudioManager.Instance?.PlayEngineIdle();
        }
    }

    void TeardownCabin()
    {
        AudioManager.Instance?.StopEngineIdle();
        if (interiorRoot != null)
        {
            Destroy(interiorRoot);
            interiorRoot = null;
        }
        cabinShown = false;
        phase = Phase.None;
        boardedCount = 0;
    }

    // ─── Depart input (any seated player may request departure) ───

    void Update()
    {
        if (!cabinShown) return;

        PlayerController local = FindLocalPlayer();
        if (local == null || !local.IsSeated) return;

        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.spaceKey.wasPressedThisFrame)
            RequestDepart(local);
        // Leaving the seat is only allowed before the van actually drives off.
        else if (phase != Phase.Transit && keyboard.xKey.wasPressedThisFrame)
            local.RequestLeaveSeat();
    }

    void RequestDepart(PlayerController local)
    {
        // Return trip from a mission site.
        var missionManager = LostItemMissionManager.Instance;
        if (missionManager != null)
        {
            missionManager.RequestDepartVan();
            return;
        }

        // Outbound from HQ — server validates "everyone aboard" before launching.
        var computer = Object.FindAnyObjectByType<OfficeComputer>();
        if (computer != null)
            computer.RequestDepart(local);
    }

    static PlayerController FindLocalPlayer()
    {
        var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in all)
            if (p.IsOwner) return p;
        return null;
    }

    // ─── Minimal boarding/transit HUD (non-blocking strip) ───

    void OnGUI()
    {
        if (!cabinShown) return;
        EnsureStyles();

        PlayerController local = FindLocalPlayer();
        bool seated = local != null && local.IsSeated;

        float w = 520f;
        var rect = new Rect((Screen.width - w) * 0.5f, 24f, w, 60f);

        string header = string.IsNullOrEmpty(taskTitle)
            ? MvpLocale.T("van_cabin")
            : $"{taskTitle}  ·  {locationName}";
        GUI.Label(rect, header, headingStyle);

        if (phase == Phase.Transit)
        {
            GUI.Label(new Rect(rect.x, rect.y + 30f, w, 22f), MvpLocale.T("dispatch_outbound"), smallStyle);
        }
        else if (seated)
        {
            int total = GetTotalPlayerCount();
            string status = boardedCount >= total
                ? MvpLocale.T("all_aboard", boardedCount, total)
                : MvpLocale.T("waiting_team", boardedCount, total);
            GUI.Label(new Rect(rect.x, rect.y + 30f, w, 22f),
                status + "    " + MvpLocale.T("press_space_depart") + "    " + MvpLocale.T("press_x_leave"),
                smallStyle);

            // Return-trip grace countdown: a teammate armed departure; stragglers can still board.
            int countdown = Mathf.CeilToInt(LostItemMissionManager.DepartCountdownSeconds);
            if (countdown > 0)
                GUI.Label(new Rect(rect.x, rect.y + 52f, w, 22f), MvpLocale.T("departing_in", countdown), smallStyle);
        }
    }

    void EnsureStyles()
    {
        if (headingStyle != null) return;
        if (GUI.skin == null || GUI.skin.label == null) return;

        headingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.92f, 0.88f, 0.72f) }
        };
        smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.56f, 0.92f, 0.72f) }
        };
        MvpFontProvider.ApplyToStyle(headingStyle);
        MvpFontProvider.ApplyToStyle(smallStyle);
    }

    static int GetTotalPlayerCount()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network != null && network.IsListening)
            return Mathf.Max(1, network.ConnectedClientsIds.Count);
        return 1;
    }

    // ─── Procedural enclosed cabin (no windows) ───

    GameObject CreateProceduralInterior()
    {
        var root = new GameObject("MVP_VanTransitInterior_Procedural");

        Material wallMat = MakeFlatMaterial(new Color(0.184f, 0.310f, 0.294f));
        Material metalMat = MakeFlatMaterial(new Color(0.067f, 0.078f, 0.075f));
        Material benchMat = MakeFlatMaterial(new Color(0.125f, 0.208f, 0.196f));
        Material blackMat = MakeFlatMaterial(new Color(0.035f, 0.04f, 0.037f));
        Material amberMat = MakeFlatMaterial(new Color(0.851f, 0.604f, 0.192f));

        CreateInteriorBox("Floor", root.transform, new Vector3(0.45f, 0.36f, 0f), new Vector3(2f, 0.02f, 1.36f), metalMat);
        CreateInteriorBox("Ceiling", root.transform, new Vector3(0.45f, 1.44f, 0f), new Vector3(2f, 0.02f, 1.36f), metalMat);
        CreateInteriorBox("WallL", root.transform, new Vector3(0.45f, 0.92f, -0.68f), new Vector3(2f, 0.56f, 0.02f), wallMat);
        CreateInteriorBox("WallR", root.transform, new Vector3(0.45f, 0.92f, 0.68f), new Vector3(2f, 0.56f, 0.02f), wallMat);
        // Front (cab bulkhead) and rear doors — fully enclosed, no windows.
        CreateInteriorBox("WallFront", root.transform, new Vector3(-0.55f, 0.92f, 0f), new Vector3(0.02f, 0.56f, 1.36f), wallMat);
        CreateInteriorBox("WallRear", root.transform, new Vector3(1.45f, 0.92f, 0f), new Vector3(0.02f, 0.56f, 1.36f), wallMat);

        CreateInteriorBox("BenchL", root.transform, new Vector3(0.50f, 0.48f, -0.52f), new Vector3(1.5f, 0.04f, 0.18f), benchMat);
        CreateInteriorBox("BenchBackL", root.transform, new Vector3(0.50f, 0.80f, -0.64f), new Vector3(1.5f, 0.30f, 0.04f), benchMat);
        CreateInteriorBox("BenchR", root.transform, new Vector3(0.50f, 0.48f, 0.52f), new Vector3(1.5f, 0.04f, 0.18f), benchMat);
        CreateInteriorBox("BenchBackR", root.transform, new Vector3(0.50f, 0.80f, 0.64f), new Vector3(1.5f, 0.30f, 0.04f), benchMat);

        CreateInteriorBox("Light", root.transform, new Vector3(0.45f, 1.39f, 0f), new Vector3(0.72f, 0.012f, 0.025f), amberMat);

        // Municipal Debt Noir detail layer
        Material paperMat = MakeFlatMaterial(new Color(0.839f, 0.784f, 0.608f));
        Material debtMat = MakeFlatMaterial(new Color(0.761f, 0.227f, 0.169f));
        Material greenMat = MakeFlatMaterial(new Color(0.482f, 0.812f, 0.541f));
        Material grimeMat = MakeFlatMaterial(new Color(0.090f, 0.141f, 0.133f));

        CreateInteriorBox("SafetyNotice", root.transform,
            new Vector3(0.35f, 0.95f, -0.66f), new Vector3(0.32f, 0.22f, 0.01f), paperMat);
        CreateInteriorBox("SafetyNoticeStamp", root.transform,
            new Vector3(0.42f, 0.88f, -0.655f), new Vector3(0.1f, 0.06f, 0.008f), debtMat);
        CreateInteriorBox("NoSmokingSign", root.transform,
            new Vector3(0.72f, 1.05f, 0.665f), new Vector3(0.18f, 0.12f, 0.01f), debtMat);
        CreateInteriorBox("CompanyLogoBar", root.transform,
            new Vector3(-0.54f, 1.18f, 0f), new Vector3(0.01f, 0.06f, 0.48f), greenMat);
        CreateInteriorBox("FloorGrimeA", root.transform,
            new Vector3(0.55f, 0.372f, -0.22f), new Vector3(0.35f, 0.005f, 0.28f), grimeMat);
        CreateInteriorBox("FloorGrimeB", root.transform,
            new Vector3(0.85f, 0.372f, 0.32f), new Vector3(0.22f, 0.005f, 0.18f), grimeMat);

        return root;
    }

    static GameObject CreateInteriorBox(string name, Transform parent, Vector3 pos, Vector3 scale, Material mat)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = $"Interior_{name}";
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.GetComponent<Renderer>().material = mat;
        Object.Destroy(go.GetComponent<Collider>());
        return go;
    }

    static Material MakeFlatMaterial(Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard"));
        mat.color = color;
        return mat;
    }

    void AddTransitInteriorLights()
    {
        if (interiorRoot == null) return;

        var domeGo = new GameObject("TransitCabinLight");
        domeGo.transform.SetParent(interiorRoot.transform, false);
        domeGo.transform.localPosition = new Vector3(0.45f, 1.30f, 0f);
        var dome = domeGo.AddComponent<Light>();
        dome.type = LightType.Point;
        dome.color = new Color(1.0f, 0.92f, 0.78f);
        dome.intensity = 3.2f;
        dome.range = 4.5f * VanCabin.Scale;

        var fillGo = new GameObject("TransitCabinFillLight");
        fillGo.transform.SetParent(interiorRoot.transform, false);
        fillGo.transform.localPosition = new Vector3(-0.2f, 1.05f, 0f);
        var fill = fillGo.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(1.0f, 0.88f, 0.72f);
        fill.intensity = 2.0f;
        fill.range = 3.5f * VanCabin.Scale;
    }
}
