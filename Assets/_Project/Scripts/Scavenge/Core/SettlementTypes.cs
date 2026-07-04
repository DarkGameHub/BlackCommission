namespace BlackCommission.Scavenge
{
    /// <summary>
    /// Physical condition of a scavenged item; scales its settlement value
    /// (PM-locked: Good ×1.0 / Worn ×0.7 / Damaged ×0.4, multipliers are tunable knobs).
    /// Three states only — the Contaminated state was cut 2026-06-18; degradation bottoms out at Damaged.
    /// </summary>
    public enum ItemCondition
    {
        Good,
        Worn,
        Damaged,
    }

    /// <summary>
    /// A delivered item's settlement-relevant facts — pure data input to the calculator.
    /// <see cref="IsFavoured"/> is pre-computed by the caller (content layer): true when the item's
    /// category is one the run's commissioned client favours, which pays the client-preference
    /// multiplier at settlement (scavenging-core-loop §0 D-G — there is no designated target item).
    /// </summary>
    public readonly struct SettlementItem
    {
        public readonly string Id;
        public readonly int BaseValue;
        public readonly ItemCondition Condition;
        public readonly bool IsFavoured;

        /// <summary>Salvage (economy body) or Relic (emotional anchor) — two-tier revision 2026-06-26.</summary>
        public readonly ScavengeTier Tier;

        /// <summary>Relic only: how the run's client receives it (caller-computed, like <see cref="IsFavoured"/>).</summary>
        public readonly RelicReception RelicReception;

        public SettlementItem(string id, int baseValue, ItemCondition condition, bool isFavoured = false,
            ScavengeTier tier = ScavengeTier.Salvage, RelicReception relicReception = RelicReception.NoClient)
        {
            Id = id;
            BaseValue = baseValue;
            Condition = condition;
            IsFavoured = isFavoured;
            Tier = tier;
            RelicReception = relicReception;
        }
    }

    /// <summary>One revealed settlement line (output): what a single item paid and why.</summary>
    public readonly struct SettlementLine
    {
        public readonly string ItemId;
        public readonly int BaseValue;
        public readonly ItemCondition Condition;

        /// <summary>True when the client-preference multiplier was applied (salvage in a favoured material class).</summary>
        public readonly bool PreferenceApplied;
        public readonly int Payout;

        /// <summary>Salvage or Relic — drives the reveal's line grammar (relics get the client-reception note).</summary>
        public readonly ScavengeTier Tier;

        /// <summary>Relic only: the client reception that priced this line (settlement-text hook).</summary>
        public readonly RelicReception RelicReception;

        public SettlementLine(string itemId, int baseValue, ItemCondition condition,
            bool preferenceApplied, int payout,
            ScavengeTier tier = ScavengeTier.Salvage, RelicReception relicReception = RelicReception.NoClient)
        {
            ItemId = itemId;
            BaseValue = baseValue;
            Condition = condition;
            PreferenceApplied = preferenceApplied;
            Payout = payout;
            Tier = tier;
            RelicReception = relicReception;
        }
    }
}
