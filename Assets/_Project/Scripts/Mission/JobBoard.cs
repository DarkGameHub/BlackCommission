using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(BoxCollider))]
public class JobBoard : MonoBehaviour, IInteractable
{
    float _msgTimer;
    string _msg;
    GUIStyle _msgStyle;
    Texture2D _msgBg;

    public string InteractHint
    {
        get
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                return "需要先联机";
            if (!NetworkManager.Singleton.IsHost)
                return "等待房主接单";
            return "接受工单: 地下商场积水事故 (¥1700)";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            ShowMessage("请先点 创建房间 开始联机");
            return;
        }
        if (!NetworkManager.Singleton.IsHost)
        {
            ShowMessage("只有房主可以接单");
            return;
        }
        NetworkManager.Singleton.SceneManager.LoadScene("Mall_B2", LoadSceneMode.Single);
    }

    public void OnInteractEnd(PlayerController player) { }

    void ShowMessage(string msg)
    {
        _msg = msg;
        _msgTimer = 3f;
    }

    void OnGUI()
    {
        if (_msgTimer <= 0) return;
        _msgTimer -= Time.deltaTime;

        if (_msgStyle == null)
        {
            _msgBg = new Texture2D(1, 1);
            _msgBg.SetPixel(0, 0, new Color(0.1f, 0.1f, 0.15f, 0.9f));
            _msgBg.Apply();
            _msgStyle = new GUIStyle(GUI.skin.label)
            {
                font = UIFont.Get(), fontSize = 16,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow, background = _msgBg },
                padding = new RectOffset(12, 12, 8, 8)
            };
        }

        float w = 400, h = 40;
        GUI.Label(new Rect((Screen.width - w) / 2f, Screen.height * 0.6f, w, h), _msg, _msgStyle);
    }
}
