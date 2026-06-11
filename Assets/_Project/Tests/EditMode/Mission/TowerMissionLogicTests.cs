using NUnit.Framework;

/// <summary>
/// EditMode coverage for TowerMissionLogic (quick-spec tower-mission-manager-2026-06-10
/// acceptance criteria): state transitions, completeness math, reject threshold,
/// all-downed failure, terminal latching.
/// </summary>
public class TowerMissionLogicTests
{
    static TowerMissionLogic NewLogic() => new TowerMissionLogic(0.03f, 0.5f);

    [Test]
    public void test_initial_state_in_progress_full_completeness()
    {
        var logic = NewLogic();
        Assert.AreEqual(TowerMissionState.InProgress, logic.State);
        Assert.AreEqual(1f, logic.Completeness, 1e-5f);
        Assert.IsFalse(logic.IsTerminal);
    }

    [Test]
    public void test_first_pickup_latches_objective_secured_once()
    {
        var logic = NewLogic();
        Assert.IsTrue(logic.NotifySecured());
        Assert.AreEqual(TowerMissionState.ObjectiveSecured, logic.State);
        Assert.IsFalse(logic.NotifySecured(), "second pickup must not re-fire the transition");
    }

    [Test]
    public void test_three_drops_yield_91_percent_and_273g()
    {
        var logic = NewLogic();
        logic.NotifySecured();
        for (int i = 0; i < 3; i++) logic.NotifyHardDrop();
        Assert.AreEqual(0.91f, logic.Completeness, 1e-3f);
        Assert.AreEqual(273, logic.ScaleDeliveredMoney(300), "spec example: 300G × 91% = 273G");
    }

    [Test]
    public void test_completeness_clamps_at_zero()
    {
        var logic = NewLogic();
        for (int i = 0; i < 50; i++) logic.NotifyHardDrop();
        Assert.AreEqual(0f, logic.Completeness, 1e-5f);
    }

    [Test]
    public void test_departure_with_column_at_threshold_delivers()
    {
        var logic = NewLogic();
        logic.NotifySecured();
        // 16 drops = 52% — above the 50% threshold.
        for (int i = 0; i < 16; i++) logic.NotifyHardDrop();
        Assert.AreEqual(TowerMissionState.Delivered, logic.ResolveDeparture(true));
    }

    [Test]
    public void test_departure_below_threshold_rejected_to_partial()
    {
        var logic = NewLogic();
        logic.NotifySecured();
        // 17 drops = 49% — contract: 密封罩破损不予接收.
        for (int i = 0; i < 17; i++) logic.NotifyHardDrop();
        Assert.AreEqual(TowerMissionState.PartialReturn, logic.ResolveDeparture(true));
    }

    [Test]
    public void test_departure_without_column_is_partial()
    {
        var logic = NewLogic();
        Assert.AreEqual(TowerMissionState.PartialReturn, logic.ResolveDeparture(false));
    }

    [Test]
    public void test_all_downed_fails_once_and_latches()
    {
        var logic = NewLogic();
        logic.NotifySecured();
        Assert.IsTrue(logic.NotifyAllDowned());
        Assert.AreEqual(TowerMissionState.Failed, logic.State);
        Assert.IsFalse(logic.NotifyAllDowned());
        Assert.AreEqual(TowerMissionState.Failed, logic.ResolveDeparture(true),
            "departure after failure must not resurrect the mission");
    }

    [Test]
    public void test_terminal_state_ignores_late_events()
    {
        var logic = NewLogic();
        logic.NotifySecured();
        logic.ResolveDeparture(true);
        Assert.AreEqual(TowerMissionState.Delivered, logic.State);
        Assert.IsFalse(logic.NotifyHardDrop(), "drops after delivery must not change completeness");
        Assert.IsFalse(logic.NotifySecured());
        Assert.IsFalse(logic.NotifyAllDowned());
        Assert.AreEqual(TowerMissionState.Delivered, logic.ResolveDeparture(false));
    }

    [Test]
    public void test_money_scaling_rounds_away_from_zero()
    {
        var logic = NewLogic();
        logic.NotifyHardDrop(); // 97%
        Assert.AreEqual(291, logic.ScaleDeliveredMoney(300)); // 291.0
        var half = new TowerMissionLogic(0.025f, 0.5f);
        half.NotifyHardDrop(); // 97.5% → 292.5 → rounds to 293
        Assert.AreEqual(293, half.ScaleDeliveredMoney(300));
    }
}
