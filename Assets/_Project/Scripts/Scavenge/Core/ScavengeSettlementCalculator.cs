using System.Collections.Generic;

namespace BlackCommission.Scavenge
{
    /// <summary>Outcome of one settlement: the per-item reveal lines plus the run total.</summary>
    public sealed class SettlementResult
    {
        public IReadOnlyList<SettlementLine> Lines { get; }
        public int Total { get; }

        public SettlementResult(IReadOnlyList<SettlementLine> lines, int total)
        {
            Lines = lines;
            Total = total;
        }
    }

    /// <summary>
    /// Pure settlement math for scavenging (scavenging-core-loop §4). Each delivered item pays
    /// <c>baseValue × conditionMultiplier × preferenceMultiplier</c>; an item in one of the run
    /// client's favoured categories (caller-flagged <see cref="SettlementItem.IsFavoured"/>) is
    /// multiplied by <c>clientPreferenceMultiplier</c>; the run total is the sum of per-item
    /// payouts. Per-item payouts round away from zero so the revealed lines add up to the total
    /// exactly. Multipliers are injected (data-driven knobs) with the locked defaults — condition
    /// 1.0 / 0.7 / 0.4 and client preference 1.3× (PM 2026-06-18). No Unity, no dispute (P4),
    /// no designated target item (dropped — D-G), no free-salvage market mode (layers on later).
    /// </summary>
    public sealed class ScavengeSettlementCalculator
    {
        readonly float conditionGood;
        readonly float conditionWorn;
        readonly float conditionDamaged;
        readonly float clientPreference;

        public ScavengeSettlementCalculator(
            float conditionGood = 1.0f,
            float conditionWorn = 0.7f,
            float conditionDamaged = 0.4f,
            float clientPreferenceMultiplier = 1.3f)
        {
            this.conditionGood = conditionGood;
            this.conditionWorn = conditionWorn;
            this.conditionDamaged = conditionDamaged;
            this.clientPreference = clientPreferenceMultiplier;
        }

        /// <summary>Value multiplier for an item's condition.</summary>
        public float ConditionMultiplier(ItemCondition condition)
        {
            switch (condition)
            {
                case ItemCondition.Good:    return conditionGood;
                case ItemCondition.Worn:    return conditionWorn;
                case ItemCondition.Damaged: return conditionDamaged;
                default:                    return conditionGood;
            }
        }

        /// <summary>
        /// Settle a delivered set into per-item payout lines + run total. A null or empty
        /// delivery settles to 0. Negative base values are clamped to 0. Items flagged
        /// <see cref="SettlementItem.IsFavoured"/> receive the client-preference multiplier.
        /// </summary>
        public SettlementResult Settle(IReadOnlyList<SettlementItem> deliveredItems)
        {
            var lines = new List<SettlementLine>();
            int total = 0;

            if (deliveredItems != null)
            {
                for (int i = 0; i < deliveredItems.Count; i++)
                {
                    var item = deliveredItems[i];
                    int baseValue = item.BaseValue < 0 ? 0 : item.BaseValue;

                    bool favoured = item.IsFavoured;
                    float value = baseValue * ConditionMultiplier(item.Condition);
                    if (favoured) value *= clientPreference;

                    int payout = (int)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
                    total += payout;

                    lines.Add(new SettlementLine(item.Id, baseValue, item.Condition, favoured, payout));
                }
            }

            return new SettlementResult(lines, total);
        }
    }
}
