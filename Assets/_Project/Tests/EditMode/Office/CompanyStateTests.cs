using NUnit.Framework;

namespace BlackCommission.Office.Tests
{
    /// <summary>
    /// Money-only settlement + tutorial-acquisition math for the host-authoritative company
    /// state. Reputation / Experience / OfficeLevel / HostileTakeoverPressure were removed
    /// (PM decision 2026-06-17): settlement only moves Funds and the job counters. Start
    /// −300G / 300 debt; tutorial acquisition costs 150G. Pure logic — no I/O, no networking.
    /// </summary>
    public class CompanyStateTests
    {
        static CompanyState NewCompany() => new CompanyState { Funds = -300, Debt = 300 };

        // ---- Success settlement ----

        [Test]
        public void Success_AddsMoneyAndRecordsRun()
        {
            CompanyState state = NewCompany();

            state.ApplyMissionResult(true, 300, 120f);

            Assert.AreEqual(0, state.Funds, "Start −300 + full reward 300 should land at 0.");
            Assert.IsTrue(state.LastMissionSucceeded);
            Assert.AreEqual(120f, state.LastMissionTimeSeconds);
        }

        [Test]
        public void Success_IncrementsLostItemJobCounter()
        {
            CompanyState state = NewCompany();

            state.ApplyMissionResult(true, 300, 60f);

            Assert.AreEqual(1, state.CompletedLostItemJobs);
        }

        [Test]
        public void Success_WithoutLostItemProgressFlag_DoesNotIncrementJobCounter()
        {
            CompanyState state = NewCompany();

            state.ApplyMissionResult(true, 300, 60f, countsTowardLostItemProgress: false);

            Assert.AreEqual(0, state.CompletedLostItemJobs);
        }

        // ---- Partial settlement ----

        [Test]
        public void Partial_AddsMoneyButNoJobProgressAndNoFailure()
        {
            CompanyState state = NewCompany();

            state.ApplyMissionResult(false, 60, 60f, true, MvpMissionResultKind.Partial);

            Assert.AreEqual(-240, state.Funds);
            Assert.AreEqual(0, state.CompletedLostItemJobs);
            Assert.AreEqual(0, state.FailedJobs, "Partial is not a failure.");
        }

        // ---- Failure settlement ----

        [Test]
        public void Failure_AddsConsolationMoneyAndIncrementsFailedJobs()
        {
            CompanyState state = NewCompany();

            state.ApplyMissionResult(false, 20, 60f, true, MvpMissionResultKind.Failed);

            Assert.AreEqual(-280, state.Funds, "Consolation money still lands.");
            Assert.AreEqual(1, state.FailedJobs);
            Assert.IsFalse(state.LastMissionSucceeded);
        }

        // ---- Tutorial acquisition (registry: tutorial_acquisition_cost = 150G) ----

        [Test]
        public void TutorialAcquisition_Costs150()
        {
            Assert.AreEqual(150, NewCompany().TutorialAcquisitionCost);
        }

        [Test]
        public void TutorialAcquisition_RequiresTwoJobsAndFunds_DeductsAndSetsFlag()
        {
            CompanyState state = NewCompany();
            state.Funds = 200;
            state.CompletedLostItemJobs = 2;

            Assert.IsTrue(state.TryAcquireTutorialOffice());
            Assert.AreEqual(50, state.Funds);
            Assert.IsTrue(state.HasAcquiredTutorialOffice);
        }

        [Test]
        public void TutorialAcquisition_BlockedWithOnlyOneCompletedJob()
        {
            CompanyState state = NewCompany();
            state.Funds = 200;
            state.CompletedLostItemJobs = 1;

            Assert.IsFalse(state.TryAcquireTutorialOffice());
            Assert.AreEqual(200, state.Funds, "A refused acquisition must not charge money.");
        }

        [Test]
        public void TutorialAcquisition_BlockedWithInsufficientFunds()
        {
            CompanyState state = NewCompany();
            state.Funds = 100; // below the 150 cost
            state.CompletedLostItemJobs = 2;

            Assert.IsFalse(state.TryAcquireTutorialOffice());
            Assert.AreEqual(100, state.Funds);
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
    }
}
