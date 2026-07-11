namespace BlackCommission.WorkOrders.Core
{
    public enum WorkOrderPrintState
    {
        Idle,
        Printing,
        ReadyToTear,
        Torn
    }

    /// <summary>Pure transition rules shared by the network printer and EditMode tests.</summary>
    public sealed class WorkOrderStateMachine
    {
        public WorkOrderPrintState State { get; private set; } = WorkOrderPrintState.Idle;
        public string TaskId { get; private set; }

        public bool Begin(string taskId)
        {
            if (string.IsNullOrWhiteSpace(taskId) || State != WorkOrderPrintState.Idle) return false;
            TaskId = taskId;
            State = WorkOrderPrintState.Printing;
            return true;
        }

        public bool CompletePrint()
        {
            if (State != WorkOrderPrintState.Printing) return false;
            State = WorkOrderPrintState.ReadyToTear;
            return true;
        }

        public bool Tear()
        {
            if (State != WorkOrderPrintState.ReadyToTear) return false;
            State = WorkOrderPrintState.Torn;
            return true;
        }

        public void Reset()
        {
            TaskId = null;
            State = WorkOrderPrintState.Idle;
        }
    }
}
