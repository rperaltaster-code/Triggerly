using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;

namespace Triggerly.Infrastructure.Repositories;

public class WorkflowVersionRepository : IWorkflowVersionRepository
{
    private readonly AppDbContext _db;
    public WorkflowVersionRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(WorkflowVersion version, CancellationToken cancellationToken = default) =>
        await _db.WorkflowVersions.AddAsync(version, cancellationToken);

    public Task<int> CountByWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default) =>
        _db.WorkflowVersions.CountAsync(v => v.WorkflowId == workflowId, cancellationToken);

    public Task<WorkflowVersion?> GetLatestByWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default) =>
        _db.WorkflowVersions
            .Where(v => v.WorkflowId == workflowId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<WorkflowVersion>> GetByWorkflowAsync(Guid workflowId, CancellationToken cancellationToken = default) =>
        await _db.WorkflowVersions
            .Where(v => v.WorkflowId == workflowId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
}
