using Triggerly.Shared.Models;

namespace Triggerly.Domain.Entities;

public class WorkflowExecution
{
    private readonly List<ExecutionStep> _steps = [];
    private readonly List<ExecutionComment> _comments = [];

    public Guid Id { get; private set; }
    public Guid WorkflowId { get; private set; }
    public string TemporalWorkflowId { get; private set; } = string.Empty;
    public string TemporalRunId { get; private set; } = string.Empty;
    public ExecutionStatus Status { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string? TriggeredBy { get; private set; }
    public Dictionary<string, object> InputData { get; private set; } = [];
    public Dictionary<string, object> OutputData { get; private set; } = [];
    public string? ErrorMessage { get; private set; }
    public int CurrentStepOrder { get; private set; }
    public string? CurrentStepName { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? SlaBreachedAt { get; private set; }

    public Guid? WorkflowVersionId { get; private set; }
    public int WorkflowVersionNumber { get; private set; }

    public Guid? ClientId { get; private set; }
    public Guid? ClientServiceId { get; private set; }

    public WorkflowDefinition? Workflow { get; private set; }
    public IReadOnlyList<ExecutionStep> Steps => _steps.AsReadOnly();
    public IReadOnlyList<ExecutionComment> Comments => _comments.AsReadOnly();

    private WorkflowExecution() { }

    public static WorkflowExecution Create(
        Guid workflowId,
        string temporalWorkflowId,
        string tenantId,
        string? triggeredBy,
        Dictionary<string, object>? inputData)
    {
        return new WorkflowExecution
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            TemporalWorkflowId = temporalWorkflowId,
            TemporalRunId = string.Empty,
            Status = ExecutionStatus.Pending,
            TenantId = tenantId,
            TriggeredBy = triggeredBy,
            InputData = inputData ?? [],
            OutputData = [],
            StartedAt = DateTime.UtcNow
        };
    }

    public void Start(string temporalRunId)
    {
        TemporalRunId = temporalRunId;
        Status = ExecutionStatus.Running;
    }

    public void UpdateCurrentStep(int stepOrder, string stepName)
    {
        CurrentStepOrder = stepOrder;
        CurrentStepName = stepName;
    }

    public void RequestApproval()
    {
        Status = ExecutionStatus.WaitingApproval;
    }

    public void Approve()
    {
        if (Status != ExecutionStatus.WaitingApproval)
            throw new InvalidOperationException("Execution is not awaiting approval.");
        Status = ExecutionStatus.Approved;
    }

    public void Reject(string reason)
    {
        if (Status != ExecutionStatus.WaitingApproval)
            throw new InvalidOperationException("Execution is not awaiting approval.");
        Status = ExecutionStatus.Rejected;
        ErrorMessage = reason;
        CompletedAt = DateTime.UtcNow;
    }

    public void Complete(Dictionary<string, object>? outputData)
    {
        Status = ExecutionStatus.Completed;
        OutputData = outputData ?? [];
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string errorMessage)
    {
        Status = ExecutionStatus.Failed;
        ErrorMessage = errorMessage;
        CompletedAt = DateTime.UtcNow;
    }

    public void Cancel()
    {
        Status = ExecutionStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    public void MarkSlaBreached()
    {
        SlaBreachedAt = DateTime.UtcNow;
    }

    public void SetVersion(Guid versionId, int versionNumber)
    {
        WorkflowVersionId = versionId;
        WorkflowVersionNumber = versionNumber;
    }

    public void SetClient(Guid clientId, Guid clientServiceId)
    {
        ClientId = clientId;
        ClientServiceId = clientServiceId;
    }

    public ExecutionStep AddStep(Guid stepId, string stepName, int order)
    {
        var executionStep = ExecutionStep.Create(Id, stepId, stepName, order);
        _steps.Add(executionStep);
        return executionStep;
    }

    public ExecutionComment AddComment(string authorId, string authorName, string content)
    {
        var comment = ExecutionComment.Create(Id, TenantId, authorId, authorName, content);
        _comments.Add(comment);
        return comment;
    }
}
