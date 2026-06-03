using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UGUI canvas-based main menu. Builds its own hierarchy procedurally so there is no
/// scene YAML editing required. Replaces the legacy OnGUI QuickNetworkUI for new
/// installations; QuickNetworkUI hides itself when this is present in the scene.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    enum MenuState { Main, JoinInput, Connecting, HostWaiting }

    // ─── Color tokens (mirrors art bible) ─────────────────────────────────
    static readonly Color CivicTeal = new(0.184f, 0.310f, 0.294f);
    static readonly Color DeepCivicTeal = new(0.090f, 0.141f, 0.133f);
    static readonly Color PanelBg = new(0.075f, 0.060f, 0.043f, 0.94f);
    static readonly Color PanelBorder = new(0.47f, 0.34f, 0.17f, 0.62f);
    static readonly Color ScreenFog = new(0.012f, 0.016f, 0.014f, 0.78f);
    static readonly Color DispatchGreen = new(0.482f, 0.812f, 0.541f);
    static readonly Color DispatchGreenDark = new(0.133f, 0.349f, 0.227f);
    static readonly Color StampRed = new(0.761f, 0.227f, 0.169f);
    static readonly Color SodiumAmber = new(0.851f, 0.604f, 0.192f);
    static readonly Color AgedPaper = new(0.839f, 0.784f, 0.608f);
    static readonly Color DirtyBone = new(0.788f, 0.761f, 0.667f);
    static readonly Color FieldBg = new(0.024f, 0.020f, 0.015f, 0.96f);
    static readonly Color BtnPrimary = new(0.050f, 0.085f, 0.050f, 0.92f);
    static readonly Color BtnPrimaryHover = new(0.072f, 0.125f, 0.074f, 0.96f);
    static readonly Color BtnPrimaryPressed = new(0.040f, 0.065f, 0.040f, 1f);
    static readonly Color BtnSecondary = new(0.075f, 0.060f, 0.043f, 0.74f);
    static readonly Color BtnSecondaryHover = new(0.120f, 0.095f, 0.060f, 0.84f);
    static readonly Color BtnSecondaryPressed = new(0.050f, 0.040f, 0.030f, 0.96f);
    static readonly Color HintText = new(0.66f, 0.61f, 0.48f);

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
    GameObject connectedStatusPanel;

    TMP_Text titleText;
    TMP_Text subtitleText;
    TMP_Text statusText;
    TMP_Text hostCodeText;
    TMP_Text connectingText;
    TMP_Text versionText;

    TMP_InputField joinCodeInput;
    TMP_InputField directAddressInput;
    TMP_InputField directPortInput;
    TMP_InputField nameInput;

    // Lobby roster (shown once connected): one row per player in the office.
    TMP_Text rosterHeaderText;
    readonly Image[] rosterSwatches = new Image[4];
    readonly TMP_Text[] rosterNames = new TMP_Text[4];
    readonly Button[] rosterMuteButtons = new Button[4];
    readonly TMP_Text[] rosterMuteLabels = new TMP_Text[4];
    readonly Button[] rosterKickButtons = new Button[4];
    readonly ulong[] rosterClientIds = new ulong[4];
    float nextRosterRefresh;

    Image backgroundImage;
    bool usingBakedMenuArt;

    Button continueBtn;
    Button newOfficeBtn;
    Button joinBtn;
    Button settingsBtn;
    Button quitBtn;
    Button directBtn;
    Button joinSubmitBtn;
    Button joinCancelBtn;
    Button settingsCloseBtn;
    Button directCloseBtn;
    Button hostCodeHideBtn;

    void Awake()
    {
        SettingsOverlay.EnsureInstance();
        BuildEventSystemIfMissing();
        BuildHierarchy();
        BindEvents();
        SetState(MenuState.Main);
    }

    void OnEnable()
    {
        SubscribeConnectionEvents();
    }

    void OnDisable()
    {
        UnsubscribeConnectionEvents();
    }

    void Update()
    {
        UpdateConnectedVisibility();
        UpdateStatusVisibility();
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
        connectedStatusPanel = BuildConnectedStatusPanel(transform);

        versionText = AddText(transform, "Version", "v0.1 MVP", 14,
            new Color(0.25f, 0.32f, 0.28f, 1f), TextAlignmentOptions.Center);
        var versionRt = versionText.rectTransform;
        versionRt.anchorMin = new Vector2(0f, 0f);
        versionRt.anchorMax = new Vector2(1f, 0f);
        versionRt.pivot = new Vector2(0.5f, 0f);
        versionRt.anchoredPosition = new Vector2(0f, 16f);
        versionRt.sizeDelta = new Vector2(0f, 24f);
        if (usingBakedMenuArt)
            versionText.gameObject.SetActive(false);
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

        usingBakedMenuArt = false;
        var tex = Resources.Load<Texture2D>("UI/MainMenuBg");
        if (tex != null)
        {
            image.sprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            image.color = Color.white;
            image.preserveAspect = false;
            image.type = Image.Type.Simple;
        }
        else
        {
            image.color = new Color(0.04f, 0.05f, 0.045f, 1f);
        }
        return image;
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
        image.color = usingBakedMenuArt ? new Color(0f, 0f, 0f, 0f) : new Color(0f, 0f, 0f, 0.08f);
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

        // ─── Title block (top-left of the screen) ──────────────────────
        if (!usingBakedMenuArt)
        {
        titleText = AddText(panel.transform, "Title", "BLACK COMMISSION", 54,
            AgedPaper, TextAlignmentOptions.Left);
        titleText.fontStyle = FontStyles.Bold;
        var titleRt = titleText.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 1f);
        titleRt.anchorMax = new Vector2(0f, 1f);
        titleRt.pivot = new Vector2(0f, 1f);
        titleRt.anchoredPosition = new Vector2(48f, -40f);
        titleRt.sizeDelta = new Vector2(760f, 64f);

        subtitleText = AddText(panel.transform, "Subtitle", MvpLocale.T("subtitle"), 20,
            DispatchGreen, TextAlignmentOptions.Left);
        var subRt = subtitleText.rectTransform;
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(0f, 1f);
        subRt.pivot = new Vector2(0f, 1f);
        subRt.anchoredPosition = new Vector2(50f, -104f);
        subRt.sizeDelta = new Vector2(760f, 28f);
        }

        // ─── Agent name field (top-left, works in baked + procedural modes) ─
        BuildNameField(panel.transform);

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

    // Top-left "Agent Name" label + input. Persisted to PlayerProfile and synced at spawn.
    void BuildNameField(Transform parent)
    {
        var label = AddText(parent, "NameLabel", MvpLocale.T("player_name"), 16,
            DispatchGreen, TextAlignmentOptions.Left);
        var lRt = label.rectTransform;
        lRt.anchorMin = new Vector2(0f, 1f);
        lRt.anchorMax = new Vector2(0f, 1f);
        lRt.pivot = new Vector2(0f, 1f);
        lRt.anchoredPosition = new Vector2(50f, -150f);
        lRt.sizeDelta = new Vector2(320f, 22f);

        var go = new GameObject("NameInput",
            typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = FieldBg;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(50f, -176f);
        rt.sizeDelta = new Vector2(300f, 44f);

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
        if (usingBakedMenuArt)
        {
            BuildReferenceMenuHotspots(parent);
            return;
        }

        var card = CreateMenuPanel(parent);
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.5f, 0.5f);
        cardRt.anchorMax = new Vector2(0.5f, 0.5f);
        cardRt.pivot = new Vector2(0.5f, 0.5f);
        cardRt.anchoredPosition = new Vector2(0f, 20f);
        cardRt.sizeDelta = new Vector2(600f, 790f);

        // Five stacked options, primary (green) first — matches the mockup.
        continueBtn = CreateMenuRow(card.transform, "ContinueBtn", "CONTINUE SHIFT",
            "Resume previous shift", -202f, true);
        newOfficeBtn = CreateMenuRow(card.transform, "NewOfficeBtn", "NEW OFFICE",
            "Accept a clean commission", -325f, false);
        joinBtn = CreateMenuRow(card.transform, "JoinBtn", "JOIN OFFICE",
            "Join another office", -430f, false);
        settingsBtn = CreateMenuRow(card.transform, "SettingsBtn", "SETTINGS",
            "Adjust preferences", -535f, false);
        quitBtn = CreateMenuRow(card.transform, "QuitBtn", "QUIT",
            "Exit to desktop", -630f, false);

        // Small direct-connect (LAN) link at the bottom of the card.
        directBtn = CreateButton(card.transform, "DirectBtn", "LAN DIRECT", 14,
            new Color(0.02f, 0.016f, 0.010f, 0.18f),
            new Color(0.28f, 0.22f, 0.12f, 0.28f),
            new Color(0.12f, 0.09f, 0.05f, 0.42f));
        var dRt = directBtn.GetComponent<RectTransform>();
        dRt.anchorMin = new Vector2(0.5f, 0f);
        dRt.anchorMax = new Vector2(0.5f, 0f);
        dRt.pivot = new Vector2(0.5f, 0f);
        dRt.anchoredPosition = new Vector2(0f, 74f);
        dRt.sizeDelta = new Vector2(190f, 32f);
        var directLabel = directBtn.GetComponentInChildren<TMP_Text>();
        if (directLabel != null)
        {
            directLabel.color = new Color(0.66f, 0.58f, 0.40f, 0.78f);
            directLabel.fontStyle = FontStyles.Bold;
            directLabel.characterSpacing = 8f;
        }
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
            image.color = new Color(0.075f, 0.060f, 0.043f, 0.92f);
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
        quitBtn = CreateMenuHotspot(parent, "QuitHotspot", new Vector2(3f, -232f), new Vector2(512f, 88f));

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
        Color bg = new Color(0.015f, 0.012f, 0.008f, 0.06f);
        Color bgHover = new Color(0.10f, 0.18f, 0.10f, 0.24f);
        Color bgPressed = new Color(0.06f, 0.12f, 0.06f, 0.38f);

        var btn = CreateButton(parent, name, "", 1, bg, bgHover, bgPressed);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, yFromTop);
        rt.sizeDelta = new Vector2(440f, primary ? 96f : 76f);

        // Drop the default centred empty label; we add our own title + description.
        var emptyLabel = btn.GetComponentInChildren<TMP_Text>();
        if (emptyLabel != null) Destroy(emptyLabel.gameObject);

        var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(btn.transform, false);
        var aImg = accent.GetComponent<Image>();
        aImg.color = new Color(0.55f, 0.86f, 0.58f, 0f);
        aImg.raycastTarget = false;
        var aRt = accent.GetComponent<RectTransform>();
        aRt.anchorMin = new Vector2(0f, 0f);
        aRt.anchorMax = new Vector2(0f, 1f);
        aRt.pivot = new Vector2(0f, 0.5f);
        aRt.anchoredPosition = new Vector2(28f, 0f);
        aRt.sizeDelta = new Vector2(5f, -14f);

        var rowIcon = AddText(btn.transform, "Icon", "-", primary ? 28 : 20,
            new Color(0.54f, 0.46f, 0.31f, 0.58f), TextAlignmentOptions.Center);
        rowIcon.fontStyle = FontStyles.Bold;
        rowIcon.raycastTarget = false;
        var iRt = rowIcon.rectTransform;
        iRt.anchorMin = new Vector2(0f, 0.5f);
        iRt.anchorMax = new Vector2(0f, 0.5f);
        iRt.pivot = new Vector2(0.5f, 0.5f);
        iRt.anchoredPosition = new Vector2(54f, primary ? 1f : 0f);
        iRt.sizeDelta = new Vector2(36f, 40f);

        var rowTitle = AddText(btn.transform, "Title", title, primary ? 30 : 27,
            new Color(0.80f, 0.73f, 0.58f, 0.92f), TextAlignmentOptions.Left);
        rowTitle.fontStyle = FontStyles.Bold;
        rowTitle.raycastTarget = false;
        var tRt = rowTitle.rectTransform;
        tRt.anchorMin = new Vector2(0f, 1f);
        tRt.anchorMax = new Vector2(1f, 1f);
        tRt.pivot = new Vector2(0.5f, 1f);
        tRt.anchoredPosition = new Vector2(84f, primary ? -20f : -12f);
        tRt.sizeDelta = new Vector2(-128f, 34f);
        rowTitle.characterSpacing = primary ? 5f : 3f;

        var rowDesc = AddText(btn.transform, "Desc", desc, 14,
            new Color(0.58f, 0.53f, 0.42f, 0.80f), TextAlignmentOptions.Left);
        rowDesc.raycastTarget = false;
        var dRt = rowDesc.rectTransform;
        dRt.anchorMin = new Vector2(0f, 0f);
        dRt.anchorMax = new Vector2(1f, 0f);
        dRt.pivot = new Vector2(0.5f, 0f);
        dRt.anchoredPosition = new Vector2(84f, primary ? 22f : 12f);
        dRt.sizeDelta = new Vector2(-128f, 22f);

        ConfigureMenuRowHover(btn, rowIcon, rowTitle, rowDesc, aImg, primary);

        return btn;
    }

    void ConfigureMenuRowHover(Button btn, TMP_Text icon, TMP_Text title, TMP_Text desc,
        Image accent, bool primary)
    {
        Color normalTitle = new Color(0.80f, 0.73f, 0.58f, 0.92f);
        Color normalIcon = new Color(0.54f, 0.46f, 0.31f, 0.58f);
        Color normalDesc = new Color(0.58f, 0.53f, 0.42f, 0.80f);
        Color green = new Color(0.55f, 0.86f, 0.58f, 0.96f);
        Color greenDim = new Color(0.55f, 0.86f, 0.58f, 0.72f);

        void ApplyHover(bool hovered)
        {
            title.color = hovered ? green : normalTitle;
            icon.color = hovered ? green : normalIcon;
            desc.color = hovered ? greenDim : normalDesc;
            accent.color = hovered
                ? new Color(0.55f, 0.86f, 0.58f, 1f)
                : new Color(0.55f, 0.86f, 0.58f, 0f);
            icon.text = hovered ? ">" : "-";
        }

        var trigger = btn.gameObject.AddComponent<EventTrigger>();
        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ApplyHover(true));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => ApplyHover(false));
        trigger.triggers.Add(exit);
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

        // Two red strip overlays (top + bottom borders) to match the OnGUI stamp look.
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
            new Color(0.96f, 0.42f, 0.34f), TextAlignmentOptions.Center);
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

        return panel;
    }

    GameObject BuildConnectingPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "ConnectingPanel", new Vector2(420f, 200f));
        panel.SetActive(false);

        connectingText = AddText(panel.transform, "ConnText", "", 24,
            AgedPaper, TextAlignmentOptions.Center);
        connectingText.rectTransform.anchoredPosition = new Vector2(0f, 14f);

        AddText(panel.transform, "PleaseWait", MvpLocale.T("please_wait"), 14,
            HintText, TextAlignmentOptions.Center)
            .rectTransform.anchoredPosition = new Vector2(0f, -38f);
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
        var panel = CreatePanel(parent, "SettingsPanel", new Vector2(440f, 360f));
        panel.SetActive(false);

        AddText(panel.transform, "Header", MvpLocale.T("game"), 22,
            DispatchGreen, TextAlignmentOptions.Left)
            .rectTransform.anchoredPosition = new Vector2(0f, 140f);

        settingsCloseBtn = CreateButton(panel.transform, "Close", "X", 18,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        var cbRt = settingsCloseBtn.GetComponent<RectTransform>();
        cbRt.anchorMin = new Vector2(1f, 1f);
        cbRt.anchorMax = new Vector2(1f, 1f);
        cbRt.pivot = new Vector2(1f, 1f);
        cbRt.anchoredPosition = new Vector2(-12f, -12f);
        cbRt.sizeDelta = new Vector2(38f, 32f);

        BuildLangSection(panel.transform, new Vector2(0f, 70f));
        BuildVolumeSection(panel.transform, new Vector2(0f, 0f));
        BuildSensitivitySection(panel.transform, new Vector2(0f, -90f));

        return panel;
    }

    void BuildLangSection(Transform parent, Vector2 position)
    {
        AddText(parent, "LangLabel", "语言 / Language", 14,
            HintText, TextAlignmentOptions.Left)
            .rectTransform.anchoredPosition = position + new Vector2(0f, 18f);

        var zhBtn = CreateButton(parent, "ZhBtn", "简体中文", 16,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        zhBtn.GetComponent<RectTransform>().anchoredPosition = position + new Vector2(-90f, -10f);
        zhBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 38f);
        zhBtn.onClick.AddListener(() => { MvpHud.LanguageIndexStatic = 0; RebuildAllLabels(); });

        var enBtn = CreateButton(parent, "EnBtn", "English", 16,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        enBtn.GetComponent<RectTransform>().anchoredPosition = position + new Vector2(90f, -10f);
        enBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 38f);
        enBtn.onClick.AddListener(() => { MvpHud.LanguageIndexStatic = 1; RebuildAllLabels(); });
    }

    void BuildVolumeSection(Transform parent, Vector2 position)
    {
        AddText(parent, "VolumeLabel", "音量 / Master Volume", 14,
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
        AddText(parent, "SensLabel", "鼠标灵敏度 / Sensitivity", 14,
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
            new Color(0.02f, 0.016f, 0.010f, 0.20f),
            new Color(0.24f, 0.18f, 0.10f, 0.28f),
            new Color(0.15f, 0.10f, 0.05f, 0.40f));
        var closeRt = directCloseBtn.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-62f, -142f);
        closeRt.sizeDelta = new Vector2(42f, 34f);
        var closeLabel = directCloseBtn.GetComponentInChildren<TMP_Text>();
        if (closeLabel != null)
            closeLabel.color = new Color(0.80f, 0.73f, 0.58f, 0.92f);

        var header = AddText(panel.transform, "Header", "LAN DIRECT", 28,
            DispatchGreen, TextAlignmentOptions.Center);
        header.fontStyle = FontStyles.Bold;
        header.characterSpacing = 6f;
        header.rectTransform.anchoredPosition = new Vector2(0f, 126f);
        header.rectTransform.sizeDelta = new Vector2(300f, 38f);

        AddText(panel.transform, "Hint", "Local host or join by IP", 14,
            new Color(0.58f, 0.53f, 0.42f, 0.82f), TextAlignmentOptions.Center)
            .rectTransform.anchoredPosition = new Vector2(0f, 88f);

        directAddressInput = CreateInputField(panel.transform, "AddressInput",
            connectAddress, new Vector2(-70f, 22f), new Vector2(218f, 44f));
        directPortInput = CreateInputField(panel.transform, "PortInput",
            connectPort, new Vector2(106f, 22f), new Vector2(96f, 44f));

        var hostDirectBtn = CreateButton(panel.transform, "DirectHost",
            MvpLocale.T("direct_create"), 16,
            new Color(0.015f, 0.012f, 0.008f, 0.22f),
            new Color(0.10f, 0.18f, 0.10f, 0.30f),
            new Color(0.06f, 0.12f, 0.06f, 0.44f));
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
            new Color(0.015f, 0.012f, 0.008f, 0.22f),
            new Color(0.24f, 0.18f, 0.10f, 0.30f),
            new Color(0.15f, 0.10f, 0.05f, 0.44f));
        joinDirectBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(84f, -56f);
        joinDirectBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, 46f);
        var joinLabel = joinDirectBtn.GetComponentInChildren<TMP_Text>();
        if (joinLabel != null)
        {
            joinLabel.color = new Color(0.80f, 0.73f, 0.58f, 0.94f);
            joinLabel.fontStyle = FontStyles.Bold;
        }
        joinDirectBtn.onClick.AddListener(StartClientDirect);

        return panel;
    }

    GameObject BuildConnectedStatusPanel(Transform parent)
    {
        const float headerH = 24f;
        const float rowH = 30f;
        float panelH = 16f + headerH + 6f + rowH * 4 + 12f;

        var panel = new GameObject("ConnectedStatus", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = new Color(0.055f, 0.078f, 0.070f, 0.88f);
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
                new Color(0.30f, 0.10f, 0.08f, 0.85f),
                new Color(0.45f, 0.14f, 0.11f, 0.92f),
                new Color(0.22f, 0.07f, 0.05f, 0.95f));
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

    void AddInsetFrame(Transform parent, string name, Color color, float inset, float thickness)
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
    }

    void AddFrameEdge(Transform parent, string name, Color color, Vector2 anchorMin,
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

        var labelText = AddText(go.transform, "Label", label, fontSize, Color.white, TextAlignmentOptions.Center);
        var labelRt = labelText.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = Vector2.zero;
        labelRt.offsetMax = Vector2.zero;
        labelText.raycastTarget = false;

        return btn;
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
        settingsBtn.onClick.AddListener(() => SettingsOverlay.Toggle());
        quitBtn.onClick.AddListener(QuitGame);
        directBtn.onClick.AddListener(ToggleDirectConnect);
        joinSubmitBtn.onClick.AddListener(StartJoin);
        joinCancelBtn.onClick.AddListener(() =>
        {
            joinCode = "";
            if (joinCodeInput != null) joinCodeInput.text = "";
            SetState(MenuState.Main);
        });
        settingsCloseBtn.onClick.AddListener(() => settingsPanel.SetActive(false));
        directCloseBtn.onClick.AddListener(() => directConnectPanel.SetActive(false));
        hostCodeHideBtn.onClick.AddListener(() => hostCodePanel.SetActive(false));

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

    void SetState(MenuState next)
    {
        state = next;
        mainPanel.SetActive(state == MenuState.Main);
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

    void UpdateStatusVisibility()
    {
        if (statusText == null) return;
        bool live = !string.IsNullOrEmpty(statusMessage) && Time.unscaledTime < statusMessageUntil;
        // Status bar is reserved for transient error / success messages only.
        // The dispatch card already shows the create_hint as a permanent caption,
        // so don't echo it here — that was causing the same line to render twice.
        statusText.text = live ? statusMessage : "";
        statusText.color = live ? new Color(0.96f, 0.42f, 0.34f) : HintText;
    }

    void UpdateConnectedVisibility()
    {
        bool listening = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening;

        // Once connected, hide every menu panel except a small corner status badge.
        if (listening)
        {
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
            if (state != MenuState.HostWaiting && hostCodePanel.activeSelf) hostCodePanel.SetActive(false);
            if (versionText != null && versionText.gameObject.activeSelf) versionText.gameObject.SetActive(false);

            connectedStatusPanel.SetActive(true);
            RefreshRoster();

            if (state == MenuState.HostWaiting && !string.IsNullOrEmpty(hostJoinCode))
            {
                hostCodePanel.SetActive(true);
                if (hostCodeText != null) hostCodeText.text = hostJoinCode;
            }
        }
        else
        {
            if (backgroundImage != null && !backgroundImage.gameObject.activeSelf)
                backgroundImage.gameObject.SetActive(true);
            if (!screenVeil.activeSelf) screenVeil.SetActive(true);
            if (versionText != null && !usingBakedMenuArt && !versionText.gameObject.activeSelf)
                versionText.gameObject.SetActive(true);
            connectedStatusPanel.SetActive(false);
            if (state == MenuState.Main && !mainPanel.activeSelf) mainPanel.SetActive(true);
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
    }
}
