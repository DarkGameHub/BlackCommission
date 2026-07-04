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

    /// <summary>
    /// Two-tier loot architecture (quick-spec revision APPROVED 2026-06-26). Salvage is the
    /// scavenging economy body (weight-vs-value tension, class preference multiplier);
    /// Relics are the hand-placed emotional anchors (letters / photos / drawings / diaries /
    /// civic papers) that settle on client emotional match instead of material class.
    /// </summary>
    public enum ScavengeTier
    {
        Salvage = 0,
        Relic = 1,
    }

    /// <summary>
    /// Tier-1 material class the client preference keys off (quick-spec §2, D1 resolved:
    /// four classes — each one a side of Earth the Mars clients want to consume).
    /// </summary>
    public enum MaterialClass
    {
        Domestic = 0,   // 家居烟火 — how real Earth people lived
        Labour = 1,     // 劳作器械 — real Earth labour
        Natural = 2,    // 自然遗存 — (polluted) Earth ecology
        Culture = 3,    // 文化信仰 — Earth spirit / indulgence
    }

    /// <summary>
    /// How the run's client receives a relic at settlement (quick-spec §3): a nostalgic
    /// personal client pays the emotional multiplier; an institution / collector applies the
    /// detached discount; a run with no client (Free Salvage) pays plain market rate.
    /// </summary>
    public enum RelicReception
    {
        NoClient = 0,
        Matched = 1,
        Mismatched = 2,
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
