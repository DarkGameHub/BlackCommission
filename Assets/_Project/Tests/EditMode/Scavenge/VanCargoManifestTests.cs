using NUnit.Framework;
using BlackCommission.Scavenge;

/// <summary>
/// EditMode coverage for VanCargoManifest — the deposit gate that backs ScavengeCargoZone
/// (scavenging quick-spec §3 van weight limit + §4 settlement input). Verifies the ledger and
/// the recorded item list stay in lock-step through load / reject / unload / reset.
/// </summary>
public class VanCargoManifestTests
{
    static SettlementItem Item(string id, int value) => new SettlementItem(id, value, ItemCondition.Good);

    [Test]
    public void test_new_manifest_is_empty_with_full_remaining()
    {
        var m = new VanCargoManifest(12);
        Assert.AreEqual(12, m.Capacity);
        Assert.AreEqual(0, m.LoadUnits);
        Assert.AreEqual(12, m.Remaining);
        Assert.AreEqual(0, m.Count);
        Assert.IsFalse(m.IsFull);
    }

    [Test]
    public void test_try_load_records_item_and_consumes_weight()
    {
        var m = new VanCargoManifest(12);
        Assert.IsTrue(m.TryLoad(Item("letter", 40), WeightClass.Light));
        Assert.AreEqual(1, m.Count);
        Assert.AreEqual(1, m.LoadUnits);
        Assert.AreEqual(11, m.Remaining);
        Assert.AreEqual("letter", m.Items[0].Id);
    }

    [Test]
    public void test_load_order_is_preserved()
    {
        var m = new VanCargoManifest(12);
        m.TryLoad(Item("a", 10), WeightClass.Light);
        m.TryLoad(Item("b", 20), WeightClass.Medium);
        m.TryLoad(Item("c", 30), WeightClass.Light);
        Assert.AreEqual("a", m.Items[0].Id);
        Assert.AreEqual("b", m.Items[1].Id);
        Assert.AreEqual("c", m.Items[2].Id);
    }

    [Test]
    public void test_rejected_item_does_not_change_load_or_manifest()
    {
        var m = new VanCargoManifest(4);
        Assert.IsTrue(m.TryLoad(Item("heavy", 200), WeightClass.Heavy)); // fills 4/4
        Assert.IsTrue(m.IsFull);
        Assert.IsFalse(m.TryLoad(Item("late", 10), WeightClass.Light), "a full van rejects new items");
        Assert.AreEqual(1, m.Count, "a rejected item must not be recorded");
        Assert.AreEqual(4, m.LoadUnits);
    }

    [Test]
    public void test_can_load_matches_remaining_capacity_per_class()
    {
        var m = new VanCargoManifest(12);
        m.TryLoad(Item("h1", 100), WeightClass.Heavy);  // 4
        m.TryLoad(Item("h2", 100), WeightClass.Heavy);  // 8
        m.TryLoad(Item("med", 50), WeightClass.Medium); // 10, remaining 2
        Assert.IsFalse(m.CanLoad(WeightClass.Heavy), "Heavy (4) does not fit in 2 remaining");
        Assert.IsTrue(m.CanLoad(WeightClass.Medium), "Medium (2) exactly fits");
        Assert.IsTrue(m.CanLoad(WeightClass.Light));
    }

    [Test]
    public void test_unload_frees_weight_and_removes_the_right_item()
    {
        var m = new VanCargoManifest(12);
        m.TryLoad(Item("a", 10), WeightClass.Light);   // 1
        m.TryLoad(Item("b", 20), WeightClass.Heavy);   // 5
        m.TryLoad(Item("c", 30), WeightClass.Medium);  // 7
        Assert.IsTrue(m.TryUnload(1)); // remove the Heavy "b"
        Assert.AreEqual(2, m.Count);
        Assert.AreEqual(3, m.LoadUnits, "removing the Heavy (4) frees its weight: 7 - 4 = 3");
        Assert.AreEqual("a", m.Items[0].Id);
        Assert.AreEqual("c", m.Items[1].Id);
    }

    [Test]
    public void test_unload_out_of_range_is_ignored()
    {
        var m = new VanCargoManifest(12);
        m.TryLoad(Item("a", 10), WeightClass.Light);
        Assert.IsFalse(m.TryUnload(-1));
        Assert.IsFalse(m.TryUnload(5));
        Assert.AreEqual(1, m.Count);
        Assert.AreEqual(1, m.LoadUnits);
    }

    [Test]
    public void test_unload_then_reload_uses_freed_capacity()
    {
        var m = new VanCargoManifest(4);
        Assert.IsTrue(m.TryLoad(Item("heavy", 200), WeightClass.Heavy)); // full
        Assert.IsFalse(m.TryLoad(Item("light", 5), WeightClass.Light));
        Assert.IsTrue(m.TryUnload(0));
        Assert.IsTrue(m.TryLoad(Item("light", 5), WeightClass.Light), "freed capacity accepts the item");
        Assert.AreEqual(1, m.LoadUnits);
    }

    [Test]
    public void test_reset_empties_items_and_ledger()
    {
        var m = new VanCargoManifest(12);
        m.TryLoad(Item("a", 10), WeightClass.Heavy);
        m.TryLoad(Item("b", 20), WeightClass.Medium);
        m.Reset();
        Assert.AreEqual(0, m.Count);
        Assert.AreEqual(0, m.LoadUnits);
        Assert.IsFalse(m.IsFull);
    }

    [Test]
    public void test_to_settlement_items_is_a_defensive_copy()
    {
        var m = new VanCargoManifest(12);
        m.TryLoad(Item("a", 10), WeightClass.Light);
        var snapshot = m.ToSettlementItems();
        m.TryLoad(Item("b", 20), WeightClass.Light);
        Assert.AreEqual(1, snapshot.Count, "the snapshot must not see items loaded after it was taken");
        Assert.AreEqual(2, m.Count);
    }

    [Test]
    public void test_manifest_feeds_settlement_total()
    {
        // The manifest output drives the settlement calculator (quick-spec §4). Good condition
        // ×1.0, no commissioned target → total is the sum of base values.
        var m = new VanCargoManifest(12);
        m.TryLoad(Item("letter", 40), WeightClass.Light);
        m.TryLoad(Item("specimen", 120), WeightClass.Medium);
        var result = new ScavengeSettlementCalculator().Settle(m.ToSettlementItems());
        Assert.AreEqual(160, result.Total);
        Assert.AreEqual(2, result.Lines.Count);
    }
}
