using NUnit.Framework;
using UnityEngine;

namespace BlackCommission.Office.Tests
{
    /// <summary>
    /// Contract-window clock math: game-hour conversion, overtime detection and
    /// penalties, and the player-facing clock formatting used by the HUD and the
    /// settlement screen.
    /// </summary>
    public class MvpMissionClockTests
    {
        OfficeTaskDefinition task;

        [SetUp]
        public void CreateTask()
        {
            task = ScriptableObject.CreateInstance<OfficeTaskDefinition>();
            task.missionStartClockHour = 8f;
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

        // ---- Time conversion ----

        [Test]
        public void ElapsedGameHours_ConvertsRealSecondsAtTaskRate()
        {
            Assert.AreEqual(2f, MvpMissionClock.GetElapsedGameHours(task, 120f), 0.0001f);
        }

        [Test]
        public void ElapsedGameHours_NegativeSecondsClampToZero()
        {
            Assert.AreEqual(0f, MvpMissionClock.GetElapsedGameHours(task, -50f));
        }

        [Test]
        public void CurrentClockHour_AdvancesFromStartHour()
        {
            // 8:00 start + 90s at 60s/h = 9:30.
            Assert.AreEqual(9.5f, MvpMissionClock.GetCurrentClockHour(task, 90f), 0.0001f);
        }

        [Test]
        public void DeadlineClockHour_IsStartPlusContractWindow()
        {
            Assert.AreEqual(20f, MvpMissionClock.GetDeadlineClockHour(task), 0.0001f);
        }

        [Test]
        public void RemainingGameHours_ClampsToZeroPastDeadline()
        {
            Assert.AreEqual(0f, MvpMissionClock.GetRemainingGameHours(task, 999999f));
        }

        // ---- Overtime ----

        [Test]
        public void Overtime_ZeroInsideContractWindow()
        {
            Assert.AreEqual(0f, MvpMissionClock.GetOvertimeGameHours(task, 720f));
            Assert.AreEqual(0, MvpMissionClock.GetOvertimeMoneyPenalty(task, 720f));
            Assert.AreEqual(0, MvpMissionClock.GetOvertimeReputationPenalty(task, 720f));
        }

        [Test]
        public void Overtime_MoneyPenaltyCeilsPartialHours()
        {
            // 12.5h elapsed → 0.5h overtime → ceil(0.5) × 30 = 30.
            Assert.AreEqual(30, MvpMissionClock.GetOvertimeMoneyPenalty(task, 750f));
        }

        [Test]
        public void Overtime_ReputationPenaltyFloorsByTwoHourBlocks()
        {
            Assert.AreEqual(0, MvpMissionClock.GetOvertimeReputationPenalty(task, 810f),
                "1.5h overtime has not completed a 2h block.");
            Assert.AreEqual(1, MvpMissionClock.GetOvertimeReputationPenalty(task, 840f),
                "2h overtime completes exactly one block.");
            Assert.AreEqual(2, MvpMissionClock.GetOvertimeReputationPenalty(task, 960f),
                "4h overtime completes two blocks.");
        }

        // ---- Null-task defaults ----

        [Test]
        public void NullTask_UsesDefaultScheduleConstants()
        {
            Assert.AreEqual(8f, MvpMissionClock.GetStartClockHour(null));
            Assert.AreEqual(12f, MvpMissionClock.GetContractWindowGameHours(null));
            Assert.AreEqual(60f, MvpMissionClock.GetRealSecondsPerGameHour(null));
        }

        // ---- Player-facing formatting ----

        [Test]
        public void FormatClock_SameDayShowsHourMinute()
        {
            Assert.AreEqual("08:30", MvpMissionClock.FormatClock(8.5f));
        }

        [Test]
        public void FormatClock_PastMidnightShowsNextDayPrefix()
        {
            Assert.AreEqual("次日 01:00", MvpMissionClock.FormatClock(25f));
        }

        [Test]
        public void FormatClock_TwoDaysOutShowsDayCounter()
        {
            Assert.AreEqual("第3天 01:00", MvpMissionClock.FormatClock(49f));
        }

        [Test]
        public void FormatGameHours_RoundsMinutesAndCarriesToWholeHours()
        {
            Assert.AreEqual("0h", MvpMissionClock.FormatGameHours(0f));
            Assert.AreEqual("1h30m", MvpMissionClock.FormatGameHours(1.5f));
            Assert.AreEqual("2h", MvpMissionClock.FormatGameHours(1.9999f));
        }

        [Test]
        public void DaylightLabel_MapsClockHourToChineseBands()
        {
            Assert.AreEqual("清晨", MvpMissionClock.GetDaylightLabel(6f));
            Assert.AreEqual("下午", MvpMissionClock.GetDaylightLabel(14f));
            Assert.AreEqual("深夜", MvpMissionClock.GetDaylightLabel(23f));
            Assert.AreEqual("深夜", MvpMissionClock.GetDaylightLabel(26f), "Hours wrap modulo 24.");
        }
    }
}
