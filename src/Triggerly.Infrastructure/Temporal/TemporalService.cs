using Temporalio.Client;
using Triggerly.Application.Interfaces;
using Triggerly.Shared.Contracts;

namespace Triggerly.Infrastructure.Temporal;

public class TemporalService : ITemporalService
{
    private readonly ITemporalClient _client;

    public TemporalService(ITemporalClient client) => _client = client;

    public async Task<string> StartWorkflowAsync(
        Guid workflowDefinitionId,
        Guid executionId,
        string tenantId,
        Dictionary<string, object>? inputData,
        List<WorkflowStepInput> steps,
        CancellationToken cancellationToken = default)
    {
        var workflowId = $"triggerly-{executionId:N}";
        var input = new AutomationWorkflowInput(workflowDefinitionId, executionId, tenantId, inputData ?? [], steps);

        var handle = await _client.StartWorkflowAsync(
            (IAutomationWorkflow wf) => wf.RunAsync(input),
            new WorkflowOptions
            {
                Id = workflowId,
                TaskQueue = TemporalConstants.TaskQueue
            });

        return handle.FirstExecutionRunId ?? string.Empty;
    }

    public async Task SendApprovalSignalAsync(
        string temporalWorkflowId, bool approved, string actorId, string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var handle = _client.GetWorkflowHandle(temporalWorkflowId);
        await handle.SignalAsync(
            (IAutomationWorkflow wf) => wf.ApprovalSignalAsync(new ApprovalSignal(approved, actorId, reason)));
    }

    public async Task CancelWorkflowAsync(string temporalWorkflowId, CancellationToken cancellationToken = default)
    {
        var handle = _client.GetWorkflowHandle(temporalWorkflowId);
        await handle.CancelAsync();
    }

    public async Task<string> GetWorkflowStatusAsync(string temporalWorkflowId, CancellationToken cancellationToken = default)
    {
        var handle = _client.GetWorkflowHandle(temporalWorkflowId);
        var description = await handle.DescribeAsync();
        return description.Status.ToString();
    }
}
