namespace Triggerly.Shared.Models;

public enum ExecutionStatus
{
    Pending = 0,
    Running = 1,
    WaitingApproval = 2,
    Approved = 3,
    Rejected = 4,
    Completed = 5,
    Failed = 6,
    Cancelled = 7,
    TimedOut = 8
}
