using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game HUD: objectives, water level warning, phase indicator, teammate status.
/// </summary>
public class HUDController : MonoBehaviour
{
    [Header("Objectives")]
    [SerializeField] TextMeshProUGUI survivorStatusText;
    [SerializeField] TextMeshProUGUI pumpStatusText;
    [SerializeField] Image survivor1Icon;
    [SerializeField] Image survivor2Icon;

    [Header("Hazard")]
    [SerializeField] Slider waterLevelSlider;
    [SerializeField] TextMeshProUGUI waterWarningText;
    [SerializeField] TextMeshProUGUI phaseText;

    [Header("Evacuation")]
    [SerializeField] TextMeshProUGUI timeRemainingText;
    [SerializeField] GameObject forcedEvacWarning;

    [Header("Teammate Status")]
    [SerializeField] Transform teammateStatusParent;  // 3 teammate panels

    Color safe = new Color(0.2f, 0.9f, 0.3f);
    Color danger = new Color(0.9f, 0.3f, 0.3f);
    Color neutral = Color.white;

    void Update()
    {
        UpdateObjectives();
        UpdateWaterLevel();
        UpdateEvacTimer();
    }

    void UpdateObjectives()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        int rescued = gm.SurvivorsRescued.Value;
        survivorStatusText.text = $"幸存者: {rescued}/2";
        survivorStatusText.color = rescued >= 1 ? safe : neutral;

        pumpStatusText.text = gm.PumpRepaired.Value ? "排水泵: 已修复" : "排水泵: 故障";
        pumpStatusText.color = gm.PumpRepaired.Value ? safe : danger;
    }

    void UpdateWaterLevel()
    {
        var wm = WaterLevelManager.Instance;
        if (wm == null) return;

        float level = wm.NormalizedLevel;
        waterLevelSlider.value = level;

        bool highWater = level > 0.6f;
        waterWarningText.gameObject.SetActive(highWater);
        if (highWater) waterWarningText.text = level > 0.85f ? "水位严重！" : "水位上涨";

        var gm = GameManager.Instance;
        phaseText.text = gm?.CurrentPhase.Value switch
        {
            GameManager.MissionPhase.Active => "状态: 可控",
            GameManager.MissionPhase.Escalating => "状态: 恶化",
            GameManager.MissionPhase.Critical => "状态: 失控",
            GameManager.MissionPhase.ForcedEvac => "状态: 强制撤离！",
            _ => ""
        };
    }

    void UpdateEvacTimer()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        float remaining = gm.TimeRemainingToForcedEvac;
        timeRemainingText.text = $"{Mathf.CeilToInt(remaining / 60):00}:{Mathf.CeilToInt(remaining % 60):00}";

        bool forceEvac = gm.CurrentPhase.Value == GameManager.MissionPhase.ForcedEvac;
        forcedEvacWarning.SetActive(forceEvac);
    }
}
