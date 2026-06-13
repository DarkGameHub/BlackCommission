using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class QuickNetworkUI : MonoBehaviour
{
    enum MenuState { Main, JoinInput, Connecting, HostWaiting }

    Texture2D[] charUniformTex;
    Texture2D[] charVestTex;
    Texture2D[] charHelmetTex;
    Texture2D charSlotBgTex;
    Texture2D charSlotSelectedTex;

    void EnsureCharacterTextures()
    {
        if (charUniformTex != null) return;
        int n = PlayerCharacterPalette.Count;
        charUniformTex = new Texture2D[n];
        charVestTex = new Texture2D[n];
        charHelmetTex = new Texture2D[n];
        for (int i = 0; i < n; i++)
        {
            var c = PlayerCharacterPalette.Get(i);
            charUniformTex[i] = MakeTex(c.uniform);
            charVestTex[i] = MakeTex(c.vest);
            charHelmetTex[i] = MakeTex(c.helmet);
        }
        charSlotBgTex = MakeTex(BlackCommissionUiTheme.ConcretePanel);
        charSlotSelectedTex = MakeTex(new Color(0.260f, 0.560f, 0.250f, 0.72f));
    }

    GUIStyle titleStyle;
    GUIStyle subtitleStyle;
    GUIStyle hostBtnStyle;
    GUIStyle joinBtnStyle;
    GUIStyle hintStyle;
    GUIStyle statusStyle;
    GUIStyle codeStyle;
    GUIStyle codeInputStyle;
    GUIStyle versionStyle;
    Texture2D screenTex;
    Texture2D panelTex;
    Texture2D panelBorderTex;
    Texture2D hostBtnNormal;
    Texture2D hostBtnHover;
    Texture2D hostBtnActive;
    Texture2D joinBtnNormal;
    Texture2D joinBtnHover;
    Texture2D joinBtnActive;
    Texture2D fieldTex;
    Texture2D debtSlashTex;
    bool stylesReady;
    bool settingsOpen;

    MenuState state = MenuState.Main;
    string joinCode = "";
    string statusMessage = "";
    string hostJoinCode = "";
    float statusMessageUntil;

    // Direct connect fallback
    string connectAddress = "127.0.0.1";
    string connectPort = "7778";
    bool showDirectConnect;
    ushort lastHostPort = 7778;

    void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        screenTex = MakeTex(new Color(0.000f, 0.000f, 0.000f, 0.70f));
        panelTex = MakeTex(BlackCommissionUiTheme.ConcreteBlack);
        panelBorderTex = MakeTex(BlackCommissionUiTheme.MilitaryGreenDim);
        fieldTex = MakeTex(new Color(0.025f, 0.030f, 0.027f, 1f));
        debtSlashTex = MakeTex(BlackCommissionUiTheme.Rust);

        hostBtnNormal = MakeTex(BlackCommissionUiTheme.MilitaryGreenDark);
        hostBtnHover = MakeTex(BlackCommissionUiTheme.MilitaryGreen);
        hostBtnActive = MakeTex(new Color(0.060f, 0.080f, 0.055f, 1f));
        joinBtnNormal = MakeTex(BlackCommissionUiTheme.ConcreteRaised);
        joinBtnHover = MakeTex(new Color(0.130f, 0.145f, 0.122f, 1f));
        joinBtnActive = MakeTex(BlackCommissionUiTheme.ConcretePanel);

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = BlackCommissionUiTheme.CrtGreen },
            padding = new RectOffset(0, 0, 0, 0)
        };

        subtitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = BlackCommissionUiTheme.OldPaper },
            padding = new RectOffset(0, 0, 2, 8)
        };

        hostBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 18, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = hostBtnNormal, textColor = BlackCommissionUiTheme.CrtGreen },
            hover = { background = hostBtnHover, textColor = BlackCommissionUiTheme.CrtGreen },
            active = { background = hostBtnActive, textColor = BlackCommissionUiTheme.OldPaper },
            border = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(12, 12, 10, 10)
        };

        joinBtnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = joinBtnNormal, textColor = BlackCommissionUiTheme.Text },
            hover = { background = joinBtnHover, textColor = BlackCommissionUiTheme.CrtGreen },
            active = { background = joinBtnActive, textColor = BlackCommissionUiTheme.OldPaper },
            border = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(12, 12, 8, 8)
        };

        hintStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            normal = { textColor = BlackCommissionUiTheme.MutedText },
            padding = new RectOffset(8, 8, 4, 4)
        };

        statusStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = BlackCommissionUiTheme.CrtGreen },
            padding = new RectOffset(10, 10, 6, 6)
        };

        codeStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 36, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = BlackCommissionUiTheme.CrtGreen }
        };

        codeInputStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 22, fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { background = fieldTex, textColor = BlackCommissionUiTheme.CrtGreen },
            focused = { background = fieldTex, textColor = BlackCommissionUiTheme.OldPaper },
            padding = new RectOffset(12, 12, 8, 8)
        };

        versionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = BlackCommissionUiTheme.MutedText }
        };

        MvpFontProvider.ApplyToStyle(titleStyle);
        MvpFontProvider.ApplyToStyle(subtitleStyle);
        MvpFontProvider.ApplyToStyle(hostBtnStyle);
        MvpFontProvider.ApplyToStyle(joinBtnStyle);
        MvpFontProvider.ApplyToStyle(hintStyle);
        MvpFontProvider.ApplyToStyle(statusStyle);
        MvpFontProvider.ApplyToStyle(codeStyle);
        MvpFontProvider.ApplyToStyle(codeInputStyle);
        MvpFontProvider.ApplyToStyle(versionStyle);
    }

    static Texture2D MakeTex(Color col)
    {
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, col);
        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (NetworkManager.Singleton == null) return;
        // New UGUI-based menu (MainMenuUI) supersedes this OnGUI panel when present.
        if (Object.FindFirstObjectByType<MainMenuUI>() != null) return;
        InitStyles();

        if (NetworkManager.Singleton.IsListening)
        {
            DrawConnectedStatus();

            if (state == MenuState.HostWaiting && !string.IsNullOrEmpty(hostJoinCode))
                DrawHostJoinCodeOverlay();

            return;
        }

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), screenTex);

        switch (state)
        {
            case MenuState.Main: DrawMainMenu(); break;
            case MenuState.JoinInput: DrawJoinInput(); break;
            case MenuState.Connecting: DrawConnecting(); break;
        }

        if (settingsOpen) DrawMenuSettings();

        GUI.Label(new Rect(0, Screen.height - 28, Screen.width, 24), "v0.1 MVP", versionStyle);
    }

    void DrawConnectedStatus()
    {
        int clients = NetworkManager.Singleton.ConnectedClientsIds.Count;
        string role = NetworkManager.Singleton.IsHost ? MvpLocale.T("host") : MvpLocale.T("client");
        GUI.Label(new Rect(8, 8, 320, 28), MvpLocale.T("connected_status", role, clients), statusStyle);
    }

    void DrawHostJoinCodeOverlay()
    {
        float pw = 320, ph = 120;
        Rect panel = new Rect((Screen.width - pw) * 0.5f, 52, pw, ph);
        GUI.DrawTexture(new Rect(panel.x - 2, panel.y - 2, panel.width + 4, panel.height + 4), panelBorderTex);
        GUI.DrawTexture(panel, panelTex);

        GUI.Label(new Rect(panel.x, panel.y + 12, panel.width, 22), MvpLocale.T("room_code_share"), subtitleStyle);
        GUI.Label(new Rect(panel.x, panel.y + 38, panel.width, 48), hostJoinCode, codeStyle);
        GUI.Label(new Rect(panel.x, panel.y + ph - 28, panel.width, 20), MvpLocale.T("room_code_join_hint"), hintStyle);

        if (GUI.Button(new Rect(panel.x + panel.width - 68, panel.y + 8, 60, 24), MvpLocale.T("hide"), joinBtnStyle))
            hostJoinCode = "";
    }

    void DrawMainMenu()
    {
        float pw = Mathf.Min(420f, Mathf.Max(340f, Screen.width - 56f));
        float ph = Mathf.Min(showDirectConnect ? 520f : 420f, Screen.height - 32f);
        float px = (Screen.width - pw) * 0.5f;
        float py = (Screen.height - ph) * 0.5f;

        GUI.DrawTexture(new Rect(px - 2, py - 2, pw + 4, ph + 4), panelBorderTex);
        GUI.DrawTexture(new Rect(px, py, pw, ph), panelTex);

        float cx = px + 24;
        float bw = pw - 48;
        float cy = py + 18;

        GUIStyle titleLeft = new GUIStyle(titleStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 24
        };
        GUIStyle subtitleLeft = new GUIStyle(subtitleStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            padding = new RectOffset(0, 0, 0, 0)
        };
        GUIStyle smallLabel = new GUIStyle(hintStyle)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 11,
            padding = new RectOffset(0, 0, 0, 0)
        };
        GUIStyle stampStyle = new GUIStyle(hintStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 10,
            fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.RustWarning },
            padding = new RectOffset(0, 0, 0, 0)
        };

        GUI.DrawTexture(new Rect(cx, cy, bw, 2), panelBorderTex);
        cy += 9;
        GUI.Label(new Rect(cx, cy, bw - 88, 32), "BLACK COMMISSION", titleLeft);

        Rect stamp = new Rect(cx + bw - 80, cy + 4, 78, 24);
        GUI.DrawTexture(stamp, fieldTex);
        GUI.DrawTexture(new Rect(stamp.x, stamp.y, stamp.width, 2), debtSlashTex);
        GUI.DrawTexture(new Rect(stamp.x, stamp.yMax - 2, stamp.width, 2), debtSlashTex);
        GUI.Label(stamp, "DEBT", stampStyle);
        cy += 34;

        GUI.Label(new Rect(cx, cy, bw, 20), MvpLocale.T("subtitle"), subtitleLeft);
        cy += 26;

        GUI.DrawTexture(new Rect(cx, cy, bw, 1), panelBorderTex);
        cy += 12;

        GUI.Label(new Rect(cx, cy, bw, 16), "UNIFORM FILE", smallLabel);
        cy += 18;
        GUI.DrawTexture(new Rect(cx, cy - 2, bw, 46), fieldTex);
        DrawCharacterSelector(cx, cy, bw);
        cy += 56;

        GUI.Label(new Rect(cx, cy, bw, 16), "DISPATCH", smallLabel);
        cy += 18;

        if (GUI.Button(new Rect(cx, cy, bw, 46), MvpLocale.T("create_office"), hostBtnStyle))
            StartHost();
        cy += 54;

        if (GUI.Button(new Rect(cx, cy, bw, 40), MvpLocale.T("join_office"), joinBtnStyle))
            state = MenuState.JoinInput;
        cy += 50;

        GUI.DrawTexture(new Rect(cx, cy, bw, 1), panelBorderTex);
        cy += 10;

        if (!string.IsNullOrEmpty(statusMessage) && Time.unscaledTime < statusMessageUntil)
        {
            GUIStyle msgStyle = new GUIStyle(hintStyle) { normal = { textColor = BlackCommissionUiTheme.RustWarning } };
            GUI.Label(new Rect(cx, cy, bw, 32), statusMessage, msgStyle);
            cy += 34;
        }
        else
        {
            GUI.Label(new Rect(cx, cy, bw, 32), MvpLocale.T("create_hint"), hintStyle);
            cy += 34;
        }

        // Settings button (gear symbol)
        if (GUI.Button(new Rect(cx, cy, bw * 0.48f, 24), "⚙ " + MvpLocale.T("game"), hintStyle))
            settingsOpen = !settingsOpen;

        // Direct connect toggle
        if (GUI.Button(new Rect(cx + bw * 0.52f, cy, bw * 0.48f, 24), showDirectConnect ? MvpLocale.T("hide_direct") : MvpLocale.T("show_direct"), hintStyle))
            showDirectConnect = !showDirectConnect;
        cy += 28;

        if (showDirectConnect)
            DrawDirectConnect(cx, cy, bw);
    }

    void DrawMenuSettings()
    {
        float pw = 360, ph = 240;
        float px = (Screen.width - pw) * 0.5f;
        float py = (Screen.height + 20) * 0.5f; // below center

        GUI.DrawTexture(new Rect(px - 2, py - 2, pw + 4, ph + 4), panelBorderTex);
        GUI.DrawTexture(new Rect(px, py, pw, ph), panelTex);

        float cx = px + 24;
        float bw = pw - 48;
        float cy = py + 16;

        GUI.Label(new Rect(cx, cy, bw - 40, 22), MvpLocale.T("game"), hintStyle);
        if (GUI.Button(new Rect(px + pw - 52, py + 8, 40, 22), "✕", joinBtnStyle))
            settingsOpen = false;
        cy += 28;

        // Index order must match MvpLocale: 0 = English (default), 1 = 中文.
        string[] langLabels = { "English", "中文 (简体)" };
        GUI.Label(new Rect(cx, cy, bw, 18), MvpLocale.T("language", langLabels[MvpHud.LanguageIndexStatic]), hintStyle);
        cy += 20;
        int newLang = GUI.SelectionGrid((Rect)(new Rect(cx, cy, bw, 26)), MvpHud.LanguageIndexStatic, langLabels, 2, joinBtnStyle);
        if (newLang != MvpHud.LanguageIndexStatic) MvpHud.LanguageIndexStatic = newLang;
        cy += 32;

        GUI.Label(new Rect(cx, cy, bw, 18), MvpLocale.T("master_volume", $"{MvpHud.MasterVolumeStatic:0.0}"), hintStyle);
        cy += 20;
        MvpHud.MasterVolumeStatic = GUI.HorizontalSlider(new Rect(cx, cy, bw, 18), MvpHud.MasterVolumeStatic, 0f, 1f);
        cy += 28;

        GUI.Label(new Rect(cx, cy, bw, 18), MvpLocale.T("h_sensitivity", $"{PlayerCameraController.HorizontalSensitivity:0.0}"), hintStyle);
        cy += 20;
        PlayerCameraController.HorizontalSensitivity = GUI.HorizontalSlider(new Rect(cx, cy, bw, 18), PlayerCameraController.HorizontalSensitivity, 0.25f, 8f);
    }

    void DrawDirectConnect(float cx, float cy, float bw)
    {
        GUIStyle fieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 13,
            alignment = TextAnchor.MiddleLeft,
            normal = { background = fieldTex, textColor = BlackCommissionUiTheme.Text },
            focused = { background = fieldTex, textColor = BlackCommissionUiTheme.OldPaper },
            padding = new RectOffset(6, 6, 4, 4)
        };

        GUI.Label(new Rect(cx, cy, 40, 22), MvpLocale.T("address"), hintStyle);
        connectAddress = GUI.TextField(new Rect(cx + 40, cy, bw - 120, 26), connectAddress, fieldStyle);
        connectPort = GUI.TextField(new Rect(cx + bw - 72, cy, 72, 26), connectPort, fieldStyle);
        cy += 32;

        if (GUI.Button(new Rect(cx, cy, (bw - 8) * 0.5f, 34), MvpLocale.T("direct_create"), joinBtnStyle))
            StartHostDirect();
        if (GUI.Button(new Rect(cx + (bw - 8) * 0.5f + 8, cy, (bw - 8) * 0.5f, 34), MvpLocale.T("direct_join"), joinBtnStyle))
            StartClientDirect();
    }

    void DrawJoinInput()
    {
        float pw = 380, ph = 260;
        float px = (Screen.width - pw) * 0.5f;
        float py = (Screen.height - ph) * 0.5f;

        GUI.DrawTexture(new Rect(px - 2, py - 2, pw + 4, ph + 4), panelBorderTex);
        GUI.DrawTexture(new Rect(px, py, pw, ph), panelTex);

        float cx = px + 28;
        float bw = pw - 56;
        float cy = py + 24;

        GUI.Label(new Rect(cx, cy, bw, 28), MvpLocale.T("enter_code"), titleStyle);
        cy += 42;

        GUI.Label(new Rect(cx, cy, bw, 20), MvpLocale.T("ask_host_code"), subtitleStyle);
        cy += 30;

        GUI.SetNextControlName("JoinCodeField");
        joinCode = GUI.TextField(new Rect(cx + (bw - 200) * 0.5f, cy, 200, 44), joinCode, 6, codeInputStyle);
        if (joinCode != null) joinCode = joinCode.ToUpper().Trim();
        cy += 56;

        bool codeReady = joinCode != null && joinCode.Length >= 4;
        GUI.enabled = codeReady;
        if (GUI.Button(new Rect(cx, cy, bw, 42), MvpLocale.T("join"), hostBtnStyle))
            StartJoin();
        GUI.enabled = true;
        cy += 52;

        if (GUI.Button(new Rect(cx, cy, bw, 28), MvpLocale.T("back"), joinBtnStyle))
        {
            state = MenuState.Main;
            joinCode = "";
        }

        if (!string.IsNullOrEmpty(statusMessage) && Time.unscaledTime < statusMessageUntil)
        {
            GUIStyle msgStyle = new GUIStyle(hintStyle) { normal = { textColor = BlackCommissionUiTheme.RustWarning } };
            GUI.Label(new Rect(cx, cy + 32, bw, 28), statusMessage, msgStyle);
        }

        GUI.FocusControl("JoinCodeField");
    }

    void DrawConnecting()
    {
        float pw = 300, ph = 120;
        float px = (Screen.width - pw) * 0.5f;
        float py = (Screen.height - ph) * 0.5f;

        GUI.DrawTexture(new Rect(px - 2, py - 2, pw + 4, ph + 4), panelBorderTex);
        GUI.DrawTexture(new Rect(px, py, pw, ph), panelTex);

        string dots = new string('.', (int)(Time.unscaledTime * 2f) % 4);
        GUI.Label(new Rect(px, py + 30, pw, 30), MvpLocale.T("connecting") + dots, subtitleStyle);
        GUI.Label(new Rect(px, py + 60, pw, 24), MvpLocale.T("please_wait"), hintStyle);
    }

    void DrawCharacterSelector(float cx, float cy, float bw)
    {
        int current = PlayerCharacterPalette.SavedIndex;
        float slotSize = Mathf.Min((bw - (PlayerCharacterPalette.Count - 1) * 6f) / PlayerCharacterPalette.Count, 48f);
        float totalWidth = PlayerCharacterPalette.Count * slotSize + (PlayerCharacterPalette.Count - 1) * 6f;
        float startX = cx + (bw - totalWidth) * 0.5f;

        GUI.Label(new Rect(cx, cy, bw, 18), PlayerCharacterPalette.Get(current).label, hintStyle);
        cy += 20;

        EnsureCharacterTextures();
        for (int i = 0; i < PlayerCharacterPalette.Count; i++)
        {
            Rect slot = new Rect(startX + i * (slotSize + 6f), cy, slotSize, slotSize * 0.7f);
            GUI.DrawTexture(slot, i == current ? charSlotSelectedTex : charSlotBgTex);

            float uniformH = slot.height * 0.55f;
            float vestH = slot.height * 0.30f;
            GUI.DrawTexture(new Rect(slot.x + 2, slot.y + 2, slot.width - 4, uniformH), charUniformTex[i]);
            GUI.DrawTexture(new Rect(slot.x + 2, slot.y + 2 + uniformH + 1, slot.width - 4, vestH), charVestTex[i]);
            GUI.DrawTexture(new Rect(slot.x + slot.width * 0.3f, slot.y + slot.height - 6, slot.width * 0.4f, 4), charHelmetTex[i]);

            if (GUI.Button(slot, GUIContent.none, GUIStyle.none))
                PlayerCharacterPalette.SavedIndex = i;
        }
    }

    // ─── Connection Methods ───

    void StartHost()
    {
        if (ConnectionManager.Instance != null && ConnectionManager.Instance.ServicesReady)
        {
            state = MenuState.Connecting;
            ConnectionManager.Instance.OnJoinCodeReady += HandleJoinCodeReady;
            ConnectionManager.Instance.OnError += HandleError;
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
            state = MenuState.Connecting;
            ConnectionManager.Instance.OnConnected += HandleConnected;
            ConnectionManager.Instance.OnError += HandleJoinError;
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
        state = MenuState.HostWaiting;
        ConnectionManager.Instance.OnJoinCodeReady -= HandleJoinCodeReady;
        ConnectionManager.Instance.OnError -= HandleError;
    }

    void HandleConnected()
    {
        state = MenuState.Main;
        joinCode = "";
        ConnectionManager.Instance.OnConnected -= HandleConnected;
        ConnectionManager.Instance.OnError -= HandleJoinError;
    }

    void HandleError(string error)
    {
        state = MenuState.Main;
        SetStatus(error);
        ConnectionManager.Instance.OnJoinCodeReady -= HandleJoinCodeReady;
        ConnectionManager.Instance.OnError -= HandleError;
    }

    void HandleJoinError(string error)
    {
        state = MenuState.JoinInput;
        SetStatus(error);
        ConnectionManager.Instance.OnConnected -= HandleConnected;
        ConnectionManager.Instance.OnError -= HandleJoinError;
    }

    void SetStatus(string msg)
    {
        statusMessage = msg;
        statusMessageUntil = Time.unscaledTime + 5f;
    }

    // ─── Direct Connect Fallback ───

    void StartHostDirect()
    {
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

            if (NetworkManager.Singleton.StartHost())
            {
                connectAddress = "127.0.0.1";
                SetStatus(MvpLocale.T("direct_host_started", connectAddress, port));
                return;
            }

            NetworkManager.Singleton.Shutdown();
        }

        SetStatus(MvpLocale.T("host_port_busy"));
    }

    void StartClientDirect()
    {
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
}
