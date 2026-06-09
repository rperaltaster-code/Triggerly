using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;
using Triggerly.Shared.Models;

namespace Triggerly.Infrastructure.Repositories;

public class WorkflowRepository : IWorkflowRepository
{
    private readonly AppDbContext _context;

    public WorkflowRepository(AppDbContext context) => _context = context;

    public Task<WorkflowDefinition?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Workflows.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public Task<WorkflowDefinition?> GetByIdWithStepsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Workflows
            .Include(w => w.Steps)
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<WorkflowDefinition> Items, int TotalCount)> GetPagedAsync(
        string tenantId, int page, int pageSize, WorkflowStatus? status = null, string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Workflows
            .Include(w => w.Steps)
            .Where(w => w.TenantId == tenantId);

        if (status.HasValue)
            query = query.Where(w => w.Status == status.Value);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(w => w.Name.Contains(search) || w.Description.Contains(search));

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(w => w.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default) =>
        await _context.Workflows.AddAsync(workflow, cancellationToken);

    public Task UpdateAsync(WorkflowDefinition workflow, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(workflow).State == EntityState.Detached)
            _context.Workflows.Update(workflow);
        return Task.CompletedTask;
    }

    public async Task RemoveAllStepsAsync(Guid workflowId, CancellationToken cancellationToken = default)
    {
        var steps = await _context.WorkflowSteps
            .Where(s => s.WorkflowId == workflowId)
            .ToListAsync(cancellationToken);
        _context.WorkflowSteps.RemoveRange(steps);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var workflow = await GetByIdAsync(id, cancellationToken);
        if (workflow is not null) _context.Workflows.Remove(workflow);
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Workflows.AnyAsync(w => w.Id == id, cancellationToken);

    public Task<int> CountExecutionsAsync(Guid workflowId, CancellationToken cancellationToken = default) =>
        _context.Executions.CountAsync(e => e.WorkflowId == workflowId, cancellationToken);
}
