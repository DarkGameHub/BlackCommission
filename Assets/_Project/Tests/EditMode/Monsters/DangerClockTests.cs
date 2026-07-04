using NUnit.Framework;
using BlackCommission.Monsters;

/// <summary>
/// EditMode coverage for the site danger track math (danger-infection quick-spec 2026-06-18):
/// phase boundaries at 0/8/18/28 min, the continuous 0→100 level, multi-trip head starts, and
/// the per-phase seed activation counts consumed by MonsterSpawnBootstrap.
/// </summary>
public class DangerClockTests
{
    static DangerClock SpecClock() => new DangerClock();   // 480/600/600 s defaults

    [Test]
    public void test_danger_phase_boundaries_match_spec_timeline()
    {
        var clock = SpecClock();
        Assert.AreEqual(DangerPhase.Survey, clock.PhaseAt(0f));
        Assert.AreEqual(DangerPhase.Survey, clock.PhaseAt(479f));
        Assert.AreEqual(DangerPhase.Active, clock.PhaseAt(480f));
        Assert.AreEqual(DangerPhase.Active, clock.PhaseAt(1079f));
        Assert.AreEqual(DangerPhase.Pursuit, clock.PhaseAt(1080f));
        Assert.AreEqual(DangerPhase.Pursuit, clock.PhaseAt(1679f));
        Assert.AreEqual(DangerPhase.Saturation, clock.PhaseAt(1680f));
    }

    [Test]
    public void test_danger_level_rises_monotonically_and_clamps_at_100()
    {
        var clock = SpecClock();
        Assert.AreEqual(0f, clock.DangerLevelAt(0f), 0.001f);
        Assert.AreEqual(50f, clock.DangerLevelAt(840f), 0.001f);   // half of 28 min
        Assert.AreEqual(100f, clock.DangerLevelAt(1680f), 0.001f);
        Assert.AreEqual(100f, clock.DangerLevelAt(99999f), 0.001f); // clamped past saturation
        Assert.AreEqual(0f, clock.DangerLevelAt(-5f), 0.001f);      // negative time clamped
    }

    [Test]
    public void test_danger_reentry_advances_starting_phase_per_trip()
    {
        // Spec: trip 2 enters at Active, trip 3+ enters at Pursuit.
        var clock = SpecClock();
        Assert.AreEqual(DangerPhase.Active, clock.PhaseAt(0f, tripIndex: 1));
        Assert.AreEqual(DangerPhase.Pursuit, clock.PhaseAt(0f, tripIndex: 2));
        Assert.AreEqual(DangerPhase.Pursuit, clock.PhaseAt(0f, tripIndex: 5));
    }

    [Test]
    public void test_danger_seed_counts_scale_by_phase()
    {
        // Mars (6 seeds): 2 → 4 → 6; tower (3 seeds): 1 → 2 → 3.
        Assert.AreEqual(2, DangerClock.ActiveSeedCount(6, DangerPhase.Survey));
        Assert.AreEqual(4, DangerClock.ActiveSeedCount(6, DangerPhase.Active));
        Assert.AreEqual(6, DangerClock.ActiveSeedCount(6, DangerPhase.Pursuit));
        Assert.AreEqual(6, DangerClock.ActiveSeedCount(6, DangerPhase.Saturation));
        Assert.AreEqual(1, DangerClock.ActiveSeedCount(3, DangerPhase.Survey));
        Assert.AreEqual(2, DangerClock.ActiveSeedCount(3, DangerPhase.Active));
        Assert.AreEqual(3, DangerClock.ActiveSeedCount(3, DangerPhase.Pursuit));
    }

    [Test]
    public void test_danger_seed_count_edge_cases_stay_sane()
    {
        // A map with one seed still opens with it; zero seeds spawn nothing.
        Assert.AreEqual(1, DangerClock.ActiveSeedCount(1, DangerPhase.Survey));
        Assert.AreEqual(0, DangerClock.ActiveSeedCount(0, DangerPhase.Pursuit));
    }

    [Test]
    public void test_danger_custom_knobs_shift_boundaries()
    {
        // 5-min survey / 6-min active / 5-min pursuit → saturation at 16 min.
        var clock = new DangerClock(300f, 360f, 300f);
        Assert.AreEqual(960f, clock.SaturationSeconds, 0.001f);
        Assert.AreEqual(DangerPhase.Active, clock.PhaseAt(300f));
        Assert.AreEqual(DangerPhase.Pursuit, clock.PhaseAt(660f));
        Assert.AreEqual(DangerPhase.Saturation, clock.PhaseAt(960f));
    }
}
