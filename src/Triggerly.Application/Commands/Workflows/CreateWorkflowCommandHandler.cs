using MediatR;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Workflows;

public class CreateWorkflowCommandHandler : IRequestHandler<CreateWorkflowCommand, WorkflowDto>
{
    private readonly IWorkflowRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateWorkflowCommandHandler(IWorkflowRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
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
