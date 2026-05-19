using Unity.Netcode;
using UnityEngine;

public class EvidenceBoxItem : Carriable
{
    public NetworkVariable<bool> IsDamaged = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Update()
    {
        if (!IsServer) return;
        if (IsDamaged.Value) return;
        if (IsBeingCarried.Value) return;

        if (WaterLevelManager.Instance == null) return;
        float waterHeight = WaterLevelManager.Instance.CurrentWaterHeight.Value;
        if (waterHeight > transform.position.y + 0.2f)
        {
            IsDamaged.Value = true;
            DamagedClientRpc();
        }
    }

    [ClientRpc]
    void DamagedClientRpc()
    {
        var rend = GetComponent<Renderer>();
        if (rend != null) rend.material.color = new Color(0.4f, 0.3f, 0.2f);
    }
}
