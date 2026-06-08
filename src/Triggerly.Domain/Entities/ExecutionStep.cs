using Triggerly.Shared.Models;

namespace Triggerly.Domain.Entities;

public class ExecutionStep
{
    public Guid Id { get; private set; }
    public Guid ExecutionId { get; private set; }
    public Guid StepId { get; private set; }
    public string StepName { get; private set; } = string.Empty;
    public ExecutionStatus Status { get; private set; }
    public int Order { get; private set; }
    public string? Output { get; private set; }
    public string? ErrorMessage { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }

    private ExecutionStep() { }

    public static ExecutionStep Create(Guid executionId, Guid stepId, string stepName, int order)
    {
        return new ExecutionStep
        {
            Id = Guid.NewGuid(),
            ExecutionId = executionId,
            StepId = stepId,
            StepName = stepName,
            Status = ExecutionStatus.Pending,
            Order = order
        };
    }

    public void Start()
    {
        Status = ExecutionStatus.Running;
        StartedAt = DateTime.UtcNow;
    }

    public void Complete(string? output)
    {
        Status = ExecutionStatus.Completed;
        Output = output;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = ExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }
}
