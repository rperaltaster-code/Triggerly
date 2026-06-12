using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Temporalio.Activities;
using Triggerly.Application.Interfaces;

namespace Triggerly.Worker.Activities;

public class NotificationActivities
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _templateService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _baseUrl;
    private readonly string _tenantSlackWebhookUrl;

    public NotificationActivities(
        IEmailService emailService,
        IEmailTemplateService templateService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _emailService = emailService;
        _templateService = templateService;
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
            var tokens = new Dictionary<string, string>
            {
                ["message"] = message ?? string.Empty,
            };
            var (subject, body) = await _templateService.GetRenderedAsync(
                tenantId, "notification", tokens,
                ActivityExecutionContext.Current.CancellationToken);

            await _emailService.SendAsync(recipient, subject, body,
                ActivityExecutionContext.Current.CancellationToken);
        }
        else
        {
            await Task.Delay(50, ActivityExecutionContext.Current.CancellationToken);
        }
    }

    [Activity]
    public async Task SendApprovalReminderAsync(
        string tenantId, string approverEmail, string stepName, string executionId,
        string workflowName, int percentElapsed, int slaHours)
    {
        var approvalsUrl = $"{_baseUrl}/approvals";
        var remainingHours = (int)Math.Ceiling(slaHours * (100 - percentElapsed) / 100.0);

        var tokens = new Dictionary<string, string>
        {
            ["workflowName"] = workflowName,
            ["stepName"] = stepName,
            ["executionId"] = executionId,
            ["approvalsUrl"] = approvalsUrl,
            ["percentElapsed"] = percentElapsed.ToString(),
            ["remainingHours"] = remainingHours.ToString(),
            ["slaHours"] = slaHours.ToString(),
        };
        var (subject, body) = await _templateService.GetRenderedAsync(
            tenantId, "approval_reminder", tokens,
            ActivityExecutionContext.Current.CancellationToken);

        await _emailService.SendAsync(approverEmail, subject, body,
            ActivityExecutionContext.Current.CancellationToken);

        if (!string.IsNullOrEmpty(_tenantSlackWebhookUrl))
            await PostSlackMessageAsync(_tenantSlackWebhookUrl,
                $":clock1: *Approval reminder* — *{workflowName}* / {stepName} — {percentElapsed}% of SLA elapsed, ~{remainingHours}h remaining\n<{approvalsUrl}|Review in Triggerly>");
    }

    [Activity]
    public async Task SendEscalationNotificationAsync(
        string tenantId, string escalationEmail, string? primaryEmail,
        string stepName, string executionId, string workflowName, int slaHours)
    {
        var approvalsUrl = $"{_baseUrl}/approvals";

        var tokens = new Dictionary<string, string>
        {
            ["workflowName"] = workflowName,
            ["stepName"] = stepName,
            ["executionId"] = executionId,
            ["approvalsUrl"] = approvalsUrl,
            ["primaryEmail"] = primaryEmail ?? "N/A",
            ["slaHours"] = slaHours.ToString(),
        };
        var (subject, body) = await _templateService.GetRenderedAsync(
            tenantId, "escalation", tokens,
            ActivityExecutionContext.Current.CancellationToken);

        await _emailService.SendAsync(escalationEmail, subject, body,
            ActivityExecutionContext.Current.CancellationToken);

        if (!string.IsNullOrEmpty(_tenantSlackWebhookUrl))
            await PostSlackMessageAsync(_tenantSlackWebhookUrl,
                $":rotating_light: *Escalation* — *{workflowName}* / {stepName} escalated to {escalationEmail}. Primary approver ({primaryEmail ?? "N/A"}) has not responded.\n<{approvalsUrl}|Review in Triggerly>");
    }

    [Activity]
    public async Task SendApprovalRequestNotificationAsync(
        string tenantId, string approverEmail, string stepName, string executionId, string workflowName)
    {
        var approvalsUrl = $"{_baseUrl}/approvals";

        var tokens = new Dictionary<string, string>
        {
            ["workflowName"] = workflowName,
            ["stepName"] = stepName,
            ["executionId"] = executionId,
            ["approvalsUrl"] = approvalsUrl,
        };
        var (subject, body) = await _templateService.GetRenderedAsync(
            tenantId, "approval_request", tokens,
            ActivityExecutionContext.Current.CancellationToken);

        await _emailService.SendAsync(approverEmail, subject, body,
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
        string tenantId, string approverEmail, string stepName, string executionId,
        string workflowName, int slaHours)
    {
        var tokens = new Dictionary<string, string>
        {
            ["workflowName"] = workflowName,
            ["stepName"] = stepName,
            ["executionId"] = executionId,
            ["approvalsUrl"] = $"{_baseUrl}/approvals",
            ["slaHours"] = slaHours.ToString(),
        };
        var (subject, body) = await _templateService.GetRenderedAsync(
            tenantId, "sla_breach", tokens,
            ActivityExecutionContext.Current.CancellationToken);

        await _emailService.SendAsync(approverEmail, subject, body,
            ActivityExecutionContext.Current.CancellationToken);

        if (!string.IsNullOrEmpty(_tenantSlackWebhookUrl))
        {
            await PostSlackMessageAsync(
                _tenantSlackWebhookUrl,
                $":warning: *SLA breached* — approval step *{stepName}* exceeded {slaHours}h SLA and timed out.");
        }
    }

    [Activity]
    public async Task SendTaskAssignedNotificationAsync(
        string tenantId, string assigneeEmail, string assigneeName, string stepName,
        string executionId, string workflowName, string? clientName, int slaHours)
    {
        var tasksUrl = $"{_baseUrl}/my-tasks";

        var tokens = new Dictionary<string, string>
        {
            ["workflowName"] = workflowName,
            ["stepName"] = stepName,
            ["executionId"] = executionId,
            ["approvalsUrl"] = tasksUrl,
            ["slaHours"] = slaHours.ToString(),
        };
        var (subject, body) = await _templateService.GetRenderedAsync(
            tenantId, "approval_request", tokens,
            ActivityExecutionContext.Current.CancellationToken);

        await _emailService.SendAsync(assigneeEmail, subject, body,
            ActivityExecutionContext.Current.CancellationToken);

        if (!string.IsNullOrEmpty(_tenantSlackWebhookUrl))
        {
            var clientPart = string.IsNullOrEmpty(clientName) ? string.Empty : $" · Client: {clientName}";
            await PostSlackMessageAsync(
                _tenantSlackWebhookUrl,
                $":clipboard: *New task assigned to {assigneeName}* — *{workflowName}* / {stepName}{clientPart} · Due: {slaHours}h\n<{tasksUrl}|Open My Tasks>");
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
