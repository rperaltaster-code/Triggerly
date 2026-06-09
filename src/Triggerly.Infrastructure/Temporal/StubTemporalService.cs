using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triggerly.Application.Interfaces;
using Triggerly.Infrastructure.Persistence;
using Triggerly.Shared.Contracts;
using Triggerly.Shared.Models;

namespace Triggerly.Infrastructure.Temporal;

// Used in development when Temporal is not running locally.
// Fakes workflow orchestration — simple workflows auto-complete immediately.
// Executions waiting for approval remain in WaitingApproval until manually approved.
public class StubTemporalService : ITemporalService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public StubTemporalService(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public Task<string> StartWorkflowAsync(
        Guid workflowDefinitionId,
        Guid executionId,
        string tenantId,
        Dictionary<string, object>? inputData,
        List<WorkflowStepInput> steps,
        CancellationToken cancellationToken = default)
    {
        var fakeRunId = $"stub-run-{Guid.NewGuid():N}";

        // Fire-and-forget: complete the execution after the current request commits.
        _ = Task.Run(async () =>
        {
            await Task.Delay(500); // let the triggering SaveChanges finish first
            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var execution = await db.Executions.FirstOrDefaultAsync(e => e.Id == executionId);
            if (execution is null) return;

            // Only auto-complete if the workflow has no approval step; otherwise
            // leave it in WaitingApproval so the user can approve/reject via the UI.
            var hasApprovalStep = await db.WorkflowSteps
                .AnyAsync(s => s.WorkflowId == workflowDefinitionId && s.Type == StepType.Approval);

            if (hasApprovalStep)
                execution.RequestApproval();
            else
                execution.Complete(null);

            await db.SaveChangesAsync();
        });

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
