using Unity.Netcode;
using UnityEngine;

public class TemporaryBatteryItem : Carriable
{
    public NetworkVariable<bool> IsDestroyed = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    void Update()
    {
        if (!IsServer) return;
        if (IsDestroyed.Value) return;
        if (IsBeingCarried.Value) return;

        if (WaterLevelManager.Instance == null) return;
        float waterHeight = WaterLevelManager.Instance.CurrentWaterHeight.Value;
        if (waterHeight > transform.position.y + 0.1f)
        {
            IsDestroyed.Value = true;
            DestroyedClientRpc();
        }
    }

    [ClientRpc]
    void DestroyedClientRpc()
    {
        var rend = GetComponentInChildren<Renderer>();
        if (rend != null) rend.material.color = Color.gray;
    }

    protected override void OnDropDamage(float impactForce)
    {
        if (!IsServer) return;
        if (impactForce > 10f)
        {
            IsDestroyed.Value = true;
            DestroyedClientRpc();
        }
    }
}
