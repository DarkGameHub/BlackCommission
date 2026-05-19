using Unity.Netcode;
using UnityEngine;

public class HQSpawnManager : MonoBehaviour
{
    [SerializeField] Transform spawnPoint;

    void Start()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsListening)
            Invoke(nameof(TeleportAllPlayers), 0.2f);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer) return;
        Invoke(nameof(TeleportAllPlayers), 0.3f);
    }

    void TeleportAllPlayers()
    {
        if (spawnPoint == null) return;

        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
        {
            var cc = players[i].GetComponent<CharacterController>();
            if (cc == null) continue;
            cc.enabled = false;
            players[i].transform.position = spawnPoint.position + Vector3.right * (i * 1.5f);
            players[i].transform.rotation = spawnPoint.rotation;
            cc.enabled = true;
        }
    }
}
