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
        Failed
    }

    [Header("Fallback Rewards")]
    [SerializeField] int fallbackMoneyReward = 300;
    [SerializeField] int fallbackReputationReward = 5;
    [SerializeField] int fallbackExperienceReward = 80;
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

    public NetworkVariable<bool> RewardsGranted = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> MissionTimer = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        CurrentPhase.Value = MissionPhase.Searching;
        LostItemCollected.Value = false;
        CarrierClientId.Value = ulong.MaxValue;
        RewardsGranted.Value = false;
        MissionTimer.Value = 0f;
    }

    void Update()
    {
        if (!HasMissionAuthority) return;
        if (CurrentPhase.Value == MissionPhase.Completed || CurrentPhase.Value == MissionPhase.Failed) return;

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

    public void TryExitMission(ulong requestingClientId, Vector3 exitPosition, float requiredDistance)
    {
        if (!HasMissionAuthority) return;
        if (!LostItemCollected.Value) return;
        if (CurrentPhase.Value == MissionPhase.Completed || CurrentPhase.Value == MissionPhase.Failed) return;
        if (CarrierClientId.Value != requestingClientId) return;
        if (!IsPlayerNearExit(requestingClientId, exitPosition, requiredDistance)) return;

        CurrentPhase.Value = MissionPhase.Completed;
        GrantRewardsAndReturn(true);
    }

    public void FailMission()
    {
        if (!HasMissionAuthority) return;
        if (CurrentPhase.Value == MissionPhase.Completed || CurrentPhase.Value == MissionPhase.Failed) return;

        CurrentPhase.Value = MissionPhase.Failed;
        GrantRewardsAndReturn(false);
    }

    void GrantRewardsAndReturn(bool success)
    {
        if (RewardsGranted.Value) return;
        RewardsGranted.Value = true;

        OfficeTaskDefinition task = MvpMissionRuntime.ActiveTask;
        int money = success ? GetMoneyReward(task) : GetFailureMoney(task);
        int reputation = success ? GetReputationReward(task) : GetFailureReputation(task);
        int experience = success ? GetExperienceReward(task) : GetFailureExperience(task);
        string officeScene = MvpMissionRuntime.HasActiveTask ? MvpMissionRuntime.ReturnOfficeScene : fallbackOfficeScene;

        if (IsServer)
        {
            RestorePlayersForOffice();
            SetPendingRewardClientRpc(money, reputation, experience, success, MissionTimer.Value);
            StartCoroutine(LoadOfficeAfterRewardDispatch(officeScene));
        }
        else
        {
            MvpPendingReward.Set(money, reputation, experience, success, MissionTimer.Value);
            MvpMissionRuntime.Clear();
            SceneManager.LoadScene(officeScene);
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
    void SetPendingRewardClientRpc(int money, int reputation, int experience, bool success, float elapsedSeconds)
    {
        MvpPendingReward.Set(money, reputation, experience, success, elapsedSeconds);
    }

    int GetMoneyReward(OfficeTaskDefinition task) => task != null ? task.moneyReward : fallbackMoneyReward;
    int GetReputationReward(OfficeTaskDefinition task) => task != null ? task.reputationReward : fallbackReputationReward;
    int GetExperienceReward(OfficeTaskDefinition task) => task != null ? task.experienceReward : fallbackExperienceReward;
    int GetFailureMoney(OfficeTaskDefinition task) => task != null ? task.failureConsolationMoney : fallbackFailureMoney;
    int GetFailureReputation(OfficeTaskDefinition task) => task != null ? task.failureReputationPenalty : fallbackFailureReputation;
    int GetFailureExperience(OfficeTaskDefinition task) => task != null ? task.failureExperience : fallbackFailureExperience;
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

    IEnumerator LoadOfficeAfterRewardDispatch(string officeScene)
    {
        yield return new WaitForSeconds(0.25f);
        MvpMissionRuntime.Clear();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.LoadScene(officeScene, LoadSceneMode.Single);
        else
            SceneManager.LoadScene(officeScene);
    }
}
