using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// UGUI canvas-based main menu. Builds its own hierarchy procedurally so there is no
/// scene YAML editing required. Replaces the legacy OnGUI QuickNetworkUI for new
/// installations; QuickNetworkUI hides itself when this is present in the scene.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    enum MenuState { Main, JoinInput, Connecting, HostWaiting }

    public static bool IsGameplayInputBlockedByMenu { get; private set; }
    public static bool IsMenuVisible { get; private set; }

    // ─── Color tokens (mirrors art bible) ─────────────────────────────────
    static readonly Color CivicTeal = BlackCommissionUiTheme.MilitaryGreen;
    static readonly Color DeepCivicTeal = BlackCommissionUiTheme.MilitaryGreenDark;
    static readonly Color PanelBg = BlackCommissionUiTheme.ConcreteBlack;
    static readonly Color PanelBorder = BlackCommissionUiTheme.MilitaryGreenDim;
    static readonly Color ScreenFog = BlackCommissionUiTheme.Shadow;
    static readonly Color DispatchGreen = BlackCommissionUiTheme.CrtGreen;
    static readonly Color DispatchGreenDark = BlackCommissionUiTheme.CrtGreenDim;
    static readonly Color StampRed = BlackCommissionUiTheme.RustWarning;
    static readonly Color SodiumAmber = BlackCommissionUiTheme.OldWood;
    static readonly Color AgedPaper = BlackCommissionUiTheme.OldPaper;
    static readonly Color DirtyBone = BlackCommissionUiTheme.Text;
    static readonly Color FieldBg = new(0.025f, 0.030f, 0.027f, 0.96f);
    static readonly Color BtnPrimary = BlackCommissionUiTheme.MilitaryGreenDark;
    static readonly Color BtnPrimaryHover = BlackCommissionUiTheme.MilitaryGreen;
    static readonly Color BtnPrimaryPressed = new(0.060f, 0.080f, 0.055f, 1f);
    static readonly Color BtnSecondary = BlackCommissionUiTheme.ConcreteRaised;
    static readonly Color BtnSecondaryHover = new(0.130f, 0.145f, 0.122f, 0.96f);
    static readonly Color BtnSecondaryPressed = BlackCommissionUiTheme.ConcretePanel;
    static readonly Color HintText = BlackCommissionUiTheme.PaperDim;

    // ─── State ────────────────────────────────────────────────────────────
    MenuState state = MenuState.Main;
    string joinCode = "";
    string hostJoinCode = "";
    string statusMessage = "";
    float statusMessageUntil;
    string connectAddress = "127.0.0.1";
    string connectPort = "7778";
    ushort lastHostPort = 7778;

    // ─── UI references (assigned during Build) ────────────────────────────
    Canvas rootCanvas;
    GameObject screenVeil;
    GameObject mainPanel;
    GameObject joinPanel;
    GameObject connectingPanel;
    GameObject hostCodePanel;
    GameObject settingsPanel;
    GameObject directConnectPanel;
    GameObject quitConfirmPanel;
    GameObject lobbyWaitingPanel;
    GameObject connectedStatusPanel;

    TMP_Text titleText;
    TMP_Text subtitleText;
    TMP_Text statusText;
    TMP_Text hostCodeText;
    TMP_Text connectingText;
    TMP_Text versionText;
    TMP_Text lobbyCodeText;
    TMP_Text lobbyCountText;
    TMP_Text lobbyHintText;
    TMP_Text lobbyTitleText;
    TMP_Text lobbyRoomCodeLabelText;
    RectTransform lobbyWaitingTerminalRt;

    TMP_InputField joinCodeInput;
    TMP_InputField directAddressInput;
    TMP_InputField directPortInput;
    TMP_InputField nameInput;
    TMP_Text characterNameText;
    readonly Button[] characterButtons = new Button[PlayerCharacterPalette.Count];
    readonly Image[] characterButtonImages = new Image[PlayerCharacterPalette.Count];
    readonly TMP_Text[] characterButtonLabels = new TMP_Text[PlayerCharacterPalette.Count];
    readonly Image[] characterUniformSwatches = new Image[PlayerCharacterPalette.Count];
    readonly Image[] characterVestSwatches = new Image[PlayerCharacterPalette.Count];
    readonly Image[] characterHelmetSwatches = new Image[PlayerCharacterPalette.Count];
    int lastCharacterSelectorIndex = -1;

    // Lobby roster (shown once connected): one row per player in the office.
    TMP_Text rosterHeaderText;
    readonly Image[] rosterSwatches = new Image[4];
    readonly TMP_Text[] rosterNames = new TMP_Text[4];
    readonly Button[] rosterMuteButtons = new Button[4];
    readonly TMP_Text[] rosterMuteLabels = new TMP_Text[4];
    readonly Button[] rosterKickButtons = new Button[4];
    readonly ulong[] rosterClientIds = new ulong[4];
    float nextRosterRefresh;

    readonly Image[] lobbySwatches = new Image[4];
    readonly TMP_Text[] lobbyNames = new TMP_Text[4];
    readonly TMP_Text[] lobbyRoles = new TMP_Text[4];
    // Own-row vest colour cycling (lobby.md: 色板可改+撞色独占, shown on your row only).
    readonly Button[] lobbyPrevBtns = new Button[4];
    readonly Button[] lobbyNextBtns = new Button[4];
    Button lobbyEnterBtn;
    TMP_Text lobbyEnterLabel;
    bool wasListening;
    bool lobbyWaitingDismissed;
    float nextLobbyRefresh;

    Image backgroundImage;
    bool usingBakedMenuArt;
    // World-space CRT carrier (main-menu.md): when present, the screen-space main
    // panel + baked art stay hidden and the menu rows live on the office CRT.
    CrtMenuStage crtStage;
    AudioSource menuAudioSource;
    AudioClip menuHoverClip;
    AudioClip menuSelectClip;

    Button continueBtn;
    Button newOfficeBtn;
    Button joinBtn;
    Button recordsBtn;
    Button settingsBtn;
    Button quitBtn;
    Button directBtn;
    Button joinSubmitBtn;
    Button joinCancelBtn;
    Button settingsQuitBtn;
    Button settingsCloseBtn;
    Button directCloseBtn;
    Button hostCodeHideBtn;
    Button quitConfirmYesBtn;
    Button quitConfirmNoBtn;

    void Awake()
    {
        SettingsOverlay.EnsureInstance();
        BuildEventSystemIfMissing();
        BuildHierarchy();
        BindEvents();
        SetState(MenuState.Main);

        // World-space CRT carrier PARKED (PM 2026-06-12: LC-style flat menu instead —
        // the 3D desk shot read ugly/blurry). Flip to true to revisit the experiment.
        const bool useCrtWorldStage = false;
#pragma warning disable CS0162
        if (useCrtWorldStage)
        {
            crtStage = CrtMenuStage.TryCreate(this);
            if (crtStage != null)
            {
                backgroundImage.gameObject.SetActive(false);
                screenVeil.SetActive(false);
                mainPanel.SetActive(false);
                if (versionText != null) versionText.gameObject.SetActive(false);
            }
        }
#pragma warning restore CS0162
    }

    void OnEnable()
    {
        SubscribeConnectionEvents();
    }

    void OnDisable()
    {
        UnsubscribeConnectionEvents();
        IsGameplayInputBlockedByMenu = false;
        IsMenuVisible = false;
    }

    void Update()
    {
        UpdateConnectedVisibility();
        UpdateStatusVisibility();
        RefreshCharacterSelector();
        HandleKeyboardShortcuts();
        UpdateGameplayInputBlock();
        UpdateMenuVisibilityFlag();
        UpdateResponsiveMenuPanels();
    }

    // ─── Layout build ─────────────────────────────────────────────────────

    void BuildEventSystemIfMissing()
    {
        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.transform.SetParent(transform, false);
        }
    }

    void BuildHierarchy()
    {
        rootCanvas = gameObject.GetComponent<Canvas>();
        if (rootCanvas == null) rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 100;

        var scaler = gameObject.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
            gameObject.AddComponent<GraphicRaycaster>();

        backgroundImage = BuildBackground(transform);
        screenVeil = BuildScreenVeil(transform);
        mainPanel = BuildMainPanel(transform);
        joinPanel = BuildJoinPanel(transform);
        connectingPanel = BuildConnectingPanel(transform);
        hostCodePanel = BuildHostCodePanel(transform);
        settingsPanel = BuildSettingsPanel(transform);
        directConnectPanel = BuildDirectConnectPanel(transform);
        quitConfirmPanel = BuildQuitConfirmPanel(transform);
        lobbyWaitingPanel = BuildLobbyWaitingPanel(transform);
        connectedStatusPanel = BuildConnectedStatusPanel(transform);

        versionText = AddText(transform, "Version", "ver 1.0.0", 12,
            BlackCommissionUiTheme.MutedText, TextAlignmentOptions.Center);
        var versionRt = versionText.rectTransform;
        versionRt.anchorMin = new Vector2(0f, 0f);
        versionRt.anchorMax = new Vector2(1f, 0f);
        versionRt.pivot = new Vector2(0.5f, 0f);
        versionRt.anchoredPosition = new Vector2(-902f, 16f);
        versionRt.sizeDelta = new Vector2(0f, 24f);
        // Retired: the mockup-B footer (FooterVersion in BuildTerminalMenu) owns the
        // version line now.
        versionText.gameObject.SetActive(false);
    }

    void UpdateResponsiveMenuPanels()
    {
        if (rootCanvas == null) return;

        var canvasRt = rootCanvas.GetComponent<RectTransform>();
        if (canvasRt == null) return;

        Vector2 canvasSize = canvasRt.rect.size;
        if (canvasSize.x <= 0f || canvasSize.y <= 0f) return;

        if (lobbyWaitingTerminalRt != null && lobbyWaitingTerminalRt.gameObject.activeInHierarchy)
        {
            float lobbyScale = Mathf.Min(canvasSize.x * 0.88f / 940f, canvasSize.y * 0.82f / 620f);
            lobbyScale = Mathf.Clamp(lobbyScale, 0.58f, 1.18f);
            lobbyWaitingTerminalRt.localScale = new Vector3(lobbyScale, lobbyScale, 1f);
        }
    }

    // Full-screen background art. Drop the menu mockup (ideally without the centre
    // panel baked in) at Assets/Resources/UI/MainMenuBg.png and it appears here; until
    // then it falls back to a flat dark fill so the menu still reads.
    Image BuildBackground(Transform parent)
    {
        var go = new GameObject("Background", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.raycastTarget = false;

        // REAL SCENERY (PM 2026-06-12): the menu lives in the HQ scene, so HQMenuCamera
        // already renders the actual 3D office behind this screen-space overlay. Instead
        // of painting flat blocks over it, this background is a horizontal scrim — opaque
        // dark on the LEFT (so the menu rows stay readable) fading to TRANSPARENT on the
        // RIGHT, revealing the real lit office. No baked art, no little boxes.
        usingBakedMenuArt = false;
        EnsureMenuBackdropCamera();
        var grad = new Texture2D(2, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        grad.SetPixel(0, 0, new Color(0.063f, 0.063f, 0.055f, 0.96f)); // left: near-opaque dark
        grad.SetPixel(1, 0, new Color(0.063f, 0.063f, 0.055f, 0f));    // right: transparent → office shows
        grad.Apply();
        image.sprite = Sprite.Create(grad, new Rect(0f, 0f, 2f, 1f), new Vector2(0.5f, 0.5f));
        image.color = Color.white;
        image.type = Image.Type.Simple;
        return image;
    }

    // Makes sure the HQ scene's backdrop camera is live + framed on the office while the
    // menu is up, so the real 3D office shows through the transparent side of the scrim.
    void EnsureMenuBackdropCamera() => SetMenuBackdropCamera(true);

    void SetMenuBackdropCamera(bool on)
    {
        var camGo = GameObject.Find("HQMenuCamera");
        if (camGo != null && camGo.TryGetComponent(out Camera cam))
            cam.enabled = on;
    }

    // Procedural dark-office backdrop (mockup 01): right wall, a warm tungsten lamp
    // pool, the desk CRT with a faint green glow, a filing cabinet, and a hanging
    // overdue debt notice. All runtime sprites — no baked art.
    void BuildOfficeBackdrop(Transform parent)
    {
        Color wall = new(0.118f, 0.118f, 0.106f, 1f);
        Color prop = new(0.086f, 0.082f, 0.071f, 1f);
        Color propDark = new(0.055f, 0.055f, 0.047f, 1f);

        // right wall block (anchored to centre, 1920x1080 space)
        AddRect(parent, "BackWall", new Vector2(430f, 0f), new Vector2(1060f, 1080f),
            wall, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        // warm tungsten lamp pool — the one warm source
        AddGlow(parent, "LampGlow", new Vector2(560f, -180f), 1040f,
            new Color(1f, 0.596f, 0.157f, 0.30f));

        // desk
        AddRect(parent, "Desk", new Vector2(560f, -380f), new Vector2(520f, 220f),
            new Color(0.090f, 0.078f, 0.063f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        // CRT monitor + faint green glow (the only green — electronics)
        AddGlow(parent, "CrtGlow", new Vector2(470f, -120f), 320f,
            new Color(0.424f, 1f, 0.373f, 0.14f));
        AddRect(parent, "CRT", new Vector2(470f, -120f), new Vector2(150f, 120f),
            propDark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        AddRect(parent, "CRTGlass", new Vector2(470f, -120f), new Vector2(126f, 84f),
            new Color(0.043f, 0.063f, 0.039f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        AddRect(parent, "CRTLine", new Vector2(470f, -110f), new Vector2(96f, 3f),
            new Color(0.180f, 0.360f, 0.165f, 0.8f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        // filing cabinet with drawer lines
        AddRect(parent, "Cabinet", new Vector2(755f, -230f), new Vector2(130f, 420f),
            prop, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        for (int i = 0; i < 3; i++)
            AddRect(parent, "Drawer" + i, new Vector2(755f, -120f - i * 110f), new Vector2(130f, 4f),
                propDark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        // hanging overdue debt notice (aged paper + stamp-red OVERDUE)
        var notice = AddRect(parent, "DebtNotice", new Vector2(255f, 142f), new Vector2(150f, 196f),
            new Color(0.839f, 0.800f, 0.682f, 0.32f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        notice.transform.localRotation = Quaternion.Euler(0f, 0f, 2f);
        var stamp = AddText(notice.transform, "OverdueStamp", "OVERDUE", 14,
            new Color(0.761f, 0.227f, 0.169f, 0.5f), TextAlignmentOptions.Center);
        stamp.fontStyle = FontStyles.Bold;
        stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -10f);
        Stretch(stamp.rectTransform, new Vector2(0.1f, 0.35f), new Vector2(0.9f, 0.6f));

        // mop / pole silhouette to break the symmetry
        AddRect(parent, "Pole", new Vector2(290f, -190f), new Vector2(7f, 380f),
            propDark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
    }

    // A soft radial glow as a runtime sprite (uGUI Image is rectangular; this gives a
    // round falloff for lamp / screen pools).
    void AddGlow(Transform parent, string name, Vector2 center, float diameter, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = RadialSprite();
        img.color = color;
        img.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = center;
        rt.sizeDelta = new Vector2(diameter, diameter);
    }

    static Sprite cachedRadial;
    static Sprite RadialSprite()
    {
        if (cachedRadial != null) return cachedRadial;
        const int n = 64;
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        Vector2 c = new(n / 2f, n / 2f);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), c) / (n / 2f);
                float a = Mathf.Clamp01(1f - d);
                a = a * a; // softer falloff
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        tex.Apply();
        cachedRadial = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        return cachedRadial;
    }

    GameObject BuildScreenVeil(Transform parent)
    {
        var go = new GameObject("ScreenVeil", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.color = usingBakedMenuArt ? new Color(0f, 0f, 0f, 0f) : new Color(0f, 0f, 0f, 0.10f);
        image.raycastTarget = false; // tints the screen only — must not block panel buttons
        return go;
    }

    GameObject BuildMainPanel(Transform parent)
    {
        // Transparent full-screen container so the background art shows through.
        var panel = new GameObject("MainPanel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero;
        prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero;
        prt.offsetMax = Vector2.zero;

        // ─── Title block (mockup 01: 3270 face, amber underline). English-first. ─
        titleText = AddText(panel.transform, "Title", "BLACK COMMISSION", 72,
            new Color(0.847f, 0.824f, 0.737f, 1f), TextAlignmentOptions.Left);
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 4f;
        var titleRt = titleText.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(120f, -120f);
        titleRt.sizeDelta = new Vector2(1100f, 90f);

        var underline = new GameObject("TitleUnderline", typeof(RectTransform), typeof(Image));
        underline.transform.SetParent(panel.transform, false);
        underline.GetComponent<Image>().color = new Color(1f, 0.596f, 0.157f, 1f); // tungsten amber
        underline.GetComponent<Image>().raycastTarget = false;
        var ulRt = underline.GetComponent<RectTransform>();
        ulRt.anchorMin = new Vector2(0f, 1f);
        ulRt.anchorMax = new Vector2(0f, 1f);
        ulRt.pivot = new Vector2(0f, 1f);
        ulRt.anchoredPosition = new Vector2(124f, -214f);
        ulRt.sizeDelta = new Vector2(620f, 5f);

        subtitleText = AddText(panel.transform, "Subtitle",
            "OUTSOURCED COMMISSION OFFICE", 22,
            new Color(0.604f, 0.569f, 0.482f, 1f), TextAlignmentOptions.Left);
        subtitleText.characterSpacing = 8f;
        var subRt = subtitleText.rectTransform;
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(0f, 1f);
        subRt.pivot = new Vector2(0f, 1f);
        subRt.anchoredPosition = new Vector2(124f, -280f);
        subRt.sizeDelta = new Vector2(1000f, 32f);

        // Loadout picker DELETED from the main screen (PM 2026-06-12) — vest colour
        // moves to the lobby roster card (and later the surveillance-room picker).
        // The office backdrop (BuildOfficeBackdrop) now fills the right of frame, so the
        // mockup-B van silhouette is retired here.

        // ─── Central commission terminal menu ──────────────────────────
        BuildTerminalMenu(panel.transform);

        // ─── Status message bar (bottom centre) ────────────────────────
        statusText = AddText(panel.transform, "StatusBar", "", 16,
            HintText, TextAlignmentOptions.Center);
        var statusRt = statusText.rectTransform;
        statusRt.anchorMin = new Vector2(0.5f, 0f);
        statusRt.anchorMax = new Vector2(0.5f, 0f);
        statusRt.pivot = new Vector2(0.5f, 0f);
        statusRt.anchoredPosition = new Vector2(0f, 60f);
        statusRt.sizeDelta = new Vector2(720f, 32f);

        return panel;
    }

    void BuildReferenceLeftColumn(Transform parent)
    {
        titleText = AddText(parent, "Logo", "BLACK\nCOMMISSION", 50,
            AgedPaper, TextAlignmentOptions.Left);
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 2f;
        titleText.lineSpacing = -18f;
        var titleRt = titleText.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(34f, -48f);
        titleRt.sizeDelta = new Vector2(330f, 110f);

        var tagBg = AddRect(parent, "MunicipalTagBg", new Vector2(36f, -146f), new Vector2(292f, 28f),
            new Color(0.31f, 0.34f, 0.25f, 0.82f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        tagBg.transform.SetAsFirstSibling();

        subtitleText = AddText(parent, "MunicipalTag", "MUNICIPAL CONTRACT SERVICES", 13,
            BlackCommissionUiTheme.OldPaper, TextAlignmentOptions.Center);
        subtitleText.fontStyle = FontStyles.Bold;
        subtitleText.characterSpacing = 1.5f;
        var subRt = subtitleText.rectTransform;
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(0f, 1f);
        subRt.pivot = new Vector2(0f, 1f);
        subRt.anchoredPosition = new Vector2(36f, -146f);
        subRt.sizeDelta = new Vector2(292f, 28f);

        var agent = AddText(parent, "AgentId", "Agent-499\nOFFICE ID: BC-114-A", 13,
            AgedPaper, TextAlignmentOptions.Left);
        agent.lineSpacing = 12f;
        var aRt = agent.rectTransform;
        aRt.anchorMin = new Vector2(0f, 1f);
        aRt.anchorMax = new Vector2(0f, 1f);
        aRt.pivot = new Vector2(0f, 1f);
        aRt.anchoredPosition = new Vector2(38f, -210f);
        aRt.sizeDelta = new Vector2(260f, 60f);

        BuildLedgerMetric(parent, 302f, "DEBT", "-12,450G", StampRed, "▣");
        BuildLedgerMetric(parent, 390f, "REPUTATION", "67", DispatchGreen, "◎");
        BuildLedgerMetric(parent, 478f, "OFFICE LEVEL", "LV.1", DispatchGreen, "⌂");
        BuildLedgerMetric(parent, 566f, "ACQUISITION RISK", "72%", StampRed, "◌");
        BuildLedgerMetric(parent, 654f, "LAST SETTLEMENT", "+120G", DispatchGreen, "+");
        nameInput = null;
    }

    void BuildLedgerMetric(Transform parent, float yFromTop, string label, string value, Color valueColor, string icon)
    {
        var iconText = AddText(parent, "MetricIcon_" + label, icon, 26, DispatchGreen, TextAlignmentOptions.Center);
        var iRt = iconText.rectTransform;
        iRt.anchorMin = new Vector2(0f, 1f);
        iRt.anchorMax = new Vector2(0f, 1f);
        iRt.pivot = new Vector2(0f, 1f);
        iRt.anchoredPosition = new Vector2(38f, -yFromTop);
        iRt.sizeDelta = new Vector2(38f, 34f);

        var labelText = AddText(parent, "MetricLabel_" + label, label, 12, DispatchGreen, TextAlignmentOptions.Left);
        labelText.characterSpacing = 1.2f;
        var lRt = labelText.rectTransform;
        lRt.anchorMin = new Vector2(0f, 1f);
        lRt.anchorMax = new Vector2(0f, 1f);
        lRt.pivot = new Vector2(0f, 1f);
        lRt.anchoredPosition = new Vector2(88f, -yFromTop + 2f);
        lRt.sizeDelta = new Vector2(210f, 20f);

        var valueText = AddText(parent, "MetricValue_" + label, value, 24, valueColor, TextAlignmentOptions.Left);
        valueText.fontStyle = FontStyles.Bold;
        var vRt = valueText.rectTransform;
        vRt.anchorMin = new Vector2(0f, 1f);
        vRt.anchorMax = new Vector2(0f, 1f);
        vRt.pivot = new Vector2(0f, 1f);
        vRt.anchoredPosition = new Vector2(88f, -yFromTop - 24f);
        vRt.sizeDelta = new Vector2(220f, 34f);
    }

    void BuildReferenceCrtMenu(Transform parent)
    {
        Vector2 screenTopLeft = new(408f, -248f);
        Vector2 screenSize = new(580f, 400f);
        var screenRoot = CreateScreenUiRoot(parent, screenTopLeft, screenSize);

        var frame = AddRect(screenRoot.transform, "MenuCanvas", Vector2.zero, screenSize,
            new Color(0.012f, 0.052f, 0.018f, 0.30f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        AddInsetFrame(frame.transform, "CrtInnerFrame", DispatchGreen, 7f, 2f);

        var header = AddText(frame.transform, "CrtHeader", "BLACK COMMISSION OS v1.0", 17,
            DispatchGreen, TextAlignmentOptions.Center);
        header.fontStyle = FontStyles.Bold;
        header.characterSpacing = 1f;
        header.rectTransform.anchoredPosition = new Vector2(0f, 181f);
        header.rectTransform.sizeDelta = new Vector2(540f, 28f);

        AddRect(frame.transform, "StatusDot", new Vector2(280f, 181f), new Vector2(16f, 16f),
            DispatchGreen, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        continueBtn = CreateReferenceMenuRow(frame.transform, "ContinueBtn", "Continue Office", "CONTINUE OFFICE", "▭", 112f, true);
        newOfficeBtn = CreateReferenceMenuRow(frame.transform, "NewOfficeBtn", "New Office", "NEW OFFICE", "◖", 52f, false);
        joinBtn = CreateReferenceMenuRow(frame.transform, "JoinBtn", "Join Office", "JOIN OFFICE", "●", -8f, false);
        AddRect(frame.transform, "CrtDivider", new Vector2(0f, -50f), new Vector2(540f, 2f),
            DispatchGreenDark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        recordsBtn = CreateReferenceMenuRow(frame.transform, "RecordsBtn", "Company Records", "RECORDS", "▧", -88f, false);
        settingsBtn = CreateReferenceMenuRow(frame.transform, "SettingsBtn", "Settings", "SETTINGS", "⚙", -151f, false);

        directBtn = CreateMenuHotspot(frame.transform, "DirectConnectHotspot", new Vector2(0f, -282f), new Vector2(540f, 34f));
        directBtn.gameObject.SetActive(false);

        BuildCrtOverlay(screenRoot.transform, screenSize);
        screenRoot.GetComponent<CrtScreenSkew>()?.Refresh();
    }

    GameObject CreateScreenUiRoot(Transform parent, Vector2 topLeft, Vector2 size)
    {
        var go = new GameObject("ScreenUIRoot", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = topLeft + new Vector2(size.x * 0.5f, -size.y * 0.5f);
        rt.sizeDelta = size;
        rt.localRotation = Quaternion.Euler(0f, 0f, -0.7f);
        rt.localScale = Vector3.one;

        var skew = go.AddComponent<CrtScreenSkew>();
        skew.BottomLeftOffsetX = 28f;
        skew.BottomRightOffsetX = 34f;
        skew.BottomLeftOffsetY = -22f;
        skew.BottomRightOffsetY = -4f;
        return go;
    }

    void BuildCrtOverlay(Transform parent, Vector2 screenSize)
    {
        var overlay = AddRect(parent, "CrtOverlay", Vector2.zero, screenSize,
            new Color(0f, 0f, 0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        overlay.GetComponent<Image>().raycastTarget = false;
        overlay.transform.SetAsLastSibling();

        var overlayTex = Resources.Load<Texture2D>("UI/CrtScreenOverlay");
        if (overlayTex != null)
        {
            var image = overlay.GetComponent<Image>();
            image.sprite = Sprite.Create(overlayTex,
                new Rect(0f, 0f, overlayTex.width, overlayTex.height),
                new Vector2(0.5f, 0.5f));
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = false;
            return;
        }

        AddRect(overlay.transform, "TopVignette", new Vector2(0f, 0f), new Vector2(screenSize.x, 42f),
            new Color(0f, 0f, 0f, 0.24f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        AddRect(overlay.transform, "BottomVignette", new Vector2(0f, 0f), new Vector2(screenSize.x, 50f),
            new Color(0f, 0f, 0f, 0.30f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        AddRect(overlay.transform, "LeftVignette", new Vector2(0f, 0f), new Vector2(38f, screenSize.y),
            new Color(0f, 0f, 0f, 0.22f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        AddRect(overlay.transform, "RightVignette", new Vector2(0f, 0f), new Vector2(42f, screenSize.y),
            new Color(0f, 0f, 0f, 0.26f), new Vector2(1f, 0f), new Vector2(1f, 0f));

        for (float y = 18f; y < screenSize.y - 10f; y += 18f)
        {
            AddRect(overlay.transform, "Scanline", new Vector2(0f, -y), new Vector2(screenSize.x, 1f),
                new Color(0f, 0f, 0f, 0.12f), new Vector2(0f, 1f), new Vector2(0f, 1f));
        }
    }

    Button CreateReferenceMenuRow(Transform parent, string name, string title, string desc, string icon, float y, bool primary)
    {
        Color normal = primary ? new Color(0.31f, 0.95f, 0.25f, 0.92f) : new Color(0f, 0f, 0f, 0f);
        Color hover = primary ? new Color(0.42f, 1f, 0.34f, 0.96f) : new Color(0.35f, 1f, 0.26f, 0.92f);
        Color pressed = primary ? new Color(0.18f, 0.72f, 0.16f, 1f) : new Color(0.08f, 0.22f, 0.08f, 0.90f);
        var btn = CreateButton(parent, name, "", 1, normal, hover, pressed);
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic.color = normal;
        btn.onClick.AddListener(PlayMenuSelect);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, y);
        rt.sizeDelta = new Vector2(540f, primary ? 58f : 54f);

        var emptyLabel = btn.GetComponentInChildren<TMP_Text>();
        if (emptyLabel != null) Destroy(emptyLabel.gameObject);

        Color titleColor = primary ? Color.black : DispatchGreen;
        Color subColor = primary ? new Color(0.08f, 0.18f, 0.08f, 1f) : BlackCommissionUiTheme.CrtGreenDim;

        var iconText = AddText(btn.transform, "Icon", icon, primary ? 29 : 26, titleColor, TextAlignmentOptions.Center);
        iconText.fontStyle = FontStyles.Bold;
        iconText.raycastTarget = false;
        iconText.rectTransform.anchoredPosition = new Vector2(-232f, 0f);
        iconText.rectTransform.sizeDelta = new Vector2(44f, 42f);

        var titleText = AddText(btn.transform, "Title", (primary ? "> " : "") + title, primary ? 24 : 23,
            titleColor, TextAlignmentOptions.Left);
        titleText.fontStyle = FontStyles.Bold;
        titleText.raycastTarget = false;
        titleText.rectTransform.anchoredPosition = new Vector2(35f, primary ? 8f : 7f);
        titleText.rectTransform.sizeDelta = new Vector2(340f, 28f);

        var descText = AddText(btn.transform, "Desc", desc, 13, subColor, TextAlignmentOptions.Left);
        descText.characterSpacing = 1f;
        descText.raycastTarget = false;
        descText.rectTransform.anchoredPosition = new Vector2(42f, primary ? -17f : -16f);
        descText.rectTransform.sizeDelta = new Vector2(340f, 20f);

        ConfigureReferenceMenuRowHover(btn, btn.targetGraphic as Image, iconText, titleText, descText,
            normal, hover, primary);
        return btn;
    }

    void ConfigureReferenceMenuRowHover(Button btn, Image image, TMP_Text icon, TMP_Text title,
        TMP_Text desc, Color normalBg, Color hoverBg, bool primary)
    {
        Color normalTitle = primary ? Color.black : DispatchGreen;
        Color normalDesc = primary ? new Color(0.08f, 0.18f, 0.08f, 1f) : BlackCommissionUiTheme.CrtGreenDim;
        Color darkText = Color.black;
        string plainTitle = title.text.TrimStart('>', ' ');
        Vector2 iconHome = icon.rectTransform.anchoredPosition;
        Vector2 titleHome = title.rectTransform.anchoredPosition;
        Vector2 descHome = desc.rectTransform.anchoredPosition;

        void Apply(bool hovered)
        {
            image.color = hovered ? hoverBg : normalBg;
            bool inverted = hovered || primary;
            icon.color = inverted ? darkText : normalTitle;
            title.color = inverted ? darkText : normalTitle;
            desc.color = inverted ? new Color(0.06f, 0.14f, 0.05f, 1f) : normalDesc;
            icon.rectTransform.anchoredPosition = iconHome + (hovered ? new Vector2(10f, 0f) : Vector2.zero);
            title.rectTransform.anchoredPosition = titleHome + (hovered ? new Vector2(10f, 0f) : Vector2.zero);
            desc.rectTransform.anchoredPosition = descHome + (hovered ? new Vector2(10f, 0f) : Vector2.zero);
            title.text = inverted ? "> " + plainTitle : plainTitle;
        }

        Apply(false);

        var trigger = btn.gameObject.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            PlayMenuHover();
            Apply(true);
        });
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => Apply(false));
        trigger.triggers.Add(exit);
    }

    void BuildReferenceJobStrip(Transform parent)
    {
        var root = AddRect(parent, "JobStrip", new Vector2(258f, 28f), new Vector2(900f, 118f),
            new Color(0.16f, 0.13f, 0.085f, 0.88f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        AddRect(root.transform, "PostedTab", new Vector2(16f, 92f), new Vector2(235f, 24f),
            new Color(0.10f, 0.30f, 0.10f, 0.88f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        var tab = AddText(root.transform, "PostedLabel", "NEW COMMISSION POSTED", 13, DispatchGreen, TextAlignmentOptions.Left);
        tab.fontStyle = FontStyles.Bold;
        tab.rectTransform.anchorMin = new Vector2(0f, 0f);
        tab.rectTransform.anchorMax = new Vector2(0f, 0f);
        tab.rectTransform.pivot = new Vector2(0f, 0f);
        tab.rectTransform.anchoredPosition = new Vector2(24f, 94f);
        tab.rectTransform.sizeDelta = new Vector2(250f, 22f);

        var details = AddText(root.transform, "JobDetails",
            "Client: Ms. Wang\nRequest: Recover son's homework notebook\nReward: 120G\nNote: Child says something was in the classroom after school.", 15,
            Color.black, TextAlignmentOptions.Left);
        details.fontStyle = FontStyles.Bold;
        details.rectTransform.anchorMin = new Vector2(0f, 0f);
        details.rectTransform.anchorMax = new Vector2(0f, 0f);
        details.rectTransform.pivot = new Vector2(0f, 0f);
        details.rectTransform.anchoredPosition = new Vector2(138f, 16f);
        details.rectTransform.sizeDelta = new Vector2(350f, 86f);

        AddRect(root.transform, "Photo", new Vector2(500f, 16f), new Vector2(245f, 86f),
            new Color(0.11f, 0.19f, 0.18f, 0.95f), new Vector2(0f, 0f), new Vector2(0f, 0f));
        var photoText = AddText(root.transform, "PhotoText", "SCHOOL HALLWAY PHOTO", 13,
            new Color(0.58f, 0.76f, 0.70f, 0.70f), TextAlignmentOptions.Center);
        photoText.rectTransform.anchorMin = new Vector2(0f, 0f);
        photoText.rectTransform.anchorMax = new Vector2(0f, 0f);
        photoText.rectTransform.pivot = new Vector2(0f, 0f);
        photoText.rectTransform.anchoredPosition = new Vector2(500f, 48f);
        photoText.rectTransform.sizeDelta = new Vector2(245f, 26f);

        var open = AddText(root.transform, "OpenTerminal", "Go to Commission Terminal  ›\nOPEN TERMINAL", 20,
            DispatchGreen, TextAlignmentOptions.Center);
        open.fontStyle = FontStyles.Bold;
        open.rectTransform.anchorMin = new Vector2(0f, 0f);
        open.rectTransform.anchorMax = new Vector2(0f, 0f);
        open.rectTransform.pivot = new Vector2(0f, 0f);
        open.rectTransform.anchoredPosition = new Vector2(760f, 28f);
        open.rectTransform.sizeDelta = new Vector2(210f, 62f);
    }

    void BuildCharacterSelector(Transform parent, bool referenceLayout)
    {
        Vector2 size = new(360f, 172f);
        var root = new GameObject("CharacterSelector", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        var image = root.GetComponent<Image>();
        image.color = new Color(0.015f, 0.018f, 0.016f, 0.88f);
        image.raycastTarget = false;

        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = referenceLayout ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        rt.anchorMax = rt.anchorMin;
        rt.pivot = referenceLayout ? new Vector2(0f, 1f) : new Vector2(1f, 1f);
        rt.anchoredPosition = referenceLayout ? new Vector2(1110f, -248f) : new Vector2(-48f, -152f);
        rt.sizeDelta = size;

        AddInsetFrame(root.transform, "SelectorFrame", DispatchGreenDark, 6f, 2f);

        var header = AddText(root.transform, "Header", "AGENT LOADOUT", 14,
            DispatchGreen, TextAlignmentOptions.Left);
        header.fontStyle = FontStyles.Bold;
        header.characterSpacing = 2f;
        header.rectTransform.anchoredPosition = new Vector2(-118f, 58f);
        header.rectTransform.sizeDelta = new Vector2(220f, 22f);

        characterNameText = AddText(root.transform, "SelectedName", "", 13,
            AgedPaper, TextAlignmentOptions.Right);
        characterNameText.characterSpacing = 1f;
        characterNameText.rectTransform.anchoredPosition = new Vector2(74f, 58f);
        characterNameText.rectTransform.sizeDelta = new Vector2(180f, 22f);

        for (int i = 0; i < PlayerCharacterPalette.Count; i++)
        {
            int index = i;
            var colors = PlayerCharacterPalette.Get(i);
            Button button = CreateButton(root.transform, "CharacterSlot_" + (i + 1), (i + 1).ToString("00"), 12,
                FieldBg, BtnSecondaryHover, BtnSecondaryPressed);
            button.onClick.AddListener(() => SelectCharacter(index));
            button.onClick.AddListener(PlayMenuSelect);

            var brt = button.GetComponent<RectTransform>();
            brt.anchoredPosition = new Vector2(-135f + i * 54f, -24f);
            brt.sizeDelta = new Vector2(46f, 72f);

            characterButtons[i] = button;
            characterButtonImages[i] = button.GetComponent<Image>();
            characterButtonLabels[i] = button.GetComponentInChildren<TMP_Text>();

            AddRect(button.transform, "Uniform", new Vector2(0f, 16f), new Vector2(30f, 14f),
                colors.uniform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            AddRect(button.transform, "Vest", new Vector2(0f, 0f), new Vector2(30f, 14f),
                colors.vest, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            AddRect(button.transform, "Helmet", new Vector2(0f, 31f), new Vector2(30f, 8f),
                colors.helmet, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            characterUniformSwatches[i] = button.transform.Find("Uniform")?.GetComponent<Image>();
            characterVestSwatches[i] = button.transform.Find("Vest")?.GetComponent<Image>();
            characterHelmetSwatches[i] = button.transform.Find("Helmet")?.GetComponent<Image>();

            if (characterButtonLabels[i] != null)
            {
                var labelRt = characterButtonLabels[i].rectTransform;
                labelRt.anchorMin = new Vector2(0f, 0f);
                labelRt.anchorMax = new Vector2(1f, 0f);
                labelRt.pivot = new Vector2(0.5f, 0f);
                labelRt.offsetMin = new Vector2(0f, 2f);
                labelRt.offsetMax = new Vector2(0f, 20f);
                characterButtonLabels[i].raycastTarget = false;
            }
        }

        var hint = AddText(root.transform, "Hint", "SELECT BEFORE START", 11,
            BlackCommissionUiTheme.MutedText, TextAlignmentOptions.Center);
        hint.characterSpacing = 1f;
        hint.rectTransform.anchoredPosition = new Vector2(0f, -72f);
        hint.rectTransform.sizeDelta = new Vector2(300f, 18f);

        RefreshCharacterSelector(true);
    }

    void SelectCharacter(int index)
    {
        PlayerCharacterPalette.SavedIndex = index;
        RefreshCharacterSelector(true);
    }

    void RefreshCharacterSelector(bool force = false)
    {
        if (characterButtons[0] == null) return;

        int selected = PlayerCharacterPalette.SavedIndex;
        if (!force && selected == lastCharacterSelectorIndex) return;
        lastCharacterSelectorIndex = selected;

        var selectedColors = PlayerCharacterPalette.Get(selected);
        if (characterNameText != null)
            characterNameText.text = selectedColors.label.ToUpperInvariant();

        for (int i = 0; i < PlayerCharacterPalette.Count; i++)
        {
            bool active = i == selected;
            var colors = PlayerCharacterPalette.Get(i);
            if (characterButtonImages[i] != null)
                characterButtonImages[i].color = active ? DispatchGreen : FieldBg;
            if (characterButtonLabels[i] != null)
            {
                characterButtonLabels[i].color = active ? Color.black : AgedPaper;
                characterButtonLabels[i].fontStyle = active ? FontStyles.Bold : FontStyles.Normal;
            }
            if (characterUniformSwatches[i] != null)
                characterUniformSwatches[i].color = active ? Color.Lerp(colors.uniform, Color.white, 0.18f) : colors.uniform;
            if (characterVestSwatches[i] != null)
                characterVestSwatches[i].color = active ? Color.Lerp(colors.vest, Color.white, 0.18f) : colors.vest;
            if (characterHelmetSwatches[i] != null)
                characterHelmetSwatches[i].color = active ? Color.Lerp(colors.helmet, Color.white, 0.18f) : colors.helmet;
        }
    }

    GameObject AddRect(Transform parent, string name, Vector2 anchoredPos, Vector2 size, Color color,
        Vector2 anchorMin, Vector2 pivot)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMin;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        return go;
    }

    // Top-left "Agent Name" label + input. Persisted to PlayerProfile and synced at spawn.
    void BuildNameField(Transform parent, Vector2? labelPosOverride = null,
        Vector2? inputPosOverride = null, float inputWidth = 300f)
    {
        var label = AddText(parent, "NameLabel", MvpLocale.T("player_name"), 16,
            DispatchGreen, TextAlignmentOptions.Left);
        var lRt = label.rectTransform;
        lRt.anchorMin = new Vector2(0f, 1f);
        lRt.anchorMax = new Vector2(0f, 1f);
        lRt.pivot = new Vector2(0f, 1f);
        lRt.anchoredPosition = labelPosOverride ?? new Vector2(50f, -150f);
        lRt.sizeDelta = new Vector2(320f, 22f);

        var go = new GameObject("NameInput",
            typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = FieldBg;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = inputPosOverride ?? new Vector2(50f, -176f);
        rt.sizeDelta = new Vector2(inputWidth, 44f);

        nameInput = go.GetComponent<TMP_InputField>();
        var text = AddText(go.transform, "Text", PlayerProfile.Name, 20,
            AgedPaper, TextAlignmentOptions.Left);
        var textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(10f, 4f);
        textRt.offsetMax = new Vector2(-10f, -4f);
        nameInput.textComponent = text;
        nameInput.characterLimit = PlayerProfile.MaxLength;
        nameInput.text = PlayerProfile.Name;
        nameInput.onValueChanged.AddListener(v => PlayerProfile.Name = v);
    }

    void BuildTerminalMenu(Transform parent)
    {
        // Mockup B: quiet flat list — text rows + thin dividers, amber hover arrow,
        // the row description appears only on the hovered row.
        bool hasSave = SaveIO.AnySave;

        continueBtn = CreateMenuRow(parent, "ContinueBtn", MvpLocale.T("menu_continue"),
            hasSave ? "Resume the last ledger" : MvpLocale.T("crt_no_save"), -404f, true);
        continueBtn.interactable = hasSave;
        if (!hasSave)
        {
            // Interactable was set after the row's initial style pass — re-apply the
            // muted look so 「（无存档）」 reads as the reason the row is dead.
            var t = continueBtn.transform.Find("Title")?.GetComponent<TMP_Text>();
            if (t != null) t.color = MenuRowDisabled;
            var d = continueBtn.transform.Find("Desc")?.GetComponent<TMP_Text>();
            if (d != null) { d.color = MenuRowDisabled; d.alpha = 0.6f; }
        }

        newOfficeBtn = CreateMenuRow(parent, "NewOfficeBtn", MvpLocale.T("menu_new_office"),
            "Open a fresh commission office", -502f, false);
        joinBtn = CreateMenuRow(parent, "JoinBtn", MvpLocale.T("join_office"),
            "Enter a teammate's room code", -600f, false);
        settingsBtn = CreateMenuRow(parent, "SettingsBtn", MvpLocale.T("menu_settings"),
            "Name / language / volume / sensitivity", -698f, false);
        quitBtn = CreateMenuRow(parent, "QuitBtn", MvpLocale.T("menu_shutdown"),
            "Clock out and power down", -796f, false);

        // Footer strip: LAN direct link (bottom-left) + debt flavor (bottom-right).
        directBtn = CreateButton(parent, "DirectBtn", MvpLocale.T("lan_direct_link"), 16,
            Color.clear,
            new Color(0.160f, 0.190f, 0.145f, 0.45f),
            new Color(0.090f, 0.110f, 0.080f, 0.60f));
        var dRt = directBtn.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(0f, 0f);
        dRt.pivot = new Vector2(0f, 0f);
        dRt.anchoredPosition = new Vector2(216f, 52f);
        dRt.sizeDelta = new Vector2(220f, 34f);
        var directLabel = directBtn.GetComponentInChildren<TMP_Text>();
        if (directLabel != null)
        {
            directLabel.color = new Color(0.431f, 0.478f, 0.400f, 1f); // #6E7A66
            directLabel.alignment = TextAlignmentOptions.Left;
        }

        var verText = AddText(parent, "FooterVersion", "ver 0.1", 16,
            new Color(0.431f, 0.478f, 0.400f, 1f), TextAlignmentOptions.Left);
        var vfRt = verText.rectTransform;
        vfRt.anchorMin = new Vector2(0f, 0f);
        vfRt.anchorMax = new Vector2(0f, 0f);
        vfRt.pivot = new Vector2(0f, 0f);
        vfRt.anchoredPosition = new Vector2(120f, 58f);
        vfRt.sizeDelta = new Vector2(90f, 24f);

        var debtText = AddText(parent, "FooterDebt", "QUARTERLY DEBT: 1,200G  ·  OVERDUE", 16,
            new Color(0.604f, 0.353f, 0.290f, 1f), TextAlignmentOptions.Right); // #9A5A4A
        var dbRt = debtText.rectTransform;
        dbRt.anchorMin = new Vector2(1f, 0f);
        dbRt.anchorMax = new Vector2(1f, 0f);
        dbRt.pivot = new Vector2(1f, 0f);
        dbRt.anchoredPosition = new Vector2(-120f, 58f);
        dbRt.sizeDelta = new Vector2(320f, 24f);
    }

    // Low-saturation van + roll-door silhouette, bottom-right (mockup B).
    void BuildVanSilhouette(Transform parent)
    {
        var root = new GameObject("VanSilhouette", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.anchoredPosition = new Vector2(-120f, 120f);
        rt.sizeDelta = new Vector2(430f, 240f);

        Color pad = new(0.118f, 0.149f, 0.118f, 0.85f);
        Color body = new(0.180f, 0.227f, 0.200f, 1f);
        Color dark = new(0.078f, 0.102f, 0.078f, 1f);
        Color door = new(0.141f, 0.180f, 0.149f, 1f);
        Color slat = new(0.102f, 0.133f, 0.110f, 1f);

        AddRect(root.transform, "Pad", new Vector2(0f, 0f), new Vector2(430f, 240f),
            pad, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        AddRect(root.transform, "Body", new Vector2(-65f, -45f), new Vector2(240f, 110f),
            body, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        AddRect(root.transform, "Cab", new Vector2(-100f, 20f), new Vector2(170f, 56f),
            body, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        AddRect(root.transform, "WheelL", new Vector2(-150f, -105f), new Vector2(52f, 52f),
            dark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        AddRect(root.transform, "WheelR", new Vector2(-15f, -105f), new Vector2(52f, 52f),
            dark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        AddRect(root.transform, "RollDoor", new Vector2(160f, -30f), new Vector2(100f, 180f),
            door, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        for (int i = 0; i < 4; i++)
            AddRect(root.transform, $"Slat{i}", new Vector2(160f, 40f - i * 30f), new Vector2(100f, 6f),
                slat, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
    }

    GameObject CreateMenuPanel(Transform parent)
    {
        var go = new GameObject("CommissionTerminal", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(600f, 790f);

        var image = go.GetComponent<Image>();
        image.raycastTarget = false;
        var tex = Resources.Load<Texture2D>("UI/MenuPanel");
        if (tex != null)
        {
            float horizontalCrop = tex.width * 0.045f;
            image.sprite = Sprite.Create(tex,
                new Rect(horizontalCrop, 0f, tex.width - horizontalCrop * 2f, tex.height),
                new Vector2(0.5f, 0.5f));
            image.color = Color.white;
            image.preserveAspect = false;
        }
        else
        {
            image.color = BlackCommissionUiTheme.ConcreteBlack;
            AddInsetFrame(go.transform, "FallbackFrame", PanelBorder, 10f, 2f);
        }
        return go;
    }

    void BuildReferenceMenuHotspots(Transform parent)
    {
        // The reference art already paints the full menu. These transparent
        // buttons sit over its rows so input still works without duplicate UI.
        continueBtn = CreateMenuHotspot(parent, "ContinueHotspot", new Vector2(3f, 279f), new Vector2(512f, 128f));
        newOfficeBtn = CreateMenuHotspot(parent, "NewOfficeHotspot", new Vector2(3f, 134f), new Vector2(512f, 124f));
        joinBtn = CreateMenuHotspot(parent, "JoinHotspot", new Vector2(3f, -4f), new Vector2(512f, 120f));
        settingsBtn = CreateMenuHotspot(parent, "SettingsHotspot", new Vector2(3f, -133f), new Vector2(512f, 92f));

        // Hidden fallback access for LAN testing. The visible mockup intentionally
        // has no direct-connect label, so this stays below the painted quit row.
        directBtn = CreateMenuHotspot(parent, "DirectConnectHotspot", new Vector2(3f, -322f), new Vector2(512f, 70f));
    }

    Button CreateMenuHotspot(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
    {
        var btn = CreateButton(parent, name, "", 1,
            new Color(0f, 0f, 0f, 0.001f),
            new Color(0.48f, 0.81f, 0.54f, 0.03f),
            new Color(0.48f, 0.81f, 0.54f, 0.08f));
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var label = btn.GetComponentInChildren<TMP_Text>();
        if (label != null) Destroy(label.gameObject);
        return btn;
    }

    // A wide menu row: bold title + small description, left-aligned, like the mockup.
    Button CreateMenuRow(Transform parent, string name, string title, string desc, float yFromTop, bool primary)
    {
        // Mockup B row: bare text + thin divider; hover = amber arrow + bold amber
        // title + the description fading in to the right. No background boxes.
        var btn = CreateButton(parent, name, "", 1, Color.clear, Color.clear, Color.clear);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(120f, yFromTop);
        rt.sizeDelta = new Vector2(880f, 84f);

        var emptyLabel = btn.GetComponentInChildren<TMP_Text>();
        if (emptyLabel != null) Destroy(emptyLabel.gameObject);

        var rowArrow = AddText(btn.transform, "Arrow", "▸", 30,
            MenuAmber, TextAlignmentOptions.Center);
        rowArrow.raycastTarget = false;
        rowArrow.alpha = 0f;
        var aRt = rowArrow.rectTransform;
        aRt.anchorMin = new Vector2(0f, 0.5f);
        aRt.anchorMax = new Vector2(0f, 0.5f);
        aRt.pivot = new Vector2(0.5f, 0.5f);
        aRt.anchoredPosition = new Vector2(16f, 2f);
        aRt.sizeDelta = new Vector2(36f, 44f);

        var rowTitle = AddText(btn.transform, "Title", title, 36,
            MenuRowText, TextAlignmentOptions.Left);
        rowTitle.raycastTarget = false;
        rowTitle.characterSpacing = 4f;
        var tRt = rowTitle.rectTransform;
        tRt.anchorMin = new Vector2(0f, 0f);
        tRt.anchorMax = new Vector2(0f, 1f);
        tRt.pivot = new Vector2(0f, 0.5f);
        tRt.anchoredPosition = new Vector2(44f, 2f);
        tRt.sizeDelta = new Vector2(420f, 0f);

        var rowDesc = AddText(btn.transform, "Desc", desc, 20,
            new Color(0.494f, 0.533f, 0.447f, 1f), TextAlignmentOptions.Left); // #7E8872
        rowDesc.raycastTarget = false;
        rowDesc.alpha = 0f;
        var dRt = rowDesc.rectTransform;
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(0f, 1f);
        dRt.pivot = new Vector2(0f, 0.5f);
        dRt.anchoredPosition = new Vector2(540f, 0f);
        dRt.sizeDelta = new Vector2(340f, 0f);

        var divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(btn.transform, false);
        var divImg = divider.GetComponent<Image>();
        divImg.color = new Color(0.227f, 0.271f, 0.227f, 1f); // #3A453A
        divImg.raycastTarget = false;
        var dvRt = divider.GetComponent<RectTransform>();
        dvRt.anchorMin = new Vector2(0f, 0f);
        dvRt.anchorMax = new Vector2(0f, 0f);
        dvRt.pivot = new Vector2(0f, 0f);
        dvRt.anchoredPosition = new Vector2(0f, 0f);
        dvRt.sizeDelta = new Vector2(520f, 2f);

        ConfigureMenuRowHover(btn, rowArrow, rowTitle, rowDesc);

        return btn;
    }

    static readonly Color MenuAmber = new(0.910f, 0.698f, 0.361f, 1f);   // #E8B25C
    static readonly Color MenuRowText = new(0.749f, 0.769f, 0.682f, 1f); // #BFC4AE
    static readonly Color MenuRowDisabled = new(0.353f, 0.376f, 0.322f, 1f);

    void ConfigureMenuRowHover(Button btn, TMP_Text arrow, TMP_Text title, TMP_Text desc)
    {
        void ApplyHover(bool hovered)
        {
            if (!btn.interactable)
            {
                title.color = MenuRowDisabled;
                arrow.alpha = 0f;
                desc.alpha = 0.6f; // 「（无存档）」 reads as the dim reason, always visible
                desc.color = MenuRowDisabled;
                return;
            }
            title.color = hovered ? MenuAmber : MenuRowText;
            title.fontStyle = hovered ? FontStyles.Bold : FontStyles.Normal;
            arrow.alpha = hovered ? 1f : 0f;
            desc.alpha = hovered ? 1f : 0f;
        }

        var trigger = btn.gameObject.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => { ApplyHover(true); if (btn.interactable) PlayMenuHover(); });
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => ApplyHover(false));
        trigger.triggers.Add(exit);

        ApplyHover(false);
    }

    static string MenuIconFor(string title)
    {
        if (title.Contains("NEW")) return "++";
        if (title.Contains("JOIN")) return "o-";
        if (title.Contains("SETTINGS")) return "*";
        if (title.Contains("QUIT")) return "[]";
        return ">";
    }

    // Stretches a rect to fill horizontally inside its parent at a given y-offset from
    // the top, with side padding and fixed height. Pivot top-center so anchored.y is
    // the distance from the top edge of the parent.
    static void AnchorTopStretch(RectTransform rt, float yFromTop, float height, float sidePadding)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -yFromTop);
        rt.sizeDelta = new Vector2(-sidePadding * 2f, height);
    }

    GameObject CreateDebtStamp(Transform parent)
    {
        var stamp = new GameObject("DebtStamp", typeof(RectTransform), typeof(Image));
        stamp.transform.SetParent(parent, false);
        stamp.GetComponent<Image>().color = FieldBg;

        // Two rust strip overlays (top + bottom borders) to match the stamped ledger look.
        var topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
        topBar.transform.SetParent(stamp.transform, false);
        topBar.GetComponent<Image>().color = StampRed;
        var tRt = topBar.GetComponent<RectTransform>();
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = Vector2.zero;
        tRt.sizeDelta = new Vector2(0f, 3f);

        var botBar = new GameObject("BotBar", typeof(RectTransform), typeof(Image));
        botBar.transform.SetParent(stamp.transform, false);
        botBar.GetComponent<Image>().color = StampRed;
        var bRt = botBar.GetComponent<RectTransform>();
        bRt.anchorMin = new Vector2(0f, 0f);
        bRt.anchorMax = new Vector2(1f, 0f);
        bRt.pivot = new Vector2(0.5f, 0f);
        bRt.anchoredPosition = Vector2.zero;
        bRt.sizeDelta = new Vector2(0f, 3f);

        var label = AddText(stamp.transform, "DebtText", "DEBT", 16,
            BlackCommissionUiTheme.RustWarning, TextAlignmentOptions.Center);
        label.fontStyle = FontStyles.Bold;
        var lRt = label.rectTransform;
        lRt.anchorMin = Vector2.zero;
        lRt.anchorMax = Vector2.one;
        lRt.offsetMin = Vector2.zero;
        lRt.offsetMax = Vector2.zero;

        return stamp;
    }

    GameObject BuildJoinPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "JoinPanel", new Vector2(520f, 380f));
        panel.SetActive(false);

        AddText(panel.transform, "Title", MvpLocale.T("enter_code"), 30,
            DispatchGreen, TextAlignmentOptions.Center)
            .rectTransform.anchoredPosition = new Vector2(0f, 130f);

        AddText(panel.transform, "Subtitle", MvpLocale.T("ask_host_code"), 16,
            AgedPaper, TextAlignmentOptions.Center)
            .rectTransform.anchoredPosition = new Vector2(0f, 80f);

        // Code input field.
        var inputObj = new GameObject("JoinCodeInput",
            typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputObj.transform.SetParent(panel.transform, false);
        inputObj.GetComponent<Image>().color = FieldBg;
        var inputRt = inputObj.GetComponent<RectTransform>();
        inputRt.anchorMin = new Vector2(0.5f, 0.5f);
        inputRt.anchorMax = new Vector2(0.5f, 0.5f);
        inputRt.pivot = new Vector2(0.5f, 0.5f);
        inputRt.anchoredPosition = new Vector2(0f, 20f);
        inputRt.sizeDelta = new Vector2(360f, 70f);

        joinCodeInput = inputObj.GetComponent<TMP_InputField>();
        var inputTextObj = AddText(inputObj.transform, "Text", "", 34,
            SodiumAmber, TextAlignmentOptions.Center);
        inputTextObj.fontStyle = FontStyles.Bold;
        var inputTextRt = inputTextObj.rectTransform;
        inputTextRt.anchorMin = Vector2.zero;
        inputTextRt.anchorMax = Vector2.one;
        inputTextRt.offsetMin = new Vector2(12f, 8f);
        inputTextRt.offsetMax = new Vector2(-12f, -8f);
        joinCodeInput.textComponent = inputTextObj;
        joinCodeInput.characterLimit = 6;
        joinCodeInput.contentType = TMP_InputField.ContentType.Custom;
        joinCodeInput.inputType = TMP_InputField.InputType.Standard;
        joinCodeInput.characterValidation = TMP_InputField.CharacterValidation.None;
        joinCodeInput.text = "";

        joinSubmitBtn = CreateButton(panel.transform, "SubmitBtn",
            MvpLocale.T("join"), 22,
            BtnPrimary, BtnPrimaryHover, BtnPrimaryPressed);
        joinSubmitBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -70f);
        joinSubmitBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 58f);

        joinCancelBtn = CreateButton(panel.transform, "CancelBtn",
            MvpLocale.T("back"), 18,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        joinCancelBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -130f);
        joinCancelBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 40f);

        // LAN direct entry — the old main-panel toggle is unreachable when the
        // world-space CRT stage carries the root menu, so it lives here too.
        var lanLink = CreateButton(panel.transform, "LanDirectLink",
            MvpLocale.T("lan_direct_link"), 13,
            new Color(0f, 0f, 0f, 0f),
            new Color(0.105f, 0.165f, 0.158f, 0.45f),
            new Color(0.060f, 0.095f, 0.090f, 0.6f));
        var lanRt = lanLink.GetComponent<RectTransform>();
        lanRt.anchoredPosition = new Vector2(0f, -168f);
        lanRt.sizeDelta = new Vector2(220f, 26f);
        var lanLabel = lanLink.GetComponentInChildren<TMP_Text>();
        if (lanLabel != null) lanLabel.color = HintText;
        lanLink.GetComponent<Button>().onClick.AddListener(ToggleDirectConnect);

        return panel;
    }

    GameObject BuildConnectingPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "ConnectingPanel", new Vector2(560f, 248f));
        panel.SetActive(false);

        // System-chrome loading screen reworked to mock 02b "OPENING THE OFFICE" (PM 2026-06-13):
        // tungsten amber + aged paper, NOT CRT green — green is reserved for in-world terminals
        // per the art bible, so it must not appear in menu-layer chrome.
        Color amberDim = new Color(SodiumAmber.r, SodiumAmber.g, SodiumAmber.b, 0.55f);
        AddInsetFrame(panel.transform, "BootFrame", amberDim, 7f, 2f);

        var header = AddText(panel.transform, "Header", "OPENING THE OFFICE", 17,
            SodiumAmber, TextAlignmentOptions.Center);
        header.fontStyle = FontStyles.Bold;
        header.characterSpacing = 3f;
        header.rectTransform.anchoredPosition = new Vector2(0f, 84f);
        header.rectTransform.sizeDelta = new Vector2(500f, 24f);

        var sub = AddText(panel.transform, "Sub", "DISPATCH BOOT", 12,
            HintText, TextAlignmentOptions.Center);
        sub.characterSpacing = 4f;
        sub.rectTransform.anchoredPosition = new Vector2(0f, 58f);
        sub.rectTransform.sizeDelta = new Vector2(420f, 18f);

        connectingText = AddText(panel.transform, "ConnText", "", 28,
            AgedPaper, TextAlignmentOptions.Center);
        connectingText.fontStyle = FontStyles.Bold;
        connectingText.characterSpacing = 1f;
        connectingText.rectTransform.anchoredPosition = new Vector2(0f, 8f);
        connectingText.rectTransform.sizeDelta = new Vector2(470f, 40f);

        var wait = AddText(panel.transform, "PleaseWait", MvpLocale.T("please_wait"), 14,
            HintText, TextAlignmentOptions.Center);
        wait.rectTransform.anchoredPosition = new Vector2(0f, -34f);
        wait.rectTransform.sizeDelta = new Vector2(470f, 22f);

        AddRect(panel.transform, "ProgressRail", new Vector2(0f, -74f), new Vector2(410f, 8f),
            new Color(0.090f, 0.078f, 0.055f, 0.92f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        AddRect(panel.transform, "ProgressFill", new Vector2(-82f, -74f), new Vector2(246f, 8f),
            new Color(SodiumAmber.r, SodiumAmber.g, SodiumAmber.b, 0.88f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        return panel;
    }

    GameObject BuildHostCodePanel(Transform parent)
    {
        var panel = CreatePanel(parent, "HostCodePanel", new Vector2(400f, 180f));
        panel.SetActive(false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -70f);

        AddText(panel.transform, "Header", MvpLocale.T("room_code_share"), 14,
            AgedPaper, TextAlignmentOptions.Center)
            .rectTransform.anchoredPosition = new Vector2(0f, 60f);

        hostCodeText = AddText(panel.transform, "HostCode", "", 44,
            SodiumAmber, TextAlignmentOptions.Center);
        hostCodeText.fontStyle = FontStyles.Bold;
        hostCodeText.rectTransform.anchoredPosition = new Vector2(0f, 10f);

        AddText(panel.transform, "Hint", MvpLocale.T("room_code_join_hint"), 12,
            HintText, TextAlignmentOptions.Center)
            .rectTransform.anchoredPosition = new Vector2(0f, -40f);

        hostCodeHideBtn = CreateButton(panel.transform, "HideBtn",
            MvpLocale.T("hide"), 14,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        var hbRt = hostCodeHideBtn.GetComponent<RectTransform>();
        hbRt.anchorMin = new Vector2(1f, 1f);
        hbRt.anchorMax = new Vector2(1f, 1f);
        hbRt.pivot = new Vector2(1f, 1f);
        hbRt.anchoredPosition = new Vector2(-10f, -10f);
        hbRt.sizeDelta = new Vector2(70f, 28f);

        return panel;
    }

    GameObject BuildSettingsPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "SettingsPanel", new Vector2(440f, 440f));
        panel.SetActive(false);

        AddText(panel.transform, "Header", MvpLocale.T("game"), 22,
            DispatchGreen, TextAlignmentOptions.Left)
            .rectTransform.anchoredPosition = new Vector2(0f, 180f);

        settingsCloseBtn = CreateButton(panel.transform, "Close", "X", 18,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        var cbRt = settingsCloseBtn.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(1f, 1f);
        cbRt.anchorMax = new Vector2(1f, 1f);
        cbRt.pivot = new Vector2(1f, 1f);
        cbRt.anchoredPosition = new Vector2(-12f, -12f);
        cbRt.sizeDelta = new Vector2(38f, 32f);

        // Agent name lives here now — the screen-space main panel (its old home)
        // stays hidden when the world-space CRT stage carries the menu.
        BuildNameField(panel.transform, new Vector2(28f, -64f), new Vector2(28f, -90f), 384f);

        BuildLangSection(panel.transform, new Vector2(0f, 35f));
        BuildVolumeSection(panel.transform, new Vector2(0f, -30f));
        BuildSensitivitySection(panel.transform, new Vector2(0f, -112f));

        settingsQuitBtn = CreateButton(panel.transform, "SettingsQuit", "Quit Game", 18,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        var quitRt = settingsQuitBtn.GetComponent<RectTransform>();
        quitRt.anchoredPosition = new Vector2(0f, -174f);
        quitRt.sizeDelta = new Vector2(220f, 42f);

        return panel;
    }

    void BuildLangSection(Transform parent, Vector2 position)
    {
        AddText(parent, "LangLabel", "Language", 14,
            HintText, TextAlignmentOptions.Left)
            .rectTransform.anchoredPosition = position + new Vector2(0f, 18f);

        // Index order must match MvpLocale: 0 = English (default), 1 = 中文.
        var enBtn = CreateButton(parent, "EnBtn", "English", 16,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        enBtn.GetComponent<RectTransform>().anchoredPosition = position + new Vector2(-90f, -10f);
        enBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 38f);
        enBtn.onClick.AddListener(() => { MvpHud.LanguageIndexStatic = 0; RebuildAllLabels(); });

        var zhBtn = CreateButton(parent, "ZhBtn", "中文 (简体)", 16,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        zhBtn.GetComponent<RectTransform>().anchoredPosition = position + new Vector2(90f, -10f);
        zhBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 38f);
        zhBtn.onClick.AddListener(() => { MvpHud.LanguageIndexStatic = 1; RebuildAllLabels(); });
    }

    void BuildVolumeSection(Transform parent, Vector2 position)
    {
        AddText(parent, "VolumeLabel", "Master Volume", 14,
            HintText, TextAlignmentOptions.Left)
            .rectTransform.anchoredPosition = position + new Vector2(0f, 18f);

        var sliderObj = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);
        var slider = sliderObj.GetComponent<Slider>();
        var sRt = slider.GetComponent<RectTransform>();
        sRt.anchoredPosition = position + new Vector2(0f, -10f);
        sRt.sizeDelta = new Vector2(360f, 22f);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(slider.transform, false);
        bg.GetComponent<Image>().color = FieldBg;
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(slider.transform, false);
        fill.GetComponent<Image>().color = DispatchGreen;
        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(slider.transform, false);
        fill.transform.SetParent(fillArea.transform, false);
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRt.offsetMin = new Vector2(8f, 0f);
        fillAreaRt.offsetMax = new Vector2(-8f, 0f);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        slider.fillRect = fillRt;

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.GetComponent<Image>().color = AgedPaper;
        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(slider.transform, false);
        handle.transform.SetParent(handleArea.transform, false);
        var handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(8f, 0f);
        handleAreaRt.offsetMax = new Vector2(-8f, 0f);
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(16f, 30f);
        slider.handleRect = handleRt;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = MvpHud.MasterVolumeStatic;
        slider.onValueChanged.AddListener(v => MvpHud.MasterVolumeStatic = v);
    }

    void BuildSensitivitySection(Transform parent, Vector2 position)
    {
        AddText(parent, "SensLabel", "Mouse Sensitivity", 14,
            HintText, TextAlignmentOptions.Left)
            .rectTransform.anchoredPosition = position + new Vector2(0f, 18f);

        var sliderObj = new GameObject("SensSlider", typeof(RectTransform), typeof(Slider));
        sliderObj.transform.SetParent(parent, false);
        var slider = sliderObj.GetComponent<Slider>();
        var sRt = slider.GetComponent<RectTransform>();
        sRt.anchoredPosition = position + new Vector2(0f, -10f);
        sRt.sizeDelta = new Vector2(360f, 22f);

        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(slider.transform, false);
        bg.GetComponent<Image>().color = FieldBg;
        var bgRt = bg.GetComponent<RectTransform>();
        bgRt.anchorMin = Vector2.zero;
        bgRt.anchorMax = Vector2.one;
        bgRt.offsetMin = Vector2.zero;
        bgRt.offsetMax = Vector2.zero;

        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.GetComponent<Image>().color = SodiumAmber;
        var fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(slider.transform, false);
        fill.transform.SetParent(fillArea.transform, false);
        var fillAreaRt = fillArea.GetComponent<RectTransform>();
        fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
        fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
        fillAreaRt.offsetMin = new Vector2(8f, 0f);
        fillAreaRt.offsetMax = new Vector2(-8f, 0f);
        var fillRt = fill.GetComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = Vector2.zero;
        fillRt.offsetMax = Vector2.zero;
        slider.fillRect = fillRt;

        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.GetComponent<Image>().color = AgedPaper;
        var handleArea = new GameObject("HandleArea", typeof(RectTransform));
        handleArea.transform.SetParent(slider.transform, false);
        handle.transform.SetParent(handleArea.transform, false);
        var handleAreaRt = handleArea.GetComponent<RectTransform>();
        handleAreaRt.anchorMin = Vector2.zero;
        handleAreaRt.anchorMax = Vector2.one;
        handleAreaRt.offsetMin = new Vector2(8f, 0f);
        handleAreaRt.offsetMax = new Vector2(-8f, 0f);
        var handleRt = handle.GetComponent<RectTransform>();
        handleRt.sizeDelta = new Vector2(16f, 30f);
        slider.handleRect = handleRt;

        slider.minValue = 0.25f;
        slider.maxValue = 8f;
        slider.value = PlayerCameraController.HorizontalSensitivity;
        slider.onValueChanged.AddListener(v => PlayerCameraController.HorizontalSensitivity = v);
    }

    GameObject BuildQuitConfirmPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "QuitConfirmPanel", new Vector2(460f, 220f));
        panel.SetActive(false);

        var title = AddText(panel.transform, "Title", "Confirm Quit?", 28,
            DispatchGreen, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.rectTransform.anchoredPosition = new Vector2(0f, 62f);
        title.rectTransform.sizeDelta = new Vector2(380f, 40f);

        var body = AddText(panel.transform, "Body", "Exit to desktop", 16,
            AgedPaper, TextAlignmentOptions.Center);
        body.rectTransform.anchoredPosition = new Vector2(0f, 14f);
        body.rectTransform.sizeDelta = new Vector2(390f, 28f);

        quitConfirmYesBtn = CreateButton(panel.transform, "ConfirmQuit", "Quit", 18,
            BtnPrimary, BtnPrimaryHover, BtnPrimaryPressed);
        var yesRt = quitConfirmYesBtn.GetComponent<RectTransform>();
        yesRt.anchoredPosition = new Vector2(-92f, -64f);
        yesRt.sizeDelta = new Vector2(150f, 44f);

        quitConfirmNoBtn = CreateButton(panel.transform, "CancelQuit", "Cancel", 18,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        var noRt = quitConfirmNoBtn.GetComponent<RectTransform>();
        noRt.anchoredPosition = new Vector2(92f, -64f);
        noRt.sizeDelta = new Vector2(150f, 44f);

        return panel;
    }

    GameObject BuildDirectConnectPanel(Transform parent)
    {
        var panel = CreateMenuPanel(parent);
        panel.name = "DirectConnectPanel";
        panel.SetActive(false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(0f, 20f);
        rt.sizeDelta = new Vector2(440f, 660f);

        directCloseBtn = CreateButton(panel.transform, "Close", "X", 16,
            new Color(0.025f, 0.030f, 0.027f, 0.32f),
            new Color(0.120f, 0.145f, 0.100f, 0.54f),
            new Color(0.070f, 0.090f, 0.060f, 0.70f));
        var closeRt = directCloseBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-62f, -142f);
        closeRt.sizeDelta = new Vector2(42f, 34f);
        var closeLabel = directCloseBtn.GetComponentInChildren<TMP_Text>();
        if (closeLabel != null)
            closeLabel.color = BlackCommissionUiTheme.OldPaper;

        var header = AddText(panel.transform, "Header", "LAN DIRECT", 28,
            DispatchGreen, TextAlignmentOptions.Center);
        header.fontStyle = FontStyles.Bold;
        header.characterSpacing = 6f;
        header.rectTransform.anchoredPosition = new Vector2(0f, 126f);
        header.rectTransform.sizeDelta = new Vector2(300f, 38f);

        AddText(panel.transform, "Hint", "Local host or join by IP", 14,
            BlackCommissionUiTheme.MutedText, TextAlignmentOptions.Center)
            .rectTransform.anchoredPosition = new Vector2(0f, 88f);

        directAddressInput = CreateInputField(panel.transform, "AddressInput",
            connectAddress, new Vector2(-70f, 22f), new Vector2(218f, 44f));
        directPortInput = CreateInputField(panel.transform, "PortInput",
            connectPort, new Vector2(106f, 22f), new Vector2(96f, 44f));

        var hostDirectBtn = CreateButton(panel.transform, "DirectHost",
            MvpLocale.T("direct_create"), 16,
            new Color(0.025f, 0.030f, 0.027f, 0.32f),
            new Color(0.120f, 0.170f, 0.105f, 0.44f),
            new Color(0.070f, 0.110f, 0.060f, 0.58f));
        hostDirectBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-84f, -56f);
        hostDirectBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 46f);
        var hostLabel = hostDirectBtn.GetComponentInChildren<TMP_Text>();
        if (hostLabel != null)
        {
            hostLabel.color = DispatchGreen;
            hostLabel.fontStyle = FontStyles.Bold;
        }
        hostDirectBtn.onClick.AddListener(StartHostDirect);

        var joinDirectBtn = CreateButton(panel.transform, "DirectJoin",
            MvpLocale.T("direct_join"), 16,
            new Color(0.025f, 0.030f, 0.027f, 0.32f),
            new Color(0.130f, 0.145f, 0.122f, 0.44f),
            new Color(0.070f, 0.090f, 0.060f, 0.58f));
        joinDirectBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(84f, -56f);
        joinDirectBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 46f);
        var joinLabel = joinDirectBtn.GetComponentInChildren<TMP_Text>();
        if (joinLabel != null)
        {
            joinLabel.color = BlackCommissionUiTheme.OldPaper;
            joinLabel.fontStyle = FontStyles.Bold;
        }
        joinDirectBtn.onClick.AddListener(StartClientDirect);

        return panel;
    }

    GameObject BuildLobbyWaitingPanel(Transform parent)
    {
        // Keep the waiting room readable in the dark office. The joke is in the
        // bureaucratic details, not in making the whole screen look like wet paper.
        Color screenFill = new(0.018f, 0.024f, 0.021f, 0.970f);
        Color paperFill = new(0.120f, 0.118f, 0.090f, 0.360f);
        Color fieldFill = new(0.013f, 0.018f, 0.015f, 0.920f);
        Color rowFill = new(0.018f, 0.028f, 0.022f, 0.940f);
        Color rowDivider = new(0.310f, 0.360f, 0.230f, 0.32f);
        Color textMain = BlackCommissionUiTheme.OldPaper;

        var root = new GameObject("LobbyWaitingPanel", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = Vector2.zero;
        rootRt.offsetMax = Vector2.zero;
        var veil = root.GetComponent<Image>();
        veil.color = new Color(0f, 0f, 0f, 0.72f);
        veil.raycastTarget = true;

        // ─── Dispatch dossier, aspect-fitted so the page never stretches ───
        Vector2 screenSize = new(940f, 620f);
        var screen = new GameObject("WaitingTerminal", typeof(RectTransform), typeof(Image));
        screen.transform.SetParent(root.transform, false);
        screen.GetComponent<Image>().color = screenFill;
        var panelRt = screen.GetComponent<RectTransform>();
        panelRt.anchorMin = new Vector2(0.5f, 0.5f);
        panelRt.anchorMax = new Vector2(0.5f, 0.5f);
        panelRt.pivot = new Vector2(0.5f, 0.5f);
        panelRt.anchoredPosition = Vector2.zero;
        panelRt.sizeDelta = screenSize;
        lobbyWaitingTerminalRt = panelRt;
        var panel = screen; // content parent; keeps the rest of the method readable

        AddInsetFrame(panel.transform, "DossierFrame", DispatchGreenDark, 9f, 2f);
        AddInsetFrame(panel.transform, "PaperHairline", BlackCommissionUiTheme.OldPaper, 19f, 1f);

        float halfW = screenSize.x * 0.5f; // 470
        float halfH = screenSize.y * 0.5f; // 310

        // ─── Header bar: OS banner + live status dot ───────────────────────
        AddRect(panel.transform, "HeaderBand", new Vector2(0f, halfH - 30f), new Vector2(884f, 52f),
            new Color(0.030f, 0.046f, 0.034f, 0.92f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        AddRect(panel.transform, "LeftDossier", new Vector2(-230f, -46f), new Vector2(410f, 410f),
            paperFill, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        AddRect(panel.transform, "LeftReadout", new Vector2(-230f, 92f), new Vector2(372f, 76f),
            fieldFill, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        var header = AddText(panel.transform, "CrtHeader", "BLACK COMMISSION / SURFACE RECOVERY DISPATCH", 15,
            BlackCommissionUiTheme.OldPaper, TextAlignmentOptions.Left);
        header.fontStyle = FontStyles.Bold;
        header.characterSpacing = 1.2f;
        header.rectTransform.anchoredPosition = new Vector2(-190f, halfH - 30f);
        header.rectTransform.sizeDelta = new Vector2(500f, 28f);

        AddRect(panel.transform, "StatusDot", new Vector2(halfW - 40f, halfH - 30f), new Vector2(14f, 14f),
            DispatchGreen, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        AddRect(panel.transform, "HeaderDivider", new Vector2(0f, halfH - 52f), new Vector2(884f, 2f),
            new Color(0.42f, 0.36f, 0.23f, 0.50f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        // ─── Left column: standby title + room code + hint ─────────────────
        lobbyTitleText = AddText(panel.transform, "Title", MvpLocale.T("lobby_waiting_title"), 32,
            textMain, TextAlignmentOptions.Left);
        lobbyTitleText.fontStyle = FontStyles.Bold;
        lobbyTitleText.characterSpacing = 2f;
        AnchorLeftCenter(lobbyTitleText.rectTransform, 40f, 200f, new Vector2(380f, 46f));

        lobbyRoomCodeLabelText = AddText(panel.transform, "RoomCodeLabel", MvpLocale.T("lobby_room_code"), 14,
            BlackCommissionUiTheme.RustWarning, TextAlignmentOptions.Left);
        lobbyRoomCodeLabelText.fontStyle = FontStyles.Bold;
        lobbyRoomCodeLabelText.characterSpacing = 2f;
        AnchorLeftCenter(lobbyRoomCodeLabelText.rectTransform, 42f, 146f, new Vector2(360f, 22f));

        lobbyCodeText = AddText(panel.transform, "RoomCode", "", 42,
            SodiumAmber, TextAlignmentOptions.Left);
        lobbyCodeText.fontStyle = FontStyles.Bold;
        lobbyCodeText.characterSpacing = 6f;
        AnchorLeftCenter(lobbyCodeText.rectTransform, 40f, 102f, new Vector2(400f, 54f));

        AddRect(panel.transform, "LeftDivider", new Vector2(-halfW + 240f, 56f), new Vector2(396f, 2f),
            DispatchGreenDark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

        lobbyHintText = AddText(panel.transform, "Hint", MvpLocale.T("lobby_waiting_hint"), 16,
            textMain, TextAlignmentOptions.Left);
        lobbyHintText.enableWordWrapping = true;
        lobbyHintText.lineSpacing = 8f;
        AnchorLeftCenter(lobbyHintText.rectTransform, 42f, 8f, new Vector2(380f, 132f));

        var procedure = AddText(panel.transform, "Procedure",
            "01  Stamp commission at terminal\n02  Buy gear from supply cabinet\n03  All aboard — dispatch to restricted zone", 14,
            BlackCommissionUiTheme.PaperDim, TextAlignmentOptions.Left);
        procedure.lineSpacing = 10f;
        AnchorLeftCenter(procedure.rectTransform, 42f, -150f, new Vector2(380f, 92f));

        var debtStamp = AddText(panel.transform, "DebtStamp", "DEBT\nACTIVE", 24,
            new Color(0.420f, 0.245f, 0.155f, 0.24f), TextAlignmentOptions.Center);
        debtStamp.fontStyle = FontStyles.Bold;
        debtStamp.characterSpacing = 2f;
        debtStamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -8f);
        debtStamp.rectTransform.anchoredPosition = new Vector2(-100f, 54f);
        debtStamp.rectTransform.sizeDelta = new Vector2(138f, 78f);

        // ─── Right column: crew roster header + slots + enter button ───────
        float colX = 226f; // centre of the right column
        lobbyCountText = AddText(panel.transform, "Count", "", 18,
            DispatchGreen, TextAlignmentOptions.Right);
        lobbyCountText.fontStyle = FontStyles.Bold;
        lobbyCountText.characterSpacing = 2f;
        var countRt = lobbyCountText.rectTransform;
        countRt.anchorMin = new Vector2(1f, 0.5f);
        countRt.anchorMax = new Vector2(1f, 0.5f);
        countRt.pivot = new Vector2(1f, 0.5f);
        countRt.anchoredPosition = new Vector2(-40f, 200f);
        countRt.sizeDelta = new Vector2(340f, 30f);

        for (int i = 0; i < 4; i++)
        {
            float y = 146f - i * 70f;
            var row = new GameObject($"LobbySlot{i}", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(panel.transform, false);
            var rowImage = row.GetComponent<Image>();
            rowImage.color = rowFill;
            rowImage.raycastTarget = false;
            var rowRt = row.GetComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0.5f, 0.5f);
            rowRt.anchorMax = new Vector2(0.5f, 0.5f);
            rowRt.pivot = new Vector2(0.5f, 0.5f);
            rowRt.anchoredPosition = new Vector2(colX, y);
            rowRt.sizeDelta = new Vector2(420f, 60f);

            AddRect(row.transform, "RowDivider", new Vector2(0f, -30f), new Vector2(420f, 1f),
                rowDivider, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));

            // Vest-colour bar on the left edge (set per occupant in RefreshLobbyWaiting).
            var swatch = new GameObject("Swatch", typeof(RectTransform), typeof(Image));
            swatch.transform.SetParent(row.transform, false);
            var swatchImage = swatch.GetComponent<Image>();
            swatchImage.color = Color.clear;
            swatchImage.raycastTarget = false;
            var swatchRt = swatch.GetComponent<RectTransform>();
            swatchRt.anchorMin = new Vector2(0f, 0.5f);
            swatchRt.anchorMax = new Vector2(0f, 0.5f);
            swatchRt.pivot = new Vector2(0f, 0.5f);
            swatchRt.anchoredPosition = new Vector2(14f, 0f);
            swatchRt.sizeDelta = new Vector2(8f, 44f);
            lobbySwatches[i] = swatchImage;

            var slotIndex = AddText(row.transform, "Index", $"0{i + 1}", 15,
                BlackCommissionUiTheme.PaperDim, TextAlignmentOptions.Center);
            slotIndex.fontStyle = FontStyles.Bold;
            slotIndex.raycastTarget = false;
            slotIndex.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            slotIndex.rectTransform.anchorMax = new Vector2(0f, 0.5f);
            slotIndex.rectTransform.pivot = new Vector2(0f, 0.5f);
            slotIndex.rectTransform.anchoredPosition = new Vector2(30f, 0f);
            slotIndex.rectTransform.sizeDelta = new Vector2(34f, 28f);

            var name = AddText(row.transform, "Name", "", 17,
                DispatchGreen, TextAlignmentOptions.Left);
            name.fontStyle = FontStyles.Bold;
            name.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            name.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            name.rectTransform.pivot = new Vector2(0f, 0.5f);
            name.rectTransform.anchoredPosition = new Vector2(76f, 10f);
            name.rectTransform.sizeDelta = new Vector2(-90f, 26f);
            lobbyNames[i] = name;

            var role = AddText(row.transform, "Role", "", 13,
                BlackCommissionUiTheme.PaperDim, TextAlignmentOptions.Left);
            role.rectTransform.anchorMin = new Vector2(0f, 0.5f);
            role.rectTransform.anchorMax = new Vector2(1f, 0.5f);
            role.rectTransform.pivot = new Vector2(0f, 0.5f);
            role.rectTransform.anchoredPosition = new Vector2(76f, -13f);
            role.rectTransform.sizeDelta = new Vector2(-90f, 20f);
            lobbyRoles[i] = role;

            // ‹ › vest-colour cycling, visible on the local player's row only.
            lobbyPrevBtns[i] = BuildLobbySwatchArrow(row.transform, "PrevColor", "‹", -58f, -1);
            lobbyNextBtns[i] = BuildLobbySwatchArrow(row.transform, "NextColor", "›", -28f, +1);
        }

        // Primary action: a stamped dispatch button, not a neon app CTA.
        lobbyEnterBtn = CreateButton(panel.transform, "EnterOffice",
            MvpLocale.T("lobby_enter_office") + "   ›", 20,
            new Color(0.075f, 0.140f, 0.070f, 0.96f),
            new Color(0.105f, 0.210f, 0.095f, 0.98f),
            new Color(0.040f, 0.080f, 0.040f, 1f));
        var enterRt = lobbyEnterBtn.GetComponent<RectTransform>();
        enterRt.anchoredPosition = new Vector2(330f, -158f);
        enterRt.sizeDelta = new Vector2(230f, 54f);
        lobbyEnterLabel = lobbyEnterBtn.GetComponentInChildren<TMP_Text>();
        if (lobbyEnterLabel != null)
        {
            lobbyEnterLabel.color = BlackCommissionUiTheme.OldPaper;
            lobbyEnterLabel.fontStyle = FontStyles.Bold;
            lobbyEnterLabel.characterSpacing = 2f;
        }

        // Scanline / vignette pass on top of all content for the CRT feel.
        BuildCrtOverlay(panel.transform, screenSize);

        root.SetActive(false);
        return root;
    }

    // Anchors a rect to the left-centre of its parent so anchoredPosition.x is the
    // distance from the parent's left edge — handy for the standby screen's columns.
    static void AnchorLeftCenter(RectTransform rt, float xFromLeft, float y, Vector2 size)
    {
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = new Vector2(xFromLeft, y);
        rt.sizeDelta = size;
    }

    GameObject BuildConnectedStatusPanel(Transform parent)
    {
        const float headerH = 24f;
        const float rowH = 30f;
        float panelH = 16f + headerH + 6f + rowH * 4 + 12f;

        var panel = new GameObject("ConnectedStatus", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = BlackCommissionUiTheme.ConcreteBlack;
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(340f, panelH);

        // Header: room code (host) or squad count.
        rosterHeaderText = AddText(panel.transform, "Header", "", 15,
            SodiumAmber, TextAlignmentOptions.Left);
        rosterHeaderText.fontStyle = FontStyles.Bold;
        var hRt = rosterHeaderText.rectTransform;
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = new Vector2(0f, -8f);
        hRt.sizeDelta = new Vector2(-24f, headerH);

        // Four roster rows: colour swatch + name.
        float rowTop = -(8f + headerH + 6f);
        for (int i = 0; i < 4; i++)
        {
            float y = rowTop - i * rowH;

            var swatch = new GameObject($"Swatch{i}", typeof(RectTransform), typeof(Image));
            swatch.transform.SetParent(panel.transform, false);
            var sImg = swatch.GetComponent<Image>();
            sImg.color = Color.clear;
            var sRt = swatch.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0f, 1f);
            sRt.anchorMax = new Vector2(0f, 1f);
            sRt.pivot = new Vector2(0f, 1f);
            sRt.anchoredPosition = new Vector2(14f, y - 5f);
            sRt.sizeDelta = new Vector2(16f, 16f);
            rosterSwatches[i] = sImg;

            var nameText = AddText(panel.transform, $"Name{i}", "", 14,
                AgedPaper, TextAlignmentOptions.Left);
            var nRt = nameText.rectTransform;
            nRt.anchorMin = new Vector2(0f, 1f);
            nRt.anchorMax = new Vector2(1f, 1f);
            nRt.pivot = new Vector2(0f, 1f);
            nRt.anchoredPosition = new Vector2(40f, y);
            nRt.sizeDelta = new Vector2(-150f, rowH - 6f);
            rosterNames[i] = nameText;

            int idx = i;

            // Per-listener mute toggle (hidden on your own row).
            var muteBtn = CreateButton(panel.transform, $"Mute{i}", "M", 13,
                BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
            var mRt = muteBtn.GetComponent<RectTransform>();
            mRt.anchorMin = new Vector2(1f, 1f);
            mRt.anchorMax = new Vector2(1f, 1f);
            mRt.pivot = new Vector2(1f, 1f);
            mRt.anchoredPosition = new Vector2(-54f, y - 3f);
            mRt.sizeDelta = new Vector2(42f, 22f);
            muteBtn.onClick.AddListener(() =>
            {
                ProximityVoiceChat.ToggleClientMuted(rosterClientIds[idx]);
                nextRosterRefresh = 0f;
            });
            rosterMuteButtons[i] = muteBtn;
            rosterMuteLabels[i] = muteBtn.GetComponentInChildren<TMP_Text>();

            // Host-only kick button.
            var kickBtn = CreateButton(panel.transform, $"Kick{i}", "✕", 13,
                new Color(0.360f, 0.220f, 0.145f, 0.85f),
                new Color(0.520f, 0.320f, 0.200f, 0.92f),
                new Color(0.240f, 0.150f, 0.100f, 0.95f));
            var kRt = kickBtn.GetComponent<RectTransform>();
            kRt.anchorMin = new Vector2(1f, 1f);
            kRt.anchorMax = new Vector2(1f, 1f);
            kRt.pivot = new Vector2(1f, 1f);
            kRt.anchoredPosition = new Vector2(-10f, y - 3f);
            kRt.sizeDelta = new Vector2(40f, 22f);
            kickBtn.onClick.AddListener(() =>
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                    NetworkManager.Singleton.DisconnectClient(rosterClientIds[idx], MvpLocale.T("you_were_kicked"));
            });
            rosterKickButtons[i] = kickBtn;
        }

        panel.SetActive(false);
        return panel;
    }

    // Fills the roster from the PlayerController objects present on this peer (works on
    // host and clients alike since name + character index are network-synced).
    void RefreshRoster()
    {
        if (Time.unscaledTime < nextRosterRefresh) return;
        nextRosterRefresh = Time.unscaledTime + 0.4f;

        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        System.Array.Sort(players, (a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        if (isHost && !string.IsNullOrEmpty(hostJoinCode))
            rosterHeaderText.text = MvpLocale.T("room_label", hostJoinCode);
        else
            rosterHeaderText.text = MvpLocale.T("roster_title", players.Length);

        for (int i = 0; i < 4; i++)
        {
            if (i < players.Length)
            {
                var p = players[i];
                var colors = PlayerCharacterPalette.Get(p.CharacterIndex.Value);
                string name = p.DisplayName.Value.ToString();
                if (string.IsNullOrEmpty(name)) name = $"Agent {p.OwnerClientId}";
                if (p.IsOwner) name += " " + MvpLocale.T("you_tag");

                rosterSwatches[i].color = colors.vest;
                rosterNames[i].text = name;
                rosterClientIds[i] = p.OwnerClientId;

                // Mute is for other players only; colour shows current state.
                bool showMute = !p.IsOwner;
                rosterMuteButtons[i].gameObject.SetActive(showMute);
                if (showMute && rosterMuteLabels[i] != null)
                    rosterMuteLabels[i].color = ProximityVoiceChat.IsClientMuted(p.OwnerClientId)
                        ? StampRed : DispatchGreen;

                // Kick is host-only and never targets yourself.
                rosterKickButtons[i].gameObject.SetActive(isHost && !p.IsOwner);
            }
            else
            {
                rosterSwatches[i].color = Color.clear;
                rosterNames[i].text = "";
                rosterMuteButtons[i].gameObject.SetActive(false);
                rosterKickButtons[i].gameObject.SetActive(false);
            }
        }
    }

    Button BuildLobbySwatchArrow(Transform row, string name, string glyph, float xFromRight, int dir)
    {
        var btn = CreateButton(row, name, glyph, 18,
            new Color(0.030f, 0.046f, 0.034f, 0.92f),
            new Color(0.105f, 0.165f, 0.158f, 0.96f),
            new Color(0.060f, 0.095f, 0.090f, 1f));
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(1f, 0.5f);
        rt.anchoredPosition = new Vector2(xFromRight, -13f);
        rt.sizeDelta = new Vector2(26f, 24f);
        var label = btn.GetComponentInChildren<TMP_Text>();
        if (label != null) label.color = DispatchGreen;
        btn.onClick.AddListener(() => CycleOwnCharacter(dir));
        btn.gameObject.SetActive(false);
        return btn;
    }

    /// <summary>
    /// Cycles the local player's vest colour, skipping colours other players already
    /// registered (lobby.md: 撞色独占 — first come, first served; server arbitration
    /// is a follow-up architecture item, this is the owner-side guard).
    /// </summary>
    void CycleOwnCharacter(int dir)
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        PlayerController own = null;
        var taken = new HashSet<int>();
        foreach (var p in players)
        {
            if (p.IsOwner) own = p;
            else taken.Add(p.CharacterIndex.Value);
        }
        if (own == null) return;

        int n = PlayerCharacterPalette.Count;
        int idx = own.CharacterIndex.Value;
        for (int step = 1; step <= n; step++)
        {
            int cand = ((idx + dir * step) % n + n) % n;
            if (taken.Contains(cand)) continue;
            own.CharacterIndex.Value = cand;       // owner-write NV, syncs to every peer
            PlayerCharacterPalette.SavedIndex = cand; // persists for the next session
            break;
        }
        PlayMenuHover();
        nextLobbyRefresh = 0f;
    }

    void RefreshLobbyWaiting()
    {
        if (lobbyWaitingPanel == null || !lobbyWaitingPanel.activeSelf) return;
        if (Time.unscaledTime < nextLobbyRefresh) return;
        nextLobbyRefresh = Time.unscaledTime + 0.4f;

        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        System.Array.Sort(players, (a, b) => a.OwnerClientId.CompareTo(b.OwnerClientId));

        if (lobbyCountText != null)
            lobbyCountText.text = MvpLocale.T("roster_title", players.Length);

        if (lobbyCodeText != null)
        {
            if (!string.IsNullOrEmpty(hostJoinCode))
                lobbyCodeText.text = hostJoinCode;
            else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
                lobbyCodeText.text = $"{MvpLocale.T("lobby_lan_only")} :{lastHostPort}";
            else
                lobbyCodeText.text = MvpLocale.T("lobby_lan_only");
        }

        if (lobbyHintText != null)
            lobbyHintText.text = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost
                ? MvpLocale.T("lobby_waiting_hint")
                : MvpLocale.T("lobby_client_note");

        for (int i = 0; i < 4; i++)
        {
            if (i < players.Length)
            {
                PlayerController player = players[i];
                var colors = PlayerCharacterPalette.Get(player.CharacterIndex.Value);
                string name = player.DisplayName.Value.ToString();
                if (string.IsNullOrEmpty(name))
                    name = $"Agent {player.OwnerClientId}";
                if (player.IsOwner)
                    name += " " + MvpLocale.T("you_tag");

                bool host = NetworkManager.Singleton != null &&
                    player.OwnerClientId == NetworkManager.ServerClientId;
                lobbySwatches[i].color = colors.vest;
                lobbyNames[i].text = name;
                lobbyRoles[i].text = host ? MvpLocale.T("host") : MvpLocale.T("client");
                lobbyRoles[i].color = host ? DispatchGreen : HintText;
                lobbyPrevBtns[i].gameObject.SetActive(player.IsOwner);
                lobbyNextBtns[i].gameObject.SetActive(player.IsOwner);
            }
            else
            {
                lobbySwatches[i].color = BlackCommissionUiTheme.MilitaryGreenDim;
                lobbyNames[i].text = MvpLocale.T("lobby_empty_slot");
                lobbyRoles[i].text = "";
                lobbyPrevBtns[i].gameObject.SetActive(false);
                lobbyNextBtns[i].gameObject.SetActive(false);
            }
        }
    }

    TMP_InputField CreateInputField(Transform parent, string name, string startValue,
        Vector2 anchoredPos, Vector2 size)
    {
        var go = new GameObject(name,
            typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = FieldBg;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;

        var input = go.GetComponent<TMP_InputField>();
        var text = AddText(go.transform, "Text", startValue, 16, AgedPaper, TextAlignmentOptions.Left);
        var textRt = text.rectTransform;
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(8f, 4f);
        textRt.offsetMax = new Vector2(-8f, -4f);
        input.textComponent = text;
        input.text = startValue;
        return input;
    }

    // ─── Primitives ───────────────────────────────────────────────────────

    static void Stretch(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.sizeDelta = Vector2.zero;
    }

    GameObject AddStretchRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return go;
    }

    GameObject AddInsetFrame(Transform parent, string name, Color color, float inset, float thickness)
    {
        var root = new GameObject(name, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rootRt = root.GetComponent<RectTransform>();
        rootRt.anchorMin = Vector2.zero;
        rootRt.anchorMax = Vector2.one;
        rootRt.offsetMin = new Vector2(inset, inset);
        rootRt.offsetMax = new Vector2(-inset, -inset);

        AddFrameEdge(root.transform, "Top", color, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, thickness));
        AddFrameEdge(root.transform, "Bottom", color, new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, thickness));
        AddFrameEdge(root.transform, "Left", color, new Vector2(0f, 0f), new Vector2(0f, 1f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(thickness, 0f));
        AddFrameEdge(root.transform, "Right", color, new Vector2(1f, 0f), new Vector2(1f, 1f),
            new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(thickness, 0f));
        return root;
    }

    GameObject AddFrameEdge(Transform parent, string name, Color color, Vector2 anchorMin,
        Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var edge = new GameObject(name, typeof(RectTransform), typeof(Image));
        edge.transform.SetParent(parent, false);
        var image = edge.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        var rt = edge.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        return edge;
    }

    GameObject CreatePanel(Transform parent, string name, Vector2 size)
    {
        var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = PanelBg;
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = size;

        // Border (thin frame).
        var border = new GameObject("Border", typeof(RectTransform), typeof(Image));
        border.transform.SetParent(panel.transform, false);
        var bImage = border.GetComponent<Image>();
        bImage.color = PanelBorder;
        bImage.raycastTarget = false;
        var bRt = border.GetComponent<RectTransform>();
        bRt.anchorMin = Vector2.zero;
        bRt.anchorMax = Vector2.one;
        bRt.offsetMin = new Vector2(-2f, -2f);
        bRt.offsetMax = new Vector2(2f, 2f);
        border.transform.SetAsFirstSibling();

        return panel;
    }

    GameObject CreateRow(Transform parent, string name, float height, float x, float yOffset)
    {
        var row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var rt = row.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(x, -yOffset);
        rt.sizeDelta = new Vector2(0f, height);
        return row;
    }

    void CreateDivider(Transform parent, Vector2 anchoredPos, float width)
    {
        var div = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        div.transform.SetParent(parent, false);
        var image = div.GetComponent<Image>();
        image.color = PanelBorder;
        image.raycastTarget = false;
        var rt = div.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = new Vector2(width, 1f);
    }

    Button CreateButton(Transform parent, string name, string label, int fontSize,
        Color normal, Color hover, Color pressed)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = normal;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        var btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        btn.navigation = new Navigation { mode = Navigation.Mode.None };
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(hover.r / Mathf.Max(0.01f, normal.r),
                                         hover.g / Mathf.Max(0.01f, normal.g),
                                         hover.b / Mathf.Max(0.01f, normal.b), 1f);
        cb.pressedColor = new Color(pressed.r / Mathf.Max(0.01f, normal.r),
                                     pressed.g / Mathf.Max(0.01f, normal.g),
                                     pressed.b / Mathf.Max(0.01f, normal.b), 1f);
        cb.selectedColor = Color.white;
        cb.disabledColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        btn.colors = cb;

        var labelText = AddText(go.transform, "Label", label, fontSize, BlackCommissionUiTheme.Text, TextAlignmentOptions.Center);
        var labelRt = labelText.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        labelText.raycastTarget = false;

        return btn;
    }

    void EnsureMenuAudio()
    {
        if (menuAudioSource == null)
        {
            menuAudioSource = gameObject.GetComponent<AudioSource>();
            if (menuAudioSource == null)
                menuAudioSource = gameObject.AddComponent<AudioSource>();
            menuAudioSource.playOnAwake = false;
            menuAudioSource.loop = false;
            menuAudioSource.spatialBlend = 0f;
            menuAudioSource.volume = 0.45f;
        }

        menuHoverClip ??= SynthAudio.Tone("menu_hover_slide", 920f, 0.045f, 0.12f, SynthAudio.WaveShape.Square);
        menuSelectClip ??= SynthAudio.Tone("menu_select_press", 520f, 0.08f, 0.16f, SynthAudio.WaveShape.Saw);
    }

    void PlayMenuHover()
    {
        EnsureMenuAudio();
        menuAudioSource.pitch = Random.Range(0.96f, 1.04f);
        menuAudioSource.PlayOneShot(menuHoverClip, 0.55f);
    }

    void PlayMenuSelect()
    {
        EnsureMenuAudio();
        menuAudioSource.pitch = 0.92f;
        menuAudioSource.PlayOneShot(menuSelectClip, 0.70f);
    }

    TMP_Text AddText(Transform parent, string name, string body, int fontSize,
        Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var text = go.AddComponent<TextMeshProUGUI>();
        text.text = body;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        var cjkFont = MvpTmpFontProvider.GetFontAsset();
        if (cjkFont != null) text.font = cjkFont;
        var rt = text.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(320f, fontSize + 8f);
        return text;
    }

    // ─── Event binding ────────────────────────────────────────────────────

    void BindEvents()
    {
        continueBtn.onClick.AddListener(StartHost);
        newOfficeBtn.onClick.AddListener(StartHost);
        joinBtn.onClick.AddListener(() => SetState(MenuState.JoinInput));
        if (recordsBtn != null)
            recordsBtn.onClick.AddListener(() => SetStatus("Company records not yet archived."));
        settingsBtn.onClick.AddListener(ShowSettingsPanel);
        if (quitBtn != null)
            quitBtn.onClick.AddListener(ShowQuitConfirm);
        directBtn.onClick.AddListener(ToggleDirectConnect);
        joinSubmitBtn.onClick.AddListener(StartJoin);
        joinCancelBtn.onClick.AddListener(() =>
        {
            joinCode = "";
            if (joinCodeInput != null) joinCodeInput.text = "";
            SetState(MenuState.Main);
        });
        settingsCloseBtn.onClick.AddListener(HideSettingsPanel);
        settingsQuitBtn.onClick.AddListener(ShowQuitConfirm);
        directCloseBtn.onClick.AddListener(() => directConnectPanel.SetActive(false));
        hostCodeHideBtn.onClick.AddListener(() => hostCodePanel.SetActive(false));
        lobbyEnterBtn.onClick.AddListener(DismissLobbyWaiting);
        quitConfirmYesBtn.onClick.AddListener(QuitGame);
        quitConfirmNoBtn.onClick.AddListener(HideQuitConfirm);

        if (joinCodeInput != null)
            joinCodeInput.onValueChanged.AddListener(v =>
            {
                string upper = (v ?? "").ToUpper().Trim();
                if (upper != v) joinCodeInput.SetTextWithoutNotify(upper);
                joinCode = upper;
            });

        if (directAddressInput != null)
            directAddressInput.onValueChanged.AddListener(v => connectAddress = v ?? "127.0.0.1");
        if (directPortInput != null)
            directPortInput.onValueChanged.AddListener(v => connectPort = v ?? "7778");
    }

    void SubscribeConnectionEvents()
    {
        if (ConnectionManager.Instance == null) return;
        ConnectionManager.Instance.OnJoinCodeReady += HandleJoinCodeReady;
        ConnectionManager.Instance.OnConnected += HandleConnected;
        ConnectionManager.Instance.OnError += HandleError;
    }

    void UnsubscribeConnectionEvents()
    {
        if (ConnectionManager.Instance == null) return;
        ConnectionManager.Instance.OnJoinCodeReady -= HandleJoinCodeReady;
        ConnectionManager.Instance.OnConnected -= HandleConnected;
        ConnectionManager.Instance.OnError -= HandleError;
    }

    // ─── State + transitions ──────────────────────────────────────────────

    // ─── Seams for the world-space CRT stage ──────────────────────────────

    /// <summary>True when the CRT rows should take input (root menu, no modal card open).</summary>
    public bool MenuRowsAvailable => state == MenuState.Main && !AnyModalOpen;

    public bool AnyModalOpen =>
        (joinPanel != null && joinPanel.activeSelf) ||
        (connectingPanel != null && connectingPanel.activeSelf) ||
        (settingsPanel != null && settingsPanel.activeSelf) ||
        (directConnectPanel != null && directConnectPanel.activeSelf) ||
        (quitConfirmPanel != null && quitConfirmPanel.activeSelf);

    public void UiContinueShift() => StartHost();
    public void UiOpenJoin() => SetState(MenuState.JoinInput);
    public void UiOpenSettings() => ShowSettingsPanel();
    public void UiQuitConfirm() => ShowQuitConfirm();
    public void UiPlayHover() => PlayMenuHover();
    public void UiPlaySelect() => PlayMenuSelect();

    void SetState(MenuState next)
    {
        state = next;
        mainPanel.SetActive(state == MenuState.Main && crtStage == null);
        joinPanel.SetActive(state == MenuState.JoinInput);
        connectingPanel.SetActive(state == MenuState.Connecting);
        hostCodePanel.SetActive(state == MenuState.HostWaiting && !string.IsNullOrEmpty(hostJoinCode));
        if (state != MenuState.Main && directConnectPanel != null)
            directConnectPanel.SetActive(false);

        if (state == MenuState.Connecting)
            connectingText.text = MvpLocale.T("connecting") + "...";

        if (state == MenuState.JoinInput && joinCodeInput != null)
        {
            joinCodeInput.text = joinCode ?? "";
            EventSystem.current?.SetSelectedGameObject(joinCodeInput.gameObject);
        }
        else if (state == MenuState.Main)
        {
            EventSystem.current?.SetSelectedGameObject(null);
        }
    }

    void StartHost()
    {
        if (ConnectionManager.Instance != null && ConnectionManager.Instance.ServicesReady)
        {
            SetState(MenuState.Connecting);
            ConnectionManager.Instance.HostGame();
        }
        else
        {
            // No online service: fall back to a local host, but make it explicit that
            // teammates can only join over LAN (not the internet).
            StartHostDirect();
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                SetStatus(MvpLocale.T("local_mode_only"));
        }
    }

    void StartJoin()
    {
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus(MvpLocale.T("enter_code_prompt"));
            return;
        }
        // Relay join codes are always 6 alphanumeric characters.
        if (joinCode.Trim().Length != 6)
        {
            SetStatus(MvpLocale.T("code_six_chars"));
            return;
        }
        if (ConnectionManager.Instance != null)
        {
            SetState(MenuState.Connecting);
            ConnectionManager.Instance.JoinGame(joinCode);
        }
        else
        {
            SetStatus(MvpLocale.T("relay_unavailable"));
        }
    }

    void HandleJoinCodeReady(string code)
    {
        hostJoinCode = code;
        SetState(MenuState.HostWaiting);
    }

    void HandleConnected()
    {
        joinCode = "";
        SetState(MenuState.Main);
    }

    void HandleError(string error)
    {
        SetStatus(error);
        if (state == MenuState.Connecting)
            SetState(MenuState.Main);
    }

    void SetStatus(string msg)
    {
        statusMessage = msg;
        statusMessageUntil = Time.unscaledTime + 5f;
    }

    void HandleKeyboardShortcuts()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        // C opens the crew/appearance picker (design/ux/lobby.md). Available at the
        // menu root and in the lobby; ignored while a modal card is open.
        if (keyboard.cKey.wasPressedThisFrame && !CrewPickerScreen.IsOpen && !AnyModalOpen)
        {
            CrewPickerScreen.Open();
            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            if (quitConfirmPanel != null && quitConfirmPanel.activeSelf)
            {
                HideQuitConfirm();
                return;
            }

            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                HideSettingsPanel();
                return;
            }

            if (directConnectPanel != null && directConnectPanel.activeSelf)
            {
                directConnectPanel.SetActive(false);
                return;
            }

            if (state == MenuState.JoinInput)
            {
                SetState(MenuState.Main);
                return;
            }

            // At the CRT root, Esc = 关机确认 (spec: Esc 逐级返回; 根屏无上级 → 离岗单).
            if (crtStage != null && state == MenuState.Main &&
                NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening)
            {
                ShowQuitConfirm();
                return;
            }
        }

        bool enterPressed = keyboard.enterKey.wasPressedThisFrame ||
            keyboard.numpadEnterKey.wasPressedThisFrame;
        if (!enterPressed) return;

        if (quitConfirmPanel != null && quitConfirmPanel.activeSelf)
        {
            QuitGame();
            return;
        }

        if (lobbyWaitingPanel != null && lobbyWaitingPanel.activeSelf)
        {
            DismissLobbyWaiting();
            return;
        }

        if (state == MenuState.JoinInput && joinPanel != null && joinPanel.activeSelf)
        {
            StartJoin();
            return;
        }

        bool menuReady = crtStage == null && state == MenuState.Main &&
            mainPanel != null && mainPanel.activeSelf &&
            settingsPanel != null && !settingsPanel.activeSelf &&
            directConnectPanel != null && !directConnectPanel.activeSelf &&
            NetworkManager.Singleton != null && !NetworkManager.Singleton.IsListening;
        if (menuReady)
            StartHost();
    }

    void ShowQuitConfirm()
    {
        if (quitConfirmPanel == null) return;
        if (directConnectPanel != null) directConnectPanel.SetActive(false);
        quitConfirmPanel.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(quitConfirmNoBtn != null ? quitConfirmNoBtn.gameObject : null);
    }

    void HideQuitConfirm()
    {
        if (quitConfirmPanel == null) return;
        quitConfirmPanel.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    void ShowSettingsPanel()
    {
        if (settingsPanel == null) return;
        if (directConnectPanel != null) directConnectPanel.SetActive(false);
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
        settingsPanel.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(settingsCloseBtn != null ? settingsCloseBtn.gameObject : null);
    }

    void HideSettingsPanel()
    {
        if (settingsPanel == null) return;
        settingsPanel.SetActive(false);
        if (quitConfirmPanel != null) quitConfirmPanel.SetActive(false);
        EventSystem.current?.SetSelectedGameObject(null);
    }

    void DismissLobbyWaiting()
    {
        lobbyWaitingDismissed = true;
        if (lobbyWaitingPanel != null)
            lobbyWaitingPanel.SetActive(false);
        if (connectedStatusPanel != null)
            connectedStatusPanel.SetActive(true);
        IsGameplayInputBlockedByMenu = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        // 确认到岗 → push the desk camera to the player's head and hand off (no cut).
        crtStage?.BeginPushIn();
    }

    void UpdateGameplayInputBlock()
    {
        bool inHq = SceneManager.GetActiveScene().name == "HQ";
        bool blocked = inHq && lobbyWaitingPanel != null && lobbyWaitingPanel.activeSelf;
        IsGameplayInputBlockedByMenu = blocked;

        if (blocked)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void UpdateMenuVisibilityFlag()
    {
        IsMenuVisible =
            (crtStage != null && crtStage.CameraLive) ||
            (backgroundImage != null && backgroundImage.gameObject.activeSelf) ||
            (mainPanel != null && mainPanel.activeSelf) ||
            (joinPanel != null && joinPanel.activeSelf) ||
            (connectingPanel != null && connectingPanel.activeSelf) ||
            (quitConfirmPanel != null && quitConfirmPanel.activeSelf) ||
            (lobbyWaitingPanel != null && lobbyWaitingPanel.activeSelf);
    }

    void UpdateStatusVisibility()
    {
        if (statusText == null) return;
        bool live = !string.IsNullOrEmpty(statusMessage) && Time.unscaledTime < statusMessageUntil;
        // Status bar is reserved for transient error / success messages only.
        // The dispatch card already shows the create_hint as a permanent caption,
        // so don't echo it here — that was causing the same line to render twice.
        statusText.text = live ? statusMessage : "";
        statusText.color = live ? BlackCommissionUiTheme.RustWarning : HintText;
    }

    void UpdateConnectedVisibility()
    {
        bool listening = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;
        if (listening && !wasListening)
        {
            lobbyWaitingDismissed = false;
            nextLobbyRefresh = 0f;
        }
        else if (!listening && wasListening)
        {
            lobbyWaitingDismissed = false;
            if (lobbyWaitingPanel != null)
                lobbyWaitingPanel.SetActive(false);
        }
        wasListening = listening;

        // Once connected, show the waiting room first; after dismissal, keep a small corner status badge.
        if (listening)
        {
            // In-game: the player camera takes over — turn off the menu backdrop camera
            // so the two don't double-render.
            SetMenuBackdropCamera(false);
            // The full-screen menu art lives on the canvas root (not inside mainPanel),
            // so hiding the panels alone leaves it covering the 3D office. Hide it too.
            if (backgroundImage != null && backgroundImage.gameObject.activeSelf)
                backgroundImage.gameObject.SetActive(false);
            if (screenVeil.activeSelf) screenVeil.SetActive(false);
            if (mainPanel.activeSelf) mainPanel.SetActive(false);
            if (joinPanel.activeSelf) joinPanel.SetActive(false);
            if (connectingPanel.activeSelf) connectingPanel.SetActive(false);
            if (settingsPanel.activeSelf) settingsPanel.SetActive(false);
            if (directConnectPanel.activeSelf) directConnectPanel.SetActive(false);
            if (quitConfirmPanel != null && quitConfirmPanel.activeSelf) quitConfirmPanel.SetActive(false);
            if (hostCodePanel.activeSelf) hostCodePanel.SetActive(false);
            if (versionText != null && versionText.gameObject.activeSelf) versionText.gameObject.SetActive(false);

            bool showWaiting = !lobbyWaitingDismissed;
            lobbyWaitingPanel.SetActive(showWaiting);
            connectedStatusPanel.SetActive(!showWaiting);
            RefreshRoster();
            RefreshLobbyWaiting();

            if (!showWaiting && state == MenuState.HostWaiting && !string.IsNullOrEmpty(hostJoinCode))
            {
                hostCodePanel.SetActive(true);
                if (hostCodeText != null) hostCodeText.text = hostJoinCode;
            }
        }
        else
        {
            // With the world-space CRT stage active, the 3D office IS the backdrop —
            // never re-show the baked art or the screen-space main panel.
            if (crtStage == null)
            {
                if (backgroundImage != null && !backgroundImage.gameObject.activeSelf)
                    backgroundImage.gameObject.SetActive(true);
                if (!screenVeil.activeSelf) screenVeil.SetActive(true);
                if (state == MenuState.Main && !mainPanel.activeSelf) mainPanel.SetActive(true);
            }
            lobbyWaitingPanel.SetActive(false);
            connectedStatusPanel.SetActive(false);
        }
    }

    // ─── Quit ─────────────────────────────────────────────────────────────

    void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ─── Direct connect ───────────────────────────────────────────────────

    void ToggleDirectConnect()
    {
        if (directConnectPanel == null) return;
        bool show = !directConnectPanel.activeSelf;
        settingsPanel.SetActive(false);
        directConnectPanel.SetActive(show);
    }

    void StartHostDirect()
    {
        if (NetworkManager.Singleton == null)
        {
            SetStatus("Cannot host: NetworkManager is missing from the scene.");
            return;
        }
        if (NetworkManager.Singleton.IsListening)
        {
            directConnectPanel.SetActive(false);
            SetStatus(MvpLocale.T("host_started"));
            return;
        }

        SetStatus("Starting local host...");
        if (!NetworkManager.Singleton.TryGetComponent<UnityTransport>(out var transport))
        {
            bool started = NetworkManager.Singleton.StartHost();
            SetStatus(started ? MvpLocale.T("host_started") : MvpLocale.T("host_failed"));
            if (started && directConnectPanel != null) directConnectPanel.SetActive(false);
            return;
        }

        for (int attempt = 0; attempt < 4; attempt++)
        {
            ushort port = AutoPort.AssignFreePort(transport, (ushort)(7778 + attempt));
            lastHostPort = port;
            connectPort = port.ToString();
            if (directPortInput != null) directPortInput.SetTextWithoutNotify(connectPort);

            if (NetworkManager.Singleton.StartHost())
            {
                connectAddress = "127.0.0.1";
                if (directAddressInput != null) directAddressInput.SetTextWithoutNotify(connectAddress);
                if (directConnectPanel != null) directConnectPanel.SetActive(false);
                SetStatus(MvpLocale.T("direct_host_started", connectAddress, port));
                return;
            }
            if (NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }
        SetStatus(MvpLocale.T("host_port_busy"));
    }

    void StartClientDirect()
    {
        if (NetworkManager.Singleton == null)
        {
            SetStatus("Cannot join: NetworkManager is missing from the scene.");
            return;
        }
        if (!ushort.TryParse(connectPort, out ushort port))
        {
            SetStatus(MvpLocale.T("port_error"));
            return;
        }
        if (NetworkManager.Singleton.TryGetComponent<UnityTransport>(out var transport))
            transport.SetConnectionData(connectAddress, port);

        bool started = NetworkManager.Singleton.StartClient();
        SetStatus(started ? MvpLocale.T("joining", connectAddress, port) : MvpLocale.T("join_failed"));
    }

    // ─── Localized label refresh ──────────────────────────────────────────

    void RebuildAllLabels()
    {
        if (subtitleText != null) subtitleText.text = MvpLocale.T("subtitle");
        if (lobbyTitleText != null) lobbyTitleText.text = MvpLocale.T("lobby_waiting_title");
        if (lobbyRoomCodeLabelText != null) lobbyRoomCodeLabelText.text = MvpLocale.T("lobby_room_code");
        if (lobbyHintText != null) lobbyHintText.text = MvpLocale.T("lobby_waiting_hint");
        if (lobbyEnterLabel != null) lobbyEnterLabel.text = MvpLocale.T("lobby_enter_office") + "   ›";
        nextLobbyRefresh = 0f;
    }
}
