using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// The single, shared settings panel for the whole game. Self-bootstraps as a
/// DontDestroyOnLoad IMGUI singleton so it works identically in the main menu, the HQ,
/// and every mission scene. Both the menu (MainMenuUI) and the in-game HUD (MvpHud)
/// open it via Open()/Toggle(). ESC handling: MvpHud owns it where an MvpHud exists (HQ —
/// its ESC chain closes the office panels first); everywhere else (mission maps) this
/// overlay listens for ESC itself, so brightness/gamma/sensitivity and quit stay reachable
/// mid-mission.
/// </summary>
public class SettingsOverlay : MonoBehaviour
{
    static SettingsOverlay instance;

    public static bool IsOpen { get; private set; }

    const float QuitToMenuConfirmWindow = 2.5f;

    Vector2 scroll;
    bool stylesReady;
    float quitToMenuArmedAt = float.NegativeInfinity;
    GUIStyle panelStyle, titleStyle, labelStyle, accentStyle, buttonStyle, headerTextStyle;
    GUIStyle sliderRailStyle, sliderThumbStyle;
    Texture2D panelTex, headerBandTex, railTex, thumbTex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap() => EnsureInstance();

    public static void EnsureInstance()
    {
        if (instance != null) return;
        var go = new GameObject("AS_SettingsOverlay");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SettingsOverlay>();
    }

    public static void Open()
    {
        EnsureInstance();
        instance.quitToMenuArmedAt = float.NegativeInfinity;
        IsOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Close and restore the gameplay cursor only when a local player exists.</summary>
    public static void Close()
    {
        IsOpen = false;
        if (InGameplay())
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        PlayerPrefs.Save();
    }

    /// <summary>Close without touching the cursor — used when another panel takes over.</summary>
    public static void ForceClose()
    {
        if (!IsOpen) return;
        IsOpen = false;
        PlayerPrefs.Save();
    }

    public static void Toggle()
    {
        if (IsOpen) Close();
        else Open();
    }

    static bool InGameplay()
    {
        var ctrls = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var c in ctrls)
            if (c.IsOwner) return true;
        return false;
    }

    void Update()
    {
        // HQ: MvpHud owns ESC (it closes the office panels first, then opens this overlay).
        // Menu: MainMenuUI owns ESC. Mission maps have neither — handle ESC here.
        if (MvpHud.Present || MainMenuUI.IsGameplayInputBlockedByMenu) return;

        var keyboard = Keyboard.current;
        if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame) return;

        if (IsOpen) { Close(); return; }
        if (VanTransitOverlay.IsActive) return;               // seated transit owns the screen
        if (SettlementCardOverlay.IsCardVisible) return;      // card folds itself on ESC
        if (!InGameplay()) return;
        Open();
    }

    void EnsureStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        // 偏好登记表 (main-menu spec sub-page identity): aged-paper form, civic-teal header
        // band, ink text, ink-rail sliders with a teal caliper thumb — a filled-in municipal
        // form, not a system panel.
        Color ink = new Color(0.10f, 0.095f, 0.075f, 1f);
        Color inkSoft = new Color(0.19f, 0.17f, 0.13f, 0.9f);

        panelTex = new Texture2D(1, 1);
        panelTex.SetPixel(0, 0, BlackCommissionUiTheme.OldPaper);
        panelTex.Apply();

        headerBandTex = new Texture2D(1, 1);
        headerBandTex.SetPixel(0, 0, BlackCommissionUiTheme.MilitaryGreen);
        headerBandTex.Apply();

        railTex = new Texture2D(1, 1);
        railTex.SetPixel(0, 0, new Color(0.19f, 0.17f, 0.13f, 0.30f));
        railTex.Apply();

        thumbTex = new Texture2D(1, 1);
        thumbTex.SetPixel(0, 0, BlackCommissionUiTheme.MilitaryGreen);
        thumbTex.Apply();

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTex },
            padding = new RectOffset(18, 18, 14, 14)
        };
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20, fontStyle = FontStyle.Bold,
            normal = { textColor = ink }, wordWrap = true
        };
        headerTextStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft,
            normal = { textColor = BlackCommissionUiTheme.OldPaper }
        };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15, normal = { textColor = ink }, wordWrap = true
        };
        accentStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold, normal = { textColor = inkSoft }
        };
        sliderRailStyle = new GUIStyle
        {
            normal = { background = railTex },
            fixedHeight = 8f,
            margin = new RectOffset(0, 0, 8, 8)
        };
        sliderThumbStyle = new GUIStyle
        {
            normal = { background = thumbTex },
            fixedWidth = 12f,
            fixedHeight = 18f
        };
        buttonStyle = BlackCommissionUiTheme.ButtonStyle(15);

        MvpFontProvider.ApplyToStyle(panelStyle);
        MvpFontProvider.ApplyToStyle(titleStyle);
        MvpFontProvider.ApplyToStyle(headerTextStyle);
        MvpFontProvider.ApplyToStyle(labelStyle);
        MvpFontProvider.ApplyToStyle(accentStyle);
        MvpFontProvider.ApplyToStyle(buttonStyle);
    }

    float InkSlider(float value, float min, float max)
        => GUILayout.HorizontalSlider(value, min, max, sliderRailStyle, sliderThumbStyle);

    // 登记表勾选栏: label + a stamped 是/否 cell (touch target is the cell itself).
    bool InkToggle(bool value, string label)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(label, labelStyle);
        if (GUILayout.Button(MvpLocale.T(value ? "toggle_yes" : "toggle_no"),
                buttonStyle, GUILayout.Width(56f), GUILayout.Height(26f)))
            value = !value;
        GUILayout.EndHorizontal();
        return value;
    }

    void OnGUI()
    {
        if (!IsOpen) return;
        EnsureStyles();
        BlackCommissionUiTheme.ApplyButtonSkin(buttonStyle);

        float width = Mathf.Clamp(Screen.width - 36f, 360f, 620f);
        float height = Mathf.Clamp(Screen.height - 80f, 420f, 680f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

        // Civic-teal header band over the paper form (盖章公文卡 grammar).
        GUI.DrawTexture(new Rect(rect.x - 3f, rect.y - 3f, rect.width + 6f, rect.height + 6f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.Shadow));
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 46f), headerBandTex);
        GUI.Label(new Rect(rect.x + 18f, rect.y + 6f, rect.width - 120f, 26f),
            MvpLocale.Pick("BLACK COMMISSION OFFICE  ·  PREFERENCE FORM", "黑色委托事务所  ·  偏好登记表"), headerTextStyle);
        GUI.Label(new Rect(rect.x + 18f, rect.y + 26f, rect.width - 120f, 16f),
            "FORM BC-05", headerTextStyle);
        if (GUI.Button(new Rect(rect.xMax - 96f, rect.y + 8f, 80f, 30f), MvpLocale.T("resume"), buttonStyle))
        {
            Close();
            return;
        }

        GUILayout.BeginArea(new Rect(rect.x, rect.y + 50f, rect.width, rect.height - 50f), GUIContent.none, panelStyle);
        scroll = GUILayout.BeginScrollView(scroll, false, true);

        // ─── Game ───
        GUILayout.Space(8);
        GUILayout.Label(MvpLocale.T("game"), accentStyle);
        string[] languageLabels = MvpLocale.IsEnglish
            ? new[] { "English", "Chinese (Simplified)" }
            : new[] { "英文", "中文（简体）" };
        GUILayout.Label(MvpLocale.T("language", languageLabels[Mathf.Clamp(MvpHud.LanguageIndexStatic, 0, 1)]), labelStyle);
        int selectedLanguage = GUILayout.SelectionGrid(MvpHud.LanguageIndexStatic, languageLabels, 2);
        if (selectedLanguage != MvpHud.LanguageIndexStatic)
            MvpHud.LanguageIndexStatic = selectedLanguage;
        GUILayout.Label(MvpLocale.T("master_volume", $"{MvpHud.MasterVolumeStatic:0.00}"), labelStyle);
        MvpHud.MasterVolumeStatic = InkSlider(MvpHud.MasterVolumeStatic, 0f, 1f);

        // ─── Display ───
        GUILayout.Space(10);
        GUILayout.Label(MvpLocale.T("display"), accentStyle);
        GUILayout.Label(MvpLocale.T("brightness", $"{DisplaySettings.Brightness:0.0}"), labelStyle);
        float newBrightness = InkSlider(DisplaySettings.Brightness,
            DisplaySettings.MinBrightness, DisplaySettings.MaxBrightness);
        if (!Mathf.Approximately(newBrightness, DisplaySettings.Brightness))
        {
            DisplaySettings.Brightness = newBrightness;
            BrightnessController.Apply();
        }
        GUILayout.Label(MvpLocale.T("gamma", $"{DisplaySettings.Gamma:0.00}"), labelStyle);
        float newGamma = InkSlider(DisplaySettings.Gamma,
            DisplaySettings.MinGamma, DisplaySettings.MaxGamma);
        if (!Mathf.Approximately(newGamma, DisplaySettings.Gamma))
        {
            DisplaySettings.Gamma = newGamma;
            BrightnessController.Apply();
        }
        bool newFullscreen = InkToggle(DisplaySettings.Fullscreen, MvpLocale.T("fullscreen"));
        if (newFullscreen != DisplaySettings.Fullscreen)
        {
            DisplaySettings.Fullscreen = newFullscreen;
            DisplaySettings.ApplyFullscreen();
        }
        DrawQualitySelector();

        // ─── Camera ───
        GUILayout.Space(10);
        GUILayout.Label(MvpLocale.T("camera"), accentStyle);
        GUILayout.Label(MvpLocale.T("h_sensitivity", $"{PlayerCameraController.HorizontalSensitivity:0.00}"), labelStyle);
        PlayerCameraController.HorizontalSensitivity = InkSlider(PlayerCameraController.HorizontalSensitivity, 0.25f, 8f);
        GUILayout.Label(MvpLocale.T("v_sensitivity", $"{PlayerCameraController.VerticalSensitivity:0.00}"), labelStyle);
        PlayerCameraController.VerticalSensitivity = InkSlider(PlayerCameraController.VerticalSensitivity, 0.25f, 8f);
        PlayerCameraController.InvertY = InkToggle(PlayerCameraController.InvertY, MvpLocale.T("invert_y"));
        GUILayout.Label(MvpLocale.T("fov", $"{PlayerCameraController.FieldOfView:0}"), labelStyle);
        PlayerCameraController.FieldOfView = InkSlider(PlayerCameraController.FieldOfView, 55f, 95f);

        // ─── Voice ───
        GUILayout.Space(10);
        GUILayout.Label(MvpLocale.T("voice"), accentStyle);
        ProximityVoiceChat.VoiceEnabled = InkToggle(ProximityVoiceChat.VoiceEnabled, MvpLocale.T("voice_default_on"));
        ProximityVoiceChat.Muted = InkToggle(ProximityVoiceChat.Muted, MvpLocale.T("mute_self"));
        ProximityVoiceChat.PushToTalk = InkToggle(ProximityVoiceChat.PushToTalk, MvpLocale.T("push_to_talk"));
        DrawMicrophoneSelector();
        GUILayout.Label(MvpLocale.T("mic_gain", $"{ProximityVoiceChat.MicGain:0.0}"), labelStyle);
        ProximityVoiceChat.MicGain = InkSlider(ProximityVoiceChat.MicGain, 0f, 2f);
        GUILayout.Label(MvpLocale.T("voice_volume", $"{ProximityVoiceChat.OutputVolume:0.0}"), labelStyle);
        ProximityVoiceChat.OutputVolume = InkSlider(ProximityVoiceChat.OutputVolume, 0f, 2f);
        GUILayout.Label(MvpLocale.T("voice_distance", $"{ProximityVoiceChat.MaxDistance:0}"), labelStyle);
        ProximityVoiceChat.MaxDistance = InkSlider(ProximityVoiceChat.MaxDistance, 4f, 40f);

        // ─── Accessibility (辅助功能) — the switches every UX spec promised this form ───
        GUILayout.Space(10);
        GUILayout.Label(MvpLocale.T("a11y_section"), accentStyle);
        AccessibilityPrefs.ReducedMotion = InkToggle(AccessibilityPrefs.ReducedMotion, MvpLocale.T("a11y_reduced_motion"));
        AccessibilityPrefs.InspectToggleMode = InkToggle(AccessibilityPrefs.InspectToggleMode, MvpLocale.T("a11y_inspect_toggle"));
        AccessibilityPrefs.ReducedFlash = InkToggle(AccessibilityPrefs.ReducedFlash, MvpLocale.T("a11y_reduced_flash"));

        // ─── Buttons ───
        GUILayout.Space(14);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(MvpLocale.T("reset_defaults"), GUILayout.Height(32)))
            ResetDefaults();
        // Quit-to-menu is two-press (host quitting ends the session for everyone).
        bool quitToMenuArmed = Time.unscaledTime - quitToMenuArmedAt < QuitToMenuConfirmWindow;
        if (GUILayout.Button(MvpLocale.T(quitToMenuArmed ? "quit_to_menu_confirm" : "quit_to_menu"),
                GUILayout.Height(32)))
        {
            if (quitToMenuArmed) { QuitToMainMenu(); return; }
            quitToMenuArmedAt = Time.unscaledTime;
        }
        if (GUILayout.Button(MvpLocale.T("quit_game"), GUILayout.Height(32)))
            QuitGame();
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }

    void DrawQualitySelector()
    {
        string[] levels = QualitySettings.names;
        if (levels == null || levels.Length == 0) return;
        int current = DisplaySettings.QualityLevel;
        GUILayout.Label(MvpLocale.T("quality", levels[Mathf.Clamp(current, 0, levels.Length - 1)]), labelStyle);
        int selected = GUILayout.SelectionGrid(current, levels, Mathf.Min(levels.Length, 3));
        if (selected != current)
        {
            DisplaySettings.QualityLevel = selected;
            DisplaySettings.ApplyQuality();
        }
    }

    void DrawMicrophoneSelector()
    {
        string[] devices = Microphone.devices;
        bool hasDevices = devices != null && devices.Length > 0;
        GUILayout.Label(MvpLocale.T("mic_device", ProximityVoiceChat.SelectedMicrophoneDeviceName), labelStyle);
        GUILayout.BeginHorizontal();
        GUI.enabled = hasDevices;
        if (GUILayout.Button(MvpLocale.T("prev"), GUILayout.Height(28)) && hasDevices)
        {
            int count = devices.Length;
            ProximityVoiceChat.MicrophoneDeviceIndex = (ProximityVoiceChat.MicrophoneDeviceIndex + count - 1) % count;
        }
        if (GUILayout.Button(MvpLocale.T("next"), GUILayout.Height(28)) && hasDevices)
        {
            int count = devices.Length;
            ProximityVoiceChat.MicrophoneDeviceIndex = (ProximityVoiceChat.MicrophoneDeviceIndex + 1) % count;
        }
        GUI.enabled = true;
        GUILayout.EndHorizontal();
    }

    void ResetDefaults()
    {
        MvpHud.LanguageIndexStatic = 0;
        MvpHud.MasterVolumeStatic = 1f;
        PlayerCameraController.HorizontalSensitivity = 2f;
        PlayerCameraController.VerticalSensitivity = 2f;
        PlayerCameraController.InvertY = false;
        PlayerCameraController.FieldOfView = 68f;
        ProximityVoiceChat.VoiceEnabled = true;
        ProximityVoiceChat.Muted = false;
        ProximityVoiceChat.PushToTalk = false;
        ProximityVoiceChat.MicGain = 1f;
        ProximityVoiceChat.OutputVolume = 1f;
        ProximityVoiceChat.MaxDistance = 18f;
        ProximityVoiceChat.MicrophoneDeviceIndex = 0;
        AccessibilityPrefs.ResetDefaults();
        DisplaySettings.ResetDefaults();
        DisplaySettings.ApplyFullscreen();
        DisplaySettings.ApplyQuality();
        BrightnessController.Apply();
    }

    /// <summary>Leave the session and return to the main menu (the non-networked HQ scene).
    /// Mirrors DisconnectHandler's return path; a host shutting down drops its clients, whose
    /// own DisconnectHandler walks them back to the menu too.</summary>
    static void QuitToMainMenu()
    {
        PlayerPrefs.Save();
        IsOpen = false;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
        // Guests may be mirroring the host's company; restore the local save. Stale commission
        // state must not leak into the fresh menu session either.
        CompanyData.ReloadFromDisk();
        MvpMissionRuntime.Clear();
        UnityEngine.SceneManagement.SceneManager.LoadScene("HQ");
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
}
