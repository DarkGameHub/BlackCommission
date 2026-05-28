using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkObject))]
public class LostItemMissionManager : NetworkBehaviour
{
    public static LostItemMissionManager Instance { get; private set; }

    public enum MissionPhase
    {
        Searching,
        ReturnToExit,
        Completed,
        ReturnedEarly,
        Failed
    }

    [Header("Fallback Rewards")]
    [SerializeField] int fallbackMoneyReward = 300;
    [SerializeField] int fallbackReputationReward = 5;
    [SerializeField] int fallbackExperienceReward = 80;
    [SerializeField] int fallbackPartialMoneyReward = 60;
    [SerializeField] int fallbackPartialReputationReward = 0;
    [SerializeField] int fallbackPartialExperienceReward = 15;
    [SerializeField] int bonusEvidenceMoneyReward = 90;
    [SerializeField] int bonusEvidenceReputationReward = 1;
    [SerializeField] int bonusEvidenceExperienceReward = 20;
    [SerializeField] int fallbackFailureMoney = 20;
    [SerializeField] int fallbackFailureReputation = -2;
    [SerializeField] int fallbackFailureExperience = 0;

    [Header("Scene Flow")]
    [SerializeField] string fallbackOfficeScene = "HQ";

    public NetworkVariable<MissionPhase> CurrentPhase = new(MissionPhase.Searching,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> LostItemCollected = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<ulong> CarrierClientId = new(ulong.MaxValue,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> BonusEvidenceCollected = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> SchoolEntranceOpened = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<int> WrongHomeworkAttempts = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> RewardsGranted = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> MissionTimer = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    OfficeTaskDefinition ActiveTask => MvpMissionRuntime.ActiveTask;
    public float ElapsedGameHours => MvpMissionClock.GetElapsedGameHours(ActiveTask, MissionTimer.Value);
    public float CurrentClockHour => MvpMissionClock.GetCurrentClockHour(ActiveTask, MissionTimer.Value);
    public float DeadlineClockHour => MvpMissionClock.GetDeadlineClockHour(ActiveTask);
    public float RemainingGameHours => MvpMissionClock.GetRemainingGameHours(ActiveTask, MissionTimer.Value);
    public float OvertimeGameHours => MvpMissionClock.GetOvertimeGameHours(ActiveTask, MissionTimer.Value);
    public bool IsOvertime => OvertimeGameHours > 0f;
    public int OvertimeMoneyPenalty => MvpMissionClock.GetOvertimeMoneyPenalty(ActiveTask, MissionTimer.Value);
    public int OvertimeReputationPenalty => MvpMissionClock.GetOvertimeReputationPenalty(ActiveTask, MissionTimer.Value);
    public int WrongHomeworkMoneyPenalty => GetWrongHomeworkMoneyPenalty();
    public string CurrentClockLabel => MvpMissionClock.FormatClock(CurrentClockHour);
    public string DeadlineClockLabel => MvpMissionClock.FormatClock(DeadlineClockHour);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        SchoolEntranceOpened.OnValueChanged += HandleSchoolEntranceOpenedChanged;
        if (SchoolEntranceOpened.Value)
            SchoolEntranceDoor.SetAllOpen(true);

        if (!IsServer) return;
        CurrentPhase.Value = MissionPhase.Searching;
        LostItemCollected.Value = false;
        CarrierClientId.Value = ulong.MaxValue;
        BonusEvidenceCollected.Value = false;
        SchoolEntranceOpened.Value = false;
        WrongHomeworkAttempts.Value = 0;
        RewardsGranted.Value = false;
        MissionTimer.Value = 0f;
    }

    public override void OnNetworkDespawn()
    {
        SchoolEntranceOpened.OnValueChanged -= HandleSchoolEntranceOpenedChanged;
    }

    void Update()
    {
        if (!HasMissionAuthority) return;
        if (CurrentPhase.Value == MissionPhase.Completed ||
            CurrentPhase.Value == MissionPhase.ReturnedEarly ||
            CurrentPhase.Value == MissionPhase.Failed) return;

        MissionTimer.Value += Time.deltaTime;
        CheckAllPlayersDowned();
    }

    public void TryCollectItem(ulong clientId, NetworkObjectReference itemRef)
    {
        if (!HasMissionAuthority) return;
        if (LostItemCollected.Value) return;
        if (CurrentPhase.Value != MissionPhase.Searching) return;

        LostItemCollected.Value = true;
        CarrierClientId.Value = clientId;
        CurrentPhase.Value = MissionPhase.ReturnToExit;
        if (IsServer)
            HideCollectedItemClientRpc(itemRef, clientId);
    }

    public void RequestOpenSchoolEntrance()
    {
        if (!HasMissionAuthority && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            RequestOpenSchoolEntranceServerRpc();
            return;
        }

        OpenSchoolEntrance();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestOpenSchoolEntranceServerRpc()
    {
        OpenSchoolEntrance();
    }

    void OpenSchoolEntrance()
    {
        if (!HasMissionAuthority) return;
        SchoolEntranceOpened.Value = true;
        SchoolEntranceDoor.SetAllOpen(true);
    }

    void HandleSchoolEntranceOpenedChanged(bool previousValue, bool newValue)
    {
        SchoolEntranceDoor.SetAllOpen(newValue);
    }

    public void RequestWrongHomeworkAttempt(Vector3 itemPosition, float requiredDistance = 3f)
    {
        if (!HasMissionAuthority && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            RequestWrongHomeworkAttemptServerRpc(itemPosition, requiredDistance);
            return;
        }

        TryRegisterWrongHomeworkAttempt(0, itemPosition, requiredDistance);
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestWrongHomeworkAttemptServerRpc(Vector3 itemPosition, float requiredDistance, ServerRpcParams rpcParams = default)
    {
        TryRegisterWrongHomeworkAttempt(rpcParams.Receive.SenderClientId, itemPosition, requiredDistance);
    }

    void TryRegisterWrongHomeworkAttempt(ulong clientId, Vector3 itemPosition, float requiredDistance)
    {
        if (!HasMissionAuthority) return;
        if (CurrentPhase.Value != MissionPhase.Searching) return;
        if (BonusEvidenceCollected.Value) return;
        if (!IsPlayerNearExit(clientId, itemPosition, requiredDistance)) return;
        if (WrongHomeworkAttempts.Value >= 3) return;

        WrongHomeworkAttempts.Value += 1;
        SchoolMonsterAI.TryDistractNearest(itemPosition, 18f, 6f);
    }

    public void RequestCollectBonusEvidence(Vector3 evidencePosition, float requiredDistance = 3f)
    {
        if (!HasMissionAuthority && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            RequestCollectBonusEvidenceServerRpc(evidencePosition, requiredDistance);
            return;
        }

        TryCollectBonusEvidence(0, evidencePosition, requiredDistance);
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestCollectBonusEvidenceServerRpc(Vector3 evidencePosition, float requiredDistance, ServerRpcParams rpcParams = default)
    {
        TryCollectBonusEvidence(rpcParams.Receive.SenderClientId, evidencePosition, requiredDistance);
    }

    void TryCollectBonusEvidence(ulong clientId, Vector3 evidencePosition, float requiredDistance)
    {
        if (!HasMissionAuthority) return;
        if (BonusEvidenceCollected.Value) return;
        if (CurrentPhase.Value == MissionPhase.Completed ||
            CurrentPhase.Value == MissionPhase.ReturnedEarly ||
            CurrentPhase.Value == MissionPhase.Failed) return;
        if (!IsPlayerNearExit(clientId, evidencePosition, requiredDistance)) return;

        BonusEvidenceCollected.Value = true;
    }

    public void TryExitMission(ulong requestingClientId, Vector3 exitPosition, float requiredDistance, float returnTransitSeconds = 6f)
    {
        if (!HasMissionAuthority) return;
        if (CurrentPhase.Value == MissionPhase.Completed ||
            CurrentPhase.Value == MissionPhase.ReturnedEarly ||
            CurrentPhase.Value == MissionPhase.Failed) return;
        if (!IsPlayerNearExit(requestingClientId, exitPosition, requiredDistance)) return;

        if (LostItemCollected.Value)
        {
            if (CarrierClientId.Value != requestingClientId) return;
            CurrentPhase.Value = MissionPhase.Completed;
            GrantRewardsAndReturn(true, MvpMissionResultKind.Success, returnTransitSeconds);
            return;
        }

        CurrentPhase.Value = MissionPhase.ReturnedEarly;
        GrantRewardsAndReturn(false, MvpMissionResultKind.Partial, returnTransitSeconds);
    }

    public void FailMission()
    {
        if (!HasMissionAuthority) return;
        if (CurrentPhase.Value == MissionPhase.Completed ||
            CurrentPhase.Value == MissionPhase.ReturnedEarly ||
            CurrentPhase.Value == MissionPhase.Failed) return;

        CurrentPhase.Value = MissionPhase.Failed;
        GrantRewardsAndReturn(false, MvpMissionResultKind.Failed, 2.5f);
    }

    void GrantRewardsAndReturn(bool success, MvpMissionResultKind resultKind, float returnTransitSeconds)
    {
        if (RewardsGranted.Value) return;
        RewardsGranted.Value = true;

        OfficeTaskDefinition task = MvpMissionRuntime.ActiveTask;
        int money = GetMoneyForResult(task, resultKind);
        int reputation = GetReputationForResult(task, resultKind);
        int experience = GetExperienceForResult(task, resultKind);
        float overtimeGameHours = MvpMissionClock.GetOvertimeGameHours(task, MissionTimer.Value);
        int overtimeMoneyPenalty = MvpMissionClock.GetOvertimeMoneyPenalty(task, MissionTimer.Value);
        int overtimeReputationPenalty = MvpMissionClock.GetOvertimeReputationPenalty(task, MissionTimer.Value);
        money -= overtimeMoneyPenalty;
        reputation -= overtimeReputationPenalty;
        if (resultKind != MvpMissionResultKind.Failed)
            money -= GetWrongHomeworkMoneyPenalty();
        string officeScene = MvpMissionRuntime.HasActiveTask ? MvpMissionRuntime.ReturnOfficeScene : fallbackOfficeScene;
        string taskTitle = task != null ? task.title : "委托";
        string locationName = task != null ? task.locationName : "任务地点";

        if (IsServer)
        {
            RestorePlayersForOffice();
            ShowReturnTransitClientRpc(taskTitle, locationName, Mathf.Max(1.5f, returnTransitSeconds));
            SetPendingRewardClientRpc(money, reputation, experience, success, MissionTimer.Value, (int)resultKind,
                overtimeGameHours, overtimeMoneyPenalty, overtimeReputationPenalty);
            StartCoroutine(LoadOfficeAfterRewardDispatch(officeScene, Mathf.Max(1.5f, returnTransitSeconds)));
        }
        else
        {
            VanTransitOverlay.ShowReturn(taskTitle, locationName, Mathf.Max(1.5f, returnTransitSeconds));
            MvpPendingReward.Set(money, reputation, experience, success, MissionTimer.Value,
                resultKind == MvpMissionResultKind.Success, resultKind,
                overtimeGameHours, overtimeMoneyPenalty, overtimeReputationPenalty);
            StartCoroutine(LoadOfficeLocalAfterTransit(officeScene, Mathf.Max(1.5f, returnTransitSeconds)));
        }
    }

    [ClientRpc]
    void HideCollectedItemClientRpc(NetworkObjectReference itemRef, ulong carrierClientId)
    {
        if (itemRef.TryGet(out NetworkObject itemObject))
            itemObject.gameObject.SetActive(false);
    }

    void CheckAllPlayersDowned()
    {
        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        if (players.Length == 0) return;

        foreach (var player in players)
        {
            if (player != null && !player.IsDowned.Value)
                return;
        }

        FailMission();
    }

    [ClientRpc]
    void ShowReturnTransitClientRpc(string taskTitle, string locationName, float durationSeconds)
    {
        MvpMissionRuntime.Clear();
        VanTransitOverlay.ShowReturn(taskTitle, locationName, durationSeconds);
    }

    [ClientRpc]
    void SetPendingRewardClientRpc(
        int money,
        int reputation,
        int experience,
        bool success,
        float elapsedSeconds,
        int resultKind,
        float overtimeGameHours,
        int overtimeMoneyPenalty,
        int overtimeReputationPenalty)
    {
        MvpMissionResultKind kind = (MvpMissionResultKind)Mathf.Clamp(resultKind, 0, (int)MvpMissionResultKind.Failed);
        MvpPendingReward.Set(money, reputation, experience, success, elapsedSeconds,
            kind == MvpMissionResultKind.Success, kind,
            overtimeGameHours, overtimeMoneyPenalty, overtimeReputationPenalty);
    }

    int GetMoneyForResult(OfficeTaskDefinition task, MvpMissionResultKind resultKind)
    {
        int reward;
        switch (resultKind)
        {
            case MvpMissionResultKind.Success:
                reward = GetMoneyReward(task);
                break;
            case MvpMissionResultKind.Partial:
                reward = GetPartialMoney(task);
                break;
            default:
                return GetFailureMoney(task);
        }

        return reward + GetBonusMoneyForResult(resultKind);
    }

    int GetReputationForResult(OfficeTaskDefinition task, MvpMissionResultKind resultKind)
    {
        int reward;
        switch (resultKind)
        {
            case MvpMissionResultKind.Success:
                reward = GetReputationReward(task);
                break;
            case MvpMissionResultKind.Partial:
                reward = GetPartialReputation(task);
                break;
            default:
                return GetFailureReputation(task);
        }

        return reward + GetBonusReputationForResult(resultKind);
    }

    int GetExperienceForResult(OfficeTaskDefinition task, MvpMissionResultKind resultKind)
    {
        int reward;
        switch (resultKind)
        {
            case MvpMissionResultKind.Success:
                reward = GetExperienceReward(task);
                break;
            case MvpMissionResultKind.Partial:
                reward = GetPartialExperience(task);
                break;
            default:
                return GetFailureExperience(task);
        }

        return reward + GetBonusExperienceForResult(resultKind);
    }

    int GetMoneyReward(OfficeTaskDefinition task) => task != null ? task.moneyReward : fallbackMoneyReward;
    int GetReputationReward(OfficeTaskDefinition task) => task != null ? task.reputationReward : fallbackReputationReward;
    int GetExperienceReward(OfficeTaskDefinition task) => task != null ? task.experienceReward : fallbackExperienceReward;
    int GetPartialMoney(OfficeTaskDefinition task) => task != null
        ? Mathf.Max(task.failureConsolationMoney, Mathf.RoundToInt(task.moneyReward * 0.22f))
        : fallbackPartialMoneyReward;
    int GetPartialReputation(OfficeTaskDefinition task) => task != null ? 0 : fallbackPartialReputationReward;
    int GetPartialExperience(OfficeTaskDefinition task) => task != null
        ? Mathf.Max(0, Mathf.RoundToInt(task.experienceReward * 0.2f))
        : fallbackPartialExperienceReward;
    int GetBonusMoneyForResult(MvpMissionResultKind resultKind) =>
        BonusEvidenceCollected.Value && resultKind != MvpMissionResultKind.Failed ? bonusEvidenceMoneyReward : 0;
    int GetBonusReputationForResult(MvpMissionResultKind resultKind) =>
        BonusEvidenceCollected.Value && resultKind == MvpMissionResultKind.Success ? bonusEvidenceReputationReward : 0;
    int GetBonusExperienceForResult(MvpMissionResultKind resultKind) =>
        BonusEvidenceCollected.Value && resultKind != MvpMissionResultKind.Failed ? bonusEvidenceExperienceReward : 0;
    int GetFailureMoney(OfficeTaskDefinition task) => task != null ? task.failureConsolationMoney : fallbackFailureMoney;
    int GetFailureReputation(OfficeTaskDefinition task) => task != null ? task.failureReputationPenalty : fallbackFailureReputation;
    int GetFailureExperience(OfficeTaskDefinition task) => task != null ? task.failureExperience : fallbackFailureExperience;
    int GetWrongHomeworkMoneyPenalty() => Mathf.Min(WrongHomeworkAttempts.Value, 3) * 30;

    bool HasMissionAuthority => IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

    bool IsPlayerNearExit(ulong clientId, Vector3 exitPosition, float requiredDistance)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return true;

        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) &&
            client.PlayerObject != null)
        {
            if (client.PlayerObject.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
                return false;
            return Vector3.Distance(client.PlayerObject.transform.position, exitPosition) <= requiredDistance;
        }

        return false;
    }

    void RestorePlayersForOffice()
    {
        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player != null)
                player.Heal(999f);
        }
    }

    IEnumerator LoadOfficeAfterRewardDispatch(string officeScene, float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        MvpMissionRuntime.Clear();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.LoadScene(officeScene, LoadSceneMode.Single);
        else
            SceneManager.LoadScene(officeScene);
    }

    IEnumerator LoadOfficeLocalAfterTransit(string officeScene, float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        MvpMissionRuntime.Clear();
        SceneManager.LoadScene(officeScene);
    }
}
