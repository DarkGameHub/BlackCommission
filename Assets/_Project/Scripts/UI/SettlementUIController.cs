using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SettlementUIController : MonoBehaviour
{
    public static SettlementUIController Instance { get; private set; }

    public bool IsVisible => _visible;
    bool _visible;
    SettlementData _data;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void ShowSettlement(SettlementData data)
    {
        _data = data;
        _visible = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void OnGUI()
    {
        if (!_visible || _data == null) return;

        float pw = 480f, ph = 420f;
        float px = (Screen.width - pw) / 2f;
        float py = (Screen.height - ph) / 2f;

        Rect panelRect = new Rect(px, py, pw, ph);
        BlackCommissionUiTheme.DrawPanelFrame(panelRect);

        GUILayout.BeginArea(new Rect(px + 20, py + 20, pw - 40, ph - 40));

        var titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = BlackCommissionUiTheme.OldPaper }
        };
        var labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            normal = { textColor = BlackCommissionUiTheme.Text }
        };
        var profitStyle = new GUIStyle(labelStyle)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            normal = { textColor = _data.Net >= 0 ? BlackCommissionUiTheme.CrtGreen : BlackCommissionUiTheme.RustWarning }
        };

        string resultTitle = _data.Net >= 0 ? "Mission Complete" : "Mission Over (Loss)";
        GUILayout.Label(resultTitle, titleStyle);
        GUILayout.Space(10);

        int mins = (int)(_data.TimeElapsed / 60);
        int secs = (int)(_data.TimeElapsed % 60);

        GUILayout.Label($"Survivors Rescued: {_data.SurvivorsRescued}/2", labelStyle);
        GUILayout.Label($"Pump Repaired: {(_data.PumpRepaired ? "Yes" : "No")}", labelStyle);
        GUILayout.Label($"Time Elapsed: {mins:00}:{secs:00}", labelStyle);
        GUILayout.Space(8);
        GUILayout.Label($"Income: ¥{_data.Income}", labelStyle);
        GUILayout.Label($"Expenses: -¥{_data.Expenses}", labelStyle);
        GUILayout.Space(4);
        GUILayout.Label($"Net Profit: ¥{_data.Net}", profitStyle);
        GUILayout.Space(8);

        var fundsStyle = new GUIStyle(labelStyle)
        {
            normal = { textColor = CompanyData.Current.IsInDebt ? BlackCommissionUiTheme.RustWarning : BlackCommissionUiTheme.Text }
        };
        GUILayout.Label($"Office Funds: ¥{CompanyData.Current.Funds}", fundsStyle);

        GUILayout.Space(16);

        var btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 16,
            fixedHeight = 36,
            normal = { background = BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreenDark), textColor = BlackCommissionUiTheme.CrtGreen },
            hover = { background = BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreen), textColor = BlackCommissionUiTheme.CrtGreen },
            active = { background = BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.ConcretePanel), textColor = BlackCommissionUiTheme.OldPaper }
        };
        MvpFontProvider.ApplyToStyle(titleStyle);
        MvpFontProvider.ApplyToStyle(labelStyle);
        MvpFontProvider.ApplyToStyle(profitStyle);
        MvpFontProvider.ApplyToStyle(fundsStyle);
        MvpFontProvider.ApplyToStyle(btnStyle);
        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;
        bool isClient = NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient && !isHost;

        if (isClient)
        {
            GUILayout.Label("Waiting for host to return...", new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = BlackCommissionUiTheme.MutedText }
            });
        }
        else if (GUILayout.Button("Return to Office", btnStyle))
        {
            _visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.SceneManager.LoadScene("HQ", LoadSceneMode.Single);
            else
                SceneManager.LoadScene("HQ");
        }

        GUILayout.EndArea();
    }
}
