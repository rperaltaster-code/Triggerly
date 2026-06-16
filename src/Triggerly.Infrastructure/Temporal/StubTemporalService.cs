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
        string workflowName,
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
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var exists = await db.Executions.AnyAsync(e => e.Id == executionId);
            if (!exists) return;

            // Only auto-complete if the workflow has no approval step; otherwise
            // leave it in WaitingApproval so the user can approve/reject via the UI.
            var approvalStep = await db.WorkflowSteps
                .FirstOrDefaultAsync(s => s.WorkflowId == workflowDefinitionId && s.Type == StepType.Approval);

            if (approvalStep != null)
            {
                await db.Executions
                    .Where(e => e.Id == executionId)
                    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Status, ExecutionStatus.WaitingApproval));

                if (!string.IsNullOrEmpty(approvalStep.ApproverEmail))
                {
                    await emailService.SendAsync(
                        approvalStep.ApproverEmail,
                        $"Approval Required: {workflowName} — {approvalStep.Name}",
                        $"""
                        <p>Your approval is required for a workflow step.</p>
                        <ul>
                          <li><strong>Workflow:</strong> {workflowName}</li>
                          <li><strong>Step:</strong> {approvalStep.Name}</li>
                          <li><strong>Execution ID:</strong> <code>{executionId}</code></li>
                        </ul>
                        <p>Please review and approve or reject this step in <a href="http://localhost:5173/approvals">Triggerly</a>.</p>
                        """);
                }
            }
            else
                await db.Executions
                    .Where(e => e.Id == executionId)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(e => e.Status, ExecutionStatus.Completed)
                        .SetProperty(e => e.CompletedAt, DateTime.UtcNow));
        });

        return Task.FromResult(fakeRunId);
    }

    public Task SendApprovalSignalAsync(
        string temporalWorkflowId, bool approved, string actorId, string? reason = null,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SendActionCompleteSignalAsync(
        string temporalWorkflowId, Guid stepId, string actorId, string actorName,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task CancelWorkflowAsync(string temporalWorkflowId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<string> GetWorkflowStatusAsync(string temporalWorkflowId, CancellationToken cancellationToken = default)
        => Task.FromResult("Running");
}
