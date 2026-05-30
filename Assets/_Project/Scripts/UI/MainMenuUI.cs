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
    static readonly Color PanelBg = new(0.055f, 0.078f, 0.070f, 0.96f);
    static readonly Color PanelBorder = new(0.184f, 0.310f, 0.294f, 0.85f);
    static readonly Color ScreenFog = new(0.012f, 0.016f, 0.014f, 0.78f);
    static readonly Color DispatchGreen = new(0.482f, 0.812f, 0.541f);
    static readonly Color DispatchGreenDark = new(0.133f, 0.349f, 0.227f);
    static readonly Color StampRed = new(0.761f, 0.227f, 0.169f);
    static readonly Color SodiumAmber = new(0.851f, 0.604f, 0.192f);
    static readonly Color AgedPaper = new(0.839f, 0.784f, 0.608f);
    static readonly Color DirtyBone = new(0.788f, 0.761f, 0.667f);
    static readonly Color FieldBg = new(0.020f, 0.028f, 0.024f, 1f);
    static readonly Color BtnPrimary = new(0.133f, 0.349f, 0.227f);
    static readonly Color BtnPrimaryHover = new(0.180f, 0.480f, 0.310f);
    static readonly Color BtnPrimaryPressed = new(0.080f, 0.220f, 0.150f);
    static readonly Color BtnSecondary = new(0.067f, 0.098f, 0.082f);
    static readonly Color BtnSecondaryHover = new(0.110f, 0.155f, 0.130f);
    static readonly Color BtnSecondaryPressed = new(0.040f, 0.060f, 0.050f);
    static readonly Color HintText = new(0.55f, 0.65f, 0.58f);

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
    TMP_Text uniformLabelText;
    TMP_Text statusText;
    TMP_Text hostCodeText;
    TMP_Text connectingText;
    TMP_Text versionText;
    TMP_Text connectedStatusText;

    Image uniformSwatch;
    Image vestSwatch;
    Image helmetSwatch;

    TMP_InputField joinCodeInput;
    TMP_InputField directAddressInput;
    TMP_InputField directPortInput;

    RawImage characterPreviewImage;
    MainMenuCharacterPreview characterPreview;

    Button hostBtn;
    Button joinBtn;
    Button settingsBtn;
    Button directBtn;
    Button charPrevBtn;
    Button charNextBtn;
    Button joinSubmitBtn;
    Button joinCancelBtn;
    Button settingsCloseBtn;
    Button hostCodeHideBtn;

    void Awake()
    {
        BuildEventSystemIfMissing();
        BuildHierarchy();
        BindEvents();
        EnsureCharacterPreviewStage();
        RefreshCharacterDisplay();
        SetState(MenuState.Main);
    }

    void EnsureCharacterPreviewStage()
    {
        var preview = FindFirstObjectByType<MainMenuCharacterPreview>();
        if (preview == null)
        {
            var stage = new GameObject("MainMenuCharacterPreview");
            preview = stage.AddComponent<MainMenuCharacterPreview>();
        }
        AttachCharacterPreview(preview, preview.Texture);
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
        image.color = ScreenFog;
        image.raycastTarget = false; // tints the screen only — must not block panel buttons
        return go;
    }

    GameObject BuildMainPanel(Transform parent)
    {
        var panel = CreatePanel(parent, "MainPanel", new Vector2(900f, 620f));
        var rt = panel.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(0f, 0f);

        // ─── Title block (top of panel) ────────────────────────────────
        var titleRow = CreateRow(panel.transform, "TitleRow", 80f, 0f, 24f);
        titleText = AddText(titleRow.transform, "Title", "BLACK COMMISSION", 42,
            DispatchGreen, TextAlignmentOptions.Left);
        var titleRt = titleText.rectTransform;
        titleRt.anchorMin = new Vector2(0f, 0f);
        titleRt.anchorMax = new Vector2(0.7f, 1f);
        titleRt.offsetMin = new Vector2(32f, 0f);
        titleRt.offsetMax = new Vector2(0f, 0f);
        titleText.fontStyle = FontStyles.Bold;

        var debtStamp = CreateDebtStamp(titleRow.transform);
        var stampRt = debtStamp.GetComponent<RectTransform>();
        stampRt.anchorMin = new Vector2(1f, 0.5f);
        stampRt.anchorMax = new Vector2(1f, 0.5f);
        stampRt.pivot = new Vector2(1f, 0.5f);
        stampRt.anchoredPosition = new Vector2(-32f, 0f);
        stampRt.sizeDelta = new Vector2(120f, 40f);

        subtitleText = AddText(panel.transform, "Subtitle", MvpLocale.T("subtitle"), 20,
            AgedPaper, TextAlignmentOptions.Left);
        var subRt = subtitleText.rectTransform;
        subRt.anchorMin = new Vector2(0f, 1f);
        subRt.anchorMax = new Vector2(1f, 1f);
        subRt.pivot = new Vector2(0.5f, 1f);
        subRt.anchoredPosition = new Vector2(0f, -86f);
        subRt.sizeDelta = new Vector2(-64f, 28f);

        // ─── Two columns: character preview (left) + dispatch (right) ──
        BuildCharacterColumn(panel.transform);
        BuildDispatchColumn(panel.transform);

        // ─── Status message bar (above footer) ─────────────────────────
        statusText = AddText(panel.transform, "StatusBar", "", 16,
            HintText, TextAlignmentOptions.Center);
        var statusRt = statusText.rectTransform;
        statusRt.anchorMin = new Vector2(0f, 0f);
        statusRt.anchorMax = new Vector2(1f, 0f);
        statusRt.pivot = new Vector2(0.5f, 0f);
        statusRt.anchoredPosition = new Vector2(0f, 80f);
        statusRt.sizeDelta = new Vector2(-64f, 36f);

        // ─── Footer row (settings + direct connect) ────────────────────
        var footer = CreateRow(panel.transform, "FooterRow", 48f, 0f, 16f);
        var footerRt = footer.GetComponent<RectTransform>();
        footerRt.anchorMin = new Vector2(0f, 0f);
        footerRt.anchorMax = new Vector2(1f, 0f);
        footerRt.pivot = new Vector2(0.5f, 0f);
        footerRt.anchoredPosition = new Vector2(0f, 16f);
        footerRt.sizeDelta = new Vector2(-64f, 48f);

        settingsBtn = CreateButton(footer.transform, "SettingsBtn",
            "⚙  " + MvpLocale.T("game"), 18, BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        var sbRt = settingsBtn.GetComponent<RectTransform>();
        sbRt.anchorMin = new Vector2(0f, 0.5f);
        sbRt.anchorMax = new Vector2(0f, 0.5f);
        sbRt.pivot = new Vector2(0f, 0.5f);
        sbRt.anchoredPosition = new Vector2(0f, 0f);
        sbRt.sizeDelta = new Vector2(220f, 44f);

        directBtn = CreateButton(footer.transform, "DirectBtn",
            "▶  " + MvpLocale.T("show_direct"), 18, BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        var dbRt = directBtn.GetComponent<RectTransform>();
        dbRt.anchorMin = new Vector2(1f, 0.5f);
        dbRt.anchorMax = new Vector2(1f, 0.5f);
        dbRt.pivot = new Vector2(1f, 0.5f);
        dbRt.anchoredPosition = new Vector2(0f, 0f);
        dbRt.sizeDelta = new Vector2(260f, 44f);

        return panel;
    }

    void BuildCharacterColumn(Transform panel)
    {
        // Layout budget (310 wide × 380 tall, top-down, every section has explicit gap):
        //   12 pad | 18 header | 6 gap | 232 preview | 10 gap | 24 label | 8 gap
        //     | 24 swatches | 12 gap | 38 buttons | 6 pad        sum = 380
        // Preview is now the dominant element; swatches and arrows reduced so they
        // don't compete with the character art.
        var card = CreatePanel(panel, "CharacterCard", new Vector2(310f, 380f));
        card.GetComponent<Image>().color = FieldBg;
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0f, 0.5f);
        cardRt.anchorMax = new Vector2(0f, 0.5f);
        cardRt.pivot = new Vector2(0f, 0.5f);
        cardRt.anchoredPosition = new Vector2(32f, -10f);

        // Header: "UNIFORM FILE"
        var header = AddText(card.transform, "Header", "UNIFORM FILE", 13,
            HintText, TextAlignmentOptions.Center);
        AnchorTopStretch(header.rectTransform, yFromTop: 12f, height: 18f, sidePadding: 10f);

        // 3D character preview window — taller, dominates the card.
        characterPreviewImage = new GameObject("PreviewWindow",
            typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        characterPreviewImage.transform.SetParent(card.transform, false);
        characterPreviewImage.color = Color.white;
        var prRt = characterPreviewImage.rectTransform;
        prRt.anchorMin = new Vector2(0.5f, 1f);
        prRt.anchorMax = new Vector2(0.5f, 1f);
        prRt.pivot = new Vector2(0.5f, 1f);
        prRt.anchoredPosition = new Vector2(0f, -36f);
        prRt.sizeDelta = new Vector2(280f, 254f);

        // Variant name label (e.g. "Standard"). Smaller than before.
        uniformLabelText = AddText(card.transform, "VariantLabel", "Standard", 18,
            DispatchGreen, TextAlignmentOptions.Center);
        uniformLabelText.fontStyle = FontStyles.Bold;
        AnchorTopStretch(uniformLabelText.rectTransform, yFromTop: 298f, height: 24f, sidePadding: 20f);

        // Swatch row — smaller swatches, accent role only.
        var swatchRow = new GameObject("SwatchRow", typeof(RectTransform));
        swatchRow.transform.SetParent(card.transform, false);
        AnchorTopStretch(swatchRow.GetComponent<RectTransform>(), yFromTop: 330f, height: 30f, sidePadding: 54f);

        const float swatchWidth = 42f;
        const float swatchHeight = 22f;
        const float swatchGap = 10f;
        const float totalSwatchSpan = swatchWidth * 3 + swatchGap * 2;
        for (int i = 0; i < 3; i++)
        {
            float xCenter = -totalSwatchSpan * 0.5f + swatchWidth * 0.5f + i * (swatchWidth + swatchGap);
            var swatch = CreateSwatch(swatchRow.transform, $"Swatch_{i}", xCenter, swatchWidth, swatchHeight);
            if (i == 0) uniformSwatch = swatch;
            else if (i == 1) vestSwatch = swatch;
            else helmetSwatch = swatch;
        }

        // Prev / next buttons — smaller, accent role only.
        charPrevBtn = CreateButton(card.transform, "CharPrevBtn", "<", 18,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        var pbRt = charPrevBtn.GetComponent<RectTransform>();
        pbRt.anchorMin = new Vector2(0f, 1f);
        pbRt.anchorMax = new Vector2(0f, 1f);
        pbRt.pivot = new Vector2(0f, 1f);
        pbRt.anchoredPosition = new Vector2(24f, -330f);
        pbRt.sizeDelta = new Vector2(34f, 30f);

        charNextBtn = CreateButton(card.transform, "CharNextBtn", ">", 18,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        var nbRt = charNextBtn.GetComponent<RectTransform>();
        nbRt.anchorMin = new Vector2(1f, 1f);
        nbRt.anchorMax = new Vector2(1f, 1f);
        nbRt.pivot = new Vector2(1f, 1f);
        nbRt.anchoredPosition = new Vector2(-24f, -330f);
        nbRt.sizeDelta = new Vector2(34f, 30f);
    }

    Image CreateSwatch(Transform parent, string name, float xCenter, float width, float height)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);
        var rt = root.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = new Vector2(xCenter, 0f);
        rt.sizeDelta = new Vector2(width, height);
        root.GetComponent<Image>().color = Color.white;
        return root.GetComponent<Image>();
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

    void BuildDispatchColumn(Transform panel)
    {
        var card = CreatePanel(panel, "DispatchCard", new Vector2(480f, 380f));
        card.GetComponent<Image>().color = new Color(0.030f, 0.045f, 0.038f, 0.65f);
        var cardRt = card.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(1f, 0.5f);
        cardRt.anchorMax = new Vector2(1f, 0.5f);
        cardRt.pivot = new Vector2(1f, 0.5f);
        cardRt.anchoredPosition = new Vector2(-32f, -10f);

        var header = AddText(card.transform, "Header", "DISPATCH", 14,
            HintText, TextAlignmentOptions.Left);
        var hRt = header.rectTransform;
        hRt.anchorMin = new Vector2(0f, 1f);
        hRt.anchorMax = new Vector2(1f, 1f);
        hRt.pivot = new Vector2(0.5f, 1f);
        hRt.anchoredPosition = new Vector2(0f, -16f);
        hRt.sizeDelta = new Vector2(-24f, 18f);

        // ─── Host (primary) button ─────────────────────────────────────
        hostBtn = CreateButton(card.transform, "HostBtn",
            MvpLocale.T("create_office"), 24,
            BtnPrimary, BtnPrimaryHover, BtnPrimaryPressed);
        var hbRt = hostBtn.GetComponent<RectTransform>();
        hbRt.anchorMin = new Vector2(0f, 1f);
        hbRt.anchorMax = new Vector2(1f, 1f);
        hbRt.pivot = new Vector2(0.5f, 1f);
        hbRt.anchoredPosition = new Vector2(0f, -52f);
        hbRt.sizeDelta = new Vector2(-32f, 72f);
        var hbLabel = hostBtn.GetComponentInChildren<TMP_Text>();
        if (hbLabel != null) hbLabel.fontStyle = FontStyles.Bold;

        // Subtle dispatch-green tag bar inside the host button (left edge accent).
        var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        accent.transform.SetParent(hostBtn.transform, false);
        accent.GetComponent<Image>().color = DispatchGreen;
        var accentRt = accent.GetComponent<RectTransform>();
        accentRt.anchorMin = new Vector2(0f, 0f);
        accentRt.anchorMax = new Vector2(0f, 1f);
        accentRt.pivot = new Vector2(0f, 0.5f);
        accentRt.anchoredPosition = new Vector2(0f, 0f);
        accentRt.sizeDelta = new Vector2(6f, 0f);

        // ─── Join (secondary) button ───────────────────────────────────
        joinBtn = CreateButton(card.transform, "JoinBtn",
            MvpLocale.T("join_office"), 22,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        var jbRt = joinBtn.GetComponent<RectTransform>();
        jbRt.anchorMin = new Vector2(0f, 1f);
        jbRt.anchorMax = new Vector2(1f, 1f);
        jbRt.pivot = new Vector2(0.5f, 1f);
        jbRt.anchoredPosition = new Vector2(0f, -140f);
        jbRt.sizeDelta = new Vector2(-32f, 56f);

        // Hint text.
        var hint = AddText(card.transform, "Hint", MvpLocale.T("create_hint"), 16,
            HintText, TextAlignmentOptions.Center);
        var hintRt = hint.rectTransform;
        hint.enableWordWrapping = true;
        hintRt.anchorMin = new Vector2(0f, 0f);
        hintRt.anchorMax = new Vector2(1f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 24f);
        hintRt.sizeDelta = new Vector2(-32f, 80f);
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
        var panel = CreatePanel(parent, "DirectConnectPanel", new Vector2(480f, 220f));
        panel.SetActive(false);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 80f);

        AddText(panel.transform, "Header", MvpLocale.T("show_direct"), 16,
            HintText, TextAlignmentOptions.Left)
            .rectTransform.anchoredPosition = new Vector2(-200f, 80f);

        directAddressInput = CreateInputField(panel.transform, "AddressInput",
            connectAddress, new Vector2(-110f, 30f), new Vector2(220f, 38f));
        directPortInput = CreateInputField(panel.transform, "PortInput",
            connectPort, new Vector2(120f, 30f), new Vector2(120f, 38f));

        var hostDirectBtn = CreateButton(panel.transform, "DirectHost",
            MvpLocale.T("direct_create"), 16,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        hostDirectBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(-110f, -30f);
        hostDirectBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 38f);
        hostDirectBtn.onClick.AddListener(StartHostDirect);

        var joinDirectBtn = CreateButton(panel.transform, "DirectJoin",
            MvpLocale.T("direct_join"), 16,
            BtnSecondary, BtnSecondaryHover, BtnSecondaryPressed);
        joinDirectBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(120f, -30f);
        joinDirectBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(220f, 38f);
        joinDirectBtn.onClick.AddListener(StartClientDirect);

        return panel;
    }

    GameObject BuildConnectedStatusPanel(Transform parent)
    {
        var panel = new GameObject("ConnectedStatus", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        panel.GetComponent<Image>().color = new Color(0.055f, 0.078f, 0.070f, 0.85f);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(20f, -20f);
        rt.sizeDelta = new Vector2(320f, 40f);

        connectedStatusText = AddText(panel.transform, "Text", "", 14,
            DispatchGreen, TextAlignmentOptions.Left);
        var tRt = connectedStatusText.rectTransform;
        tRt.anchorMin = Vector2.zero;
        tRt.anchorMax = Vector2.one;
        tRt.offsetMin = new Vector2(12f, 6f);
        tRt.offsetMax = new Vector2(-12f, -6f);

        panel.SetActive(false);
        return panel;
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
        hostBtn.onClick.AddListener(StartHost);
        joinBtn.onClick.AddListener(() => SetState(MenuState.JoinInput));
        settingsBtn.onClick.AddListener(() => settingsPanel.SetActive(!settingsPanel.activeSelf));
        directBtn.onClick.AddListener(ToggleDirectConnect);
        charPrevBtn.onClick.AddListener(() => CycleCharacter(-1));
        charNextBtn.onClick.AddListener(() => CycleCharacter(1));
        joinSubmitBtn.onClick.AddListener(StartJoin);
        joinCancelBtn.onClick.AddListener(() =>
        {
            joinCode = "";
            if (joinCodeInput != null) joinCodeInput.text = "";
            SetState(MenuState.Main);
        });
        settingsCloseBtn.onClick.AddListener(() => settingsPanel.SetActive(false));
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

        if (state == MenuState.Connecting)
            connectingText.text = MvpLocale.T("connecting") + "...";

        if (state == MenuState.JoinInput && joinCodeInput != null)
        {
            joinCodeInput.text = joinCode ?? "";
            EventSystem.current?.SetSelectedGameObject(joinCodeInput.gameObject);
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
            StartHostDirect();
        }
    }

    void StartJoin()
    {
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetStatus(MvpLocale.T("enter_code_prompt"));
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
            if (screenVeil.activeSelf) screenVeil.SetActive(false);
            if (mainPanel.activeSelf) mainPanel.SetActive(false);
            if (joinPanel.activeSelf) joinPanel.SetActive(false);
            if (connectingPanel.activeSelf) connectingPanel.SetActive(false);
            if (settingsPanel.activeSelf) settingsPanel.SetActive(false);
            if (directConnectPanel.activeSelf) directConnectPanel.SetActive(false);
            if (state != MenuState.HostWaiting && hostCodePanel.activeSelf) hostCodePanel.SetActive(false);
            if (versionText != null && versionText.gameObject.activeSelf) versionText.gameObject.SetActive(false);

            connectedStatusPanel.SetActive(true);
            int clients = NetworkManager.Singleton.ConnectedClientsIds.Count;
            string role = NetworkManager.Singleton.IsHost ? MvpLocale.T("host") : MvpLocale.T("client");
            connectedStatusText.text = MvpLocale.T("connected_status", role, clients);

            if (state == MenuState.HostWaiting && !string.IsNullOrEmpty(hostJoinCode))
            {
                hostCodePanel.SetActive(true);
                if (hostCodeText != null) hostCodeText.text = hostJoinCode;
            }
        }
        else
        {
            if (!screenVeil.activeSelf) screenVeil.SetActive(true);
            if (versionText != null && !versionText.gameObject.activeSelf) versionText.gameObject.SetActive(true);
            connectedStatusPanel.SetActive(false);
            if (state == MenuState.Main && !mainPanel.activeSelf) mainPanel.SetActive(true);
        }
    }

    // ─── Character selection ──────────────────────────────────────────────

    void CycleCharacter(int delta)
    {
        int n = PlayerCharacterPalette.Count;
        int next = ((PlayerCharacterPalette.SavedIndex + delta) % n + n) % n;
        PlayerCharacterPalette.SavedIndex = next;
        RefreshCharacterDisplay();
        if (characterPreview != null) characterPreview.ApplyCharacterIndex(next);
    }

    void RefreshCharacterDisplay()
    {
        int idx = PlayerCharacterPalette.SavedIndex;
        var colors = PlayerCharacterPalette.Get(idx);
        if (uniformLabelText != null) uniformLabelText.text = colors.label;
        if (uniformSwatch != null) uniformSwatch.color = colors.uniform;
        if (vestSwatch != null) vestSwatch.color = colors.vest;
        if (helmetSwatch != null) helmetSwatch.color = colors.helmet;
    }

    public void AttachCharacterPreview(MainMenuCharacterPreview preview, RenderTexture texture)
    {
        characterPreview = preview;
        if (characterPreviewImage != null && texture != null)
            characterPreviewImage.texture = texture;
        if (preview != null) preview.ApplyCharacterIndex(PlayerCharacterPalette.SavedIndex);
    }

    // ─── Direct connect ───────────────────────────────────────────────────

    void ToggleDirectConnect()
    {
        directConnectPanel.SetActive(!directConnectPanel.activeSelf);
    }

    void StartHostDirect()
    {
        if (NetworkManager.Singleton == null) return;
        if (!NetworkManager.Singleton.TryGetComponent<UnityTransport>(out var transport))
        {
            bool started = NetworkManager.Singleton.StartHost();
            SetStatus(started ? MvpLocale.T("host_started") : MvpLocale.T("host_failed"));
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
                SetStatus(MvpLocale.T("direct_host_started", connectAddress, port));
                return;
            }
            NetworkManager.Singleton.Shutdown();
        }
        SetStatus(MvpLocale.T("host_port_busy"));
    }

    void StartClientDirect()
    {
        if (NetworkManager.Singleton == null) return;
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
        var hostLbl = hostBtn != null ? hostBtn.GetComponentInChildren<TMP_Text>() : null;
        if (hostLbl != null) hostLbl.text = MvpLocale.T("create_office");
        var joinLbl = joinBtn != null ? joinBtn.GetComponentInChildren<TMP_Text>() : null;
        if (joinLbl != null) joinLbl.text = MvpLocale.T("join_office");
    }
}
