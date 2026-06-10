using Triggerly.Shared.Contracts;

namespace Triggerly.Application.Interfaces;

public interface ITemporalService
{
    Task<string> StartWorkflowAsync(
        Guid workflowDefinitionId,
        Guid executionId,
        string tenantId,
        string workflowName,
        Dictionary<string, object>? inputData,
        List<WorkflowStepInput> steps,
        CancellationToken cancellationToken = default);

    Task SendApprovalSignalAsync(string temporalWorkflowId, bool approved, string actorId, string? reason = null, CancellationToken cancellationToken = default);

    Task CancelWorkflowAsync(string temporalWorkflowId, CancellationToken cancellationToken = default);

    Task<string> GetWorkflowStatusAsync(string temporalWorkflowId, CancellationToken cancellationToken = default);
}
