using MediatR;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Workflows;

public class UpdateWorkflowCommandHandler : IRequestHandler<UpdateWorkflowCommand, WorkflowDto>
{
    private readonly IWorkflowRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateWorkflowCommandHandler(IWorkflowRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkflowDto> Handle(UpdateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _repository.GetByIdWithStepsAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.Id} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        workflow.Update(request.Name, request.Description);
        await _repository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkflowDto(
            workflow.Id, workflow.Name, workflow.Description, workflow.Status,
            workflow.TenantId, workflow.Version,
            workflow.Steps.Select(s => new WorkflowStepDto(s.Id, s.Name, s.Type, s.Order, s.Config, s.NextStepId)).ToList(),
            workflow.CreatedAt, workflow.UpdatedAt);
    }
}
