using Triggerly.Domain.Entities;

namespace Triggerly.Domain.Interfaces;

public interface IWorkflowVersionRepository
{
    Task AddAsync(WorkflowVersion version, CancellationToken cancellationToken = default);
    Task<int> CountByWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<WorkflowVersion?> GetLatestByWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowVersion>> GetByWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default);
}
