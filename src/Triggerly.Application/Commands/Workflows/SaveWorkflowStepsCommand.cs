using System.Text.Json;
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
    private readonly IWorkflowVersionRepository _versionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SaveWorkflowStepsCommandHandler(
        IWorkflowRepository repository,
        IWorkflowVersionRepository versionRepository,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _versionRepository = versionRepository;
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

        // Wire up next-step links from explicit canvas edges (no sequential auto-wiring)
        var orderToId = newSteps.ToDictionary(s => s.Order, s => s.Id);
        foreach (var step in newSteps)
        {
            var config = new Dictionary<string, object>(step.Config);

            if (step.Type == StepType.Condition)
            {
                if (config.TryGetValue("trueBranchOrder", out var trueOrder))
                {
                    if (orderToId.TryGetValue(ToInt(trueOrder), out var trueId))
                        config["trueBranchNextStepId"] = trueId.ToString();
                    config.Remove("trueBranchOrder");
                }
                if (config.TryGetValue("falseBranchOrder", out var falseOrder))
                {
                    if (orderToId.TryGetValue(ToInt(falseOrder), out var falseId))
                        config["falseBranchNextStepId"] = falseId.ToString();
                    config.Remove("falseBranchOrder");
                }
            }
            else if (config.TryGetValue("nextOrder", out var nextOrder))
            {
                if (orderToId.TryGetValue(ToInt(nextOrder), out var nextId))
                    step.SetNextStep(nextId);
                config.Remove("nextOrder");
            }

            step.UpdateConfig(config);
        }

        await _repository.AddStepsAsync(newSteps, cancellationToken);

        // Snapshot the new step set as a new version
        var nextVersionNumber = await _versionRepository.CountByWorkflowAsync(request.WorkflowId, cancellationToken) + 1;
        var stepsJson = JsonSerializer.Serialize(newSteps.OrderBy(s => s.Order).Select(s => new {
            s.Id, s.Name, s.Type, s.Order, s.Config, s.ApproverEmail
        }));
        var version = WorkflowVersion.Create(request.WorkflowId, nextVersionNumber, stepsJson, request.TenantId);
        await _versionRepository.AddAsync(version, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        static int ToInt(object v) => v is JsonElement je ? je.GetInt32() : Convert.ToInt32(v);

        return new WorkflowDto(
            workflow.Id, workflow.Name, workflow.Description, workflow.Status,
            workflow.TenantId, workflow.Version,
            newSteps.OrderBy(s => s.Order).Select(s => new WorkflowStepDto(
                s.Id, s.Name, s.Type, s.Order, s.Config, s.NextStepId)).ToList(),
            workflow.CreatedAt, workflow.UpdatedAt, workflow.FormSchema);
    }
}
