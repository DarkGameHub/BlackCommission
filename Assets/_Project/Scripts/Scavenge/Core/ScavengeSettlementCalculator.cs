using System.Collections.Generic;

namespace BlackCommission.Scavenge
{
    /// <summary>Outcome of one settlement: the per-item reveal lines plus the run total.</summary>
    public sealed class SettlementResult
    {
        public IReadOnlyList<SettlementLine> Lines { get; }
        public int Total { get; }
        public bool CommissionedTargetDelivered { get; }

        public SettlementResult(IReadOnlyList<SettlementLine> lines, int total, bool commissionedTargetDelivered)
        {
            Lines = lines;
            Total = total;
            CommissionedTargetDelivered = commissionedTargetDelivered;
        }
    }

    /// <summary>
    /// Pure settlement math for scavenging (quick-spec §4/§5). Each delivered item pays
    /// <c>baseValue × conditionMultiplier</c>; the commissioned target (if delivered) is
    /// further multiplied by the commissioned bonus; the run total is the sum of per-item
    /// payouts. Per-item payouts round away from zero so the revealed lines add up to the
    /// total exactly. Multipliers are injected (data-driven knobs) with the locked defaults
    /// — condition 1.0 / 0.7 / 0.4 (PM 2026-06-17) and commissioned bonus 1.4×. No Unity,
    /// no dispute (P4), no free-salvage market mode (those layer on later).
    /// </summary>
    public sealed class ScavengeSettlementCalculator
    {
        readonly float conditionGood;
        readonly float conditionWorn;
        readonly float conditionDamaged;
        readonly float commissionedBonus;

        public ScavengeSettlementCalculator(
            float conditionGood = 1.0f,
            float conditionWorn = 0.7f,
            float conditionDamaged = 0.4f,
            float commissionedBonusMultiplier = 1.4f)
        {
            this.conditionGood = conditionGood;
            this.conditionWorn = conditionWorn;
            this.conditionDamaged = conditionDamaged;
            this.commissionedBonus = commissionedBonusMultiplier;
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
        /// delivery settles to 0. Negative base values are clamped to 0. Any item flagged
        /// as the commissioned target receives the bonus multiplier (callers flag one).
        /// </summary>
        public SettlementResult Settle(IReadOnlyList<SettlementItem> deliveredItems)
        {
            var lines = new List<SettlementLine>();
            int total = 0;
            bool commissionedDelivered = false;

            if (deliveredItems != null)
            {
                for (int i = 0; i < deliveredItems.Count; i++)
                {
                    var item = deliveredItems[i];
                    int baseValue = item.BaseValue < 0 ? 0 : item.BaseValue;

                    bool bonus = item.IsCommissionedTarget;
                    if (bonus) commissionedDelivered = true;

                    float value = baseValue * ConditionMultiplier(item.Condition);
                    if (bonus) value *= commissionedBonus;

                    int payout = (int)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
                    total += payout;

                    lines.Add(new SettlementLine(item.Id, baseValue, item.Condition, bonus, payout));
                }
            }

            return new SettlementResult(lines, total, commissionedDelivered);
        }
    }
}
