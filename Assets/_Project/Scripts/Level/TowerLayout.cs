using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace BlackCommission.Level
{
    /// <summary>
    /// Pure, deterministic slot-filling logic. Given the same slots, catalog, and seed
    /// it produces the same room assignment on every machine — that is what keeps the
    /// random layout in sync across host and clients without sending the whole layout
    /// over the network (only the seed travels).
    ///
    /// Networked content prefabs (those with a NetworkObject) are spawned by the server
    /// only and replicate normally; non-networked decor is instantiated locally on every
    /// peer. Either way the *choice* of room is identical, because selection never
    /// depends on whether we are the server.
    /// </summary>
    public static class TowerLayout
    {
        /// <summary>
        /// Fill every slot. Returns the GameObjects placed locally (for clearing/preview).
        /// </summary>
        /// <param name="isServer">
        /// True on the host/server (and in editor preview). Controls who instantiates
        /// networked content; selection is identical regardless.
        /// </param>
        public static List<GameObject> Fill(IReadOnlyList<RoomSlot> slots, TowerRoomCatalog catalog,
            int seed, bool isServer)
        {
            var placed = new List<GameObject>();
            if (slots == null || catalog == null) return placed;

            // Deterministic slot order: every peer iterates in the same sequence.
            var ordered = new List<RoomSlot>(slots);
            ordered.Sort((a, b) => string.CompareOrdinal(SlotKey(a), SlotKey(b)));

            var rng = new System.Random(seed);
            var used = new HashSet<RoomDef>();

            foreach (var slot in ordered)
            {
                if (slot == null) continue;

                var candidates = catalog.Candidates(slot, used);
                if (candidates.Count == 0)
                {
                    // No eligible room. Advance the RNG by a fixed amount anyway so a
                    // missing pool entry for one slot does not desync the rest.
                    rng.Next();
                    continue;
                }

                RoomDef chosen = WeightedPick(candidates, rng);
                if (!chosen.allowDuplicates) used.Add(chosen);

                GameObject go = Place(slot, chosen, isServer);
                if (go != null)
                {
                    slot.placedContent = go;
                    placed.Add(go);
                }
            }

            return placed;
        }

        static GameObject Place(RoomSlot slot, RoomDef def, bool isServer)
        {
            bool networked = def.contentPrefab.GetComponent<NetworkObject>() != null;

            // On a client, networked content arrives via replication — do not instantiate it.
            if (networked && !isServer)
                return null;

            if (networked)
            {
                // Server spawns at world pose (no parenting before spawn).
                GameObject go = Object.Instantiate(def.contentPrefab,
                    slot.transform.position, slot.transform.rotation);
                go.name = def.roomName;
                var netObj = go.GetComponent<NetworkObject>();
                if (netObj != null && NetworkManager.Singleton != null &&
                    NetworkManager.Singleton.IsListening)
                {
                    netObj.Spawn(true);
                }
                return go;
            }

            // Pure decor: parent under the slot, instantiated identically on every peer.
            GameObject decor = Object.Instantiate(def.contentPrefab,
                slot.transform.position, slot.transform.rotation, slot.transform);
            decor.name = def.roomName;
            return decor;
        }

        static RoomDef WeightedPick(List<RoomDef> candidates, System.Random rng)
        {
            int total = 0;
            foreach (var c in candidates) total += Mathf.Max(1, c.weight);

            int roll = rng.Next(0, total);
            foreach (var c in candidates)
            {
                roll -= Mathf.Max(1, c.weight);
                if (roll < 0) return c;
            }
            return candidates[candidates.Count - 1];
        }

        static string SlotKey(RoomSlot s)
        {
            // Prefer the explicit id; fall back to hierarchy path for stability.
            if (!string.IsNullOrEmpty(s.slotId)) return s.slotId;
            return s.transform.GetSiblingIndex().ToString("00000") + "_" + s.name;
        }
    }
}
