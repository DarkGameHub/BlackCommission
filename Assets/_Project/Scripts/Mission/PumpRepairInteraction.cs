using Unity.Netcode;
using UnityEngine;

public class PumpRepairInteraction : NetworkBehaviour, IInteractable
{
    [SerializeField] float repairDuration = 6f;
    [SerializeField] Transform panelPoint;
    [SerializeField] Transform valvePoint;

    public NetworkVariable<bool> IsRepaired = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> RepairProgress = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> FuseInstalled = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> BatteryInstalled = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    NetworkVariable<ulong> panelPlayerId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<ulong> valvePlayerId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    bool bothPresent => panelPlayerId.Value != 0 && valvePlayerId.Value != 0;

    void Update()
    {
        if (!IsServer) return;
        if (IsRepaired.Value) return;
        if (!FuseInstalled.Value || !BatteryInstalled.Value) return;

        if (bothPresent)
        {
            RepairProgress.Value += Time.deltaTime / repairDuration;
            if (RepairProgress.Value >= 1f)
                CompleteRepair();
        }
        else
        {
            RepairProgress.Value = Mathf.Max(0, RepairProgress.Value - Time.deltaTime / repairDuration);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AssignStationServerRpc(ulong playerId, bool isPanel)
    {
        if (IsRepaired.Value) return;
        if (!FuseInstalled.Value || !BatteryInstalled.Value) return;
        if (isPanel && panelPlayerId.Value == 0)
            panelPlayerId.Value = playerId;
        else if (!isPanel && valvePlayerId.Value == 0)
            valvePlayerId.Value = playerId;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReleaseStationServerRpc(ulong playerId)
    {
        if (panelPlayerId.Value == playerId) panelPlayerId.Value = 0;
        if (valvePlayerId.Value == playerId) valvePlayerId.Value = 0;
    }

    [ServerRpc(RequireOwnership = false)]
    void InstallFuseServerRpc(NetworkObjectReference fuseRef, NetworkObjectReference playerRef)
    {
        if (FuseInstalled.Value) return;
        if (!fuseRef.TryGet(out NetworkObject fuseNet)) return;
        if (fuseNet.GetComponent<FuseItem>() == null) return;

        FuseInstalled.Value = true;

        if (playerRef.TryGet(out NetworkObject playerNet))
        {
            var carry = playerNet.GetComponent<CarrySystem>();
            carry?.Drop();
        }

        fuseNet.Despawn();
    }

    [ServerRpc(RequireOwnership = false)]
    void InstallBatteryServerRpc(NetworkObjectReference batteryRef, NetworkObjectReference playerRef)
    {
        if (!FuseInstalled.Value) return;
        if (BatteryInstalled.Value) return;
        if (!batteryRef.TryGet(out NetworkObject batteryNet)) return;

        var battery = batteryNet.GetComponent<TemporaryBatteryItem>();
        if (battery == null || battery.IsDestroyed.Value) return;

        BatteryInstalled.Value = true;

        if (playerRef.TryGet(out NetworkObject playerNet))
        {
            var carry = playerNet.GetComponent<CarrySystem>();
            carry?.Drop();
        }

        batteryNet.Despawn();
    }

    public string InteractHint
    {
        get
        {
            if (IsRepaired.Value) return "排水泵已修复 ✓";
            if (!FuseInstalled.Value) return "需要安装保险丝 (携带保险丝后按E)";
            if (!BatteryInstalled.Value) return "需要安装临时电池 (携带电池后按E)";
            int pct = Mathf.RoundToInt(RepairProgress.Value * 100);
            bool oneAssigned = panelPlayerId.Value != 0 || valvePlayerId.Value != 0;
            return oneAssigned
                ? $"继续修复... {pct}%  (需要2人同时)"
                : $"修复排水泵 {pct}%  (需要2人同时)";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        if (IsRepaired.Value) return;

        var carry = player.GetComponent<CarrySystem>();
        var playerNetRef = new NetworkObjectReference(player.GetComponent<NetworkObject>());

        if (!FuseInstalled.Value)
        {
            if (carry != null && carry.CarriedItem is FuseItem)
            {
                InstallFuseServerRpc(new NetworkObjectReference(carry.CarriedItem.NetworkObject), playerNetRef);
                return;
            }
            return;
        }

        if (!BatteryInstalled.Value)
        {
            if (carry != null && carry.CarriedItem is TemporaryBatteryItem bat && !bat.IsDestroyed.Value)
            {
                InstallBatteryServerRpc(new NetworkObjectReference(carry.CarriedItem.NetworkObject), playerNetRef);
                return;
            }
            return;
        }

        ulong id = player.OwnerClientId;
        bool takePanel = panelPlayerId.Value == 0 || panelPlayerId.Value == id;
        AssignStationServerRpc(id, takePanel);
    }

    public void OnInteractEnd(PlayerController player)
    {
        ReleaseStationServerRpc(player.OwnerClientId);
    }

    void CompleteRepair()
    {
        IsRepaired.Value = true;
        RepairProgress.Value = 1f;
        GameManager.Instance?.PumpFixed();
        OnRepairCompleteClientRpc();
    }

    [ClientRpc]
    void OnRepairCompleteClientRpc()
    {
        AudioManager.Instance?.PlayPumpStartup(transform.position);
    }
}
