using MediatR;
using Triggerly.Shared.DTOs;
using Triggerly.Shared.Models;

namespace Triggerly.Application.Commands.AutomationRules;

public record CreateAutomationRuleCommand(
    string Name,
    string Description,
    TriggerType TriggerType,
    string TriggerConfig,
    Guid WorkflowId,
    string TenantId
) : IRequest<AutomationRuleDto>;

public record UpdateAutomationRuleCommand(
    Guid Id,
    string Name,
    string Description,
    string TriggerConfig,
    string TenantId
) : IRequest<AutomationRuleDto>;

public record DeleteAutomationRuleCommand(Guid Id, string TenantId) : IRequest;

public record ToggleAutomationRuleCommand(Guid Id, bool Enable, string TenantId) : IRequest;
