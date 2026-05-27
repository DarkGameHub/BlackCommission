using Unity.Netcode;
using UnityEngine;

public class MvpConnectionLimiter : MonoBehaviour
{
    [SerializeField] int maxPlayers = 4;

    NetworkManager networkManager;

    void Awake()
    {
        networkManager = GetComponent<NetworkManager>();
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;
    }

    void OnEnable()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;
        if (networkManager == null) return;

        networkManager.NetworkConfig.ConnectionApproval = true;
        networkManager.ConnectionApprovalCallback += ApproveConnection;
    }

    void OnDisable()
    {
        if (networkManager != null)
            networkManager.ConnectionApprovalCallback -= ApproveConnection;
    }

    void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        int connectedPlayers = networkManager != null ? networkManager.ConnectedClientsIds.Count : 0;
        bool approved = connectedPlayers < maxPlayers;

        response.Approved = approved;
        response.CreatePlayerObject = approved;
        response.Pending = false;
        response.Reason = approved ? string.Empty : "事务所最多支持 4 名玩家。";
    }
}
