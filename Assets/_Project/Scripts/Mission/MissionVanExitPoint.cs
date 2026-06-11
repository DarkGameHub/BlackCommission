using Unity.Netcode;
using UnityEngine;

/// <summary>
/// The mission-site van interactable: board &amp; sit (E), locker hand-outs
/// (flashlights/batteries), and the return/partial-return decision — ported from the
/// retired school exit point onto <see cref="TowerMissionManager"/>. Boarding shows the
/// shared cabin overlay; the return request resolves through the mission manager
/// (full delivery vs partial settlement by cargo-zone check).
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

    static bool ObjectiveSecured =>
        TowerMissionManager.Instance != null &&
        (TowerMissionState)TowerMissionManager.Instance.SyncedState.Value == TowerMissionState.ObjectiveSecured;

    public string InteractHint
    {
        get
        {
            if (VanTransitOverlay.IsActive) return "";
            return ObjectiveSecured ? "上车返程" : "上车";
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
        var manager = TowerMissionManager.Instance;
        if (manager == null) return "委托车已停在前院。";
        string seal = $"生态柱密封完整度 {manager.SyncedCompleteness.Value:P0}。";
        if (manager.SyncedCompleteness.Value < 0.5f)
            seal += " 警告：低于 50% 客户拒收，只能按部分结算。";
        return ObjectiveSecured
            ? $"生态柱已到手——放进货舱再发车即可完整结算。{seal}"
            : $"目标尚未到手，房主可提前返程；事务所只会按部分结果结算。{seal}";
    }

    public string GetReturnButtonLabel() =>
        ObjectiveSecured ? "关门返程 - 完成委托" : "房主关门返程 - 部分结算";

    public bool IsPartialReturnRequest() => !ObjectiveSecured;

    public bool CanLocalPlayerRequestReturn()
    {
        if (TowerMissionManager.Instance == null) return true;
        if (!ObjectiveSecured) return IsLocalHostOrSolo();
        return true;
    }

    public string GetReturnBlockedReason() =>
        !ObjectiveSecured ? "提前返程会拉全队回事务所，需要房主确认。" : "";

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
        TowerMissionManager.Instance?.RequestDepart();
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
