using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerHealth : NetworkBehaviour, IInteractable
{
    [Header("Health")]
    [SerializeField] float maxHP = 100f;
    [SerializeField] float electrocutionDamageRate = 15f;

    [Header("Revive")]
    [SerializeField] float reviveDuration = 4f;
    [SerializeField] float reviveHP = 50f;

    public NetworkVariable<float> CurrentHP = new(100f,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> IsDowned = new(false,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    float reviveProgress;
    ulong reviverClientId;
    bool reviverActive;

    public string InteractHint
    {
        get
        {
            if (!IsDowned.Value) return "";
            int pct = Mathf.RoundToInt(reviveProgress / reviveDuration * 100);
            return reviverActive ? $"救援队友 ({pct}%)" : "救援队友";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        if (!IsDowned.Value) return;
        if (player.GetComponent<NetworkObject>().OwnerClientId == OwnerClientId) return;
        BeginReviveServerRpc(player.OwnerClientId);
    }

    public void OnInteractEnd(PlayerController player)
    {
        if (!IsDowned.Value) return;
        StopReviveServerRpc(player.OwnerClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    void BeginReviveServerRpc(ulong reviverId)
    {
        if (!IsDowned.Value) return;
        reviverClientId = reviverId;
        reviverActive = true;
    }

    [ServerRpc(RequireOwnership = false)]
    void StopReviveServerRpc(ulong reviverId)
    {
        if (reviverClientId == reviverId)
        {
            reviverActive = false;
            reviveProgress = Mathf.Max(0, reviveProgress - 0.5f);
        }
    }

    void Update()
    {
        if (!IsServer) return;
        if (!IsDowned.Value)
        {
            UpdateElectrocution();
            return;
        }

        if (reviverActive)
        {
            reviveProgress += Time.deltaTime;
            if (reviveProgress >= reviveDuration)
                CompleteRevive();
        }
    }

    void UpdateElectrocution()
    {
        if (WaterLevelManager.Instance == null) return;
        if (GameManager.Instance != null && GameManager.Instance.PumpRepaired.Value) return;

        float waterHeight = WaterLevelManager.Instance.CurrentWaterHeight.Value;
        float feetY = transform.position.y - 1f;
        if (waterHeight > feetY)
            TakeDamage(electrocutionDamageRate * Time.deltaTime);
    }

    public void TakeDamage(float damage)
    {
        if (!IsServer) return;
        if (IsDowned.Value) return;

        CurrentHP.Value = Mathf.Max(0, CurrentHP.Value - damage);
        if (CurrentHP.Value <= 0)
            EnterDowned();
    }

    void EnterDowned()
    {
        IsDowned.Value = true;
        reviveProgress = 0;
        reviverActive = false;
        DownedClientRpc();
    }

    void CompleteRevive()
    {
        IsDowned.Value = false;
        CurrentHP.Value = reviveHP;
        reviveProgress = 0;
        reviverActive = false;
        RevivedClientRpc();
    }

    [ClientRpc]
    void DownedClientRpc()
    {
        AudioManager.Instance?.PlaySurvivorCallout(transform.position);
    }

    [ClientRpc]
    void RevivedClientRpc()
    {
        AudioManager.Instance?.PlaySurvivorCalm(transform.position);
    }
}
