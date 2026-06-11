using System.Collections.Generic;
using UnityEngine;

public static class BlackCommissionUiTheme
{
    static readonly Dictionary<int, Texture2D> TextureCache = new();

    // Municipal Debt Noir tokens, matched 1:1 to the tower's V8 whitebox palette
    // (TowerV8WhiteboxBuilder.EnsureMaterials + art-bible/style-lock v2) so every UI
    // surface speaks the same language as the map. Field names are legacy — the
    // "MilitaryGreen" slots now hold civic teal (#3F5F5C family), "RustWarning" holds
    // stamp red (#C23A2B), "OldPaper" holds aged paper (#D6CCAE).
    public static readonly Color ConcreteBlack = new(0.020f, 0.024f, 0.022f, 0.94f);   // dead rubber black
    public static readonly Color ConcretePanel = new(0.058f, 0.062f, 0.060f, 0.94f);
    public static readonly Color ConcreteRaised = new(0.098f, 0.104f, 0.100f, 0.96f);
    public static readonly Color MilitaryGreen = new(0.247f, 0.373f, 0.361f, 0.95f);   // civic teal #3F5F5C
    public static readonly Color MilitaryGreenDim = new(0.148f, 0.226f, 0.218f, 0.92f);
    public static readonly Color MilitaryGreenDark = new(0.082f, 0.130f, 0.125f, 0.94f);
    public static readonly Color OldWood = new(0.780f, 0.550f, 0.200f, 0.92f);          // sodium amber
    public static readonly Color OldPaper = new(0.839f, 0.800f, 0.682f, 1f);            // aged paper #D6CCAE
    public static readonly Color PaperDim = new(0.610f, 0.578f, 0.490f, 1f);
    public static readonly Color Text = new(0.835f, 0.842f, 0.790f, 1f);
    public static readonly Color MutedText = new(0.565f, 0.585f, 0.545f, 1f);
    public static readonly Color CrtGreen = new(0.424f, 1.000f, 0.373f, 1f);            // screens/lamps ONLY
    public static readonly Color CrtGreenDim = new(0.260f, 0.560f, 0.250f, 1f);
    public static readonly Color Rust = new(0.549f, 0.349f, 0.216f, 1f);                // rust steel #8C5937
    public static readonly Color RustWarning = new(0.761f, 0.227f, 0.169f, 1f);         // stamp red #C23A2B
    public static readonly Color Shadow = new(0f, 0f, 0f, 0.62f);

    public static Texture2D MakeTex(Color color)
    {
        Color32 c = color;
        int key = (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
        if (TextureCache.TryGetValue(key, out Texture2D cached) && cached != null)
            return cached;

        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        TextureCache[key] = texture;
        return texture;
    }

    public static GUIStyle PanelStyle(int padding = 14)
    {
        var style = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(ConcreteBlack) },
            padding = new RectOffset(padding, padding, padding, padding),
            border = new RectOffset(2, 2, 2, 2)
        };
        MvpFontProvider.ApplyToStyle(style);
        return style;
    }

    public static GUIStyle SlotStyle(bool selected = false)
    {
        var style = new GUIStyle(GUI.skin.box)
        {
            normal = { background = MakeTex(selected ? MilitaryGreen : ConcretePanel) },
            padding = new RectOffset(8, 8, 8, 8),
            border = new RectOffset(2, 2, 2, 2)
        };
        MvpFontProvider.ApplyToStyle(style);
        return style;
    }

    public static GUIStyle LabelStyle(int size, Color color, FontStyle fontStyle = FontStyle.Normal,
        TextAnchor alignment = TextAnchor.UpperLeft, bool wordWrap = true)
    {
        var style = new GUIStyle(GUI.skin.label)
        {
            fontSize = size,
            fontStyle = fontStyle,
            alignment = alignment,
            wordWrap = wordWrap,
            normal = { textColor = color },
            padding = new RectOffset(0, 0, 1, 1)
        };
        MvpFontProvider.ApplyToStyle(style);
        return style;
    }

    public static GUIStyle ButtonStyle(int size = 15, bool primary = false)
    {
        Color normal = primary ? MilitaryGreen : ConcreteRaised;
        Color hover = primary ? new Color(0.330f, 0.400f, 0.280f, 0.98f) : new Color(0.135f, 0.145f, 0.128f, 0.98f);
        Color active = primary ? MilitaryGreenDark : ConcretePanel;
        var style = new GUIStyle(GUI.skin.button)
        {
            fontSize = size,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            wordWrap = false,
            normal = { background = MakeTex(normal), textColor = primary ? CrtGreen : Text },
            hover = { background = MakeTex(hover), textColor = CrtGreen },
            active = { background = MakeTex(active), textColor = OldPaper },
            focused = { background = MakeTex(normal), textColor = primary ? CrtGreen : Text },
            padding = new RectOffset(10, 10, 7, 7),
            border = new RectOffset(2, 2, 2, 2)
        };
        MvpFontProvider.ApplyToStyle(style);
        return style;
    }

    public static void DrawPanelFrame(Rect rect)
    {
        GUI.DrawTexture(new Rect(rect.x - 2f, rect.y - 2f, rect.width + 4f, rect.height + 4f),
            MakeTex(MilitaryGreenDim));
        GUI.DrawTexture(rect, MakeTex(ConcreteBlack));
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2f), MakeTex(CrtGreenDim));
    }

    public static void ApplyButtonSkin(GUIStyle buttonStyle)
    {
        GUI.skin.button = buttonStyle;
    }
}
