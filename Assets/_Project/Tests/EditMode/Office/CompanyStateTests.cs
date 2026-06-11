using NUnit.Framework;

namespace BlackCommission.Office.Tests
{
    /// <summary>
    /// Settlement + progression math for the host-authoritative company state — the
    /// "Office Economy & Settlement" high-risk system in design/systems-index.md.
    /// As-built ground truth (also captured in production/session-state/active.md):
    /// start −300G / 300 debt; pressure −25 on success / +12 partial / +35 failure
    /// (+5/+5 partial and +15/+10 failure surcharges while funds/rep negative);
    /// ultimatum then takeover at pressure 100 with negative funds AND rep;
    /// tutorial acquisition costs 150G. Pure logic — no I/O, no networking.
    /// </summary>
    public class CompanyStateTests
    {
        static CompanyState NewCompany() => new CompanyState
        {
            Funds = -300,
            Reputation = 0,
            OfficeLevel = 1,
            Experience = 0,
            Debt = 300
        };

        // ---- Success settlement ----

        [Test]
        public void Success_AddsMoneyReputationAndExperience()
        {
            CompanyState state = NewCompany();

            state.ApplyMissionResult(true, 300, 5, 80, 120f);

            Assert.AreEqual(0, state.Funds, "Start −300 + full reward 300 should land at 0.");
            Assert.AreEqual(5, state.Reputation);
            Assert.AreEqual(80, state.Experience);
            Assert.IsTrue(state.LastMissionSucceeded);
            Assert.AreEqual(120f, state.LastMissionTimeSeconds);
        }

        [Test]
        public void Success_IncrementsLostItemJobCounter()
        {
            CompanyState state = NewCompany();

            state.ApplyMissionResult(true, 300, 5, 80, 60f);

            Assert.AreEqual(1, state.CompletedLostItemJobs);
        }

        [Test]
        public void Success_WithoutLostItemProgressFlag_DoesNotIncrementJobCounter()
        {
            CompanyState state = NewCompany();

            state.ApplyMissionResult(true, 300, 5, 80, 60f, countsTowardLostItemProgress: false);

            Assert.AreEqual(0, state.CompletedLostItemJobs);
        }

        [Test]
        public void Success_ReducesPressureBy25_FloorAtZero()
        {
            CompanyState state = NewCompany();
            state.HostileTakeoverPressure = 30;

            state.ApplyMissionResult(true, 300, 5, 80, 60f);

            Assert.AreEqual(5, state.HostileTakeoverPressure);

            state.ApplyMissionResult(true, 300, 5, 80, 60f);

            Assert.AreEqual(0, state.HostileTakeoverPressure, "Pressure must not go negative.");
        }

        [Test]
        public void Success_BelowPressure70_ClearsUltimatum()
        {
            CompanyState state = NewCompany();
            state.HostileTakeoverPressure = 90;
            state.HasHostileTakeoverUltimatum = true;

            state.ApplyMissionResult(true, 300, 5, 80, 60f);

            Assert.AreEqual(65, state.HostileTakeoverPressure);
            Assert.IsFalse(state.HasHostileTakeoverUltimatum,
                "Dropping below 70 pressure must lift the takeover ultimatum.");
        }

        // ---- Partial settlement ----

        [Test]
        public void Partial_AddsExperienceButNoJobProgress()
        {
            CompanyState state = NewCompany();

            state.ApplyMissionResult(false, 60, 0, 15, 60f, true, MvpMissionResultKind.Partial);

            Assert.AreEqual(-240, state.Funds);
            Assert.AreEqual(15, state.Experience, "Partial settlements still pay experience.");
            Assert.AreEqual(0, state.CompletedLostItemJobs);
            Assert.AreEqual(0, state.FailedJobs, "Partial is not a failure.");
        }

        [Test]
        public void Partial_WhileStillInDebt_GainsSurchargedPressure()
        {
            CompanyState state = NewCompany();

            // Funds end at −240 (< 0), reputation 0 (not < 0): 12 + 5 + 0 = 17.
            state.ApplyMissionResult(false, 60, 0, 15, 60f, true, MvpMissionResultKind.Partial);

            Assert.AreEqual(17, state.HostileTakeoverPressure);
        }

        [Test]
        public void Partial_WithPositiveFundsAndReputation_GainsBasePressureOnly()
        {
            CompanyState state = NewCompany();
            state.Funds = 100;
            state.Reputation = 3;

            state.ApplyMissionResult(false, 60, 0, 15, 60f, true, MvpMissionResultKind.Partial);

            Assert.AreEqual(12, state.HostileTakeoverPressure);
        }

        [Test]
        public void Partial_PressureIsCappedAt100()
        {
            CompanyState state = NewCompany();
            state.Reputation = -1;
            state.HostileTakeoverPressure = 95;

            state.ApplyMissionResult(false, 60, 0, 15, 60f, true, MvpMissionResultKind.Partial);

            Assert.AreEqual(100, state.HostileTakeoverPressure);
        }

        // ---- Failure settlement ----

        [Test]
        public void Failure_NeverGrantsExperience()
        {
            CompanyState state = NewCompany();

            state.ApplyMissionResult(false, 20, -2, 999, 60f, true, MvpMissionResultKind.Failed);

            Assert.AreEqual(0, state.Experience, "Failed runs must pay 0 XP even if a value is passed.");
            Assert.AreEqual(1, state.FailedJobs);
            Assert.AreEqual(-280, state.Funds, "Consolation money still lands.");
            Assert.AreEqual(-2, state.Reputation);
        }

        [Test]
        public void Failure_WhileInDebtAndDisreputable_GainsMaximumPressure()
        {
            CompanyState state = NewCompany();
            state.Reputation = -3;

            // 35 base + 15 (funds < 0) + 10 (rep < 0) = 60.
            state.ApplyMissionResult(false, 20, -2, 0, 60f, true, MvpMissionResultKind.Failed);

            Assert.AreEqual(60, state.HostileTakeoverPressure);
        }

        // ---- Hostile takeover: ultimatum then acquisition ----

        [Test]
        public void Failure_AtPressure100WhileBroke_IssuesUltimatumFirst()
        {
            CompanyState state = NewCompany();
            state.Reputation = -3;
            state.HostileTakeoverPressure = 70;

            state.ApplyMissionResult(false, 20, -2, 0, 60f, true, MvpMissionResultKind.Failed);

            Assert.AreEqual(100, state.HostileTakeoverPressure);
            Assert.IsTrue(state.HasHostileTakeoverUltimatum);
            Assert.IsTrue(state.WasRecentlyIssuedTakeoverUltimatum);
            Assert.IsFalse(state.WasRecentlyHostileAcquired, "First strike is a warning, not the takeover.");
        }

        [Test]
        public void Failure_SecondStrikeUnderUltimatum_ExecutesHostileTakeover()
        {
            CompanyState state = NewCompany();
            state.Reputation = -3;
            state.OfficeLevel = 2;
            state.Experience = 150;
            state.CompletedLostItemJobs = 2;
            state.HasAcquiredTutorialOffice = true;
            state.HostileTakeoverPressure = 70;
            state.ApplyMissionResult(false, 20, -2, 0, 60f, true, MvpMissionResultKind.Failed); // ultimatum

            state.ApplyMissionResult(false, 20, -2, 0, 60f, true, MvpMissionResultKind.Failed); // takeover

            Assert.IsTrue(state.WasRecentlyHostileAcquired);
            Assert.IsFalse(state.HasHostileTakeoverUltimatum);
            Assert.AreEqual(1, state.OfficeLevel, "Takeover demotes the office one level.");
            Assert.AreEqual(800, state.Debt, "Takeover piles on 500 extra debt.");
            Assert.LessOrEqual(state.Funds, -500, "Funds are forced to −500 or worse.");
            Assert.LessOrEqual(state.Reputation, -5);
            Assert.AreEqual(0, state.Experience);
            Assert.AreEqual(0, state.CompletedLostItemJobs);
            Assert.IsFalse(state.HasAcquiredTutorialOffice);
            Assert.AreEqual(35, state.HostileTakeoverPressure, "Post-takeover pressure resets to 35.");
        }

        [Test]
        public void Failure_AtPressure100WithPositiveFunds_DoesNotIssueUltimatum()
        {
            CompanyState state = NewCompany();
            state.Funds = 500;
            state.Reputation = -3;
            state.HostileTakeoverPressure = 99;

            state.ApplyMissionResult(false, 20, -2, 0, 60f, true, MvpMissionResultKind.Failed);

            Assert.AreEqual(100, state.HostileTakeoverPressure);
            Assert.IsFalse(state.HasHostileTakeoverUltimatum,
                "Solvent companies are not taken over, even at max pressure.");
        }

        // ---- Office level ups ----

        [Test]
        public void LevelUp_At300Experience_ReachesLevel2()
        {
            CompanyState state = NewCompany();

            state.ApplyMissionResult(true, 300, 5, 300, 60f);

            Assert.AreEqual(2, state.OfficeLevel);
            Assert.AreEqual(0, state.Experience, "Spent XP is consumed by the level-up.");
        }

        [Test]
        public void LevelUp_CarriesOverflowExperienceAcrossMultipleLevels()
        {
            CompanyState state = NewCompany();

            // L1→L2 costs 300, L2→L3 costs 600; 950 XP → level 3 with 50 left.
            state.ApplyMissionResult(true, 300, 5, 950, 60f);

            Assert.AreEqual(3, state.OfficeLevel);
            Assert.AreEqual(50, state.Experience);
        }

        [Test]
        public void LevelUp_StopsAtLevelCap8()
        {
            CompanyState state = NewCompany();
            state.OfficeLevel = 8;

            state.ApplyMissionResult(true, 300, 5, 99999, 60f);

            Assert.AreEqual(8, state.OfficeLevel);
        }

        // ---- Tutorial acquisition (registry: tutorial_acquisition_cost = 150G) ----

        [Test]
        public void TutorialAcquisition_Costs150()
        {
            Assert.AreEqual(150, NewCompany().TutorialAcquisitionCost);
        }

        [Test]
        public void TutorialAcquisition_RequiresTwoJobsFundsAndLowPressure()
        {
            CompanyState state = NewCompany();
            state.Funds = 200;
            state.CompletedLostItemJobs = 2;
            state.HostileTakeoverPressure = 69;

            Assert.IsTrue(state.TryAcquireTutorialOffice());
            Assert.AreEqual(50, state.Funds);
            Assert.AreEqual(2, state.OfficeLevel);
            Assert.AreEqual(1, state.Reputation);
            Assert.IsTrue(state.HasAcquiredTutorialOffice);
            Assert.AreEqual(49, state.HostileTakeoverPressure, "Acquisition vents 20 pressure.");
        }

        [Test]
        public void TutorialAcquisition_BlockedAtPressure70()
        {
            CompanyState state = NewCompany();
            state.Funds = 200;
            state.CompletedLostItemJobs = 2;
            state.HostileTakeoverPressure = 70;

            Assert.IsFalse(state.TryAcquireTutorialOffice());
            Assert.AreEqual(200, state.Funds, "A refused acquisition must not charge money.");
        }

        [Test]
        public void TutorialAcquisition_BlockedWithOnlyOneCompletedJob()
        {
            CompanyState state = NewCompany();
            state.Funds = 200;
            state.CompletedLostItemJobs = 1;

            Assert.IsFalse(state.TryAcquireTutorialOffice());
        }

        [Test]
        public void TutorialAcquisition_BlockedWhenAlreadyAcquired()
        {
            CompanyState state = NewCompany();
            state.Funds = 400;
            state.CompletedLostItemJobs = 2;
            Assert.IsTrue(state.TryAcquireTutorialOffice());

            Assert.IsFalse(state.TryAcquireTutorialOffice(), "The tutorial office can only be bought once.");
        }

        // ---- Derived flags ----

        [Test]
        public void IsHostileTakeoverRisk_TrueAtHighPressureWhileBroke()
        {
            CompanyState state = NewCompany();
            state.HostileTakeoverPressure = 70;

            Assert.IsTrue(state.IsHostileTakeoverRisk);

            state.Funds = 10;
            Assert.IsFalse(state.IsHostileTakeoverRisk, "Solvent + no ultimatum = no risk flag.");
        }
    }
}
