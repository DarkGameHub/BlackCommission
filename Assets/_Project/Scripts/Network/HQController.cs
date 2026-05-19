using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HQController : MonoBehaviour
{
    void OnGUI()
    {
        var boxStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 14,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = Color.white }
        };
        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14,
            normal = { textColor = Color.white }
        };
        var btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fixedHeight = 34
        };

        float x = 20, y = 20, w = 340;

        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(x - 4, y - 4, w + 8, 120), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(x, y, w, 112));
        GUILayout.Label($"事务所资金: ¥{CompanyData.Current.Funds}", labelStyle);
        GUILayout.Label($"声誉: {CompanyData.Current.Reputation}", labelStyle);
        GUILayout.Space(6);
        GUILayout.Label("工单：地下商场积水事故", labelStyle);
        GUILayout.Label("目标：救幸存者、修排水泵、撤离", labelStyle);
        GUILayout.Label("预计报酬：¥1700", labelStyle);
        GUILayout.EndArea();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost)
        {
            if (GUI.Button(new Rect(x, y + 120, w, 34), "开始任务", btnStyle))
            {
                NetworkManager.Singleton.SceneManager.LoadScene("Mall_B2", LoadSceneMode.Single);
            }
        }
    }
}
