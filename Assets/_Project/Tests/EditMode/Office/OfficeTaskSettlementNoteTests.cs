using NUnit.Framework;
using UnityEngine;

namespace BlackCommission.Office.Tests
{
    /// <summary>
    /// Client usage note selection for the settlement card (design/ux/settlement.md).
    /// The pick must be deterministic for a given seed so every peer reads the same
    /// line, route by result kind, and return null (block hidden) when unset.
    /// </summary>
    public class OfficeTaskSettlementNoteTests
    {
        OfficeTaskDefinition task;

        [SetUp]
        public void CreateTaskWithNotes()
        {
            task = ScriptableObject.CreateInstance<OfficeTaskDefinition>();
            task.settlementNotesSuccess = new[] { "success-a", "success-b" };
            task.settlementNotesPartial = new[] { "partial-a" };
            task.settlementNotesFailure = new[] { "failure-a", "failure-b", "failure-c" };
        }

        [TearDown]
        public void DestroyTask()
        {
            Object.DestroyImmediate(task);
        }

        [Test]
        public void GetSettlementNote_RoutesByResultKind()
        {
            Assert.AreEqual("success-a", task.GetSettlementNote(MvpMissionResultKind.Success, 0));
            Assert.AreEqual("partial-a", task.GetSettlementNote(MvpMissionResultKind.Partial, 0));
            Assert.AreEqual("failure-a", task.GetSettlementNote(MvpMissionResultKind.Failed, 0));
        }

        [Test]
        public void GetSettlementNote_SameSeed_SamePick()
        {
            // Peers feed the same settlement-derived seed in; the card text must match.
            string first = task.GetSettlementNote(MvpMissionResultKind.Success, 4217);
            string second = task.GetSettlementNote(MvpMissionResultKind.Success, 4217);

            Assert.AreEqual(first, second);
        }

        [Test]
        public void GetSettlementNote_SeedWrapsAroundPool()
        {
            Assert.AreEqual("failure-a", task.GetSettlementNote(MvpMissionResultKind.Failed, 3));
            Assert.AreEqual("failure-b", task.GetSettlementNote(MvpMissionResultKind.Failed, 4));
            Assert.AreEqual("failure-c", task.GetSettlementNote(MvpMissionResultKind.Failed, 5));
        }

        [Test]
        public void GetSettlementNote_NegativeSeed_StillPicksValidEntry()
        {
            string note = task.GetSettlementNote(MvpMissionResultKind.Success, -7);

            Assert.IsNotNull(note);
            CollectionAssert.Contains(task.settlementNotesSuccess, note);
        }

        [Test]
        public void GetSettlementNote_EmptyPool_ReturnsNull()
        {
            task.settlementNotesPartial = new string[0];

            Assert.IsNull(task.GetSettlementNote(MvpMissionResultKind.Partial, 1));
        }

        [Test]
        public void GetSettlementNote_NullPool_ReturnsNull()
        {
            task.settlementNotesFailure = null;

            Assert.IsNull(task.GetSettlementNote(MvpMissionResultKind.Failed, 1));
        }
    }
}
