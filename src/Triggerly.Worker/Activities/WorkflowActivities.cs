using Temporalio.Activities;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.Models;
using Triggerly.Infrastructure.Persistence;

namespace Triggerly.Worker.Activities;

public record WorkflowStepInfo(
    Guid Id, string Name, string Type, int Order,
    Dictionary<string, object> Config, string? ApproverEmail);

public class WorkflowActivities
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IClientServiceRepository _clientServices;
    private readonly IUnitOfWork _unitOfWork;

    public WorkflowActivities(
        IWorkflowRepository workflowRepository,
        IWorkflowExecutionRepository executionRepository,
        IClientServiceRepository clientServices,
        IUnitOfWork unitOfWork)
    {
        _workflowRepository = workflowRepository;
        _executionRepository = executionRepository;
        _clientServices = clientServices;
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
    public async Task UpdateExecutionStatusAsync(Guid executionId, Guid stepId, int stepOrder, string stepName, string status)
    {
        await _executionRepository.UpdateCurrentStepAsync(executionId, stepOrder, stepName);

        var exists = await _executionRepository.StepExistsAsync(executionId, stepId);
        if (!exists)
        {
            var step = ExecutionStep.Create(executionId, stepId, stepName, stepOrder);
            step.Start();
            await _executionRepository.AddStepAsync(step);
            await _unitOfWork.SaveChangesAsync();
        }
    }

    [Activity]
    public async Task RequestApprovalAsync(Guid executionId, Guid stepId, string stepName, string? approverEmail)
    {
        await _executionRepository.SetStatusAsync(executionId, ExecutionStatus.WaitingApproval, null, null);

        var exists = await _executionRepository.StepExistsAsync(executionId, stepId);
        if (!exists)
        {
            var currentOrder = await _executionRepository.GetCurrentStepOrderAsync(executionId);
            var step = ExecutionStep.Create(executionId, stepId, stepName, currentOrder);
            step.Start();
            await _executionRepository.AddStepAsync(step);
            await _unitOfWork.SaveChangesAsync();
        }

        ActivityExecutionContext.Current.Logger.LogInformation(
            "Approval requested for execution {ExecutionId}, step {StepName}, approver: {Approver}",
            executionId, stepName, approverEmail ?? "any");
    }

    [Activity]
    public async Task CompleteStepAsync(Guid executionId, Guid stepId, bool success, string? errorMessage)
    {
        await _executionRepository.CompleteStepAsync(executionId, stepId, success, errorMessage);
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
        var status = success ? ExecutionStatus.Completed : ExecutionStatus.Failed;
        await _executionRepository.SetStatusAsync(
            executionId, status, errorMessage, DateTime.UtcNow);

        if (success)
        {
            var execution = await _executionRepository.GetByIdAsync(executionId);
            if (execution?.ClientServiceId.HasValue == true)
            {
                var svc = await _clientServices.GetByIdAsync(execution.ClientServiceId.Value);
                if (svc != null)
                {
                    svc.RecordFiling(DateTime.UtcNow);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync();
    }

    [Activity]
    public async Task MarkSlaBreachedAsync(Guid executionId)
    {
        await _executionRepository.SetSlaBreachedAsync(executionId, DateTime.UtcNow);
        await _unitOfWork.SaveChangesAsync();
    }
}
