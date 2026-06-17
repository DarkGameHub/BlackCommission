using NUnit.Framework;
using BlackCommission.Scavenge;

/// <summary>
/// EditMode coverage for VanWeightLedger + WeightClass units (scavenging quick-spec §3:
/// shared van capacity, Light/Medium/Heavy = 1/2/4 units, full van rejects new items).
/// </summary>
public class VanWeightLedgerTests
{
    [Test]
    public void test_new_ledger_starts_empty_with_full_remaining()
    {
        var v = new VanWeightLedger(12);
        Assert.AreEqual(12, v.Capacity);
        Assert.AreEqual(0, v.Load);
        Assert.AreEqual(12, v.Remaining);
        Assert.IsFalse(v.IsFull);
    }

    [Test]
    public void test_negative_capacity_clamps_to_zero_and_is_full()
    {
        var v = new VanWeightLedger(-5);
        Assert.AreEqual(0, v.Capacity);
        Assert.IsTrue(v.IsFull);
    }

    [Test]
    public void test_can_fit_rejects_nonpositive_and_overflow()
    {
        var v = new VanWeightLedger(4);
        Assert.IsFalse(v.CanFit(0));
        Assert.IsFalse(v.CanFit(-2));
        Assert.IsTrue(v.CanFit(4));
        Assert.IsFalse(v.CanFit(5));
    }

    [Test]
    public void test_try_add_loads_when_it_fits()
    {
        var v = new VanWeightLedger(12);
        Assert.IsTrue(v.TryAdd(4));
        Assert.AreEqual(4, v.Load);
        Assert.AreEqual(8, v.Remaining);
    }

    [Test]
    public void test_try_add_rejects_and_preserves_load_when_over_capacity()
    {
        var v = new VanWeightLedger(4);
        Assert.IsTrue(v.TryAdd(3));
        Assert.IsFalse(v.TryAdd(2), "3 + 2 > 4 must be rejected");
        Assert.AreEqual(3, v.Load, "a rejected add must not change the load");
    }

    [Test]
    public void test_exact_fill_is_allowed_and_marks_full()
    {
        var v = new VanWeightLedger(4);
        Assert.IsTrue(v.TryAdd(4));
        Assert.IsTrue(v.IsFull);
        Assert.AreEqual(0, v.Remaining);
    }

    [Test]
    public void test_weight_class_units_are_1_2_4()
    {
        Assert.AreEqual(1, WeightClass.Light.Units());
        Assert.AreEqual(2, WeightClass.Medium.Units());
        Assert.AreEqual(4, WeightClass.Heavy.Units());
    }

    [Test]
    public void test_try_add_weight_class_overload_uses_unit_cost()
    {
        var v = new VanWeightLedger(4);
        Assert.IsTrue(v.TryAdd(WeightClass.Heavy)); // costs 4
        Assert.IsTrue(v.IsFull);
        Assert.IsFalse(v.TryAdd(WeightClass.Light));
    }

    [Test]
    public void test_remove_decrements_and_clamps_at_zero()
    {
        var v = new VanWeightLedger(12);
        v.TryAdd(6);
        v.Remove(2);
        Assert.AreEqual(4, v.Load);
        v.Remove(100);
        Assert.AreEqual(0, v.Load, "remove must clamp at zero");
    }

    [Test]
    public void test_remove_ignores_nonpositive()
    {
        var v = new VanWeightLedger(12);
        v.TryAdd(5);
        v.Remove(0);
        v.Remove(-3);
        Assert.AreEqual(5, v.Load);
    }

    [Test]
    public void test_reset_empties_the_van()
    {
        var v = new VanWeightLedger(12);
        v.TryAdd(10);
        v.Reset();
        Assert.AreEqual(0, v.Load);
        Assert.IsFalse(v.IsFull);
    }

    [Test]
    public void test_spec_scenario_capacity_12_mixed_load_rejects_when_tight()
    {
        // quick-spec §3: Heavy=4, Medium=2, Light=1, shared capacity 12.
        var v = new VanWeightLedger(12);
        Assert.IsTrue(v.TryAdd(WeightClass.Heavy));   // 4
        Assert.IsTrue(v.TryAdd(WeightClass.Heavy));   // 8
        Assert.IsTrue(v.TryAdd(WeightClass.Medium));  // 10
        Assert.AreEqual(2, v.Remaining);
        Assert.IsFalse(v.TryAdd(WeightClass.Heavy), "a Heavy (4) won't fit in 2 remaining");
        Assert.IsTrue(v.TryAdd(WeightClass.Medium));  // 12
        Assert.IsTrue(v.IsFull);
        Assert.IsFalse(v.TryAdd(WeightClass.Light), "a full van rejects even the lightest item");
    }
}
