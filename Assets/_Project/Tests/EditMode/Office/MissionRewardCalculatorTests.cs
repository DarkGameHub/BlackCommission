using NUnit.Framework;
using UnityEngine;

namespace BlackCommission.Office.Tests
{
    /// <summary>
    /// Settlement reward math (registry formula: settlement_reward) — MONEY ONLY
    /// (reputation/XP removed, PM decision 2026-06-17). Covers all three result kinds,
    /// bonus-evidence gating, and overtime money deductions. Uses a ScriptableObject task
    /// built in-test (no asset I/O).
    /// </summary>
    public class MissionRewardCalculatorTests
    {
        OfficeTaskDefinition task;

        [SetUp]
        public void CreateStandardTask()
        {
            // Canonical tower commission: 300G full / 20G consolation, 12h window at 60s per
            // game hour, 30G per overtime hour.
            task = ScriptableObject.CreateInstance<OfficeTaskDefinition>();
            task.moneyReward = 300;
            task.failureConsolationMoney = 20;
            task.contractWindowGameHours = 12f;
            task.realSecondsPerGameHour = 60f;
            task.overtimeMoneyPenaltyPerGameHour = 30;
        }

        [TearDown]
        public void DestroyTask()
        {
            Object.DestroyImmediate(task);
        }

        MissionRewardResult Calculate(MvpMissionResultKind kind, float timerSeconds = 0f, bool bonus = false)
        {
            return MissionRewardCalculator.Calculate(
                task, kind, timerSeconds, bonus,
                MissionRewardFallbacks.Default, MissionRewardBonus.Default);
        }

        static MissionRewardResult CalculateWithoutTask(MvpMissionResultKind kind,
            float timerSeconds = 0f, bool bonus = false)
        {
            return MissionRewardCalculator.Calculate(
                null, kind, timerSeconds, bonus,
                MissionRewardFallbacks.Default, MissionRewardBonus.Default);
        }

        // ---- Base money per result kind ----

        [Test]
        public void Success_InsideContractWindow_PaysFullMoney()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Success, timerSeconds: 300f);
            Assert.AreEqual(300, r.Money);
            Assert.AreEqual(0, r.OvertimeMoneyPenalty);
        }

        [Test]
        public void Partial_Pays22PercentFlooredAtConsolation()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Partial);
            Assert.AreEqual(66, r.Money, "max(20 consolation, round(300 × 0.22)) = 66.");
        }

        [Test]
        public void Partial_ConsolationFloorWinsWhenHigherThanPercentage()
        {
            task.failureConsolationMoney = 100;
            MissionRewardResult r = Calculate(MvpMissionResultKind.Partial);
            Assert.AreEqual(100, r.Money, "Partial pay floors at the failure consolation.");
        }

        [Test]
        public void Failure_PaysConsolationMoney()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Failed);
            Assert.AreEqual(20, r.Money);
        }

        // ---- Bonus evidence gating (money only) ----

        [Test]
        public void BonusEvidence_OnSuccess_AddsBonusMoney()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Success, bonus: true);
            Assert.AreEqual(390, r.Money, "300 base + 90 bonus.");
        }

        [Test]
        public void BonusEvidence_OnPartial_AddsBonusMoney()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Partial, bonus: true);
            Assert.AreEqual(156, r.Money, "66 partial + 90 bonus.");
        }

        [Test]
        public void BonusEvidence_OnFailure_AddsNothing()
        {
            MissionRewardResult r = Calculate(MvpMissionResultKind.Failed, bonus: true);
            Assert.AreEqual(20, r.Money);
        }

        // ---- Overtime money deductions (no reputation penalty anymore) ----

        [Test]
        public void Overtime_90MinutesPastWindow_DeductsCeiledMoney()
        {
            // 13.5 game hours at 60s each = 810s → 1.5h overtime: money −ceil(1.5) × 30 = −60.
            MissionRewardResult r = Calculate(MvpMissionResultKind.Success, timerSeconds: 810f);
            Assert.AreEqual(1.5f, r.OvertimeGameHours, 0.001f);
            Assert.AreEqual(60, r.OvertimeMoneyPenalty);
            Assert.AreEqual(240, r.Money);
        }

        [Test]
        public void Overtime_AppliesEvenToFailedRuns()
        {
            // As-built: overtime is deducted on every result kind, so a long failed run can
            // settle negative. Documented so any future change is deliberate.
            MissionRewardResult r = Calculate(MvpMissionResultKind.Failed, timerSeconds: 840f);
            Assert.AreEqual(-40, r.Money, "20 consolation − 60 overtime.");
        }

        // ---- No-task fallbacks (registry: full 300 / partial 60 / failure 20) ----

        [Test]
        public void NullTask_UsesMoneyFallbackTable()
        {
            Assert.AreEqual(300, CalculateWithoutTask(MvpMissionResultKind.Success).Money);
            Assert.AreEqual(60, CalculateWithoutTask(MvpMissionResultKind.Partial).Money);
            Assert.AreEqual(20, CalculateWithoutTask(MvpMissionResultKind.Failed).Money);
        }

        [Test]
        public void NullTask_Calculate_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
                CalculateWithoutTask(MvpMissionResultKind.Success, timerSeconds: 5000f, bonus: true));
        }
    }
}
