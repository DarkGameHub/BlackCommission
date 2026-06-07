using UnityEngine;

namespace BlackCommission.Level
{
    /// <summary>
    /// Scene-side geometry for one connector in the tower graph. The connectivity graph
    /// itself is authored in code (<see cref="TowerTopologyV3"/>) and resolved per seed by
    /// <see cref="TowerTopology"/>; this component just supplies the physical corridor/door
    /// and its rubble blocker, matched by <see cref="id"/>.
    ///
    /// On generation the server-chosen seed yields an open-edge set; the generator calls
    /// <see cref="ApplyOpen"/> on every connector so each peer shows the identical maze.
    /// </summary>
    public class Connector : MonoBehaviour
    {
        [Header("Identity (must match an edge id in TowerTopologyV3)")]
        [Tooltip("Stable edge id, e.g. \"T4\" or \"E-LH\". Matched against the resolved open set.")]
        public string id;

        [Header("Endpoints (authoring reference only)")]
        public string aSlotId;
        public string bSlotId;
        public EdgeKind kind = EdgeKind.Corridor;

        [Header("Geometry to toggle")]
        [Tooltip("The walkable corridor/door mesh+collider. Enabled when the edge is OPEN.")]
        [SerializeField] GameObject geometry;
        [Tooltip("Rubble/tarp/locked filler (carries a NavMeshObstacle). Enabled when CLOSED.")]
        [SerializeField] GameObject blocker;

        /// <summary>Whether this connector is currently open (runtime state).</summary>
        public bool IsOpen { get; private set; } = true;

        /// <summary>
        /// Show/hide the walkable geometry vs the blocker. Closing enables the blocker so its
        /// NavMeshObstacle carves agents around it; opening restores the walkable path.
        /// </summary>
        public void ApplyOpen(bool open)
        {
            IsOpen = open;
            if (geometry != null) geometry.SetActive(open);
            if (blocker != null) blocker.SetActive(!open);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = kind switch
            {
                EdgeKind.Stair => new Color(0.8f, 0.8f, 0.2f),
                EdgeKind.ScaffoldDrop => new Color(1f, 0.5f, 0.1f),
                EdgeKind.Door => new Color(0.3f, 0.6f, 1f),
                _ => new Color(0.5f, 0.9f, 0.6f)
            };
            Gizmos.DrawWireCube(transform.position + Vector3.up * 0.25f, new Vector3(2f, 0.5f, 2f));
        }
    }
}
