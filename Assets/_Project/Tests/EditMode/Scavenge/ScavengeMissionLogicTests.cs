using NUnit.Framework;
using BlackCommission.Scavenge;

/// <summary>
/// EditMode coverage for ScavengeMissionLogic — the single-fire run state machine that backs
/// ScavengeMissionManager. Verifies a run settles exactly once whether the crew departs or is
/// downed, and never re-settles after it is terminal.
/// </summary>
public class ScavengeMissionLogicTests
{
    [Test]
    public void test_new_run_is_in_progress_and_not_terminal()
    {
        var m = new ScavengeMissionLogic();
        Assert.AreEqual(ScavengeMissionState.InProgress, m.State);
        Assert.IsFalse(m.IsTerminal);
    }

    [Test]
    public void test_resolve_departure_settles_once()
    {
        var m = new ScavengeMissionLogic();
        Assert.IsTrue(m.ResolveDeparture());
        Assert.AreEqual(ScavengeMissionState.Settled, m.State);
        Assert.IsTrue(m.IsTerminal);
        Assert.IsFalse(m.ResolveDeparture(), "a settled run must not settle again");
    }

    [Test]
    public void test_all_downed_fails_once()
    {
        var m = new ScavengeMissionLogic();
        Assert.IsTrue(m.NotifyAllDowned());
        Assert.AreEqual(ScavengeMissionState.Failed, m.State);
        Assert.IsTrue(m.IsTerminal);
        Assert.IsFalse(m.NotifyAllDowned(), "a failed run must not fire again");
    }

    [Test]
    public void test_downed_after_departure_is_ignored()
    {
        var m = new ScavengeMissionLogic();
        m.ResolveDeparture();
        Assert.IsFalse(m.NotifyAllDowned(), "already settled — being downed afterward changes nothing");
        Assert.AreEqual(ScavengeMissionState.Settled, m.State);
    }

    [Test]
    public void test_departure_after_failure_is_ignored()
    {
        var m = new ScavengeMissionLogic();
        m.NotifyAllDowned();
        Assert.IsFalse(m.ResolveDeparture(), "already failed — departing afterward changes nothing");
        Assert.AreEqual(ScavengeMissionState.Failed, m.State);
    }
}
