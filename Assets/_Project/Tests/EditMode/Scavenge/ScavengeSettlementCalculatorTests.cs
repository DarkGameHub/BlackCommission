using System.Collections.Generic;
using NUnit.Framework;
using BlackCommission.Scavenge;

/// <summary>
/// EditMode coverage for ScavengeSettlementCalculator (scavenging-core-loop §4):
/// per-item condition multipliers (PM-locked 1.0/0.7/0.4), the client-preference multiplier
/// (1.3× for favoured-category items), sum-of-lines total, round-away-from-zero, data-driven knobs.
/// </summary>
public class ScavengeSettlementCalculatorTests
{
    static ScavengeSettlementCalculator NewCalc() => new ScavengeSettlementCalculator();

    static List<SettlementItem> One(string id, int baseValue, ItemCondition cond, bool favoured = false)
        => new List<SettlementItem> { new SettlementItem(id, baseValue, cond, favoured) };

    [Test]
    public void test_empty_delivery_settles_to_zero()
    {
        var r = NewCalc().Settle(new List<SettlementItem>());
        Assert.AreEqual(0, r.Total);
        Assert.AreEqual(0, r.Lines.Count);
    }

    [Test]
    public void test_null_delivery_is_safe()
    {
        var r = NewCalc().Settle(null);
        Assert.AreEqual(0, r.Total);
        Assert.AreEqual(0, r.Lines.Count);
    }

    [Test]
    public void test_good_item_pays_full_base_value()
    {
        var r = NewCalc().Settle(One("doc", 100, ItemCondition.Good));
        Assert.AreEqual(100, r.Lines[0].Payout);
        Assert.AreEqual(100, r.Total);
    }

    [Test]
    public void test_condition_multipliers_applied()
    {
        Assert.AreEqual(100, NewCalc().Settle(One("a", 100, ItemCondition.Good)).Total);
        Assert.AreEqual(70,  NewCalc().Settle(One("a", 100, ItemCondition.Worn)).Total);
        Assert.AreEqual(40,  NewCalc().Settle(One("a", 100, ItemCondition.Damaged)).Total);
    }

    [Test]
    public void test_total_is_sum_of_line_payouts()
    {
        var items = new List<SettlementItem>
        {
            new SettlementItem("a", 100, ItemCondition.Good),     // 100
            new SettlementItem("b", 100, ItemCondition.Worn),     // 70
            new SettlementItem("c", 50,  ItemCondition.Damaged),  // 20
        };
        var r = NewCalc().Settle(items);
        int sum = 0;
        foreach (var l in r.Lines) sum += l.Payout;
        Assert.AreEqual(sum, r.Total, "total must equal the sum of revealed line payouts");
        Assert.AreEqual(190, r.Total);
    }

    [Test]
    public void test_favoured_item_gets_preference_multiplier()
    {
        // 100 × 1.0 (Good) × 1.3 (favoured category) = 130.
        var r = NewCalc().Settle(One("effects", 100, ItemCondition.Good, favoured: true));
        Assert.AreEqual(130, r.Lines[0].Payout);
        Assert.IsTrue(r.Lines[0].PreferenceApplied);
    }

    [Test]
    public void test_preference_stacks_with_condition()
    {
        // 100 × 0.7 (Worn) × 1.3 (favoured) = 91.
        var r = NewCalc().Settle(One("effects", 100, ItemCondition.Worn, favoured: true));
        Assert.AreEqual(91, r.Lines[0].Payout);
    }

    [Test]
    public void test_non_favoured_item_pays_market_rate_no_flag()
    {
        var r = NewCalc().Settle(One("doc", 100, ItemCondition.Good));
        Assert.IsFalse(r.Lines[0].PreferenceApplied);
        Assert.AreEqual(100, r.Lines[0].Payout); // market rate, no multiplier
    }

    [Test]
    public void test_payout_rounds_half_away_from_zero()
    {
        // 0.5 is exact in float; 85 × 0.5 = 42.5 → away-from-zero → 43.
        var calc = new ScavengeSettlementCalculator(conditionWorn: 0.5f);
        Assert.AreEqual(43, calc.Settle(One("a", 85, ItemCondition.Worn)).Total);
    }

    [Test]
    public void test_negative_base_value_clamped_to_zero()
    {
        var r = NewCalc().Settle(One("junk", -50, ItemCondition.Good));
        Assert.AreEqual(0, r.Lines[0].Payout);
        Assert.AreEqual(0, r.Lines[0].BaseValue);
    }

    [Test]
    public void test_custom_knobs_are_respected()
    {
        // Injected: Worn = 0.5, client preference = 2.0 → 100 × 0.5 × 2.0 = 100.
        var calc = new ScavengeSettlementCalculator(conditionWorn: 0.5f, clientPreferenceMultiplier: 2.0f);
        Assert.AreEqual(100, calc.Settle(One("effects", 100, ItemCondition.Worn, favoured: true)).Total);
    }

    [Test]
    public void test_condition_multiplier_lookup_defaults()
    {
        var calc = NewCalc();
        Assert.AreEqual(1.0f, calc.ConditionMultiplier(ItemCondition.Good), 1e-5f);
        Assert.AreEqual(0.7f, calc.ConditionMultiplier(ItemCondition.Worn), 1e-5f);
        Assert.AreEqual(0.4f, calc.ConditionMultiplier(ItemCondition.Damaged), 1e-5f);
    }
}
