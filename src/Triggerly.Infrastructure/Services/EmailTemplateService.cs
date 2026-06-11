using Triggerly.Application.Interfaces;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Infrastructure.Services;

public class EmailTemplateService : IEmailTemplateService
{
    private readonly IEmailTemplateRepository _repository;

    // Default subjects and bodies keyed by template key
    private static readonly Dictionary<string, (string Subject, string Body)> Defaults = new()
    {
        ["approval_request"] = (
            "Approval Required: {{workflowName}} — {{stepName}}",
            """
            <p>Your approval is required for a workflow step.</p>
            <ul>
              <li><strong>Workflow:</strong> {{workflowName}}</li>
              <li><strong>Step:</strong> {{stepName}}</li>
              <li><strong>Execution ID:</strong> <code>{{executionId}}</code></li>
            </ul>
            <p><a href="{{approvalsUrl}}">Review and approve or reject this step in Triggerly</a></p>
            """),

        ["approval_reminder"] = (
            "Reminder: Approval Required — {{workflowName}} / {{stepName}}",
            """
            <p>A reminder that your approval is still required.</p>
            <ul>
              <li><strong>Workflow:</strong> {{workflowName}}</li>
              <li><strong>Step:</strong> {{stepName}}</li>
              <li><strong>SLA progress:</strong> {{percentElapsed}}% elapsed (~{{remainingHours}}h remaining)</li>
            </ul>
            <p><a href="{{approvalsUrl}}">Review in Triggerly</a></p>
            """),

        ["escalation"] = (
            "Escalation: Approval overdue — {{workflowName}} / {{stepName}}",
            """
            <p>An approval step has been escalated to you because the primary approver has not responded.</p>
            <ul>
              <li><strong>Workflow:</strong> {{workflowName}}</li>
              <li><strong>Step:</strong> {{stepName}}</li>
              <li><strong>Primary approver:</strong> {{primaryEmail}}</li>
              <li><strong>SLA:</strong> {{slaHours}}h</li>
            </ul>
            <p><a href="{{approvalsUrl}}">Review in Triggerly</a></p>
            """),

        ["sla_breach"] = (
            "SLA Breach: Approval overdue for '{{stepName}}'",
            """
            <p>An approval step has exceeded its SLA of <strong>{{slaHours}} hours</strong> and has timed out.</p>
            <ul>
              <li><strong>Workflow:</strong> {{workflowName}}</li>
              <li><strong>Step:</strong> {{stepName}}</li>
              <li><strong>SLA:</strong> {{slaHours}} hours</li>
            </ul>
            <p>The workflow has been marked as timed out. Please review in Triggerly.</p>
            """),

        ["notification"] = (
            "Workflow Notification",
            "<p>{{message}}</p>"),
    };

    public EmailTemplateService(IEmailTemplateRepository repository) => _repository = repository;

    public async Task<(string Subject, string Body)> GetRenderedAsync(
        string tenantId,
        string templateKey,
        Dictionary<string, string> tokens,
        CancellationToken cancellationToken = default)
    {
        var custom = await _repository.GetAsync(tenantId, templateKey, cancellationToken);

        string subject, body;
        if (custom != null)
        {
            subject = custom.Subject;
            body = custom.Body;
        }
        else if (Defaults.TryGetValue(templateKey, out var def))
        {
            subject = def.Subject;
            body = def.Body;
        }
        else
        {
            subject = "Triggerly Notification";
            body = "<p>A workflow event has occurred.</p>";
        }

        foreach (var (key, value) in tokens)
        {
            subject = subject.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
            body = body.Replace($"{{{{{key}}}}}", value, StringComparison.OrdinalIgnoreCase);
        }

        return (subject, body);
    }

    public static IReadOnlyDictionary<string, (string Subject, string Body)> GetDefaults() => Defaults;
}
