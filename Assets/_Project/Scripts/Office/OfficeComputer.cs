using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkObject))]
public class OfficeComputer : NetworkBehaviour, IInteractable
{
    const float OfficeGroundStorageUseDistance = 5.2f;

    [SerializeField] OfficeTaskDefinition demoTask;
    [SerializeField] string returnOfficeScene = "HQ";
    [SerializeField] bool allowNonNetworkSoloStart = false;
    [SerializeField] float dispatchTransitSeconds = 8f;

    static int storedMedkits;
    static int storedDecoys;
    static int storedStunSprays;
    static int storedFlashlights;

    bool missionLaunching;
    public NetworkVariable<int> StoredMedkitCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> StoredDecoyCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> StoredStunSprayCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> StoredFlashlightCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public OfficeTaskDefinition DemoTask => demoTask;
    public bool HasSelectedDemoTask => demoTask != null && MvpMissionRuntime.SelectedTask == demoTask;
    public string DemoTaskTitle => demoTask != null ? demoTask.title : "被遗忘的作业本";
    public string DemoTaskClient => demoTask != null ? demoTask.client : "家长";
    public string DemoTaskDescription => demoTask != null ? demoTask.description : "去学校找回被遗忘的作业本，然后安全撤离。";
    public string DemoTaskLocation => demoTask != null ? demoTask.locationName : "学校";
    public int DemoTaskMoneyReward => demoTask != null ? demoTask.moneyReward : 0;
    public int DemoTaskReputationReward => demoTask != null ? demoTask.reputationReward : 0;
    public int DemoTaskExperienceReward => demoTask != null ? demoTask.experienceReward : 0;

    public override void OnNetworkSpawn()
    {
        if (!IsServer || NetworkManager.Singleton == null) return;
        RestoreGroundStorageCounts();
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        BroadcastCompanyState();
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }

    public string InteractHint
    {
        get
        {
            if (MvpPendingReward.HasPending)
            {
                if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !NetworkManager.Singleton.IsHost)
                    return "等待房主领取结算";
                return "打开委托终端: 领取结算";
            }

            if (MvpMissionRuntime.HasSelectedTask)
                return "打开委托终端: 查看已锁定委托";

            if (CompanyData.Current.CanShowTutorialAcquisition)
                return "打开委托终端: 事务所收购文件";

            return "打开委托终端";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        MvpHud.OpenComputer(this);
    }

    public void ExecuteComputerAction(PlayerController player)
    {
        if (missionLaunching) return;
        if (MvpPendingReward.HasPending)
        {
            if (IsClientOnly) return;
            if (MvpPendingReward.Claim())
                BroadcastCompanyState(clearPendingReward: true);
            return;
        }

        if (CompanyData.Current.CanAffordTutorialAcquisition)
        {
            if (!IsClientOnly && CompanyData.Current.TryAcquireTutorialOffice())
                BroadcastCompanyState();
            return;
        }

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            if (allowNonNetworkSoloStart && CanStartDemoTask())
                QueueDemoTask();
            return;
        }

        if (!CanStartDemoTask()) return;

        if (NetworkManager.Singleton.IsHost)
            QueueDemoTask();
    }

    public void OnInteractEnd(PlayerController player) { }

    public int GetDroppedItemCount(MvpHotbarItemId itemId)
    {
        switch (itemId)
        {
            case MvpHotbarItemId.Medkit:
                return StoredMedkitCount.Value;
            case MvpHotbarItemId.Decoy:
                return StoredDecoyCount.Value;
            case MvpHotbarItemId.StunSpray:
                return StoredStunSprayCount.Value;
            case MvpHotbarItemId.Flashlight:
                return StoredFlashlightCount.Value;
            default:
                return 0;
        }
    }

    public bool TryStoreDroppedItemServer(MvpHotbarItemId itemId, int quantity)
    {
        if (itemId == MvpHotbarItemId.None || quantity <= 0) return false;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening && !IsServer)
            return false;

        SetDroppedItemCount(itemId, GetDroppedItemCount(itemId) + quantity);
        return true;
    }

    public void TryTakeDroppedItem(MvpHotbarItemId itemId)
    {
        if (itemId == MvpHotbarItemId.None || GetDroppedItemCount(itemId) <= 0) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            PlayerHotbar hotbar = Object.FindAnyObjectByType<PlayerHotbar>();
            if (hotbar != null && hotbar.TryReceiveLocalItem(itemId, 1))
                SetDroppedItemCount(itemId, Mathf.Max(0, GetDroppedItemCount(itemId) - 1));
            return;
        }

        RequestTakeDroppedItemServerRpc(itemId);
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestTakeDroppedItemServerRpc(MvpHotbarItemId itemId, ServerRpcParams rpcParams = default)
    {
        if (itemId == MvpHotbarItemId.None || GetDroppedItemCount(itemId) <= 0) return;

        NetworkManager network = NetworkManager.Singleton;
        if (network == null) return;
        if (!network.ConnectedClients.TryGetValue(rpcParams.Receive.SenderClientId, out var client)) return;
        if (client.PlayerObject == null) return;
        if (Vector3.Distance(client.PlayerObject.transform.position, transform.position) > OfficeGroundStorageUseDistance) return;
        if (client.PlayerObject.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value) return;
        if (!client.PlayerObject.TryGetComponent<PlayerHotbar>(out var hotbar)) return;

        if (!hotbar.GrantItemServer(itemId, 1)) return;
        SetDroppedItemCount(itemId, Mathf.Max(0, GetDroppedItemCount(itemId) - 1));
    }

    public void LaunchSelectedMissionFromVehicle(PlayerController player)
    {
        if (missionLaunching) return;
        if (!HasSelectedDemoTask) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            if (allowNonNetworkSoloStart && CanStartDemoTask())
                StartMissionLocalWithTransit();
            return;
        }

        if (!CanStartDemoTask()) return;

        if (NetworkManager.Singleton.IsHost)
            StartMissionServerSideWithTransit();
    }

    void QueueDemoTask()
    {
        if (demoTask == null) return;
        if (!CanStartDemoTask()) return;
        MvpMissionRuntime.SelectMission(demoTask, returnOfficeScene);
        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            SyncSelectedDemoTaskClientRpc();
    }

    void RestoreGroundStorageCounts()
    {
        StoredMedkitCount.Value = storedMedkits;
        StoredDecoyCount.Value = storedDecoys;
        StoredStunSprayCount.Value = storedStunSprays;
        StoredFlashlightCount.Value = storedFlashlights;
    }

    void SetDroppedItemCount(MvpHotbarItemId itemId, int count)
    {
        count = Mathf.Max(0, count);
        switch (itemId)
        {
            case MvpHotbarItemId.Medkit:
                StoredMedkitCount.Value = count;
                storedMedkits = count;
                break;
            case MvpHotbarItemId.Decoy:
                StoredDecoyCount.Value = count;
                storedDecoys = count;
                break;
            case MvpHotbarItemId.StunSpray:
                StoredStunSprayCount.Value = count;
                storedStunSprays = count;
                break;
            case MvpHotbarItemId.Flashlight:
                StoredFlashlightCount.Value = count;
                storedFlashlights = count;
                break;
        }
    }

    void StartMissionLocalWithTransit()
    {
        if (demoTask == null) return;
        if (!CanStartDemoTask()) return;
        if (missionLaunching) return;

        missionLaunching = true;
        MvpMissionRuntime.BeginMission(demoTask, returnOfficeScene);
        float duration = Mathf.Max(1.5f, dispatchTransitSeconds);
        VanTransitOverlay.ShowOutbound(demoTask.title, demoTask.locationName, duration);
        StartCoroutine(LoadMissionLocalAfterTransit(demoTask.sceneName, duration));
    }

    IEnumerator LoadMissionLocalAfterTransit(string sceneName, float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        SceneManager.LoadScene(sceneName);
    }

    void StartMissionServerSideWithTransit()
    {
        if (!IsServer || demoTask == null) return;
        if (!CanStartDemoTask()) return;
        if (missionLaunching) return;

        missionLaunching = true;
        MvpMissionRuntime.BeginMission(demoTask, returnOfficeScene);
        ShowDispatchTransitClientRpc(
            demoTask.title,
            demoTask.locationName,
            Mathf.Max(1.5f, dispatchTransitSeconds));
        StartCoroutine(LoadMissionAfterTransit(demoTask.sceneName, Mathf.Max(1.5f, dispatchTransitSeconds)));
    }

    IEnumerator LoadMissionAfterTransit(string sceneName, float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    [ClientRpc]
    void ShowDispatchTransitClientRpc(string taskTitle, string locationName, float durationSeconds)
    {
        if (demoTask != null)
            MvpMissionRuntime.BeginMission(demoTask, returnOfficeScene);
        VanTransitOverlay.ShowOutbound(taskTitle, locationName, durationSeconds);
    }

    [ClientRpc]
    void SyncSelectedDemoTaskClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (demoTask != null)
            MvpMissionRuntime.SelectMission(demoTask, returnOfficeScene);
    }

    bool CanStartDemoTask()
    {
        if (demoTask == null) return false;
        if (CompanyData.Current.OfficeLevel < demoTask.requiredOfficeLevel) return false;
        if (CompanyData.Current.Reputation < demoTask.minimumReputation) return false;
        return true;
    }

    bool IsClientOnly => NetworkManager.Singleton != null &&
        NetworkManager.Singleton.IsListening &&
        !NetworkManager.Singleton.IsHost;

    void HandleClientConnected(ulong clientId)
    {
        BroadcastCompanyState();
        if (!HasSelectedDemoTask) return;

        var clientRpcParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        };
        SyncSelectedDemoTaskClientRpc(clientRpcParams);
    }

    void BroadcastCompanyState(bool clearPendingReward = false)
    {
        if (!IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return;

        CompanyState c = CompanyData.Current;
        SyncCompanyStateClientRpc(
            c.Funds,
            c.Reputation,
            c.OfficeLevel,
            c.Experience,
            c.Debt,
            c.CompletedLostItemJobs,
            c.FailedJobs,
            c.HostileTakeoverPressure,
            c.HasAcquiredTutorialOffice,
            c.LastMissionSucceeded,
            c.WasRecentlyHostileAcquired,
            c.HasHostileTakeoverUltimatum,
            c.WasRecentlyIssuedTakeoverUltimatum,
            c.LastMissionTimeSeconds,
            clearPendingReward);
    }

    [ClientRpc]
    void SyncCompanyStateClientRpc(
        int funds,
        int reputation,
        int officeLevel,
        int experience,
        int debt,
        int completedLostItemJobs,
        int failedJobs,
        int hostileTakeoverPressure,
        bool hasAcquiredTutorialOffice,
        bool lastMissionSucceeded,
        bool wasRecentlyHostileAcquired,
        bool hasHostileTakeoverUltimatum,
        bool wasRecentlyIssuedTakeoverUltimatum,
        float lastMissionTimeSeconds,
        bool clearPendingReward)
    {
        CompanyData.ApplySnapshot(
            funds,
            reputation,
            officeLevel,
            experience,
            debt,
            completedLostItemJobs,
            failedJobs,
            hostileTakeoverPressure,
            hasAcquiredTutorialOffice,
            lastMissionSucceeded,
            wasRecentlyHostileAcquired,
            hasHostileTakeoverUltimatum,
            wasRecentlyIssuedTakeoverUltimatum,
            lastMissionTimeSeconds);
        if (clearPendingReward)
            MvpPendingReward.Clear();
    }
}
