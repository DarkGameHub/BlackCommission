using UnityEngine;

namespace BlackCommission.Scavenge
{
    /// <summary>
    /// Tuning knobs for the scavenging loop (quick-spec §Tuning Knobs), spec-locked
    /// defaults. Authored at <c>Assets/Resources/Config/ScavengingConfig.asset</c> so the
    /// runtime can <c>Resources.Load</c> it. Feeds <see cref="VanWeightLedger"/> (capacity),
    /// <see cref="LootSpawnPlanner"/> (item count) and <see cref="ScavengeSettlementCalculator"/>
    /// (condition + client-preference multipliers).
    /// </summary>
    [CreateAssetMenu(menuName = "Black Commission/Scavenging Config", fileName = "ScavengingConfig")]
    public class ScavengingConfig : ScriptableObject
    {
        [Header("Van")]
        [Tooltip("Shared team cargo capacity in weight units (spec default 12).")]
        public int vanWeightCapacity = 12;

        [Header("Spawn")]
        public int itemsPerMapInstanceMin = 10;
        public int itemsPerMapInstanceMax = 14;
        [Tooltip("Light items pocketable per player slot (spec default 2).")]
        public int lightItemPocketSlots = 2;

        [Header("Settlement")]
        [Tooltip("Favoured-category multiplier on Commissioned/Black runs (PM 2026-06-18, default 1.3).")]
        public float clientPreferenceMultiplier = 1.3f;
        [Tooltip("Condition value multipliers (PM 2026-06-17).")]
        public float conditionGood = 1.0f;
        public float conditionWorn = 0.7f;
        public float conditionDamaged = 0.4f;

        /// <summary>Build a settlement calculator wired to these knobs.</summary>
        public ScavengeSettlementCalculator CreateSettlementCalculator()
            => new ScavengeSettlementCalculator(conditionGood, conditionWorn, conditionDamaged, clientPreferenceMultiplier);

        /// <summary>Build a fresh van weight ledger from the configured capacity.</summary>
        public VanWeightLedger CreateVanLedger() => new VanWeightLedger(vanWeightCapacity);
    }
}
