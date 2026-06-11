using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative mission manager for the abandoned-tower commission
/// (design/quick-specs/tower-mission-manager-2026-06-10.md). The host forwards
/// eco-column pickup/hard-drop events and the van departure into the pure
/// <see cref="TowerMissionLogic"/>, owns the synced state, and settles through
/// <see cref="MissionRewardCalculator"/> → <see cref="MvpPendingReward"/> exactly
/// like LostItemMissionManager (which stays untouched — school missions unaffected).
/// Works offline too (PreviewWalker walkthroughs) via the same authority fallback.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class TowerMissionManager : NetworkBehaviour
{
    [Header("Tuning (quick-spec 2026-06-10, PM-locked)")]
    [SerializeField] float dropPenalty = 0.03f;
    [SerializeField] float rejectThreshold = 0.5f;

    [Header("Scene Wiring")]
    [SerializeField] EcoColumnCarriable ecoColumn;
    [SerializeField] BoxCollider cargoZone;
    [SerializeField] string officeSceneName = "HQ";
    [SerializeField] float returnToOfficeDelaySeconds = 6f;

    [Header("Reward Fallbacks (registry: full 300/5/80, partial 60/0/15, failure 20/-2/0)")]
    [SerializeField] int fullMoney = 300;
    [SerializeField] int fullReputation = 5;
    [SerializeField] int fullExperience = 80;
    [SerializeField] int partialMoney = 60;
    [SerializeField] int partialReputation = 0;
    [SerializeField] int partialExperience = 15;
    [SerializeField] int failureMoney = 20;
    [SerializeField] int failureReputation = -2;
    [SerializeField] int failureExperience = 0;

    public NetworkVariable<int> SyncedState = new((int)TowerMissionState.InProgress,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> SyncedCompleteness = new(1f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Future monster-aggro hook: fires once on the authority when the column is first lifted.</summary>
    public static event System.Action OnObjectiveSecured;

    /// <summary>Scene-singleton: non-null means a tower mission scene is active (HUD/shop gates).</summary>
    public static TowerMissionManager Instance { get; private set; }

    TowerMissionLogic logic;
    float downedPollTimer;

    public bool IsTerminalState => (TowerMissionState)SyncedState.Value is TowerMissionState.Delivered
        or TowerMissionState.PartialReturn or TowerMissionState.Failed;

    bool HasMissionAuthority =>
        IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

    void Awake() => logic = new TowerMissionLogic(dropPenalty, rejectThreshold);

    void OnEnable()
    {
        Instance = this;
        if (ecoColumn == null) return;
        ecoColumn.IsBeingCarried.OnValueChanged += OnCarriedChanged;
        ecoColumn.HardImpact += OnHardImpact;
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
        if (ecoColumn == null) return;
        ecoColumn.IsBeingCarried.OnValueChanged -= OnCarriedChanged;
        ecoColumn.HardImpact -= OnHardImpact;
    }

    void OnCarriedChanged(bool wasCarried, bool isCarried)
    {
        if (!HasMissionAuthority || !isCarried) return;
        if (logic.NotifySecured())
        {
            SyncedState.Value = (int)logic.State;
            OnObjectiveSecured?.Invoke();
        }
    }

    void OnHardImpact(float impactForce)
    {
        if (!HasMissionAuthority) return;
        if (logic.NotifyHardDrop())
            SyncedCompleteness.Value = logic.Completeness;
    }

    void Update()
    {
        if (!HasMissionAuthority || logic.IsTerminal) return;
        downedPollTimer += Time.deltaTime;
        if (downedPollTimer < 1f) return;
        downedPollTimer = 0f;
        if (AllPlayersDowned() && logic.NotifyAllDowned())
            Settle(MvpMissionResultKind.Failed);
    }

    bool AllPlayersDowned()
    {
        var healths = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        if (healths.Length == 0) return false;
        foreach (var h in healths)
            if (!h.IsDowned.Value) return false;
        return true;
    }

    /// <summary>Owner-side intent (depart lever); routed to the authority like LostItemMissionManager.</summary>
    public void RequestDepart()
    {
        if (HasMissionAuthority) { ResolveDepartureOnAuthority(); return; }
        RequestDepartServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestDepartServerRpc(ServerRpcParams rpcParams = default) => ResolveDepartureOnAuthority();

    void ResolveDepartureOnAuthority()
    {
        if (logic.IsTerminal) return;
        // Aboard = set down inside the cargo zone (carrying it next to the van doesn't count).
        bool aboard = ecoColumn != null && cargoZone != null &&
                      !ecoColumn.IsBeingCarried.Value &&
                      cargoZone.bounds.Contains(ecoColumn.transform.position);
        TowerMissionState outcome = logic.ResolveDeparture(aboard);
        Settle(outcome == TowerMissionState.Delivered
            ? MvpMissionResultKind.Success
            : MvpMissionResultKind.Partial);
    }

    void Settle(MvpMissionResultKind kind)
    {
        SyncedState.Value = (int)logic.State;
        SyncedCompleteness.Value = logic.Completeness;

        MissionRewardResult result = MissionRewardCalculator.Calculate(
            null, kind, 0f, false, 0, BuildFallbacks(), new MissionRewardBonus());
        int money = kind == MvpMissionResultKind.Success
            ? logic.ScaleDeliveredMoney(result.Money)
            : result.Money;

        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            ApplyResultClientRpc(money, result.Reputation, result.Experience, (int)kind);
        else
            ApplyResultLocally(money, result.Reputation, result.Experience, kind);

        Debug.Log($"[TowerMission] Settled {logic.State}: {money}G / rep {result.Reputation} / " +
                  $"xp {result.Experience} (completeness {logic.Completeness:P0})");
        Invoke(nameof(ReturnToOffice), returnToOfficeDelaySeconds);
    }

    void ReturnToOffice()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.SceneManager.LoadScene(officeSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        else
            Debug.Log("[TowerMission] (offline) would return to HQ now — settlement applied locally.");
    }

    [ClientRpc]
    void ApplyResultClientRpc(int money, int reputation, int experience, int kind) =>
        ApplyResultLocally(money, reputation, experience, (MvpMissionResultKind)kind);

    static void ApplyResultLocally(int money, int reputation, int experience, MvpMissionResultKind kind)
    {
        MvpPendingReward.Set(money, reputation, experience,
            kind == MvpMissionResultKind.Success, 0f,
            kind == MvpMissionResultKind.Success, kind);
        // 印章落纸 — the settlement becomes official on every peer.
        AudioManager.Instance?.PlayStamp();
    }

    MissionRewardFallbacks BuildFallbacks() => new MissionRewardFallbacks
    {
        moneyReward = fullMoney,
        reputationReward = fullReputation,
        experienceReward = fullExperience,
        partialMoneyReward = partialMoney,
        partialReputationReward = partialReputation,
        partialExperienceReward = partialExperience,
        failureMoney = failureMoney,
        failureReputation = failureReputation,
        failureExperience = failureExperience
    };
}
