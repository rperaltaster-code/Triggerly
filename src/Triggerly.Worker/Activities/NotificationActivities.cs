using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Temporalio.Activities;
using Triggerly.Application.Interfaces;

namespace Triggerly.Worker.Activities;

public class NotificationActivities
{
    private readonly IEmailService _emailService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;
    private readonly string _tenantSlackWebhookUrl;

    public NotificationActivities(
        IEmailService emailService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _emailService = emailService;
        _httpClientFactory = httpClientFactory;
        _baseUrl = configuration["App:BaseUrl"] ?? "http://localhost:5173";
        _tenantSlackWebhookUrl = configuration["Slack:WebhookUrl"] ?? string.Empty;
    }

    [Activity]
    public async Task SendNotificationAsync(string tenantId, Dictionary<string, object> config, Dictionary<string, object> context)
    {
        var channel = config.TryGetValue("channel", out var ch) ? ch?.ToString() : "email";
        var recipient = config.TryGetValue("recipient", out var r) ? r?.ToString() : null;
        var message = config.TryGetValue("message", out var m) ? m?.ToString() : "Workflow notification";

        ActivityExecutionContext.Current.Logger.LogInformation(
            "Sending notification via {Channel} to {Recipient}: {Message}",
            channel, recipient, message);

        if (recipient != null && recipient.Contains("{{input."))
        {
            ActivityExecutionContext.Current.Logger.LogWarning(
                "Notification step has unresolved tokens in recipient — workflow has no form data. " +
                "Define form fields in the Trigger Form tab and trigger with the form.");
            return;
        }

        if (channel == "slack")
        {
            var webhookUrl = config.TryGetValue("webhookUrl", out var wh) && !string.IsNullOrWhiteSpace(wh?.ToString())
                ? wh.ToString()!
                : _tenantSlackWebhookUrl;

            if (!string.IsNullOrEmpty(webhookUrl))
                await PostSlackMessageAsync(webhookUrl, message ?? "Workflow notification");
            else
                ActivityExecutionContext.Current.Logger.LogWarning("Slack channel selected but no webhook URL configured.");
        }
        else if (channel == "email" && !string.IsNullOrEmpty(recipient))
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
    public async Task SendApprovalReminderAsync(
        string approverEmail, string stepName, string executionId, string workflowName,
        int percentElapsed, int slaHours)
    {
        var approvalsUrl = $"{_baseUrl}/approvals";
        var remainingHours = (int)Math.Ceiling(slaHours * (100 - percentElapsed) / 100.0);

        await _emailService.SendAsync(
            approverEmail,
            $"Reminder: Approval Required — {workflowName} / {stepName}",
            $"""
            <p>A reminder that your approval is still required.</p>
            <ul>
              <li><strong>Workflow:</strong> {workflowName}</li>
              <li><strong>Step:</strong> {stepName}</li>
              <li><strong>SLA progress:</strong> {percentElapsed}% elapsed (~{remainingHours}h remaining)</li>
            </ul>
            <p><a href="{approvalsUrl}">Review in Triggerly</a></p>
            """,
            ActivityExecutionContext.Current.CancellationToken);

        if (!string.IsNullOrEmpty(_tenantSlackWebhookUrl))
            await PostSlackMessageAsync(_tenantSlackWebhookUrl,
                $":clock1: *Approval reminder* — *{workflowName}* / {stepName} — {percentElapsed}% of SLA elapsed, ~{remainingHours}h remaining\n<{approvalsUrl}|Review in Triggerly>");
    }

    [Activity]
    public async Task SendEscalationNotificationAsync(
        string escalationEmail, string? primaryEmail, string stepName, string executionId, string workflowName, int slaHours)
    {
        var approvalsUrl = $"{_baseUrl}/approvals";

        await _emailService.SendAsync(
            escalationEmail,
            $"Escalation: Approval overdue — {workflowName} / {stepName}",
            $"""
            <p>An approval step has been escalated to you because the primary approver has not responded.</p>
            <ul>
              <li><strong>Workflow:</strong> {workflowName}</li>
              <li><strong>Step:</strong> {stepName}</li>
              <li><strong>Primary approver:</strong> {primaryEmail ?? "N/A"}</li>
              <li><strong>SLA:</strong> {slaHours}h</li>
            </ul>
            <p><a href="{approvalsUrl}">Review in Triggerly</a></p>
            """,
            ActivityExecutionContext.Current.CancellationToken);

        if (!string.IsNullOrEmpty(_tenantSlackWebhookUrl))
            await PostSlackMessageAsync(_tenantSlackWebhookUrl,
                $":rotating_light: *Escalation* — *{workflowName}* / {stepName} escalated to {escalationEmail}. Primary approver ({primaryEmail ?? "N/A"}) has not responded.\n<{approvalsUrl}|Review in Triggerly>");
    }

    [Activity]
    public async Task SendApprovalRequestNotificationAsync(
        string approverEmail, string stepName, string executionId, string workflowName)
    {
        var approvalsUrl = $"{_baseUrl}/approvals";

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
            <p><a href="{approvalsUrl}">Review and approve or reject this step in Triggerly</a></p>
            """,
            ActivityExecutionContext.Current.CancellationToken);

        if (!string.IsNullOrEmpty(_tenantSlackWebhookUrl))
        {
            await PostSlackMessageAsync(
                _tenantSlackWebhookUrl,
                $":bell: *Approval required* — *{workflowName}* / {stepName}\n<{approvalsUrl}|Review in Triggerly>");
        }
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

        if (!string.IsNullOrEmpty(_tenantSlackWebhookUrl))
        {
            await PostSlackMessageAsync(
                _tenantSlackWebhookUrl,
                $":warning: *SLA breached* — approval step *{stepName}* exceeded {slaHours}h SLA and timed out.");
        }
    }

    private async Task PostSlackMessageAsync(string webhookUrl, string text)
    {
        var client = _httpClientFactory.CreateClient("webhook");
        var response = await client.PostAsJsonAsync(webhookUrl, new { text },
            ActivityExecutionContext.Current.CancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            ActivityExecutionContext.Current.Logger.LogWarning(
                "Slack webhook returned {Status}: {Body}", (int)response.StatusCode, body);
        }
    }
}
