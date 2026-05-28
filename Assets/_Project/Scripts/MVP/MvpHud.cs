using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class MvpHud : MonoBehaviour
{
    const float OfficeComputerShopDistance = 3.4f;
    static OfficeComputer activeComputer;
    static SchoolExitPoint activeMissionVan;
    static bool settingsOpen;
    public static bool IsComputerOpen => activeComputer != null;
    public static bool IsBlockingPanelOpen => activeComputer != null || activeMissionVan != null || settingsOpen;

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
    Texture2D medkitIcon;
    Texture2D sprayIcon;
    Texture2D decoyIcon;
    Texture2D flashlightIcon;
    string shopMessage;
    float shopMessageUntil;
    string missionMessage;
    float missionMessageUntil;
    Vector2 officeScrollPosition;
    Vector2 settingsScrollPosition;
    static SchoolExitPoint partialReturnConfirmVan;
    static float partialReturnConfirmUntil;

    static int LanguageIndex
    {
        get => PlayerPrefs.GetInt("AS.Settings.Language", 0);
        set => PlayerPrefs.SetInt("AS.Settings.Language", Mathf.Clamp(value, 0, 1));
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

    void Awake()
    {
        activeComputer = null;
        activeMissionVan = null;
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

        if (activeComputer != null)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                CloseComputer();
                return;
            }

            if (!MvpPendingReward.HasPending)
            {
                PlayerHotbar activeHotbar = FindLocalHotbar();
                if (activeHotbar != null)
                {
                    if (keyboard.f1Key.wasPressedThisFrame) TryBuy(activeHotbar, MvpHotbarItemId.Medkit);
                    if (keyboard.f2Key.wasPressedThisFrame) TryBuy(activeHotbar, MvpHotbarItemId.Decoy);
                    if (keyboard.f3Key.wasPressedThisFrame) TryBuy(activeHotbar, MvpHotbarItemId.StunSpray);
                    if (keyboard.f4Key.wasPressedThisFrame) TryBuy(activeHotbar, MvpHotbarItemId.Flashlight);
                    if (keyboard.f5Key.wasPressedThisFrame) TryBuyWristwatch(activeHotbar);
                }
            }

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
        }

        if (showNetworkHint)
            DrawFooterHint();
        if (settingsOpen)
            DrawSettingsPanel();
    }

    void DrawSettingsPanel()
    {
        float width = Mathf.Clamp(Screen.width - 36f, 360f, 620f);
        float height = Mathf.Clamp(Screen.height - 80f, 420f, 660f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUILayout.BeginArea(rect, GUIContent.none, panelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("暂停", titleStyle);
        if (GUILayout.Button("继续", GUILayout.Width(72), GUILayout.Height(30)))
        {
            CloseSettings();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        settingsScrollPosition = GUILayout.BeginScrollView(settingsScrollPosition, false, true);
        GUILayout.Space(8);
        GUILayout.Label("游戏", accentStyle);
        string[] languageLabels = { "简体中文", "English" };
        GUILayout.Label($"语言: {languageLabels[LanguageIndex]}", labelStyle);
        int selectedLanguage = GUILayout.SelectionGrid(LanguageIndex, languageLabels, 2);
        if (selectedLanguage != LanguageIndex)
            LanguageIndex = selectedLanguage;
        GUILayout.Label($"主音量: {MasterVolume:0.00}", labelStyle);
        MasterVolume = GUILayout.HorizontalSlider(MasterVolume, 0f, 1f);
        showNetworkHint = GUILayout.Toggle(showNetworkHint, "显示网络提示");

        GUILayout.Space(10);
        GUILayout.Label("视角", accentStyle);
        GUILayout.Label($"鼠标水平速度: {PlayerCameraController.HorizontalSensitivity:0.00}", labelStyle);
        PlayerCameraController.HorizontalSensitivity = GUILayout.HorizontalSlider(PlayerCameraController.HorizontalSensitivity, 0.25f, 8f);
        GUILayout.Label($"鼠标垂直速度: {PlayerCameraController.VerticalSensitivity:0.00}", labelStyle);
        PlayerCameraController.VerticalSensitivity = GUILayout.HorizontalSlider(PlayerCameraController.VerticalSensitivity, 0.25f, 8f);
        PlayerCameraController.InvertY = GUILayout.Toggle(PlayerCameraController.InvertY, "反转垂直视角");
        GUILayout.Label($"视野范围: {PlayerCameraController.FieldOfView:0}", labelStyle);
        PlayerCameraController.FieldOfView = GUILayout.HorizontalSlider(PlayerCameraController.FieldOfView, 55f, 95f);

        GUILayout.Space(10);
        GUILayout.Label("语音", accentStyle);
        ProximityVoiceChat.VoiceEnabled = GUILayout.Toggle(ProximityVoiceChat.VoiceEnabled, "默认开启语音");
        ProximityVoiceChat.Muted = GUILayout.Toggle(ProximityVoiceChat.Muted, "静音自己");
        DrawMicrophoneSelector();
        GUILayout.Label($"麦克风增益: {ProximityVoiceChat.MicGain:0.0}", labelStyle);
        ProximityVoiceChat.MicGain = GUILayout.HorizontalSlider(ProximityVoiceChat.MicGain, 0f, 2f);
        GUILayout.Label($"语音音量: {ProximityVoiceChat.OutputVolume:0.0}", labelStyle);
        ProximityVoiceChat.OutputVolume = GUILayout.HorizontalSlider(ProximityVoiceChat.OutputVolume, 0f, 2f);
        GUILayout.Label($"语音距离: {ProximityVoiceChat.MaxDistance:0} m", labelStyle);
        ProximityVoiceChat.MaxDistance = GUILayout.HorizontalSlider(ProximityVoiceChat.MaxDistance, 4f, 40f);

        GUILayout.Space(14);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("恢复默认", GUILayout.Height(32)))
            ResetSettingsDefaults();
        if (GUILayout.Button("退出游戏", GUILayout.Height(32)))
            QuitGame();
        GUILayout.EndHorizontal();
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawMicrophoneSelector()
    {
        string[] devices = Microphone.devices;
        bool hasDevices = devices != null && devices.Length > 0;
        GUILayout.Label($"麦克风: {ProximityVoiceChat.SelectedMicrophoneDeviceName}", labelStyle);
        GUILayout.BeginHorizontal();
        GUI.enabled = hasDevices;
        if (GUILayout.Button("上一个", GUILayout.Height(28)))
        {
            int count = devices.Length;
            ProximityVoiceChat.MicrophoneDeviceIndex = (ProximityVoiceChat.MicrophoneDeviceIndex + count - 1) % count;
        }
        if (GUILayout.Button("下一个", GUILayout.Height(28)))
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
            GUILayout.Label("Accident Squad", titleStyle);
            string officeStatus;
            GUIStyle statusStyle = accentStyle;
            if (MvpPendingReward.HasPending)
            {
                officeStatus = $"结算待领取: {MvpPendingReward.ResultLabel}。去电脑盖章入账。";
                statusStyle = warningStyle;
            }
            else if (MvpMissionRuntime.HasSelectedTask && MvpMissionRuntime.SelectedTask != null)
            {
                officeStatus = $"委托已接受: {MvpMissionRuntime.SelectedTask.title}。采购后去公司车。";
            }
            else
            {
                officeStatus = nearShop ? "办公室电脑已连接，可接单或采购。" : "事务所待机中。";
            }

            GUILayout.Label(officeStatus, statusStyle);
            GUILayout.EndArea();
            return;
        }

        OfficeComputer computer = activeComputer;
        float computerWidth = Mathf.Clamp(Screen.width - 36f, 360f, 720f);
        Rect rect = new Rect((Screen.width - computerWidth) * 0.5f, 42, computerWidth, Mathf.Min(600, Screen.height - 84));

        GUILayout.BeginArea(rect, GUIContent.none, panelStyle);
        GUILayout.BeginHorizontal();
        GUILayout.Label("ACCIDENT SQUAD 委托终端", titleStyle);
        if (GUILayout.Button("关闭电脑", GUILayout.Width(96), GUILayout.Height(30)))
        {
            CloseComputer();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        officeScrollPosition = GUILayout.BeginScrollView(officeScrollPosition, false, true);
        GUILayout.Space(8);
        GUILayout.Label($"资金: {company.Funds} G    债务: {company.Debt} G", company.Funds < 0 ? warningStyle : labelStyle);
        GUILayout.Label($"声望: {company.Reputation}    等级: {company.OfficeLevel}    经验: {company.Experience}/{company.ExperienceForNextLevel}", labelStyle);
        GUILayout.Label($"已解锁委托类别: {company.UnlockedCategoryCount}/8", labelStyle);
        GUILayout.Label($"被吞并压力: {company.HostileTakeoverPressure}/100", company.IsHostileTakeoverRisk ? warningStyle : labelStyle);
        GUILayout.Label($"找回失物委托: {company.CompletedLostItemJobs}/2", labelStyle);
        GUILayout.Space(12);

        if (company.WasRecentlyHostileAcquired)
        {
            GUILayout.Label("警告: 事务所刚被竞对低价吞并，债务重组，等级和委托进度已被压回。", warningStyle);
            GUILayout.Label("继续赚钱和提升声望可以降低下一次被吞并风险。", mutedStyle);
        }
        else if (company.WasRecentlyIssuedTakeoverUltimatum)
        {
            GUILayout.Label("竞对发来收购威胁: 事务所已进入最后通牒状态。", warningStyle);
            GUILayout.Label("下一次失败前最好先把资金或声望拉回安全线。", mutedStyle);
        }
        else if (company.HasHostileTakeoverUltimatum)
        {
            GUILayout.Label("最后通牒: 再失败且资金/声望仍为负，就会被竞对强制吞并。", warningStyle);
            GUILayout.Label("完成委托、攒现金或提高声望可以解除风险。", mutedStyle);
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
            if (computer != null && GUILayout.Button(hostCanClaim ? "领取结算" : "等待房主领取结算", GUILayout.Height(34)))
                computer.ExecuteComputerAction(FindLocalPlayer());
            GUI.enabled = true;
            if (!hostCanClaim)
                GUILayout.Label("结算盖章只能由房主确认，确认后全队同步。", mutedStyle);
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
            GUILayout.Label("委托终端未连接。", warningStyle);
            return;
        }

        GUILayout.Label("可用委托", accentStyle);
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
            if (GUILayout.Button("接受委托", GUILayout.Height(36)))
            {
                computer.ExecuteComputerAction(FindLocalPlayer());
                if (MvpMissionRuntime.HasSelectedTask)
                    CloseComputer();
            }
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            GUI.enabled = false;
            GUILayout.Button("等待房主接受委托", GUILayout.Height(36));
            GUI.enabled = true;
            GUILayout.Label("等待房主选择委托。", mutedStyle);
        }
        else
        {
            GUI.enabled = false;
            GUILayout.Button("Start Host 后接受委托", GUILayout.Height(36));
            GUI.enabled = true;
            GUILayout.Label("主机启动后可接受委托。", mutedStyle);
        }

        GUILayout.EndVertical();
    }

    void DrawOfficeShop(bool nearShop)
    {
        GUILayout.Label("电脑商店", accentStyle);
        PlayerHotbar activeHotbar = FindLocalHotbar();
        GUILayout.Label(GetHotbarStorageSummary(activeHotbar), mutedStyle);
        if (MvpPendingReward.HasPending)
        {
            GUILayout.Label("先在电脑上领取本次委托结算，之后才能采购下一单道具。", mutedStyle);
            return;
        }

        bool canBuy = activeHotbar != null && nearShop;

        GUILayout.BeginHorizontal();
        DrawShopButton(activeHotbar, MvpHotbarItemId.Medkit, "回血药", canBuy);
        DrawShopButton(activeHotbar, MvpHotbarItemId.Decoy, "诱饵", canBuy);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        DrawShopButton(activeHotbar, MvpHotbarItemId.StunSpray, "定身喷雾", canBuy);
        DrawShopButton(activeHotbar, MvpHotbarItemId.Flashlight, "手电", canBuy);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        DrawWristwatchShopButton(activeHotbar, canBuy);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(shopMessage) && Time.time < shopMessageUntil)
            GUILayout.Label(shopMessage, shopMessage.Contains("不足") ? warningStyle : accentStyle);
        GUILayout.Label(canBuy
            ? "快捷采购: F1回血药 / F2诱饵 / F3定身喷雾 / F4手电 / F5工时表。HQ 内按 G 可把当前热栏道具放到地上存放。"
            : "站到电脑前才可采购。HQ 内按 G 可把当前热栏道具放到地上存放。",
            mutedStyle);
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

        GUILayout.EndArea();
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
        GUILayout.Label("事故车后舱", titleStyle);
        if (GUILayout.Button("关门", GUILayout.Width(72), GUILayout.Height(30)))
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
        GUILayout.Label("先决定是否返程；补给柜只是可选物资，不影响关门回事务所。", mutedStyle);
        GUILayout.Space(10);

        if (DrawMissionVanReturnControls(van))
        {
            GUILayout.EndArea();
            return;
        }

        GUILayout.Space(10);
        GUILayout.Label("车载补给柜", accentStyle);
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
            if (GUILayout.Button("取出", GUILayout.Width(88), GUILayout.Height(28)))
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
        if (GUILayout.Button("继续搜索", GUILayout.Height(32)))
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
        GUILayout.Label("准确时间: 回事故车看车载钟，或在事务所购买廉价工时表。", mutedStyle);
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
        }

        GUI.Label(new Rect(startX, y - 24f, totalWidth, 22f),
            "1-5切换  左键/H使用  回到事故车可取补给或提前返程",
            mutedStyle);
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
                return "目标: 带着作业本回到校门口的事故车尾，按 E 打开后舱返程。" + paperworkRisk;
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
        settingsOpen = false;
        activeComputer = computer;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void OpenMissionVan(SchoolExitPoint van)
    {
        activeComputer = null;
        settingsOpen = false;
        activeMissionVan = van;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    static void OpenSettings()
    {
        activeComputer = null;
        activeMissionVan = null;
        settingsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    static void CloseComputer()
    {
        activeComputer = null;
        RestoreGameplayCursor();
    }

    static void CloseMissionVan()
    {
        activeMissionVan = null;
        partialReturnConfirmVan = null;
        RestoreGameplayCursor();
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
            case MvpHotbarItemId.Medkit:
                return medkitIcon;
            case MvpHotbarItemId.StunSpray:
                return sprayIcon;
            case MvpHotbarItemId.Decoy:
                return decoyIcon;
            case MvpHotbarItemId.Flashlight:
                return flashlightIcon;
            default:
                return emptyIcon;
        }
    }

    static string GetShopItemLabel(MvpHotbarItemId itemId)
    {
        switch (itemId)
        {
            case MvpHotbarItemId.Medkit:
                return "回血药";
            case MvpHotbarItemId.Decoy:
                return "诱饵";
            case MvpHotbarItemId.StunSpray:
                return "定身喷雾";
            case MvpHotbarItemId.Flashlight:
                return "手电";
            default:
                return "道具";
        }
    }

    static string GetHotbarStorageSummary(PlayerHotbar hotbar)
    {
        if (hotbar == null)
            return "热栏: 未找到本地玩家。";

        int usedSlots = 0;
        int medkits = 0;
        int decoys = 0;
        int sprays = 0;
        int flashlights = 0;
        for (int i = 0; i < PlayerHotbar.SlotCount; i++)
        {
            HotbarSlot slot = hotbar.GetSlot(i);
            if (slot == null || slot.IsEmpty) continue;

            usedSlots++;
            switch (slot.itemId)
            {
                case MvpHotbarItemId.Medkit:
                    medkits += slot.quantity;
                    break;
                case MvpHotbarItemId.Decoy:
                    decoys += slot.quantity;
                    break;
                case MvpHotbarItemId.StunSpray:
                    sprays += slot.quantity;
                    break;
                case MvpHotbarItemId.Flashlight:
                    flashlights += slot.quantity;
                    break;
            }
        }

        string watch = hotbar.HasWristwatchOwned ? "已佩戴工时表" : "未购工时表";
        return $"热栏: {usedSlots}/{PlayerHotbar.SlotCount}格  回血药x{medkits} / 诱饵x{decoys} / 定身喷雾x{sprays} / 手电x{flashlights}  {watch}";
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
    }

    void EnsureIcons()
    {
        emptyIcon = MakeIcon(new Color(0.07f, 0.08f, 0.08f, 0.9f), new Color(0.24f, 0.28f, 0.26f), Color.clear, 0);
        medkitIcon = MakeIcon(new Color(0.88f, 0.87f, 0.78f), new Color(0.78f, 0.06f, 0.04f), Color.white, 1);
        decoyIcon = MakeIcon(new Color(0.95f, 0.63f, 0.16f), new Color(0.38f, 0.18f, 0.04f), new Color(0.9f, 0.08f, 0.05f), 2);
        sprayIcon = MakeIcon(new Color(0.13f, 0.62f, 0.72f), new Color(0.02f, 0.08f, 0.1f), new Color(0.8f, 0.95f, 1f), 3);
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
}
