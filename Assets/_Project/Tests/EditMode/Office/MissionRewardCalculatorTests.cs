using NUnit.Framework;
using UnityEngine;

namespace BlackCommission.Office.Tests
{
    /// <summary>
    /// Settlement reward math (registry formula: settlement_reward) for all three
    /// result kinds, the bonus-evidence gating, the wrong-homework penalty cap, and
    /// overtime deductions. Uses a ScriptableObject task built in-test (no asset I/O).
    /// </summary>
    public class MissionRewardCalculatorTests
    {
        OfficeTaskDefinition task;

        [SetUp]
        public void CreateStandardTask()
        {
            // Mirrors the canonical lost-homework job: 300G / +5 rep / +80 XP,
            // 20G consolation, −2 rep on failure, 12h window at 60s per game hour,
            // 30G per overtime hour, 1 rep per 2 overtime hours.
            task = ScriptableObject.CreateInstance<OfficeTaskDefinition>();
            task.moneyReward = 300;
            task.reputationReward = 5;
            task.experienceReward = 80;
            task.failureConsolationMoney = 20;
            task.failureReputationPenalty = -2;
            task.failureExperience = 0;
            task.contractWindowGameHours = 12f;
            task.realSecondsPerGameHour = 60f;
            task.overtimeMoneyPenaltyPerGameHour = 30;
            task.overtimeReputationPenaltyBlockGameHours = 2f;
            task.overtimeReputationPenaltyPerBlock = 1;
        }

        [TearDown]
        public void DestroyTask()
        {
            Object.DestroyImmediate(task);
        }

        MissionRewardResult Calculate(MvpMissionResultKind kind, float timerSeconds = 0f,
            bool bonus = false, int wrongAttempts = 0)
        {
            return MissionRewardCalculator.Calculate(
                task, kind, timerSeconds, bonus, wrongAttempts,
                MissionRewardFallbacks.Default, MissionRewardBonus.Default);
        }

        static MissionRewardResult CalculateWithoutTask(MvpMissionResultKind kind,
            float timerSeconds = 0f, bool bonus = false, int wrongAttempts = 0)
        {
            return MissionRewardCalculator.Calculate(
                null, kind, timerSeconds, bonus, wrongAttempts,
                MissionRewardFallbacks.Default, MissionRewardBonus.Default);
        }

        // ---- Base rewards per result kind ----

        [Test]
        public void Success_InsideContractWindow_PaysFullReward()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Success, timerSeconds: 300f);

            Assert.AreEqual(300, r.Money);
            Assert.AreEqual(5, r.Reputation);
            Assert.AreEqual(80, r.Experience);
            Assert.AreEqual(0, r.OvertimeMoneyPenalty);
        }

        [Test]
        public void Partial_Pays22PercentMoneyAnd20PercentExperience_NoReputation()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Partial);

            Assert.AreEqual(66, r.Money, "max(20 consolation, round(300 × 0.22)) = 66.");
            Assert.AreEqual(0, r.Reputation, "Partial returns never pay task reputation.");
            Assert.AreEqual(16, r.Experience, "round(80 × 0.2) = 16.");
        }

        [Test]
        public void Partial_ConsolationFloorWinsWhenHigherThanPercentage()
        {
            task.failureConsolationMoney = 100;

            MissionRewardResult r = Calculate(MvpMissionResultKind.Partial);

            Assert.AreEqual(100, r.Money, "Partial pay floors at the failure consolation.");
        }

        [Test]
        public void Failure_PaysConsolationAndReputationPenalty()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Failed);

            Assert.AreEqual(20, r.Money);
            Assert.AreEqual(-2, r.Reputation);
            Assert.AreEqual(0, r.Experience);
        }

        // ---- Bonus evidence gating ----

        [Test]
        public void BonusEvidence_OnSuccess_AddsMoneyReputationAndExperience()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Success, bonus: true);

            Assert.AreEqual(390, r.Money);
            Assert.AreEqual(6, r.Reputation);
            Assert.AreEqual(100, r.Experience);
        }

        [Test]
        public void BonusEvidence_OnPartial_AddsMoneyAndExperienceButNoReputation()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Partial, bonus: true);

            Assert.AreEqual(156, r.Money, "66 partial + 90 bonus.");
            Assert.AreEqual(0, r.Reputation, "Bonus reputation is success-only.");
            Assert.AreEqual(36, r.Experience, "16 partial + 20 bonus.");
        }

        [Test]
        public void BonusEvidence_OnFailure_AddsNothing()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Failed, bonus: true);

            Assert.AreEqual(20, r.Money);
            Assert.AreEqual(-2, r.Reputation);
            Assert.AreEqual(0, r.Experience);
        }

        // ---- Wrong-homework penalty ----

        [Test]
        public void WrongHomework_DeductsThirtyPerAttempt()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Success, wrongAttempts: 2);

            Assert.AreEqual(240, r.Money, "300 − 2 × 30.");
        }

        [Test]
        public void WrongHomework_PenaltyCapsAtThreeAttempts()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Success, wrongAttempts: 7);

            Assert.AreEqual(210, r.Money, "Cap: 300 − 3 × 30, no matter how many extra attempts.");
        }

        [Test]
        public void WrongHomework_NotChargedOnFailure()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Failed, wrongAttempts: 3);

            Assert.AreEqual(20, r.Money, "Failure consolation is not reduced by wrong attempts.");
        }

        [Test]
        public void WrongHomework_PenaltyHelperClampsNegativeAttempts()
        {
            Assert.AreEqual(0, MissionRewardCalculator.GetWrongHomeworkMoneyPenalty(-1));
        }

        // ---- Overtime deductions ----

        [Test]
        public void Overtime_90MinutesPastWindow_DeductsCeiledMoneyOnly()
        {
            // 13.5 game hours at 60s each = 810s → 1.5h overtime:
            // money −ceil(1.5) × 30 = −60; reputation −floor(1.5 / 2) = 0.
            MissionRewardResult r = Calculate(MvpMissionResultKind.Success, timerSeconds: 810f);

            Assert.AreEqual(1.5f, r.OvertimeGameHours, 0.001f);
            Assert.AreEqual(60, r.OvertimeMoneyPenalty);
            Assert.AreEqual(0, r.OvertimeReputationPenalty);
            Assert.AreEqual(240, r.Money);
            Assert.AreEqual(5, r.Reputation);
        }

        [Test]
        public void Overtime_TwoFullHours_AlsoDeductsReputation()
        {
            // 14 game hours = 840s → 2h overtime: money −60, reputation −1.
            MissionRewardResult r = Calculate(MvpMissionResultKind.Success, timerSeconds: 840f);

            Assert.AreEqual(60, r.OvertimeMoneyPenalty);
            Assert.AreEqual(1, r.OvertimeReputationPenalty);
            Assert.AreEqual(240, r.Money);
            Assert.AreEqual(4, r.Reputation);
        }

        [Test]
        public void Overtime_AppliesEvenToFailedRuns()
        {
            // As-built behavior: overtime is deducted on every result kind, so a long
            // failed run can settle negative. Documented here so any future change is deliberate.
            MissionRewardResult r = Calculate(MvpMissionResultKind.Failed, timerSeconds: 840f);

            Assert.AreEqual(-40, r.Money, "20 consolation − 60 overtime.");
            Assert.AreEqual(-3, r.Reputation, "−2 failure − 1 overtime block.");
        }

        // ---- No-task fallbacks (registry: full_job_reward / partial / failure constants) ----

        [Test]
        public void NullTask_UsesFallbackRewardTable()
        {
            MissionRewardResult success = CalculateWithoutTask(MvpMissionResultKind.Success);
            MissionRewardResult partial = CalculateWithoutTask(MvpMissionResultKind.Partial);
            MissionRewardResult failed = CalculateWithoutTask(MvpMissionResultKind.Failed);

            Assert.AreEqual(300, success.Money);
            Assert.AreEqual(5, success.Reputation);
            Assert.AreEqual(80, success.Experience);
            Assert.AreEqual(60, partial.Money);
            Assert.AreEqual(0, partial.Reputation);
            Assert.AreEqual(15, partial.Experience);
            Assert.AreEqual(20, failed.Money);
            Assert.AreEqual(-2, failed.Reputation);
            Assert.AreEqual(0, failed.Experience);
        }

        [Test]
        public void NullTask_Calculate_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                CalculateWithoutTask(MvpMissionResultKind.Success, timerSeconds: 5000f, bonus: true,
                    wrongAttempts: 3));
        }
    }
}
