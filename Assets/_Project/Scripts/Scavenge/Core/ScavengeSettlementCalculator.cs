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
    /// Pure settlement math for scavenging — two-tier (scavenging-core-loop §4 + two-tier
    /// revision APPROVED 2026-06-26). Salvage pays <c>baseValue × condition ×
    /// (favouredClass ? clientPreference : 1)</c>; relics pay <c>baseValue × condition ×
    /// (matched ? relicEmotional : mismatched ? relicMismatch : 1)</c> — the favoured/reception
    /// flags are caller-computed (content layer). The run total is the sum of per-item payouts;
    /// each rounds away from zero so the revealed lines add up to the total exactly. Multipliers
    /// are injected (data-driven knobs) with the locked defaults — condition 1.0 / 0.7 / 0.4,
    /// class preference 1.3× (PM 2026-06-18), relic emotional 2.0× / mismatch 0.8× (D2 2026-06-26).
    /// No Unity, no dispute (P4), no designated target item (dropped — D-G).
    /// </summary>
    public sealed class ScavengeSettlementCalculator
    {
        readonly float conditionGood;
        readonly float conditionWorn;
        readonly float conditionDamaged;
        readonly float clientPreference;
        readonly float relicEmotional;
        readonly float relicMismatch;

        public ScavengeSettlementCalculator(
            float conditionGood = 1.0f,
            float conditionWorn = 0.7f,
            float conditionDamaged = 0.4f,
            float clientPreferenceMultiplier = 1.3f,
            float relicEmotionalMultiplier = 2.0f,
            float relicMismatchMultiplier = 0.8f)
        {
            this.conditionGood = conditionGood;
            this.conditionWorn = conditionWorn;
            this.conditionDamaged = conditionDamaged;
            this.clientPreference = clientPreferenceMultiplier;
            this.relicEmotional = relicEmotionalMultiplier;
            this.relicMismatch = relicMismatchMultiplier;
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
                    float value = baseValue * ConditionMultiplier(item.Condition);

                    // Two-tier formula (quick-spec §7 APPROVED 2026-06-26):
                    //   salvage_i = base × cond × (matchesClass ? clientPreference : 1.0)
                    //   relic_j   = base × cond × (matched ? relicEmotional : mismatched ? relicMismatch : 1.0)
                    bool favoured = item.Tier == ScavengeTier.Salvage && item.IsFavoured;
                    if (favoured) value *= clientPreference;
                    if (item.Tier == ScavengeTier.Relic)
                    {
                        if (item.RelicReception == RelicReception.Matched) value *= relicEmotional;
                        else if (item.RelicReception == RelicReception.Mismatched) value *= relicMismatch;
                    }

                    int payout = (int)System.Math.Round(value, System.MidpointRounding.AwayFromZero);
                    total += payout;

                    lines.Add(new SettlementLine(item.Id, baseValue, item.Condition, favoured, payout,
                        item.Tier, item.RelicReception));
                }
            }

            return new SettlementResult(lines, total);
        }
    }
}
