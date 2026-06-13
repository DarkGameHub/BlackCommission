using Unity.Netcode;
using UnityEngine;

public class HQSpawnManager : MonoBehaviour
{
    const int MaxTeleportAttempts = 16;
    const float TeleportAttemptInterval = 0.25f;

    [SerializeField] Transform spawnPoint;
    int teleportAttemptsRemaining;

    void Start()
    {
        if (NetworkManager.Singleton == null) return;

        if (NetworkManager.Singleton.IsListening)
            BeginTeleportAttempts(0.15f);

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    void OnDestroy()
    {
        CancelInvoke(nameof(TeleportLocalPlayerAttempt));

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton == null || clientId != NetworkManager.Singleton.LocalClientId) return;
        BeginTeleportAttempts(0.3f);
    }

    void BeginTeleportAttempts(float initialDelay)
    {
        teleportAttemptsRemaining = MaxTeleportAttempts;
        CancelInvoke(nameof(TeleportLocalPlayerAttempt));
        InvokeRepeating(nameof(TeleportLocalPlayerAttempt), initialDelay, TeleportAttemptInterval);
    }

    void TeleportLocalPlayerAttempt()
    {
        if (TeleportLocalPlayer() || --teleportAttemptsRemaining <= 0)
            CancelInvoke(nameof(TeleportLocalPlayerAttempt));
    }

    bool TeleportLocalPlayer()
    {
        if (spawnPoint == null) return true;

        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var player in players)
        {
            if (player == null || !player.IsOwner) continue;
            var cc = player.GetComponent<CharacterController>();
            if (cc == null) continue;

            int offsetIndex = GetLocalSpawnOffsetIndex();
            Vector3 spawnPosition = spawnPoint.position + Vector3.right * (offsetIndex * 1.5f);

            // Crew arriving inside the van: the scene loaded underneath the windowless cabin
            // (boarding-transit spec) and VanTransitOverlay owns the disembark moment — hand
            // it the spawn instead of yanking the player out of the cabin mid-ritual.
            if (player.IsSeated && VanTransitOverlay.IsActive)
            {
                VanTransitOverlay.RegisterArrivalSpawn(spawnPosition, spawnPoint.rotation);
                return true;
            }

            player.RestoreControlAt(spawnPosition, spawnPoint.rotation);
            return true;
        }

        return false;
    }

    int GetLocalSpawnOffsetIndex()
    {
        NetworkManager network = NetworkManager.Singleton;
        if (network == null || !network.IsListening) return 0;
        return Mathf.Clamp((int)network.LocalClientId, 0, 3);
    }
}
