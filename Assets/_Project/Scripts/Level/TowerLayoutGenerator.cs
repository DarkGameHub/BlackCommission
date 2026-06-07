using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace BlackCommission.Level
{
    /// <summary>
    /// Drives the slot-based room generation at runtime. The server picks one seed,
    /// replicates it via a NetworkVariable, and every peer (including the host) runs
    /// the same deterministic fill from that seed — so the random layout is identical
    /// everywhere while only an int crosses the wire.
    ///
    /// Place this on a NetworkObject in the tower scene, assign the catalog, and it
    /// finds every <see cref="RoomSlot"/> in the loaded scenes on spawn.
    /// </summary>
    [RequireComponent(typeof(NetworkObject))]
    public class TowerLayoutGenerator : NetworkBehaviour
    {
        [Header("Pool")]
        [SerializeField] TowerRoomCatalog catalog;

        [Header("Seed")]
        [Tooltip("If non-zero, the server forces this seed (handy for repeatable tests). " +
                 "Zero = the server rolls a random seed each session.")]
        [SerializeField] int fixedSeedForTesting = 0;

        // Replicated seed: server writes, everyone reads. -1 means "not generated yet".
        readonly NetworkVariable<int> netSeed =
            new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone,
                                         NetworkVariableWritePermission.Server);

        readonly List<GameObject> placed = new List<GameObject>();
        bool generated;

        public override void OnNetworkSpawn()
        {
            netSeed.OnValueChanged += OnSeedChanged;

            if (IsServer)
            {
                int seed = fixedSeedForTesting != 0
                    ? fixedSeedForTesting
                    : new System.Random().Next(1, int.MaxValue);
                netSeed.Value = seed; // triggers OnSeedChanged on server too
                GenerateIfReady();     // server can generate immediately
            }
            else
            {
                // Late joiner: the value may already be set before our callback registered.
                GenerateIfReady();
            }
        }

        public override void OnNetworkDespawn()
        {
            netSeed.OnValueChanged -= OnSeedChanged;
        }

        void OnSeedChanged(int previous, int current) => GenerateIfReady();

        void GenerateIfReady()
        {
            if (generated || netSeed.Value < 0) return;
            if (catalog == null)
            {
                Debug.LogError("[TowerLayoutGenerator] No catalog assigned — cannot generate.");
                return;
            }

            var slots = FindSlots();
            if (slots.Count == 0)
            {
                Debug.LogWarning("[TowerLayoutGenerator] No RoomSlots found in the scene.");
                return;
            }

            placed.AddRange(TowerLayout.Fill(slots, catalog, netSeed.Value, IsServer));
            generated = true;
            Debug.Log($"[TowerLayoutGenerator] Filled {slots.Count} slots from seed {netSeed.Value} " +
                      $"(server={IsServer}).");
        }

        static List<RoomSlot> FindSlots()
        {
#if UNITY_2023_1_OR_NEWER
            return new List<RoomSlot>(Object.FindObjectsByType<RoomSlot>(FindObjectsSortMode.None));
#else
            return new List<RoomSlot>(Object.FindObjectsOfType<RoomSlot>());
#endif
        }
    }
}
