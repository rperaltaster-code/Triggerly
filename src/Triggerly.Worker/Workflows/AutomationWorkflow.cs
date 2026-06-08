using Temporalio.Activities;
using Temporalio.Workflows;
using Triggerly.Shared.Contracts;
using Triggerly.Worker.Activities;

namespace Triggerly.Worker.Workflows;

[Workflow]
public class AutomationWorkflow : IAutomationWorkflow
{
    private ApprovalSignal? _approvalSignal;
    private string _currentStatus = "Initializing";

    [WorkflowRun]
    public async Task<AutomationWorkflowResult> RunAsync(AutomationWorkflowInput input)
    {
        var retryPolicy = new Temporalio.Common.RetryPolicy
        {
            MaximumAttempts = 3,
            InitialInterval = TimeSpan.FromSeconds(5),
            BackoffCoefficient = 2.0f,
            MaximumInterval = TimeSpan.FromMinutes(2)
        };

        var activityOptions = new ActivityOptions
        {
            StartToCloseTimeout = TimeSpan.FromMinutes(10),
            RetryPolicy = retryPolicy
        };

        try
        {
            _currentStatus = "Validating";
            var steps = await Workflow.ExecuteActivityAsync(
                (WorkflowActivities act) => act.LoadWorkflowStepsAsync(input.WorkflowDefinitionId),
                activityOptions);

            var context = new Dictionary<string, object>(input.InputData);

            foreach (var step in steps.OrderBy(s => s.Order))
            {
                _currentStatus = $"Executing: {step.Name}";

                await Workflow.ExecuteActivityAsync(
                    (WorkflowActivities act) => act.UpdateExecutionStatusAsync(
                        input.ExecutionId, step.Order, step.Name, "Running"),
                    activityOptions);

                switch (step.Type)
                {
                    case "Approval":
                        _currentStatus = $"Awaiting approval: {step.Name}";

                        await Workflow.ExecuteActivityAsync(
                            (WorkflowActivities act) => act.RequestApprovalAsync(
                                input.ExecutionId, step.Id, step.Name, step.ApproverEmail),
                            activityOptions);

                        var approved = await Workflow.WaitConditionAsync(
                            () => _approvalSignal != null,
                            TimeSpan.FromHours(72));

                        if (!approved || _approvalSignal?.Approved == false)
                        {
                            var reason = _approvalSignal?.Reason ?? "Timed out waiting for approval";
                            _currentStatus = "Rejected";
                            await Workflow.ExecuteActivityAsync(
                                (WorkflowActivities act) => act.CompleteStepAsync(
                                    input.ExecutionId, step.Id, false, reason),
                                activityOptions);
                            return new AutomationWorkflowResult(false, context, reason);
                        }

                        _approvalSignal = null;
                        break;

                    case "Notification":
                        await Workflow.ExecuteActivityAsync(
                            (NotificationActivities act) => act.SendNotificationAsync(
                                input.TenantId, step.Config, context),
                            activityOptions);
                        break;

                    case "Delay":
                        var delaySeconds = step.Config.TryGetValue("delaySeconds", out var d)
                            ? Convert.ToInt32(d) : 60;
                        await Workflow.DelayAsync(TimeSpan.FromSeconds(delaySeconds));
                        break;

                    case "DataTransform":
                        context = await Workflow.ExecuteActivityAsync(
                            (DataActivities act) => act.TransformDataAsync(context, step.Config),
                            activityOptions);
                        break;

                    case "Webhook":
                        var webhookResult = await Workflow.ExecuteActivityAsync(
                            (DataActivities act) => act.CallWebhookAsync(step.Config, context),
                            activityOptions);
                        context["webhookResult"] = webhookResult;
                        break;

                    default:
                        await Workflow.ExecuteActivityAsync(
                            (WorkflowActivities act) => act.ExecuteActionStepAsync(
                                input.ExecutionId, step.Id, step.Config, context),
                            activityOptions);
                        break;
                }

                await Workflow.ExecuteActivityAsync(
                    (WorkflowActivities act) => act.CompleteStepAsync(input.ExecutionId, step.Id, true, null),
                    activityOptions);
            }

            _currentStatus = "Completed";
            await Workflow.ExecuteActivityAsync(
                (WorkflowActivities act) => act.CompleteExecutionAsync(input.ExecutionId, true, context, null),
                activityOptions);

            return new AutomationWorkflowResult(true, context, null);
        }
        catch (Exception ex)
        {
            _currentStatus = "Failed";
            var emptyOutput = new Dictionary<string, object>();
            await Workflow.ExecuteActivityAsync(
                (WorkflowActivities act) => act.CompleteExecutionAsync(
                    input.ExecutionId, false, emptyOutput, ex.Message),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(2) });

            return new AutomationWorkflowResult(false, emptyOutput, ex.Message);
        }
    }

    [WorkflowSignal]
    public Task ApprovalSignalAsync(ApprovalSignal signal)
    {
        _approvalSignal = signal;
        return Task.CompletedTask;
    }

    [WorkflowQuery]
    public string GetCurrentStatus() => _currentStatus;
}
