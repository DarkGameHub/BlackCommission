using Unity.Netcode;
using UnityEngine;

public class MvpHud : MonoBehaviour
{
    [SerializeField] int panelWidth = 390;
    [SerializeField] bool showNetworkHint = true;

    GUIStyle panelStyle;
    GUIStyle titleStyle;
    GUIStyle labelStyle;
    GUIStyle mutedStyle;
    GUIStyle accentStyle;
    GUIStyle warningStyle;
    GUIStyle slotStyle;
    GUIStyle selectedSlotStyle;
    Texture2D panelTexture;
    Texture2D slotTexture;
    Texture2D selectedSlotTexture;

    void OnGUI()
    {
        EnsureStyles();

        if (LostItemMissionManager.Instance != null)
        {
            DrawMissionPanel();
            DrawHotbar();
        }
        else
        {
            DrawOfficePanel();
        }

        if (showNetworkHint)
            DrawFooterHint();
    }

    void DrawOfficePanel()
    {
        CompanyState company = CompanyData.Current;
        GUILayout.BeginArea(new Rect(18, 18, panelWidth, 330), GUIContent.none, panelStyle);
        GUILayout.Label("Accident Squad 破产事务所", titleStyle);
        GUILayout.Space(8);
        GUILayout.Label($"资金: {company.Funds} G    债务: {company.Debt} G", company.Funds < 0 ? warningStyle : labelStyle);
        GUILayout.Label($"声望: {company.Reputation}    等级: {company.OfficeLevel}    经验: {company.Experience}/{company.ExperienceForNextLevel}", labelStyle);
        GUILayout.Label($"已解锁委托类别: {company.UnlockedCategoryCount}/8", labelStyle);
        GUILayout.Label($"被吞并压力: {company.HostileTakeoverPressure}/100", company.IsHostileTakeoverRisk ? warningStyle : labelStyle);
        GUILayout.Label($"找回失物委托: {company.CompletedLostItemJobs}/2", labelStyle);
        GUILayout.Space(8);

        if (company.WasRecentlyHostileAcquired)
        {
            GUILayout.Label("警告: 事务所刚被竞对低价吞并，债务重组，等级和委托进度已被压回。", warningStyle);
            GUILayout.Label("继续赚钱和提升声望可以降低下一次被吞并风险。", mutedStyle);
        }
        else if (company.WasRecentlyIssuedTakeoverUltimatum)
        {
            GUILayout.Label("竞对发来收购威胁: 事务所已进入最后通牒状态。", warningStyle);
            GUILayout.Label("下一次失败前最好先把资金或声望拉回安全线。", mutedStyle);
        }
        else if (company.HasHostileTakeoverUltimatum)
        {
            GUILayout.Label("最后通牒: 再失败且资金/声望仍为负，就会被竞对强制吞并。", warningStyle);
            GUILayout.Label("完成委托、攒现金或提高声望可以解除风险。", mutedStyle);
        }
        else if (MvpPendingReward.HasPending)
        {
            string result = MvpPendingReward.Success ? "完成" : "失败";
            int displayedExperience = MvpPendingReward.Success ? MvpPendingReward.Experience : 0;
            GUILayout.Label($"待领取奖励: {result}  金钱 {MvpPendingReward.Money} / 声望 {MvpPendingReward.Reputation} / 经验 {displayedExperience}", accentStyle);
            GUILayout.Label("靠近办公室电脑按 [E] 结算。", mutedStyle);
        }
        else if (company.CanShowTutorialAcquisition)
        {
            GUILayout.Label($"新手扩张: 可吞并 0 级事务所，费用 {company.TutorialAcquisitionCost}G。", company.CanAffordTutorialAcquisition ? accentStyle : warningStyle);
            GUILayout.Label(company.CanAffordTutorialAcquisition
                ? "靠近办公室电脑按 [E] 完成第一次吞并。"
                : "需要足够资金，并且被吞并压力低于 70。", mutedStyle);
        }
        else if (company.HasAcquiredTutorialOffice)
        {
            GUILayout.Label("扩张完成: 已吞并一家 0 级事务所，第二类委托入口已解锁为后续内容。", accentStyle);
            GUILayout.Label("继续接找回失物任务可以积累资金和声望。", mutedStyle);
        }
        else
        {
            GUILayout.Label("靠近办公室电脑按 [E] 接取家长委托。", accentStyle);
            GUILayout.Label("第一个任务: 去学校找回被遗忘的作业本。", mutedStyle);
        }

        GUILayout.Space(8);
        GUILayout.Label("当前 MVP 目标: 接任务 -> 到学校 -> 拾取作业本 -> 躲开追击 -> 撤离回事务所 -> 领取奖励。", mutedStyle);
        GUILayout.EndArea();
    }

    void DrawMissionPanel()
    {
        LostItemMissionManager mission = LostItemMissionManager.Instance;
        GUILayout.BeginArea(new Rect(18, 18, panelWidth, 200), GUIContent.none, panelStyle);
        GUILayout.Label($"用时: {FormatTime(mission.MissionTimer.Value)}", labelStyle);
        GUILayout.Label(GetMissionObjective(mission), accentStyle);
        GUILayout.Label(GetCarrierText(mission), mutedStyle);

        string monsterText = GetMonsterStatus();
        if (!string.IsNullOrEmpty(monsterText))
            GUILayout.Label(monsterText, monsterText.Contains("追击") ? warningStyle : mutedStyle);

        GUILayout.Space(8);
        GUILayout.Label("交互 [E]    使用道具 [Mouse Left]    快捷栏 [1-5]", mutedStyle);
        GUILayout.EndArea();
    }

    void DrawHotbar()
    {
        PlayerHotbar hotbar = FindLocalHotbar();
        if (hotbar == null) return;

        const int slotSize = 92;
        const int gap = 8;
        int totalWidth = PlayerHotbar.SlotCount * slotSize + (PlayerHotbar.SlotCount - 1) * gap;
        float startX = (Screen.width - totalWidth) * 0.5f;
        float y = Screen.height - 92f;

        for (int i = 0; i < PlayerHotbar.SlotCount; i++)
        {
            HotbarSlot slot = hotbar.GetSlot(i);
            bool selected = hotbar.SelectedSlot.Value == i;
            Rect rect = new Rect(startX + i * (slotSize + gap), y, slotSize, 70);
            GUI.Label(rect, GUIContent.none, selected ? selectedSlotStyle : slotStyle);

            string itemName = slot == null || slot.IsEmpty ? "空" : GetItemName(slot.itemId);
            string qty = slot == null || slot.IsEmpty ? "" : $" x{slot.quantity}";
            GUI.Label(new Rect(rect.x + 8, rect.y + 8, rect.width - 16, 18), $"{i + 1}", mutedStyle);
            GUI.Label(new Rect(rect.x + 8, rect.y + 30, rect.width - 16, 22), itemName, selected ? accentStyle : labelStyle);
            GUI.Label(new Rect(rect.x + 8, rect.y + 50, rect.width - 16, 16), qty, mutedStyle);
        }
    }

    void DrawFooterHint()
    {
        string text = "多人 MVP: Start Host 后接任务；最多 4 人由 NetworkManager 玩家上限控制。";
        GUI.Label(new Rect(18, Screen.height - 30, 720, 24), text, mutedStyle);
    }

    static string GetMissionObjective(LostItemMissionManager mission)
    {
        switch (mission.CurrentPhase.Value)
        {
            case LostItemMissionManager.MissionPhase.Searching:
                return "目标: 在教室里找到作业本。";
            case LostItemMissionManager.MissionPhase.ReturnToExit:
                return "目标: 带着作业本回到校门安全区。";
            case LostItemMissionManager.MissionPhase.Completed:
                return "目标: 委托完成，返回事务所领取奖励。";
            case LostItemMissionManager.MissionPhase.Failed:
                return "目标: 委托失败，返回事务所复盘。";
            default:
                return "目标: 等待任务状态。";
        }
    }

    static string GetCarrierText(LostItemMissionManager mission)
    {
        if (!mission.LostItemCollected.Value) return "作业本状态: 尚未找回";
        ulong localId = NetworkManager.Singleton != null ? NetworkManager.Singleton.LocalClientId : 0;
        return mission.CarrierClientId.Value == localId
            ? "作业本状态: 你拿到了，快回校门。"
            : $"作业本状态: 队友 {mission.CarrierClientId.Value} 拿到了。";
    }

    static string GetMonsterStatus()
    {
        SchoolMonsterAI[] monsters = FindObjectsByType<SchoolMonsterAI>(FindObjectsSortMode.None);
        bool anyStunned = false;
        foreach (var monster in monsters)
        {
            if (monster == null) continue;
            if (monster.IsChasing) return "危险: 怪物正在追击。";
            anyStunned |= monster.IsStunned;
            if (monster.IsDistracted) return "危险: 怪物被诱饵短暂吸引。";
        }

        return anyStunned ? "危险: 怪物被短暂控制。" : "危险: 保持安静，别靠太近。";
    }

    static PlayerHotbar FindLocalHotbar()
    {
        PlayerHotbar[] hotbars = FindObjectsByType<PlayerHotbar>(FindObjectsSortMode.None);
        foreach (var hotbar in hotbars)
        {
            if (hotbar != null && hotbar.IsOwner)
                return hotbar;
        }

        return hotbars.Length > 0 ? hotbars[0] : null;
    }

    static string GetItemName(MvpHotbarItemId itemId)
    {
        switch (itemId)
        {
            case MvpHotbarItemId.Medkit:
                return "回血药";
            case MvpHotbarItemId.StunSpray:
                return "定身喷雾";
            case MvpHotbarItemId.Decoy:
                return "诱饵";
            case MvpHotbarItemId.Flashlight:
                return "手电";
            default:
                return "空";
        }
    }

    static string FormatTime(float seconds)
    {
        int total = Mathf.Max(0, Mathf.FloorToInt(seconds));
        return $"{total / 60:00}:{total % 60:00}";
    }

    void EnsureStyles()
    {
        if (panelStyle != null) return;

        panelTexture = MakeTexture(new Color(0.03f, 0.035f, 0.04f, 0.82f));
        slotTexture = MakeTexture(new Color(0.05f, 0.06f, 0.07f, 0.78f));
        selectedSlotTexture = MakeTexture(new Color(0.16f, 0.22f, 0.20f, 0.9f));

        panelStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = panelTexture },
            padding = new RectOffset(16, 16, 14, 14)
        };
        slotStyle = new GUIStyle(GUI.skin.box)
        {
            normal = { background = slotTexture },
            padding = new RectOffset(8, 8, 8, 8)
        };
        selectedSlotStyle = new GUIStyle(slotStyle)
        {
            normal = { background = selectedSlotTexture }
        };
        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.9f, 0.95f, 0.9f) },
            wordWrap = true
        };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            normal = { textColor = new Color(0.86f, 0.88f, 0.84f) },
            wordWrap = true
        };
        mutedStyle = new GUIStyle(labelStyle)
        {
            fontSize = 13,
            normal = { textColor = new Color(0.62f, 0.66f, 0.64f) }
        };
        accentStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.56f, 0.92f, 0.72f) }
        };
        warningStyle = new GUIStyle(labelStyle)
        {
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(1f, 0.5f, 0.42f) }
        };
    }

    static Texture2D MakeTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
