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
        // Sent to the host when this peer connects as a client, and checked in ApproveConnection.
        networkManager.NetworkConfig.ConnectionData = GameBuild.VersionPayload;
        networkManager.ConnectionApprovalCallback += ApproveConnection;
    }

    void OnDisable()
    {
        if (networkManager != null)
            networkManager.ConnectionApprovalCallback -= ApproveConnection;
    }

    void ApproveConnection(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string clientVersion = GameBuild.ReadVersion(request.Payload);
        bool versionOk = clientVersion == GameBuild.Version;

        int connectedPlayers = networkManager != null ? networkManager.ConnectedClientsIds.Count : 0;
        bool roomHasSpace = connectedPlayers < maxPlayers;

        bool approved = versionOk && roomHasSpace;

        response.Approved = approved;
        response.CreatePlayerObject = approved;
        response.Pending = false;
        response.Reason = !versionOk
            ? $"版本不一致 / Version mismatch (host {GameBuild.Version} ≠ client {clientVersion})。"
            : !roomHasSpace ? "事务所最多支持 4 名玩家。" : string.Empty;
    }
}
