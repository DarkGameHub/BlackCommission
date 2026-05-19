using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PumpRepairInteraction : NetworkBehaviour, IInteractable
{
    [SerializeField] float repairDuration = 6f;
    [SerializeField] Transform panelPoint;
    [SerializeField] Transform valvePoint;
    [SerializeField] float interactRange = 2f;  // read by PlayerInteraction.cs (M2)

    public NetworkVariable<bool> IsRepaired = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> RepairProgress = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    NetworkVariable<ulong> panelPlayerId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<ulong> valvePlayerId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    bool bothPresent => panelPlayerId.Value != 0 && valvePlayerId.Value != 0;

    void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer) return;
        if (IsRepaired.Value) return;

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

    // ── IInteractable ────────────────────────────────────────────────────────
    public string InteractHint
    {
        get
        {
            if (IsRepaired.Value) return "排水泵已修复 ✓";
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
