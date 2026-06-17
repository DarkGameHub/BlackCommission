using UnityEngine;

namespace BlackCommission.Level
{
    /// <summary>
    /// What surface a loot anchor sits on. Lets the scavenging / loot-spawn system
    /// pick an item appropriate to the spot (a document on a desk vs a crate on the floor).
    /// </summary>
    public enum DressingSurface
    {
        Floor,
        DeskSurface,
        ShelfSlot,
        Cabinet,
        CrateTop,
        Wall
    }

    /// <summary>
    /// Marker dropped by <c>RoomDresser</c> on furniture surfaces. The scavenging /
    /// loot-spawn system finds these at runtime to place scavengeable items. This is a
    /// pure marker — no networking, no gameplay logic. RoomDresser owns decoration;
    /// the scavenging system owns what (if anything) spawns here per seed.
    /// </summary>
    public class LootAnchor : MonoBehaviour
    {
        [Tooltip("Surface kind this anchor represents — used by the loot spawner to filter items.")]
        public DressingSurface surface = DressingSurface.Floor;

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.7f, 0.2f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.18f);
        }
    }
}
