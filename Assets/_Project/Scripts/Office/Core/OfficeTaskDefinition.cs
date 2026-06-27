using UnityEngine;

/// <summary>
/// Commission tier (scavenging-core-loop §3.5). Free Salvage pays market rate for everything;
/// Commissioned / Black favour 1-2 item categories that settle at the client-preference
/// multiplier and carry the per-item settlement satire. Black differs from Commissioned in
/// narrative register only, not mechanics.
/// </summary>
public enum CommissionClientType { FreeSalvage, Commissioned, BlackCommission }

[CreateAssetMenu(menuName = "Black Commission/Office Task Definition")]
public class OfficeTaskDefinition : ScriptableObject
{
    [Header("Identity")]
    public string taskId = "tower_ecocolumn_01";
    public string title = "「真实海岸」生态柱回收";
    public MvpTaskCategory category = MvpTaskCategory.LostItemRecovery;

    [Header("Brief")]
    [TextArea] public string client = "私人收藏家";
    [TextArea] public string description = "潜入地球海岸壹号烂尾楼，取回封存的「真实海岸」生态柱并撤离。";
    public string locationName = "地球海岸壹号·烂尾楼";
    public string sceneName = "Tower_EarthCoast_01";
    public int recommendedPlayersMin = 1;
    public int recommendedPlayersMax = 4;

    [Header("Client Preference (scavenging — scavenging-core-loop §3.4)")]
    [Tooltip("Free Salvage pays market rate for everything; Commissioned/Black favour the categories below.")]
    public CommissionClientType clientType = CommissionClientType.FreeSalvage;
    [Tooltip("Item-category IDs (cast from ScavengeCategory) this client favours — set by the commission builder. " +
             "Items in these categories settle at ScavengingConfig.clientPreferenceMultiplier on Commissioned/Black runs; " +
             "empty = market rate. Stored as int (not ScavengeCategory[]) so Office.Core need not reference the Scavenge assembly.")]
    public int[] favouredCategoryIds;

    // TODO: gate by license stage (game-pillars.md) — requiredOfficeLevel and minimumReputation removed 2026-06-17.

    [Header("Schedule")]
    [Tooltip("Game clock hour when the crew clocks in. 8 = 08:00.")]
    public float missionStartClockHour = 8f;
    [Tooltip("How many in-game hours the standard contract window lasts.")]
    public float contractWindowGameHours = 12f;
    [Tooltip("Real seconds per in-game hour. 60 means 12 real minutes equals 12 in-game hours.")]
    public float realSecondsPerGameHour = 60f;
    public int overtimeMoneyPenaltyPerGameHour = 30;

    [Header("Rewards")]
    public int moneyReward = 300;
    public int failureConsolationMoney = 20;

    [Header("Settlement Notes (结算单·客户使用备注)")]
    [Tooltip("Per result kind; one entry is picked (deterministic across peers). Empty hides the note block. design/ux/settlement.md")]
    [TextArea] public string[] settlementNotesSuccess;
    [TextArea] public string[] settlementNotesPartial;
    [TextArea] public string[] settlementNotesFailure;

    /// <summary>Client usage note for the settlement card; deterministic pick so all peers read the same line.</summary>
    public string GetSettlementNote(MvpMissionResultKind kind, int seed)
    {
        string[] pool = kind switch
        {
            MvpMissionResultKind.Success => settlementNotesSuccess,
            MvpMissionResultKind.Partial => settlementNotesPartial,
            _ => settlementNotesFailure
        };
        if (pool == null || pool.Length == 0) return null;
        return pool[Mathf.Abs(seed) % pool.Length];
    }

    /// <summary>True when an item category (cast to int from ScavengeCategory) earns the
    /// client-preference multiplier this run. Free Salvage favours nothing.</summary>
    public bool FavoursCategoryId(int categoryId)
    {
        if (clientType == CommissionClientType.FreeSalvage || favouredCategoryIds == null) return false;
        for (int i = 0; i < favouredCategoryIds.Length; i++)
            if (favouredCategoryIds[i] == categoryId) return true;
        return false;
    }
}
