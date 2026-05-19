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
        panelBg.SetPixel(0, 0, new Color(0, 0, 0, 0.6f));
        panelBg.Apply();

        var font = UIFont.Get();
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            font = font, fontSize = 14,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) }
        };
        headerStyle = new GUIStyle(GUI.skin.label)
        {
            font = font, fontSize = 16, fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.95f, 0.85f, 0.4f) }
        };
    }

    void OnGUI()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        InitStyles();

        float x = 20, y = 40, w = 300;
        GUI.DrawTexture(new Rect(x - 4, y - 4, w + 8, 130), panelBg);

        GUILayout.BeginArea(new Rect(x, y, w, 122));
        GUILayout.Label("外包事故组", headerStyle);
        GUILayout.Label($"资金: ¥{CompanyData.Current.Funds}", labelStyle);
        GUILayout.Label($"声誉: {CompanyData.Current.Reputation}", labelStyle);
        GUILayout.Space(4);
        GUILayout.Label("走到工单板按 [E] 接受任务", labelStyle);
        GUILayout.Label("走到装备架按 [F] 拿取装备", labelStyle);
        GUILayout.EndArea();
    }
}
