using Unity.Netcode;
using UnityEngine;

public class SimpleHUD : MonoBehaviour
{
    GUIStyle labelStyle;
    GUIStyle warningStyle;
    GUIStyle endStyle;
    GUIStyle keyStyle;
    Texture2D panelBg;
    Texture2D endBg;
    bool stylesReady;

    void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        panelBg = MakeTex(1, 1, new Color(0, 0, 0, 0.5f));
        endBg = MakeTex(1, 1, new Color(0.05f, 0.05f, 0.1f, 0.9f));

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            richText = true,
            normal = { textColor = new Color(0.9f, 0.9f, 0.9f) },
            padding = new RectOffset(6, 6, 2, 2)
        };

        warningStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            normal = { textColor = new Color(1f, 0.35f, 0.1f) }
        };

        endStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            normal = { textColor = new Color(1f, 0.95f, 0.6f) },
            padding = new RectOffset(16, 16, 16, 16)
        };

        keyStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            richText = true,
            normal = { textColor = new Color(0.55f, 0.55f, 0.6f) },
            padding = new RectOffset(4, 4, 1, 1)
        };
    }

    static Texture2D MakeTex(int w, int h, Color col)
    {
        var pixels = new Color[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = col;
        var tex = new Texture2D(w, h);
        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    void OnGUI()
    {
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsListening) return;
        InitStyles();

        var gm = GameManager.Instance;
        var wm = WaterLevelManager.Instance;

        float y = 10;
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;

        GUI.DrawTexture(new Rect(6, 6, 260, 220), panelBg);

        if (gm != null)
        {
            string phaseStr = gm.CurrentPhase.Value switch
            {
                GameManager.MissionPhase.Active     => "<color=#88ff88>可控</color>",
                GameManager.MissionPhase.Escalating => "<color=#ffcc00>恶化</color>",
                GameManager.MissionPhase.Critical   => "<color=#ff6600>失控</color>",
                GameManager.MissionPhase.ForcedEvac => "<color=#ff2222>强制撤离</color>",
                GameManager.MissionPhase.Ended      => "<color=#ffff00>任务结束</color>",
                _ => ""
            };
            GUI.Label(new Rect(10, y, 250, 22), $"状态  {phaseStr}", labelStyle); y += 22;

            float remaining = gm.TimeRemainingToForcedEvac;
            int min = Mathf.FloorToInt(remaining / 60);
            int sec = Mathf.FloorToInt(remaining % 60);
            string timeColor = remaining < 60 ? "#ff4444" : remaining < 180 ? "#ffcc00" : "#aaaaaa";
            GUI.Label(new Rect(10, y, 250, 22), $"倒计时  <color={timeColor}>{min:00}:{sec:00}</color>", labelStyle); y += 22;

            string pumpStr = gm.PumpRepaired.Value
                ? "<color=#88ff88>已修复</color>"
                : "<color=#ff4444>故障</color>";
            GUI.Label(new Rect(10, y, 250, 22), $"排水泵  {pumpStr}", labelStyle); y += 22;

            GUI.Label(new Rect(10, y, 250, 22), $"幸存者  {gm.SurvivorsRescued.Value}/2", labelStyle); y += 22;
        }

        if (wm != null)
        {
            float pct = wm.NormalizedLevel * 100f;
            string waterColor = pct > 70 ? "#ff4444" : pct > 40 ? "#ffcc00" : "#88bbff";
            GUI.Label(new Rect(10, y, 250, 22),
                $"水位  <color={waterColor}>{pct:F0}%</color>", labelStyle); y += 22;
        }

        if (localPlayer != null && localPlayer.TryGetComponent<PlayerHealth>(out var ph))
        {
            string hpColor = ph.CurrentHP.Value > 50 ? "#88ff88" : ph.CurrentHP.Value > 20 ? "#ffcc00" : "#ff4444";
            GUI.Label(new Rect(10, y, 250, 22),
                $"HP  <color={hpColor}>{ph.CurrentHP.Value:F0}/100</color>", labelStyle); y += 22;
            if (ph.IsDowned.Value)
            {
                GUI.Label(new Rect(10, y, 250, 22),
                    "<color=#ff2222>已倒地 - 等待队友救援</color>", labelStyle); y += 22;
            }
        }

        if (localPlayer != null && localPlayer.TryGetComponent<FlashlightController>(out var fl))
        {
            string flColor = fl.BatteryNormalized > 0.3f ? "#aaccff" : "#ff6644";
            string cdText = fl.CooldownRemaining > 0 ? $"  冷却{fl.CooldownRemaining:F0}s" : "";
            GUI.Label(new Rect(10, y, 250, 22),
                $"手电  <color={flColor}>{fl.BatteryNormalized * 100:F0}%{cdText}</color>", labelStyle); y += 22;
        }

        if (localPlayer != null)
        {
            var pc = localPlayer.GetComponent<PlayerController>();
            if (pc != null)
            {
                string stColor = pc.Stamina > 30 ? "#88ff88" : "#ffcc00";
                GUI.Label(new Rect(10, y, 250, 22),
                    $"体力  <color={stColor}>{pc.Stamina:F0}/100</color>", labelStyle); y += 22;
            }
        }

        // ── Controls hint (bottom-right) ────────────────────
        float ky = Screen.height - 90;
        float kx = Screen.width - 220;
        GUI.DrawTexture(new Rect(kx - 4, ky - 4, 220, 78), panelBg);
        GUI.Label(new Rect(kx, ky, 210, 16), "[WASD] 移动   [Shift] 冲刺", keyStyle); ky += 16;
        GUI.Label(new Rect(kx, ky, 210, 16), "[E] 交互   [F] 拾取   [G] 丢弃", keyStyle); ky += 16;
        GUI.Label(new Rect(kx, ky, 210, 16), "[V] 手电筒   [左键] 强光", keyStyle); ky += 16;
        GUI.Label(new Rect(kx, ky, 210, 16), "[C] 蹲下   [空格] 跳跃", keyStyle);

        // ── Center warning ──────────────────────────────────
        if (gm?.CurrentPhase.Value == GameManager.MissionPhase.ForcedEvac)
        {
            GUI.Label(new Rect((Screen.width - 400) / 2f, 8, 400, 32),
                "立即前往撤离点！", warningStyle);
        }

        // ── End screen (skip when settlement panel showing) ─
        if (gm?.CurrentPhase.Value == GameManager.MissionPhase.Ended
            && !(SettlementUIController.Instance != null && SettlementUIController.Instance.IsVisible))
        {
            float bw = 400, bh = 120;
            float bx = (Screen.width - bw) / 2f, by = (Screen.height - bh) / 2f;
            GUI.DrawTexture(new Rect(bx, by, bw, bh), endBg);

            string title = gm.MissionFailed.Value
                ? "<color=#ff4444>任务失败</color>"
                : "<color=#88ff88>任务完成！</color>";
            string pump = gm.PumpRepaired.Value ? "已修复" : "未修复";
            GUI.Label(new Rect(bx, by, bw, bh),
                $"{title}\n\n救出幸存者: {gm.SurvivorsRescued.Value}/2\n排水泵: {pump}", endStyle);
        }
    }
}
