using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// HQ lobby UI: host game, join game with code, display company funds/reputation.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Company Info")]
    [SerializeField] TextMeshProUGUI fundsText;
    [SerializeField] TextMeshProUGUI reputationText;
    [SerializeField] TextMeshProUGUI debtWarningText;

    [Header("Multiplayer")]
    [SerializeField] Button hostButton;
    [SerializeField] Button joinButton;
    [SerializeField] TMP_InputField joinCodeInput;
    [SerializeField] TextMeshProUGUI lobbyCodeDisplay;
    [SerializeField] TextMeshProUGUI statusText;

    [Header("Mission")]
    [SerializeField] Button startMissionButton;
    [SerializeField] string missionSceneName = "Mall_B2";

    void Start()
    {
        UpdateCompanyInfo();

        hostButton.onClick.AddListener(OnHost);
        joinButton.onClick.AddListener(OnJoin);
        startMissionButton.onClick.AddListener(OnStartMission);
        startMissionButton.gameObject.SetActive(false);

        if (ConnectionManager.Instance != null)
        {
            ConnectionManager.Instance.OnJoinCodeReady += code =>
            {
                lobbyCodeDisplay.text = $"房间代码: {code}";
                lobbyCodeDisplay.gameObject.SetActive(true);
                startMissionButton.gameObject.SetActive(true);
                SetStatus("等待队友加入...");
            };
            ConnectionManager.Instance.OnConnected += () =>
            {
                SetStatus("已连接!");
                startMissionButton.gameObject.SetActive(true);
            };
            ConnectionManager.Instance.OnError += msg => SetStatus($"错误: {msg}");
        }
    }

    void UpdateCompanyInfo()
    {
        var data = CompanyData.Current;
        fundsText.text = $"资金: ¥{data.Funds}";
        fundsText.color = data.IsInDebt ? Color.red : Color.white;
        reputationText.text = $"信誉: {data.Reputation}";
        debtWarningText.gameObject.SetActive(data.IsInDebt);
    }

    void OnHost() => ConnectionManager.Instance?.HostGame();
    void OnJoin() => ConnectionManager.Instance?.JoinGame(joinCodeInput.text.Trim().ToUpper());
    void OnStartMission() => SceneManager.LoadScene(missionSceneName);

    void SetStatus(string msg) => statusText.text = msg;
}
