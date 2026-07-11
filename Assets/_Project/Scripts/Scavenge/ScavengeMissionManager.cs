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
            : new SettlementResult(System.Array.Empty<SettlementLine>(), 0);

        int money = result.Total;
        WorkOrderItem.DespawnAllServer();
        SyncedState.Value = (int)logic.State;
        SyncedTotal.Value = money;

        var kind = logic.State == ScavengeMissionState.Failed
            ? MvpMissionResultKind.Failed
            : MvpMissionResultKind.Success;

        // Per-item reveal payload (quick-spec §4 P2): names resolved host-side so peers only
        // render; payouts shipped (not recomputed) so every card matches the credited total.
        BuildRevealPayload(result, out string namesJoined, out int[] payouts, out byte[] flags);

        float minTransit = Mathf.Max(1.5f, returnToOfficeDelaySeconds);
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
        {
            ApplyResultClientRpc(money, (int)kind, namesJoined, payouts, flags);
            // 全队随车: seat everyone (downed included) and ride the return transit while HQ loads.
            PlayerController.SeatAllConnectedServer();
            BeginReturnTransitClientRpc(minTransit);
            Invoke(nameof(ReturnToOffice), 0.75f);
        }
        else
        {
            ApplyResultLocally(money, kind, namesJoined, payouts, flags);
            VanTransitOverlay.ShowReturn(OfficeTaskText.Title(MvpMissionRuntime.ActiveTask), null, minTransit);
            Invoke(nameof(ReturnToOffice), minTransit);
        }

        Debug.Log($"[ScavengeMission] Settled {logic.State}: {money}G from {result.Lines.Count} item(s).");
    }

    /// <summary>
    /// Flatten settlement lines into RPC-friendly parallel arrays. Flag byte layout:
    /// bit0 = class-preference applied, bit1 = relic, bits 2-3 = RelicReception.
    /// </summary>
    static void BuildRevealPayload(SettlementResult result,
        out string namesJoined, out int[] payouts, out byte[] flags)
    {
        var lines = result.Lines;
        var names = new string[lines.Count];
        payouts = new int[lines.Count];
        flags = new byte[lines.Count];

        var defs = Resources.LoadAll<ScavengeItemDefinition>("Scavenge/Items");
        for (int i = 0; i < lines.Count; i++)
        {
            SettlementLine line = lines[i];
            names[i] = line.ItemId;
            foreach (var def in defs)
                if (def != null && def.id == line.ItemId && !string.IsNullOrWhiteSpace(def.displayName))
                { names[i] = def.displayName; break; }

            payouts[i] = line.Payout;
            flags[i] = (byte)((line.PreferenceApplied ? 1 : 0)
                              | (line.Tier == ScavengeTier.Relic ? 2 : 0)
                              | ((int)line.RelicReception << 2));
        }
        namesJoined = string.Join("\n", names);
    }

    [ClientRpc]
    void BeginReturnTransitClientRpc(float minTransitSeconds) =>
        VanTransitOverlay.ShowReturn(OfficeTaskText.Title(MvpMissionRuntime.ActiveTask), null, minTransitSeconds);

    [ClientRpc]
    void ApplyResultClientRpc(int money, int kind, string namesJoined, int[] payouts, byte[] flags)
        => ApplyResultLocally(money, (MvpMissionResultKind)kind, namesJoined, payouts, flags);

    static void ApplyResultLocally(int money, MvpMissionResultKind kind,
        string namesJoined, int[] payouts, byte[] flags)
    {
        bool success = kind == MvpMissionResultKind.Success;
        MvpPendingReward.Set(money, success, 0f, success, kind);

        string[] names = string.IsNullOrEmpty(namesJoined)
            ? System.Array.Empty<string>()
            : namesJoined.Split('\n');
        if (names.Length > 0 && payouts != null && payouts.Length == names.Length)
            SettlementCardOverlay.ShowScavengeReveal(kind, money, names, payouts, flags);
        else
            SettlementCardOverlay.Show(kind, money, money, 1f);  // empty cargo — nothing to reveal
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
