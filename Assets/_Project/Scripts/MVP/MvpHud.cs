using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MvpHud : MonoBehaviour
{
    const float OfficeComputerShopDistance = 3.4f;
    static OfficeComputer activeComputer;
    static SchoolExitPoint activeMissionVan;
    static OfficeCabinetStorage activeCabinet;
    static OfficeMonsterBestiary activeBestiary;
    static bool settingsOpen;
    public static bool IsComputerOpen => activeComputer != null;
    public static bool IsBlockingPanelOpen =>
        activeComputer != null || activeMissionVan != null || activeCabinet != null || activeBestiary != null || settingsOpen;

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
    Texture2D panelTexture;
    Texture2D slotTexture;
    Texture2D selectedSlotTexture;
    Texture2D emptyIcon;
    Texture2D flashlightIcon;
    Texture2D decoyIcon; // reused for battery
    string shopMessage;
    float shopMessageUntil;
    string missionMessage;
    float missionMessageUntil;
    PlayerInteraction cachedLocalInteraction;
    Texture2D crtScanlineTex;
    Texture2D crtVignetteTex;
    Texture2D crtBezelTex;
    Texture2D hpBarBg;
    Texture2D hpBarFill;
    Texture2D damageFlashTex;
    float damageFlashUntil;
    float lastKnownHp = 100f;
    Vector2 officeScrollPosition;
    Vector2 cabinetScrollPosition;
    Vector2 bestiaryScrollPosition;
    string cabinetMessage;
    float cabinetMessageUntil;
    Vector2 settingsScrollPosition;
    static SchoolExitPoint partialReturnConfirmVan;
    static float partialReturnConfirmUntil;

    static int LanguageIndex
    {
        get => PlayerPrefs.GetInt("AS.Settings.Language", 0);
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

    void Awake()
    {
        activeComputer = null;
        activeMissionVan = null;
        activeCabinet = null;
        activeBestiary = null;
        settingsOpen = false;
        showNetworkHint = false;
        AudioListener.volume = MasterVolume;
        ProximityVoiceChat.EnsureInstance();
        if (LostItemMissionManager.Instance != null)
            RestoreGameplayCursor();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (settingsOpen)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
                CloseSettings();
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

            // Shop purchases are mouse-click only (buttons in DrawOfficeShop)

            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            OpenSettings();
            return;
        }

        if (LostItemMissionManager.Instance != null) return;
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

    void OnGUI()
    {
        EnsureStyles();

        if (LostItemMissionManager.Instance != null)
        {
            DrawMissionPanel();
            if (activeMissionVan != null)
                DrawMissionVanPanel();
            DrawHotbar();
        }
        else
        {
            DrawOfficePanel();
            if (activeCabinet != null)
                DrawCabinetPanel();
            if (activeBestiary != null)
                DrawBestiaryPanel();
        }

        DrawDamageFlash();
        DrawCrosshair();
        DrawGestureHint();
        if (showNetworkHint)
            DrawFooterHint();
        if (settingsOpen)
            DrawSettingsPanel();
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
        GUI.Label(new Rect(Screen.width * 0.5f - 80f, Screen.height - 140f, 160f, 28f), name, accentStyle);
        GUI.color = Color.white;
    }

    void DrawDamageFlash()
    {
        if (Time.time > damageFlashUntil) return;
        if (damageFlashTex == null) return;
        float alpha = (damageFlashUntil - Time.time) / 0.35f;
        GUI.color = new Color(1f, 1f, 1f, alpha * 0.55f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), damageFlashTex, ScaleMode.StretchToFill);
        GUI.color = Color.white;
    }

    void DrawCrosshair()
    {
        if (IsBlockingPanelOpen || VanTransitOverlay.IsActive) return;

        float cx = Screen.width * 0.5f;
        float cy = Screen.height * 0.5f;

        if (cachedLocalInteraction == null)
        {
            PlayerController[] ctrlArr = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var c in ctrlArr)
                if (c.IsOwner) { cachedLocalInteraction = c.GetComponent<PlayerInteraction>(); break; }
        }

        bool hasTarget = cachedLocalInteraction != null && cachedLocalInteraction.CurrentTarget != null;
        Color dotColor = hasTarget ? new Color(0.56f, 0.92f, 0.72f, 0.92f) : new Color(0.9f, 0.9f, 0.9f, 0.55f);
        float size = hasTarget ? 6f : 4f;

        Texture2D dot = hpBarBg ?? MakeTexture(Color.white);
        GUI.color = dotColor;
        GUI.DrawTexture(new Rect(cx - size * 0.5f, cy - size * 0.5f, size, size), dot, ScaleMode.StretchToFill);
        GUI.color = Color.white;
    }

    void DrawSettingsPanel()
    {
        float width = Mathf.Clamp(Screen.width - 36f, 360f, 620f);
        float height = Mathf.Clamp(Screen.height - 80f, 420f, 660f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUILayout.BeginArea(rect, GUIContent.none, panelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label(MvpLocale.T("pause"), titleStyle);
        if (GUILayout.Button(MvpLocale.T("resume"), GUILayout.Width(72), GUILayout.Height(30)))
        {
            CloseSettings();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        settingsScrollPosition = GUILayout.BeginScrollView(settingsScrollPosition, false, true);
        GUILayout.Space(8);
        GUILayout.Label(MvpLocale.T("game"), accentStyle);
        string[] languageLabels = { "简体中文", "English" };
        GUILayout.Label(MvpLocale.T("language", languageLabels[LanguageIndex]), labelStyle);
        int selectedLanguage = GUILayout.SelectionGrid(LanguageIndex, languageLabels, 2);
        if (selectedLanguage != LanguageIndex)
            LanguageIndex = selectedLanguage;
        GUILayout.Label(MvpLocale.T("master_volume", $"{MasterVolume:0.00}"), labelStyle);
        MasterVolume = GUILayout.HorizontalSlider(MasterVolume, 0f, 1f);
        showNetworkHint = GUILayout.Toggle(showNetworkHint, MvpLocale.T("show_network"));

        GUILayout.Space(10);
        GUILayout.Label(MvpLocale.T("camera"), accentStyle);
        GUILayout.Label(MvpLocale.T("h_sensitivity", $"{PlayerCameraController.HorizontalSensitivity:0.00}"), labelStyle);
        PlayerCameraController.HorizontalSensitivity = GUILayout.HorizontalSlider(PlayerCameraController.HorizontalSensitivity, 0.25f, 8f);
        GUILayout.Label(MvpLocale.T("v_sensitivity", $"{PlayerCameraController.VerticalSensitivity:0.00}"), labelStyle);
        PlayerCameraController.VerticalSensitivity = GUILayout.HorizontalSlider(PlayerCameraController.VerticalSensitivity, 0.25f, 8f);
        PlayerCameraController.InvertY = GUILayout.Toggle(PlayerCameraController.InvertY, MvpLocale.T("invert_y"));
        GUILayout.Label(MvpLocale.T("fov", $"{PlayerCameraController.FieldOfView:0}"), labelStyle);
        PlayerCameraController.FieldOfView = GUILayout.HorizontalSlider(PlayerCameraController.FieldOfView, 55f, 95f);

        GUILayout.Space(10);
        GUILayout.Label(MvpLocale.T("voice"), accentStyle);
        ProximityVoiceChat.VoiceEnabled = GUILayout.Toggle(ProximityVoiceChat.VoiceEnabled, MvpLocale.T("voice_default_on"));
        ProximityVoiceChat.Muted = GUILayout.Toggle(ProximityVoiceChat.Muted, MvpLocale.T("mute_self"));
        DrawMicrophoneSelector();
        GUILayout.Label(MvpLocale.T("mic_gain", $"{ProximityVoiceChat.MicGain:0.0}"), labelStyle);
        ProximityVoiceChat.MicGain = GUILayout.HorizontalSlider(ProximityVoiceChat.MicGain, 0f, 2f);
        GUILayout.Label(MvpLocale.T("voice_volume", $"{ProximityVoiceChat.OutputVolume:0.0}"), labelStyle);
        ProximityVoiceChat.OutputVolume = GUILayout.HorizontalSlider(ProximityVoiceChat.OutputVolume, 0f, 2f);
        GUILayout.Label(MvpLocale.T("voice_distance", $"{ProximityVoiceChat.MaxDistance:0}"), labelStyle);
        ProximityVoiceChat.MaxDistance = GUILayout.HorizontalSlider(ProximityVoiceChat.MaxDistance, 4f, 40f);

        GUILayout.Space(14);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(MvpLocale.T("reset_defaults"), GUILayout.Height(32)))
            ResetSettingsDefaults();
        if (GUILayout.Button(MvpLocale.T("quit_game"), GUILayout.Height(32)))
            QuitGame();
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawMicrophoneSelector()
    {
        string[] devices = Microphone.devices;
        bool hasDevices = devices != null && devices.Length > 0;
        GUILayout.Label(MvpLocale.T("mic_device", ProximityVoiceChat.SelectedMicrophoneDeviceName), labelStyle);
        GUILayout.BeginHorizontal();
        GUI.enabled = hasDevices;
        if (GUILayout.Button(MvpLocale.T("prev"), GUILayout.Height(28)))
        {
            int count = devices.Length;
            ProximityVoiceChat.MicrophoneDeviceIndex = (ProximityVoiceChat.MicrophoneDeviceIndex + count - 1) % count;
        }
        if (GUILayout.Button(MvpLocale.T("next"), GUILayout.Height(28)))
        {
            int count = devices.Length;
            ProximityVoiceChat.MicrophoneDeviceIndex = (ProximityVoiceChat.MicrophoneDeviceIndex + 1) % count;
        }
        GUI.enabled = true;
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
            GUILayout.BeginArea(new Rect(18, 18, 320, 74), GUIContent.none, panelStyle);
            GUILayout.Label("Black Commission", titleStyle);
            string officeStatus;
            GUIStyle statusStyle = accentStyle;
            if (MvpPendingReward.HasPending)
            {
                officeStatus = MvpLocale.T("reward_pending", MvpPendingReward.ResultLabel);
                statusStyle = warningStyle;
            }
            else if (MvpMissionRuntime.HasSelectedTask && MvpMissionRuntime.SelectedTask != null)
            {
                officeStatus = MvpLocale.T("task_accepted", MvpMissionRuntime.SelectedTask.title);
            }
            else
            {
                officeStatus = nearShop ? MvpLocale.T("computer_connected") : MvpLocale.T("office_idle");
            }

            GUILayout.Label(officeStatus, statusStyle);
            GUILayout.EndArea();
            return;
        }

        OfficeComputer computer = activeComputer;
        float computerWidth = Mathf.Clamp(Screen.width - 36f, 360f, 720f);
        float computerHeight = Mathf.Min(600, Screen.height - 84);
        Rect rect = new Rect((Screen.width - computerWidth) * 0.5f, 42, computerWidth, computerHeight);

        Color savedColor = GUI.color;

        GUILayout.BeginArea(rect, GUIContent.none, panelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label(MvpLocale.T("terminal_title"), titleStyle);
        if (GUILayout.Button(MvpLocale.T("close_computer"), GUILayout.Width(96), GUILayout.Height(30)))
        {
            CloseComputer();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        officeScrollPosition = GUILayout.BeginScrollView(officeScrollPosition, false, true);
        GUILayout.Space(8);
        GUILayout.Label(MvpLocale.T("funds_debt", company.Funds, company.Debt), company.Funds < 0 ? warningStyle : labelStyle);
        GUILayout.Label(MvpLocale.T("rep_level_xp", company.Reputation, company.OfficeLevel, company.Experience, company.ExperienceForNextLevel), labelStyle);
        // Locked job categories hidden until content is available
        GUILayout.Label(MvpLocale.T("takeover_pressure", company.HostileTakeoverPressure), company.IsHostileTakeoverRisk ? warningStyle : labelStyle);
        GUILayout.Label(MvpLocale.T("lost_item_progress", company.CompletedLostItemJobs), labelStyle);
        GUILayout.Space(12);

        if (company.WasRecentlyHostileAcquired)
        {
            GUILayout.Label(MvpLocale.T("hostile_acquired"), warningStyle);
            GUILayout.Label(MvpLocale.T("hostile_acquired_hint"), mutedStyle);
        }
        else if (company.WasRecentlyIssuedTakeoverUltimatum)
        {
            GUILayout.Label(MvpLocale.T("ultimatum_issued"), warningStyle);
            GUILayout.Label(MvpLocale.T("ultimatum_hint"), mutedStyle);
        }
        else if (company.HasHostileTakeoverUltimatum)
        {
            GUILayout.Label(MvpLocale.T("ultimatum_active"), warningStyle);
            GUILayout.Label(MvpLocale.T("ultimatum_resolve"), mutedStyle);
        }
        else if (MvpPendingReward.HasPending)
        {
            string result = MvpPendingReward.ResultLabel;
            int displayedExperience = MvpPendingReward.ResultKind == MvpMissionResultKind.Failed ? 0 : MvpPendingReward.Experience;
            GUILayout.Label($"待领取奖励: {result}  金钱 {MvpPendingReward.Money} / 声望 {MvpPendingReward.Reputation} / 经验 {displayedExperience}", accentStyle);
            if (MvpPendingReward.HasOvertimePenalty)
                GUILayout.Label(
                    $"含超时扣罚: {MvpMissionClock.FormatGameHours(MvpPendingReward.OvertimeGameHours)}  -{MvpPendingReward.OvertimeMoneyPenalty}G / 声望 -{MvpPendingReward.OvertimeReputationPenalty}",
                    warningStyle);
            bool hostCanClaim = IsLocalHostOrSolo();
            GUI.enabled = hostCanClaim;
            if (computer != null && GUILayout.Button(hostCanClaim ? MvpLocale.T("claim_reward") : MvpLocale.T("wait_host_claim"), GUILayout.Height(34)))
                computer.ExecuteComputerAction(FindLocalPlayer());
            GUI.enabled = true;
            if (!hostCanClaim)
                GUILayout.Label(MvpLocale.T("host_only_claim"), mutedStyle);
        }
        else if (company.CanShowTutorialAcquisition)
        {
            GUILayout.Label($"新手扩张: 可吞并 0 级事务所，费用 {company.TutorialAcquisitionCost}G。", company.CanAffordTutorialAcquisition ? accentStyle : warningStyle);
            if (company.CanAffordTutorialAcquisition && computer != null)
            {
                if (GUILayout.Button("确认收购", GUILayout.Height(34)))
                    computer.ExecuteComputerAction(FindLocalPlayer());
            }
            else
            {
                GUILayout.Label("需要足够资金，并且被吞并压力低于 70。", mutedStyle);
            }
        }
        else if (company.HasAcquiredTutorialOffice)
        {
            GUILayout.Label("扩张完成: 已吞并一家 0 级事务所，第二类委托入口已解锁为后续内容。", accentStyle);
            GUILayout.Label("继续接找回失物任务可以积累资金和声望。", mutedStyle);
            GUILayout.Space(8);
            DrawCurrentCommissionOrDemo(computer);
        }
        else
        {
            DrawCurrentCommissionOrDemo(computer);
        }

        GUILayout.Space(12);
        DrawOfficeShop(computerOpen || nearShop);
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        GUI.color = savedColor;
        EnsureCrtTextures();
        DrawCrtOverlay(rect);
    }

    void DrawCurrentCommissionOrDemo(OfficeComputer computer)
    {
        if (MvpMissionRuntime.HasSelectedTask && MvpMissionRuntime.SelectedTask != null)
        {
            GUILayout.Label($"已接受委托: {MvpMissionRuntime.SelectedTask.title}", accentStyle);
            GUILayout.Label("采购完道具后，去外面的公司车出发。", mutedStyle);
            return;
        }

        DrawDemoTaskCard(computer);
    }

    void DrawDemoTaskCard(OfficeComputer computer)
    {
        if (computer == null)
        {
            GUILayout.Label(MvpLocale.T("terminal_offline"), warningStyle);
            return;
        }

        GUILayout.Label(MvpLocale.T("available_commissions"), accentStyle);
        GUILayout.BeginVertical(slotStyle);
        GUILayout.Label(computer.DemoTaskTitle, titleStyle);
        GUILayout.Label($"委托人: {computer.DemoTaskClient}    地点: {computer.DemoTaskLocation}", labelStyle);
        GUILayout.Label(computer.DemoTaskDescription, mutedStyle);
        GUILayout.Label($"报酬: {computer.DemoTaskMoneyReward} G    声望 +{computer.DemoTaskReputationReward}    经验 +{computer.DemoTaskExperienceReward}", labelStyle);
        GUILayout.Label($"作业窗口: {MvpMissionClock.GetScheduleSummary(computer.DemoTask)}", labelStyle);
        GUILayout.Label(MvpMissionClock.GetOvertimeRuleSummary(computer.DemoTask), mutedStyle);
        GUILayout.Space(8);

        bool hostReady = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && NetworkManager.Singleton.IsHost;
        if (hostReady)
        {
            if (GUILayout.Button(MvpLocale.T("accept_task"), GUILayout.Height(36)))
            {
                computer.ExecuteComputerAction(FindLocalPlayer());
                if (MvpMissionRuntime.HasSelectedTask)
                    CloseComputer();
            }
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            GUI.enabled = false;
            GUILayout.Button(MvpLocale.T("wait_host_accept"), GUILayout.Height(36));
            GUI.enabled = true;
            GUILayout.Label(MvpLocale.T("wait_host_hint"), mutedStyle);
        }
        else
        {
            GUI.enabled = false;
            GUILayout.Button(MvpLocale.T("start_host_first"), GUILayout.Height(36));
            GUI.enabled = true;
            GUILayout.Label(MvpLocale.T("start_host_hint"), mutedStyle);
        }

        GUILayout.EndVertical();
    }

    void DrawOfficeShop(bool nearShop)
    {
        GUILayout.Label(MvpLocale.T("shop_title"), accentStyle);
        PlayerHotbar activeHotbar = FindLocalHotbar();
        GUILayout.Label(GetHotbarStorageSummary(activeHotbar), mutedStyle);
        if (MvpPendingReward.HasPending)
        {
            GUILayout.Label(MvpLocale.T("claim_first"), mutedStyle);
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

        float width = Mathf.Clamp(Screen.width - 36f, 380f, 700f);
        float height = Mathf.Clamp(Screen.height - 96f, 420f, 620f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, 48, width, height);
        PlayerHotbar hotbar = FindLocalHotbar();

        GUILayout.BeginArea(rect, GUIContent.none, panelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("补给柜", titleStyle);
        if (GUILayout.Button("关闭", GUILayout.Width(72), GUILayout.Height(30)))
        {
            CloseCabinet();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        cabinetScrollPosition = GUILayout.BeginScrollView(cabinetScrollPosition, false, true);
        GUILayout.Space(6);
        GUILayout.Label("柜内 8 格", accentStyle);
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

        GUILayout.Space(12);
        GUILayout.Label("热栏", accentStyle);
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

    void DrawBestiaryPanel()
    {
        if (activeBestiary == null) return;

        float width = Mathf.Clamp(Screen.width - 36f, 380f, 680f);
        float height = Mathf.Clamp(Screen.height - 96f, 380f, 580f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, 54, width, height);

        GUILayout.BeginArea(rect, GUIContent.none, panelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("怪物图鉴", titleStyle);
        if (GUILayout.Button("关闭", GUILayout.Width(72), GUILayout.Height(30)))
        {
            CloseBestiary();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        bestiaryScrollPosition = GUILayout.BeginScrollView(bestiaryScrollPosition, false, true);
        GUILayout.Space(8);
        bool unlocked = MonsterBestiaryProgress.IsHomeworkDebtCollectorUnlocked;
        GUILayout.BeginVertical(unlocked ? selectedSlotStyle : slotStyle);
        GUILayout.Label(unlocked ? "作业债务催收员" : "未解锁档案", accentStyle);
        if (unlocked)
        {
            GUILayout.Label("介绍", accentStyle);
            GUILayout.Label("一种在逾期表格、家长签字和旧教室灯管之间徘徊的异常。它会被错误翻找、噪声和落单玩家吸引，追击时像是在索要一笔永远算不清的账。", labelStyle);
            GUILayout.Space(8);
            GUILayout.Label("弱点", accentStyle);
            GUILayout.Label("强光会让它短暂失去行动节奏；错误作业本和登记簿附近的响动可以把它引开。保持距离、利用储物柜和路线分岔，别在空走廊里硬跑。", labelStyle);
            GUILayout.Space(8);
            GUILayout.Label("解锁记录: 已遭遇异常，已采集毛发/踪迹。", mutedStyle);
        }
        else
        {
            string encounter = MonsterBestiaryProgress.HasEncounteredHomeworkDebtCollector ? "已遭遇" : "未遭遇";
            string trace = MonsterBestiaryProgress.HasHomeworkDebtCollectorTrace ? "已采集" : "未采集";
            GUILayout.Label($"解锁条件: 遭遇怪物 + 采集毛发/踪迹。当前: {encounter} / {trace}。", mutedStyle);
            GUILayout.Label("档案纸页上只有水渍和空白格，等你带回足够可靠的证据。", labelStyle);
        }
        GUILayout.EndVertical();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawMissionPanel()
    {
        LostItemMissionManager mission = LostItemMissionManager.Instance;
        GUILayout.BeginArea(new Rect(18, 18, panelWidth, 230), GUIContent.none, panelStyle);
        DrawFieldClockLine(mission);
        GUILayout.Label(GetMissionObjective(mission), accentStyle);
        GUILayout.Label(GetCarrierText(mission), mission.LostItemCollected.Value ? accentStyle : mutedStyle);
        GUILayout.Label(GetBonusEvidenceText(mission), mission.BonusEvidenceCollected.Value ? accentStyle : mutedStyle);

        string monsterText = GetMonsterStatus();
        if (!string.IsNullOrEmpty(monsterText))
            GUILayout.Label(monsterText, monsterText.Contains("追击") ? warningStyle : mutedStyle);
        if (!string.IsNullOrEmpty(missionMessage) && Time.time < missionMessageUntil)
            GUILayout.Label(missionMessage, missionMessage.Contains("警告") ? warningStyle : accentStyle);

        DrawSpectatorHint();

        GUILayout.EndArea();
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
        return null;
    }

    void DrawMissionVanPanel()
    {
        SchoolExitPoint van = activeMissionVan;
        if (van == null) return;

        float width = Mathf.Clamp(Screen.width - 36f, 320f, 560f);
        float height = Mathf.Clamp(Screen.height - 112f, 360f, 560f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, 56, width, height);
        GUILayout.BeginArea(rect, GUIContent.none, panelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label(MvpLocale.T("mission_van"), titleStyle);
        if (GUILayout.Button(MvpLocale.T("close_door"), GUILayout.Width(72), GUILayout.Height(30)))
        {
            CloseMissionVan();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label(van.GetReturnSummary(), accentStyle);
        DrawVehicleClockBlock(LostItemMissionManager.Instance);
        GUILayout.Label(MvpLocale.T("van_decide_hint"), mutedStyle);
        GUILayout.Space(10);

        if (DrawMissionVanReturnControls(van))
        {
            GUILayout.EndArea();
            return;
        }

        GUILayout.Space(10);
        GUILayout.Label(MvpLocale.T("van_locker"), accentStyle);
        PlayerHotbar localHotbar = FindLocalHotbar();
        for (int i = 0; i < SchoolExitPoint.LockerSlotCount; i++)
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

    bool DrawMissionVanReturnControls(SchoolExitPoint van)
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

    void DrawFieldClockLine(LostItemMissionManager mission)
    {
        if (mission == null) return;

        if (HasLocalWristwatch())
        {
            GUILayout.Label(
                $"工时表: {mission.CurrentClockLabel}    合同截止: {mission.DeadlineClockLabel}",
                mission.IsOvertime ? warningStyle : labelStyle);
            DrawOvertimeLine(mission);
            return;
        }

        GUILayout.Label($"天光判断: {MvpMissionClock.GetDaylightLabel(mission.CurrentClockHour)}", labelStyle);
        GUILayout.Label("准确时间: 回委托车看车载钟，或在事务所购买廉价工时表。", mutedStyle);
        if (mission.IsOvertime)
            GUILayout.Label("你感觉已经拖过合同窗口了，返程结算会被扣。", warningStyle);
    }

    void DrawVehicleClockBlock(LostItemMissionManager mission)
    {
        if (mission == null) return;

        GUILayout.BeginVertical(slotStyle);
        GUILayout.Label(
            $"车载时钟: {mission.CurrentClockLabel}    标准下班: {mission.DeadlineClockLabel}",
            mission.IsOvertime ? warningStyle : labelStyle);
        if (mission.IsOvertime)
        {
            DrawOvertimeLine(mission);
        }
        else
        {
            GUILayout.Label($"剩余窗口: {MvpMissionClock.FormatGameHours(mission.RemainingGameHours)}", accentStyle);
        }
        GUILayout.EndVertical();
        GUILayout.Space(8);
    }

    void DrawOvertimeLine(LostItemMissionManager mission)
    {
        if (mission == null || !mission.IsOvertime) return;

        GUILayout.Label(
            $"超时: {MvpMissionClock.FormatGameHours(mission.OvertimeGameHours)}    预计扣款 -{mission.OvertimeMoneyPenalty}G / 声望 -{mission.OvertimeReputationPenalty}",
            warningStyle);
    }

    bool HasLocalWristwatch()
    {
        PlayerHotbar hotbar = FindLocalHotbar();
        return hotbar != null && hotbar.HasWristwatchOwned;
    }

    void DrawHotbar()
    {
        PlayerHotbar hotbar = FindLocalHotbar();
        if (hotbar == null) return;

        int slotSize = Mathf.Clamp((Screen.width - 56) / PlayerHotbar.SlotCount, 62, 92);
        int gap = Mathf.Clamp(slotSize / 11, 5, 8);
        int slotHeight = Mathf.Clamp(Mathf.RoundToInt(slotSize * 0.76f), 52, 70);
        int iconSize = Mathf.Clamp(Mathf.RoundToInt(slotSize * 0.48f), 32, 44);
        int totalWidth = PlayerHotbar.SlotCount * slotSize + (PlayerHotbar.SlotCount - 1) * gap;
        float startX = (Screen.width - totalWidth) * 0.5f;
        float y = Screen.height - 92f;

        for (int i = 0; i < PlayerHotbar.SlotCount; i++)
        {
            HotbarSlot slot = hotbar.GetSlot(i);
            bool selected = hotbar.SelectedSlot.Value == i;
            Rect rect = new Rect(startX + i * (slotSize + gap), y, slotSize, slotHeight);
            GUI.Label(rect, GUIContent.none, selected ? selectedSlotStyle : slotStyle);

            string qty = slot == null || slot.IsEmpty ? "" : $" x{slot.quantity}";
            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, 18), $"{i + 1}", mutedStyle);
            GUI.DrawTexture(new Rect(rect.x + (rect.width - iconSize) * 0.5f, rect.y + 16, iconSize, iconSize),
                GetItemIcon(slot == null || slot.IsEmpty ? MvpHotbarItemId.None : slot.itemId),
                ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(rect.x + rect.width - 38, rect.y + rect.height - 22, 34, 16), qty, mutedStyle);

            // Battery level bar for flashlight slot
            if (slot != null && slot.itemId == MvpHotbarItemId.Flashlight)
                DrawFlashlightBar(rect);
        }

        DrawHealthBar(startX, y - 14f, totalWidth);
    }

    void DrawHealthBar(float x, float y, float width)
    {
        PlayerHealth localHealth = FindLocalPlayerHealth();
        if (localHealth == null) return;

        float hp = localHealth.CurrentHP.Value;
        float maxHp = 100f;

        if (hp < lastKnownHp)
        {
            damageFlashUntil = Time.time + 0.35f;
            lastKnownHp = hp;
        }
        else if (hp > lastKnownHp)
            lastKnownHp = hp;

        float barH = 6f;
        float fillW = width * Mathf.Clamp01(hp / maxHp);
        GUI.DrawTexture(new Rect(x, y, width, barH), hpBarBg);
        GUI.DrawTexture(new Rect(x, y, fillW, barH), hpBarFill);
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
        Color barColor = normalized > 0.4f
            ? new Color(0.85f, 0.60f, 0.19f, 0.9f)  // amber
            : new Color(0.9f, 0.1f, 0.05f, 0.9f);    // red when low

        GUI.DrawTexture(new Rect(slotRect.x + 4, barY, barW, 4), hpBarBg);
        GUI.DrawTexture(new Rect(slotRect.x + 4, barY, barW * normalized, 4), MakeTexture(barColor));
    }

    void DrawFooterHint()
    {
        string text = "多人 MVP: Start Host 后接任务；1-5 切热栏，左键/H 使用，HQ 内按 G 丢当前格到地上存放。";
        GUI.Label(new Rect(18, Screen.height - 30, 720, 24), text, mutedStyle);
    }

    static string GetMissionObjective(LostItemMissionManager mission)
    {
        string paperworkRisk = mission.WrongHomeworkAttempts.Value > 0
            ? $" 已翻错 {mission.WrongHomeworkAttempts.Value}/3 本，预计扣 {mission.WrongHomeworkMoneyPenalty}G。"
            : "";

        switch (mission.CurrentPhase.Value)
        {
            case LostItemMissionManager.MissionPhase.Searching:
                return "目标: 核对记录室登记簿，找到真正盖章作业本。" + paperworkRisk;
            case LostItemMissionManager.MissionPhase.ReturnToExit:
                return "目标: 带着作业本回到校门口的委托车尾，按 E 打开后舱返程。" + paperworkRisk;
            case LostItemMissionManager.MissionPhase.Completed:
                return "目标: 委托完成，返回事务所领取奖励。";
            case LostItemMissionManager.MissionPhase.ReturnedEarly:
                return "目标: 已提前返程，回事务所做部分结算。";
            case LostItemMissionManager.MissionPhase.Failed:
                return "目标: 委托失败，返回事务所复盘。";
            default:
                return "目标: 等待任务状态。";
        }
    }

    static string GetCarrierText(LostItemMissionManager mission)
    {
        if (!mission.LostItemCollected.Value) return "作业本状态: 尚未找回";
        ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
        return mission.CarrierClientId.Value == localId
            ? "作业本状态: 你拿到了，快回校门。"
            : $"作业本状态: 队友 {mission.CarrierClientId.Value} 拿到了。";
    }

    static string GetBonusEvidenceText(LostItemMissionManager mission)
    {
        if (mission.BonusEvidenceCollected.Value)
            return mission.WrongHomeworkAttempts.Value > 0
                ? "核验状态: 登记簿已拍照，但之前翻错的作业本仍会扣一点结算。"
                : "核验状态: 登记簿已拍照，真正作业本更容易确认。";

        return "核验状态: 记录室登记簿尚未拍照，乱翻相似作业本会扣结算。";
    }

    static string GetMonsterStatus()
    {
        SchoolMonsterAI[] monsters = FindObjectsByType<SchoolMonsterAI>(FindObjectsSortMode.None);
        bool anyStunned = false;
        foreach (var monster in monsters)
        {
            if (monster == null) continue;
            if (monster.IsChasing) return "危险: 怪物正在追击。";
            anyStunned |= monster.IsStunned;
            if (monster.IsDistracted) return "危险: 怪物被诱饵短暂吸引。";
        }

        return anyStunned ? "危险: 怪物被短暂控制。" : "危险: 保持安静，别靠太近。";
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

    public static void OpenComputer(OfficeComputer computer)
    {
        activeMissionVan = null;
        activeCabinet = null;
        activeBestiary = null;
        settingsOpen = false;
        activeComputer = computer;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ComputerCloseupCamera.Enter(computer.transform);
        AudioManager.Instance?.PlayComputerOpen(computer.transform.position);
    }

    public static void OpenMissionVan(SchoolExitPoint van)
    {
        activeComputer = null;
        activeCabinet = null;
        activeBestiary = null;
        settingsOpen = false;
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
        settingsOpen = false;
        activeCabinet = cabinet;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void OpenBestiary(OfficeMonsterBestiary bestiary)
    {
        activeComputer = null;
        activeMissionVan = null;
        activeCabinet = null;
        settingsOpen = false;
        activeBestiary = bestiary;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    static void OpenSettings()
    {
        activeComputer = null;
        activeMissionVan = null;
        activeCabinet = null;
        activeBestiary = null;
        settingsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    static void CloseComputer()
    {
        ComputerCloseupCamera.Exit();
        activeComputer = null;
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

    static void CloseSettings()
    {
        settingsOpen = false;
        RestoreGameplayCursor();
    }

    static void ResetSettingsDefaults()
    {
        LanguageIndex = 0;
        MasterVolume = 1f;
        PlayerCameraController.HorizontalSensitivity = 2f;
        PlayerCameraController.VerticalSensitivity = 2f;
        PlayerCameraController.InvertY = false;
        PlayerCameraController.FieldOfView = 68f;
        ProximityVoiceChat.VoiceEnabled = true;
        ProximityVoiceChat.Muted = false;
        ProximityVoiceChat.MicGain = 1f;
        ProximityVoiceChat.OutputVolume = 1f;
        ProximityVoiceChat.MaxDistance = 18f;
        ProximityVoiceChat.MicrophoneDeviceIndex = 0;
    }

    static void QuitGame()
    {
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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

    bool IsPartialReturnConfirmed(SchoolExitPoint van)
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

        panelTexture = MakeTexture(new Color(0.03f, 0.035f, 0.04f, 0.82f));
        slotTexture = MakeTexture(new Color(0.05f, 0.06f, 0.07f, 0.78f));
        selectedSlotTexture = MakeTexture(new Color(0.16f, 0.22f, 0.20f, 0.9f));
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
            normal = { textColor = new Color(0.9f, 0.95f, 0.9f) },
            wordWrap = true
        };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            normal = { textColor = new Color(0.86f, 0.88f, 0.84f) },
            wordWrap = true
        };
        mutedStyle = new GUIStyle(labelStyle)
        {
            fontSize = 13,
            normal = { textColor = new Color(0.62f, 0.66f, 0.64f) }
        };
        accentStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.56f, 0.92f, 0.72f) }
        };
        warningStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.5f, 0.42f) }
        };

        EnsureCrtTextures();

        if (hpBarBg == null)
        {
            hpBarBg = MakeTexture(new Color(0.06f, 0.07f, 0.07f, 0.85f));
            hpBarFill = MakeTexture(new Color(0.78f, 0.12f, 0.08f, 0.9f));
            damageFlashTex = MakeTexture(new Color(0.9f, 0.05f, 0.02f, 0.35f));
        }

        MvpFontProvider.ApplyToStyle(panelStyle);
        MvpFontProvider.ApplyToStyle(titleStyle);
        MvpFontProvider.ApplyToStyle(labelStyle);
        MvpFontProvider.ApplyToStyle(mutedStyle);
        MvpFontProvider.ApplyToStyle(accentStyle);
        MvpFontProvider.ApplyToStyle(warningStyle);
        MvpFontProvider.ApplyToStyle(slotStyle);
        MvpFontProvider.ApplyToStyle(selectedSlotStyle);
    }

    void EnsureIcons()
    {
        emptyIcon = MakeIcon(new Color(0.07f, 0.08f, 0.08f, 0.9f), new Color(0.24f, 0.28f, 0.26f), Color.clear, 0);
        // battery: amber cylinder shape
        decoyIcon = MakeIcon(new Color(0.73f, 0.50f, 0.16f), new Color(0.45f, 0.28f, 0.05f), new Color(0.95f, 0.85f, 0.3f), 2);
        flashlightIcon = MakeIcon(new Color(0.18f, 0.2f, 0.19f), new Color(1f, 0.86f, 0.25f), new Color(0.03f, 0.035f, 0.04f), 4);
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
