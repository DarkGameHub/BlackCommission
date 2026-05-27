using UnityEngine;

[CreateAssetMenu(menuName = "AccidentSquad/Office Task Definition")]
public class OfficeTaskDefinition : ScriptableObject
{
    [Header("Identity")]
    public string taskId = "lost_homework_01";
    public string title = "Missing Homework Notebook";
    public MvpTaskCategory category = MvpTaskCategory.LostItemRecovery;

    [Header("Brief")]
    [TextArea] public string client = "Worried Parent";
    [TextArea] public string description = "Recover the homework notebook from the school and get out.";
    public string locationName = "School";
    public string sceneName = "School_LostItem_01";
    public int recommendedPlayersMin = 1;
    public int recommendedPlayersMax = 4;

    [Header("Requirements")]
    public int requiredOfficeLevel = 1;
    public int minimumReputation = -100;

    [Header("Rewards")]
    public int moneyReward = 300;
    public int reputationReward = 5;
    public int experienceReward = 80;
    public int failureConsolationMoney = 20;
    public int failureReputationPenalty = -2;
    public int failureExperience = 0;
}
