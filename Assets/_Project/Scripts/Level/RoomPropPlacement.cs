using UnityEngine;

namespace BlackCommission.Level
{
    /// <summary>
    /// Baked by <c>TowerRoomPoolBuilder</c> onto each wall-hugging prop: which wall it sits against
    /// and its offset along that wall (metres from the room centre). At fill time
    /// <see cref="RoomDoorClearance"/> reads this to slide only the props that actually sit across a
    /// real door, leaving everything else exactly where it was authored. Centre props carry
    /// <see cref="DoorEdge.None"/> and are ignored (they never reach a wall, so never block a door).
    /// </summary>
    public class RoomPropPlacement : MonoBehaviour
    {
        [Tooltip("Wall this prop hugs (N/S/E/W). None = centre prop, never blocks a door.")]
        public DoorEdge wall = DoorEdge.None;

        [Tooltip("Signed offset of the prop along its wall, metres from the room centre.")]
        public float along;
    }
}
