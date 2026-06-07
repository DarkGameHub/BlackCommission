using UnityEngine;

namespace BlackCommission.Level
{
    /// <summary>
    /// One entry in the room pool. This is your "100 rooms": author many of these,
    /// each tagged with a size, and the generator drops a matching one into each
    /// same-size slot. The prefab is the room's *content* (furniture, loot, props,
    /// or a gameplay object) placed at the slot anchor inside the fixed shell.
    /// </summary>
    [CreateAssetMenu(menuName = "Black Commission/Room Def", fileName = "RoomDef")]
    public class RoomDef : ScriptableObject
    {
        [Header("Identity")]
        public string roomName = "Unnamed Room";

        [Tooltip("Must equal the slot's size for this room to be eligible.")]
        public RoomSizeClass size = RoomSizeClass.Medium;

        [Tooltip("Content placed at the slot anchor. If it has a NetworkObject, only " +
                 "the server spawns it (it then syncs); otherwise every peer instantiates " +
                 "it deterministically from the shared seed.")]
        public GameObject contentPrefab;

        [Header("Placement constraints")]
        [Tooltip("Which slot role this room may fill. Random rooms fill Random slots; " +
                 "fixed gameplay rooms (objective, power gate) target their role.")]
        public RoomSlotRole roleFilter = RoomSlotRole.Random;

        public bool floor1Only;
        public bool floor2Only;

        [Tooltip("Relative odds vs other eligible rooms of the same size.")]
        public int weight = 1;

        [Tooltip("If false, this room appears at most once per generated layout.")]
        public bool allowDuplicates;

        /// <summary>True if this room can legally go into the given slot.</summary>
        public bool Fits(RoomSlot slot)
        {
            if (size != slot.size) return false;
            if (roleFilter != slot.role) return false;
            if (floor1Only && slot.floor != 1) return false;
            if (floor2Only && slot.floor != 2) return false;
            return true;
        }
    }
}
