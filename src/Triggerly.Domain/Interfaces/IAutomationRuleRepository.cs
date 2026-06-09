using Triggerly.Domain.Entities;

namespace Triggerly.Domain.Interfaces;

public interface IAutomationRuleRepository
{
    Task<AutomationRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<AutomationRule> Items, int TotalCount)> GetPagedAsync(
        string tenantId, int page, int pageSize, bool? isEnabled = null,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AutomationRule>> GetByWorkflowIdAsync(Guid workflowId, CancellationToken cancellationToken = default);
    Task AddAsync(AutomationRule rule, CancellationToken cancellationToken = default);
    Task UpdateAsync(AutomationRule rule, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AutomationRule>> GetEnabledScheduleRulesAsync(CancellationToken cancellationToken = default);
    Task RecordTriggerAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AutomationRule?> GetByWebhookTokenAsync(string token, CancellationToken cancellationToken = default);
}
