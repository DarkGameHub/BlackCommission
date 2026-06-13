using UnityEngine;

/// <summary>
/// The single, shared settings panel for the whole game. Self-bootstraps as a
/// DontDestroyOnLoad IMGUI singleton so it works identically in the main menu, the HQ,
/// and every mission scene. Both the menu (MainMenuUI) and the in-game HUD (MvpHud)
/// open it via Open()/Toggle(); ESC handling for the gameplay case stays in MvpHud.
/// </summary>
public class SettingsOverlay : MonoBehaviour
{
    static SettingsOverlay instance;

    public static bool IsOpen { get; private set; }

    Vector2 scroll;
    bool stylesReady;
    GUIStyle panelStyle, titleStyle, labelStyle, accentStyle, buttonStyle, headerTextStyle;
    Texture2D panelTex, headerBandTex;

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

    void EnsureStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        panelTex = new Texture2D(1, 1);
        panelTex.SetPixel(0, 0, BlackCommissionUiTheme.ConcreteBlack);
        panelTex.Apply();

        headerBandTex = new Texture2D(1, 1);
        headerBandTex.SetPixel(0, 0, BlackCommissionUiTheme.MilitaryGreen);  // = oxblood now
        headerBandTex.Apply();

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTex },
            padding = new RectOffset(16, 16, 14, 14)
        };
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20, fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.OldPaper }, wordWrap = true
        };
        headerTextStyle = new GUIStyle(titleStyle) { fontSize = 18, alignment = TextAnchor.MiddleLeft };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15, normal = { textColor = BlackCommissionUiTheme.Text }, wordWrap = true
        };
        // Section headers use tungsten amber (the UI accent) — green is reserved for
        // electronic screens only (terminal), never UI chrome.
        accentStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold, normal = { textColor = BlackCommissionUiTheme.OldWood }
        };
        buttonStyle = BlackCommissionUiTheme.ButtonStyle(15);

        MvpFontProvider.ApplyToStyle(panelStyle);
        MvpFontProvider.ApplyToStyle(titleStyle);
        MvpFontProvider.ApplyToStyle(headerTextStyle);
        MvpFontProvider.ApplyToStyle(labelStyle);
        MvpFontProvider.ApplyToStyle(accentStyle);
        MvpFontProvider.ApplyToStyle(buttonStyle);
    }

    void OnGUI()
    {
        if (!IsOpen) return;
        EnsureStyles();
        BlackCommissionUiTheme.ApplyButtonSkin(buttonStyle);

        float width = Mathf.Clamp(Screen.width - 36f, 360f, 620f);
        float height = Mathf.Clamp(Screen.height - 80f, 420f, 680f);
        Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);

        // Oxblood "stamped document" header band across the top of the panel.
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 46f), headerBandTex);
        GUI.Label(new Rect(rect.x + 18f, rect.y + 10f, rect.width - 120f, 28f),
            "PREFERENCE RECORD", headerTextStyle);
        GUI.Label(new Rect(rect.x + 18f, rect.y + 28f, rect.width - 120f, 16f),
            "FORM BC-05", labelStyle);
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
        string[] languageLabels = { "English", "中文 (简体)" };
        GUILayout.Label(MvpLocale.T("language", languageLabels[Mathf.Clamp(MvpHud.LanguageIndexStatic, 0, 1)]), labelStyle);
        int selectedLanguage = GUILayout.SelectionGrid(MvpHud.LanguageIndexStatic, languageLabels, 2);
        if (selectedLanguage != MvpHud.LanguageIndexStatic)
            MvpHud.LanguageIndexStatic = selectedLanguage;
        GUILayout.Label(MvpLocale.T("master_volume", $"{MvpHud.MasterVolumeStatic:0.00}"), labelStyle);
        MvpHud.MasterVolumeStatic = GUILayout.HorizontalSlider(MvpHud.MasterVolumeStatic, 0f, 1f);

        // ─── Display ───
        GUILayout.Space(10);
        GUILayout.Label(MvpLocale.T("display"), accentStyle);
        GUILayout.Label(MvpLocale.T("brightness", $"{DisplaySettings.Brightness:0.0}"), labelStyle);
        float newBrightness = GUILayout.HorizontalSlider(DisplaySettings.Brightness,
            DisplaySettings.MinBrightness, DisplaySettings.MaxBrightness);
        if (!Mathf.Approximately(newBrightness, DisplaySettings.Brightness))
        {
            DisplaySettings.Brightness = newBrightness;
            BrightnessController.Apply();
        }
        bool newFullscreen = GUILayout.Toggle(DisplaySettings.Fullscreen, MvpLocale.T("fullscreen"));
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
        PlayerCameraController.HorizontalSensitivity = GUILayout.HorizontalSlider(PlayerCameraController.HorizontalSensitivity, 0.25f, 8f);
        GUILayout.Label(MvpLocale.T("v_sensitivity", $"{PlayerCameraController.VerticalSensitivity:0.00}"), labelStyle);
        PlayerCameraController.VerticalSensitivity = GUILayout.HorizontalSlider(PlayerCameraController.VerticalSensitivity, 0.25f, 8f);
        PlayerCameraController.InvertY = GUILayout.Toggle(PlayerCameraController.InvertY, MvpLocale.T("invert_y"));
        GUILayout.Label(MvpLocale.T("fov", $"{PlayerCameraController.FieldOfView:0}"), labelStyle);
        PlayerCameraController.FieldOfView = GUILayout.HorizontalSlider(PlayerCameraController.FieldOfView, 55f, 95f);

        // ─── Voice ───
        GUILayout.Space(10);
        GUILayout.Label(MvpLocale.T("voice"), accentStyle);
        ProximityVoiceChat.VoiceEnabled = GUILayout.Toggle(ProximityVoiceChat.VoiceEnabled, MvpLocale.T("voice_default_on"));
        ProximityVoiceChat.Muted = GUILayout.Toggle(ProximityVoiceChat.Muted, MvpLocale.T("mute_self"));
        ProximityVoiceChat.PushToTalk = GUILayout.Toggle(ProximityVoiceChat.PushToTalk, MvpLocale.T("push_to_talk"));
        DrawMicrophoneSelector();
        GUILayout.Label(MvpLocale.T("mic_gain", $"{ProximityVoiceChat.MicGain:0.0}"), labelStyle);
        ProximityVoiceChat.MicGain = GUILayout.HorizontalSlider(ProximityVoiceChat.MicGain, 0f, 2f);
        GUILayout.Label(MvpLocale.T("voice_volume", $"{ProximityVoiceChat.OutputVolume:0.0}"), labelStyle);
        ProximityVoiceChat.OutputVolume = GUILayout.HorizontalSlider(ProximityVoiceChat.OutputVolume, 0f, 2f);
        GUILayout.Label(MvpLocale.T("voice_distance", $"{ProximityVoiceChat.MaxDistance:0}"), labelStyle);
        ProximityVoiceChat.MaxDistance = GUILayout.HorizontalSlider(ProximityVoiceChat.MaxDistance, 4f, 40f);

        // ─── Buttons ───
        GUILayout.Space(14);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(MvpLocale.T("reset_defaults"), GUILayout.Height(32)))
            ResetDefaults();
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
        DisplaySettings.ResetDefaults();
        DisplaySettings.ApplyFullscreen();
        DisplaySettings.ApplyQuality();
        BrightnessController.Apply();
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
