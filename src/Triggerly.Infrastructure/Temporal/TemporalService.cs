using Microsoft.Extensions.Logging;
using Temporalio.Client;
using Temporalio.Exceptions;
using Triggerly.Application.Interfaces;
using Triggerly.Shared.Contracts;

namespace Triggerly.Infrastructure.Temporal;

public class TemporalService : ITemporalService
{
    private readonly ITemporalClient _client;
    private readonly ILogger<TemporalService> _logger;

    public TemporalService(ITemporalClient client, ILogger<TemporalService> logger)
    {
        _client = client;
        _logger = logger;
    }

    public async Task<string> StartWorkflowAsync(
        Guid workflowDefinitionId,
        Guid executionId,
        string tenantId,
        string workflowName,
        Dictionary<string, object>? inputData,
        List<WorkflowStepInput> steps,
        CancellationToken cancellationToken = default)
    {
        var workflowId = $"triggerly-{executionId:N}";
        var input = new AutomationWorkflowInput(workflowDefinitionId, executionId, tenantId, workflowName, inputData ?? [], steps);

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
        try
        {
            var handle = _client.GetWorkflowHandle(temporalWorkflowId);
            await handle.SignalAsync(
                (IAutomationWorkflow wf) => wf.ApprovalSignalAsync(new ApprovalSignal(approved, actorId, reason)));
        }
        catch (RpcException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Workflow {WorkflowId} not found in Temporal — signal skipped (DB already updated).", temporalWorkflowId);
        }
    }

    public async Task SendActionCompleteSignalAsync(
        string temporalWorkflowId, Guid stepId, string actorId, string actorName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var handle = _client.GetWorkflowHandle(temporalWorkflowId);
            await handle.SignalAsync(
                (IAutomationWorkflow wf) => wf.ActionCompleteSignalAsync(new ActionCompleteSignal(stepId, actorId, actorName)));
        }
        catch (RpcException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Workflow {WorkflowId} not found in Temporal — action signal skipped.", temporalWorkflowId);
        }
    }

    public async Task CancelWorkflowAsync(string temporalWorkflowId, CancellationToken cancellationToken = default)
    {
        try
        {
            var handle = _client.GetWorkflowHandle(temporalWorkflowId);
            await handle.CancelAsync();
        }
        catch (RpcException ex) when (ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Workflow {WorkflowId} not found in Temporal — cancel skipped (DB already updated).", temporalWorkflowId);
        }
    }

    public async Task<string> GetWorkflowStatusAsync(string temporalWorkflowId, CancellationToken cancellationToken = default)
    {
        var handle = _client.GetWorkflowHandle(temporalWorkflowId);
        var description = await handle.DescribeAsync();
        return description.Status.ToString();
    }
}
