namespace BlackCommission.Scavenge
{
    /// <summary>
    /// Physical condition of a scavenged item; scales its settlement value
    /// (PM 2026-06-17: Good ×1.0 / Worn ×0.7 / Damaged ×0.4, multipliers are tunable knobs).
    /// </summary>
    public enum ItemCondition
    {
        Good,
        Worn,
        Damaged,
    }

    /// <summary>A delivered item's settlement-relevant facts — pure data input to the calculator.</summary>
    public readonly struct SettlementItem
    {
        public readonly string Id;
        public readonly int BaseValue;
        public readonly ItemCondition Condition;
        public readonly bool IsCommissionedTarget;

        public SettlementItem(string id, int baseValue, ItemCondition condition, bool isCommissionedTarget = false)
        {
            Id = id;
            BaseValue = baseValue;
            Condition = condition;
            IsCommissionedTarget = isCommissionedTarget;
        }
    }

    /// <summary>One revealed settlement line (output): what a single item paid and why.</summary>
    public readonly struct SettlementLine
    {
        public readonly string ItemId;
        public readonly int BaseValue;
        public readonly ItemCondition Condition;
        public readonly bool CommissionedBonusApplied;
        public readonly int Payout;

        public SettlementLine(string itemId, int baseValue, ItemCondition condition,
            bool commissionedBonusApplied, int payout)
        {
            ItemId = itemId;
            BaseValue = baseValue;
            Condition = condition;
            CommissionedBonusApplied = commissionedBonusApplied;
            Payout = payout;
        }
    }
}
