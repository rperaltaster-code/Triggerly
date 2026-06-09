using Temporalio.Workflows;

namespace Triggerly.Shared.Contracts;

public static class TemporalConstants
{
    public const string TaskQueue = "triggerly-automation";
}

public record WorkflowStepInput(
    Guid Id,
    string Name,
    string Type,
    int Order,
    Dictionary<string, object> Config,
    string? ApproverEmail
);

public record AutomationWorkflowInput(
    Guid WorkflowDefinitionId,
    Guid ExecutionId,
    string TenantId,
    Dictionary<string, object> InputData,
    List<WorkflowStepInput> Steps
);

public record AutomationWorkflowResult(
    bool Success,
    Dictionary<string, object> OutputData,
    string? ErrorMessage
);

public record ApprovalSignal(
    bool Approved,
    string ActorId,
    string? Reason
);

[Workflow]
public interface IAutomationWorkflow
{
    [WorkflowRun]
    Task<AutomationWorkflowResult> RunAsync(AutomationWorkflowInput input);

    [WorkflowSignal]
    Task ApprovalSignalAsync(ApprovalSignal signal);

    [WorkflowQuery]
    string GetCurrentStatus();
}
