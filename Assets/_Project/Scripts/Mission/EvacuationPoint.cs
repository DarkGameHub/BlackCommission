using Unity.Netcode;
using UnityEngine;

public class EvacuationPoint : NetworkBehaviour, IInteractable
{
    [SerializeField] float survivorRescueRadius = 15f;

    public string InteractHint
    {
        get
        {
            if (GameManager.Instance == null) return "撤离";
            if (GameManager.Instance.CurrentPhase.Value == GameManager.MissionPhase.Ended)
                return "";
            bool pumpDone = GameManager.Instance.PumpRepaired.Value;
            int survivors = GameManager.Instance.SurvivorsRescued.Value;
            return $"撤离  (泵:{(pumpDone ? "✓" : "✗")}  幸存者:{survivors}/2)";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        if (GameManager.Instance?.CurrentPhase.Value == GameManager.MissionPhase.Ended) return;
        RequestEvacServerRpc();
    }

    public void OnInteractEnd(PlayerController player) { }

    [ServerRpc(RequireOwnership = false)]
    void RequestEvacServerRpc()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentPhase.Value == GameManager.MissionPhase.Ended) return;

        // Auto-rescue nearby survivors
        var colliders = Physics.OverlapSphere(transform.position, survivorRescueRadius);
        foreach (var col in colliders)
        {
            var survivor = col.GetComponentInParent<SurvivorController>();
            if (survivor != null && !survivor.IsRescued.Value)
                survivor.MarkRescued();
        }

        GameManager.Instance.TriggerEvacuation(
            GameManager.Instance.SurvivorsRescued.Value, 0);
    }
}
