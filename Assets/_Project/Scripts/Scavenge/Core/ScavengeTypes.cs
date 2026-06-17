namespace BlackCommission.Scavenge
{
    /// <summary>
    /// Carry weight class. The enum value IS the weight cost in shared van units
    /// (scavenging quick-spec §3: Light = 1, Medium = 2, Heavy = 4), so
    /// <c>(int)weightClass</c> — or <see cref="WeightClassExtensions.Units"/> — gives the cost.
    /// </summary>
    public enum WeightClass
    {
        Light = 1,
        Medium = 2,
        Heavy = 4,
    }

    /// <summary>
    /// Surface a loot anchor sits on, used to filter which items may spawn there.
    /// Mirrors <c>BlackCommission.Level.DressingSurface</c> value-for-value so the
    /// runtime layer can map between them with a direct cast; defined here as its own
    /// type to keep this pure-logic assembly engine- and scene-independent.
    /// </summary>
    public enum LootSurface
    {
        Floor = 0,
        DeskSurface = 1,
        ShelfSlot = 2,
        Cabinet = 3,
        CrateTop = 4,
        Wall = 5,
    }

    /// <summary>A loot anchor described as plain data for <see cref="LootSpawnPlanner"/>.</summary>
    public readonly struct LootAnchorSlot
    {
        public readonly int Id;
        public readonly LootSurface Surface;

        public LootAnchorSlot(int id, LootSurface surface)
        {
            Id = id;
            Surface = surface;
        }
    }

    /// <summary>One planner decision: spawn item <see cref="ItemId"/> at anchor <see cref="AnchorId"/>.</summary>
    public readonly struct LootPlacement
    {
        public readonly int AnchorId;
        public readonly string ItemId;

        public LootPlacement(int anchorId, string itemId)
        {
            AnchorId = anchorId;
            ItemId = itemId;
        }
    }

    /// <summary>Weight-class helpers.</summary>
    public static class WeightClassExtensions
    {
        /// <summary>Weight cost in shared van units (Light 1 / Medium 2 / Heavy 4).</summary>
        public static int Units(this WeightClass weight) => (int)weight;
    }
}
