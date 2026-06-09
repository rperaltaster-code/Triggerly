using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;

namespace Triggerly.Infrastructure.Repositories;

public class AutomationRuleRepository : IAutomationRuleRepository
{
    private readonly AppDbContext _context;

    public AutomationRuleRepository(AppDbContext context) => _context = context;

    public Task<AutomationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.AutomationRules.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<AutomationRule> Items, int TotalCount)> GetPagedAsync(
        string tenantId, int page, int pageSize, bool? isEnabled = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AutomationRules.Where(r => r.TenantId == tenantId);
        if (isEnabled.HasValue) query = query.Where(r => r.IsEnabled == isEnabled.Value);

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<IReadOnlyList<AutomationRule>> GetByWorkflowIdAsync(Guid workflowId, CancellationToken cancellationToken = default) =>
        await _context.AutomationRules.Where(r => r.WorkflowId == workflowId).ToListAsync(cancellationToken);

    public async Task AddAsync(AutomationRule rule, CancellationToken cancellationToken = default) =>
        await _context.AutomationRules.AddAsync(rule, cancellationToken);

    public Task UpdateAsync(AutomationRule rule, CancellationToken cancellationToken = default)
    {
        if (_context.Entry(rule).State == Microsoft.EntityFrameworkCore.EntityState.Detached)
            _context.AutomationRules.Update(rule);
        return Task.CompletedTask;
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await GetByIdAsync(id, cancellationToken);
        if (rule is not null) _context.AutomationRules.Remove(rule);
    }

    public Task<AutomationRule?> GetByWebhookTokenAsync(string token, CancellationToken cancellationToken = default) =>
        _context.AutomationRules.FirstOrDefaultAsync(r => r.WebhookToken == token, cancellationToken);

    public async Task<IReadOnlyList<AutomationRule>> GetEnabledScheduleRulesAsync(CancellationToken cancellationToken = default) =>
        await _context.AutomationRules
            .Where(r => r.IsEnabled && r.TriggerType == Triggerly.Shared.Models.TriggerType.Schedule)
            .ToListAsync(cancellationToken);

    public Task RecordTriggerAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.AutomationRules
            .Where(r => r.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.LastTriggeredAt, DateTime.UtcNow)
                .SetProperty(r => r.ExecutionCount, r => r.ExecutionCount + 1)
                .SetProperty(r => r.UpdatedAt, DateTime.UtcNow), cancellationToken);
}
