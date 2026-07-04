using System.Collections.Generic;
using NUnit.Framework;
using BlackCommission.Scavenge;

/// <summary>
/// EditMode coverage for the two-tier settlement formula (quick-spec revision APPROVED
/// 2026-06-26 §7): salvage pays base × cond × (favouredClass ? 1.3 : 1); relics pay
/// base × cond × (matched ? 2.0 : mismatched ? 0.8 : 1). Relics ignore the class-preference
/// flag; Free Salvage runs (no client) pay relics plain market rate.
/// </summary>
public class ScavengeSettlementTwoTierTests
{
    static ScavengeSettlementCalculator NewCalc() => new ScavengeSettlementCalculator();

    static List<SettlementItem> Relic(int baseValue, ItemCondition cond, RelicReception reception,
        bool favoured = false)
        => new List<SettlementItem>
        {
            new SettlementItem("relic", baseValue, cond, favoured, ScavengeTier.Relic, reception)
        };

    [Test]
    public void test_settlement_relic_matched_client_pays_emotional_multiplier()
    {
        // Arrange + Act: 100G Good relic, nostalgic client matched (×2.0 default)
        var r = NewCalc().Settle(Relic(100, ItemCondition.Good, RelicReception.Matched));

        // Assert
        Assert.AreEqual(200, r.Total);
    }

    [Test]
    public void test_settlement_relic_mismatched_client_pays_detached_discount()
    {
        // 100G Good relic, institution/collector client (×0.8 default)
        var r = NewCalc().Settle(Relic(100, ItemCondition.Good, RelicReception.Mismatched));
        Assert.AreEqual(80, r.Total);
    }

    [Test]
    public void test_settlement_relic_no_client_pays_market_rate()
    {
        // Free Salvage run: no client to receive the relic → plain market rate
        var r = NewCalc().Settle(Relic(100, ItemCondition.Good, RelicReception.NoClient));
        Assert.AreEqual(100, r.Total);
    }

    [Test]
    public void test_settlement_relic_ignores_class_preference_flag()
    {
        // A relic wrongly flagged favoured must NOT take the 1.3× class multiplier (spec: relics
        // don't walk the material-class path).
        var r = NewCalc().Settle(Relic(100, ItemCondition.Good, RelicReception.NoClient, favoured: true));
        Assert.AreEqual(100, r.Total);
        Assert.IsFalse(r.Lines[0].PreferenceApplied);
    }

    [Test]
    public void test_settlement_relic_condition_stacks_with_emotional_multiplier()
    {
        // 100 × 0.7 (Worn) × 2.0 (matched) = 140
        var r = NewCalc().Settle(Relic(100, ItemCondition.Worn, RelicReception.Matched));
        Assert.AreEqual(140, r.Total);
    }

    [Test]
    public void test_settlement_mixed_delivery_total_is_sum_of_two_tier_lines()
    {
        // Arrange: favoured salvage 100 (→130) + matched relic 50 (→100) + plain salvage 30 (→30)
        var items = new List<SettlementItem>
        {
            new SettlementItem("pot", 100, ItemCondition.Good, isFavoured: true),
            new SettlementItem("letter", 50, ItemCondition.Good, false, ScavengeTier.Relic, RelicReception.Matched),
            new SettlementItem("tool", 30, ItemCondition.Good),
        };

        // Act
        var r = NewCalc().Settle(items);

        // Assert
        Assert.AreEqual(260, r.Total);
        Assert.AreEqual(130, r.Lines[0].Payout);
        Assert.AreEqual(100, r.Lines[1].Payout);
        Assert.AreEqual(30, r.Lines[2].Payout);
    }

    [Test]
    public void test_settlement_line_carries_tier_and_reception_for_reveal()
    {
        // The reveal UI needs tier + reception on the line to pick its grammar.
        var r = NewCalc().Settle(Relic(50, ItemCondition.Good, RelicReception.Matched));
        Assert.AreEqual(ScavengeTier.Relic, r.Lines[0].Tier);
        Assert.AreEqual(RelicReception.Matched, r.Lines[0].RelicReception);
    }

    [Test]
    public void test_settlement_relic_knobs_are_injected()
    {
        // Custom knobs (config-driven): emotional 3.0 / mismatch 0.6
        var calc = new ScavengeSettlementCalculator(relicEmotionalMultiplier: 3.0f, relicMismatchMultiplier: 0.6f);
        Assert.AreEqual(300, calc.Settle(Relic(100, ItemCondition.Good, RelicReception.Matched)).Total);
        Assert.AreEqual(60, calc.Settle(Relic(100, ItemCondition.Good, RelicReception.Mismatched)).Total);
    }

    [Test]
    public void test_settlement_legacy_ctor_defaults_to_salvage_backcompat()
    {
        // Pre-two-tier call sites (4-arg ctor) must keep settling exactly as before.
        var item = new SettlementItem("old", 100, ItemCondition.Good, true);
        Assert.AreEqual(ScavengeTier.Salvage, item.Tier);
        Assert.AreEqual(RelicReception.NoClient, item.RelicReception);
        Assert.AreEqual(130, NewCalc().Settle(new List<SettlementItem> { item }).Total);
    }
}
