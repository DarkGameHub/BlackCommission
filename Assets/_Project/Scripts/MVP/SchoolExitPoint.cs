using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class SchoolExitPoint : NetworkBehaviour, IInteractable
{
    [SerializeField] float exitUseRadius = 3.5f;

    public string InteractHint
    {
        get
        {
            var manager = LostItemMissionManager.Instance;
            if (manager == null) return "上车返回事务所";
            if (!manager.LostItemCollected.Value) return "先找到作业本，再回车上";

            ulong localClientId = NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening
                ? NetworkManager.Singleton.LocalClientId
                : 0;
            return manager.CarrierClientId.Value == localClientId
                ? "上车撤离返回事务所"
                : "需要拿作业本的人上车";
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

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            LostItemMissionManager.Instance?.TryExitMission(0, transform.position, exitUseRadius);
            return;
        }

        RequestExitServerRpc();
    }

    public void OnInteractEnd(PlayerController player) { }

    [ServerRpc(RequireOwnership = false)]
    void RequestExitServerRpc(ServerRpcParams rpcParams = default)
    {
        LostItemMissionManager.Instance?.TryExitMission(
            rpcParams.Receive.SenderClientId,
            transform.position,
            exitUseRadius);
    }
}
