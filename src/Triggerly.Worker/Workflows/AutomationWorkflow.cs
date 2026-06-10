using System.Text.Json;
using Temporalio.Workflows;
using Triggerly.Shared.Contracts;
using Triggerly.Shared.Utils;
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
            _currentStatus = "Running";
            var context = new Dictionary<string, object>(input.InputData);

            var stepsById = input.Steps.ToDictionary(s => s.Id);
            var currentStep = input.Steps.OrderBy(s => s.Order).FirstOrDefault();
            var visited = new HashSet<Guid>();

            while (currentStep is not null)
            {
                if (!visited.Add(currentStep.Id))
                    break; // cycle guard

                _currentStatus = $"Executing: {currentStep.Name}";
                var resolvedConfig = TemplateEngine.Resolve(currentStep.Config, input.InputData);

                await Workflow.ExecuteActivityAsync(
                    (WorkflowActivities act) => act.UpdateExecutionStatusAsync(
                        input.ExecutionId, currentStep.Id, currentStep.Order, currentStep.Name, "Running"),
                    activityOptions);

                Guid? nextStepId = currentStep.NextStepId;

                switch (currentStep.Type)
                {
                    case "Approval":
                        _currentStatus = $"Awaiting approval: {currentStep.Name}";

                        var slaHours = resolvedConfig.TryGetValue("slaHours", out var h)
                            ? Convert.ToInt32(h) : 72;

                        await Workflow.ExecuteActivityAsync(
                            (WorkflowActivities act) => act.RequestApprovalAsync(
                                input.ExecutionId, currentStep.Id, currentStep.Name, currentStep.ApproverEmail),
                            activityOptions);

                        if (!string.IsNullOrEmpty(currentStep.ApproverEmail))
                        {
                            await Workflow.ExecuteActivityAsync(
                                (NotificationActivities act) => act.SendApprovalRequestNotificationAsync(
                                    currentStep.ApproverEmail, currentStep.Name,
                                    input.ExecutionId.ToString(), input.WorkflowName),
                                activityOptions);
                        }

                        var approved = await Workflow.WaitConditionAsync(
                            () => _approvalSignal != null,
                            TimeSpan.FromHours(slaHours));

                        if (!approved)
                        {
                            var timeoutReason = $"SLA timeout: no response within {slaHours} hours";
                            _currentStatus = "TimedOut";

                            await Workflow.ExecuteActivityAsync(
                                (WorkflowActivities act) => act.MarkSlaBreachedAsync(input.ExecutionId),
                                activityOptions);

                            if (!string.IsNullOrEmpty(currentStep.ApproverEmail))
                            {
                                await Workflow.ExecuteActivityAsync(
                                    (NotificationActivities act) => act.SendSlaBreachNotificationAsync(
                                        currentStep.ApproverEmail, currentStep.Name,
                                        input.ExecutionId.ToString(), slaHours),
                                    activityOptions);
                            }

                            await Workflow.ExecuteActivityAsync(
                                (WorkflowActivities act) => act.CompleteStepAsync(
                                    input.ExecutionId, currentStep.Id, false, timeoutReason),
                                activityOptions);
                            return new AutomationWorkflowResult(false, context, timeoutReason);
                        }

                        if (_approvalSignal?.Approved == false)
                        {
                            var rejectReason = _approvalSignal.Reason ?? "Rejected";
                            _currentStatus = "Rejected";
                            await Workflow.ExecuteActivityAsync(
                                (WorkflowActivities act) => act.CompleteStepAsync(
                                    input.ExecutionId, currentStep.Id, false, rejectReason),
                                activityOptions);
                            return new AutomationWorkflowResult(false, context, rejectReason);
                        }

                        _approvalSignal = null;
                        break;

                    case "Condition":
                        var branchTaken = EvaluateCondition(resolvedConfig, context);
                        context[$"__branch_{currentStep.Id}"] = branchTaken ? "true" : "false";
                        var branchKey = branchTaken ? "trueBranchNextStepId" : "falseBranchNextStepId";
                        nextStepId = resolvedConfig.TryGetValue(branchKey, out var bid) &&
                                     Guid.TryParse(bid?.ToString(), out var branchGuid)
                            ? branchGuid
                            : null;
                        break;

                    case "Notification":
                        await Workflow.ExecuteActivityAsync(
                            (NotificationActivities act) => act.SendNotificationAsync(
                                input.TenantId, resolvedConfig, context),
                            activityOptions);
                        break;

                    case "Delay":
                        var delaySeconds = resolvedConfig.TryGetValue("delaySeconds", out var d)
                            ? Convert.ToInt32(d) : 60;
                        await Workflow.DelayAsync(TimeSpan.FromSeconds(delaySeconds));
                        break;

                    case "DataTransform":
                        context = await Workflow.ExecuteActivityAsync(
                            (DataActivities act) => act.TransformDataAsync(context, resolvedConfig),
                            activityOptions);
                        break;

                    case "Webhook":
                        var webhookResult = await Workflow.ExecuteActivityAsync(
                            (DataActivities act) => act.CallWebhookAsync(resolvedConfig, context),
                            activityOptions);
                        context["webhookResult"] = webhookResult;
                        break;

                    default:
                        await Workflow.ExecuteActivityAsync(
                            (WorkflowActivities act) => act.ExecuteActionStepAsync(
                                input.ExecutionId, currentStep.Id, resolvedConfig, context),
                            activityOptions);
                        break;
                }

                await Workflow.ExecuteActivityAsync(
                    (WorkflowActivities act) => act.CompleteStepAsync(input.ExecutionId, currentStep.Id, true, null),
                    activityOptions);

                currentStep = nextStepId.HasValue && stepsById.TryGetValue(nextStepId.Value, out var next)
                    ? next : null;
            }

            _currentStatus = "Completed";
            await Workflow.ExecuteActivityAsync(
                (WorkflowActivities act) => act.CompleteExecutionAsync(input.ExecutionId, true, context, null),
                new ActivityOptions { StartToCloseTimeout = TimeSpan.FromMinutes(2) });

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

    private static bool EvaluateCondition(Dictionary<string, object> config, Dictionary<string, object> context)
    {
        var field = GetString(config, "field");
        var op = GetString(config, "operator") ?? "equals";
        var expected = GetString(config, "value");

        if (string.IsNullOrEmpty(field)) return false;

        // If field is a context key, look up its value; otherwise treat the resolved field value directly
        var actual = context.TryGetValue(field, out var av) ? GetStringValue(av) : field;

        return op switch
        {
            "equals" => actual == expected,
            "not-equals" => actual != expected,
            "contains" => actual?.Contains(expected ?? string.Empty, StringComparison.OrdinalIgnoreCase) == true,
            "greater-than" => double.TryParse(actual, out var a1) && double.TryParse(expected, out var e1) && a1 > e1,
            "less-than" => double.TryParse(actual, out var a2) && double.TryParse(expected, out var e2) && a2 < e2,
            _ => false
        };
    }

    private static string? GetString(Dictionary<string, object> dict, string key) =>
        dict.TryGetValue(key, out var v) ? GetStringValue(v) : null;

    private static string? GetStringValue(object? v) => v switch
    {
        string s => s,
        JsonElement je => je.ValueKind == JsonValueKind.String ? je.GetString() : je.ToString(),
        _ => v?.ToString()
    };
}
