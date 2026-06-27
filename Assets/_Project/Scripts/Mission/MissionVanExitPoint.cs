using Unity.Netcode;
using UnityEngine;

/// <summary>
/// The mission-site van interactable: board &amp; sit (E), locker hand-outs
/// (flashlights/batteries), and the return/partial-return decision. Boarding shows the
/// shared cabin overlay; the return request resolves through <see cref="ScavengeMissionManager"/>
/// (departing settles the cargo currently loaded in the van — no partial/objective split).
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class MissionVanExitPoint : NetworkBehaviour, IInteractable
{
    public const int LockerSlotCount = 4;

    [SerializeField] float exitUseRadius = 3.5f;

    public NetworkVariable<int> FlashlightCount = new(1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> BatteryCount = new(2,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public string InteractHint
    {
        get
        {
            if (VanTransitOverlay.IsActive) return "";
            return "上车";
        }
    }

    void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    public void OnInteractStart(PlayerController player)
    {
        if (player != null && player.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
            return;
        if (VanTransitOverlay.IsActive) return;

        if (player != null)
            player.RequestSeat();

        bool isHost = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsHost;
        string title = MvpMissionRuntime.ActiveTask?.title ?? MvpLocale.T("commission");
        string loc = MvpMissionRuntime.ActiveTask?.locationName ?? MvpLocale.T("mission_location");
        VanTransitOverlay.ShowBoarding(title, loc, isHost);
    }

    public void OnInteractEnd(PlayerController player) { }

    public MvpHotbarItemId GetLockerItemId(int index)
    {
        switch (index)
        {
            case 0: return MvpHotbarItemId.Flashlight;
            case 1: return MvpHotbarItemId.Battery;
            default: return MvpHotbarItemId.None;
        }
    }

    public int GetLockerQuantity(int index)
    {
        switch (index)
        {
            case 0: return FlashlightCount.Value;
            case 1: return BatteryCount.Value;
            default: return 0;
        }
    }

    public string GetReturnSummary()
    {
        var cargo = ScavengeCargoZone.Instance;
        if (cargo == null || cargo.Capacity.Value <= 0)
            return "委托车已停在前院。发车即按车上现有货物结算。";
        return $"已装载 {cargo.ItemCount.Value} 件，舱位 {cargo.LoadUnits.Value}/{cargo.Capacity.Value}。" +
               "发车即按车上现有货物结算——发车后全队随车返回事务所。";
    }

    public string GetReturnButtonLabel() => "关门返程 - 发车结算";

    public bool IsPartialReturnRequest() => false;

    public bool CanLocalPlayerRequestReturn() => IsLocalHostOrSolo();

    public string GetReturnBlockedReason() =>
        IsLocalHostOrSolo() ? "" : "发车会拉全队回事务所，需要房主确认。";

    public void TryTakeLockerItem(int slotIndex)
    {
        if (GetLockerQuantity(slotIndex) <= 0) return;
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            PlayerHotbar hotbar = Object.FindAnyObjectByType<PlayerHotbar>();
            if (hotbar != null && hotbar.TryReceiveLocalItem(GetLockerItemId(slotIndex), 1))
                SetLocalLockerQuantity(slotIndex, Mathf.Max(0, GetLockerQuantity(slotIndex) - 1));
            return;
        }

        RequestTakeLockerItemServerRpc(slotIndex);
    }

    public void RequestReturnToOffice(PlayerController player)
    {
        if (player != null && player.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
            return;
        // This screen already shows the cargo summary + host gate, so it counts as a confirmed
        // request: depart and settle whatever is loaded in the van.
        ScavengeMissionManager.Instance?.RequestDepart();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestTakeLockerItemServerRpc(int slotIndex, ServerRpcParams rpcParams = default)
    {
        if (slotIndex < 0 || slotIndex >= LockerSlotCount) return;
        if (GetLockerQuantity(slotIndex) <= 0) return;

        NetworkManager network = NetworkManager.Singleton;
        if (network == null) return;
        if (!network.ConnectedClients.TryGetValue(rpcParams.Receive.SenderClientId, out var client)) return;
        if (client.PlayerObject == null) return;
        if (client.PlayerObject.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value) return;
        if (Vector3.Distance(client.PlayerObject.transform.position, transform.position) > exitUseRadius) return;
        if (!client.PlayerObject.TryGetComponent<PlayerHotbar>(out var hotbar)) return;

        if (!hotbar.GrantItemServer(GetLockerItemId(slotIndex), 1)) return;
        SetLocalLockerQuantity(slotIndex, Mathf.Max(0, GetLockerQuantity(slotIndex) - 1));
    }

    void SetLocalLockerQuantity(int slotIndex, int quantity)
    {
        switch (slotIndex)
        {
            case 0: FlashlightCount.Value = quantity; break;
            case 1: BatteryCount.Value = quantity; break;
        }
    }

    static bool IsLocalHostOrSolo()
    {
        NetworkManager network = NetworkManager.Singleton;
        return network == null || !network.IsListening || network.IsHost;
    }
}
