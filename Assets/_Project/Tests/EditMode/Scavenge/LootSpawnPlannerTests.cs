using System.Collections.Generic;
using NUnit.Framework;
using BlackCommission.Scavenge;

/// <summary>
/// EditMode coverage for LootSpawnPlanner: determinism per seed, count within range,
/// surface filtering, no anchor reused, and degenerate-input safety.
/// </summary>
public class LootSpawnPlannerTests
{
    // ── fixtures ──────────────────────────────────────────────────────────────

    static List<LootAnchorSlot> Anchors() => new List<LootAnchorSlot>
    {
        new LootAnchorSlot(1, LootSurface.Floor),
        new LootAnchorSlot(2, LootSurface.DeskSurface),
        new LootAnchorSlot(3, LootSurface.ShelfSlot),
        new LootAnchorSlot(4, LootSurface.Cabinet),
        new LootAnchorSlot(5, LootSurface.CrateTop),
        new LootAnchorSlot(6, LootSurface.Wall),
    };

    // One item allowed on any surface — guarantees every anchor is fillable.
    static List<ScavengeItemSpec> AnywherePool() => new List<ScavengeItemSpec>
    {
        new ScavengeItemSpec("anywhere", WeightClass.Light),
    };

    static List<ScavengeItemSpec> MixedPool() => new List<ScavengeItemSpec>
    {
        new ScavengeItemSpec("doc",   WeightClass.Light,  LootSurface.DeskSurface, LootSurface.ShelfSlot, LootSurface.Cabinet),
        new ScavengeItemSpec("crate", WeightClass.Heavy,  LootSurface.Floor, LootSurface.CrateTop),
        new ScavengeItemSpec("tool",  WeightClass.Medium, LootSurface.ShelfSlot, LootSurface.CrateTop, LootSurface.Floor),
        new ScavengeItemSpec("sign",  WeightClass.Light,  LootSurface.Wall),
    };

    static string Signature(List<LootPlacement> plan)
    {
        var parts = new List<string>();
        foreach (var p in plan) parts.Add(p.AnchorId + "=" + p.ItemId);
        parts.Sort(System.StringComparer.Ordinal);
        return string.Join(",", parts);
    }

    // ── tests ─────────────────────────────────────────────────────────────────

    [Test]
    public void test_same_seed_yields_identical_plan()
    {
        var a = LootSpawnPlanner.Plan(1234, Anchors(), MixedPool(), 3, 6);
        var b = LootSpawnPlanner.Plan(1234, Anchors(), MixedPool(), 3, 6);
        Assert.AreEqual(Signature(a), Signature(b));
    }

    [Test]
    public void test_different_seeds_produce_more_than_one_distinct_plan()
    {
        var signatures = new HashSet<string>();
        for (int seed = 0; seed < 12; seed++)
            signatures.Add(Signature(LootSpawnPlanner.Plan(seed, Anchors(), MixedPool(), 4, 6)));
        Assert.Greater(signatures.Count, 1, "varying the seed should vary the plan");
    }

    [Test]
    public void test_item_count_stays_within_target_range()
    {
        // An anywhere-pool guarantees every chosen anchor is fillable, so count == target.
        for (int seed = 0; seed < 20; seed++)
        {
            var plan = LootSpawnPlanner.Plan(seed, Anchors(), AnywherePool(), 2, 4);
            Assert.GreaterOrEqual(plan.Count, 2);
            Assert.LessOrEqual(plan.Count, 4);
        }
    }

    [Test]
    public void test_target_clamps_to_anchor_count()
    {
        var anchors = new List<LootAnchorSlot>
        {
            new LootAnchorSlot(1, LootSurface.Floor),
            new LootAnchorSlot(2, LootSurface.Floor),
        };
        for (int seed = 0; seed < 10; seed++)
        {
            var plan = LootSpawnPlanner.Plan(seed, anchors, AnywherePool(), 5, 10);
            Assert.LessOrEqual(plan.Count, 2, "cannot place more items than there are anchors");
        }
    }

    [Test]
    public void test_item_never_spawns_on_disallowed_surface()
    {
        var anchors = new List<LootAnchorSlot>
        {
            new LootAnchorSlot(1, LootSurface.Floor),
            new LootAnchorSlot(2, LootSurface.DeskSurface),
        };
        var pool = new List<ScavengeItemSpec>
        {
            new ScavengeItemSpec("crate", WeightClass.Heavy, LootSurface.Floor, LootSurface.CrateTop),
            new ScavengeItemSpec("doc",   WeightClass.Light, LootSurface.DeskSurface),
        };
        for (int seed = 0; seed < 20; seed++)
        {
            var plan = LootSpawnPlanner.Plan(seed, anchors, pool, 2, 2);
            foreach (var p in plan)
            {
                if (p.AnchorId == 1) Assert.AreEqual("crate", p.ItemId, "floor anchor must get the floor item");
                if (p.AnchorId == 2) Assert.AreEqual("doc", p.ItemId, "desk anchor must get the desk item");
            }
        }
    }

    [Test]
    public void test_anchor_with_no_matching_item_is_skipped()
    {
        var anchors = new List<LootAnchorSlot> { new LootAnchorSlot(1, LootSurface.Floor) };
        var pool = new List<ScavengeItemSpec> { new ScavengeItemSpec("wallart", WeightClass.Light, LootSurface.Wall) };
        var plan = LootSpawnPlanner.Plan(7, anchors, pool, 1, 1);
        Assert.AreEqual(0, plan.Count, "no item fits a Floor anchor → nothing spawns there");
    }

    [Test]
    public void test_no_anchor_used_twice()
    {
        var plan = LootSpawnPlanner.Plan(99, Anchors(), AnywherePool(), 4, 6);
        var seen = new HashSet<int>();
        foreach (var p in plan)
            Assert.IsTrue(seen.Add(p.AnchorId), "anchor " + p.AnchorId + " was assigned twice");
    }

    [Test]
    public void test_empty_or_null_inputs_return_empty_plan()
    {
        Assert.AreEqual(0, LootSpawnPlanner.Plan(1, new List<LootAnchorSlot>(), AnywherePool(), 1, 5).Count);
        Assert.AreEqual(0, LootSpawnPlanner.Plan(1, Anchors(), new List<ScavengeItemSpec>(), 1, 5).Count);
        Assert.AreEqual(0, LootSpawnPlanner.Plan(1, null, AnywherePool(), 1, 5).Count);
        Assert.AreEqual(0, LootSpawnPlanner.Plan(1, Anchors(), null, 1, 5).Count);
    }

    [Test]
    public void test_min_greater_than_max_is_clamped_not_throwing()
    {
        List<LootPlacement> plan = null;
        Assert.DoesNotThrow(() => plan = LootSpawnPlanner.Plan(3, Anchors(), AnywherePool(), 5, 2));
        Assert.LessOrEqual(plan.Count, Anchors().Count);
    }

    [Test]
    public void test_item_with_no_surface_restriction_is_allowed_anywhere()
    {
        var item = new ScavengeItemSpec("any", WeightClass.Light);
        Assert.IsTrue(item.AllowsSurface(LootSurface.Floor));
        Assert.IsTrue(item.AllowsSurface(LootSurface.Wall));
    }
}
