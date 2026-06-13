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
    // Minimum outbound transit (boarding-transit spec): the ride lasts at least this long
    // even if the mission scene loads faster; if loading runs longer, the ride stretches.
    [SerializeField] float dispatchTransitSeconds = 8f;

    // 「已签发」章砸落 → 卡片收起的演出节拍; the scene load starts just after it so the
    // stamp moment never lands on a load hitch frame.
    const float SignatureBeatSeconds = 0.9f;

    static int storedFlashlights;
    static int storedBatteries;

    bool missionLaunching;
    public NetworkVariable<int> StoredFlashlightCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> StoredBatteryCount = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public OfficeTaskDefinition DemoTask => ResolveDemoTask();
    public bool HasSelectedDemoTask => ResolveDemoTask() != null && MvpMissionRuntime.SelectedTask == ResolveDemoTask();
    public string DemoTaskTitle => demoTask != null ? demoTask.title : "The Forgotten Homework";
    public string DemoTaskClient => demoTask != null ? demoTask.client : "Parent";
    public string DemoTaskDescription => demoTask != null ? demoTask.description : "Go to the school and retrieve the forgotten homework, then evacuate safely.";
    public string DemoTaskLocation => demoTask != null ? demoTask.locationName : "School";
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
                    return "Waiting for host to collect settlement";
                return "Open commission terminal: collect settlement";
            }

            if (MvpMissionRuntime.HasSelectedTask)
                return "Open commission terminal: view locked commission";

            if (CompanyData.Current.CanShowTutorialAcquisition)
                return "Open commission terminal: office acquisition file";

            return "Open commission terminal";
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
            message = "Commission van is being dispatched.";
            return false;
        }

        if (MvpPendingReward.HasPending)
        {
            message = "Collect the previous settlement first.";
            return false;
        }

        if (CompanyData.Current.CanAffordTutorialAcquisition)
        {
            message = "Handle the office acquisition file first.";
            return false;
        }

        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening)
        {
            if (allowNonNetworkSoloStart && CanStartDemoTask(out message))
            {
                QueueDemoTask();
                message = $"Commission accepted: {DemoTaskTitle}";
                return MvpMissionRuntime.HasSelectedTask;
            }

            message = "Create an office session first.";
            return false;
        }

        if (!network.IsHost)
        {
            message = "Only the host can accept commissions.";
            return false;
        }

        if (!CanStartDemoTask(out message))
            return false;

        QueueDemoTask();
        bool accepted = MvpMissionRuntime.HasSelectedTask;
        message = accepted ? $"Commission accepted: {DemoTaskTitle}" : "Commission lock failed.";
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
        VanTransitOverlay.ShowOutbound(task.title, task.locationName, Mathf.Max(1.5f, dispatchTransitSeconds));
        StartCoroutine(LoadMissionLocalAfterSignature(task.sceneName));
    }

    IEnumerator LoadMissionLocalAfterSignature(string sceneName)
    {
        yield return new WaitForSecondsRealtime(SignatureBeatSeconds);
        SceneManager.LoadSceneAsync(sceneName);
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
        StartCoroutine(LoadMissionAfterSignature(task.sceneName));
    }

    // Sign-and-load (boarding-transit spec): the mission scene starts loading right after the
    // stamp beat — the crew stays seated in the DontDestroyOnLoad cabin through the swap and
    // VanTransitOverlay opens the rear door only when the scene is ready AND the minimum
    // transit (dispatchTransitSeconds) has elapsed. No black screen, no 2D loading page.
    IEnumerator LoadMissionAfterSignature(string sceneName)
    {
        yield return new WaitForSecondsRealtime(SignatureBeatSeconds);

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
            reason = "No commission configured for this terminal.";
            return false;
        }
        if (CompanyData.Current.OfficeLevel < demoTask.requiredOfficeLevel)
        {
            reason = $"Office level too low: requires LV.{demoTask.requiredOfficeLevel}.";
            return false;
        }
        if (CompanyData.Current.Reputation < demoTask.minimumReputation)
        {
            reason = $"Insufficient reputation: requires {demoTask.minimumReputation}.";
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
