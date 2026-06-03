using UnityEngine;

/// <summary>
/// Persisted display preferences (brightness, fullscreen, quality). Brightness is a
/// URP post-exposure value in EV, applied through BrightnessController.
/// </summary>
public static class DisplaySettings
{
    public const float MinBrightness = -1.5f;
    public const float MaxBrightness = 2f;

    public static float Brightness
    {
        get => PlayerPrefs.GetFloat("AS.Display.Brightness", 0f);
        set => PlayerPrefs.SetFloat("AS.Display.Brightness", Mathf.Clamp(value, MinBrightness, MaxBrightness));
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt("AS.Display.Fullscreen", 1) != 0;
        set => PlayerPrefs.SetInt("AS.Display.Fullscreen", value ? 1 : 0);
    }

    public static int QualityLevel
    {
        get
        {
            int max = Mathf.Max(0, QualitySettings.names.Length - 1);
            return Mathf.Clamp(PlayerPrefs.GetInt("AS.Display.Quality", QualitySettings.GetQualityLevel()), 0, max);
        }
        set => PlayerPrefs.SetInt("AS.Display.Quality", value);
    }

    public static void ApplyFullscreen() => Screen.fullScreen = Fullscreen;

    public static void ApplyQuality()
    {
        int level = QualityLevel;
        if (level != QualitySettings.GetQualityLevel())
            QualitySettings.SetQualityLevel(level, true);
    }

    public static void ResetDefaults()
    {
        Brightness = 0f;
        Fullscreen = true;
        QualityLevel = QualitySettings.GetQualityLevel();
    }
}
