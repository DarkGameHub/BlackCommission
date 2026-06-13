using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Supplies the project's primary TMP font. The UI font is the Lethal-Company face
/// 3270 (IBM 3270 terminal revival, OFL) — a retro-terminal monospace that unifies
/// the menu with the in-game CRT terminal. 3270 is Latin-only, so the Chinese pixel
/// font FusionPixel12 is attached as a CJK fallback (any 中文 string still renders).
///
/// Both atlases are built at runtime (Dynamic population), so no editor-baked TMP
/// Font Asset is required. If 3270 is missing we fall back to FusionPixel12 as the
/// primary (previous behaviour) so the UI never breaks.
/// </summary>
public static class MvpTmpFontProvider
{
    const string PrimaryFontResource = "Fonts/3270-Regular";  // Lethal Company UI font
    const string CjkFontResource = "Fonts/FusionPixel12";     // CJK fallback

    static TMP_FontAsset cached;
    static bool tried;

    public static TMP_FontAsset GetFontAsset()
    {
        if (cached != null) return cached;
        if (tried) return null;
        tried = true;

        TMP_FontAsset cjk = BuildFontAsset(CjkFontResource, 90, 2048, "FusionPixel12_TMP_Runtime");

        TMP_FontAsset primary = BuildFontAsset(PrimaryFontResource, 96, 1024, "BC3270_TMP_Runtime");
        if (primary != null)
        {
            // Attach the CJK atlas so Chinese glyphs still resolve through fallback.
            if (cjk != null)
            {
                primary.fallbackFontAssetTable ??= new List<TMP_FontAsset>();
                if (!primary.fallbackFontAssetTable.Contains(cjk))
                    primary.fallbackFontAssetTable.Add(cjk);
            }
            cached = primary;
        }
        else
        {
            // 3270 missing — keep the old behaviour (CJK pixel font as primary).
            cached = cjk;
        }

        if (cached != null)
            EnsureFallbackOnDefault(cached);
        return cached;
    }

    static TMP_FontAsset BuildFontAsset(string resourcePath, int samplingPointSize, int atlasSize, string name)
    {
        Font src = Resources.Load<Font>(resourcePath);
        if (src == null)
        {
            Debug.LogWarning($"[MvpTmpFontProvider] font '{resourcePath}' not found in Resources.");
            return null;
        }

        var asset = TMP_FontAsset.CreateFontAsset(
            src,
            samplingPointSize: samplingPointSize,
            atlasPadding: 9,
            renderMode: GlyphRenderMode.SDFAA,
            atlasWidth: atlasSize,
            atlasHeight: atlasSize,
            atlasPopulationMode: AtlasPopulationMode.Dynamic,
            enableMultiAtlasSupport: true);

        if (asset != null) asset.name = name;
        return asset;
    }

    // Adds our atlases as fallbacks on TMP's global default font, so any TMP text not
    // directly assigned a font still resolves Latin (3270) and CJK glyphs.
    static void EnsureFallbackOnDefault(TMP_FontAsset primary)
    {
        var defaultFont = TMP_Settings.defaultFontAsset;
        if (defaultFont == null || defaultFont.fallbackFontAssetTable == null) return;
        if (!defaultFont.fallbackFontAssetTable.Contains(primary))
            defaultFont.fallbackFontAssetTable.Add(primary);
    }
}
