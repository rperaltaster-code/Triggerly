using Triggerly.Application.Interfaces;

namespace Triggerly.Infrastructure.Temporal;

// Used in development when Temporal is not running locally.
// Fakes workflow orchestration so executions, approvals, and cancellations
// all work through the UI without a real Temporal server.
public class StubTemporalService : ITemporalService
{
    public Task<string> StartWorkflowAsync(
        Guid workflowDefinitionId,
        Guid executionId,
        string tenantId,
        Dictionary<string, object>? inputData,
        CancellationToken cancellationToken = default)
    {
        var fakeRunId = $"stub-run-{Guid.NewGuid():N}";
        return Task.FromResult(fakeRunId);
    }

    public Task SendApprovalSignalAsync(
        string temporalWorkflowId, bool approved, string actorId, string? reason = null,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task CancelWorkflowAsync(string temporalWorkflowId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<string> GetWorkflowStatusAsync(string temporalWorkflowId, CancellationToken cancellationToken = default)
        => Task.FromResult("Running");
}
