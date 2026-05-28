using Unity.Netcode;
using UnityEngine;

public class HQSpawnManager : MonoBehaviour
{
    [SerializeField] Transform spawnPoint;

    void Start()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsListening)
            Invoke(nameof(TeleportLocalPlayer), 0.2f);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null || clientId != NetworkManager.Singleton.LocalClientId) return;
        Invoke(nameof(TeleportLocalPlayer), 0.3f);
    }

    void TeleportLocalPlayer()
    {
        if (spawnPoint == null) return;

        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player == null || !player.IsOwner) continue;
            var cc = player.GetComponent<CharacterController>();
            if (cc == null) continue;

            int offsetIndex = GetLocalSpawnOffsetIndex();
            cc.enabled = false;
            player.transform.position = spawnPoint.position + Vector3.right * (offsetIndex * 1.5f);
            player.transform.rotation = spawnPoint.rotation;
            cc.enabled = true;
        }
    }

    int GetLocalSpawnOffsetIndex()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening) return 0;
        return Mathf.Clamp((int)network.LocalClientId, 0, 3);
    }
}
