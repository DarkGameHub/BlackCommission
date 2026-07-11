using BlackCommission.WorkOrders.Core;
using NUnit.Framework;

public class WorkOrderStateMachineTests
{
    [Test]
    public void AcceptedOrder_MustPrintBeforeItCanBeTorn()
    {
        var machine = new WorkOrderStateMachine();

        Assert.That(machine.Begin("tower_01"), Is.True);
        Assert.That(machine.State, Is.EqualTo(WorkOrderPrintState.Printing));
        Assert.That(machine.Tear(), Is.False);
        Assert.That(machine.CompletePrint(), Is.True);
        Assert.That(machine.Tear(), Is.True);
        Assert.That(machine.State, Is.EqualTo(WorkOrderPrintState.Torn));
    }

    [Test]
    public void RepeatedAccept_DoesNotDuplicateTheOrder()
    {
        var machine = new WorkOrderStateMachine();

        Assert.That(machine.Begin("tower_01"), Is.True);
        Assert.That(machine.Begin("mars_01"), Is.False);
        Assert.That(machine.TaskId, Is.EqualTo("tower_01"));
    }

    [Test]
    public void Reset_AllowsAReprintCycle()
    {
        var machine = new WorkOrderStateMachine();
        machine.Begin("tower_01");
        machine.CompletePrint();
        machine.Tear();

        machine.Reset();

        Assert.That(machine.State, Is.EqualTo(WorkOrderPrintState.Idle));
        Assert.That(machine.Begin("tower_01"), Is.True);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void InvalidTaskId_CannotBegin(string taskId)
    {
        var machine = new WorkOrderStateMachine();
        Assert.That(machine.Begin(taskId), Is.False);
        Assert.That(machine.State, Is.EqualTo(WorkOrderPrintState.Idle));
    }

}
