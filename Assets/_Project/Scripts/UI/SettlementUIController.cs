using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Post-mission settlement screen: shows income, penalties, net result.
/// </summary>
public class SettlementUIController : MonoBehaviour
{
    public static SettlementUIController Instance { get; private set; }

    [SerializeField] GameObject settlementPanel;
    [SerializeField] TextMeshProUGUI incomeText;
    [SerializeField] TextMeshProUGUI expensesText;
    [SerializeField] TextMeshProUGUI netText;
    [SerializeField] TextMeshProUGUI survivorsText;
    [SerializeField] TextMeshProUGUI pumpText;
    [SerializeField] TextMeshProUGUI evidenceText;
    [SerializeField] TextMeshProUGUI timeText;
    [SerializeField] TextMeshProUGUI companyFundsText;
    [SerializeField] Button returnToHQButton;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        settlementPanel.SetActive(false);
    }

    public void ShowSettlement(SettlementData data)
    {
        settlementPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;

        incomeText.text   = $"收入: ¥{data.Income}";
        expensesText.text = $"扣款: -¥{data.Expenses}";
        netText.text      = $"净利润: ¥{data.Net}";
        netText.color     = data.Net >= 0 ? new Color(0.2f, 0.9f, 0.3f) : Color.red;

        survivorsText.text = $"救出幸存者: {data.SurvivorsRescued}/2";
        pumpText.text      = data.PumpRepaired ? "排水泵: 已修复" : "排水泵: 未修复";
        evidenceText.text  = $"证据数量: {data.EvidenceCollected}";

        int mins = (int)(data.TimeElapsed / 60);
        int secs = (int)(data.TimeElapsed % 60);
        timeText.text = $"任务用时: {mins:00}:{secs:00}";

        companyFundsText.text = $"事务所资金: ¥{CompanyData.Current.Funds}";
        if (CompanyData.Current.IsInDebt)
            companyFundsText.color = Color.red;

        returnToHQButton.onClick.AddListener(() => SceneManager.LoadScene("HQ"));
    }
}
