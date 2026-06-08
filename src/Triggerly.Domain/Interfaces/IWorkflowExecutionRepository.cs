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
    Task<int> CountByStatusAsync(string tenantId, ExecutionStatus status, CancellationToken cancellationToken = default);
    Task<int> CountCompletedTodayAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<List<(string Date, int Completed, int Failed)>> GetRecentTrendAsync(string tenantId, int days, CancellationToken cancellationToken = default);
}
