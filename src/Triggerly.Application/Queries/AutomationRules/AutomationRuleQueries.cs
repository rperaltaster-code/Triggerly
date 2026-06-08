using MediatR;
using Triggerly.Application.Common;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Queries.AutomationRules;

public record GetAutomationRuleByIdQuery(Guid Id, string TenantId) : IRequest<AutomationRuleDto?>;

public record ListAutomationRulesQuery(
    string TenantId,
    int Page = 1,
    int PageSize = 20,
    bool? IsEnabled = null
) : IRequest<PagedResult<AutomationRuleDto>>;
