using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NetworkObject))]
public class OfficeComputer : NetworkBehaviour, IInteractable
{
    [SerializeField] OfficeTaskDefinition demoTask;
    [SerializeField] string returnOfficeScene = "HQ";
    [SerializeField] bool allowNonNetworkSoloStart = false;

    bool missionLaunching;
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
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        BroadcastCompanyState();
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
    }

    public string InteractHint => "";

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

    public void LaunchSelectedMissionFromVehicle(PlayerController player)
    {
        if (missionLaunching) return;
        if (!HasSelectedDemoTask) return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            if (allowNonNetworkSoloStart && CanStartDemoTask())
                StartMissionLocal();
            return;
        }

        if (!CanStartDemoTask()) return;

        if (NetworkManager.Singleton.IsHost)
            StartMissionServerSide();
    }

    void QueueDemoTask()
    {
        if (demoTask == null) return;
        if (!CanStartDemoTask()) return;
        MvpMissionRuntime.SelectMission(demoTask, returnOfficeScene);
    }

    void StartMissionLocal()
    {
        if (demoTask == null) return;
        if (!CanStartDemoTask()) return;
        missionLaunching = true;
        MvpMissionRuntime.BeginMission(demoTask, returnOfficeScene);
        SceneManager.LoadScene(demoTask.sceneName);
    }

    void StartMissionServerSide()
    {
        if (!IsServer || demoTask == null) return;
        if (!CanStartDemoTask()) return;
        if (missionLaunching) return;

        missionLaunching = true;
        MvpMissionRuntime.BeginMission(demoTask, returnOfficeScene);

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.SceneManager != null)
            NetworkManager.Singleton.SceneManager.LoadScene(demoTask.sceneName, LoadSceneMode.Single);
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
