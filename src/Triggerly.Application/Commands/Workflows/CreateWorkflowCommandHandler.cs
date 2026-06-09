using MediatR;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Workflows;

public class CreateWorkflowCommandHandler : IRequestHandler<CreateWorkflowCommand, WorkflowDto>
{
    private readonly IWorkflowRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _audit;

    public CreateWorkflowCommandHandler(IWorkflowRepository repository, IUnitOfWork unitOfWork, IAuditService audit)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _audit = audit;
    }

    public async Task<WorkflowDto> Handle(CreateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = WorkflowDefinition.Create(
            request.Name,
            request.Description,
            request.TenantId,
            request.CreatedBy);

        foreach (var stepRequest in request.Steps.OrderBy(s => s.Order))
        {
            var step = workflow.AddStep(
                stepRequest.Name,
                stepRequest.Type,
                stepRequest.Order,
                stepRequest.Config ?? []);

            if (stepRequest.ApproverEmail is not null)
                step.SetApprover(stepRequest.ApproverEmail);
        }

        await _repository.AddAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(request.TenantId, request.CreatedBy, request.CreatedByName ?? request.CreatedBy,
            "WorkflowCreated", "Workflow", workflow.Id.ToString(), workflow.Name,
            ct: cancellationToken);

        return MapToDto(workflow);
    }

    private static WorkflowDto MapToDto(WorkflowDefinition workflow) =>
        new(
            workflow.Id,
            workflow.Name,
            workflow.Description,
            workflow.Status,
            workflow.TenantId,
            workflow.Version,
            workflow.Steps.Select(s => new WorkflowStepDto(
                s.Id, s.Name, s.Type, s.Order, s.Config, s.NextStepId)).ToList(),
            workflow.CreatedAt,
            workflow.UpdatedAt);
}
