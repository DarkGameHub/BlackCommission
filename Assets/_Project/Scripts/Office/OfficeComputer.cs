using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkObject))]
public class OfficeComputer : NetworkBehaviour, IInteractable
{
    const float OfficeGroundStorageUseDistance = 5.2f;
    const string DefaultTaskResourcePath = "Tasks/SnowLotus_01";

    [SerializeField] OfficeTaskDefinition demoTask;
    [SerializeField] string returnOfficeScene = "HQ";
    [SerializeField] bool allowNonNetworkSoloStart = false;
    [SerializeField] float dispatchTransitSeconds = 8f;

    static int storedFlashlights;
    static int storedBatteries;

    bool missionLaunching;
    public NetworkVariable<int> StoredFlashlightCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> StoredBatteryCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public OfficeTaskDefinition DemoTask => ResolveDemoTask();
    public bool HasSelectedDemoTask => ResolveDemoTask() != null && MvpMissionRuntime.SelectedTask == ResolveDemoTask();
    public string DemoTaskTitle => demoTask != null ? demoTask.title : "被遗忘的作业本";
    public string DemoTaskClient => demoTask != null ? demoTask.client : "家长";
    public string DemoTaskDescription => demoTask != null ? demoTask.description : "去学校找回被遗忘的作业本，然后安全撤离。";
    public string DemoTaskLocation => demoTask != null ? demoTask.locationName : "学校";
    public int DemoTaskMoneyReward => demoTask != null ? demoTask.moneyReward : 0;
    public int DemoTaskReputationReward => demoTask != null ? demoTask.reputationReward : 0;
    public int DemoTaskExperienceReward => demoTask != null ? demoTask.experienceReward : 0;

    OfficeTaskDefinition ResolveDemoTask()
    {
        if (demoTask == null && !string.IsNullOrWhiteSpace(DefaultTaskResourcePath))
            demoTask = Resources.Load<OfficeTaskDefinition>(DefaultTaskResourcePath);
        return demoTask;
    }

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

        TryAcceptDemoTask(out _);
    }

    public bool TryAcceptDemoTask(out string message)
    {
        if (missionLaunching)
        {
            message = "委托车正在调度中。";
            return false;
        }

        if (MvpPendingReward.HasPending)
        {
            message = "先领取上一单结算。";
            return false;
        }

        if (CompanyData.Current.CanAffordTutorialAcquisition)
        {
            message = "先处理事务所收购文件。";
            return false;
        }

        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening)
        {
            if (allowNonNetworkSoloStart && CanStartDemoTask(out message))
            {
                QueueDemoTask();
                message = $"已接受委托: {DemoTaskTitle}";
                return MvpMissionRuntime.HasSelectedTask;
            }

            message = "先创建事务所。";
            return false;
        }

        if (!network.IsHost)
        {
            message = "只有房主能接受委托。";
            return false;
        }

        if (!CanStartDemoTask(out message))
            return false;

        QueueDemoTask();
        bool accepted = MvpMissionRuntime.HasSelectedTask;
        message = accepted ? $"已接受委托: {DemoTaskTitle}" : "委托锁定失败。";
        return accepted;
    }

    public void OnInteractEnd(PlayerController player) { }

    public int GetDroppedItemCount(MvpHotbarItemId itemId)
    {
        return itemId switch
        {
            MvpHotbarItemId.Flashlight => StoredFlashlightCount.Value,
            MvpHotbarItemId.Battery => StoredBatteryCount.Value,
            _ => 0
        };
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
        if (client.PlayerObject.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value) return;
        if (!client.PlayerObject.TryGetComponent<PlayerHotbar>(out var hotbar)) return;

        if (!hotbar.GrantItemServer(itemId, 1)) return;
        SetDroppedItemCount(itemId, Mathf.Max(0, GetDroppedItemCount(itemId) - 1));
    }

    /// <summary>
    /// Departure gate for the HQ van (Space while seated). Outbound to a mission site
    /// requires the whole living crew to be seated — only the host actually launches.
    /// Stranding teammates is only allowed on the return trip, not on the way out.
    /// </summary>
    public void RequestDepart(PlayerController requester)
    {
        if (missionLaunching) return;
        if (!HasSelectedDemoTask) return;

        // Solo / no network: keep the old direct local launch.
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            if (allowNonNetworkSoloStart && CanStartDemoTask())
                StartMissionLocalWithTransit();
            return;
        }

        // Only the host launches; clients pressing Space are no-ops (the HUD shows the count).
        if (!NetworkManager.Singleton.IsHost) return;
        if (!CanStartDemoTask()) return;
        if (!PlayerController.AreAllLivingSeated()) return; // everyone must be aboard

        StartMissionServerSideWithTransit();
    }

    void QueueDemoTask()
    {
        OfficeTaskDefinition task = ResolveDemoTask();
        if (task == null) return;
        if (!CanStartDemoTask()) return;
        MvpMissionRuntime.SelectMission(task, returnOfficeScene);
        if (IsServer && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            SyncSelectedDemoTaskClientRpc();
    }

    void RestoreGroundStorageCounts()
    {
        StoredFlashlightCount.Value = storedFlashlights;
        StoredBatteryCount.Value = storedBatteries;
    }

    void SetDroppedItemCount(MvpHotbarItemId itemId, int count)
    {
        count = Mathf.Max(0, count);
        switch (itemId)
        {
            case MvpHotbarItemId.Flashlight:
                StoredFlashlightCount.Value = count;
                storedFlashlights = count;
                break;
            case MvpHotbarItemId.Battery:
                StoredBatteryCount.Value = count;
                storedBatteries = count;
                break;
        }
    }

    void StartMissionLocalWithTransit()
    {
        OfficeTaskDefinition task = ResolveDemoTask();
        if (task == null) return;
        if (!CanStartDemoTask()) return;
        if (missionLaunching) return;

        missionLaunching = true;
        MvpMissionRuntime.BeginMission(task, returnOfficeScene);
        float duration = Mathf.Max(1.5f, dispatchTransitSeconds);
        VanTransitOverlay.ShowOutbound(task.title, task.locationName, duration);
        StartCoroutine(LoadMissionLocalAfterTransit(task.sceneName, duration));
    }

    IEnumerator LoadMissionLocalAfterTransit(string sceneName, float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);
        SceneManager.LoadScene(sceneName);
    }

    void StartMissionServerSideWithTransit()
    {
        OfficeTaskDefinition task = ResolveDemoTask();
        if (!IsServer || task == null) return;
        if (!CanStartDemoTask()) return;
        if (missionLaunching) return;

        missionLaunching = true;
        MvpMissionRuntime.BeginMission(task, returnOfficeScene);
        ShowDispatchTransitClientRpc(
            task.title,
            task.locationName,
            Mathf.Max(1.5f, dispatchTransitSeconds));
        StartCoroutine(LoadMissionAfterTransit(task.sceneName, Mathf.Max(1.5f, dispatchTransitSeconds)));
    }

    IEnumerator LoadMissionAfterTransit(string sceneName, float delaySeconds)
    {
        yield return new WaitForSecondsRealtime(delaySeconds);

        // Riders un-seat before the scene swaps so they regain movement at the mission spawn.
        PlayerController.ClearAllSeatsServer();

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    [ClientRpc]
    void ShowDispatchTransitClientRpc(string taskTitle, string locationName, float durationSeconds)
    {
        OfficeTaskDefinition task = ResolveDemoTask();
        if (task != null)
            MvpMissionRuntime.BeginMission(task, returnOfficeScene);
        VanTransitOverlay.ShowOutbound(taskTitle, locationName, durationSeconds);
    }

    [ClientRpc]
    void SyncSelectedDemoTaskClientRpc(ClientRpcParams clientRpcParams = default)
    {
        OfficeTaskDefinition task = ResolveDemoTask();
        if (task != null)
            MvpMissionRuntime.SelectMission(task, returnOfficeScene);
    }

    bool CanStartDemoTask()
    {
        return CanStartDemoTask(out _);
    }

    bool CanStartDemoTask(out string reason)
    {
        ResolveDemoTask();
        if (demoTask == null)
        {
            reason = "终端没有配置可用委托。";
            return false;
        }
        if (CompanyData.Current.OfficeLevel < demoTask.requiredOfficeLevel)
        {
            reason = $"事务所等级不足: 需要 LV.{demoTask.requiredOfficeLevel}。";
            return false;
        }
        if (CompanyData.Current.Reputation < demoTask.minimumReputation)
        {
            reason = $"声望不足: 需要 {demoTask.minimumReputation}。";
            return false;
        }
        reason = "";
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
