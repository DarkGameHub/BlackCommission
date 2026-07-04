using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MvpHud : MonoBehaviour
{
    const float OfficeComputerShopDistance = 3.4f;
    static OfficeComputer activeComputer;
    static MissionVanExitPoint activeMissionVan;
    static OfficeCabinetStorage activeCabinet;
    static OfficeMonsterBestiary activeBestiary;
    public static bool IsComputerOpen => activeComputer != null;
    /// <summary>The terminal the local player currently has open, or null. Lets the camera
    /// frame a head-on "seated at the desk" view of the monitor while the terminal is up.</summary>
    public static OfficeComputer ActiveComputer => activeComputer;
    public static bool IsBlockingPanelOpen =>
        activeComputer != null || activeMissionVan != null || activeCabinet != null || activeBestiary != null || SettingsOverlay.IsOpen;

    // Resolution-adaptive HUD canvas. Everything is laid out in a fixed 1080-tall reference space
    // and scaled to the real screen by a GUI.matrix in OnGUI, so all panels/bars/labels keep their
    // proportions at any resolution. Scale is height-driven (stable for first-person PC across
    // standard/ultrawide); RefW tracks the real aspect so the extra width on ultrawide is empty
    // side margin. ScavengeCargoZone reads UiScale to match the same canvas.
    const float RefH = 1080f;
    float uiScale = 1f;
    public static float UiScale { get; private set; } = 1f;
    float RefW => Screen.width / Mathf.Max(0.01f, uiScale);

    [SerializeField] int panelWidth = 390;
    [SerializeField] bool showNetworkHint = false;

    GUIStyle panelStyle;
    GUIStyle titleStyle;
    GUIStyle labelStyle;
    GUIStyle mutedStyle;
    GUIStyle accentStyle;
    GUIStyle warningStyle;
    GUIStyle slotStyle;
    GUIStyle selectedSlotStyle;
    GUIStyle buttonStyle;
    GUIStyle sectionHeaderStyle;
    GUIStyle metaStyle;
    GUIStyle terminalPaperStyle;
    GUIStyle terminalBoxStyle;
    GUIStyle terminalTitleStyle;
    GUIStyle terminalLabelStyle;
    GUIStyle terminalMutedStyle;
    GUIStyle terminalSmallStyle;
    GUIStyle terminalButtonStyle;
    GUIStyle terminalSelectedButtonStyle;
    GUIStyle terminalInverseStyle;   // dark text on green inverse-video (warnings/active tab)
    GUIStyle terminalLabelRightStyle; // right-aligned green label (connection state)
    int terminalTab;                  // 0 = commissions, 1 = supply, 2 = ledger
    Texture2D panelTexture;
    Texture2D slotTexture;
    Texture2D selectedSlotTexture;
    Texture2D terminalPaperTexture;
    Texture2D terminalBoxTexture;
    Texture2D terminalSelectedTexture;
    Texture2D terminalLineTexture;
    Texture2D emptyIcon;
    Texture2D flashlightIcon;
    Texture2D decoyIcon; // reused for battery
    string shopMessage;
    float shopMessageUntil;
    string officeMessage;
    float officeMessageUntil;
    string missionMessage;
    float missionMessageUntil;
    PlayerInteraction cachedLocalInteraction;
    Texture2D crtScanlineTex;
    Texture2D crtVignetteTex;
    Texture2D crtBezelTex;
    Texture2D hpBarBg;
    Texture2D hpBarFill;
    Texture2D staminaBarFill;
    Texture2D staminaBarLowFill;
    Texture2D staminaBarFrame;
    Texture2D hpFillHealthy;   // bone fill for healthy HP in the vitals monitor (low → hpBarFill red)
    Texture2D damageFlashTex;
    float damageFlashUntil;
    float lastKnownHp = 100f;
    Vector2 officeScrollPosition;
    Vector2 cabinetScrollPosition;
    Vector2 bestiaryScrollPosition;
    string cabinetMessage;
    float cabinetMessageUntil;
    static MissionVanExitPoint partialReturnConfirmVan;
    static float partialReturnConfirmUntil;

    static int LanguageIndex
    {
        get => PlayerPrefs.GetInt("AS.Settings.Language", 1); // default 中文 (content is ZH-only)
        set => PlayerPrefs.SetInt("AS.Settings.Language", Mathf.Clamp(value, 0, 1));
    }

    public static int LanguageIndexStatic
    {
        get => LanguageIndex;
        set => LanguageIndex = value;
    }

    static float MasterVolume
    {
        get => PlayerPrefs.GetFloat("AS.Audio.MasterVolume", 1f);
        set
        {
            float volume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat("AS.Audio.MasterVolume", volume);
            AudioListener.volume = volume;
        }
    }

    public static float MasterVolumeStatic
    {
        get => MasterVolume;
        set => MasterVolume = value;
    }

    /// <summary>True while a scene-authored MvpHud exists (HQ). Scenes without one (mission maps)
    /// fall back to SettingsOverlay's own ESC handling.</summary>
    public static bool Present { get; private set; }

    void Awake()
    {
        Present = true;
        activeComputer = null;
        activeMissionVan = null;
        activeCabinet = null;
        activeBestiary = null;
        showNetworkHint = false;
        AudioListener.volume = MasterVolume;
        ProximityVoiceChat.EnsureInstance();
        SettingsOverlay.EnsureInstance();
        if (ScavengeMissionManager.Instance != null)
            RestoreGameplayCursor();
    }

    void OnDestroy()
    {
        Present = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (SettingsOverlay.IsOpen)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
                SettingsOverlay.Close();
            return;
        }

        if (activeMissionVan != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
                CloseMissionVan();
            return;
        }

        if (activeCabinet != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
                CloseCabinet();
            return;
        }

        if (activeBestiary != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
                CloseBestiary();
            return;
        }

        if (activeComputer != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseComputer();
                return;
            }

            // Number keys switch tabs even outside OnGUI (so it feels instant).
            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) terminalTab = 0;
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) terminalTab = 1;
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) terminalTab = 2;

            // E triggers the single primary action (claim / accept / confirm). Debounced
            // so the same E press that opened the terminal doesn't instantly fire it.
            if (keyboard.eKey.wasPressedThisFrame && Time.unscaledTime - computerOpenedAt > 0.25f)
                ExecuteTerminalPrimaryAction(activeComputer);

            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            SettingsOverlay.Open();
            return;
        }

        if (ScavengeMissionManager.Instance != null) return;
    }

    void TryBuy(PlayerHotbar hotbar, MvpHotbarItemId itemId)
    {
        int cost = PlayerHotbar.GetItemCost(itemId);
        if (CompanyData.Current.Funds < cost)
        {
            SetShopMessage($"资金不足: {GetShopItemLabel(itemId)} 需要 {cost}G。");
            return;
        }

        if (!hotbar.CanReceiveItem(itemId, out string reason))
        {
            SetShopMessage($"{GetShopItemLabel(itemId)}无法入库: {reason}");
            return;
        }

        if (hotbar.TryPurchaseItem(itemId))
            SetShopMessage(IsNetworkedPlay()
                ? $"采购申请已提交: {GetShopItemLabel(itemId)}，等待账本同步。"
                : $"采购申请已盖章: {GetShopItemLabel(itemId)} -{cost}G。");
        else
            SetShopMessage($"{GetShopItemLabel(itemId)}采购失败。");
    }

    void TryBuyWristwatch(PlayerHotbar hotbar)
    {
        if (hotbar == null) return;
        if (hotbar.HasWristwatchOwned)
        {
            SetShopMessage("你已经戴着一块廉价工时表。");
            return;
        }

        if (CompanyData.Current.Funds < PlayerHotbar.WristwatchCost)
        {
            SetShopMessage($"资金不足: 廉价工时表需要 {PlayerHotbar.WristwatchCost}G。");
            return;
        }

        if (hotbar.TryPurchaseWristwatch())
            SetShopMessage(IsNetworkedPlay()
                ? "采购申请已提交: 廉价工时表，等待账本同步。"
                : $"采购申请已盖章: 廉价工时表 -{PlayerHotbar.WristwatchCost}G。");
        else
            SetShopMessage("廉价工时表采购失败。");
    }

    void SetShopMessage(string message)
    {
        shopMessage = message;
        shopMessageUntil = Time.time + 2.5f;
    }

    void SetOfficeMessage(string message)
    {
        officeMessage = message;
        officeMessageUntil = Time.time + 3f;
    }

    void OnGUI()
    {
        if (MainMenuUI.IsMenuVisible)
            return;

        // Resolution-adaptive HUD: lay everything out in a fixed 1920x1080-equivalent reference
        // space and let GUI.matrix scale it to the real screen, so every panel/bar/label keeps its
        // proportions at any resolution. Scale is height-driven (stable for first-person PC across
        // standard/ultrawide); RefW absorbs the aspect ratio as empty side margins.
        uiScale = Mathf.Clamp(Screen.height / RefH, 0.5f, 3f);
        UiScale = uiScale;

        EnsureStyles();
        BlackCommissionUiTheme.ApplyButtonSkin(buttonStyle);

        Matrix4x4 savedMatrix = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(uiScale, uiScale, 1f));

        if (ScavengeMissionManager.Instance != null)
        {
            DrawMissionPanel();
            if (activeMissionVan != null)
                DrawMissionVanPanel();
        }
        else
        {
            DrawOfficePanel();
            if (activeCabinet != null)
                DrawCabinetPanel();
            if (activeBestiary != null)
                DrawBestiaryPanel();
        }

        // Always-on inventory bar (HQ + mission). It used to live inside the mission-only
        // branch above, so it was invisible in the HQ; it is HUD chrome like the crosshair/
        // vitals, not mission-specific — draw it whenever a local player owns a hotbar.
        DrawHotbar();

        // Full-screen flash works under the matrix too (RefW x RefH == the real screen when scaled).
        DrawDamageFlash();
        DrawCrosshair();
        DrawGestureHint();
        DrawVitalsBlock();
        if (showNetworkHint)
            DrawFooterHint();

        GUI.matrix = savedMatrix;
    }

    void DrawGestureHint()
    {
        PlayerController localCtrl = FindLocalPlayer();
        if (localCtrl == null) return;
        int gestureId = localCtrl.GestureId.Value;
        if (gestureId <= 0) return;

        string name = PlayerGestures.GetName(gestureId, LanguageIndex);
        if (string.IsNullOrEmpty(name)) return;

        float alpha = Mathf.Clamp01(1.5f - (Time.time % 3f));
        GUI.color = new Color(1f, 1f, 1f, alpha);
        GUI.Label(new Rect(RefW * 0.5f - 80f, RefH - 140f, 160f, 28f), name, accentStyle);
        GUI.color = Color.white;
    }

    void DrawDamageFlash()
    {
        if (Time.time > damageFlashUntil) return;
        if (damageFlashTex == null) return;
        float alpha = (damageFlashUntil - Time.time) / 0.35f;
        GUI.color = new Color(1f, 1f, 1f, alpha * 0.55f);
        if (AccessibilityPrefs.ReducedFlash)
        {
            // 减弱屏闪 (hud.md a11y): an 8px stamp-red edge frame instead of the full-screen wash.
            const float t = 8f;
            GUI.DrawTexture(new Rect(0, 0, RefW, t), damageFlashTex, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(0, RefH - t, RefW, t), damageFlashTex, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(0, 0, t, RefH), damageFlashTex, ScaleMode.StretchToFill);
            GUI.DrawTexture(new Rect(RefW - t, 0, t, RefH), damageFlashTex, ScaleMode.StretchToFill);
        }
        else
        {
            GUI.DrawTexture(new Rect(0, 0, RefW, RefH), damageFlashTex, ScaleMode.StretchToFill);
        }
        GUI.color = Color.white;
    }

    void DrawCrosshair()
    {
        if (IsBlockingPanelOpen || VanTransitOverlay.IsActive) return;

        float cx = RefW * 0.5f;
        float cy = RefH * 0.5f;

        if (cachedLocalInteraction == null)
        {
            PlayerController[] ctrlArr = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var c in ctrlArr)
                if (c.IsOwner) { cachedLocalInteraction = c.GetComponent<PlayerInteraction>(); break; }
        }

        bool hasTarget = cachedLocalInteraction != null && cachedLocalInteraction.CurrentTarget != null;
        // Interact dot warms to sodium amber on a target — the map's "warm point in
        // cold space" signal — instead of screen-only CRT green.
        Color dotColor = hasTarget ? new Color(0.780f, 0.550f, 0.200f, 0.95f) : new Color(0.72f, 0.74f, 0.68f, 0.55f);
        float size = hasTarget ? 6f : 4f;

        Texture2D dot = hpBarBg ?? MakeTexture(Color.white);
        GUI.color = dotColor;
        GUI.DrawTexture(new Rect(cx - size * 0.5f, cy - size * 0.5f, size, size), dot, ScaleMode.StretchToFill);
        GUI.color = Color.white;
    }

    void DrawTerminalHeader(string title, string subtitle = null)
    {
        GUILayout.Label(title, titleStyle);
        GUILayout.FlexibleSpace();
        if (!string.IsNullOrEmpty(subtitle))
            GUILayout.Label(subtitle, mutedStyle, GUILayout.Width(160));
    }

    void DrawTerminalSection(string title)
    {
        GUILayout.Space(8);
        GUILayout.Label(title, sectionHeaderStyle);
    }

    void DrawTerminalBlock(System.Action draw)
    {
        GUILayout.BeginVertical(slotStyle);
        draw?.Invoke();
        GUILayout.EndVertical();
    }

    void DrawLedgerLine(string label, string value, bool warning = false)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, mutedStyle, GUILayout.Width(150));
        GUILayout.Label(value, warning ? warningStyle : metaStyle);
        GUILayout.EndHorizontal();
    }

    void DrawOfficePanel()
    {
        CompanyState company = CompanyData.Current;
        PlayerHotbar localHotbar = FindLocalHotbar();
        bool nearShop = localHotbar != null && IsNearOfficeComputer(localHotbar.transform.position);
        bool computerOpen = activeComputer != null;

        if (!computerOpen)
        {
            // Field-form grammar: a small paper slip, and only when it has something to say
            // (pending reward / locked commission / terminal in reach). The old black
            // "Office on standby." badge was zero-information chrome floating over the wall CRT.
            string officeStatus = null;
            bool warn = false;
            if (MvpPendingReward.HasPending)
            {
                officeStatus = MvpLocale.T("reward_pending", MvpPendingReward.ResultLabel);
                warn = true;
            }
            else if (MvpMissionRuntime.HasSelectedTask && MvpMissionRuntime.SelectedTask != null)
            {
                officeStatus = MvpLocale.T("task_accepted", MvpMissionRuntime.SelectedTask.title);
            }
            else if (nearShop)
            {
                officeStatus = MvpLocale.T("computer_connected");
            }

            if (officeStatus != null)
                DrawOfficeSlip(officeStatus, warn);
            return;
        }

        OfficeComputer computer = activeComputer;
        float computerWidth = Mathf.Clamp(RefW - 48f, 760f, 1050f);
        float computerHeight = Mathf.Clamp(RefH - 76f, 560f, 720f);
        Rect rect = new Rect((RefW - computerWidth) * 0.5f, 38f, computerWidth, computerHeight);

        DrawOfficeManagementTerminal(rect, computer, company, nearShop);

        EnsureCrtTextures();
        DrawCrtOverlay(rect);
    }

    GUIStyle officeSlipStyle;

    // Aged-paper slip, top-left: civic-teal rule + ink text (same grammar as the dispatch
    // ticket strip); the side tab flips stamp-red when a settlement is waiting to be claimed.
    void DrawOfficeSlip(string text, bool warning)
    {
        if (officeSlipStyle == null && GUI.skin != null && GUI.skin.label != null)
        {
            officeSlipStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                wordWrap = false,
                normal = { textColor = new Color(0.10f, 0.095f, 0.075f, 1f) }
            };
            MvpFontProvider.ApplyToStyle(officeSlipStyle);
        }
        if (officeSlipStyle == null) return;

        var slip = new Rect(18f, 18f, 336f, 42f);
        GUI.DrawTexture(new Rect(slip.x - 2f, slip.y - 2f, slip.width + 4f, slip.height + 4f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.Shadow));
        GUI.DrawTexture(slip, BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.OldPaper));
        GUI.DrawTexture(new Rect(slip.x, slip.y, slip.width, 2f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreen));
        GUI.DrawTexture(new Rect(slip.x + 6f, slip.y + 7f, 4f, slip.height - 14f),
            BlackCommissionUiTheme.MakeTex(warning
                ? BlackCommissionUiTheme.RustWarning
                : BlackCommissionUiTheme.MilitaryGreen));
        GUI.Label(new Rect(slip.x + 18f, slip.y + 2f, slip.width - 28f, slip.height - 4f),
            text, officeSlipStyle);
    }

    void DrawCurrentCommissionOrDemo(OfficeComputer computer)
    {
        if (MvpMissionRuntime.HasSelectedTask && MvpMissionRuntime.SelectedTask != null)
        {
            DrawTerminalSection("已锁定委托 / ACTIVE FILE");
            DrawTerminalBlock(() =>
            {
                GUILayout.Label($"已接受委托: {MvpMissionRuntime.SelectedTask.title}", accentStyle);
                GUILayout.Label("采购完道具后，去外面的公司车出发。", mutedStyle);
            });
            return;
        }

        DrawDemoTaskCard(computer);
    }

    static string[] TerminalTabLabels => new[]
        { MvpLocale.T("term_tab_comm"), MvpLocale.T("term_tab_supply"), MvpLocale.T("term_tab_ledger") };

    // Tabbed BC-DOS office management terminal (office-computer-terminal.md): single
    // monochrome-green CRT — Z1 top bar / Z2 status / Z3 tabs / Z4 content / Z5 action.
    void DrawOfficeManagementTerminal(Rect rect, OfficeComputer computer, CompanyState company, bool nearShop)
    {
        GUI.BeginGroup(rect, GUIContent.none, terminalPaperStyle);

        float pad = 24f;
        float x = pad;
        float w = rect.width - pad * 2f;

        // Z1 — top bar
        GUI.Label(new Rect(x, 16f, w - 320f, 22f),
            MvpLocale.T("term_title"), terminalTitleStyle);
        GUI.Label(new Rect(x + w - 320f, 16f, 320f, 22f),
            MvpLocale.T("term_userline"), terminalLabelStyle);
        DrawTerminalLine(new Rect(x, 44f, w, 1f));

        // Z2 — status strip (funds / debt / license / connection)
        DrawTerminalStatusBar(new Rect(x, 52f, w, 22f), company);

        // Z3 — tabs (number keys jump straight to a tab)
        HandleTerminalTabKeys();
        DrawTerminalTabs(new Rect(x, 82f, w, 28f));
        DrawTerminalLine(new Rect(x, 114f, w, 1f));

        // Z4 — content by tab
        Rect content = new Rect(x, 124f, w, rect.height - 124f - 56f);
        switch (terminalTab)
        {
            case 1: DrawTabSupply(content, nearShop); break;
            case 2: DrawTabLedger(content, company); break;
            default: DrawTabCommissions(content, computer, company); break;
        }

        // Z5 — single primary action bar + key hints
        DrawTerminalActionBar(new Rect(x, rect.height - 48f, w, 34f), computer, company);

        GUI.EndGroup();
    }

    void HandleTerminalTabKeys()
    {
        Event e = Event.current;
        if (e == null || e.type != EventType.KeyDown) return;
        if (e.keyCode is KeyCode.Alpha1 or KeyCode.Keypad1) { terminalTab = 0; e.Use(); }
        else if (e.keyCode is KeyCode.Alpha2 or KeyCode.Keypad2) { terminalTab = 1; e.Use(); }
        else if (e.keyCode is KeyCode.Alpha3 or KeyCode.Keypad3) { terminalTab = 2; e.Use(); }
    }

    void DrawTerminalTabs(Rect rect)
    {
        float tw = rect.width / TerminalTabLabels.Length;
        for (int i = 0; i < TerminalTabLabels.Length; i++)
        {
            Rect t = new Rect(rect.x + i * tw, rect.y, tw - 8f, rect.height);
            bool active = terminalTab == i;
            if (active) GUI.Box(t, GUIContent.none, terminalSelectedButtonStyle);
            GUI.Label(new Rect(t.x + 12f, t.y + 3f, t.width - 16f, t.height - 4f),
                (active ? "▸ " : "  ") + TerminalTabLabels[i],
                active ? terminalInverseStyle : terminalLabelStyle);
            if (GUI.Button(t, GUIContent.none, GUIStyle.none)) terminalTab = i;
        }
    }

    // Z2: funds / debt / license / connection. Negative funds = inverse-video + '!'.
    void DrawTerminalStatusBar(Rect rect, CompanyState company)
    {
        bool broke = company.Funds < 0;
        string funds = (broke ? "! " : "") + MvpLocale.T("term_funds") + " " + company.Funds + "G";
        float fw = terminalLabelStyle.CalcSize(new GUIContent(funds)).x + 10f;
        if (broke) GUI.Box(new Rect(rect.x, rect.y - 1f, fw, rect.height + 2f), GUIContent.none, terminalSelectedButtonStyle);
        GUI.Label(new Rect(rect.x + 4f, rect.y, fw, rect.height), funds, broke ? terminalInverseStyle : terminalLabelStyle);
        float fx = rect.x + fw + 22f;
        GUI.Label(new Rect(fx, rect.y, 150f, rect.height), MvpLocale.T("term_debt") + " " + company.Debt + "G", terminalLabelStyle);
        GUI.Label(new Rect(fx + 150f, rect.y, 260f, rect.height), MvpLocale.T("term_license"), terminalMutedStyle);
        string conn = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
            ? (NetworkManager.Singleton.IsHost ? MvpLocale.T("term_conn_host") : MvpLocale.T("term_conn_client"))
            : MvpLocale.T("term_conn_offline");
        GUI.Label(new Rect(rect.x + rect.width - 220f, rect.y, 220f, rect.height), conn, terminalLabelRightStyle);
    }

    static string MenuGlyph(int index)
    {
        return index switch
        {
            0 => "▣",
            1 => "▤",
            2 => "▾",
            3 => "▰",
            4 => "▧",
            _ => "⚙"
        };
    }

    // [1] Commissions — the selectable commission pool. Host/solo clicks a row to choose which
    // commission to dispatch; clients see the host's choice (synced). Data-driven, no fake rows.
    void DrawTabCommissions(Rect content, OfficeComputer computer, CompanyState company)
    {
        float y = content.y;

        // Returning state: a pending-settlement block jumps to the top (inverse video).
        if (MvpPendingReward.HasPending)
        {
            Rect box = new Rect(content.x, y, content.width, 50f);
            GUI.Box(box, GUIContent.none, terminalSelectedButtonStyle);
            GUI.Label(new Rect(box.x + 12f, box.y + 5f, box.width - 24f, 20f),
                MvpLocale.T("term_pending_head", MvpPendingReward.ResultLabel), terminalInverseStyle);
            GUI.Label(new Rect(box.x + 12f, box.y + 26f, box.width - 24f, 20f),
                MvpLocale.T("term_pending_body", MvpPendingReward.Money), terminalInverseStyle);
            y += 60f;
        }

        // Column captions sit at the same x offsets as the data rows (a single spaced
        // string can't line up once the captions are CJK).
        GUI.Label(new Rect(content.x + 8f, y, 52f, 20f), MvpLocale.T("term_col_no"), terminalMutedStyle);
        GUI.Label(new Rect(content.x + 60f, y, content.width * 0.40f, 20f), MvpLocale.T("term_col_name"), terminalMutedStyle);
        GUI.Label(new Rect(content.x + content.width * 0.50f, y, content.width * 0.18f, 20f), MvpLocale.T("term_col_client"), terminalMutedStyle);
        GUI.Label(new Rect(content.x + content.width * 0.70f, y, 84f, 20f), MvpLocale.T("term_col_pay"), terminalMutedStyle);
        GUI.Label(new Rect(content.x + content.width * 0.82f, y, content.width * 0.18f, 20f), MvpLocale.T("term_col_status"), terminalMutedStyle);
        DrawTerminalLine(new Rect(content.x, y + 22f, content.width, 1f));
        y += 30f;

        OfficeTaskDefinition[] pool = computer != null ? computer.Commissions : null;
        int selectedIdx = computer != null ? computer.CurrentCommissionIndex : 0;
        bool canSwitch = computer != null && IsLocalHostOrSolo()
            && !MvpMissionRuntime.HasSelectedTask && !MvpPendingReward.HasPending;

        if (pool != null && pool.Length > 0)
        {
            for (int i = 0; i < pool.Length; i++)
            {
                OfficeTaskDefinition t = pool[i];
                if (t == null) continue;
                bool sel = i == selectedIdx;
                Rect row = new Rect(content.x, y - 2f, content.width, 26f);
                if (sel) GUI.Box(row, GUIContent.none, terminalSelectedButtonStyle);
                GUIStyle rs = sel ? terminalInverseStyle : terminalLabelStyle;
                GUI.Label(new Rect(content.x + 8f, y, 52f, 22f), $"{i + 1:000}", rs);
                GUI.Label(new Rect(content.x + 60f, y, content.width * 0.40f, 22f), t.title, rs);
                GUI.Label(new Rect(content.x + content.width * 0.50f, y, content.width * 0.18f, 22f), t.client, rs);
                GUI.Label(new Rect(content.x + content.width * 0.70f, y, 84f, 22f), t.moneyReward + "G", rs);
                GUI.Label(new Rect(content.x + content.width * 0.82f, y, content.width * 0.18f, 22f),
                    sel ? GetDemoTaskStatus(computer) : "—", rs);
                if (canSwitch && !sel && GUI.Button(row, GUIContent.none, GUIStyle.none))
                    computer.SelectCommission(i);
                y += 30f;
            }
        }
        else
        {
            GUI.Label(new Rect(content.x, y, content.width, 22f),
                MvpLocale.T("term_pool_empty"), terminalMutedStyle);
            y += 30f;
        }

        if (company.CanShowTutorialAcquisition)
        {
            GUI.Label(new Rect(content.x + 8f, y, content.width - 16f, 22f),
                MvpLocale.T("term_acq_row"), terminalTitleStyle);
            y += 30f;
        }

        DrawTerminalLine(new Rect(content.x, y + 6f, content.width, 1f));
        y += 18f;

        if (pool != null && pool.Length > 0)
        {
            OfficeTaskDefinition t = pool[Mathf.Clamp(selectedIdx, 0, pool.Length - 1)];
            GUI.Label(new Rect(content.x, y, content.width, 20f),
                MvpLocale.T("term_detail", $"{selectedIdx + 1:000}", t.title), terminalLabelStyle);
            y += 26f;
            DrawTerminalDetailLine(content.x, y, MvpLocale.T("term_site"), t.locationName, null); y += 22f;
            DrawTerminalDetailLine(content.x, y, MvpLocale.T("term_pay"), t.moneyReward + "G  回收估价", null); y += 22f;
            DrawTerminalDetailLine(content.x, y, MvpLocale.T("term_window"), MvpMissionClock.GetScheduleSummary(t), null); y += 22f;
            DrawTerminalDetailLine(content.x, y, MvpLocale.T("term_client_label"), CommissionTierLabel(t.clientType), null); y += 24f;
            GUI.Label(new Rect(content.x, y, content.width, 40f), MvpLocale.T("term_note", t.description), terminalSmallStyle);
            if (canSwitch && pool.Length > 1)
            {
                y += 44f;
                GUI.Label(new Rect(content.x, y, content.width, 20f), "[ 点击上方任一行切换委托 ]", terminalMutedStyle);
            }
        }
    }

    static string CommissionTierLabel(CommissionClientType type) => type switch
    {
        CommissionClientType.FreeSalvage => "自由采集 (市价回收)",
        CommissionClientType.Commissioned => "指定委托 (客户偏好加价)",
        CommissionClientType.BlackCommission => "黑色委托 (客户偏好加价)",
        _ => "委托"
    };

    // [2] Supply — gear catalog (host only). Frozen until a pending settlement is claimed.
    void DrawTabSupply(Rect content, bool nearShop)
    {
        if (MvpPendingReward.HasPending)
        {
            GUI.Box(content, GUIContent.none, terminalBoxStyle);
            GUI.Label(new Rect(content.x + 20f, content.y + content.height * 0.42f, content.width - 40f, 24f),
                MvpLocale.T("term_supply_frozen"), terminalTitleStyle);
            return;
        }

        GUI.Label(new Rect(content.x, content.y, content.width, 20f), MvpLocale.T("term_supply_head"), terminalMutedStyle);
        DrawTerminalLine(new Rect(content.x, content.y + 22f, content.width, 1f));

        PlayerHotbar hotbar = FindLocalHotbar();
        bool canBuy = hotbar != null && nearShop && IsLocalHostOrSolo();
        float y = content.y + 34f;
        DrawSupplyRow(new Rect(content.x, y, content.width, 30f), "F1", MvpLocale.T("flashlight"), MvpHotbarItemId.Flashlight, hotbar, canBuy); y += 40f;
        DrawSupplyRow(new Rect(content.x, y, content.width, 30f), "F2", MvpLocale.T("battery"), MvpHotbarItemId.Battery, hotbar, canBuy); y += 40f;

        bool ownsWatch = hotbar != null && hotbar.HasWristwatchOwned;
        GUI.Label(new Rect(content.x + 4f, y + 4f, 40f, 22f), "F3", terminalLabelStyle);
        GUI.Label(new Rect(content.x + 48f, y + 4f, content.width * 0.5f, 22f), MvpLocale.T("term_wristwatch"), terminalLabelStyle);
        GUI.Label(new Rect(content.x + content.width * 0.62f, y + 4f, 90f, 22f), PlayerHotbar.WristwatchCost + "G", terminalLabelStyle);
        GUI.enabled = canBuy && !ownsWatch;
        if (GUI.Button(new Rect(content.x + content.width - 120f, y, 112f, 30f),
                MvpLocale.T(ownsWatch ? "term_owned" : "term_buy"), terminalButtonStyle))
            TryBuyWristwatch(hotbar);
        GUI.enabled = true;
        y += 44f;

        if (!nearShop)
            GUI.Label(new Rect(content.x, y, content.width, 20f), MvpLocale.T("term_stand_close"), terminalMutedStyle);
        if (!string.IsNullOrEmpty(shopMessage) && Time.time < shopMessageUntil)
            GUI.Label(new Rect(content.x, content.yMax - 24f, content.width, 22f), shopMessage,
                shopMessage.Contains("不足") || shopMessage.Contains("only") ? terminalInverseStyle : terminalLabelStyle);
    }

    void DrawSupplyRow(Rect rect, string key, string label, MvpHotbarItemId item, PlayerHotbar hotbar, bool canBuy)
    {
        GUI.Label(new Rect(rect.x + 4f, rect.y + 4f, 40f, 22f), key, terminalLabelStyle);
        GUI.Label(new Rect(rect.x + 48f, rect.y + 4f, rect.width * 0.5f, 22f), label, terminalLabelStyle);
        GUI.Label(new Rect(rect.x + rect.width * 0.62f, rect.y + 4f, 90f, 22f), PlayerHotbar.GetItemCost(item) + "G", terminalLabelStyle);
        GUI.enabled = canBuy;
        if (GUI.Button(new Rect(rect.x + rect.width - 120f, rect.y, 112f, rect.height),
                MvpLocale.T("term_buy"), terminalButtonStyle))
            TryBuy(hotbar, item);
        GUI.enabled = true;
    }

    // [3] Ledger — current funds/debt + recent settlements (per-commission history needs
    // a SaveIO extension; for now this shows current balances + any pending settlement).
    void DrawTabLedger(Rect content, CompanyState company)
    {
        GUI.Label(new Rect(content.x, content.y, content.width, 20f), MvpLocale.T("term_ledger_head"), terminalTitleStyle);
        DrawTerminalLine(new Rect(content.x, content.y + 24f, content.width, 1f));
        float y = content.y + 34f;
        DrawTerminalDetailLine(content.x, y, MvpLocale.T("term_cur_funds"), company.Funds + "G", null); y += 24f;
        DrawTerminalDetailLine(content.x, y, MvpLocale.T("term_out_debt"), company.Debt + "G", null); y += 34f;
        GUI.Label(new Rect(content.x, y, content.width, 20f), MvpLocale.T("term_recent"), terminalMutedStyle); y += 26f;
        if (MvpPendingReward.HasPending)
            GUI.Label(new Rect(content.x, y, content.width, 22f),
                MvpLocale.T("term_ledger_pending", MvpPendingReward.ResultLabel, MvpPendingReward.Money),
                terminalLabelStyle);
        else
            GUI.Label(new Rect(content.x, y, content.width, 22f), MvpLocale.T("term_ledger_empty"), terminalMutedStyle);
    }

    // Z5: one primary action (priority: claim settlement -> accept -> confirm acquisition)
    // + key hints, with the transient office message just above it.
    void DrawTerminalActionBar(Rect rect, OfficeComputer computer, CompanyState company)
    {
        if (!string.IsNullOrEmpty(officeMessage) && Time.time < officeMessageUntil)
            GUI.Label(new Rect(rect.x, rect.y - 22f, rect.width, 20f), officeMessage, terminalLabelStyle);
        DrawTerminalPrimaryAction(new Rect(rect.x, rect.y, rect.width * 0.58f, rect.height), computer, company);
        GUI.Label(new Rect(rect.x + rect.width * 0.6f, rect.y, rect.width * 0.4f, rect.height),
            MvpLocale.T("term_hints"), terminalLabelRightStyle);
    }

    // The [E] path for the single primary action — same priority as the on-screen
    // button (claim settlement → accept commission → confirm acquisition).
    void ExecuteTerminalPrimaryAction(OfficeComputer computer)
    {
        if (computer == null) return;
        CompanyState company = CompanyData.Current;

        if (MvpPendingReward.HasPending)
        {
            if (IsLocalHostOrSolo())
            {
                computer.ExecuteComputerAction(FindLocalPlayer());
                SetOfficeMessage(MvpLocale.T("msg_settle_submitted"));
            }
            return;
        }

        if (company.CanShowTutorialAcquisition)
        {
            if (company.CanAffordTutorialAcquisition && IsLocalHostOrSolo())
            {
                computer.ExecuteComputerAction(FindLocalPlayer());
                SetOfficeMessage(MvpLocale.T("msg_acq_submitted"));
            }
            return;
        }

        if (MvpMissionRuntime.HasSelectedTask) return; // already locked

        if (CanAcceptFromTerminal(computer))
        {
            if (computer.TryAcceptDemoTask(out string msg))
                CloseComputer();
            else if (msg != null)
                SetOfficeMessage(msg);
        }
    }

    void DrawTerminalPrimaryAction(Rect rect, OfficeComputer computer, CompanyState company)
    {
        string label = MvpLocale.T("act_accept");
        bool enabled = CanAcceptFromTerminal(computer);
        System.Action action = () =>
        {
            string message = null;
            bool accepted = computer != null && computer.TryAcceptDemoTask(out message);
            if (message != null)
                SetOfficeMessage(message);
            if (accepted)
                CloseComputer();
        };

        if (MvpPendingReward.HasPending)
        {
            label = MvpLocale.T("act_claim");
            enabled = IsLocalHostOrSolo();
            action = () =>
            {
                computer?.ExecuteComputerAction(FindLocalPlayer());
                SetOfficeMessage(MvpLocale.T("msg_settle_submitted"));
            };
        }
        else if (company.CanShowTutorialAcquisition)
        {
            label = MvpLocale.T("act_acq");
            enabled = company.CanAffordTutorialAcquisition && IsLocalHostOrSolo();
            action = () =>
            {
                computer?.ExecuteComputerAction(FindLocalPlayer());
                SetOfficeMessage(MvpLocale.T("msg_acq_submitted"));
            };
        }
        else if (MvpMissionRuntime.HasSelectedTask)
        {
            label = MvpLocale.T("act_locked");
            enabled = false;
        }

        GUI.enabled = enabled;
        if (GUI.Button(rect, label, terminalButtonStyle))
            action();
        GUI.enabled = true;
    }

    void DrawOfficeTableHeader(float x, float y, float width)
    {
        GUI.Label(new Rect(x + 8f, y, 54f, 20f), "编号", terminalSmallStyle);
        GUI.Label(new Rect(x + 70f, y, width * 0.32f, 20f), "委托名称", terminalSmallStyle);
        GUI.Label(new Rect(x + width * 0.47f, y, width * 0.20f, 20f), "委托人", terminalSmallStyle);
        GUI.Label(new Rect(x + width * 0.68f, y, width * 0.14f, 20f), "状态", terminalSmallStyle);
        GUI.Label(new Rect(x + width * 0.83f, y, width * 0.16f, 20f), "截止日期", terminalSmallStyle);
        DrawTerminalLine(new Rect(x, y + 22f, width, 1f));
    }

    void DrawOfficeTaskRow(float x, float y, float width, string id, string title, string client, string status, string due, bool selected)
    {
        if (selected)
            GUI.Box(new Rect(x, y - 2f, width, 28f), GUIContent.none, terminalSelectedButtonStyle);
        DrawTerminalLine(new Rect(x, y + 27f, width, 1f));
        GUI.Label(new Rect(x + 8f, y + 2f, 54f, 22f), id, terminalSmallStyle);
        GUI.Label(new Rect(x + 70f, y, width * 0.32f, 24f), title + "\n" + EnglishTaskName(id), terminalSmallStyle);
        GUI.Label(new Rect(x + width * 0.47f, y + 2f, width * 0.20f, 22f), client, terminalSmallStyle);
        GUI.Label(new Rect(x + width * 0.68f, y + 2f, width * 0.14f, 22f), status, status == "已锁定" ? warningStyle : terminalSmallStyle);
        GUI.Label(new Rect(x + width * 0.83f, y + 2f, width * 0.16f, 22f), due, due.Contains("天") ? warningStyle : terminalSmallStyle);
    }

    static string EnglishTaskName(string id)
    {
        return id switch
        {
            "tower_ecocolumn_01" => "ECO-COLUMN RETRIEVAL",
            _ => id.ToUpper()
        };
    }

    void DrawTerminalDetailLine(float x, float y, string label, string value, string english)
    {
        string body = string.IsNullOrEmpty(english)
            ? $"{label}:  {value}"
            : $"{label}:  {value} ({english})";
        GUI.Label(new Rect(x, y, 420f, 20f), body, terminalSmallStyle);
    }

    void DrawTerminalStatusStrip(Rect rect, CompanyState company, bool nearShop)
    {
        string message = !string.IsNullOrEmpty(officeMessage) && Time.time < officeMessageUntil
            ? officeMessage
            : $"资金 {company.Funds}G   债务 {company.Debt}G   电脑连接 {(nearShop ? "稳定" : "本地")}";
        GUI.Label(new Rect(rect.x + 28f, rect.yMax - 30f, rect.width - 56f, 22f), message,
            message.Contains("失败") || message.Contains("不足") || message.Contains("只有") ? warningStyle : terminalSmallStyle);
    }

    bool CanAcceptFromTerminal(OfficeComputer computer)
    {
        if (computer == null || MvpMissionRuntime.HasSelectedTask || MvpPendingReward.HasPending)
            return false;
        NetworkManager network = NetworkManager.Singleton;
        return network != null && network.IsListening && network.IsHost;
    }

    string GetDemoTaskStatus(OfficeComputer computer)
    {
        if (MvpMissionRuntime.HasSelectedTask)
            return "已锁定";
        if (MvpPendingReward.HasPending)
            return "待结算";
        if (computer == null)
            return "离线";
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening)
            return "待联机";
        return network.IsHost ? "可接受" : "等房主";
    }

    void DrawTerminalLine(Rect rect)
    {
        GUI.DrawTexture(rect, terminalLineTexture);
    }

    void DrawDemoTaskCard(OfficeComputer computer)
    {
        if (computer == null)
        {
            GUILayout.Label(MvpLocale.T("terminal_offline"), warningStyle);
            return;
        }

        DrawTerminalSection("可用委托 / COMMISSION FILE");
        GUILayout.BeginVertical(selectedSlotStyle);
        GUILayout.Label(computer.DemoTaskTitle, titleStyle);
        DrawLedgerLine("委托人 / 地点", $"{computer.DemoTaskClient} / {computer.DemoTaskLocation}");
        GUILayout.Label(computer.DemoTaskDescription, mutedStyle);
        DrawLedgerLine("报酬 / REWARD", $"{computer.DemoTaskMoneyReward}G");
        DrawLedgerLine("TIME WINDOW", MvpMissionClock.GetScheduleSummary(computer.DemoTask));
        GUILayout.Label(MvpMissionClock.GetOvertimeRuleSummary(computer.DemoTask), mutedStyle);
        GUILayout.Space(8);

        bool hostReady = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsHost;
        if (hostReady)
        {
            if (GUILayout.Button(MvpLocale.T("accept_task"), buttonStyle, GUILayout.Height(36)))
            {
                bool accepted = computer.TryAcceptDemoTask(out string message);
                SetOfficeMessage(message);
                if (accepted)
                    CloseComputer();
            }
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            GUI.enabled = false;
            GUILayout.Button(MvpLocale.T("wait_host_accept"), buttonStyle, GUILayout.Height(36));
            GUI.enabled = true;
            GUILayout.Label(MvpLocale.T("wait_host_hint"), mutedStyle);
        }
        else
        {
            GUI.enabled = false;
            GUILayout.Button(MvpLocale.T("start_host_first"), buttonStyle, GUILayout.Height(36));
            GUI.enabled = true;
            GUILayout.Label(MvpLocale.T("start_host_hint"), mutedStyle);
        }

        if (!string.IsNullOrEmpty(officeMessage) && Time.time < officeMessageUntil)
            GUILayout.Label(officeMessage, officeMessage.StartsWith("已") ? accentStyle : warningStyle);

        GUILayout.EndVertical();
    }

    void DrawOfficeShop(bool nearShop)
    {
        DrawTerminalSection("旧货采购 / USED GEAR");
        PlayerHotbar activeHotbar = FindLocalHotbar();
        GUILayout.BeginVertical(slotStyle);
        GUILayout.Label(MvpLocale.T("shop_title"), accentStyle);
        GUILayout.Label(GetHotbarStorageSummary(activeHotbar), mutedStyle);
        if (MvpPendingReward.HasPending)
        {
            GUILayout.Label(MvpLocale.T("claim_first"), mutedStyle);
            GUILayout.EndVertical();
            return;
        }

        bool canBuy = activeHotbar != null && nearShop;

        GUILayout.BeginHorizontal();
        DrawShopButton(activeHotbar, MvpHotbarItemId.Flashlight, MvpLocale.T("flashlight"), canBuy);
        DrawShopButton(activeHotbar, MvpHotbarItemId.Battery, MvpLocale.T("battery"), canBuy);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        DrawWristwatchShopButton(activeHotbar, canBuy);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(shopMessage) && Time.time < shopMessageUntil)
            GUILayout.Label(shopMessage, shopMessage.Contains("不足") ? warningStyle : accentStyle);
        if (!canBuy)
            GUILayout.Label(MvpLocale.T("shop_stand_near"), mutedStyle);
        GUILayout.EndVertical();
    }

    void DrawShopButton(PlayerHotbar hotbar, MvpHotbarItemId itemId, string label, bool canBuy)
    {
        GUI.enabled = canBuy;
        if (GUILayout.Button($"{label}  {PlayerHotbar.GetItemCost(itemId)}G", GUILayout.Height(30)))
            TryBuy(hotbar, itemId);
        GUI.enabled = true;
    }

    void DrawWristwatchShopButton(PlayerHotbar hotbar, bool canBuy)
    {
        bool alreadyOwned = hotbar != null && hotbar.HasWristwatchOwned;
        GUI.enabled = canBuy && !alreadyOwned;
        string label = alreadyOwned ? "廉价工时表 已佩戴" : $"廉价工时表  {PlayerHotbar.WristwatchCost}G";
        if (GUILayout.Button(label, GUILayout.Height(30)))
            TryBuyWristwatch(hotbar);
        GUI.enabled = true;
    }

    void DrawCabinetPanel()
    {
        OfficeCabinetStorage cabinet = activeCabinet;
        if (cabinet == null) return;

        float width = Mathf.Clamp(RefW - 36f, 380f, 700f);
        float height = Mathf.Clamp(RefH - 96f, 420f, 620f);
        Rect rect = new Rect((RefW - width) * 0.5f, 48, width, height);
        PlayerHotbar hotbar = FindLocalHotbar();

        GUILayout.BeginArea(rect, GUIContent.none, panelStyle);
        GUILayout.BeginHorizontal();
        DrawTerminalHeader("补给柜", "STORAGE");
        if (GUILayout.Button("关闭", GUILayout.Width(72), GUILayout.Height(30)))
        {
            CloseCabinet();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        cabinetScrollPosition = GUILayout.BeginScrollView(cabinetScrollPosition, false, true);
        DrawTerminalSection("柜内库存 / CABINET");
        for (int i = 0; i < OfficeCabinetStorage.SlotCount; i++)
        {
            HotbarSlot slot = cabinet.GetSlot(i);
            bool empty = slot == null || slot.IsEmpty;
            GUILayout.BeginHorizontal(empty ? slotStyle : selectedSlotStyle);
            GUILayout.Label($"{i + 1}. {(empty ? "空" : OfficeCabinetStorage.GetItemLabel(slot.itemId))}", labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.Label(empty ? "" : $"x{slot.quantity}", mutedStyle, GUILayout.Width(48));
            GUI.enabled = !empty && hotbar != null;
            if (GUILayout.Button("取出", GUILayout.Width(72), GUILayout.Height(28)))
            {
                cabinet.TryTakeToHotbar(hotbar, i, out cabinetMessage);
                cabinetMessageUntil = Time.time + 2.5f;
            }
            GUI.enabled = true;
            GUILayout.EndHorizontal();
        }

        DrawTerminalSection("个人热栏 / HOTBAR");
        if (hotbar == null)
        {
            GUILayout.Label("没有找到本地玩家热栏。", warningStyle);
        }
        else
        {
            for (int i = 0; i < PlayerHotbar.SlotCount; i++)
            {
                HotbarSlot slot = hotbar.GetSlot(i);
                bool empty = slot == null || slot.IsEmpty;
                bool selected = hotbar.SelectedSlot.Value == i;
                GUILayout.BeginHorizontal(selected ? selectedSlotStyle : slotStyle);
                GUILayout.Label($"{i + 1}. {(empty ? "空" : OfficeCabinetStorage.GetItemLabel(slot.itemId))}", labelStyle);
                GUILayout.FlexibleSpace();
                GUILayout.Label(empty ? "" : $"x{slot.quantity}", mutedStyle, GUILayout.Width(48));
                GUI.enabled = !empty;
                if (GUILayout.Button("存入", GUILayout.Width(72), GUILayout.Height(28)))
                {
                    cabinet.TryStoreFromHotbar(hotbar, i, out cabinetMessage);
                    cabinetMessageUntil = Time.time + 2.5f;
                }
                GUI.enabled = true;
                GUILayout.EndHorizontal();
            }
        }

        if (!string.IsNullOrEmpty(cabinetMessage) && Time.time < cabinetMessageUntil)
            GUILayout.Label(cabinetMessage, cabinetMessage.Contains("不能") || cabinetMessage.Contains("无法") || cabinetMessage.Contains("没有") ? warningStyle : accentStyle);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    GUIStyle dossierHeadStyle, dossierInkStyle, dossierMutedStyle, dossierStampStyle, dossierBandStyle;

    struct DossierEntry
    {
        public string speciesId, fileNo, title, behaviour, counter;
    }

    static readonly DossierEntry[] Dossiers =
    {
        new DossierEntry
        {
            speciesId = MonsterBestiaryProgress.EchoMold, fileNo = "BC-M-01", title = "回声菌",
            behaviour = "感染性真菌人形。录下附近的谈话，在暗处原句回放，把队员从彼此身边引开；诱捕失败即转入高速追猎。",
            counter = "它只会重放听过的话，造不出新内容——对暗号、管住嘴。听到重复过的语句，那不是你的同事。",
        },
        new DossierEntry
        {
            speciesId = MonsterBestiaryProgress.FileWarden, fileNo = "BC-M-02", title = "档案怨灵",
            behaviour = "盘踞在档案与货架区的同源感染体。行动比回声菌更急，被惊动后几乎不放弃追索。",
            counter = "与回声菌同一套声音纪律。别赌你跑得过它——用货架绕行，把它甩进死角。",
        },
        new DossierEntry
        {
            speciesId = MonsterBestiaryProgress.CivicIdol, fileNo = "BC-M-03", title = "市政圣像",
            behaviour = "铜绿雕像。被注视时纹丝不动；视线一离开就高速逼近，近身重击。红眼亮起说明它已选中猎物。",
            counter = "盯着它，它就动不了——分工：一人盯防，其余人搬运。移开视线前先退出它的半径。",
        },
    };

    // 案卷卷宗: the bestiary as civic paperwork — one archive page per species, entries
    // earned by having actually been hunted (earned-precision applied to knowledge).
    // Unwitnessed species stay as redaction bars + a 待补录 stamp.
    void DrawBestiaryPanel()
    {
        if (activeBestiary == null) return;
        EnsureDossierStyles();
        if (dossierHeadStyle == null) return;

        float width = Mathf.Clamp(RefW - 36f, 420f, 640f);
        float height = Mathf.Clamp(RefH - 96f, 420f, 600f);
        Rect rect = new Rect((RefW - width) * 0.5f, 54f, width, height);

        // Paper folder + civic-teal header band.
        GUI.DrawTexture(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.Shadow));
        GUI.DrawTexture(rect, BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.OldPaper));
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 40f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreen));
        GUI.Label(new Rect(rect.x + 16f, rect.y + 9f, rect.width - 120f, 24f),
            "黑色委托事务所  ·  异常体案卷", dossierBandStyle);
        if (GUI.Button(new Rect(rect.xMax - 92f, rect.y + 6f, 78f, 28f), "合上 [Esc]", terminalButtonStyle))
        {
            CloseBestiary();
            return;
        }

        var inner = new Rect(rect.x + 14f, rect.y + 50f, rect.width - 28f, rect.height - 62f);
        GUILayout.BeginArea(inner);
        bestiaryScrollPosition = GUILayout.BeginScrollView(bestiaryScrollPosition, false, true);

        foreach (DossierEntry d in Dossiers)
        {
            bool known = MonsterBestiaryProgress.HasEncountered(d.speciesId);
            DrawDossier(d, known, inner.width - 20f);
            GUILayout.Space(12f);
        }

        GUILayout.Label("补录规则：只有被它盯上过、并活着回来的人，才有资格执笔。", dossierMutedStyle);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawDossier(DossierEntry d, bool known, float w)
    {
        // Header row: file number + name (or redacted) + status stamp.
        GUILayout.BeginHorizontal();
        GUILayout.Label($"案卷 {d.fileNo}", dossierMutedStyle, GUILayout.Width(110f));
        GUILayout.Label(known ? d.title : "██████", dossierHeadStyle);
        GUILayout.FlexibleSpace();
        GUILayout.Label(known ? "〔已立案〕" : "〔待补录〕", dossierStampStyle, GUILayout.Width(88f));
        GUILayout.EndHorizontal();

        Rect line = GUILayoutUtility.GetRect(w, 1f);
        GUI.DrawTexture(line, BlackCommissionUiTheme.MakeTex(new Color(0.19f, 0.17f, 0.13f, 0.45f)));
        GUILayout.Space(4f);

        if (known)
        {
            GUILayout.Label("已证实行为", dossierMutedStyle);
            GUILayout.Label(d.behaviour, dossierInkStyle);
            GUILayout.Space(4f);
            GUILayout.Label("对策（幸存者执笔）", dossierMutedStyle);
            GUILayout.Label(d.counter, dossierInkStyle);
        }
        else
        {
            // Redaction bars: the page exists, the testimony doesn't.
            float[] bars = { 0.86f, 0.62f, 0.74f, 0.4f };
            foreach (float b in bars)
            {
                Rect r = GUILayoutUtility.GetRect(w * b, 12f);
                GUI.DrawTexture(new Rect(r.x, r.y, w * b, 12f),
                    BlackCommissionUiTheme.MakeTex(new Color(0.13f, 0.12f, 0.10f, 0.85f)));
                GUILayout.Space(5f);
            }
            GUILayout.Label("现场尚无可靠证词。", dossierMutedStyle);
        }
    }

    void EnsureDossierStyles()
    {
        if (dossierHeadStyle != null) return;
        if (GUI.skin == null || GUI.skin.label == null) return;

        Color ink = new Color(0.10f, 0.095f, 0.075f, 1f);
        Color inkSoft = new Color(0.19f, 0.17f, 0.13f, 0.85f);

        dossierBandStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15, fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.OldPaper }
        };
        dossierHeadStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 17, fontStyle = FontStyle.Bold, normal = { textColor = ink }
        };
        dossierInkStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14, wordWrap = true, normal = { textColor = ink }
        };
        dossierMutedStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12, wordWrap = true, normal = { textColor = inkSoft }
        };
        dossierStampStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14, fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.RustWarning }
        };
        MvpFontProvider.ApplyToStyle(dossierBandStyle);
        MvpFontProvider.ApplyToStyle(dossierHeadStyle);
        MvpFontProvider.ApplyToStyle(dossierInkStyle);
        MvpFontProvider.ApplyToStyle(dossierMutedStyle);
        MvpFontProvider.ApplyToStyle(dossierStampStyle);
    }



    void DrawMissionPanel()
    {
        ScavengeMissionManager mission = ScavengeMissionManager.Instance;
        GUILayout.BeginArea(new Rect(18, 18, panelWidth, 230), GUIContent.none, panelStyle);
        GUILayout.Label(GetMissionObjective(mission), accentStyle);
        var cargo = ScavengeCargoZone.Instance;
        if (cargo != null && cargo.Capacity.Value > 0)
        {
            int load = cargo.LoadUnits.Value;
            int cap = cargo.Capacity.Value;
            bool full = load >= cap;
            GUILayout.Label($"舱位: {load}/{cap}　已装 {cargo.ItemCount.Value} 件",
                full ? warningStyle : mutedStyle);
        }
        if (!string.IsNullOrEmpty(missionMessage) && Time.time < missionMessageUntil)
            GUILayout.Label(missionMessage, missionMessage.Contains("警告") ? warningStyle : accentStyle);
        DrawSpectatorHint();
        GUILayout.EndArea();
    }

    static string GetMissionObjective(ScavengeMissionManager mission)
    {
        if (mission != null && mission.IsTerminalState)
            return "目标: 已发车结算，正在返回事务所。";
        return "目标: 搜刮值钱物件 → 放进货舱 → 搬完上车，拉杆发车结算。";
    }

    void DrawSpectatorHint()
    {
        PlayerHealth localHealth = FindLocalPlayerHealth();
        if (localHealth == null || !localHealth.IsDowned.Value) return;

        PlayerCameraController cam = localHealth.GetComponentInChildren<PlayerCameraController>();
        if (cam == null) return;

        GUILayout.Space(6);
        if (cam.IsSpectating)
        {
            string targetName = cam.SpectateTargetName;
            GUILayout.Label(MvpLocale.T("downed_spectating", targetName), warningStyle);
        }
        else
        {
            GUILayout.Label(MvpLocale.T("downed_all"), warningStyle);
        }
    }

    PlayerHealth FindLocalPlayerHealth()
    {
        PlayerHealth[] all = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (var h in all)
        {
            if (h.IsOwner) return h;
        }
        // Offline / preview walkthroughs have no network-owned player; fall back to the only one
        // present so the HP bar still renders — matches FindLocalPlayer / FindLocalHotbar, which
        // already do this. Without it the HP bar silently vanished while the hotbar stayed up.
        return all.Length > 0 ? all[0] : null;
    }

    void DrawMissionVanPanel()
    {
        MissionVanExitPoint van = activeMissionVan;
        if (van == null) return;

        float width = Mathf.Clamp(RefW - 36f, 320f, 560f);
        float height = Mathf.Clamp(RefH - 112f, 360f, 560f);
        Rect rect = new Rect((RefW - width) * 0.5f, 56, width, height);
        GUILayout.BeginArea(rect, GUIContent.none, panelStyle);
        GUILayout.BeginHorizontal();
        DrawTerminalHeader(MvpLocale.T("mission_van"), "RETURN GATE");
        if (GUILayout.Button(MvpLocale.T("close_door"), GUILayout.Width(72), GUILayout.Height(30)))
        {
            CloseMissionVan();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        DrawTerminalSection("返程决策 / RETURN");
        GUILayout.Label(van.GetReturnSummary(), accentStyle);

        GUILayout.Label(MvpLocale.T("van_decide_hint"), mutedStyle);
        GUILayout.Space(10);

        if (DrawMissionVanReturnControls(van))
        {
            GUILayout.EndArea();
            return;
        }

        DrawTerminalSection(MvpLocale.T("van_locker") + " / LOCKER");
        PlayerHotbar localHotbar = FindLocalHotbar();
        for (int i = 0; i < MissionVanExitPoint.LockerSlotCount; i++)
        {
            GUILayout.BeginHorizontal(slotStyle);
            MvpHotbarItemId itemId = van.GetLockerItemId(i);
            int quantity = van.GetLockerQuantity(i);
            GUILayout.Label($"{i + 1}. {GetShopItemLabel(itemId)}  x{quantity}", labelStyle);
            string lockerReason = string.Empty;
            bool canReceive = localHotbar != null && localHotbar.CanReceiveItem(itemId, out lockerReason);
            GUI.enabled = quantity > 0 && canReceive;
            if (GUILayout.Button(MvpLocale.T("take_item"), GUILayout.Width(88), GUILayout.Height(28)))
            {
                van.TryTakeLockerItem(i);
                SetMissionMessage($"车载物资申请: {GetShopItemLabel(itemId)}。");
            }
            GUI.enabled = true;
            if (quantity > 0 && localHotbar != null && !canReceive)
                GUILayout.Label(lockerReason, warningStyle);
            GUILayout.EndHorizontal();
        }

        GUILayout.Space(12);
        if (GUILayout.Button(MvpLocale.T("continue_search"), GUILayout.Height(32)))
        {
            CloseMissionVan();
            GUILayout.EndArea();
            return;
        }

        GUILayout.EndArea();
    }

    bool DrawMissionVanReturnControls(MissionVanExitPoint van)
    {
        bool canReturn = van.CanLocalPlayerRequestReturn();
        GUI.enabled = canReturn;
        if (GUILayout.Button(van.GetReturnButtonLabel(), GUILayout.Height(44)))
        {
            if (van.IsPartialReturnRequest() && !IsPartialReturnConfirmed(van))
            {
                partialReturnConfirmVan = van;
                partialReturnConfirmUntil = Time.unscaledTime + 4f;
                SetMissionMessage("警告: 再次点击返程会让全队提前回事务所，只做部分结算。");
                GUI.enabled = true;
                return false;
            }

            van.RequestReturnToOffice(FindLocalPlayer());
            CloseMissionVan();
            GUI.enabled = true;
            return true;
        }
        GUI.enabled = true;
        if (!canReturn)
            GUILayout.Label(van.GetReturnBlockedReason(), warningStyle);

        return false;
    }







    bool HasLocalWristwatch()
    {
        PlayerHotbar hotbar = FindLocalHotbar();
        return hotbar != null && hotbar.HasWristwatchOwned;
    }

    // Always-on inventory bar — LC-style bottom-centre slot row in the Municipal Debt Noir
    // palette. Per the art bible: bare locker cells (NO backing slip, NO drop shadow, NO rule),
    // 48px squares, empty slots are outline-only, the selected slot is marked with sodium-amber
    // corner brackets (top-left + bottom-right). Laid out in 1920x1080 reference space; the OnGUI
    // GUI.matrix scales it to the real resolution. Hidden while a blocking panel or the transit
    // cabin owns the screen (matches the crosshair/vitals gating).
    void DrawHotbar()
    {
        if (IsBlockingPanelOpen || VanTransitOverlay.IsActive) return;

        PlayerHotbar hotbar = FindLocalHotbar();
        if (hotbar == null) return;

        int count = PlayerHotbar.SlotCount;
        const float slotSize = 48f;   // art-bible hotbar spec @1080p
        const float gap = 4f;
        const float iconSize = 24f;
        float totalWidth = count * slotSize + (count - 1) * gap;
        float startX = (RefW - totalWidth) * 0.5f;   // centred (PM-confirmed)
        float y = RefH - slotSize - 16f;

        Color cellBorder  = new Color(0.565f, 0.585f, 0.545f, 0.45f); // MutedText, faint outline
        Color cellFill    = new Color(0.058f, 0.062f, 0.060f, 0.78f); // ConcretePanel chip (populated)
        Color cellFillSel = new Color(0.098f, 0.104f, 0.100f, 0.88f); // ConcreteRaised (selected lift)

        for (int i = 0; i < count; i++)
        {
            HotbarSlot slot = hotbar.GetSlot(i);
            bool empty = slot == null || slot.IsEmpty;
            bool selected = hotbar.SelectedSlot.Value == i;
            var rect = new Rect(startX + i * (slotSize + gap), y, slotSize, slotSize);

            // Populated cells get a dark concrete chip; the selected cell lifts slightly. Empty
            // cells stay unfilled (outline only) so the row reads light, not as a black brick.
            if (!empty)
                GUI.DrawTexture(rect, BlackCommissionUiTheme.MakeTex(selected ? cellFillSel : cellFill));
            else if (selected)
                GUI.DrawTexture(rect, BlackCommissionUiTheme.MakeTex(cellFillSel));

            DrawSlotBorder(rect, cellBorder, 1f);

            GUI.Label(new Rect(rect.x + 4f, rect.y + 2f, 16f, 14f), $"{i + 1}", mutedStyle);

            if (!empty)
            {
                GUI.DrawTexture(
                    new Rect(rect.x + (slotSize - iconSize) * 0.5f, rect.y + (slotSize - iconSize) * 0.5f, iconSize, iconSize),
                    GetItemIcon(slot.itemId), ScaleMode.ScaleToFit, true);

                if (slot.quantity > 1)
                    GUI.Label(new Rect(rect.xMax - 22f, rect.yMax - 16f, 20f, 14f), $"x{slot.quantity}", mutedStyle);

                // Battery level bar for the flashlight slot.
                if (slot.itemId == MvpHotbarItemId.Flashlight)
                    DrawFlashlightBar(rect);
            }

            // Selected: sodium-amber corner brackets — the only warm accent on the bar.
            if (selected)
                DrawSlotCornerBrackets(rect, BlackCommissionUiTheme.OldWood, 8f, 2f);
        }
    }

    // Thin 4-edge outline for a hotbar cell (reference px; scaled by the HUD matrix).
    void DrawSlotBorder(Rect r, Color color, float t)
    {
        Texture2D tex = BlackCommissionUiTheme.MakeTex(color);
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, t), tex);
        GUI.DrawTexture(new Rect(r.x, r.yMax - t, r.width, t), tex);
        GUI.DrawTexture(new Rect(r.x, r.y, t, r.height), tex);
        GUI.DrawTexture(new Rect(r.xMax - t, r.y, t, r.height), tex);
    }

    // Sodium-amber selection mark: an L at the top-left and a reverse-L at the bottom-right
    // (art-bible hotbar selection grammar) — lighter than a full frame, reads as a scan direction.
    void DrawSlotCornerBrackets(Rect r, Color color, float len, float t)
    {
        Texture2D tex = BlackCommissionUiTheme.MakeTex(color);
        GUI.DrawTexture(new Rect(r.x, r.y, len, t), tex);                 // top-left horizontal
        GUI.DrawTexture(new Rect(r.x, r.y, t, len), tex);                 // top-left vertical
        GUI.DrawTexture(new Rect(r.xMax - len, r.yMax - t, len, t), tex); // bottom-right horizontal
        GUI.DrawTexture(new Rect(r.xMax - t, r.yMax - len, t, len), tex); // bottom-right vertical
    }

    // Minimal bottom-left vitals (PM 2026-06-13): no panel box, no numbers — just an "HP"
    // and "STAMINA" label over a thin bar. Length + colour carry the state (fills go
    // stamp-red when critical); clean LC-style readout on the dark scene.
    void DrawVitalsBlock()
    {
        if (IsBlockingPanelOpen || VanTransitOverlay.IsActive) return;

        PlayerHealth localHealth = FindLocalPlayerHealth();
        PlayerController localPlayer = FindLocalPlayer();
        if (localHealth == null && localPlayer == null) return;

        const float pad = 24f, w = 160f;
        float x = pad;
        float yTop = RefH - pad - 50f;

        if (localHealth != null)
        {
            float hp = localHealth.CurrentHP.Value;
            const float maxHp = 100f;
            // Damage-flash hook: screen tints on HP loss.
            if (hp < lastKnownHp) { damageFlashUntil = Time.time + 0.35f; lastKnownHp = hp; }
            else if (hp > lastKnownHp) lastKnownHp = hp;

            float n = Mathf.Clamp01(hp / maxHp);
            bool low = n <= 0.25f;
            DrawVitalRow(x, yTop, w, "HP", n, low ? hpBarFill : hpFillHealthy, low);
        }

        if (localPlayer != null && localPlayer.MaxStamina > 0f)
        {
            float n = Mathf.Clamp01(localPlayer.Stamina / localPlayer.MaxStamina);
            bool low = n <= 0.24f || localPlayer.IsExhausted;
            DrawVitalRow(x, yTop + 28f, w, "STAMINA", n, low ? staminaBarLowFill : staminaBarFill, low);
        }
    }

    // Label over a thin bar, no frame/box. The bar track (hpBarBg) keeps the empty portion
    // faintly readable on the dark scene without reading as a panel.
    void DrawVitalRow(float x, float topY, float w, string label, float normalized, Texture2D fill, bool low)
    {
        GUI.Label(new Rect(x, topY, w, 15f), label, low ? warningStyle : labelStyle);
        float by = topY + 15f, bh = 4f;
        GUI.DrawTexture(new Rect(x, by, w, bh), hpBarBg);
        GUI.DrawTexture(new Rect(x, by, w * Mathf.Clamp01(normalized), bh), fill);
    }

    void DrawFlashlightBar(Rect slotRect)
    {
        FlashlightController fl = null;
        PlayerController[] controllers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var c in controllers)
            if (c.IsOwner) { fl = c.GetComponent<FlashlightController>(); break; }
        if (fl == null) return;

        float normalized = fl.BatteryNormalized;
        float barW = slotRect.width - 8f;
        float barY = slotRect.y + slotRect.height - 8f;
        // Battery reads in lamp colors: sodium amber when healthy, stamp red when dying
        // (CRT green stays on actual screens per the art bible).
        Color barColor = normalized > 0.4f
            ? new Color(BlackCommissionUiTheme.OldWood.r, BlackCommissionUiTheme.OldWood.g, BlackCommissionUiTheme.OldWood.b, 0.95f)
            : new Color(BlackCommissionUiTheme.RustWarning.r, BlackCommissionUiTheme.RustWarning.g, BlackCommissionUiTheme.RustWarning.b, 0.95f);

        GUI.DrawTexture(new Rect(slotRect.x + 4, barY, barW, 4), hpBarBg);
        GUI.DrawTexture(new Rect(slotRect.x + 4, barY, barW * normalized, 4), MakeTexture(barColor));
    }

    void DrawFooterHint()
    {
        string text = "多人 MVP: Start Host 后接任务；1-5 切热栏，左键/H 使用，HQ 内按 G 丢当前格到地上存放。";
        GUI.Label(new Rect(18, RefH - 30, 720, 24), text, mutedStyle);
    }









    static PlayerHotbar FindLocalHotbar()
    {
        PlayerHotbar[] hotbars = FindObjectsByType<PlayerHotbar>(FindObjectsSortMode.None);
        foreach (var hotbar in hotbars)
        {
            if (hotbar != null && hotbar.IsOwner)
                return hotbar;
        }

        return hotbars.Length > 0 ? hotbars[0] : null;
    }

    static PlayerController FindLocalPlayer()
    {
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player != null && player.IsOwner)
                return player;
        }

        return players.Length > 0 ? players[0] : null;
    }

    static float computerOpenedAt;
    static float computerClosedAt;

    public static void OpenComputer(OfficeComputer computer)
    {
        // Debounce the close→reopen race: accepting a commission calls CloseComputer(), but the
        // player is usually still holding E aimed at the terminal, so PlayerInteraction would
        // re-fire OnInteractStart next frame and pop it straight back open ("感觉没关上"). Swallow
        // any open within a short window of a close.
        if (Time.unscaledTime - computerClosedAt < 0.35f) return;
        activeMissionVan = null;
        activeCabinet = null;
        activeBestiary = null;
        SettingsOverlay.ForceClose();
        activeComputer = computer;
        computerOpenedAt = Time.unscaledTime;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Camera: PlayerCameraController sees MvpHud.ActiveComputer and eases the local camera to
        // the head-on GetTerminalCameraPose. (The legacy ComputerCloseupCamera hardcoded a world
        // -Z offset — a side-on view of the -X-wall CRT — and disabled the controller, which
        // starved the head-on path. Do not re-enter it here.)
        AudioManager.Instance?.PlayComputerOpen(computer.transform.position);
    }

    public static void OpenMissionVan(MissionVanExitPoint van)
    {
        activeComputer = null;
        activeCabinet = null;
        activeBestiary = null;
        SettingsOverlay.ForceClose();
        activeMissionVan = van;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SetLocalPlayerHidden(true);
    }

    public static void OpenCabinet(OfficeCabinetStorage cabinet)
    {
        activeComputer = null;
        activeMissionVan = null;
        activeBestiary = null;
        SettingsOverlay.ForceClose();
        activeCabinet = cabinet;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        AudioManager.Instance?.PlayCabinetOpen(cabinet.transform.position);
    }

    public static void OpenBestiary(OfficeMonsterBestiary bestiary)
    {
        activeComputer = null;
        activeMissionVan = null;
        activeCabinet = null;
        SettingsOverlay.ForceClose();
        activeBestiary = bestiary;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    static void CloseComputer()
    {
        ComputerCloseupCamera.Exit();   // no-op unless a legacy closeup instance is still alive
        activeComputer = null;
        computerClosedAt = Time.unscaledTime;
        RestoreGameplayCursor();
    }

    static void CloseMissionVan()
    {
        activeMissionVan = null;
        partialReturnConfirmVan = null;
        SetLocalPlayerHidden(false);
        RestoreGameplayCursor();
    }

    static void CloseCabinet()
    {
        if (activeCabinet != null)
            AudioManager.Instance?.PlayCabinetClose(activeCabinet.transform.position);
        activeCabinet = null;
        RestoreGameplayCursor();
    }

    static void CloseBestiary()
    {
        activeBestiary = null;
        RestoreGameplayCursor();
    }

    static void SetLocalPlayerHidden(bool hidden)
    {
        PlayerController[] all = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var c in all)
            if (c.IsOwner && c.IsServer)
            {
                c.HiddenFromMonsters.Value = hidden;
                break;
            }
    }

    static void RestoreGameplayCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    static bool IsNearOfficeComputer(Vector3 position)
    {
        OfficeComputer[] computers = FindObjectsByType<OfficeComputer>(FindObjectsSortMode.None);
        foreach (var computer in computers)
        {
            if (computer == null) continue;
            if (Vector3.Distance(position, computer.transform.position) <= OfficeComputerShopDistance)
                return true;
        }

        return false;
    }

    void SetMissionMessage(string message)
    {
        missionMessage = message;
        missionMessageUntil = Time.time + 3f;
    }

    bool IsPartialReturnConfirmed(MissionVanExitPoint van)
    {
        return partialReturnConfirmVan == van && Time.unscaledTime <= partialReturnConfirmUntil;
    }

    static bool IsLocalHostOrSolo()
    {
        NetworkManager network = NetworkManager.Singleton;
        return network == null || !network.IsListening || network.IsHost;
    }

    static bool IsNetworkedPlay()
    {
        NetworkManager network = NetworkManager.Singleton;
        return network != null && network.IsListening;
    }

    Texture2D GetItemIcon(MvpHotbarItemId itemId)
    {
        switch (itemId)
        {
            case MvpHotbarItemId.Flashlight: return flashlightIcon;
            case MvpHotbarItemId.Battery: return decoyIcon; // reuse amber-toned icon for battery
            default: return emptyIcon;
        }
    }

    static string GetShopItemLabel(MvpHotbarItemId itemId)
    {
        return itemId switch
        {
            MvpHotbarItemId.Flashlight => MvpLocale.T("flashlight"),
            MvpHotbarItemId.Battery => MvpLocale.T("battery"),
            _ => MvpLocale.T("flashlight")
        };
    }

    static string GetHotbarStorageSummary(PlayerHotbar hotbar)
    {
        if (hotbar == null) return "热栏: 未找到本地玩家。";

        int usedSlots = 0, flashlights = 0, batteries = 0;
        for (int i = 0; i < PlayerHotbar.SlotCount; i++)
        {
            HotbarSlot slot = hotbar.GetSlot(i);
            if (slot == null || slot.IsEmpty) continue;
            usedSlots++;
            if (slot.itemId == MvpHotbarItemId.Flashlight) flashlights += slot.quantity;
            else if (slot.itemId == MvpHotbarItemId.Battery) batteries += slot.quantity;
        }

        string watch = hotbar.HasWristwatchOwned ? MvpLocale.T("wristwatch_status_owned") : MvpLocale.T("wristwatch_status_none");
        return MvpLocale.T("hotbar_summary", usedSlots, PlayerHotbar.SlotCount, flashlights, batteries, watch);
    }

    void EnsureStyles()
    {
        if (panelStyle != null) return;

        panelTexture = MakeTexture(BlackCommissionUiTheme.ConcreteBlack);
        slotTexture = MakeTexture(BlackCommissionUiTheme.ConcretePanel);
        selectedSlotTexture = MakeTexture(BlackCommissionUiTheme.MilitaryGreen);
        // Monochrome green phosphor CRT (office-computer-terminal.md): the office
        // computer is an electronic screen, so it speaks CRT green, not paper. Warnings
        // = inverse-video (green fill on terminalSelectedTexture) + '!', never red.
        terminalPaperTexture = MakeTexture(new Color(0.024f, 0.055f, 0.031f, 0.99f));   // near-black green glass
        terminalBoxTexture = MakeTexture(new Color(0.055f, 0.140f, 0.075f, 0.30f));     // faint panel fill
        terminalSelectedTexture = MakeTexture(new Color(0.424f, 1.000f, 0.373f, 0.92f)); // CRT green (inverse-video)
        terminalLineTexture = MakeTexture(new Color(0.180f, 0.470f, 0.205f, 0.62f));    // dim green rule
        EnsureIcons();

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTexture },
            padding = new RectOffset(16, 16, 14, 14)
        };
        slotStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = slotTexture },
            padding = new RectOffset(8, 8, 8, 8)
        };
        selectedSlotStyle = new GUIStyle(slotStyle)
        {
            normal = { background = selectedSlotTexture }
        };
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.OldPaper },
            wordWrap = true
        };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            normal = { textColor = BlackCommissionUiTheme.Text },
            wordWrap = true
        };
        mutedStyle = new GUIStyle(labelStyle)
        {
            fontSize = 13,
            normal = { textColor = BlackCommissionUiTheme.MutedText }
        };
        accentStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.CrtGreen }
        };
        warningStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.RustWarning }
        };
        sectionHeaderStyle = new GUIStyle(labelStyle)
        {
            fontSize = 13,
            fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.CrtGreen }
        };
        metaStyle = new GUIStyle(labelStyle)
        {
            fontSize = 14,
            normal = { textColor = BlackCommissionUiTheme.OldPaper }
        };
        buttonStyle = BlackCommissionUiTheme.ButtonStyle(15);
        terminalPaperStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = terminalPaperTexture },
            padding = new RectOffset(0, 0, 0, 0)
        };
        terminalBoxStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = terminalBoxTexture },
            border = new RectOffset(1, 1, 1, 1),
            padding = new RectOffset(0, 0, 0, 0)
        };
        terminalTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.CrtGreen },
            wordWrap = false
        };
        terminalLabelStyle = new GUIStyle(terminalTitleStyle)
        {
            fontSize = 14,
            fontStyle = FontStyle.Normal
        };
        terminalMutedStyle = new GUIStyle(terminalLabelStyle)
        {
            fontSize = 13,
            normal = { textColor = BlackCommissionUiTheme.CrtGreenDim }
        };
        terminalSmallStyle = new GUIStyle(terminalLabelStyle)
        {
            fontSize = 12,
            normal = { textColor = BlackCommissionUiTheme.CrtGreen },
            wordWrap = true
        };
        terminalButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
            normal = { background = terminalBoxTexture, textColor = BlackCommissionUiTheme.CrtGreen },
            hover = { background = terminalSelectedTexture, textColor = new Color(0.02f, 0.06f, 0.03f, 1f) },
            active = { background = terminalSelectedTexture, textColor = new Color(0.02f, 0.06f, 0.03f, 1f) },
            padding = new RectOffset(12, 8, 4, 4)
        };
        terminalSelectedButtonStyle = new GUIStyle(terminalBoxStyle)
        {
            normal = { background = terminalSelectedTexture }
        };
        terminalInverseStyle = new GUIStyle(terminalLabelStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.02f, 0.06f, 0.03f, 1f) }   // dark text on green inverse-video
        };
        terminalLabelRightStyle = new GUIStyle(terminalLabelStyle) { alignment = TextAnchor.MiddleRight };
        // Apply the project font to all terminal styles (3270 + CJK fallback chain).
        foreach (var s in new[] { terminalTitleStyle, terminalLabelStyle, terminalMutedStyle, terminalSmallStyle, terminalButtonStyle, terminalInverseStyle, terminalLabelRightStyle })
            MvpFontProvider.ApplyToStyle(s);

        EnsureCrtTextures();

        if (hpBarBg == null || hpBarFill == null || staminaBarFill == null || staminaBarLowFill == null || staminaBarFrame == null || hpFillHealthy == null)
        {
            hpBarBg = MakeTexture(new Color(0.055f, 0.060f, 0.055f, 0.90f));
            hpBarFill = MakeTexture(BlackCommissionUiTheme.RustWarning);
            hpFillHealthy = MakeTexture(new Color(0.80f, 0.76f, 0.62f, 0.96f));   // bone — healthy HP
            staminaBarFill = MakeTexture(new Color(0.90f, 0.78f, 0.22f, 0.96f));
            staminaBarLowFill = MakeTexture(new Color(0.92f, 0.34f, 0.18f, 0.96f));
            staminaBarFrame = MakeTexture(new Color(0.58f, 0.53f, 0.39f, 0.78f));
            damageFlashTex = MakeTexture(new Color(0.55f, 0.30f, 0.20f, 0.32f));
        }

        MvpFontProvider.ApplyToStyle(panelStyle);
        MvpFontProvider.ApplyToStyle(titleStyle);
        MvpFontProvider.ApplyToStyle(labelStyle);
        MvpFontProvider.ApplyToStyle(mutedStyle);
        MvpFontProvider.ApplyToStyle(accentStyle);
        MvpFontProvider.ApplyToStyle(warningStyle);
        MvpFontProvider.ApplyToStyle(sectionHeaderStyle);
        MvpFontProvider.ApplyToStyle(metaStyle);
        MvpFontProvider.ApplyToStyle(slotStyle);
        MvpFontProvider.ApplyToStyle(selectedSlotStyle);
        MvpFontProvider.ApplyToStyle(buttonStyle);
        MvpFontProvider.ApplyToStyle(terminalPaperStyle);
        MvpFontProvider.ApplyToStyle(terminalBoxStyle);
        MvpFontProvider.ApplyToStyle(terminalTitleStyle);
        MvpFontProvider.ApplyToStyle(terminalLabelStyle);
        MvpFontProvider.ApplyToStyle(terminalMutedStyle);
        MvpFontProvider.ApplyToStyle(terminalSmallStyle);
        MvpFontProvider.ApplyToStyle(terminalButtonStyle);
        MvpFontProvider.ApplyToStyle(terminalSelectedButtonStyle);
    }

    void EnsureIcons()
    {
        emptyIcon = MakeIcon(new Color(0.055f, 0.060f, 0.055f, 0.9f), BlackCommissionUiTheme.MilitaryGreenDim, Color.clear, 0);
        // Battery: dull office stock with a CRT-green charge mark.
        decoyIcon = MakeIcon(BlackCommissionUiTheme.OldWood, BlackCommissionUiTheme.MilitaryGreen, BlackCommissionUiTheme.CrtGreen, 2);
        flashlightIcon = MakeIcon(BlackCommissionUiTheme.ConcreteRaised, BlackCommissionUiTheme.CrtGreenDim, BlackCommissionUiTheme.CrtGreen, 4);
    }

    static Texture2D MakeIcon(Color baseColor, Color accentColor, Color markColor, int kind)
    {
        const int size = 48;
        var texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
                texture.SetPixel(x, y, baseColor);
        }

        switch (kind)
        {
            case 1:
                FillRect(texture, 9, 14, 30, 20, Color.white);
                FillRect(texture, 22, 18, 4, 12, accentColor);
                FillRect(texture, 18, 22, 12, 4, accentColor);
                break;
            case 2:
                FillCircle(texture, 24, 23, 14, accentColor);
                FillRect(texture, 21, 34, 6, 7, markColor);
                break;
            case 3:
                FillRect(texture, 17, 10, 14, 29, accentColor);
                FillRect(texture, 15, 7, 18, 5, markColor);
                FillRect(texture, 20, 15, 8, 16, baseColor);
                break;
            case 4:
                FillRect(texture, 13, 20, 24, 10, markColor);
                FillRect(texture, 30, 17, 9, 16, accentColor);
                FillRect(texture, 9, 23, 6, 4, accentColor);
                break;
            default:
                FillRect(texture, 12, 22, 24, 4, accentColor);
                break;
        }

        texture.Apply();
        return texture;
    }

    static void FillRect(Texture2D texture, int x, int y, int width, int height, Color color)
    {
        for (int yy = y; yy < y + height; yy++)
        {
            for (int xx = x; xx < x + width; xx++)
            {
                if (xx >= 0 && yy >= 0 && xx < texture.width && yy < texture.height)
                    texture.SetPixel(xx, yy, color);
            }
        }
    }

    static void FillCircle(Texture2D texture, int cx, int cy, int radius, Color color)
    {
        int r2 = radius * radius;
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                int dx = x - cx;
                int dy = y - cy;
                if (dx * dx + dy * dy <= r2 && x >= 0 && y >= 0 && x < texture.width && y < texture.height)
                    texture.SetPixel(x, y, color);
            }
        }
    }

    static Texture2D MakeTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    void EnsureCrtTextures()
    {
        if (crtScanlineTex != null) return;

        crtScanlineTex = new Texture2D(1, 4, TextureFormat.RGBA32, false);
        crtScanlineTex.filterMode = FilterMode.Point;
        crtScanlineTex.wrapMode = TextureWrapMode.Repeat;
        crtScanlineTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.18f));
        crtScanlineTex.SetPixel(0, 1, Color.clear);
        crtScanlineTex.SetPixel(0, 2, Color.clear);
        crtScanlineTex.SetPixel(0, 3, new Color(0f, 0f, 0f, 0.08f));
        crtScanlineTex.Apply();

        const int vigSize = 64;
        crtVignetteTex = new Texture2D(vigSize, vigSize);
        crtVignetteTex.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < vigSize; y++)
        {
            for (int x = 0; x < vigSize; x++)
            {
                float nx = (x / (float)(vigSize - 1)) * 2f - 1f;
                float ny = (y / (float)(vigSize - 1)) * 2f - 1f;
                float d = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Clamp01((d - 0.6f) / 0.55f) * 0.7f;
                crtVignetteTex.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
            }
        }
        crtVignetteTex.Apply();

        crtBezelTex = MakeTexture(new Color(0.035f, 0.04f, 0.037f, 1f));
    }

    void DrawCrtOverlay(Rect screenArea)
    {
        float bezel = 18f;
        GUI.DrawTexture(new Rect(screenArea.x - bezel, screenArea.y - bezel,
            screenArea.width + bezel * 2f, bezel), crtBezelTex);
        GUI.DrawTexture(new Rect(screenArea.x - bezel, screenArea.yMax,
            screenArea.width + bezel * 2f, bezel + 4f), crtBezelTex);
        GUI.DrawTexture(new Rect(screenArea.x - bezel, screenArea.y,
            bezel, screenArea.height), crtBezelTex);
        GUI.DrawTexture(new Rect(screenArea.xMax, screenArea.y,
            bezel, screenArea.height), crtBezelTex);

        GUI.DrawTexture(screenArea, crtScanlineTex, ScaleMode.StretchToFill, true,
            0f, Color.white, 0f, 0f);
        GUI.DrawTexture(screenArea, crtVignetteTex, ScaleMode.StretchToFill);
    }
}
