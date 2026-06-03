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
                return "上车返程";
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
        if (manager == null) return "委托车已停在门口。";
        string bonus = manager.BonusEvidenceCollected.Value
            ? "额外登记簿已拍下，结算会加一点外快。"
            : "记录室还有一本逾期登记簿，可选拍照后再撤。";
        if (manager.WrongHomeworkAttempts.Value > 0)
            bonus += $" 乱翻相似作业本会扣 {manager.WrongHomeworkMoneyPenalty}G。";
        return manager.LostItemCollected.Value
            ? $"目标物已带回车旁，可以完整返程。{bonus}"
            : $"目标尚未完成，房主可提前返程；事务所只会按部分结果结算。{bonus}";
    }

    public string GetReturnButtonLabel()
    {
        var manager = LostItemMissionManager.Instance;
        if (manager != null && manager.LostItemCollected.Value)
            return "关门返程 - 完成委托";
        return "房主关门返程 - 部分结算";
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
            return "提前返程会拉全队回事务所，需要房主确认。";
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
