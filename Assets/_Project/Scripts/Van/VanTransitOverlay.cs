using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// The boarding → 派车单 → transit → arrival surface of the dispatch-van ritual
/// (design/ux/boarding-transit.md). Builds and shows the enclosed, windowless van cabin
/// that players actually sit inside during transit — real seated player bodies (cameras,
/// gestures, held items, nameplates) occupy it; geometry/seat math lives in
/// <see cref="VanCabin"/>.
///
/// The cabin doubles as the loading screen: the destination scene loads UNDERNEATH while
/// the crew rides (the cabin is DontDestroyOnLoad and has no windows), and the rear door
/// only opens once the scene is ready AND the minimum transit time has elapsed — no black
/// screen, no 2D loading page. Three paper surfaces share the civic-paperwork grammar:
/// the top ticket strip (派遣票据条), the central dispatch card (派车单, pops when the
/// whole living crew is seated, host signs with Space), and the early-return application
/// card (提前收工申请单, host holds E for 1.2s to sign, Esc withdraws).
/// </summary>
public class VanTransitOverlay : MonoBehaviour
{
    public enum Phase { None, Boarding, Transit, Arrived }
    enum Card { None, Dispatch, DispatchSigned, EarlyReturn }

    const float CardSlideSeconds = 0.25f;   // paper slides in from below (settlement grammar)
    const float StampSlamSeconds = 0.12f;   // 章砸落 1.3→1.0
    const float StampBeatSeconds = 0.6f;    // 已签发 → card folds, van pulls out
    const float SignHoldSeconds = 1.2f;     // hold-E signature (PM-locked)
    const float SignDecayRate = SignHoldSeconds / 0.3f; // released ink drains in 0.3s
    const float ProgressCap = 0.92f;        // bar breathes here while the scene still loads
    const float ArrivalFillSeconds = 0.4f;  // 92% → 100% top-up on arrival
    const float SquelchAtSeconds = 0.5f;    // engine-off silence, then the radio keys
    const float DoorCrackAtSeconds = 0.7f;  // 门缝渗光
    const float DoorOpenAtSeconds = 1.4f;
    const float DoorOpenSeconds = 1.2f;

    static VanTransitOverlay current;
    static float lastSceneLoadedAt = float.NegativeInfinity;
    static bool hasArrivalSpawn;
    static Vector3 arrivalSpawnPos;
    static Quaternion arrivalSpawnRot;

    GameObject interiorRoot;
    bool cabinShown;
    Phase phase;
    AudioSource radioSource;
    AudioClip radioSquelchClip;

    string taskTitle = "";
    string locationName = "";

    // Boarding counts, cached each Update for OnGUI (client-safe: counts player objects,
    // never NetworkManager.ConnectedClients which is server-only).
    int seatedCount;
    int livingCount;
    int neededCount;
    bool allSeated;

    Card card;
    float cardShownAt;
    float signedAt;              // dispatch stamp slam start
    bool drivePending;           // signed → waiting the stamp beat before driving off
    float pendingDriveDuration;
    float signHold;              // early-return ink, 0..SignHoldSeconds
    int estimatedPartialMoney;

    // Transit gate: the ride ends when BOTH the minimum duration elapsed AND a scene
    // load finished after the signature (the destination loading underneath us).
    float transitDuration;
    float transitStartTime;
    float driveRequestedAt = float.PositiveInfinity;

    // Arrival sequence (engine off → silence+squelch → door-crack light → full open → E).
    float arrivalStartedAt;
    bool squelchPlayed;
    bool doorFullyOpen;
    float doorFullyOpenAt;
    bool disembarked;
    Transform rearDoor;
    Vector3 rearDoorClosedPos;
    GameObject arrivalGlow;
    Light arrivalLight;

    GUIStyle headingStyle;
    GUIStyle smallStyle;
    GUIStyle cardHeaderStyle;
    GUIStyle cardRowStyle;
    GUIStyle cardSublineStyle;
    GUIStyle cardWarnStyle;
    GUIStyle stampStyle;
    GUIStyle stampPendingStyle;

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

    public static void ShowOutbound(string title, string location, float minTransitSeconds)
    {
        EnsureInstance();
        current.taskTitle = Resolve(title, "commission");
        current.locationName = Resolve(location, "mission_location");
        current.BeginSignedDeparture(minTransitSeconds);
    }

    public static void ShowReturn(string title, string location, float minTransitSeconds)
    {
        EnsureInstance();
        current.taskTitle = Resolve(title, "commission");
        current.locationName = Resolve(location, "office");
        current.BeginSignedDeparture(minTransitSeconds);
    }

    /// <summary>
    /// The destination's spawn manager hands over the spawn instead of teleporting a
    /// still-riding player out of the cabin; the overlay uses it when the player steps off.
    /// </summary>
    public static void RegisterArrivalSpawn(Vector3 position, Quaternion rotation)
    {
        hasArrivalSpawn = true;
        arrivalSpawnPos = position;
        arrivalSpawnRot = rotation;
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

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;
    void OnSceneLoaded(Scene scene, LoadSceneMode mode) => lastSceneLoadedAt = Time.unscaledTime;

    bool DestinationSceneReady => lastSceneLoadedAt > driveRequestedAt;

    // ─── Departure (signed by the host: stamp beat, then the van pulls out) ───

    void BeginSignedDeparture(float minTransitSeconds)
    {
        ShowCabin();
        if (phase == Phase.Transit || phase == Phase.Arrived) return;

        driveRequestedAt = Time.unscaledTime;
        hasArrivalSpawn = false;
        pendingDriveDuration = Mathf.Max(0f, minTransitSeconds);

        if (card == Card.Dispatch)
        {
            // 「已签发」章砸落 + StampThunk 同帧 → 0.6s 后卡收起，票据条接力。
            card = Card.DispatchSigned;
            signedAt = Time.unscaledTime;
            drivePending = true;
            AudioManager.Instance?.PlayStamp();
        }
        else
        {
            // No card up on this peer (failure forced return / straggler): drive straight off.
            card = Card.None;
            drivePending = false;
            BeginDrive(pendingDriveDuration);
        }
    }

    void BeginDrive(float durationSeconds)
    {
        ShowCabin();
        if (phase == Phase.Transit || phase == Phase.Arrived) return;

        phase = Phase.Transit;
        transitDuration = Mathf.Max(0.01f, durationSeconds);
        transitStartTime = Time.unscaledTime;
        squelchPlayed = false;
        doorFullyOpen = false;
        disembarked = false;
        AudioManager.Instance?.PlayEngineStart(Vector3.zero);
        AudioManager.Instance?.PlayEngineIdle();
        // Dispatch keys the channel as the van pulls out.
        if (radioSource != null && radioSquelchClip != null)
            radioSource.PlayOneShot(radioSquelchClip, 0.8f);
    }

    // ─── Cabin lifecycle ───

    void ShowCabin()
    {
        if (interiorRoot == null)
        {
            // Procedural interior by default: its benches and the VanCabin seat offsets are
            // authored in the same space, so seating is exact by construction (no tuning).
            // A modeled interior is opt-in (VanCabin.UseModeledInterior) and auto-fit.
            interiorRoot = VanCabin.UseModeledInterior ? TryCreateModeledInterior() : null;
            if (interiorRoot == null)
            {
                interiorRoot = CreateProceduralInterior();
                interiorRoot.transform.position = VanCabin.Origin;
                interiorRoot.transform.rotation = Quaternion.identity;
                interiorRoot.transform.localScale = Vector3.one * VanCabin.Scale;
            }
            DontDestroyOnLoad(interiorRoot);

            foreach (Collider c in interiorRoot.GetComponentsInChildren<Collider>())
                c.enabled = false;

            AddTransitInteriorLights();
            AddDispatchRadio();
        }
        cabinShown = true;
    }

    // Quiet dispatch-radio bed inside the cabin — static hiss with the occasional
    // squelch, the office keeping half an ear on its only van.
    void AddDispatchRadio()
    {
        var radioGo = new GameObject("DispatchRadio");
        radioGo.transform.SetParent(interiorRoot.transform, false);
        radioSource = radioGo.AddComponent<AudioSource>();
        radioSource.clip = SynthAudio.RadioStatic("synth_radio_static");
        radioSource.loop = true;
        radioSource.spatialBlend = 0f; // cabin is a local room; keep it as a bed
        radioSource.volume = 0.35f;
        radioSource.Play();
        radioSquelchClip = SynthAudio.RadioSquelch("synth_radio_squelch");
    }

    // Loads the modeled interior and AUTO-FITS it from measured bounds — scale and offset are
    // computed, never hand-tuned. Returns null if the asset isn't present. See the
    // unity-model-fit skill for the methodology.
    GameObject TryCreateModeledInterior()
    {
        GameObject prefab = Resources.Load<GameObject>(VanCabin.InteriorResourcePath);
        if (prefab == null) return null;

        GameObject root = Instantiate(prefab);
        root.name = "MVP_VanTransitInterior_Modeled";
        FitToCabin(root);
        return root;
    }

    // Measure → compute → place. Uniformly scales the model so its bounding box fits the cabin
    // bay, then translates so the footprint is centred and the model's floor sits at seat height.
    static void FitToCabin(GameObject root)
    {
        root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(VanCabin.ModelEuler));
        root.transform.localScale = Vector3.one;

        if (!TryGetWorldBounds(root, out Bounds b))
        {
            root.transform.position = VanCabin.InteriorCenter;
            return;
        }

        Vector3 target = VanCabin.InteriorSize;
        float scale = Mathf.Min(
            target.x / Mathf.Max(b.size.x, 1e-4f),
            target.y / Mathf.Max(b.size.y, 1e-4f),
            target.z / Mathf.Max(b.size.z, 1e-4f));
        root.transform.localScale = Vector3.one * scale;

        TryGetWorldBounds(root, out b); // re-measure after scaling
        Vector3 c = VanCabin.InteriorCenter;
        root.transform.position += new Vector3(c.x - b.center.x, VanCabin.FloorWorldY - b.min.y, c.z - b.center.z);
        Debug.Log($"[VanCabin] Auto-fit modeled interior: scale={scale:F3} fittedSize={b.size}");
    }

    static bool TryGetWorldBounds(GameObject root, out Bounds bounds)
    {
        bounds = default;
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return false;
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return true;
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
        card = Card.None;
        drivePending = false;
        signHold = 0f;
        transitDuration = 0f;
        transitStartTime = 0f;
        driveRequestedAt = float.PositiveInfinity;
        squelchPlayed = false;
        doorFullyOpen = false;
        disembarked = false;
        rearDoor = null;
        arrivalGlow = null;
        arrivalLight = null;
        // The ride is over: the settlement card retires with the cabin (ledger takes over).
        SettlementCardOverlay.NotifyDisembarked();
    }

    // ─── Per-frame state ───

    void Update()
    {
        if (!cabinShown) return;

        PlayerController local = FindLocalPlayer();
        RefreshSeatedCounts();
        UpdateCardLifecycle(local);

        if (phase == Phase.Transit) UpdateTransitGate();
        else if (phase == Phase.Arrived) UpdateArrival();

        HandleInput(local);
    }

    // Client-safe seat census: every peer has all player objects, and SeatIndex /
    // IsDowned are Everyone-readable NetworkVariables.
    void RefreshSeatedCounts()
    {
        seatedCount = 0;
        livingCount = 0;
        foreach (var pc in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
        {
            if (pc.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
                continue;
            livingCount++;
            if (pc.IsSeated) seatedCount++;
        }
        // Never require more seated than the cabin holds (over-capacity lobby can't block).
        neededCount = Mathf.Max(1, Mathf.Min(livingCount, VanCabin.Count));
        allSeated = livingCount > 0 && seatedCount >= neededCount;
    }

    void UpdateCardLifecycle(PlayerController local)
    {
        float now = Time.unscaledTime;

        if (drivePending && now - signedAt >= StampBeatSeconds)
        {
            drivePending = false;
            card = Card.None;
            BeginDrive(pendingDriveDuration);
            return;
        }

        if (phase == Phase.Transit || phase == Phase.Arrived || drivePending) return;
        if (card == Card.EarlyReturn || card == Card.DispatchSigned) return;

        bool localSeated = local != null && local.IsSeated;
        bool terminal = TowerMissionManager.Instance != null && TowerMissionManager.Instance.IsTerminalState;

        // 全员就位才弹中央派车单 (PM 锁定); the card folds again if someone steps off.
        if (card == Card.None && allSeated && localSeated && !terminal)
        {
            card = Card.Dispatch;
            cardShownAt = now;
        }
        else if (card == Card.Dispatch && (!allSeated || !localSeated || terminal))
        {
            card = Card.None;
        }
    }

    void UpdateTransitGate()
    {
        bool timeDone = Time.unscaledTime - transitStartTime >= transitDuration;
        if (timeDone && DestinationSceneReady)
            StartArrival();
    }

    void StartArrival()
    {
        phase = Phase.Arrived;
        arrivalStartedAt = Time.unscaledTime;
        // 到站时序 (PM 锁定): 引擎声收束熄火 → 0.5s 静默+squelch → 门缝渗光 → 门全开。
        AudioManager.Instance?.StopEngineIdle();
    }

    void UpdateArrival()
    {
        float t = Time.unscaledTime - arrivalStartedAt;

        if (!squelchPlayed && t >= SquelchAtSeconds)
        {
            squelchPlayed = true;
            if (radioSource != null && radioSquelchClip != null)
                radioSource.PlayOneShot(radioSquelchClip, 0.8f);
        }

        if (t >= DoorCrackAtSeconds)
        {
            EnsureArrivalDoorPieces();
            float crack = Mathf.Clamp01((t - DoorCrackAtSeconds) / 0.3f);
            float open = Mathf.Clamp01((t - DoorOpenAtSeconds) / DoorOpenSeconds);
            if (rearDoor != null)
                rearDoor.localPosition = rearDoorClosedPos + new Vector3(0f, 0f, 0.08f * crack + 1.26f * open);
            if (arrivalLight != null)
                arrivalLight.intensity = Mathf.Lerp(0f, 5.5f, Mathf.Max(crack * 0.25f, open));
            if (open >= 1f && !doorFullyOpen)
            {
                doorFullyOpen = true;
                doorFullyOpenAt = Time.unscaledTime;
            }
        }

        // Edge: the local player was never seated (offline oddities) — door's open, nobody
        // to disembark; retire the cabin so it doesn't linger forever.
        if (doorFullyOpen && !disembarked)
        {
            PlayerController local = FindLocalPlayer();
            if ((local == null || !local.IsSeated) && Time.unscaledTime - doorFullyOpenAt > 1f)
                TeardownCabin();
        }
    }

    // Door dressing built lazily on arrival: the new scene's light leaks through the gap —
    // 光色即地点签名 (HQ=钨丝暖 / 塔楼=阴天钢灰 #B8C4CE, spec Transitions).
    void EnsureArrivalDoorPieces()
    {
        if (arrivalGlow != null || interiorRoot == null) return;

        rearDoor = interiorRoot.transform.Find("Interior_WallRear");
        if (rearDoor != null) rearDoorClosedPos = rearDoor.localPosition;

        bool atOffice = SceneManager.GetActiveScene().name.Contains("HQ");
        Color glow = atOffice ? new Color(1f, 0.92f, 0.74f) : new Color(0.722f, 0.769f, 0.808f);

        arrivalGlow = GameObject.CreatePrimitive(PrimitiveType.Cube);
        arrivalGlow.name = "Interior_ArrivalGlow";
        Destroy(arrivalGlow.GetComponent<Collider>());
        arrivalGlow.transform.SetParent(interiorRoot.transform, false);
        arrivalGlow.transform.localPosition = new Vector3(1.52f, 0.92f, 0f);
        arrivalGlow.transform.localScale = new Vector3(0.012f, 0.56f, 1.36f);
        var mat = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color"));
        mat.color = glow;
        arrivalGlow.GetComponent<Renderer>().material = mat;

        var lightGo = new GameObject("ArrivalDoorLight");
        lightGo.transform.SetParent(interiorRoot.transform, false);
        lightGo.transform.localPosition = new Vector3(1.30f, 1.0f, 0f);
        arrivalLight = lightGo.AddComponent<Light>();
        arrivalLight.type = LightType.Point;
        arrivalLight.color = glow;
        arrivalLight.intensity = 0f;
        arrivalLight.range = 5f * VanCabin.Scale;
    }

    // ─── Input ───

    void HandleInput(PlayerController local)
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        bool seated = local != null && local.IsSeated;

        if (phase == Phase.Arrived)
        {
            // Door fully open → step off. The settlement card eats the first E to fold
            // itself; the same-frame guard covers either Update() running first.
            if (doorFullyOpen && !disembarked && seated &&
                keyboard.eKey.wasPressedThisFrame &&
                !SettlementCardOverlay.IsCardVisible && !SettlementCardOverlay.ConsumedCloseThisFrame)
                Disembark(local);
            return;
        }

        if (phase == Phase.Transit || drivePending || !seated) return;

        if (card == Card.EarlyReturn)
        {
            HandleEarlyReturnInput(keyboard);
            return;
        }

        if (card == Card.Dispatch && keyboard.spaceKey.wasPressedThisFrame && IsLocalHost())
        {
            HandleHostSignature(local);
            return;
        }

        // Leaving the seat is only allowed before the stamp falls.
        if (keyboard.xKey.wasPressedThisFrame)
            local.RequestLeaveSeat();
    }

    void HandleHostSignature(PlayerController local)
    {
        var mission = TowerMissionManager.Instance;
        if (mission != null && !mission.IsTerminalState)
        {
            if (mission.IsObjectiveAboard)
            {
                mission.RequestDepart(); // full delivery — Settle stamps the card via ShowReturn
            }
            else
            {
                // 未取柱: Space opens the application card instead of departing (防误触).
                card = Card.EarlyReturn;
                cardShownAt = Time.unscaledTime;
                signHold = 0f;
                estimatedPartialMoney = mission.EstimatePartialMoney();
                mission.SetFilingEarlyReturn(true);
            }
            return;
        }

        // Outbound from HQ — server validates "everyone aboard" before launching.
        var computer = Object.FindAnyObjectByType<OfficeComputer>();
        if (computer != null)
            computer.RequestDepart(local);
    }

    void HandleEarlyReturnInput(Keyboard keyboard)
    {
        var mission = TowerMissionManager.Instance;

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            // 撤回: back to the waiting dispatch card; can be re-opened any number of times.
            card = Card.Dispatch;
            cardShownAt = Time.unscaledTime;
            signHold = 0f;
            mission?.SetFilingEarlyReturn(false);
            return;
        }

        if (keyboard.eKey.isPressed)
            signHold += Time.unscaledDeltaTime;
        else
            signHold = Mathf.Max(0f, signHold - Time.unscaledDeltaTime * SignDecayRate);

        if (signHold >= SignHoldSeconds)
        {
            signHold = 0f;
            card = Card.None;
            AudioManager.Instance?.PlayStamp(); // 落章音 — the signature takes effect
            mission?.SetFilingEarlyReturn(false);
            mission?.RequestDepart(confirmedPartial: true);
        }
    }

    void Disembark(PlayerController local)
    {
        disembarked = true;

        Vector3 position;
        Quaternion rotation;
        if (hasArrivalSpawn)
        {
            position = arrivalSpawnPos;
            rotation = arrivalSpawnRot;
        }
        else
        {
            // No spawn manager in this scene (e.g. the tower): use the scene-safe position,
            // fanned out by seat so the crew doesn't stack on one point.
            int seat = Mathf.Max(0, local.SeatIndex.Value);
            rotation = Quaternion.identity;
            position = local.SceneSafePosition + Vector3.right * (seat * 1.2f);
        }

        // RestoreControlAt clears the seat; the suppressed ExitSeat path hides the cabin.
        local.RestoreControlAt(position, rotation);
    }

    static bool IsLocalHost()
    {
        NetworkManager network = NetworkManager.Singleton;
        return network == null || !network.IsListening || network.IsHost;
    }

    static PlayerController FindLocalPlayer()
    {
        var all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in all)
            if (p.IsOwner) return p;
        return null;
    }

    // ─── Paper surfaces (ticket strip + cards) ───

    void OnGUI()
    {
        if (!cabinShown) return;
        EnsureStyles();
        if (headingStyle == null) return;

        DrawTicketStrip();

        switch (card)
        {
            case Card.Dispatch: DrawDispatchCard(false); break;
            case Card.DispatchSigned: DrawDispatchCard(true); break;
            case Card.EarlyReturn: DrawEarlyReturnCard(); break;
        }
    }

    // 派遣票据条: aged-paper slip with a civic-teal rule and a stamp-red seal — the same
    // civic-paperwork grammar as the tower's identity dressing. Carries the whole journey:
    // 就位 N/M → 在途里程 → 已抵达.
    void DrawTicketStrip()
    {
        float w = 520f;
        var rect = new Rect((Screen.width - w) * 0.5f, 24f, w, 60f);
        var ticket = new Rect(rect.x - 12f, rect.y - 6f, rect.width + 24f, rect.height + 18f);
        GUI.DrawTexture(new Rect(ticket.x - 2f, ticket.y - 2f, ticket.width + 4f, ticket.height + 4f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.Shadow));
        GUI.DrawTexture(ticket, BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.OldPaper));
        GUI.DrawTexture(new Rect(ticket.x, ticket.y, ticket.width, 3f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreen));
        GUI.DrawTexture(new Rect(ticket.x, ticket.yMax - 2f, ticket.width, 2f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreenDim));
        GUI.DrawTexture(new Rect(ticket.x + 8f, ticket.y + 10f, 14f, 14f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.RustWarning)); // 章

        string header = string.IsNullOrEmpty(taskTitle)
            ? MvpLocale.T("van_cabin")
            : $"{taskTitle}  ·  {locationName}";
        GUI.Label(rect, header, headingStyle);

        var statusRect = new Rect(rect.x, rect.y + 30f, w, 22f);
        if (phase == Phase.Transit)
        {
            DrawTransitProgress(rect, w);
        }
        else if (phase == Phase.Arrived)
        {
            string line = doorFullyOpen ? "已抵达    [E] 下车" : "已抵达";
            GUI.Label(statusRect, line, smallStyle);
            DrawProgressBar(rect, w,
                Mathf.Lerp(ProgressCap, 1f, Mathf.Clamp01((Time.unscaledTime - arrivalStartedAt) / ArrivalFillSeconds)));
        }
        else
        {
            string status = allSeated
                ? MvpLocale.T("all_aboard", seatedCount, neededCount)
                : MvpLocale.T("waiting_team", seatedCount, neededCount);
            GUI.Label(statusRect, status + "    " + MvpLocale.T("press_x_leave"), smallStyle);
        }

        // 队友同步行: the host is filling the early-return application right now.
        var mission = TowerMissionManager.Instance;
        if (mission != null && mission.HostFilingEarlyReturn.Value && card != Card.EarlyReturn)
        {
            var subStrip = new Rect(ticket.x + 24f, ticket.yMax + 4f, ticket.width - 48f, 22f);
            GUI.DrawTexture(subStrip, BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.OldPaper));
            GUI.Label(new Rect(subStrip.x, subStrip.y + 2f, subStrip.width, 18f),
                "房主正在填写提前收工申请…", smallStyle);
        }
    }

    // En-route ink bar: uniform 0→92% over the minimum transit; if the scene is still
    // loading past that, the bar breathes at the cap and reads 「即将抵达」 — never "加载".
    void DrawTransitProgress(Rect rect, float w)
    {
        float now = Time.unscaledTime;
        float raw = (now - transitStartTime) / transitDuration;

        string line;
        float displayed;
        if (raw >= 1f && !DestinationSceneReady)
        {
            displayed = ProgressCap + 0.02f * Mathf.Sin(now * 2.4f);
            line = "即将抵达…";
        }
        else
        {
            displayed = Mathf.Clamp01(raw) * ProgressCap;
            int dots = 1 + (int)(now * 2f) % 3;
            int eta = Mathf.Max(0, Mathf.CeilToInt(transitDuration * (1f - Mathf.Clamp01(raw))));
            line = MvpLocale.T("dispatch_outbound") + new string('.', dots) +
                   $"    {Mathf.RoundToInt(displayed * 100f)}%    ~{eta}s";
        }

        GUI.Label(new Rect(rect.x, rect.y + 24f, w, 20f), line, smallStyle);
        DrawProgressBar(rect, w, displayed);
    }

    // Progress = a civic-teal fill drawn on the paper slip (ink on a form, not a
    // glowing bar — CRT green stays on actual screens).
    void DrawProgressBar(Rect rect, float w, float fill01)
    {
        float barY = rect.y + 46f;
        var bg = new Rect(rect.x, barY, w, 9f);
        GUI.DrawTexture(bg, BlackCommissionUiTheme.MakeTex(new Color(0.18f, 0.17f, 0.13f, 0.30f)));
        GUI.DrawTexture(new Rect(bg.x, bg.y, Mathf.Clamp(w * fill01, 2f, w), 9f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreen));
    }

    // 派车单: the stamped dispatch card, pops when the whole living crew is aboard.
    void DrawDispatchCard(bool signed)
    {
        float w = 400f, h = 252f;
        Rect cardRect = SlideCardRect(w, h);
        DrawCardPaper(cardRect, "黑色委托事务所  ·  派 车 单");

        float y = cardRect.y + 64f;
        DrawCardRow(cardRect, ref y, "委托事项", taskTitle);
        DrawCardRow(cardRect, ref y, "目的地", locationName);
        DrawCardRow(cardRect, ref y, "乘员", allSeated ? $"{seatedCount}/{neededCount}  全员就位" : $"{seatedCount}/{neededCount}");

        // Stamp block, lower right: hollow ink 待签发 → red 已签发 slamming in (1.3→1.0).
        var stampCenter = new Vector2(cardRect.xMax - 90f, cardRect.yMax - 84f);
        if (signed)
        {
            float slam = Mathf.Clamp01((Time.unscaledTime - signedAt) / StampSlamSeconds);
            DrawStamp(stampCenter, "已签发", BlackCommissionUiTheme.RustWarning, stampStyle,
                Mathf.Lerp(1.3f, 1f, slam), Mathf.Lerp(0.4f, 0.92f, slam));
        }
        else
        {
            DrawStamp(stampCenter, "待签发", new Color(0.19f, 0.17f, 0.13f, 0.55f), stampPendingStyle, 1f, 0.55f);
        }

        string footer = signed ? "已签发 — 发车"
            : IsLocalHost() ? "[Space] 签发发车      [X] 离座"
            : "等待房主签发      [X] 离座";
        GUI.Label(new Rect(cardRect.x + 18f, cardRect.yMax - 32f, cardRect.width - 36f, 20f), footer, cardSublineStyle);
    }

    // 提前收工申请单: clause B-2 conversion preview + warnings + hold-E signature line.
    void DrawEarlyReturnCard()
    {
        var mission = TowerMissionManager.Instance;
        float completeness = mission != null ? mission.SyncedCompleteness.Value : 1f;
        float rejectAt = mission != null ? mission.RejectThreshold : 0.5f;
        bool rejectRisk = completeness < rejectAt;

        float w = 420f, h = rejectRisk ? 286f : 264f;
        Rect cardRect = SlideCardRect(w, h);
        DrawCardPaper(cardRect, "黑色委托事务所  ·  提前收工申请单");

        float y = cardRect.y + 62f;
        GUI.Label(new Rect(cardRect.x + 18f, y, cardRect.width - 36f, 20f),
            "目标未回收。按条款 B-2 折算：", cardRowStyle);
        y += 26f;

        // 条款式账目行 (settlement grammar): label … amount, clause fine print below.
        GUI.Label(new Rect(cardRect.x + 18f, y, cardRect.width - 160f, 22f), "提前收工折算", cardRowStyle);
        GUI.Label(new Rect(cardRect.xMax - 170f, y, 152f, 22f), $"预估实付 {estimatedPartialMoney}G", cardRowStyle);
        y += 24f;
        GUI.Label(new Rect(cardRect.x + 32f, y, cardRect.width - 64f, 16f),
            "· 按委托报酬 22% 折算，不低于慰问金", cardSublineStyle);
        y += 24f;

        GUI.Label(new Rect(cardRect.x + 18f, y, cardRect.width - 36f, 20f),
            "⚠ 签发后全队将随车返回事务所", cardWarnStyle);
        y += 22f;
        if (rejectRisk)
        {
            GUI.Label(new Rect(cardRect.x + 18f, y, cardRect.width - 36f, 20f),
                $"⚠ 完整度 {completeness:P0} — 低于 {rejectAt:P0} 客户拒收", cardWarnStyle);
            y += 22f;
        }

        // 签字栏: hold-E ink fill (1.2s); released ink drains back in 0.3s.
        y += 8f;
        GUI.Label(new Rect(cardRect.x + 18f, y, 120f, 22f), "签字栏 [按住 E]", cardRowStyle);
        var inkBg = new Rect(cardRect.x + 148f, y + 5f, cardRect.width - 184f, 12f);
        GUI.DrawTexture(inkBg, BlackCommissionUiTheme.MakeTex(new Color(0.18f, 0.17f, 0.13f, 0.30f)));
        float inkFill = Mathf.Clamp01(signHold / SignHoldSeconds);
        if (inkFill > 0f)
            GUI.DrawTexture(new Rect(inkBg.x, inkBg.y, Mathf.Max(2f, inkBg.width * inkFill), inkBg.height),
                BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreen));

        GUI.Label(new Rect(cardRect.x + 18f, cardRect.yMax - 32f, cardRect.width - 36f, 20f),
            "[Esc] 撤回申请", cardSublineStyle);
    }

    Rect SlideCardRect(float w, float h)
    {
        float age = Time.unscaledTime - cardShownAt;
        float slide = Mathf.Clamp01(age / CardSlideSeconds);
        float ease = 1f - (1f - slide) * (1f - slide);
        float targetY = (Screen.height - h) * 0.45f;
        return new Rect((Screen.width - w) * 0.5f, Mathf.Lerp(Screen.height, targetY, ease), w, h);
    }

    // Shared 盖章公文卡 body: shadow, aged paper, civic-teal header band (settlement grammar).
    void DrawCardPaper(Rect cardRect, string title)
    {
        GUI.depth = -90; // above the ticket strip, below the settlement card (-100)
        GUI.DrawTexture(new Rect(cardRect.x - 3f, cardRect.y - 3f, cardRect.width + 6f, cardRect.height + 6f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.Shadow));
        GUI.DrawTexture(cardRect, BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.OldPaper));
        var headerBand = new Rect(cardRect.x, cardRect.y, cardRect.width, 44f);
        GUI.DrawTexture(headerBand, BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreen));
        GUI.Label(new Rect(cardRect.x + 16f, cardRect.y + 12f, cardRect.width - 32f, 22f), title, cardHeaderStyle);
    }

    void DrawCardRow(Rect cardRect, ref float y, string label, string value)
    {
        GUI.Label(new Rect(cardRect.x + 18f, y, 92f, 22f), $"{label} ……", cardRowStyle);
        GUI.Label(new Rect(cardRect.x + 112f, y, cardRect.width - 130f, 22f), value, cardRowStyle);
        y += 28f;
    }

    void DrawStamp(Vector2 center, string label, Color color, GUIStyle style, float scale, float alpha)
    {
        Matrix4x4 prevMatrix = GUI.matrix;
        Color prevColor = GUI.color;
        GUIUtility.RotateAroundPivot(-8f, center);
        GUI.color = new Color(1f, 1f, 1f, alpha);

        float sw = 118f * scale, sh = 44f * scale;
        var box = new Rect(center.x - sw * 0.5f, center.y - sh * 0.5f, sw, sh);
        Texture2D frame = BlackCommissionUiTheme.MakeTex(color);
        GUI.DrawTexture(new Rect(box.x, box.y, box.width, 3f), frame);
        GUI.DrawTexture(new Rect(box.x, box.yMax - 3f, box.width, 3f), frame);
        GUI.DrawTexture(new Rect(box.x, box.y, 3f, box.height), frame);
        GUI.DrawTexture(new Rect(box.xMax - 3f, box.y, 3f, box.height), frame);
        GUI.Label(box, label, style);

        GUI.color = prevColor;
        GUI.matrix = prevMatrix;
    }

    void EnsureStyles()
    {
        if (headingStyle != null) return;
        if (GUI.skin == null || GUI.skin.label == null) return;

        // Ink on paper (the ticket background is aged paper, so text is dark ink).
        Color ink = new Color(0.10f, 0.095f, 0.075f, 1f);
        Color inkSoft = new Color(0.19f, 0.17f, 0.13f, 0.9f);

        headingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = ink }
        };
        smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = inkSoft }
        };
        // Sizes match SettlementCardOverlay exactly — all civic cards share one type scale.
        cardHeaderStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.OldPaper }
        };
        cardRowStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = ink } };
        cardSublineStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = inkSoft } };
        // Warnings carry the ⚠ glyph, not just the red, for color-independent reading.
        cardWarnStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.RustWarning }
        };
        stampStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = BlackCommissionUiTheme.RustWarning }
        };
        stampPendingStyle = new GUIStyle(stampStyle)
        {
            normal = { textColor = new Color(0.19f, 0.17f, 0.13f, 0.7f) }
        };
        MvpFontProvider.ApplyToStyle(headingStyle);
        MvpFontProvider.ApplyToStyle(smallStyle);
        MvpFontProvider.ApplyToStyle(cardHeaderStyle);
        MvpFontProvider.ApplyToStyle(cardRowStyle);
        MvpFontProvider.ApplyToStyle(cardSublineStyle);
        MvpFontProvider.ApplyToStyle(cardWarnStyle);
        MvpFontProvider.ApplyToStyle(stampStyle);
        MvpFontProvider.ApplyToStyle(stampPendingStyle);
    }

    // ─── Procedural enclosed cabin (no windows) ───

    GameObject CreateProceduralInterior()
    {
        var root = new GameObject("MVP_VanTransitInterior_Procedural");

        // Textured (procedurally grimed) materials for the big surfaces so the cabin reads as
        // worn painted metal instead of flat plastic cubes; small props stay flat-shaded.
        // Colors match the tower's V8 whitebox palette exactly (TowerV8WhiteboxBuilder
        // EnsureMaterials) so the van and the map read as the same world: civic teal paint
        // #3F5F5C, dark steel #4A4845 / #2A2826, aged paper #D6CCAE, stamp red #C23A2B.
        Material wallMat = MakeGrimeMaterial(new Color(0.247f, 0.373f, 0.361f), 0.26f, 5f);   // V8_Civic_TealPaint
        Material metalMat = MakeGrimeMaterial(new Color(0.290f, 0.282f, 0.271f), 0.32f, 7f);  // V8_Steel_Dark
        Material benchMat = MakeGrimeMaterial(new Color(0.165f, 0.157f, 0.149f), 0.28f, 6f);  // tray steel, darker
        Material blackMat = MakeFlatMaterial(new Color(0.035f, 0.04f, 0.037f));
        Material tungstenMat = MakeFlatMaterial(BlackCommissionUiTheme.OldPaper);

        CreateInteriorBox("Floor", root.transform, new Vector3(0.45f, 0.36f, 0f), new Vector3(2f, 0.02f, 1.36f), metalMat);
        CreateInteriorBox("Ceiling", root.transform, new Vector3(0.45f, 1.44f, 0f), new Vector3(2f, 0.02f, 1.36f), metalMat);
        CreateInteriorBox("WallL", root.transform, new Vector3(0.45f, 0.92f, -0.68f), new Vector3(2f, 0.56f, 0.02f), wallMat);
        CreateInteriorBox("WallR", root.transform, new Vector3(0.45f, 0.92f, 0.68f), new Vector3(2f, 0.56f, 0.02f), wallMat);
        // Front (cab bulkhead) and rear doors — fully enclosed, no windows. The rear wall is
        // the arrival door: UpdateArrival slides it aside to let the new scene's light in.
        CreateInteriorBox("WallFront", root.transform, new Vector3(-0.55f, 0.92f, 0f), new Vector3(0.02f, 0.56f, 1.36f), wallMat);
        CreateInteriorBox("WallRear", root.transform, new Vector3(1.45f, 0.92f, 0f), new Vector3(0.02f, 0.56f, 1.36f), wallMat);

        CreateInteriorBox("BenchL", root.transform, new Vector3(0.50f, 0.48f, -0.52f), new Vector3(1.5f, 0.04f, 0.18f), benchMat);
        CreateInteriorBox("BenchBackL", root.transform, new Vector3(0.50f, 0.80f, -0.64f), new Vector3(1.5f, 0.30f, 0.04f), benchMat);
        CreateInteriorBox("BenchR", root.transform, new Vector3(0.50f, 0.48f, 0.52f), new Vector3(1.5f, 0.04f, 0.18f), benchMat);
        CreateInteriorBox("BenchBackR", root.transform, new Vector3(0.50f, 0.80f, 0.64f), new Vector3(1.5f, 0.30f, 0.04f), benchMat);

        CreateInteriorBox("Light", root.transform, new Vector3(0.45f, 1.39f, 0f), new Vector3(0.72f, 0.012f, 0.025f), tungstenMat);

        // Civic-paperwork detail layer (same identity grammar as the tower's
        // IdentityDressing: aged paper + stamp red on paper/signage only).
        Material paperMat = MakeFlatMaterial(new Color(0.839f, 0.800f, 0.682f));  // V8_Paper_Aged
        Material debtMat = MakeFlatMaterial(new Color(0.761f, 0.227f, 0.169f));   // V8_Stamp_Red
        Material grimeMat = MakeFlatMaterial(new Color(0.10f, 0.10f, 0.095f));

        CreateInteriorBox("SafetyNotice", root.transform,
            new Vector3(0.35f, 0.95f, -0.66f), new Vector3(0.32f, 0.22f, 0.01f), paperMat);
        CreateInteriorBox("SafetyNoticeStamp", root.transform,
            new Vector3(0.42f, 0.88f, -0.655f), new Vector3(0.1f, 0.06f, 0.008f), debtMat);
        CreateInteriorBox("NoSmokingSign", root.transform,
            new Vector3(0.72f, 1.05f, 0.665f), new Vector3(0.18f, 0.12f, 0.01f), debtMat);
        // Bulkhead: company plate in paper + a single small dispatch-green status lamp
        // (CRT green is restricted to screens/lamps per the art bible — no glowing bars).
        CreateInteriorBox("CompanyPlate", root.transform,
            new Vector3(-0.54f, 1.18f, 0f), new Vector3(0.01f, 0.10f, 0.48f), paperMat);
        Material lampMat = MakeFlatMaterial(BlackCommissionUiTheme.CrtGreenDim);
        CreateInteriorBox("DispatchLamp", root.transform,
            new Vector3(-0.54f, 1.05f, 0.20f), new Vector3(0.012f, 0.03f, 0.03f), lampMat);
        CreateInteriorBox("FloorGrimeA", root.transform,
            new Vector3(0.55f, 0.372f, -0.22f), new Vector3(0.35f, 0.005f, 0.28f), grimeMat);
        CreateInteriorBox("FloorGrimeB", root.transform,
            new Vector3(0.85f, 0.372f, 0.32f), new Vector3(0.22f, 0.005f, 0.18f), grimeMat);

        // Grab rail + poles down the aisle — sells the "standing transit van" read.
        Material railMat = MakeFlatMaterial(BlackCommissionUiTheme.Text);
        CreateInteriorBox("GrabRail", root.transform,
            new Vector3(0.45f, 1.33f, 0f), new Vector3(1.85f, 0.03f, 0.03f), railMat);
        CreateInteriorBox("GrabPoleFront", root.transform,
            new Vector3(-0.30f, 0.9f, 0f), new Vector3(0.035f, 0.95f, 0.035f), railMat);
        CreateInteriorBox("GrabPoleRear", root.transform,
            new Vector3(1.20f, 0.9f, 0f), new Vector3(0.035f, 0.95f, 0.035f), railMat);

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

    // Painted-metal look: a Perlin-noise grime/streak texture tinted toward `color`. `grime`
    // is how dark the dirt gets (0..1) and `tiling` how often it repeats across a surface.
    static Material MakeGrimeMaterial(Color color, float grime, float tiling)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard"));
        Texture2D tex = MakeGrimeTexture(color, grime);

        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", tex);
            mat.SetTextureScale("_BaseMap", new Vector2(tiling, tiling));
            mat.SetColor("_BaseColor", Color.white);
        }
        else
        {
            mat.mainTexture = tex;
            mat.mainTextureScale = new Vector2(tiling, tiling);
        }
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.12f);
        mat.color = Color.white;
        return mat;
    }

    static Texture2D MakeGrimeTexture(Color baseColor, float grime)
    {
        // Lo-fi rules (style-lock v2): small texture, Point filter, no mips — the chunky
        // texel grain is the feature and matches the tower's 256px/Point material language.
        const int size = 64;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Point
        };
        // Two octaves of value noise + faint vertical streaking for a used, dripped-on feel.
        float seed = Random.value * 100f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (float)x / size, v = (float)y / size;
                float n = Mathf.PerlinNoise(seed + u * 6f, seed + v * 6f) * 0.65f
                        + Mathf.PerlinNoise(seed + u * 18f, seed + v * 18f) * 0.35f;
                float streak = Mathf.PerlinNoise(seed + u * 3f, v * 22f) * 0.5f;
                float dirt = Mathf.Clamp01((n * 0.7f + streak * 0.3f));
                float shade = Mathf.Lerp(1f - grime, 1f, dirt);
                Color c = baseColor * shade;
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
        }
        tex.Apply(true);
        return tex;
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
