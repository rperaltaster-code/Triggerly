using MediatR;
using Triggerly.Application.Common;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Queries.AutomationRules;

public class GetAutomationRuleByIdQueryHandler : IRequestHandler<GetAutomationRuleByIdQuery, AutomationRuleDto?>
{
    private readonly IAutomationRuleRepository _repository;
    private readonly IWorkflowRepository _workflowRepository;

    public GetAutomationRuleByIdQueryHandler(IAutomationRuleRepository repository, IWorkflowRepository workflowRepository)
    {
        _repository = repository;
        _workflowRepository = workflowRepository;
    }

    public async Task<AutomationRuleDto?> Handle(GetAutomationRuleByIdQuery request, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (rule is null || rule.TenantId != request.TenantId) return null;

        var workflow = await _workflowRepository.GetByIdAsync(rule.WorkflowId, cancellationToken);
        return new AutomationRuleDto(
            rule.Id, rule.Name, rule.Description, rule.TriggerType,
            rule.TriggerConfig, rule.WorkflowId, workflow?.Name ?? string.Empty,
            rule.IsEnabled, rule.TenantId, rule.ExecutionCount,
            rule.LastTriggeredAt, rule.CreatedAt);
    }
}

public class ListAutomationRulesQueryHandler : IRequestHandler<ListAutomationRulesQuery, PagedResult<AutomationRuleDto>>
{
    private readonly IAutomationRuleRepository _repository;
    private readonly IWorkflowRepository _workflowRepository;

    public ListAutomationRulesQueryHandler(IAutomationRuleRepository repository, IWorkflowRepository workflowRepository)
    {
        _repository = repository;
        _workflowRepository = workflowRepository;
    }

    public async Task<PagedResult<AutomationRuleDto>> Handle(ListAutomationRulesQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.TenantId, request.Page, request.PageSize, request.IsEnabled, cancellationToken);

        var dtos = new List<AutomationRuleDto>();
        foreach (var rule in items)
        {
            var workflow = await _workflowRepository.GetByIdAsync(rule.WorkflowId, cancellationToken);
            dtos.Add(new AutomationRuleDto(
                rule.Id, rule.Name, rule.Description, rule.TriggerType,
                rule.TriggerConfig, rule.WorkflowId, workflow?.Name ?? string.Empty,
                rule.IsEnabled, rule.TenantId, rule.ExecutionCount,
                rule.LastTriggeredAt, rule.CreatedAt));
        }

        return new PagedResult<AutomationRuleDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
