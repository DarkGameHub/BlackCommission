using UnityEngine;

[CreateAssetMenu(menuName = "Black Commission/Office Task Definition")]
public class OfficeTaskDefinition : ScriptableObject
{
    [Header("Identity")]
    public string taskId = "lost_homework_01";
    public string title = "Missing Homework Notebook";
    public MvpTaskCategory category = MvpTaskCategory.LostItemRecovery;

    [Header("Brief")]
    [TextArea] public string client = "Worried Parent";
    [TextArea] public string description = "Recover the homework notebook from the school and get out.";
    public string locationName = "山间湖泊";
    public string sceneName = "Lake_DiveKey_01";
    public int recommendedPlayersMin = 1;
    public int recommendedPlayersMax = 4;

    [Header("Requirements")]
    public int requiredOfficeLevel = 1;
    public int minimumReputation = -100;

    [Header("Schedule")]
    [Tooltip("Game clock hour when the crew clocks in. 8 = 08:00.")]
    public float missionStartClockHour = 8f;
    [Tooltip("How many in-game hours the standard contract window lasts.")]
    public float contractWindowGameHours = 12f;
    [Tooltip("Real seconds per in-game hour. 60 means 12 real minutes equals 12 in-game hours.")]
    public float realSecondsPerGameHour = 60f;
    public int overtimeMoneyPenaltyPerGameHour = 30;
    public float overtimeReputationPenaltyBlockGameHours = 2f;
    public int overtimeReputationPenaltyPerBlock = 1;

    [Header("Rewards")]
    public int moneyReward = 300;
    public int reputationReward = 5;
    public int experienceReward = 80;
    public int failureConsolationMoney = 20;
    public int failureReputationPenalty = -2;
    public int failureExperience = 0;
}
