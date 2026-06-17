using UnityEngine;

namespace BlackCommission.Scavenge
{
    /// <summary>
    /// Authored definition of one scavengeable item type. Data only: the loot spawner (P1b)
    /// instantiates <see cref="prefab"/> at a matching LootAnchor, and the settlement
    /// calculator reads <see cref="baseValue"/>. <see cref="baseValue"/> is PLACEHOLDER until
    /// the economy/balance pass (PM-owned) — the structure is what P1b/P2 consume now.
    /// </summary>
    [CreateAssetMenu(menuName = "Black Commission/Scavenge Item Definition", fileName = "Item_")]
    public class ScavengeItemDefinition : ScriptableObject
    {
        [Tooltip("Stable id — matches the LootPlacement.ItemId the planner emits.")]
        public string id;

        [Tooltip("Player-facing name, shown at settlement (never during the mission).")]
        public string displayName;

        public ScavengeCategory category;

        public WeightClass weight = WeightClass.Light;

        [Tooltip("Surfaces this item may spawn on. Empty = any surface.")]
        public LootSurface[] allowedSurfaces;

        [Tooltip("PLACEHOLDER base value (G). Real values are set in the economy/balance pass.")]
        public int baseValue;

        [Tooltip("Whitebox/real prefab spawned for this item. Null until the visual pass (P1b).")]
        public GameObject prefab;

        /// <summary>Pure-data view consumed by the spawn planner.</summary>
        public ScavengeItemSpec ToSpec()
            => new ScavengeItemSpec(id, weight, allowedSurfaces ?? System.Array.Empty<LootSurface>());
    }
}
