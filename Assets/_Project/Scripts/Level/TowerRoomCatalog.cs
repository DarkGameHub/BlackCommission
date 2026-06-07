using System.Collections.Generic;
using UnityEngine;

namespace BlackCommission.Level
{
    /// <summary>
    /// The full pool of rooms for a tower. Drop every RoomDef in here. The generator
    /// queries it per slot. Keeping the pool in one asset makes it easy to balance
    /// (size distribution, weights) and to swap pools per map later.
    /// </summary>
    [CreateAssetMenu(menuName = "Black Commission/Tower Room Catalog", fileName = "TowerRoomCatalog")]
    public class TowerRoomCatalog : ScriptableObject
    {
        public List<RoomDef> rooms = new List<RoomDef>();

        /// <summary>
        /// All rooms that legally fit the slot, optionally excluding ones already used
        /// (for non-duplicate rooms). Returned in a stable order so the seeded RNG picks
        /// identically on every peer.
        /// </summary>
        public List<RoomDef> Candidates(RoomSlot slot, HashSet<RoomDef> used)
        {
            var result = new List<RoomDef>();
            foreach (var def in rooms)
            {
                if (def == null || def.contentPrefab == null) continue;
                if (!def.Fits(slot)) continue;
                if (!def.allowDuplicates && used != null && used.Contains(def)) continue;
                result.Add(def);
            }
            // Stable ordering: name then instance id, so host and clients agree.
            result.Sort((a, b) =>
            {
                int c = string.CompareOrdinal(a.roomName, b.roomName);
                return c != 0 ? c : a.GetInstanceID().CompareTo(b.GetInstanceID());
            });
            return result;
        }
    }
}
