using Triggerly.Shared.Models;

namespace Triggerly.Shared.DTOs;

public record AutomationRuleDto(
    Guid Id,
    string Name,
    string Description,
    TriggerType TriggerType,
    string TriggerConfig,
    Guid WorkflowId,
    string WorkflowName,
    bool IsEnabled,
    string TenantId,
    int ExecutionCount,
    DateTime? LastTriggeredAt,
    DateTime CreatedAt,
    string? WebhookToken = null,
    DateTime? NextRunAt = null
);
