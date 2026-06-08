using MediatR;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.AutomationRules;

public class CreateAutomationRuleCommandHandler : IRequestHandler<CreateAutomationRuleCommand, AutomationRuleDto>
{
    private readonly IAutomationRuleRepository _repository;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAutomationRuleCommandHandler(
        IAutomationRuleRepository repository,
        IWorkflowRepository workflowRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _workflowRepository = workflowRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AutomationRuleDto> Handle(CreateAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.WorkflowId} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        var rule = AutomationRule.Create(
            request.Name,
            request.Description,
            request.TriggerType,
            request.TriggerConfig,
            request.WorkflowId,
            request.TenantId);

        await _repository.AddAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new AutomationRuleDto(
            rule.Id, rule.Name, rule.Description, rule.TriggerType,
            rule.TriggerConfig, rule.WorkflowId, workflow.Name,
            rule.IsEnabled, rule.TenantId, rule.ExecutionCount,
            rule.LastTriggeredAt, rule.CreatedAt);
    }
}

public class UpdateAutomationRuleCommandHandler : IRequestHandler<UpdateAutomationRuleCommand, AutomationRuleDto>
{
    private readonly IAutomationRuleRepository _repository;
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAutomationRuleCommandHandler(
        IAutomationRuleRepository repository,
        IWorkflowRepository workflowRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _workflowRepository = workflowRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AutomationRuleDto> Handle(UpdateAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Automation rule {request.Id} not found.");

        if (rule.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        rule.Update(request.Name, request.Description, request.TriggerConfig);
        await _repository.UpdateAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var workflow = await _workflowRepository.GetByIdAsync(rule.WorkflowId, cancellationToken);
        return new AutomationRuleDto(
            rule.Id, rule.Name, rule.Description, rule.TriggerType,
            rule.TriggerConfig, rule.WorkflowId, workflow?.Name ?? string.Empty,
            rule.IsEnabled, rule.TenantId, rule.ExecutionCount,
            rule.LastTriggeredAt, rule.CreatedAt);
    }
}

public class DeleteAutomationRuleCommandHandler : IRequestHandler<DeleteAutomationRuleCommand>
{
    private readonly IAutomationRuleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAutomationRuleCommandHandler(IAutomationRuleRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Automation rule {request.Id} not found.");

        if (rule.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        await _repository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class ToggleAutomationRuleCommandHandler : IRequestHandler<ToggleAutomationRuleCommand>
{
    private readonly IAutomationRuleRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ToggleAutomationRuleCommandHandler(IAutomationRuleRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ToggleAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        var rule = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Automation rule {request.Id} not found.");

        if (rule.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        if (request.Enable) rule.Enable(); else rule.Disable();
        await _repository.UpdateAsync(rule, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
