using Unity.Netcode;
using UnityEngine;

/// <summary>
/// The lake-bottom key — the dive objective. Mirrors LostHomeworkItem: interacting
/// collects it through the shared LostItemMissionManager (Searching → ReturnToExit),
/// so the existing return/settlement flow works unchanged. Reachable while swimming.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class LakeKeyItem : NetworkBehaviour, IInteractable
{
    [SerializeField] string itemName = "湖底钥匙";
    [SerializeField] float serverPickupRadius = 3f;

    public string InteractHint
    {
        get
        {
            var manager = LostItemMissionManager.Instance;
            if (manager != null && manager.LostItemCollected.Value) return "";
            return $"拾取{itemName}";
        }
    }

    public void OnInteractStart(PlayerController player)
    {
        if (player == null) return;
        if (player.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value) return;
        AudioManager.Instance?.PlayPickup(transform.position);

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            LostItemMissionManager.Instance?.TryCollectItem(0, default);
            gameObject.SetActive(false);
            return;
        }

        RequestCollectServerRpc();
    }

    public void OnInteractEnd(PlayerController player) { }

    [ServerRpc(RequireOwnership = false)]
    void RequestCollectServerRpc(ServerRpcParams rpcParams = default)
    {
        if (!CanServerCollect(rpcParams.Receive.SenderClientId)) return;

        LostItemMissionManager.Instance?.TryCollectItem(
            rpcParams.Receive.SenderClientId,
            new NetworkObjectReference(NetworkObject));
    }

    bool CanServerCollect(ulong clientId)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
            return true;

        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client) ||
            client.PlayerObject == null)
            return false;

        GameObject playerObject = client.PlayerObject.gameObject;
        if (playerObject.TryGetComponent<PlayerHealth>(out var health) && health.IsDowned.Value)
            return false;

        return Vector3.Distance(playerObject.transform.position, transform.position) <= serverPickupRadius;
    }
}
