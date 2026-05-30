using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class VanTransitOverlay : MonoBehaviour
{
    public enum Phase { None, Boarding, Transit }

    static VanTransitOverlay current;

    string taskTitle;
    string locationName;
    float startTime;
    float transitDuration;
    float hideAt;
    bool outbound;
    bool isHost;
    Phase phase;
    int boardedCount;

    GameObject interiorRoot;
    Camera transitCamera;
    readonly List<GameObject> cosmeticPlayers = new();
    Camera disabledPlayerCamera;
    MonoBehaviour disabledCameraController;

    Texture2D ink;
    Texture2D cabin;
    Texture2D rubber;
    Texture2D amber;
    Texture2D red;
    Texture2D paper;
    Texture2D shadow;

    GUIStyle headingStyle;
    GUIStyle labelStyle;
    GUIStyle smallStyle;
    GUIStyle actionStyle;

    bool lockerOpen;
    SchoolExitPoint cachedExitPoint;
    float spaceHoldTime;
    const float ForceDepartHoldDuration = 2f;

    static readonly Vector3 TransitOffset = new(0f, 80f, 0f);

    static readonly Vector3[] SeatPositions =
    {
        new(0.15f, 0.70f, -0.42f),
        new(0.85f, 0.70f, -0.42f),
        new(0.15f, 0.70f, 0.42f),
        new(0.85f, 0.70f, 0.42f),
    };

    static readonly float[] SeatYaw = { 90f, 90f, -90f, -90f };

    public static bool IsActive => current != null && current.phase != Phase.None;
    public static Phase CurrentPhase => current != null ? current.phase : Phase.None;

    // ─── Public API ───

    public static void ShowOutbound(string taskTitle, string locationName, float durationSeconds)
    {
        EnsureInstance();
        current.BeginTransit(taskTitle, locationName, durationSeconds, true);
    }

    public static void ShowBoarding(string taskTitle, string locationName, bool host)
    {
        EnsureInstance();
        current.BeginBoarding(taskTitle, locationName, host);
    }

    public static void NotifyPlayerBoarded(int totalBoarded)
    {
        if (current != null && current.phase == Phase.Boarding)
            current.UpdateBoardedCount(totalBoarded);
    }

    public static void StartDeparture(float transitSeconds)
    {
        if (current != null && current.phase == Phase.Boarding)
            current.SwitchToTransit(transitSeconds);
    }

    public static void ShowReturn(string taskTitle, string locationName, float durationSeconds)
    {
        EnsureInstance();
        current.BeginTransit(taskTitle, locationName, durationSeconds, false);
    }

    static void EnsureInstance()
    {
        if (current != null) return;
        var go = new GameObject("MVP_VanTransitOverlay");
        DontDestroyOnLoad(go);
        current = go.AddComponent<VanTransitOverlay>();
    }

    // ─── Boarding ───

    void BeginBoarding(string newTaskTitle, string newLocationName, bool host)
    {
        taskTitle = string.IsNullOrWhiteSpace(newTaskTitle) ? MvpLocale.T("commission") : newTaskTitle;
        locationName = string.IsNullOrWhiteSpace(newLocationName) ? MvpLocale.T("mission_location") : newLocationName;
        isHost = host;
        outbound = false;
        phase = Phase.Boarding;
        boardedCount = 1;
        startTime = Time.unscaledTime;
        hideAt = float.MaxValue;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Teardown3D();
        Setup3D();
    }

    void UpdateBoardedCount(int count)
    {
        int previous = boardedCount;
        boardedCount = count;

        for (int i = previous; i < Mathf.Min(count, SeatPositions.Length); i++)
        {
            if (i == 0) continue;
            SpawnSinglePassenger(i);
        }
    }

    void SwitchToTransit(float transitSeconds)
    {
        transitDuration = Mathf.Max(1.5f, transitSeconds);
        startTime = Time.unscaledTime;
        hideAt = startTime + transitDuration + 0.65f;
        phase = Phase.Transit;
        AudioManager.Instance?.PlayEngineStart(Vector3.zero);
        AudioManager.Instance?.PlayEngineIdle();
    }

    // ─── Transit ───

    void BeginTransit(string newTaskTitle, string newLocationName, float durationSeconds, bool isOutbound)
    {
        taskTitle = string.IsNullOrWhiteSpace(newTaskTitle) ? MvpLocale.T("commission") : newTaskTitle;
        locationName = string.IsNullOrWhiteSpace(newLocationName) ? MvpLocale.T("mission_location") : newLocationName;
        transitDuration = Mathf.Max(1.5f, durationSeconds);
        outbound = isOutbound;
        phase = Phase.Transit;
        startTime = Time.unscaledTime;
        hideAt = startTime + transitDuration + 0.65f;
        boardedCount = GetPassengerCount();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (interiorRoot == null)
        {
            Teardown3D();
            Setup3D();
        }
    }

    // ─── 3D Interior ───

    void Setup3D()
    {
        GameObject prefab = Resources.Load<GameObject>("GeneratedArt/ASV4_VanTransitInterior");
        if (prefab != null)
        {
            interiorRoot = Instantiate(prefab);
            interiorRoot.name = "MVP_VanTransitInterior_3D";
        }
        else
        {
            interiorRoot = CreateProceduralInterior();
        }

        interiorRoot.transform.position = TransitOffset;
        interiorRoot.transform.rotation = Quaternion.identity;
        DontDestroyOnLoad(interiorRoot);

        foreach (Collider c in interiorRoot.GetComponentsInChildren<Collider>())
            c.enabled = false;

        // The interior lives 80m above the world, away from every scene light, and the
        // FBX/procedural cabin uses dark materials — without its own lights the boarding
        // and transit views render as a black screen. Light the cabin so it reads.
        AddTransitInteriorLights();

        SetupTransitCamera();
        DisablePlayerCamera();

        int count = Mathf.Max(1, boardedCount);
        for (int i = 1; i < Mathf.Min(count, SeatPositions.Length); i++)
            SpawnSinglePassenger(i);
    }

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

        CreateInteriorBox("BenchL", root.transform, new Vector3(0.50f, 0.48f, -0.52f), new Vector3(1.5f, 0.04f, 0.18f), benchMat);
        CreateInteriorBox("BenchBackL", root.transform, new Vector3(0.50f, 0.80f, -0.64f), new Vector3(1.5f, 0.30f, 0.04f), benchMat);
        CreateInteriorBox("BenchR", root.transform, new Vector3(0.50f, 0.48f, 0.52f), new Vector3(1.5f, 0.04f, 0.18f), benchMat);
        CreateInteriorBox("BenchBackR", root.transform, new Vector3(0.50f, 0.80f, 0.64f), new Vector3(1.5f, 0.30f, 0.04f), benchMat);

        CreateInteriorBox("CageTop", root.transform, new Vector3(-0.55f, 1.42f, 0f), new Vector3(0.04f, 0.04f, 1.36f), metalMat);
        CreateInteriorBox("CageBot", root.transform, new Vector3(-0.55f, 0.42f, 0f), new Vector3(0.04f, 0.04f, 1.36f), metalMat);
        for (int i = 0; i < 7; i++)
        {
            float z = -0.54f + i * 0.18f;
            CreateInteriorCylinder($"Bar{i}", root.transform, new Vector3(-0.55f, 0.92f, z), 0.012f, 1.0f, metalMat);
        }

        CreateInteriorBox("DriverTorso", root.transform, new Vector3(-1.30f, 0.88f, 0f), new Vector3(0.24f, 0.36f, 0.20f), blackMat);
        CreateInteriorBox("DriverHead", root.transform, new Vector3(-1.30f, 1.28f, -0.02f), new Vector3(0.18f, 0.20f, 0.16f), blackMat);
        CreateInteriorBox("DriverCap", root.transform, new Vector3(-1.30f, 1.42f, -0.04f), new Vector3(0.22f, 0.04f, 0.18f), blackMat);

        CreateInteriorBox("Light", root.transform, new Vector3(0.45f, 1.39f, 0f), new Vector3(0.72f, 0.012f, 0.025f), amberMat);

        for (int i = 0; i < 2; i++)
        {
            float z = i == 0 ? -0.32f : 0.32f;
            CreateInteriorCylinder($"GrabBar{i}", root.transform, new Vector3(0.45f, 1.38f, z), 0.018f, 1.6f, metalMat, horizontal: true);
        }

        // Municipal Debt Noir detail layer
        Material paperMat = MakeFlatMaterial(new Color(0.839f, 0.784f, 0.608f));
        Material debtMat = MakeFlatMaterial(new Color(0.761f, 0.227f, 0.169f));
        Material greenMat = MakeFlatMaterial(new Color(0.482f, 0.812f, 0.541f));
        Material cardboardMat = MakeFlatMaterial(new Color(0.451f, 0.314f, 0.165f));
        Material grimeMat = MakeFlatMaterial(new Color(0.090f, 0.141f, 0.133f));

        CreateInteriorBox("SafetyNotice", root.transform,
            new Vector3(0.35f, 0.95f, -0.66f), new Vector3(0.32f, 0.22f, 0.01f), paperMat);
        CreateInteriorBox("SafetyNoticeStamp", root.transform,
            new Vector3(0.42f, 0.88f, -0.655f), new Vector3(0.1f, 0.06f, 0.008f), debtMat);
        CreateInteriorBox("NoSmokingSign", root.transform,
            new Vector3(0.72f, 1.05f, 0.665f), new Vector3(0.18f, 0.12f, 0.01f), debtMat);
        CreateInteriorBox("CompanyLogoBar", root.transform,
            new Vector3(-0.50f, 1.18f, 0f), new Vector3(0.01f, 0.06f, 0.48f), greenMat);
        CreateInteriorBox("CompanyLogoLeft", root.transform,
            new Vector3(-0.505f, 1.02f, -0.20f), new Vector3(0.01f, 0.38f, 0.06f), greenMat);
        CreateInteriorBox("CompanyLogoRight", root.transform,
            new Vector3(-0.505f, 1.02f, 0.20f), new Vector3(0.01f, 0.38f, 0.06f), greenMat);
        var slash = CreateInteriorBox("CompanyDebtSlash", root.transform,
            new Vector3(-0.51f, 1.02f, 0f), new Vector3(0.008f, 0.44f, 0.06f), debtMat);
        slash.transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
        CreateInteriorBox("GearCrate", root.transform,
            new Vector3(1.25f, 0.44f, 0.08f), new Vector3(0.22f, 0.16f, 0.18f), cardboardMat);
        CreateInteriorBox("GearCrateLabel", root.transform,
            new Vector3(1.25f, 0.46f, 0.18f), new Vector3(0.14f, 0.08f, 0.008f), paperMat);
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

    static void CreateInteriorCylinder(string name, Transform parent, Vector3 pos, float radius, float height, Material mat, bool horizontal = false)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = $"Interior_{name}";
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        if (horizontal)
            go.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        go.GetComponent<Renderer>().material = mat;
        Object.Destroy(go.GetComponent<Collider>());
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

        // Main warm cabin dome light near the ceiling above the seats.
        var domeGo = new GameObject("TransitCabinLight");
        domeGo.transform.SetParent(interiorRoot.transform, false);
        domeGo.transform.localPosition = new Vector3(0.45f, 1.30f, 0f);
        var dome = domeGo.AddComponent<Light>();
        dome.type = LightType.Point;
        dome.color = new Color(1.0f, 0.92f, 0.78f);
        dome.intensity = 3.2f;
        dome.range = 4.5f;

        // Fill light up by the driver/cab so the front of the cabin isn't black.
        var fillGo = new GameObject("TransitCabinFillLight");
        fillGo.transform.SetParent(interiorRoot.transform, false);
        fillGo.transform.localPosition = new Vector3(-1.0f, 1.05f, 0f);
        var fill = fillGo.AddComponent<Light>();
        fill.type = LightType.Point;
        fill.color = new Color(1.0f, 0.88f, 0.72f);
        fill.intensity = 2.0f;
        fill.range = 3.5f;
    }

    void SetupTransitCamera()
    {
        var camGo = new GameObject("TransitCamera");
        camGo.transform.SetParent(interiorRoot.transform);
        camGo.transform.localPosition = SeatPositions[0] + new Vector3(0f, 0.55f, 0f);
        camGo.transform.localRotation = Quaternion.Euler(5f, SeatYaw[0], 0f);

        transitCamera = camGo.AddComponent<Camera>();
        transitCamera.fieldOfView = 68f;
        transitCamera.nearClipPlane = 0.05f;
        transitCamera.farClipPlane = 20f;
        transitCamera.clearFlags = CameraClearFlags.SolidColor;
        // Dim warm cabin-dark rather than pure black, so a slightly off frame never
        // reads as a "black screen" while the next scene streams in.
        transitCamera.backgroundColor = new Color(0.05f, 0.045f, 0.04f);
        transitCamera.depth = 100f;

        camGo.AddComponent<AudioListener>();
    }

    void DisablePlayerCamera()
    {
        PlayerCameraController[] controllers = FindObjectsByType<PlayerCameraController>(FindObjectsSortMode.None);
        foreach (var ctrl in controllers)
        {
            if (!ctrl.IsOwner) continue;
            Camera cam = ctrl.GetComponentInChildren<Camera>();
            if (cam != null)
            {
                cam.enabled = false;
                disabledPlayerCamera = cam;
            }
            AudioListener listener = ctrl.GetComponentInChildren<AudioListener>();
            if (listener != null)
                listener.enabled = false;
            ctrl.enabled = false;
            disabledCameraController = ctrl;
            break;
        }
    }

    void RestorePlayerCamera()
    {
        if (disabledPlayerCamera != null)
        {
            disabledPlayerCamera.enabled = true;
            disabledPlayerCamera = null;
        }
        if (disabledCameraController != null)
        {
            disabledCameraController.enabled = true;
            AudioListener listener = disabledCameraController.GetComponentInChildren<AudioListener>();
            if (listener != null)
                listener.enabled = true;
            disabledCameraController = null;
        }
    }

    void SpawnSinglePassenger(int seatIndex)
    {
        if (interiorRoot == null) return;
        if (seatIndex < 0 || seatIndex >= SeatPositions.Length) return;

        int charIndex = GetCharacterIndexForSeat(seatIndex);
        var colors = PlayerCharacterPalette.Get(charIndex);

        // Prefer the new textured character mesh (per seat's chosen slot); fall back to
        // the old worker, then primitives.
        GameObject characterPrefab = Resources.Load<GameObject>(PlayerCharacterModels.Get(charIndex));
        GameObject workerPrefab = characterPrefab == null
            ? Resources.Load<GameObject>("GeneratedArt/ASV4_WorkerCheapOutsourcedUniform")
            : null;
        GameObject model;
        if (characterPrefab != null)
        {
            model = Instantiate(characterPrefab, interiorRoot.transform);
            Color tint = PlayerCharacterModels.TintFor(charIndex);
            foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
            {
                Material mat = r.material;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
                else mat.color = tint;
            }
        }
        else if (workerPrefab != null)
        {
            model = Instantiate(workerPrefab, interiorRoot.transform);
            ApplyColorsToModel(model, colors);
        }
        else
        {
            model = CreateFallbackPlayerModel(colors);
            model.transform.SetParent(interiorRoot.transform);
        }

        model.name = $"TransitPassenger_{seatIndex}";
        model.transform.localPosition = SeatPositions[seatIndex];
        model.transform.localRotation = Quaternion.Euler(0f, SeatYaw[seatIndex], 0f);
        model.transform.localScale = Vector3.one * 0.45f;

        foreach (Collider c in model.GetComponentsInChildren<Collider>())
            c.enabled = false;
        foreach (var nb in model.GetComponentsInChildren<NetworkBehaviour>())
            Destroy(nb);
        foreach (var no in model.GetComponentsInChildren<NetworkObject>())
            Destroy(no);

        cosmeticPlayers.Add(model);
    }

    static int GetCharacterIndexForSeat(int seatIndex)
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening)
            return seatIndex % PlayerCharacterPalette.Count;

        int playerIndex = 0;
        foreach (ulong clientId in network.ConnectedClientsIds)
        {
            if (playerIndex == seatIndex && network.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<PlayerController>(out var ctrl))
                    return ctrl.CharacterIndex.Value;
                break;
            }
            playerIndex++;
        }

        return seatIndex % PlayerCharacterPalette.Count;
    }

    static PlayerCharacterPalette.CharacterColors GetCharacterColorsForSeat(int seatIndex)
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening)
            return PlayerCharacterPalette.Get(0);

        int playerIndex = 0;
        foreach (ulong clientId in network.ConnectedClientsIds)
        {
            if (playerIndex == seatIndex && network.ConnectedClients.TryGetValue(clientId, out var client))
            {
                if (client.PlayerObject != null && client.PlayerObject.TryGetComponent<PlayerController>(out var ctrl))
                    return PlayerCharacterPalette.Get(ctrl.CharacterIndex.Value);
                break;
            }
            playerIndex++;
        }

        return PlayerCharacterPalette.Get(seatIndex % PlayerCharacterPalette.Count);
    }

    static void ApplyColorsToModel(GameObject model, PlayerCharacterPalette.CharacterColors colors)
    {
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>())
        {
            string n = r.gameObject.name.ToLowerInvariant();
            if (n.Contains("uniform") || n.Contains("torso") || n.Contains("arm") || n.Contains("leg") || n.Contains("fabric"))
                r.material.color = colors.uniform;
            else if (n.Contains("vest") || n.Contains("safety"))
                r.material.color = colors.vest;
            else if (n.Contains("helmet") || n.Contains("hat") || n.Contains("hardhat"))
                r.material.color = colors.helmet;
        }
    }

    static GameObject CreateFallbackPlayerModel(PlayerCharacterPalette.CharacterColors colors)
    {
        var root = new GameObject("FallbackPassenger");

        var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
        torso.transform.SetParent(root.transform);
        torso.transform.localPosition = new Vector3(0f, 0.45f, 0f);
        torso.transform.localScale = new Vector3(0.42f, 0.52f, 0.22f);
        torso.GetComponent<Renderer>().material.color = colors.uniform;

        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(root.transform);
        head.transform.localPosition = new Vector3(0f, 0.88f, 0f);
        head.transform.localScale = new Vector3(0.28f, 0.32f, 0.26f);
        head.GetComponent<Renderer>().material.color = PlayerCharacterPalette.Skin;

        var helmet = GameObject.CreatePrimitive(PrimitiveType.Cube);
        helmet.transform.SetParent(root.transform);
        helmet.transform.localPosition = new Vector3(0f, 1.05f, 0f);
        helmet.transform.localScale = new Vector3(0.36f, 0.08f, 0.30f);
        helmet.GetComponent<Renderer>().material.color = colors.helmet;

        var vest = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vest.transform.SetParent(root.transform);
        vest.transform.localPosition = new Vector3(0f, 0.48f, 0.12f);
        vest.transform.localScale = new Vector3(0.44f, 0.24f, 0.04f);
        vest.GetComponent<Renderer>().material.color = colors.vest;

        return root;
    }

    void UpdateVanSway()
    {
        if (transitCamera == null) return;
        float t = (Time.unscaledTime - startTime) * 1.8f;
        float swayX = Mathf.Sin(t * 0.7f) * 0.3f;
        float swayZ = Mathf.Sin(t * 1.1f + 0.5f) * 0.2f;
        transitCamera.transform.localRotation = Quaternion.Euler(5f + swayX, SeatYaw[0], swayZ);
    }

    void Teardown3D()
    {
        AudioManager.Instance?.StopEngineIdle();
        RestorePlayerCamera();

        foreach (var go in cosmeticPlayers)
            if (go != null) Destroy(go);
        cosmeticPlayers.Clear();

        if (transitCamera != null)
        {
            Destroy(transitCamera.gameObject);
            transitCamera = null;
        }

        if (interiorRoot != null)
        {
            Destroy(interiorRoot);
            interiorRoot = null;
        }
    }

    // ─── Update ───

    void Update()
    {
        if (phase == Phase.Boarding)
        {
            UpdateVanSway();
            if (isHost) UpdateBoardingDepartInput();
            return;
        }

        if (phase == Phase.Transit)
        {
            UpdateVanSway();

            // F key opens/closes the van supply locker (return trips only)
            if (!outbound && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
                lockerOpen = !lockerOpen;

            if (Time.unscaledTime > hideAt)
            {
                lockerOpen = false;
                Teardown3D();
                phase = Phase.None;
                if (current == this) current = null;
                Destroy(gameObject);
            }
        }
    }

    // ─── GUI ───

    void OnGUI()
    {
        if (phase == Phase.None) return;
        EnsureTextures();
        EnsureStyles();

        if (phase == Phase.Boarding)
            DrawBoardingOverlay();
        else if (phase == Phase.Transit)
        {
            DrawTransitInfoOverlay();
            if (!outbound) DrawLockerHint();
            if (lockerOpen) DrawLockerPanel();
        }
    }

    void DrawBoardingOverlay()
    {
        float alpha = Mathf.Clamp01((Time.unscaledTime - startTime) / 0.5f) * 0.92f;
        GUI.color = new Color(1f, 1f, 1f, alpha);

        bool allAboard = boardedCount >= GetTotalPlayerCount();
        float panelW = Mathf.Clamp(Screen.width * 0.34f, 240f, 380f);
        float panelH = isHost ? 128f : 100f;
        Rect panel = new Rect(24f, Screen.height - panelH - 24f, panelW, panelH);

        // Red border flash when force-departing
        if (isHost && !allAboard && spaceHoldTime > 0.2f)
        {
            GUI.color = new Color(1f, 1f, 1f, alpha * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f)));
            DrawRect(new Rect(panel.x - 2, panel.y - 2, panel.width + 4, panel.height + 4), red);
        }
        GUI.color = new Color(1f, 1f, 1f, alpha);

        DrawRect(panel, ink);
        DrawRect(new Rect(panel.x + 4f, panel.y + 4f, panel.width - 8f, panel.height - 8f), cabin);

        GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, 20f), taskTitle, labelStyle);

        string status = allAboard
            ? MvpLocale.T("all_aboard", boardedCount, GetTotalPlayerCount())
            : MvpLocale.T("waiting_team", boardedCount, GetTotalPlayerCount());
        GUI.Label(new Rect(panel.x + 14f, panel.y + 34f, panel.width - 28f, 18f), status, smallStyle);

        float sway = Mathf.Sin(Time.unscaledTime * 2f) * 0.5f + 0.5f;
        string dots = sway > 0.66f ? "..." : sway > 0.33f ? ".." : ".";
        GUI.Label(new Rect(panel.x + 14f, panel.y + 56f, panel.width - 28f, 18f),
            MvpLocale.T("driver_waiting") + dots, smallStyle);

        if (isHost)
        {
            if (allAboard)
            {
                Rect btn = new Rect(panel.x + 14f, panel.y + panelH - 34f, panel.width - 28f, 26f);
                DrawRect(btn, amber);
                GUI.Label(btn, "[SPACE] 发车", actionStyle);
            }
            else
            {
                // Force depart progress bar
                float progress = spaceHoldTime / ForceDepartHoldDuration;
                Rect barBg = new Rect(panel.x + 14f, panel.y + panelH - 34f, panel.width - 28f, 12f);
                DrawRect(barBg, rubber);
                if (progress > 0f)
                    DrawRect(new Rect(barBg.x, barBg.y, barBg.width * progress, barBg.height), red);
                string forceLabel = progress > 0.05f
                    ? $"长按SPACE强制发车 ({(int)(ForceDepartHoldDuration - spaceHoldTime + 1)}s)"
                    : "长按SPACE强制发车";
                GUI.Label(new Rect(panel.x + 14f, panel.y + panelH - 52f, panel.width - 28f, 16f),
                    forceLabel, smallStyle);
            }
        }

        GUI.color = Color.white;
    }

    void DrawTransitInfoOverlay()
    {
        float elapsed = Time.unscaledTime - startTime;
        float progress = Mathf.Clamp01(elapsed / transitDuration);
        float fadeIn = Mathf.Clamp01(elapsed / 0.5f);
        float fadeOut = Mathf.Clamp01((hideAt - Time.unscaledTime) / 0.42f);
        GUI.color = new Color(1f, 1f, 1f, Mathf.Min(fadeIn, fadeOut) * 0.92f);

        float panelW = Mathf.Clamp(Screen.width * 0.32f, 220f, 360f);
        float panelH = 130f;
        Rect panel = new Rect(24f, Screen.height - panelH - 24f, panelW, panelH);

        DrawRect(panel, ink);
        DrawRect(new Rect(panel.x + 4f, panel.y + 4f, panel.width - 8f, panel.height - 8f), cabin);

        string title = outbound ? MvpLocale.T("dispatch_outbound") : MvpLocale.T("return_office");
        GUI.Label(new Rect(panel.x + 14f, panel.y + 10f, panel.width - 28f, 22f), title, headingStyle);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 36f, panel.width - 28f, 18f), taskTitle, labelStyle);

        string officeName = MvpLocale.T("office");
        string route = outbound ? $"{officeName} → {locationName}" : $"{locationName} → {officeName}";
        GUI.Label(new Rect(panel.x + 14f, panel.y + 60f, panel.width - 28f, 18f), route, smallStyle);
        GUI.Label(new Rect(panel.x + 14f, panel.y + 80f, 100f, 18f), MvpLocale.T("in_van", boardedCount), smallStyle);

        Rect bar = new Rect(panel.x + 14f, panel.y + panelH - 24f, panel.width - 28f, 10f);
        DrawRect(bar, rubber);
        DrawRect(new Rect(bar.x, bar.y, bar.width * progress, bar.height), outbound ? amber : red);

        GUI.color = Color.white;
    }

    void UpdateBoardingDepartInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool allAboard = boardedCount >= GetTotalPlayerCount();

        if (allAboard && keyboard.spaceKey.wasPressedThisFrame)
        {
            spaceHoldTime = 0f;
            TriggerDepart();
            return;
        }

        if (!allAboard && keyboard.spaceKey.isPressed)
        {
            spaceHoldTime += Time.unscaledDeltaTime;
            if (spaceHoldTime >= ForceDepartHoldDuration)
            {
                spaceHoldTime = 0f;
                TriggerDepart();
            }
        }
        else
        {
            spaceHoldTime = Mathf.Max(0f, spaceHoldTime - Time.unscaledDeltaTime * 2f);
        }
    }

    void TriggerDepart()
    {
        // Return from school: use LostItemMissionManager
        var missionManager = LostItemMissionManager.Instance;
        if (missionManager != null)
        {
            missionManager.RequestDepartVan();
            return;
        }

        // Depart from HQ: use OfficeDepartureVan → OfficeComputer
        var van = Object.FindAnyObjectByType<OfficeDepartureVan>();
        var computer = Object.FindAnyObjectByType<OfficeComputer>();
        var player = FindLocalPlayer();
        if (van != null && computer != null && player != null)
            computer.LaunchSelectedMissionFromVehicle(player);
    }

    static PlayerController FindLocalPlayer()
    {
        var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in all)
            if (p.IsOwner) return p;
        return null;
    }

    void DrawLockerHint()
    {
        EnsureStyles();
        float alpha = 0.7f + Mathf.Sin(Time.unscaledTime * 2.5f) * 0.3f;
        GUI.color = new Color(1f, 1f, 1f, alpha * 0.85f);
        string hint = lockerOpen ? "[F] 关闭储物柜" : "[F] 打开储物柜";
        GUI.Label(new Rect(Screen.width - 210f, Screen.height - 56f, 200f, 22f), hint, smallStyle);
        GUI.color = Color.white;
    }

    void DrawLockerPanel()
    {
        EnsureTextures();
        EnsureStyles();

        if (cachedExitPoint == null)
            cachedExitPoint = FindObjectsByType<SchoolExitPoint>(FindObjectsSortMode.None).Length > 0
                ? FindObjectsByType<SchoolExitPoint>(FindObjectsSortMode.None)[0]
                : null;

        float panelW = 240f, panelH = 130f;
        Rect panel = new Rect(Screen.width - panelW - 18f, Screen.height - panelH - 64f, panelW, panelH);

        DrawRect(panel, ink);
        DrawRect(new Rect(panel.x + 3, panel.y + 3, panel.width - 6, panel.height - 6), cabin);

        GUI.Label(new Rect(panel.x + 12, panel.y + 10, panel.width - 24, 20), "车载储物柜", headingStyle);

        if (cachedExitPoint == null)
        {
            GUI.Label(new Rect(panel.x + 12, panel.y + 38, panel.width - 24, 20), "储物柜已离线", smallStyle);
            return;
        }

        for (int i = 0; i < 2; i++)
        {
            MvpHotbarItemId itemId = cachedExitPoint.GetLockerItemId(i);
            int qty = cachedExitPoint.GetLockerQuantity(i);
            if (itemId == MvpHotbarItemId.None) continue;

            string name = itemId == MvpHotbarItemId.Flashlight ? "手电筒" : "电池";
            float rowY = panel.y + 40 + i * 34;
            GUI.Label(new Rect(panel.x + 12, rowY, 130, 20), $"{name}  x{qty}", labelStyle);

            bool canTake = qty > 0;
            GUI.enabled = canTake;
            if (GUI.Button(new Rect(panel.x + panel.width - 68, rowY - 2, 56, 24), "取出", actionStyle))
                TakeLockerItem(cachedExitPoint, i);
            GUI.enabled = true;
        }
    }

    void TakeLockerItem(SchoolExitPoint exitPoint, int slotIndex)
    {
        exitPoint.TryTakeLockerItem(slotIndex);
    }

    void DrawRect(Rect rect, Texture2D texture)
    {
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill);
    }

    // ─── Utilities ───

    void EnsureTextures()
    {
        if (ink != null) return;
        ink = MakeTexture(new Color(0.014f, 0.016f, 0.015f, 0.98f));
        cabin = MakeTexture(new Color(0.055f, 0.064f, 0.058f, 0.98f));
        rubber = MakeTexture(new Color(0.035f, 0.04f, 0.037f, 0.98f));
        amber = MakeTexture(new Color(0.78f, 0.54f, 0.2f, 0.95f));
        red = MakeTexture(new Color(0.48f, 0.11f, 0.09f, 0.95f));
        paper = MakeTexture(new Color(0.72f, 0.67f, 0.49f, 0.98f));
        shadow = MakeTexture(new Color(0f, 0f, 0f, 0.36f));
    }

    void EnsureStyles()
    {
        if (headingStyle != null) return;
        if (GUI.skin == null || GUI.skin.label == null) return;

        headingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.92f, 0.88f, 0.72f) }
        };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = new Color(0.86f, 0.86f, 0.78f) }
        };
        smallStyle = new GUIStyle(labelStyle)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.55f, 0.6f, 0.55f) }
        };
        actionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.06f, 0.06f, 0.05f) }
        };

        MvpFontProvider.ApplyToStyle(headingStyle);
        MvpFontProvider.ApplyToStyle(labelStyle);
        MvpFontProvider.ApplyToStyle(smallStyle);
        MvpFontProvider.ApplyToStyle(actionStyle);
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

    static int GetTotalPlayerCount()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network != null && network.IsListening)
            return network.ConnectedClientsIds.Count;
        return 1;
    }
}
