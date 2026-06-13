using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// 委托结算单 — the stamped settlement card dealt to every peer inside the return
/// van (design/ux/settlement.md, Approved 2026-06-11). Read-only: shows the outcome
/// stamp, the itemized money breakdown (income → clause deductions → net) and the
/// client usage note. The actual ledger write stays at the office computer claim.
/// Shown via <see cref="Show"/> from TowerMissionManager.ApplyResultLocally on each
/// peer; E/Esc folds it, Tab re-opens it while still in transit; any scene load
/// (arrival at HQ) clears it for good — the office ledger takes over from there.
/// </summary>
public class SettlementCardOverlay : MonoBehaviour
{
    const float StampDelaySeconds = 0.4f;
    const float StampSlamSeconds = 0.12f;
    const float SlideSeconds = 0.25f;

    static SettlementCardOverlay current;

    struct Row
    {
        public string label;
        public int amount;       // signed; rendered ±NG
        public string subline;   // clause fine print under the row, or null
        public bool isNet;
    }

    bool hasCard;
    bool visible;
    float shownAt;
    bool stampSoundPlayed;
    int lastFoldFrame = -1;

    string docNumber;
    string commissionTitle;
    string stampLabel;
    string clientNote;
    Row[] rows;

    GUIStyle headerStyle, rowStyle, sublineStyle, netStyle, noteStyle, footerStyle, stampStyle;

    /// <summary>Builds and shows the card on this peer. All money values are final (post-scaling).</summary>
    public static void Show(MvpMissionResultKind kind, int baseMoney, int netMoney, float completeness)
    {
        if (current == null)
        {
            var go = new GameObject("MVP_SettlementCardOverlay");
            DontDestroyOnLoad(go);
            current = go.AddComponent<SettlementCardOverlay>();
        }
        current.Build(kind, baseMoney, netMoney, completeness);
    }

    void Build(MvpMissionResultKind kind, int baseMoney, int netMoney, float completeness)
    {
        // Deterministic across peers (same settlement data in → same number/note out).
        int seed = Mathf.Abs(netMoney * 73 + (int)kind * 131 + Mathf.RoundToInt(completeness * 100f) * 17);
        docNumber = $"BC-2098-{seed % 10000:D4}";

        OfficeTaskDefinition task = MvpMissionRuntime.SelectedTask;
        commissionTitle = task != null && !string.IsNullOrWhiteSpace(task.title)
            ? task.title
            : SceneManager.GetActiveScene().name; // fallback: 委托名退化为场景名 (spec States)
        clientNote = task != null ? task.GetSettlementNote(kind, seed) : null;

        stampLabel = kind switch
        {
            MvpMissionResultKind.Success => "完 成",
            MvpMissionResultKind.Partial => "部分结算",
            _ => "失 败"
        };

        switch (kind)
        {
            case MvpMissionResultKind.Success:
                int deduction = Mathf.Max(0, baseMoney - netMoney);
                rows = deduction > 0
                    ? new[]
                    {
                        new Row { label = "委托报酬", amount = baseMoney },
                        new Row
                        {
                            label = "运输磕碰扣损（条款C-7）", amount = -deduction,
                            subline = $"封装完整度 {completeness:P0}，每次硬性磕碰扣 3%"
                        },
                        new Row { label = "实付", amount = netMoney, isNet = true }
                    }
                    : new[]
                    {
                        new Row { label = "委托报酬", amount = baseMoney },
                        new Row { label = "实付", amount = netMoney, isNet = true }
                    };
                break;
            case MvpMissionResultKind.Partial:
                rows = new[]
                {
                    new Row
                    {
                        label = "提前收工折算（条款B-2）", amount = netMoney,
                        subline = "按委托报酬 22% 折算，不低于慰问金"
                    },
                    new Row { label = "实付", amount = netMoney, isNet = true }
                };
                break;
            default:
                rows = new[]
                {
                    new Row { label = "慰问金（条款D-1）", amount = netMoney },
                    new Row { label = "实付", amount = netMoney, isNet = true }
                };
                break;
        }

        hasCard = true;
        visible = true;
        shownAt = Time.unscaledTime;
        stampSoundPlayed = false;
    }

    void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
    void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

    /// <summary>True while the card is on screen (van arrival defers its E-disembark to it).</summary>
    public static bool IsCardVisible => current != null && current.hasCard && current.visible;

    /// <summary>
    /// True on the exact frame an E/Esc press folded the card. Script execution order between
    /// this overlay and VanTransitOverlay is undefined, so without this guard the same E press
    /// could fold the card AND disembark the player.
    /// </summary>
    public static bool ConsumedCloseThisFrame => current != null && current.lastFoldFrame == Time.frameCount;

    // Arrival anywhere (HQ) retires the card — re-reads go through the office ledger.
    // The HQ now loads *during* the return transit (async window behind the windowless
    // cabin), so while the crew is still riding, the card stays; the actual retirement
    // happens when the player steps off the van (NotifyDisembarked from the overlay).
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (VanTransitOverlay.IsActive) return;
        hasCard = false;
        visible = false;
    }

    /// <summary>Called by VanTransitOverlay when the cabin tears down — the ledger takes over.</summary>
    public static void NotifyDisembarked()
    {
        if (current == null) return;
        current.hasCard = false;
        current.visible = false;
    }

    void Update()
    {
        if (!hasCard) return;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (visible && (keyboard.eKey.wasPressedThisFrame || keyboard.escapeKey.wasPressedThisFrame))
        {
            visible = false;
            lastFoldFrame = Time.frameCount;
        }
        else if (!visible && keyboard.tabKey.wasPressedThisFrame)
        {
            visible = true;
            // Re-open keeps the stamp already fallen — no second slam.
            shownAt = Time.unscaledTime - (StampDelaySeconds + StampSlamSeconds + SlideSeconds);
        }

        // 印章落纸: the settlement becomes official the moment the stamp hits the card.
        if (visible && !stampSoundPlayed && Time.unscaledTime - shownAt >= StampDelaySeconds)
        {
            stampSoundPlayed = true;
            AudioManager.Instance?.PlayStamp();
        }
    }

    void OnGUI()
    {
        if (!hasCard || !visible) return;
        EnsureStyles();
        if (headerStyle == null) return;

        GUI.depth = -100; // above the transit ticket strip

        float age = Time.unscaledTime - shownAt;
        float slide = Mathf.Clamp01(age / SlideSeconds);
        float ease = 1f - (1f - slide) * (1f - slide);

        float w = 440f;
        float noteH = string.IsNullOrEmpty(clientNote) ? 0f : 84f;
        float h = 150f + rows.Length * 30f + CountSublines() * 18f + noteH + 44f;
        float targetY = Screen.height - h - 150f;
        var card = new Rect((Screen.width - w) * 0.5f, Mathf.Lerp(Screen.height, targetY, ease), w, h);

        // Paper card with civic-teal header band (盖章公文卡 grammar).
        GUI.DrawTexture(new Rect(card.x - 3f, card.y - 3f, card.width + 6f, card.height + 6f),
            BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.Shadow));
        GUI.DrawTexture(card, BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.OldPaper));
        var headerBand = new Rect(card.x, card.y, card.width, 56f);
        GUI.DrawTexture(headerBand, BlackCommissionUiTheme.MakeTex(BlackCommissionUiTheme.MilitaryGreen));

        GUI.Label(new Rect(card.x + 16f, card.y + 8f, card.width - 32f, 22f),
            "黑色委托事务所  ·  委托结算单", headerStyle);
        GUI.Label(new Rect(card.x + 16f, card.y + 30f, card.width - 32f, 20f),
            $"单号 {docNumber}    {commissionTitle}", headerStyle);

        // Itemized rows.
        float y = card.y + 70f;
        foreach (Row row in rows)
        {
            if (row.isNet)
            {
                GUI.DrawTexture(new Rect(card.x + 16f, y + 2f, card.width - 32f, 1f),
                    BlackCommissionUiTheme.MakeTex(new Color(0.19f, 0.17f, 0.13f, 0.55f)));
                y += 8f;
                GUI.Label(new Rect(card.x + 16f, y, card.width - 140f, 26f), row.label, netStyle);
                GUI.Label(new Rect(card.xMax - 156f, y, 140f, 26f), $"{row.amount}G", netStyle);
                y += 30f;
                continue;
            }

            GUI.Label(new Rect(card.x + 16f, y, card.width - 140f, 22f), row.label, rowStyle);
            GUI.Label(new Rect(card.xMax - 156f, y, 140f, 22f),
                row.amount >= 0 ? $"+{row.amount}G" : $"−{-row.amount}G", rowStyle);
            y += 26f;
            if (!string.IsNullOrEmpty(row.subline))
            {
                GUI.Label(new Rect(card.x + 30f, y, card.width - 60f, 16f), $"· {row.subline}", sublineStyle);
                y += 18f;
            }
        }

        // Client usage note (satire carrier; hidden when unset — no empty frame).
        if (noteH > 0f)
        {
            var note = new Rect(card.x + 16f, y + 6f, card.width - 32f, noteH - 12f);
            DrawDashedFrame(note);
            GUI.Label(new Rect(note.x + 8f, note.y + 4f, note.width - 16f, 16f), "客户使用备注", sublineStyle);
            GUI.Label(new Rect(note.x + 10f, note.y + 22f, note.width - 20f, note.height - 26f),
                $"“{clientNote}”", noteStyle);
            y += noteH;
        }

        GUI.Label(new Rect(card.x + 16f, card.yMax - 32f, card.width - 32f, 20f),
            "回所后凭本单至办公终端入账    [E] 收起   [Tab] 重看", footerStyle);

        // Outcome stamp: slams in at StampDelay, slightly rotated, over the table's top-right.
        if (age >= StampDelaySeconds)
        {
            float slam = Mathf.Clamp01((age - StampDelaySeconds) / StampSlamSeconds);
            float scale = Mathf.Lerp(1.3f, 1f, slam);
            DrawStamp(new Vector2(card.xMax - 86f, card.y + 92f), scale,
                Mathf.Lerp(0.4f, 0.92f, slam));
        }
    }

    int CountSublines()
    {
        int n = 0;
        foreach (Row row in rows)
            if (!string.IsNullOrEmpty(row.subline)) n++;
        return n;
    }

    void DrawStamp(Vector2 center, float scale, float alpha)
    {
        Matrix4x4 prev = GUI.matrix;
        Color prevColor = GUI.color;
        GUIUtility.RotateAroundPivot(-8f, center);
        GUI.color = new Color(1f, 1f, 1f, alpha);

        float sw = 118f * scale, sh = 44f * scale;
        var box = new Rect(center.x - sw * 0.5f, center.y - sh * 0.5f, sw, sh);
        // Hollow stamp frame: red border + red text, paper showing through.
        Color red = BlackCommissionUiTheme.RustWarning;
        Texture2D redTex = BlackCommissionUiTheme.MakeTex(red);
        GUI.DrawTexture(new Rect(box.x, box.y, box.width, 3f), redTex);
        GUI.DrawTexture(new Rect(box.x, box.yMax - 3f, box.width, 3f), redTex);
        GUI.DrawTexture(new Rect(box.x, box.y, 3f, box.height), redTex);
        GUI.DrawTexture(new Rect(box.xMax - 3f, box.y, 3f, box.height), redTex);
        GUI.Label(box, stampLabel, stampStyle);

        GUI.color = prevColor;
        GUI.matrix = prev;
    }

    void DrawDashedFrame(Rect r)
    {
        Texture2D ink = BlackCommissionUiTheme.MakeTex(new Color(0.19f, 0.17f, 0.13f, 0.45f));
        const float dash = 6f, gap = 4f;
        for (float x = r.x; x < r.xMax; x += dash + gap)
        {
            GUI.DrawTexture(new Rect(x, r.y, Mathf.Min(dash, r.xMax - x), 1f), ink);
            GUI.DrawTexture(new Rect(x, r.yMax - 1f, Mathf.Min(dash, r.xMax - x), 1f), ink);
        }
        for (float y = r.y; y < r.yMax; y += dash + gap)
        {
            GUI.DrawTexture(new Rect(r.x, y, 1f, Mathf.Min(dash, r.yMax - y)), ink);
            GUI.DrawTexture(new Rect(r.xMax - 1f, y, 1f, Mathf.Min(dash, r.yMax - y)), ink);
        }
    }

    void EnsureStyles()
    {
        if (headerStyle != null) return;
        if (GUI.skin == null || GUI.skin.label == null) return;

        Color ink = new Color(0.10f, 0.095f, 0.075f, 1f);
        Color inkSoft = new Color(0.19f, 0.17f, 0.13f, 0.9f);

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 14, fontStyle = FontStyle.Bold,
            normal = { textColor = BlackCommissionUiTheme.OldPaper }
        };
        rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = ink } };
        sublineStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = inkSoft } };
        netStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19, fontStyle = FontStyle.Bold, normal = { textColor = ink }
        };
        noteStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 13, fontStyle = FontStyle.Italic, wordWrap = true,
            normal = { textColor = ink }
        };
        footerStyle = new GUIStyle(GUI.skin.label) { fontSize = 11, normal = { textColor = inkSoft } };
        stampStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
            normal = { textColor = BlackCommissionUiTheme.RustWarning }
        };
        MvpFontProvider.ApplyToStyle(headerStyle);
        MvpFontProvider.ApplyToStyle(rowStyle);
        MvpFontProvider.ApplyToStyle(sublineStyle);
        MvpFontProvider.ApplyToStyle(netStyle);
        MvpFontProvider.ApplyToStyle(noteStyle);
        MvpFontProvider.ApplyToStyle(footerStyle);
        MvpFontProvider.ApplyToStyle(stampStyle);
    }
}
