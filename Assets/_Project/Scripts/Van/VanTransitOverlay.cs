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

    // Transit progress: set when the drive begins so the HUD can show a moving "arrival progress" bar
    // that matches the dispatch duration the OfficeComputer / mission manager is counting down.
    float transitDuration;
    float transitStartTime;

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
        current.BeginDrive(durationSeconds);
    }

    public static void ShowReturn(string title, string location, float durationSeconds)
    {
        EnsureInstance();
        current.taskTitle = Resolve(title, "office");
        current.locationName = Resolve(location, "office");
        current.BeginDrive(durationSeconds);
    }

    public static void StartDeparture(float transitSeconds)
    {
        if (current != null) current.BeginDrive(transitSeconds);
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
        }
        cabinShown = true;
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

    void BeginDrive(float durationSeconds = 0f)
    {
        ShowCabin();
        if (phase != Phase.Transit)
        {
            phase = Phase.Transit;
            transitDuration = Mathf.Max(0f, durationSeconds);
            transitStartTime = Time.unscaledTime;
            AudioManager.Instance?.PlayEngineStart(Vector3.zero);
            AudioManager.Instance?.PlayEngineIdle();
        }
    }

    // 0→1 over the dispatch duration; clamped so a missing/zero duration just reads "arriving".
    float TransitProgress01()
    {
        if (transitDuration <= 0f) return 0f;
        return Mathf.Clamp01((Time.unscaledTime - transitStartTime) / transitDuration);
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
        transitDuration = 0f;
        transitStartTime = 0f;
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
        var missionManager = TowerMissionManager.Instance;
        if (missionManager != null)
        {
            missionManager.RequestDepart();
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

        // 派遣单 ticket: aged-paper slip with a civic-teal rule and a stamp-red seal —
        // the same civic-paperwork grammar as the tower's identity dressing.
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

        if (phase == Phase.Transit)
        {
            DrawTransitProgress(rect, w);
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

        }
    }

    // En-route HUD: a cycling "dispatch in transit..." line plus a fill bar that tracks the dispatch
    // countdown (or a pulsing indeterminate sweep if no duration was supplied).
    void DrawTransitProgress(Rect rect, float w)
    {
        float progress = TransitProgress01();

        int dots = 1 + (int)(Time.unscaledTime * 2f) % 3;
        string line = MvpLocale.T("dispatch_outbound") + new string('.', dots);
        if (transitDuration > 0f)
        {
            int eta = Mathf.Max(0, Mathf.CeilToInt(transitDuration * (1f - progress)));
            line += $"    {Mathf.RoundToInt(progress * 100f)}%    ~{eta}s";
        }
        GUI.Label(new Rect(rect.x, rect.y + 24f, w, 20f), line, smallStyle);

        // Progress = a civic-teal fill drawn on the paper slip (ink on a form, not a
        // glowing bar — CRT green stays on actual screens).
        float barY = rect.y + 46f;
        var bg = new Rect(rect.x, barY, w, 9f);
        GUI.DrawTexture(bg, BlackCommissionUiTheme.MakeTex(new Color(0.18f, 0.17f, 0.13f, 0.30f)));

        float fillW = transitDuration > 0f
            ? w * progress
            : w * (0.42f + 0.30f * Mathf.Sin(Time.unscaledTime * 3f));   // indeterminate sweep
        GUI.DrawTexture(new Rect(bg.x, bg.y, Mathf.Clamp(fillW, 2f, w), 9f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreen));
    }

    void EnsureStyles()
    {
        if (headingStyle != null) return;
        if (GUI.skin == null || GUI.skin.label == null) return;

        // Ink on paper (the ticket background is aged paper, so text is dark ink).
        headingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.10f, 0.095f, 0.075f, 1f) }
        };
        smallStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.19f, 0.17f, 0.13f, 0.9f) }
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
        // Front (cab bulkhead) and rear doors — fully enclosed, no windows.
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
