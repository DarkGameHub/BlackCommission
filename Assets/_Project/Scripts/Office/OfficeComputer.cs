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

    public string InteractHint
    {
        get
        {
            if (MvpPendingReward.HasPending) return IsClientOnly ? "等待房主领取结算" : "领取委托奖励";
            if (missionLaunching) return "任务启动中...";
            if (CompanyData.Current.CanAffordTutorialAcquisition)
            {
                if (IsClientOnly)
                    return "等待房主吞并 0 级事务所";
                return $"吞并 0 级事务所 ({CompanyData.Current.TutorialAcquisitionCost}G)";
            }
            string title = demoTask != null ? demoTask.title : "被遗忘的作业本";
            if ((NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening) && !allowNonNetworkSoloStart)
                return "先创建主机或开始单人主机";
            if (!CanStartDemoTask()) return "事务所等级或声望不足";
            if (IsClientOnly)
                return $"查看委托: {title} (等待房主开始)";
            if (CompanyData.Current.CanShowTutorialAcquisition)
                return $"接受委托: {title} (吞并需 {CompanyData.Current.TutorialAcquisitionCost}G/压力<70)";
            return $"接受委托: {title}";
        }
    }

    public void OnInteractStart(PlayerController player)
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
                StartMissionLocal();
            return;
        }

        if (!CanStartDemoTask()) return;

        if (NetworkManager.Singleton.IsHost)
            StartMissionServerSide();
    }

    public void OnInteractEnd(PlayerController player) { }

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
