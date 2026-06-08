using Temporalio.Activities;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Worker.Activities;

public record WorkflowStepInfo(
    Guid Id, string Name, string Type, int Order,
    Dictionary<string, object> Config, string? ApproverEmail);

public class WorkflowActivities
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public WorkflowActivities(
        IWorkflowRepository workflowRepository,
        IWorkflowExecutionRepository executionRepository,
        IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _executionRepository = executionRepository;
        _unitOfWork = unitOfWork;
    }

    [Activity]
    public async Task<List<WorkflowStepInfo>> LoadWorkflowStepsAsync(Guid workflowDefinitionId)
    {
        var workflow = await _workflowRepository.GetByIdWithStepsAsync(workflowDefinitionId)
            ?? throw new InvalidOperationException($"Workflow {workflowDefinitionId} not found.");

        return workflow.Steps
            .Select(s => new WorkflowStepInfo(
                s.Id, s.Name, s.Type.ToString(), s.Order, s.Config, s.ApproverEmail))
            .ToList();
    }

    [Activity]
    public async Task UpdateExecutionStatusAsync(Guid executionId, int stepOrder, string stepName, string status)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId)
            ?? throw new InvalidOperationException($"Execution {executionId} not found.");

        execution.UpdateCurrentStep(stepOrder, stepName);
        await _executionRepository.UpdateAsync(execution);
        await _unitOfWork.SaveChangesAsync();
    }

    [Activity]
    public async Task RequestApprovalAsync(Guid executionId, Guid stepId, string stepName, string? approverEmail)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId)
            ?? throw new InvalidOperationException($"Execution {executionId} not found.");

        execution.RequestApproval();
        execution.AddStep(stepId, stepName, execution.CurrentStepOrder);
        await _executionRepository.UpdateAsync(execution);
        await _unitOfWork.SaveChangesAsync();

        ActivityExecutionContext.Current.Logger.LogInformation(
            "Approval requested for execution {ExecutionId}, step {StepName}, approver: {Approver}",
            executionId, stepName, approverEmail ?? "any");
    }

    [Activity]
    public async Task CompleteStepAsync(Guid executionId, Guid stepId, bool success, string? errorMessage)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId);
        if (execution is null) return;

        var step = execution.Steps.FirstOrDefault(s => s.StepId == stepId);
        if (step is null) return;

        if (success) step.Complete(null);
        else step.Fail(errorMessage ?? "Unknown error");

        await _executionRepository.UpdateAsync(execution);
        await _unitOfWork.SaveChangesAsync();
    }

    [Activity]
    public async Task ExecuteActionStepAsync(Guid executionId, Guid stepId,
        Dictionary<string, object> config, Dictionary<string, object> context)
    {
        ActivityExecutionContext.Current.Logger.LogInformation(
            "Executing action step for execution {ExecutionId}", executionId);
        await Task.Delay(100);
    }

    [Activity]
    public async Task CompleteExecutionAsync(Guid executionId, bool success,
        Dictionary<string, object> outputData, string? errorMessage)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId);
        if (execution is null) return;

        if (success) execution.Complete(outputData);
        else execution.Fail(errorMessage ?? "Workflow failed");

        await _executionRepository.UpdateAsync(execution);
        await _unitOfWork.SaveChangesAsync();
    }

    [Activity]
    public async Task MarkSlaBreachedAsync(Guid executionId)
    {
        var execution = await _executionRepository.GetByIdAsync(executionId);
        if (execution is null) return;

        execution.MarkSlaBreached();
        await _executionRepository.UpdateAsync(execution);
        await _unitOfWork.SaveChangesAsync();
    }
}
