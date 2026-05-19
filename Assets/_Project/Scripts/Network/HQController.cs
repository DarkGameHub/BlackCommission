using Unity.Netcode;
using UnityEngine;

public class HQController : MonoBehaviour
{
    GUIStyle labelStyle;
    GUIStyle headerStyle;

    void InitStyles()
    {
        if (labelStyle != null) return;
        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white } };
        headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, normal = { textColor = Color.yellow } };
    }

    void OnGUI()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) return;
        InitStyles();

        float x = 20, y = 20, w = 300;

        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(x - 4, y - 4, w + 8, 130), Texture2D.whiteTexture);
        GUI.color = Color.white;

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
