using UnityEngine;

public static class UIFont
{
    static Font _cached;

    public static Font Get()
    {
        if (_cached != null) return _cached;

        string[] preferred = { "PingFang SC", "Hiragino Sans GB", "Microsoft YaHei", "Noto Sans CJK SC", "Arial" };
        foreach (var name in preferred)
        {
            _cached = Font.CreateDynamicFontFromOSFont(name, 16);
            if (_cached != null) break;
        }
        _cached ??= GUI.skin.font;
        return _cached;
    }

    public static void Apply(GUIStyle style)
    {
        style.font = Get();
    }
}
