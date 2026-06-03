using UnityEngine;

public static class MvpFontProvider
{
    static Font cachedFont;
    static bool loadAttempted;

    public static Font GetFont()
    {
        if (cachedFont != null) return cachedFont;
        if (loadAttempted) return null;
        loadAttempted = true;

        cachedFont = Resources.Load<Font>("Fonts/FusionPixel12");
        if (cachedFont == null)
            cachedFont = Resources.Load<Font>("Fonts/PixelFont");

        if (cachedFont == null)
        {
            cachedFont = Font.CreateDynamicFontFromOSFont("SimSun", 16);
        }

        if (cachedFont != null && cachedFont.material != null && cachedFont.material.mainTexture != null)
            cachedFont.material.mainTexture.filterMode = FilterMode.Point;

        return cachedFont;
    }

    public static void ApplyToStyle(GUIStyle style)
    {
        Font f = GetFont();
        if (f != null)
            style.font = f;
    }
}
