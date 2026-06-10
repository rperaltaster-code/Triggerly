using MediatR;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;
using Triggerly.Shared.Models;

namespace Triggerly.Application.Commands.Workflows;

public record SaveWorkflowFormCommand(
    Guid WorkflowId,
    string TenantId,
    List<FormField> Fields
) : IRequest<WorkflowDto>;

public class SaveWorkflowFormCommandHandler : IRequestHandler<SaveWorkflowFormCommand, WorkflowDto>
{
    private readonly IWorkflowRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveWorkflowFormCommandHandler(IWorkflowRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkflowDto> Handle(SaveWorkflowFormCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _repository.GetByIdWithStepsAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.WorkflowId} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        workflow.SetFormSchema(request.Fields);
        await _repository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkflowDto(
            workflow.Id, workflow.Name, workflow.Description, workflow.Status,
            workflow.TenantId, workflow.Version,
            workflow.Steps.OrderBy(s => s.Order)
                .Select(s => new WorkflowStepDto(s.Id, s.Name, s.Type, s.Order, s.Config, s.NextStepId))
                .ToList(),
            workflow.CreatedAt, workflow.UpdatedAt, workflow.FormSchema);
    }
}
