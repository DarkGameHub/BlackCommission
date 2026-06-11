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

            // Resolve topology FIRST (which connectors are open this seed), independent of the room
            // catalog — same seed -> same open set on every peer, so the maze is identical everywhere.
            // This runs even before any TowerRoomCatalog exists, so the whitebox can be walked/verified.
            ApplyTopology(netSeed.Value);

            var slots = FindSlots();
            if (catalog != null && slots.Count > 0)
            {
                placed.AddRange(TowerLayout.Fill(slots, catalog, netSeed.Value, IsServer));
                Debug.Log($"[TowerLayoutGenerator] Filled {slots.Count} slots from seed {netSeed.Value} " +
                          $"(server={IsServer}).");
            }
            else
            {
                Debug.Log($"[TowerLayoutGenerator] Topology applied for seed {netSeed.Value}; content fill " +
                          $"skipped (catalog assigned={catalog != null}, slots={slots.Count}).");
            }

            generated = true;
        }

        /// <summary>
        /// Resolve the authoritative connectivity graph for this seed and toggle every scene
        /// <see cref="Connector"/> to match. The graph and validation live in code
        /// (<see cref="TowerPlanV8"/> / <see cref="TowerTopology"/>) so connectivity is
        /// guaranteed; the scene only supplies geometry keyed by edge id (= plan door id).
        /// </summary>
        void ApplyTopology(int seed)
        {
            TopoGraph graph = TowerPlanV8.BuildCanonicalGraph();
            TopoResult result = TowerTopology.Resolve(graph, seed);

            if (result.UsedFallback)
                Debug.LogWarning($"[TowerLayoutGenerator] Topology fell back to all-open for seed {seed} " +
                                 $"(no seeded roll validated within the re-roll cap). Verdict: {result.Verdict}.");
            else if (!result.Ok)
                Debug.LogError($"[TowerLayoutGenerator] Topology invalid even at fallback for seed {seed}: " +
                               $"{result.Verdict}. Check the TowerPlanV8 backbone.");

            int open = 0, closed = 0, unmatched = 0;
#if UNITY_2023_1_OR_NEWER
            var connectors = Object.FindObjectsByType<Connector>(FindObjectsSortMode.None);
#else
            var connectors = Object.FindObjectsOfType<Connector>();
#endif
            foreach (var c in connectors)
            {
                if (c == null) continue;
                if (string.IsNullOrEmpty(c.id)) { unmatched++; continue; }
                bool isOpen = result.OpenEdgeIds.Contains(c.id);
                c.ApplyOpen(isOpen);
                if (isOpen) open++; else closed++;
            }

            Debug.Log($"[TowerLayoutGenerator] Topology seed {seed}: attempt {result.Attempt}, " +
                      $"{open} open / {closed} closed connectors" +
                      (unmatched > 0 ? $", {unmatched} with no id (skipped)" : "") + ".");
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
