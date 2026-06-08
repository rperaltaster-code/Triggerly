using Temporalio.Activities;
using Triggerly.Application.Interfaces;

namespace Triggerly.Worker.Activities;

public class NotificationActivities
{
    private readonly IEmailService _emailService;

    public NotificationActivities(IEmailService emailService) => _emailService = emailService;

    [Activity]
    public async Task SendNotificationAsync(string tenantId, Dictionary<string, object> config, Dictionary<string, object> context)
    {
        var channel = config.TryGetValue("channel", out var ch) ? ch?.ToString() : "email";
        var recipient = config.TryGetValue("recipient", out var r) ? r?.ToString() : null;
        var message = config.TryGetValue("message", out var m) ? m?.ToString() : "Workflow notification";

        ActivityExecutionContext.Current.Logger.LogInformation(
            "Sending notification via {Channel} to {Recipient}: {Message}",
            channel, recipient, message);

        if (channel == "email" && !string.IsNullOrEmpty(recipient))
        {
            await _emailService.SendAsync(
                recipient,
                "Workflow Notification",
                $"<p>{message}</p>",
                ActivityExecutionContext.Current.CancellationToken);
        }
        else
        {
            await Task.Delay(50, ActivityExecutionContext.Current.CancellationToken);
        }
    }

    [Activity]
    public async Task SendApprovalRequestNotificationAsync(
        string approverEmail, string stepName, string executionId, string workflowName)
    {
        await _emailService.SendAsync(
            approverEmail,
            $"Approval Required: {workflowName} — {stepName}",
            $"""
            <p>Your approval is required for a workflow step.</p>
            <ul>
              <li><strong>Workflow:</strong> {workflowName}</li>
              <li><strong>Step:</strong> {stepName}</li>
              <li><strong>Execution ID:</strong> <code>{executionId}</code></li>
            </ul>
            <p>Please review and approve or reject this step in Triggerly.</p>
            """,
            ActivityExecutionContext.Current.CancellationToken);
    }

    [Activity]
    public async Task SendSlaBreachNotificationAsync(
        string approverEmail, string stepName, string executionId, int slaHours)
    {
        await _emailService.SendAsync(
            approverEmail,
            $"SLA Breach: Approval overdue for '{stepName}'",
            $"""
            <p>An approval step has exceeded its SLA of <strong>{slaHours} hours</strong> and has timed out.</p>
            <ul>
              <li><strong>Step:</strong> {stepName}</li>
              <li><strong>Execution ID:</strong> <code>{executionId}</code></li>
              <li><strong>SLA:</strong> {slaHours} hours</li>
            </ul>
            <p>The workflow has been marked as timed out. Please review in Triggerly.</p>
            """,
            ActivityExecutionContext.Current.CancellationToken);
    }
}
