using UnityEngine;

/// <summary>
/// The game's accessibility switches — the single home the per-screen UX specs kept
/// promising ("挂入设置 spec"): hud.md (reduced screen-flash), item-inspection.md
/// (hold→toggle, reduced-motion pitch), settlement/boarding (no stamp-slam scale, no
/// ink-bar breathing). PlayerPrefs-backed; consumers read the statics every frame, so
/// changes apply live from the ESC preference form.
/// </summary>
public static class AccessibilityPrefs
{
    const string ReducedMotionKey = "AS.A11y.ReducedMotion";
    const string InspectToggleKey = "AS.A11y.InspectToggle";
    const string ReducedFlashKey = "AS.A11y.ReducedFlash";

    /// <summary>减弱动效: no stamp-slam scaling, no ink-bar breathing, inspect pitch zeroed.</summary>
    public static bool ReducedMotion
    {
        get => PlayerPrefs.GetInt(ReducedMotionKey, 0) == 1;
        set => PlayerPrefs.SetInt(ReducedMotionKey, value ? 1 : 0);
    }

    /// <summary>检视改开关式: press F to enter, press again to exit (no held key).</summary>
    public static bool InspectToggleMode
    {
        get => PlayerPrefs.GetInt(InspectToggleKey, 0) == 1;
        set => PlayerPrefs.SetInt(InspectToggleKey, value ? 1 : 0);
    }

    /// <summary>减弱屏闪: full-screen damage flash becomes an 8px edge-frame pulse.</summary>
    public static bool ReducedFlash
    {
        get => PlayerPrefs.GetInt(ReducedFlashKey, 0) == 1;
        set => PlayerPrefs.SetInt(ReducedFlashKey, value ? 1 : 0);
    }

    public static void ResetDefaults()
    {
        ReducedMotion = false;
        InspectToggleMode = false;
        ReducedFlash = false;
    }
}
