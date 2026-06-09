using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;
using Triggerly.Shared.Models;

namespace Triggerly.Infrastructure.Repositories;

public class WorkflowExecutionRepository : IWorkflowExecutionRepository
{
    private readonly AppDbContext _context;

    public WorkflowExecutionRepository(AppDbContext context) => _context = context;

    public Task<WorkflowExecution?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.Executions
            .Include(e => e.Steps)
            .Include(e => e.Comments)
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<WorkflowExecution?> GetByTemporalIdAsync(string temporalWorkflowId, CancellationToken cancellationToken = default) =>
        _context.Executions
            .Include(e => e.Steps)
            .Include(e => e.Comments)
            .FirstOrDefaultAsync(e => e.TemporalWorkflowId == temporalWorkflowId, cancellationToken);

    public async Task<(IReadOnlyList<WorkflowExecution> Items, int TotalCount)> GetPagedAsync(
        string tenantId, int page, int pageSize, Guid? workflowId = null, ExecutionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Executions.Where(e => e.TenantId == tenantId);
        if (workflowId.HasValue) query = query.Where(e => e.WorkflowId == workflowId.Value);
        if (status.HasValue) query = query.Where(e => e.Status == status.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(e => e.StartedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task AddAsync(WorkflowExecution execution, CancellationToken cancellationToken = default) =>
        await _context.Executions.AddAsync(execution, cancellationToken);

    public Task<bool> ExistsAsync(Guid id, string tenantId, CancellationToken cancellationToken = default) =>
        _context.Executions.AnyAsync(e => e.Id == id && e.TenantId == tenantId, cancellationToken);

    public async Task AddCommentAsync(ExecutionComment comment, CancellationToken cancellationToken = default) =>
        await _context.ExecutionComments.AddAsync(comment, cancellationToken);

    public Task<bool> StepExistsAsync(Guid executionId, Guid stepId, CancellationToken cancellationToken = default) =>
        _context.ExecutionSteps.AnyAsync(s => s.ExecutionId == executionId && s.StepId == stepId, cancellationToken);

    public async Task AddStepAsync(ExecutionStep step, CancellationToken cancellationToken = default) =>
        await _context.ExecutionSteps.AddAsync(step, cancellationToken);

    public Task UpdateCurrentStepAsync(Guid executionId, int stepOrder, string stepName, CancellationToken cancellationToken = default) =>
        _context.Executions
            .Where(e => e.Id == executionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.CurrentStepOrder, stepOrder)
                .SetProperty(e => e.CurrentStepName, stepName), cancellationToken);

    public Task CompleteStepAsync(Guid executionId, Guid stepId, bool success, string? errorMessage, CancellationToken cancellationToken = default) =>
        _context.ExecutionSteps
            .Where(s => s.ExecutionId == executionId && s.StepId == stepId)
            .ExecuteUpdateAsync(u => u
                .SetProperty(s => s.Status, success ? ExecutionStatus.Completed : ExecutionStatus.Failed)
                .SetProperty(s => s.ErrorMessage, success ? null : errorMessage ?? "Unknown error")
                .SetProperty(s => s.CompletedAt, DateTime.UtcNow), cancellationToken);

    public Task StartAsync(Guid executionId, string temporalRunId, CancellationToken cancellationToken = default) =>
        _context.Executions
            .Where(e => e.Id == executionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, ExecutionStatus.Running)
                .SetProperty(e => e.TemporalRunId, temporalRunId), cancellationToken);

    public Task CompleteCurrentStepAsync(Guid executionId, bool success, string? errorMessage, CancellationToken cancellationToken = default)
    {
        var status = success ? ExecutionStatus.Completed : ExecutionStatus.Failed;
        return _context.ExecutionSteps
            .Where(s => s.ExecutionId == executionId && s.Status == ExecutionStatus.Running)
            .ExecuteUpdateAsync(u => u
                .SetProperty(s => s.Status, status)
                .SetProperty(s => s.ErrorMessage, errorMessage)
                .SetProperty(s => s.CompletedAt, DateTime.UtcNow), cancellationToken);
    }

    public Task<int> GetCurrentStepOrderAsync(Guid executionId, CancellationToken cancellationToken = default) =>
        _context.Executions
            .Where(e => e.Id == executionId)
            .Select(e => e.CurrentStepOrder)
            .FirstOrDefaultAsync(cancellationToken);

    public Task SetStatusAsync(Guid executionId, ExecutionStatus status, string? errorMessage, DateTime? completedAt, CancellationToken cancellationToken = default) =>
        _context.Executions
            .Where(e => e.Id == executionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.Status, status)
                .SetProperty(e => e.ErrorMessage, errorMessage)
                .SetProperty(e => e.CompletedAt, completedAt), cancellationToken);

    public Task SetSlaBreachedAsync(Guid executionId, DateTime breachedAt, CancellationToken cancellationToken = default) =>
        _context.Executions
            .Where(e => e.Id == executionId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(e => e.SlaBreachedAt, breachedAt), cancellationToken);

    public Task UpdateAsync(WorkflowExecution execution, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(execution).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            _context.Executions.Update(execution);
        return Task.CompletedTask;
    }

    public Task<int> CountByStatusAsync(string tenantId, ExecutionStatus status, CancellationToken cancellationToken = default) =>
        _context.Executions.CountAsync(e => e.TenantId == tenantId && e.Status == status, cancellationToken);

    public Task<int> CountCompletedTodayAsync(string tenantId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        return _context.Executions.CountAsync(
            e => e.TenantId == tenantId &&
                 e.Status == ExecutionStatus.Completed &&
                 e.CompletedAt.HasValue &&
                 e.CompletedAt.Value >= today,
            cancellationToken);
    }

    public async Task<List<(string Date, int Completed, int Failed)>> GetRecentTrendAsync(
        string tenantId, int days, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.Date.AddDays(-days);
        var executions = await _context.Executions
            .Where(e => e.TenantId == tenantId && e.StartedAt >= since)
            .ToListAsync(cancellationToken);

        return Enumerable.Range(0, days)
            .Select(i =>
            {
                var date = DateTime.UtcNow.Date.AddDays(-days + i + 1);
                var dateStr = date.ToString("MM/dd");
                var completed = executions.Count(e => e.CompletedAt?.Date == date && e.Status == ExecutionStatus.Completed);
                var failed = executions.Count(e => e.CompletedAt?.Date == date && e.Status == ExecutionStatus.Failed);
                return (dateStr, completed, failed);
            })
            .ToList();
    }
}
