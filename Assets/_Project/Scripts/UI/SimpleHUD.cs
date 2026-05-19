using Unity.Netcode;
using UnityEngine;

public class SimpleHUD : MonoBehaviour
{
    GUIStyle normalStyle;
    GUIStyle warningStyle;
    GUIStyle endStyle;

    void InitStyles()
    {
        if (normalStyle != null) return;

        normalStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = Color.white }
        };
        warningStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.4f, 0.1f) }
        };
        endStyle = new GUIStyle(GUI.skin.box)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.yellow }
        };
    }

    void OnGUI()
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening) return;
        InitStyles();

        var gm = GameManager.Instance;
        var wm = WaterLevelManager.Instance;

        // ── 左上：任务状态 ────────────────────────────────
        float y = 10;
        if (gm != null)
        {
            string phaseStr = gm.CurrentPhase.Value switch
            {
                GameManager.MissionPhase.Active      => "<color=#88ff88>可控</color>",
                GameManager.MissionPhase.Escalating  => "<color=#ffcc00>恶化</color>",
                GameManager.MissionPhase.Critical    => "<color=#ff6600>失控</color>",
                GameManager.MissionPhase.ForcedEvac  => "<color=#ff2222>强制撤离！</color>",
                GameManager.MissionPhase.Ended       => "<color=#ffff00>任务结束</color>",
                _ => ""
            };
            GUI.Label(new Rect(10, y, 300, 24), $"状态: {phaseStr}", normalStyle); y += 26;

            float remaining = gm.TimeRemainingToForcedEvac;
            int min = Mathf.FloorToInt(remaining / 60);
            int sec = Mathf.FloorToInt(remaining % 60);
            GUI.Label(new Rect(10, y, 300, 24), $"强撤倒计时: {min:00}:{sec:00}", normalStyle); y += 26;

            string pumpStr = gm.PumpRepaired.Value ? "<color=#88ff88>已修复 ✓</color>" : "<color=#ff4444>故障 ✗</color>";
            GUI.Label(new Rect(10, y, 300, 24), $"排水泵: {pumpStr}", normalStyle); y += 26;

            GUI.Label(new Rect(10, y, 300, 24), $"幸存者: {gm.SurvivorsRescued.Value}/2", normalStyle); y += 26;
        }

        if (wm != null)
        {
            float pct = wm.NormalizedLevel * 100f;
            string waterColor = pct > 70 ? "#ff4444" : pct > 40 ? "#ffcc00" : "#88bbff";
            GUI.Label(new Rect(10, y, 300, 24), $"<color={waterColor}>水位: {pct:F0}%  (Y={wm.CurrentWaterHeight.Value:F2})</color>", normalStyle); y += 26;
        }

        // ── 本地玩家 HP ────────────────────────────────
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer != null && localPlayer.TryGetComponent<PlayerHealth>(out var ph))
        {
            string hpColor = ph.CurrentHP.Value > 50 ? "#88ff88" : ph.CurrentHP.Value > 20 ? "#ffcc00" : "#ff4444";
            GUI.Label(new Rect(10, y, 300, 24), $"<color={hpColor}>HP: {ph.CurrentHP.Value:F0}/{100}</color>", normalStyle); y += 26;
            if (ph.IsDowned.Value)
            {
                GUI.Label(new Rect(10, y, 300, 24), "<color=#ff2222>已倒地 — 等待队友救援</color>", normalStyle); y += 26;
            }
        }

        // Debug: confirm server is running
        bool isServer = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;
        string timerVal = gm != null ? $"{gm.MissionTimer.Value:F0}s" : "N/A";
        GUI.Label(new Rect(10, y, 300, 24),
            $"<color=#888888>[服务器:{(isServer ? "是" : "否")}  计时:{timerVal}]</color>", normalStyle); y += 26;

        // Interaction debug: shows nearest interactable for the local player
        var pi = FindFirstObjectByType<PlayerInteraction>();
        if (pi != null)
        {
            string tgt = pi.DebugTargetName;
            GUI.Label(new Rect(10, y, 300, 24),
                $"<color=#888888>[交互目标:{(string.IsNullOrEmpty(tgt) ? "无" : tgt)}]</color>", normalStyle);
        }

        // ── 中央警告 ─────────────────────────────────────
        if (gm?.CurrentPhase.Value == GameManager.MissionPhase.ForcedEvac)
        {
            GUI.Label(new Rect((Screen.width - 400) / 2f, 8, 400, 32),
                "⚠  立即前往撤离点！  ⚠", warningStyle);
        }

        // ── 任务结束画面 (skip if settlement panel is showing) ──
        if (gm?.CurrentPhase.Value == GameManager.MissionPhase.Ended
            && !(SettlementUIController.Instance != null && SettlementUIController.Instance.IsVisible))
        {
            float bw = 500, bh = 140;
            string title = gm.MissionFailed.Value ? "任务失败..." : "任务完成！";
            GUI.Box(new Rect((Screen.width - bw) / 2f, (Screen.height - bh) / 2f, bw, bh),
                $"{title}\n救出幸存者: {gm.SurvivorsRescued.Value}/2\n排水泵: {(gm.PumpRepaired.Value ? "已修复" : "未修复")}",
                endStyle);
        }
    }
}
