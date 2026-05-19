using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// B2 drainage pump repair. Requires two players simultaneously:
///   Player A — holds E at the control panel
///   Player B — holds E at the valve handle
/// Both must hold for repairDuration seconds without interruption.
/// </summary>
public class PumpRepairInteraction : NetworkBehaviour
{
    [SerializeField] float repairDuration = 6f;
    [SerializeField] Transform panelPoint;   // Player A stands here
    [SerializeField] Transform valvePoint;   // Player B stands here
    [SerializeField] float interactRange = 2f;

    public NetworkVariable<bool> IsRepaired = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> RepairProgress = new(0f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    // Which player is holding each station (0 = nobody)
    NetworkVariable<ulong> panelPlayerId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    NetworkVariable<ulong> valvePlayerId = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    bool bothPresent => panelPlayerId.Value != 0 && valvePlayerId.Value != 0;

    void Update()
    {
        if (!IsServer) return;
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

    // Called when player presses E near panel or valve
    [ServerRpc(RequireOwnership = false)]
    public void AssignStationServerRpc(ulong playerId, bool isPanel)
    {
        if (IsRepaired.Value) return;

        if (isPanel && panelPlayerId.Value == 0)
            panelPlayerId.Value = playerId;
        else if (!isPanel && valvePlayerId.Value == 0)
            valvePlayerId.Value = playerId;
    }

    // Called when player releases E or moves away
    [ServerRpc(RequireOwnership = false)]
    public void ReleaseStationServerRpc(ulong playerId)
    {
        if (panelPlayerId.Value == playerId) panelPlayerId.Value = 0;
        if (valvePlayerId.Value == playerId) valvePlayerId.Value = 0;
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
