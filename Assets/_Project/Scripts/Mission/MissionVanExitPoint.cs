using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class SchoolExitPoint : NetworkBehaviour, IInteractable
{
    public const int LockerSlotCount = 4;

    [SerializeField] float exitUseRadius = 3.5f;
    [SerializeField] float returnTransitSeconds = 6f;

    public NetworkVariable<int> FlashlightCount = new(1,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> BatteryCount = new(2,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public string InteractHint
    {
        get
        {
            if (VanTransitOverlay.IsActive) return "";
            var manager = LostItemMissionManager.Instance;
            if (manager != null && manager.LostItemCollected.Value)
                return "Board van and return";
            return "Board van";
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

        var manager = LostItemMissionManager.Instance;
        if (manager == null) return;

        manager.RequestBoardVan();      // reward/return accounting
        if (player != null)
            player.RequestSeat();        // actually sit the player in the cabin

        bool isHost = NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening || NetworkManager.Singleton.IsHost;
        string title = MvpMissionRuntime.ActiveTask?.title ?? MvpLocale.T("commission");
        string loc = MvpMissionRuntime.ActiveTask?.locationName ?? MvpLocale.T("mission_location");
        // E = board & sit; Space while seated = depart
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
        var manager = LostItemMissionManager.Instance;
        if (manager == null) return "The commission van is parked outside.";
        string bonus = manager.BonusEvidenceCollected.Value
            ? "The extra registry has been photographed — settlement will include a bonus."
            : "There is an overdue registry in the records room; you may photograph it before leaving.";
        if (manager.WrongHomeworkAttempts.Value > 0)
            bonus += $" Rifling through similar homework books will deduct {manager.WrongHomeworkMoneyPenalty}G.";
        return manager.LostItemCollected.Value
            ? $"The target item has been brought to the van — ready for a full return. {bonus}"
            : $"Objective not yet completed. The host may return early; the office will settle at partial results. {bonus}";
    }

    public string GetReturnButtonLabel()
    {
        var manager = LostItemMissionManager.Instance;
        if (manager != null && manager.LostItemCollected.Value)
            return "Close door and return - Complete commission";
        return "Host closes door and returns - Partial settlement";
    }

    public bool IsPartialReturnRequest()
    {
        var manager = LostItemMissionManager.Instance;
        return manager == null || !manager.LostItemCollected.Value;
    }

    public bool CanLocalPlayerRequestReturn()
    {
        var manager = LostItemMissionManager.Instance;
        if (manager == null) return true;
        if (!manager.LostItemCollected.Value) return IsLocalHostOrSolo();
        return true;
    }

    public string GetReturnBlockedReason()
    {
        var manager = LostItemMissionManager.Instance;
        if (manager != null && !manager.LostItemCollected.Value)
            return "Returning early will bring the whole team back to the office — requires host confirmation.";
        return "";
    }

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

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            LostItemMissionManager.Instance?.TryExitMission(0, transform.position, exitUseRadius, returnTransitSeconds);
            return;
        }

        RequestExitServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    void RequestExitServerRpc(ServerRpcParams rpcParams = default)
    {
        LostItemMissionManager.Instance?.TryExitMission(
            rpcParams.Receive.SenderClientId,
            transform.position,
            exitUseRadius,
            returnTransitSeconds);
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
