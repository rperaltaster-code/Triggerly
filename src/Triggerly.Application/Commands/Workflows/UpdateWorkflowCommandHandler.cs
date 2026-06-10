using MediatR;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Workflows;

public class UpdateWorkflowCommandHandler : IRequestHandler<UpdateWorkflowCommand, WorkflowDto>
{
    private readonly IWorkflowRepository _repository;

    public UpdateWorkflowCommandHandler(IWorkflowRepository repository) => _repository = repository;

    public async Task<WorkflowDto> Handle(UpdateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _repository.GetByIdWithStepsAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.Id} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        await _repository.UpdateDetailsAsync(request.Id, request.Name, request.Description, cancellationToken);

        return new WorkflowDto(
            workflow.Id, request.Name, request.Description, workflow.Status,
            workflow.TenantId, workflow.Version,
            workflow.Steps.Select(s => new WorkflowStepDto(s.Id, s.Name, s.Type, s.Order, s.Config, s.NextStepId)).ToList(),
            workflow.CreatedAt, DateTime.UtcNow, workflow.FormSchema);
    }
}
