using Triggerly.Domain.Entities;
using Triggerly.Shared.Models;

namespace Triggerly.Domain.Interfaces;

public interface IWorkflowExecutionRepository
{
    Task<WorkflowExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkflowExecution?> GetByTemporalIdAsync(string temporalWorkflowId, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<WorkflowExecution> Items, int TotalCount)> GetPagedAsync(
        string tenantId, int page, int pageSize, Guid? workflowId = null, ExecutionStatus? status = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowExecution execution, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowExecution execution, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, string tenantId, CancellationToken cancellationToken = default);
    Task AddCommentAsync(ExecutionComment comment, CancellationToken cancellationToken = default);
    Task<bool> StepExistsAsync(Guid executionId, Guid stepId, CancellationToken cancellationToken = default);
    Task AddStepAsync(ExecutionStep step, CancellationToken cancellationToken = default);
    Task UpdateCurrentStepAsync(Guid executionId, int stepOrder, string stepName, CancellationToken cancellationToken = default);
    Task CompleteStepAsync(Guid executionId, Guid stepId, bool success, string? errorMessage, CancellationToken cancellationToken = default);
    Task CompleteCurrentStepAsync(Guid executionId, bool success, string? errorMessage, CancellationToken cancellationToken = default);
    Task SetStatusAsync(Guid executionId, ExecutionStatus status, string? errorMessage, DateTime? completedAt, CancellationToken cancellationToken = default);
    Task<int> GetCurrentStepOrderAsync(Guid executionId, CancellationToken cancellationToken = default);
    Task SetSlaBreachedAsync(Guid executionId, DateTime breachedAt, CancellationToken cancellationToken = default);
    Task<int> CountByStatusAsync(string tenantId, ExecutionStatus status, CancellationToken cancellationToken = default);
    Task<int> CountCompletedTodayAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<List<(string Date, int Completed, int Failed)>> GetRecentTrendAsync(string tenantId, int days, CancellationToken cancellationToken = default);
}
