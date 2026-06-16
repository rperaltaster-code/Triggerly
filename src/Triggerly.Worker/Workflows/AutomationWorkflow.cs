using System.Globalization;
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
    private ActionCompleteSignal? _actionCompleteSignal;
    private Guid? _pendingActionStepId;
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
                        input.ExecutionId, currentStep.Id, currentStep.Order, currentStep.Name, currentStep.Type, "Running"),
                    activityOptions);

                Guid? nextStepId = currentStep.NextStepId;

                switch (currentStep.Type)
                {
                    case "Approval":
                        _currentStatus = $"Awaiting approval: {currentStep.Name}";

                        var slaHours = GetInt(resolvedConfig, "slaHours", 72);
                        var reminderPercents = GetIntList(resolvedConfig, "reminderAtPercent");
                        var escalationEmail = GetString(resolvedConfig, "escalationEmail");

                        // NOTE: approverEmail param was removed in this version — any execution
                        // mid-Approval-step at deploy time will get a Temporal non-determinism error.
                        // Drain or reset in-flight Approval workflows before deploying.
                        await Workflow.ExecuteActivityAsync(
                            (WorkflowActivities act) => act.RequestApprovalAsync(
                                input.ExecutionId, currentStep.Id, currentStep.Name),
                            activityOptions);

                        // Resolve assignee: prefer dynamic assignment config over static approverEmail
                        string? effectiveApproverEmail = currentStep.ApproverEmail;
                        var approvalAssignMode = GetString(resolvedConfig, "assignmentMode");
                        if (!string.IsNullOrEmpty(approvalAssignMode))
                        {
                            var approvalAssigned = await Workflow.ExecuteActivityAsync(
                                (WorkflowActivities act) => act.ResolveAndAssignStepAsync(
                                    input.ExecutionId, currentStep.Id, resolvedConfig, input.TenantId),
                                activityOptions);
                            if (approvalAssigned != null)
                            {
                                effectiveApproverEmail = approvalAssigned.Email;
                                await Workflow.ExecuteActivityAsync(
                                    (NotificationActivities act) => act.SendTaskAssignedNotificationAsync(
                                        input.TenantId, approvalAssigned.Email, approvalAssigned.UserName,
                                        currentStep.Name, input.ExecutionId.ToString(), input.WorkflowName,
                                        null, slaHours),
                                    activityOptions);
                            }
                        }
                        else if (!string.IsNullOrEmpty(effectiveApproverEmail))
                        {
                            await Workflow.ExecuteActivityAsync(
                                (NotificationActivities act) => act.SendApprovalRequestNotificationAsync(
                                    input.TenantId, effectiveApproverEmail, currentStep.Name,
                                    input.ExecutionId.ToString(), input.WorkflowName),
                                activityOptions);
                        }

                        // Incremental waiting with reminders at configured SLA percentages
                        var breakpoints = reminderPercents.OrderBy(p => p).Append(100).ToList();
                        double prevPct = 0;
                        bool signalReceived = false;

                        foreach (var pct in breakpoints)
                        {
                            var incrementHours = slaHours * (pct - prevPct) / 100.0;
                            signalReceived = await Workflow.WaitConditionAsync(
                                () => _approvalSignal != null,
                                TimeSpan.FromHours(incrementHours));

                            if (signalReceived) break;

                            if (pct < 100)
                            {
                                if (!string.IsNullOrEmpty(effectiveApproverEmail))
                                    await Workflow.ExecuteActivityAsync(
                                        (NotificationActivities act) => act.SendApprovalReminderAsync(
                                            input.TenantId, effectiveApproverEmail, currentStep.Name,
                                            input.ExecutionId.ToString(), input.WorkflowName, pct, slaHours),
                                        activityOptions);

                                // Escalate at the last reminder threshold
                                if (!string.IsNullOrEmpty(escalationEmail) && pct == breakpoints[^2])
                                    await Workflow.ExecuteActivityAsync(
                                        (NotificationActivities act) => act.SendEscalationNotificationAsync(
                                            input.TenantId, escalationEmail, effectiveApproverEmail, currentStep.Name,
                                            input.ExecutionId.ToString(), input.WorkflowName, slaHours),
                                        activityOptions);
                            }

                            prevPct = pct;
                        }

                        if (!signalReceived)
                        {
                            var timeoutReason = $"SLA timeout: no response within {slaHours} hours";
                            _currentStatus = "TimedOut";

                            await Workflow.ExecuteActivityAsync(
                                (WorkflowActivities act) => act.MarkSlaBreachedAsync(input.ExecutionId),
                                activityOptions);

                            if (!string.IsNullOrEmpty(effectiveApproverEmail))
                            {
                                await Workflow.ExecuteActivityAsync(
                                    (NotificationActivities act) => act.SendSlaBreachNotificationAsync(
                                        input.TenantId, effectiveApproverEmail, currentStep.Name,
                                        input.ExecutionId.ToString(), input.WorkflowName, slaHours),
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
                        var actionAssignMode = GetString(resolvedConfig, "assignmentMode");
                        if (!string.IsNullOrEmpty(actionAssignMode))
                        {
                            _currentStatus = $"Awaiting action: {currentStep.Name}";
                            _actionCompleteSignal = null;
                            _pendingActionStepId = currentStep.Id;

                            var actionSlaHours = GetInt(resolvedConfig, "slaHours", 72);
                            var actionReminderPercents = GetIntList(resolvedConfig, "reminderAtPercent");

                            var actionAssigned = await Workflow.ExecuteActivityAsync(
                                (WorkflowActivities act) => act.ResolveAndAssignStepAsync(
                                    input.ExecutionId, currentStep.Id, resolvedConfig, input.TenantId),
                                activityOptions);

                            if (actionAssigned != null)
                            {
                                var clientName = input.InputData.TryGetValue("client.name", out var cn)
                                    ? cn?.ToString() : null;
                                await Workflow.ExecuteActivityAsync(
                                    (NotificationActivities act) => act.SendTaskAssignedNotificationAsync(
                                        input.TenantId, actionAssigned.Email, actionAssigned.UserName,
                                        currentStep.Name, input.ExecutionId.ToString(), input.WorkflowName,
                                        clientName, actionSlaHours),
                                    activityOptions);
                            }

                            var actionBreakpoints = actionReminderPercents.OrderBy(p => p).Append(100).ToList();
                            double actionPrevPct = 0;
                            bool actionSignalReceived = false;

                            foreach (var pct in actionBreakpoints)
                            {
                                var increment = actionSlaHours * (pct - actionPrevPct) / 100.0;
                                actionSignalReceived = await Workflow.WaitConditionAsync(
                                    () => _actionCompleteSignal != null,
                                    TimeSpan.FromHours(increment));

                                if (actionSignalReceived) break;

                                if (pct < 100 && actionAssigned != null)
                                    await Workflow.ExecuteActivityAsync(
                                        (NotificationActivities act) => act.SendApprovalReminderAsync(
                                            input.TenantId, actionAssigned.Email, currentStep.Name,
                                            input.ExecutionId.ToString(), input.WorkflowName, pct, actionSlaHours),
                                        activityOptions);

                                actionPrevPct = pct;
                            }

                            if (!actionSignalReceived)
                            {
                                _currentStatus = "TimedOut";
                                var timeoutMsg = $"SLA timeout: no action within {actionSlaHours} hours";
                                await Workflow.ExecuteActivityAsync(
                                    (WorkflowActivities act) => act.MarkSlaBreachedAsync(input.ExecutionId),
                                    activityOptions);
                                await Workflow.ExecuteActivityAsync(
                                    (WorkflowActivities act) => act.CompleteStepAsync(
                                        input.ExecutionId, currentStep.Id, false, timeoutMsg),
                                    activityOptions);
                                return new AutomationWorkflowResult(false, context, timeoutMsg);
                            }

                            _actionCompleteSignal = null;
                            _pendingActionStepId = null;
                        }
                        else
                        {
                            await Workflow.ExecuteActivityAsync(
                                (WorkflowActivities act) => act.ExecuteActionStepAsync(
                                    input.ExecutionId, currentStep.Id, resolvedConfig, context),
                                activityOptions);
                        }
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

    [WorkflowSignal]
    public Task ActionCompleteSignalAsync(ActionCompleteSignal signal)
    {
        // Only accept if the signal matches the step we're currently waiting on,
        // preventing a late/duplicate signal from unblocking the wrong step.
        if (_pendingActionStepId.HasValue && signal.StepId == _pendingActionStepId.Value)
            _actionCompleteSignal = signal;
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
        dict.TryGetValue(key, out var v) ? JsonHelpers.GetString(v) : null;

    private static int GetInt(Dictionary<string, object> dict, string key, int defaultValue) =>
        dict.TryGetValue(key, out var v) ? JsonHelpers.GetInt(v, defaultValue) : defaultValue;

    private static string? GetStringValue(object? v) => JsonHelpers.GetString(v);

    private static List<int> GetIntList(Dictionary<string, object> config, string key)
    {
        if (!config.TryGetValue(key, out var raw)) return [];

        // Array from JSON deserialization
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Array)
            return je.EnumerateArray().Select(e => e.GetInt32()).ToList();

        // Comma-separated string (e.g. "50, 80")
        var str = raw is string s ? s : raw?.ToString();
        if (string.IsNullOrWhiteSpace(str)) return [];
        return str.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                  .Select(p => int.TryParse(p, out var n) ? n : (int?)null)
                  .Where(n => n.HasValue)
                  .Select(n => n!.Value)
                  .ToList();
    }
}
