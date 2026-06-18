using Unity.Netcode;
using UnityEngine;
using BlackCommission.Scavenge;

/// <summary>
/// Server-authoritative mission manager for a scavenging run — the connective tissue that turns
/// the deposit gate into a settle-able mission. It owns the run state (<see cref="ScavengeMissionLogic"/>),
/// and on departure settles the van's <see cref="ScavengeCargoZone"/> manifest through
/// <see cref="ScavengeSettlementCalculator"/>: the run pays out the sum of the loaded items' values
/// (money-only, per the progression design — no full/partial split like the tower). Settlement,
/// the return transit and the HQ load all run on the host and replicate, mirroring
/// <c>TowerMissionManager</c>. Works offline too (PreviewWalker) via the authority fallback.
///
/// Per-item settlement reveal (quick-spec §4 P2) is deferred/PM-owned; this shows the run total on
/// the existing settlement card so the loop is closeable today.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class ScavengeMissionManager : NetworkBehaviour
{
    [Header("Scene Wiring")]
    [Tooltip("The van bay whose manifest this run settles. Falls back to ScavengeCargoZone.Instance.")]
    [SerializeField] ScavengeCargoZone cargoZone;
    [SerializeField] string officeSceneName = "HQ";
    [SerializeField] float returnToOfficeDelaySeconds = 6f;

    public NetworkVariable<int> SyncedState = new((int)ScavengeMissionState.InProgress,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> SyncedTotal = new(0,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    /// <summary>Scene-singleton: non-null means a scavenging mission is active in the loaded scene.</summary>
    public static ScavengeMissionManager Instance { get; private set; }

    ScavengeMissionLogic logic;
    float downedPollTimer;

    public bool IsTerminalState => logic != null && logic.IsTerminal;

    bool HasMissionAuthority =>
        IsServer || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening;

    void Awake() => logic = new ScavengeMissionLogic();
    void OnEnable() => Instance = this;
    void OnDisable() { if (Instance == this) Instance = null; }

    public override void OnNetworkSpawn()
    {
        if (cargoZone == null) cargoZone = ScavengeCargoZone.Instance;
    }

    void Update()
    {
        if (!HasMissionAuthority || logic.IsTerminal) return;
        downedPollTimer += Time.deltaTime;
        if (downedPollTimer < 1f) return;
        downedPollTimer = 0f;
        if (AllPlayersDowned() && logic.NotifyAllDowned())
            Settle();
    }

    bool AllPlayersDowned()
    {
        var healths = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None);
        if (healths.Length == 0) return false;
        foreach (var h in healths)
            if (!h.IsDowned.Value) return false;
        return true;
    }

    /// <summary>Owner-side intent (depart trigger / boarding); routed to the authority.</summary>
    public void RequestDepart()
    {
        if (HasMissionAuthority) { ResolveDepartureOnAuthority(); return; }
        RequestDepartServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestDepartServerRpc(ServerRpcParams rpcParams = default) => ResolveDepartureOnAuthority();

    void ResolveDepartureOnAuthority()
    {
        if (logic.ResolveDeparture())
            Settle();
    }

    void Settle()
    {
        if (cargoZone == null) cargoZone = ScavengeCargoZone.Instance;
        SettlementResult result = cargoZone != null
            ? cargoZone.SettleCargo()
            : new SettlementResult(System.Array.Empty<SettlementLine>(), 0, false);

        int money = result.Total;
        SyncedState.Value = (int)logic.State;
        SyncedTotal.Value = money;

        var kind = logic.State == ScavengeMissionState.Failed
            ? MvpMissionResultKind.Failed
            : MvpMissionResultKind.Success;

        float minTransit = Mathf.Max(1.5f, returnToOfficeDelaySeconds);
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            ApplyResultClientRpc(money, (int)kind);
            // 全队随车: seat everyone (downed included) and ride the return transit while HQ loads.
            PlayerController.SeatAllConnectedServer();
            BeginReturnTransitClientRpc(minTransit);
            Invoke(nameof(ReturnToOffice), 0.75f);
        }
        else
        {
            ApplyResultLocally(money, kind);
            VanTransitOverlay.ShowReturn(MvpMissionRuntime.ActiveTask?.title, null, minTransit);
            Invoke(nameof(ReturnToOffice), minTransit);
        }

        Debug.Log($"[ScavengeMission] Settled {logic.State}: {money}G from {result.Lines.Count} item(s).");
    }

    [ClientRpc]
    void BeginReturnTransitClientRpc(float minTransitSeconds) =>
        VanTransitOverlay.ShowReturn(MvpMissionRuntime.ActiveTask?.title, null, minTransitSeconds);

    [ClientRpc]
    void ApplyResultClientRpc(int money, int kind) => ApplyResultLocally(money, (MvpMissionResultKind)kind);

    static void ApplyResultLocally(int money, MvpMissionResultKind kind)
    {
        bool success = kind == MvpMissionResultKind.Success;
        // Money-only payout (progression design): the per-item reveal sequence (P2) layers on later.
        MvpPendingReward.Set(money, success, 0f, success, kind);
        SettlementCardOverlay.Show(kind, money, money, 1f);
    }

    void ReturnToOffice()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.SceneManager.LoadScene(
                officeSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
        else
            Debug.Log("[ScavengeMission] (offline) would return to HQ now — settlement applied locally.");
    }
}
