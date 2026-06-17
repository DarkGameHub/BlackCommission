using System.Collections.Generic;
using UnityEngine;

namespace BlackCommission.Level
{
    /// <summary>
    /// One furniture placement inside a room module. Authored as data so swapping the
    /// whitebox prefab for a real art asset later is a prefab-ref change — no code,
    /// no re-placement.
    /// </summary>
    [System.Serializable]
    public class DressingPlacement
    {
        public string label = "prop";

        [Tooltip("Furniture prefab to place (whitebox in v1, art asset later).")]
        public GameObject prefab;

        public Vector3 localPosition;
        public Vector3 localEuler;
        public Vector3 localScale = Vector3.one;

        [Range(0f, 1f)]
        [Tooltip("Density knob. 1 = always placed. <1 = placed deterministically per module so re-runs are stable.")]
        public float spawnProbability = 1f;

        [Tooltip("If true, gets a carving NavMeshObstacle so agents route around it. Use only for furniture that blocks the room interior.")]
        public bool centralBlocker = false;

        [Tooltip("If true, also drop a LootAnchor here for the scavenging system.")]
        public bool lootAnchor = false;

        public DressingSurface lootSurface = DressingSurface.Floor;
    }

    /// <summary>
    /// Data-driven furniture list for one room type, applied by <c>RoomDresser</c>
    /// (editor pass) into the matching module prefab. See
    /// <c>design/quick-specs/room-dresser-set-dressing-2026-06-16.md</c>.
    /// </summary>
    [CreateAssetMenu(menuName = "Black Commission/Room Dressing Set", fileName = "Dressing_Room")]
    public class RoomDressingSet : ScriptableObject
    {
        [Tooltip("Module prefab name this set dresses, e.g. Room_Office_4x8.")]
        public string targetModule;

        [Tooltip("Emit LootAnchor markers for the scavenging system.")]
        public bool lootAnchorsEnabled = true;

        public List<DressingPlacement> placements = new List<DressingPlacement>();
    }
}
