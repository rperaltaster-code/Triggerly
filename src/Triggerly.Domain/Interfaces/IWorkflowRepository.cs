using Triggerly.Domain.Entities;
using Triggerly.Shared.Models;

namespace Triggerly.Domain.Interfaces;

public interface IWorkflowRepository
{
    Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkflowDefinition?> GetByIdWithStepsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<WorkflowDefinition> Items, int TotalCount)> GetPagedAsync(
        string tenantId, int page, int pageSize, WorkflowStatus? status = null, string? search = null,
        CancellationToken cancellationToken = default);
    Task AddAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);
    Task UpdateAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountExecutionsAsync(Guid workflowId, CancellationToken cancellationToken = default);
}
