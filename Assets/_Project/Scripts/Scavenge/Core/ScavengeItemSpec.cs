namespace BlackCommission.Scavenge
{
    /// <summary>
    /// Pure-data description of a scavengeable item type, consumed by
    /// <see cref="LootSpawnPlanner"/>. The runtime layer (P1b) builds these from the
    /// item-definition ScriptableObjects; this assembly stays engine-free so the
    /// spawn rules are unit-testable in isolation.
    /// </summary>
    public sealed class ScavengeItemSpec
    {
        /// <summary>Stable identifier (matches the item definition asset).</summary>
        public string Id { get; }

        /// <summary>Carry weight class — drives van capacity cost and carry style.</summary>
        public WeightClass Weight { get; }

        readonly LootSurface[] allowedSurfaces;

        /// <param name="allowedSurfaces">
        /// Surfaces this item may spawn on. Pass none to allow any surface.
        /// </param>
        public ScavengeItemSpec(string id, WeightClass weight, params LootSurface[] allowedSurfaces)
        {
            Id = id;
            Weight = weight;
            this.allowedSurfaces = allowedSurfaces ?? System.Array.Empty<LootSurface>();
        }

        /// <summary>
        /// True if this item may spawn on <paramref name="surface"/>. An item authored
        /// with no surface restriction is allowed anywhere.
        /// </summary>
        public bool AllowsSurface(LootSurface surface)
        {
            if (allowedSurfaces.Length == 0) return true;
            for (int i = 0; i < allowedSurfaces.Length; i++)
                if (allowedSurfaces[i] == surface) return true;
            return false;
        }
    }
}
