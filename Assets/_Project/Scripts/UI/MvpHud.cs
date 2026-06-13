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
    public static bool IsBlockingPanelOpen =>
        activeComputer != null || activeMissionVan != null || activeCabinet != null || activeBestiary != null || SettingsOverlay.IsOpen;

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
        showNetworkHint = false;
        AudioListener.volume = MasterVolume;
        ProximityVoiceChat.EnsureInstance();
        SettingsOverlay.EnsureInstance();
        if (TowerMissionManager.Instance != null)
            RestoreGameplayCursor();
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

            // Shop purchases are mouse-click only (buttons in DrawOfficeShop)

            return;
        }

        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            SettingsOverlay.Open();
            return;
        }

        if (TowerMissionManager.Instance != null) return;
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

        EnsureStyles();
        BlackCommissionUiTheme.ApplyButtonSkin(buttonStyle);

        if (TowerMissionManager.Instance != null)
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
        DrawStaminaBar();
        if (showNetworkHint)
            DrawFooterHint();
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
        float computerWidth = Mathf.Clamp(Screen.width - 48f, 760f, 1050f);
        float computerHeight = Mathf.Clamp(Screen.height - 76f, 560f, 720f);
        Rect rect = new Rect((Screen.width - computerWidth) * 0.5f, 38f, computerWidth, computerHeight);

        DrawOfficeManagementTerminal(rect, computer, company, nearShop);

        EnsureCrtTextures();
        DrawCrtOverlay(rect);
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

    void DrawOfficeManagementTerminal(Rect rect, OfficeComputer computer, CompanyState company, bool nearShop)
    {
        Color oldColor = GUI.color;
        GUI.BeginGroup(rect, GUIContent.none, terminalPaperStyle);

        float pad = 24f;
        Rect content = new Rect(pad, 20f, rect.width - pad * 2f, rect.height - 40f);
        GUI.Label(new Rect(content.x, content.y, 520f, 24f),
            "BLACK COMMISSION OFFICE MANAGEMENT SYSTEM v1.3", terminalTitleStyle);
        GUI.Label(new Rect(content.x, content.y + 22f, 340f, 22f),
            "事务所办公管理系统。", terminalMutedStyle);
        GUI.Label(new Rect(content.xMax - 250f, content.y, 250f, 22f),
            "1998-11-07   22:13", terminalLabelStyle);
        GUI.Label(new Rect(content.xMax - 250f, content.y + 22f, 250f, 22f),
            "USER: BC_STAFF", terminalLabelStyle);

        Rect left = new Rect(content.x, content.y + 58f, 210f, content.height - 72f);
        Rect main = new Rect(left.xMax + 14f, left.y, content.width - left.width - 14f, content.height - 72f);
        DrawOfficeTerminalMenu(left);
        DrawOfficeTerminalFiles(main, computer, company, nearShop);

        GUI.EndGroup();
        GUI.color = oldColor;
    }

    void DrawOfficeTerminalMenu(Rect rect)
    {
        GUI.Box(rect, GUIContent.none, terminalBoxStyle);
        GUI.Label(new Rect(rect.x + 14f, rect.y + 14f, rect.width - 28f, 22f),
            "主菜单 / MAIN MENU", terminalLabelStyle);

        string[] items =
        {
            "1. 委托管理\nCOMMISSION FILES",
            "2. 公司账本\nLEDGER",
            "3. 采购目录\nSUPPLY CATALOG",
            "4. 档案记录\nARCHIVES",
            "5. 员工管理\nSTAFF RECORD",
            "6. 系统设置\nSYSTEM"
        };

        for (int i = 0; i < items.Length; i++)
        {
            Rect row = new Rect(rect.x + 12f, rect.y + 52f + i * 58f, rect.width - 24f, 46f);
            GUI.Box(row, GUIContent.none, i == 0 ? terminalSelectedButtonStyle : terminalBoxStyle);
            GUI.Label(new Rect(row.x + 18f, row.y + 8f, 28f, 28f), MenuGlyph(i), terminalLabelStyle);
            GUI.Label(new Rect(row.x + 54f, row.y + 6f, row.width - 70f, row.height - 8f),
                items[i], terminalSmallStyle);
            if (i == 0)
                GUI.Label(new Rect(row.xMax - 18f, row.y + 12f, 12f, 20f), "▶", terminalLabelStyle);
        }

        GUI.Label(new Rect(rect.x + 16f, rect.yMax - 54f, rect.width - 32f, 20f),
            "使用方向键选择，按回车确认。", terminalSmallStyle);
        GUI.Label(new Rect(rect.x + 16f, rect.yMax - 30f, rect.width - 32f, 20f),
            "↑↓  选择     Enter  确认", terminalSmallStyle);
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

    void DrawOfficeTerminalFiles(Rect rect, OfficeComputer computer, CompanyState company, bool nearShop)
    {
        Rect table = new Rect(rect.x, rect.y, rect.width, Mathf.Min(284f, rect.height * 0.58f));
        Rect detail = new Rect(rect.x, table.yMax + 14f, rect.width * 0.58f - 7f, rect.height - table.height - 14f);
        Rect action = new Rect(detail.xMax + 14f, detail.y, rect.width - detail.width - 14f, detail.height);

        GUI.Box(table, GUIContent.none, terminalBoxStyle);
        GUI.Label(new Rect(table.x + 14f, table.y + 12f, 360f, 24f),
            "委托管理 / COMMISSION FILES", terminalTitleStyle);
        DrawTerminalLine(new Rect(table.x + 12f, table.y + 42f, table.width - 24f, 1f));

        float y = table.y + 48f;
        DrawOfficeTableHeader(table.x + 14f, y, table.width - 28f);
        y += 25f;
        DrawOfficeTaskRow(table.x + 14f, y, table.width - 28f, "001", computer != null ? computer.DemoTaskTitle : "失踪的作业本",
            computer != null ? computer.DemoTaskClient : "家长", GetDemoTaskStatus(computer), "23天后", true);
        y += 30f;
        DrawOfficeTaskRow(table.x + 14f, y, table.width - 28f, "002", "地下室的异响", "宿管", "进行中", "—", false);
        y += 30f;
        DrawOfficeTaskRow(table.x + 14f, y, table.width - 28f, "003", "夜班公交异常", "交通局", "进行中", "—", false);
        y += 30f;
        DrawOfficeTaskRow(table.x + 14f, y, table.width - 28f, "004", "404号房的回声", "学校", "可接受", "17天后", false);
        y += 30f;
        DrawOfficeTaskRow(table.x + 14f, y, table.width - 28f, "005", "废弃工厂的货物", "仓库管理员", "已完成", "—", false);
        y += 30f;
        DrawOfficeTaskRow(table.x + 14f, y, table.width - 28f, "006", "医院地下室的钥匙", "医院", "已锁定", "—", false);
        GUI.Label(new Rect(table.x + table.width * 0.5f - 70f, table.yMax - 28f, 140f, 20f),
            "第 1 页 / 共 2 页   ◀  ▶", terminalSmallStyle);

        GUI.Box(detail, GUIContent.none, terminalBoxStyle);
        GUI.Label(new Rect(detail.x + 14f, detail.y + 10f, detail.width - 28f, 22f),
            "委托详情 / FILE DETAIL [001]", terminalLabelStyle);
        DrawTerminalLine(new Rect(detail.x + 12f, detail.y + 36f, detail.width - 24f, 1f));
        DrawTerminalDetailLine(detail.x + 14f, detail.y + 48f, "委托名称", computer != null ? computer.DemoTaskTitle : "失踪的作业本", "LOST HOMEWORK");
        DrawTerminalDetailLine(detail.x + 14f, detail.y + 70f, "委托人", computer != null ? computer.DemoTaskClient : "家长", "PARENT");
        DrawTerminalDetailLine(detail.x + 14f, detail.y + 92f, "地点", computer != null ? computer.DemoTaskLocation : "西区小学", "WESTSIDE ELEMENTARY");
        DrawTerminalDetailLine(detail.x + 14f, detail.y + 114f, "报酬", $"{(computer != null ? computer.DemoTaskMoneyReward : 120)}G", null);
        DrawTerminalDetailLine(detail.x + 14f, detail.y + 136f, "预计时长", "10 - 15 MIN", null);
        DrawTerminalDetailLine(detail.x + 14f, detail.y + 158f, "结局方式", "全部 / 部分 / 失败", "FULL / PARTIAL / FAILED");
        GUI.Label(new Rect(detail.x + 14f, detail.y + 182f, detail.width - 28f, 38f),
            "备注: 孩子的作业本在放学后的教室中失踪。\n      家长担心里面记录着重要信息。", terminalSmallStyle);

        GUI.Box(action, GUIContent.none, terminalBoxStyle);
        GUI.Label(new Rect(action.x + 14f, action.y + 10f, action.width - 28f, 22f),
            "操作 / ACTION", terminalLabelStyle);
        DrawTerminalLine(new Rect(action.x + 12f, action.y + 36f, action.width - 24f, 1f));

        DrawTerminalPrimaryAction(new Rect(action.x + 18f, action.y + 54f, action.width - 36f, 34f),
            computer, company);

        if (GUI.Button(new Rect(action.x + 18f, action.y + 96f, action.width - 36f, 30f),
            "查看详情 (VIEW DETAIL)", terminalButtonStyle))
            SetOfficeMessage("详情已显示在左侧文件栏。");
        if (GUI.Button(new Rect(action.x + 18f, action.y + 132f, action.width - 36f, 30f),
            "标记完成 (MARK COMPLETE)", terminalButtonStyle))
            SetOfficeMessage("任务完成标记将在后续版本开放。");
        if (GUI.Button(new Rect(action.x + 18f, action.y + 168f, action.width - 36f, 30f),
            "放弃委托 (ABANDON COMMISSION)", terminalButtonStyle))
            SetOfficeMessage("当前演示委托不可放弃。");

        DrawTerminalStatusStrip(rect, company, nearShop);
    }

    void DrawTerminalPrimaryAction(Rect rect, OfficeComputer computer, CompanyState company)
    {
        string label = "› 接受委托 (ACCEPT COMMISSION)";
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
            label = "› 领取结算 (CLAIM SETTLEMENT)";
            enabled = IsLocalHostOrSolo();
            action = () =>
            {
                computer?.ExecuteComputerAction(FindLocalPlayer());
                SetOfficeMessage("结算申请已提交。");
            };
        }
        else if (company.CanShowTutorialAcquisition)
        {
            label = "› 确认收购 (CONFIRM ACQUISITION)";
            enabled = company.CanAffordTutorialAcquisition && IsLocalHostOrSolo();
            action = () =>
            {
                computer?.ExecuteComputerAction(FindLocalPlayer());
                SetOfficeMessage("收购文件已提交。");
            };
        }
        else if (MvpMissionRuntime.HasSelectedTask)
        {
            label = "› 委托已锁定 (COMMISSION LOCKED)";
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
            "001" => "LOST HOMEWORK",
            "002" => "BASEMENT NOISE",
            "003" => "NIGHT BUS ANOMALY",
            "004" => "ECHO FROM ROOM 404",
            "005" => "CARGO FROM ABANDONED FACTORY",
            _ => "KEYS IN THE BASEMENT"
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
            : $"资金 {company.Funds}G   债务 {company.Debt}G   声望 {company.Reputation}   电脑连接 {(nearShop ? "稳定" : "本地")}";
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
        DrawLedgerLine("报酬 / 声望 / 经验", $"{computer.DemoTaskMoneyReward}G / +{computer.DemoTaskReputationReward} / +{computer.DemoTaskExperienceReward}");
        DrawLedgerLine("作业窗口", MvpMissionClock.GetScheduleSummary(computer.DemoTask));
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

        float width = Mathf.Clamp(Screen.width - 36f, 380f, 700f);
        float height = Mathf.Clamp(Screen.height - 96f, 420f, 620f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, 48, width, height);
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

    void DrawBestiaryPanel()
    {
        if (activeBestiary == null) return;

        float width = Mathf.Clamp(Screen.width - 36f, 380f, 680f);
        float height = Mathf.Clamp(Screen.height - 96f, 380f, 580f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, 54, width, height);

        GUILayout.BeginArea(rect, GUIContent.none, panelStyle);
        GUILayout.BeginHorizontal();
        DrawTerminalHeader("怪物图鉴", "EVIDENCE FILE");
        if (GUILayout.Button("关闭", GUILayout.Width(72), GUILayout.Height(30)))
        {
            CloseBestiary();
            GUILayout.EndHorizontal();
            GUILayout.EndArea();
            return;
        }
        GUILayout.EndHorizontal();

        bestiaryScrollPosition = GUILayout.BeginScrollView(bestiaryScrollPosition, false, true);
        bool unlocked = MonsterBestiaryProgress.IsHomeworkDebtCollectorUnlocked;
        DrawTerminalSection(unlocked ? "已归档异常 / VERIFIED" : "未解锁档案 / LOCKED");
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
        TowerMissionManager mission = TowerMissionManager.Instance;
        GUILayout.BeginArea(new Rect(18, 18, panelWidth, 230), GUIContent.none, panelStyle);
        GUILayout.Label(GetMissionObjective(mission), accentStyle);
        float completeness = mission.SyncedCompleteness.Value;
        GUILayout.Label($"密封完整度: {completeness:P0}", completeness < 0.5f ? warningStyle : mutedStyle);
        if (!string.IsNullOrEmpty(missionMessage) && Time.time < missionMessageUntil)
            GUILayout.Label(missionMessage, missionMessage.Contains("警告") ? warningStyle : accentStyle);
        DrawSpectatorHint();
        GUILayout.EndArea();
    }

    static string GetMissionObjective(TowerMissionManager mission)
    {
        switch ((TowerMissionState)mission.SyncedState.Value)
        {
            case TowerMissionState.InProgress: return "目标: 恢复供电，找到「真实海岸」生态柱。";
            case TowerMissionState.ObjectiveSecured: return "目标: 把生态柱送回货舱并拉杆发车——轻拿轻放。";
            case TowerMissionState.Delivered: return "目标: 已交付，即将返回事务所结算。";
            case TowerMissionState.PartialReturn: return "目标: 部分结算，即将返回事务所。";
            case TowerMissionState.Failed: return "目标: 委托失败，返回事务所复盘。";
            default: return "目标: 等待任务状态。";
        }
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
        MissionVanExitPoint van = activeMissionVan;
        if (van == null) return;

        float width = Mathf.Clamp(Screen.width - 36f, 320f, 560f);
        float height = Mathf.Clamp(Screen.height - 112f, 360f, 560f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, 56, width, height);
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

    void DrawStaminaBar()
    {
        if (IsBlockingPanelOpen || VanTransitOverlay.IsActive) return;

        PlayerController localPlayer = FindLocalPlayer();
        if (localPlayer == null) return;

        float stamina = localPlayer.Stamina;
        float maxStamina = localPlayer.MaxStamina;
        if (maxStamina <= 0f) return;

        float normalized = Mathf.Clamp01(stamina / maxStamina);
        bool inMission = TowerMissionManager.Instance != null;
        if (!inMission && normalized >= 0.999f && !localPlayer.IsSprinting && !localPlayer.IsExhausted)
            return;

        float width;
        float x;
        float y;
        if (inMission)
        {
            int slotSize = Mathf.Clamp((Screen.width - 56) / PlayerHotbar.SlotCount, 62, 92);
            int gap = Mathf.Clamp(slotSize / 11, 5, 8);
            width = PlayerHotbar.SlotCount * slotSize + (PlayerHotbar.SlotCount - 1) * gap;
            x = (Screen.width - width) * 0.5f;
            y = Screen.height - 124f;
        }
        else
        {
            width = 220f;
            x = 24f;
            y = Screen.height - 62f;
        }
        float height = 9f;

        bool low = normalized <= 0.24f || localPlayer.IsExhausted;
        GUIStyle textStyle = low ? warningStyle : mutedStyle;
        string state = localPlayer.IsExhausted ? "  /  WINDED" : "";
        GUI.Label(new Rect(x, y - 20f, width * 0.58f, 20f), $"STAMINA{state}", textStyle);
        GUI.Label(new Rect(x + width * 0.58f, y - 20f, width * 0.42f, 20f),
            $"{Mathf.CeilToInt(normalized * 100f)}%", textStyle);

        float fillW = width * normalized;
        Texture2D fillTexture = low ? staminaBarLowFill : staminaBarFill;

        GUI.DrawTexture(new Rect(x - 1f, y - 1f, width + 2f, height + 2f), staminaBarFrame);
        GUI.DrawTexture(new Rect(x, y, width, height), hpBarBg);
        GUI.DrawTexture(new Rect(x, y, fillW, height), fillTexture);

        GUI.color = new Color(0f, 0f, 0f, 0.34f);
        for (int i = 1; i < 4; i++)
        {
            float tickX = x + width * (i / 4f);
            GUI.DrawTexture(new Rect(tickX, y, 1f, height), hpBarBg);
        }
        GUI.color = Color.white;
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
        GUI.Label(new Rect(18, Screen.height - 30, 720, 24), text, mutedStyle);
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
        SettingsOverlay.ForceClose();
        activeComputer = computer;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ComputerCloseupCamera.Enter(computer.transform);
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
        terminalPaperTexture = MakeTexture(new Color(0.67f, 0.61f, 0.47f, 0.96f));
        terminalBoxTexture = MakeTexture(new Color(0.53f, 0.49f, 0.38f, 0.18f));
        terminalSelectedTexture = MakeTexture(new Color(0.42f, 0.37f, 0.27f, 0.28f));
        terminalLineTexture = MakeTexture(new Color(0.18f, 0.17f, 0.13f, 0.58f));
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
            normal = { textColor = new Color(0.10f, 0.095f, 0.075f, 1f) },
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
            normal = { textColor = new Color(0.19f, 0.17f, 0.13f, 0.86f) }
        };
        terminalSmallStyle = new GUIStyle(terminalLabelStyle)
        {
            fontSize = 12,
            normal = { textColor = new Color(0.12f, 0.11f, 0.085f, 0.96f) },
            wordWrap = true
        };
        terminalButtonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft,
            normal = { background = terminalBoxTexture, textColor = new Color(0.10f, 0.095f, 0.075f, 1f) },
            hover = { background = terminalSelectedTexture, textColor = new Color(0.05f, 0.045f, 0.035f, 1f) },
            active = { background = terminalLineTexture, textColor = new Color(0.05f, 0.045f, 0.035f, 1f) },
            padding = new RectOffset(12, 8, 4, 4)
        };
        terminalSelectedButtonStyle = new GUIStyle(terminalBoxStyle)
        {
            normal = { background = terminalSelectedTexture }
        };

        EnsureCrtTextures();

        if (hpBarBg == null || hpBarFill == null || staminaBarFill == null || staminaBarLowFill == null || staminaBarFrame == null)
        {
            hpBarBg = MakeTexture(new Color(0.055f, 0.060f, 0.055f, 0.90f));
            hpBarFill = MakeTexture(BlackCommissionUiTheme.RustWarning);
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
