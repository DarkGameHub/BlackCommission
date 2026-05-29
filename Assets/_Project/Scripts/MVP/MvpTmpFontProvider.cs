using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Builds a TMP_FontAsset from the project's Chinese pixel font (FusionPixel12)
/// so TextMeshPro can render CJK glyphs. TMP Essentials' default LiberationSans
/// only covers Latin, which is why Chinese strings showed as tofu boxes.
/// </summary>
public static class MvpTmpFontProvider
{
    static TMP_FontAsset cached;
    static bool tried;

    public static TMP_FontAsset GetFontAsset()
    {
        if (cached != null) return cached;
        if (tried) return null;
        tried = true;

        Font sourceFont = Resources.Load<Font>("Fonts/FusionPixel12");
        if (sourceFont == null)
        {
            Debug.LogWarning("[MvpTmpFontProvider] FusionPixel12.ttf not found in Resources/Fonts. CJK chars will render as tofu.");
            return null;
        }

        cached = TMP_FontAsset.CreateFontAsset(
            sourceFont,
            samplingPointSize: 90,
            atlasPadding: 9,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: 2048,
            atlasHeight: 2048,
            atlasPopulationMode: AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true);

        if (cached != null)
        {
            cached.name = "FusionPixel12_TMP_Runtime";
            EnsureFallbackOnDefault(cached);
        }
        return cached;
    }

    // Adds our dynamic CJK atlas as a fallback on TMP's global default font asset,
    // so any TMP text not directly assigned a font still gets CJK glyph fallback.
    static void EnsureFallbackOnDefault(TMP_FontAsset cjk)
    {
        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null) return;
        if (defaultFont.fallbackFontAssetTable == null) return;
        if (defaultFont.fallbackFontAssetTable.Contains(cjk)) return;
        defaultFont.fallbackFontAssetTable.Add(cjk);
    }
}
