using UnityEngine;

namespace BlackCommission.Level
{
    /// <summary>
    /// The four standard room footprints from the floor-plan kit
    /// (design/levels/abandoned-tower-floorplan.md). A slot only accepts a
    /// RoomDef whose <see cref="RoomDef.size"/> matches.
    /// </summary>
    public enum RoomSizeClass
    {
        Small,   // S  4x4
        Medium,  // M  8x8
        Large,   // L  12x8
        Hall     // XL 16x12
    }

    /// <summary>
    /// What this slot is for. Random slots draw from the catalog pool; the
    /// others are fixed gameplay anchors that must always be present and solvable.
    /// </summary>
    public enum RoomSlotRole
    {
        Random,       // shuffled content from the catalog
        Objective,    // the sales model (沙盘) pedestal + monster nest
        PowerGate,    // the breaker that unlocks floor 2
        Van,          // dispatch van: spawn / return / partial-settlement
        Stair,        // stair core (never randomized; must align across floors)
        Fixed         // authored content, not randomized, not a special gameplay role
    }

    /// <summary>
    /// A marker dropped at the centre of a room shell. The shell (walls, doors,
    /// corridors, stairs) is authored once by the floor-plan builder so navigation
    /// and netcode stay valid; the generator only fills the *content* at this anchor.
    /// </summary>
    public class RoomSlot : MonoBehaviour
    {
        [Tooltip("Only RoomDefs of this size can be placed here.")]
        public RoomSizeClass size = RoomSizeClass.Medium;

        [Tooltip("Random = drawn from the catalog; others are fixed gameplay anchors.")]
        public RoomSlotRole role = RoomSlotRole.Random;

        [Tooltip("1 = ground floor, 2 = upper floor. Used to filter floor-locked rooms.")]
        public int floor = 1;

        [Tooltip("Stable id used to order slots deterministically across host/clients.")]
        public string slotId;

        /// <summary>What the generator actually placed here this run (runtime only).</summary>
        [System.NonSerialized] public GameObject placedContent;

        void OnDrawGizmos()
        {
            Gizmos.color = role switch
            {
                RoomSlotRole.Objective => new Color(1f, 0.55f, 0.1f),
                RoomSlotRole.PowerGate => new Color(0.2f, 0.9f, 0.4f),
                RoomSlotRole.Van       => new Color(0.3f, 0.6f, 1f),
                RoomSlotRole.Stair     => new Color(0.8f, 0.8f, 0.8f),
                RoomSlotRole.Fixed     => new Color(0.6f, 0.6f, 0.6f),
                _ => new Color(1f, 0.9f, 0.2f)
            };
            Vector3 box = size switch
            {
                RoomSizeClass.Small  => new Vector3(4f, 0.5f, 4f),
                RoomSizeClass.Medium => new Vector3(8f, 0.5f, 8f),
                RoomSizeClass.Large  => new Vector3(12f, 0.5f, 8f),
                _                    => new Vector3(16f, 0.5f, 12f)
            };
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.25f, box);
        }
    }
}
