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
    // Minimum return transit (boarding-transit spec): the HQ loads underneath the windowless
    // cabin right after settlement; the rear door opens at load-complete AND this many seconds,
    // whichever is later. Give the crew time to read the settlement card (spec suggests ≥10s).
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

    /// <summary>
    /// True while the host has the early-return application card open — peers render the
    /// "房主正在填写提前收工申请…" ticket line (boarding-transit spec, 队友同步行).
    /// </summary>
    public NetworkVariable<bool> HostFilingEarlyReturn = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Future monster-aggro hook: fires once on the authority when the column is first lifted.</summary>
    public static event System.Action OnObjectiveSecured;

    /// <summary>Scene-singleton: non-null means a tower mission scene is active (HUD/shop gates).</summary>
    public static TowerMissionManager Instance { get; private set; }

    TowerMissionLogic logic;
    float downedPollTimer;
    bool alarmPlayed;

    public bool IsTerminalState => (TowerMissionState)SyncedState.Value is TowerMissionState.Delivered
        or TowerMissionState.PartialReturn or TowerMissionState.Failed;

    bool HasMissionAuthority =>
        IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

    void Awake() => logic = new TowerMissionLogic(dropPenalty, rejectThreshold);

    void OnEnable()
    {
        Instance = this;
        SyncedState.OnValueChanged += OnSyncedStateChanged;
        if (ecoColumn == null) return;
        ecoColumn.IsBeingCarried.OnValueChanged += OnCarriedChanged;
        ecoColumn.HardImpact += OnHardImpact;
    }

    void OnDisable()
    {
        if (Instance == this) Instance = null;
        SyncedState.OnValueChanged -= OnSyncedStateChanged;
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
            PlayObjectiveAlarmOnce(); // offline path: OnValueChanged may not fire unspawned
        }
    }

    // Runs on every peer (and the latch keeps the host's double path single-fire):
    // lifting the column trips the E-07 violation buzzer at the plinth.
    void OnSyncedStateChanged(int previous, int next)
    {
        if ((TowerMissionState)next == TowerMissionState.ObjectiveSecured)
            PlayObjectiveAlarmOnce();
    }

    void PlayObjectiveAlarmOnce()
    {
        if (alarmPlayed) return;
        alarmPlayed = true;
        Vector3 pos = ecoColumn != null ? ecoColumn.transform.position : transform.position;
        AudioManager.Instance?.PlayObjectiveAlarm(pos);
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

    /// <summary>Aboard = set down inside the cargo zone (carrying it next to the van doesn't count).</summary>
    public bool IsObjectiveAboard => ecoColumn != null && cargoZone != null &&
                                     !ecoColumn.IsBeingCarried.Value &&
                                     cargoZone.bounds.Contains(ecoColumn.transform.position);

    /// <summary>Client-rejection threshold for the application card's warning row.</summary>
    public float RejectThreshold => rejectThreshold;

    /// <summary>
    /// Local preview of the clause B-2 early-return payout for the application card —
    /// the exact calculator path Settle uses, so the estimate never drifts from the bill.
    /// </summary>
    public int EstimatePartialMoney() => MissionRewardCalculator.Calculate(
        null, MvpMissionResultKind.Partial, 0f, false, 0, BuildFallbacks(), new MissionRewardBonus()).Money;

    /// <summary>Host-side: mirrors the application card open/closed state to all peers.</summary>
    public void SetFilingEarlyReturn(bool filing)
    {
        if (!HasMissionAuthority) return;
        HostFilingEarlyReturn.Value = filing;
    }

    /// <summary>
    /// Owner-side intent (depart lever / dispatch card); routed to the authority.
    /// Leaving WITHOUT the column requires the host's signed early-return application
    /// (`confirmedPartial`) — a lever pull can no longer settle partial by accident.
    /// </summary>
    public void RequestDepart(bool confirmedPartial = false)
    {
        if (HasMissionAuthority) { ResolveDepartureOnAuthority(confirmedPartial, true); return; }
        RequestDepartServerRpc(confirmedPartial);
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestDepartServerRpc(bool confirmedPartial, ServerRpcParams rpcParams = default) =>
        ResolveDepartureOnAuthority(confirmedPartial,
            rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId);

    void ResolveDepartureOnAuthority(bool confirmedPartial, bool requesterIsHost)
    {
        if (logic.IsTerminal) return;
        bool aboard = IsObjectiveAboard;
        if (!aboard && (!confirmedPartial || !requesterIsHost)) return; // 提前收工=房主签字专属
        HostFilingEarlyReturn.Value = false;
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
        int baseMoney = result.Money;
        int money = kind == MvpMissionResultKind.Success
            ? logic.ScaleDeliveredMoney(baseMoney)
            : baseMoney;

        float minTransit = Mathf.Max(1.5f, returnToOfficeDelaySeconds);
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            ApplyResultClientRpc(money, baseMoney, result.Reputation, result.Experience, (int)kind, logic.Completeness);
            // 全队随车: seat everyone (downed included), start the return transit on every
            // peer, then load the HQ behind the windowless cabin (boarding-transit spec).
            // The short delay lets the seat NetworkVariables land before the scene event.
            PlayerController.SeatAllConnectedServer();
            BeginReturnTransitClientRpc(minTransit);
            Invoke(nameof(ReturnToOffice), 0.75f);
        }
        else
        {
            ApplyResultLocally(money, baseMoney, result.Reputation, result.Experience, kind, logic.Completeness);
            VanTransitOverlay.ShowReturn(MvpMissionRuntime.ActiveTask?.title, null, minTransit);
            Invoke(nameof(ReturnToOffice), minTransit);
        }

        Debug.Log($"[TowerMission] Settled {logic.State}: {money}G / rep {result.Reputation} / " +
                  $"xp {result.Experience} (completeness {logic.Completeness:P0})");
    }

    [ClientRpc]
    void BeginReturnTransitClientRpc(float minTransitSeconds) =>
        VanTransitOverlay.ShowReturn(MvpMissionRuntime.ActiveTask?.title, null, minTransitSeconds);

    void ReturnToOffice()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.SceneManager.LoadScene(officeSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        else
            Debug.Log("[TowerMission] (offline) would return to HQ now — settlement applied locally.");
    }

    [ClientRpc]
    void ApplyResultClientRpc(int money, int baseMoney, int reputation, int experience, int kind, float completeness) =>
        ApplyResultLocally(money, baseMoney, reputation, experience, (MvpMissionResultKind)kind, completeness);

    static void ApplyResultLocally(int money, int baseMoney, int reputation, int experience,
        MvpMissionResultKind kind, float completeness)
    {
        MvpPendingReward.Set(money, reputation, experience,
            kind == MvpMissionResultKind.Success, 0f,
            kind == MvpMissionResultKind.Success, kind);
        // 委托结算单 dealt to this peer in the return van; the stamp sound now fires
        // on the card's stamp-fall frame (design/ux/settlement.md, Transitions).
        SettlementCardOverlay.Show(kind, baseMoney, money, completeness);
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
