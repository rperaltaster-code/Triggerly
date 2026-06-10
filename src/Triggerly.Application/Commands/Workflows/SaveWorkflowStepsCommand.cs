using MediatR;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;
using Triggerly.Shared.Models;

namespace Triggerly.Application.Commands.Workflows;

public record StepDefinition(
    string Name,
    string Type,
    int Order,
    Dictionary<string, object> Config,
    string? ApproverEmail
);

public record SaveWorkflowStepsCommand(
    Guid WorkflowId,
    string TenantId,
    List<StepDefinition> Steps
) : IRequest<WorkflowDto>;

public class SaveWorkflowStepsCommandHandler : IRequestHandler<SaveWorkflowStepsCommand, WorkflowDto>
{
    private readonly IWorkflowRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveWorkflowStepsCommandHandler(IWorkflowRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<WorkflowDto> Handle(SaveWorkflowStepsCommand request, CancellationToken cancellationToken)
    {
        // Load workflow without steps to avoid navigation property tracking conflicts
        var workflow = await _repository.GetByIdAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.WorkflowId} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        // Delete existing steps at DbSet level and flush — separate from the insert
        await _repository.RemoveAllStepsAsync(request.WorkflowId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Build new step entities directly (no navigation property involvement)
        var newSteps = new List<WorkflowStep>();
        foreach (var def in request.Steps)
        {
            if (!Enum.TryParse<StepType>(def.Type, out var stepType))
                throw new ArgumentException($"Unknown step type: '{def.Type}'");

            var step = WorkflowStep.Create(request.WorkflowId, def.Name, stepType, def.Order, def.Config);
            if (!string.IsNullOrEmpty(def.ApproverEmail))
                step.SetApprover(def.ApproverEmail);
            newSteps.Add(step);
        }

        // Wire up sequential next-step links by order
        var ordered = newSteps.OrderBy(s => s.Order).ToList();
        for (int i = 0; i < ordered.Count - 1; i++)
            ordered[i].SetNextStep(ordered[i + 1].Id);

        await _repository.AddStepsAsync(newSteps, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new WorkflowDto(
            workflow.Id, workflow.Name, workflow.Description, workflow.Status,
            workflow.TenantId, workflow.Version,
            newSteps.OrderBy(s => s.Order).Select(s => new WorkflowStepDto(
                s.Id, s.Name, s.Type, s.Order, s.Config, s.NextStepId)).ToList(),
            workflow.CreatedAt, workflow.UpdatedAt, workflow.FormSchema);
    }
}
