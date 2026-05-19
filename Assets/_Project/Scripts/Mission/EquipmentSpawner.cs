using Unity.Netcode;
using UnityEngine;

public class EquipmentSpawner : NetworkBehaviour
{
    [SerializeField] GameObject fusePrefab;
    [SerializeField] GameObject toolboxPrefab;
    [SerializeField] GameObject batteryPrefab;
    [SerializeField] GameObject evidenceBoxPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        SpawnLoadoutItems();
    }

    void SpawnLoadoutItems()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (var player in players)
        {
            Vector3 spawnBase = player.transform.position + player.transform.right * 1f;

            for (int i = 0; i < PlayerLoadout.SelectedIndices.Count; i++)
            {
                int itemIndex = PlayerLoadout.SelectedIndices[i];
                Vector3 offset = new Vector3(i * 0.5f, 0.5f, 0);
                Vector3 pos = spawnBase + offset;

                GameObject prefab = itemIndex switch
                {
                    1 => toolboxPrefab,
                    2 => batteryPrefab,
                    _ => null
                };

                if (prefab != null)
                {
                    var go = Instantiate(prefab, pos, Quaternion.identity);
                    go.GetComponent<NetworkObject>()?.Spawn();
                }
            }
        }
    }
}
