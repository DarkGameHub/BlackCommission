using Unity.Netcode;
using UnityEngine;

public class HQController : MonoBehaviour
{
    GUIStyle labelStyle;
    GUIStyle headerStyle;
    Texture2D panelBg;

    void InitStyles()
    {
        if (labelStyle != null) return;
        panelBg = new Texture2D(1, 1);
        panelBg.SetPixel(0, 0, BlackCommissionUiTheme.ConcreteBlack);
        panelBg.Apply();

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = BlackCommissionUiTheme.Text }
        };
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16, fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.CrtGreen }
        };
        MvpFontProvider.ApplyToStyle(labelStyle);
        MvpFontProvider.ApplyToStyle(headerStyle);
    }

    void OnGUI()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        if (FindAnyObjectByType<MvpHud>() != null) return;
        InitStyles();

        float x = 20, y = 40, w = 300;
        GUI.DrawTexture(new Rect(x - 4, y - 4, w + 8, 130), panelBg);

        GUILayout.BeginArea(new Rect(x, y, w, 122));
        GUILayout.Label("黑色委托", headerStyle);
        GUILayout.Label($"资金: ¥{CompanyData.Current.Funds}", labelStyle);
        GUILayout.Label($"声誉: {CompanyData.Current.Reputation}", labelStyle);
        GUILayout.Space(4);
        GUILayout.Label("办公室电脑可查看委托", labelStyle);
        GUILayout.Label("装备架可补充任务道具", labelStyle);
        GUILayout.EndArea();
    }
}
