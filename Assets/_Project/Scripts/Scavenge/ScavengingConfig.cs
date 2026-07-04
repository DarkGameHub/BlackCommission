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
        [Tooltip("Favoured material-class multiplier on Commissioned/Black runs (two-tier §6; PM 2026-06-18, default 1.3).")]
        public float clientPreferenceMultiplier = 1.3f;
        [Tooltip("Condition value multipliers (PM 2026-06-17).")]
        public float conditionGood = 1.0f;
        public float conditionWorn = 0.7f;
        public float conditionDamaged = 0.4f;

        [Header("Relics (two-tier §6, D2 2026-06-26)")]
        [Tooltip("Relic payout ×N for a matched nostalgic client (high variance by design; 1.5–3.0).")]
        public float relicEmotionalMultiplier = 2.0f;
        [Tooltip("Relic payout ×N for a detached institution/collector client (0.6–1.0).")]
        public float relicMismatchMultiplier = 0.8f;
        [Tooltip("Hand-placed relics per map (3–6).")]
        public int relicsPerMap = 4;

        /// <summary>Build a settlement calculator wired to these knobs.</summary>
        public ScavengeSettlementCalculator CreateSettlementCalculator()
            => new ScavengeSettlementCalculator(conditionGood, conditionWorn, conditionDamaged,
                clientPreferenceMultiplier, relicEmotionalMultiplier, relicMismatchMultiplier);

        /// <summary>Build a fresh van weight ledger from the configured capacity.</summary>
        public VanWeightLedger CreateVanLedger() => new VanWeightLedger(vanWeightCapacity);
    }
}
