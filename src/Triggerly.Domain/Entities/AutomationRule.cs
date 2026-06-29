using Triggerly.Shared.Models;

namespace Triggerly.Domain.Entities;

public class AutomationRule
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TriggerType TriggerType { get; private set; }
    public string TriggerConfig { get; private set; } = string.Empty;
    public Guid WorkflowId { get; private set; }
    public bool IsEnabled { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public int ExecutionCount { get; private set; }
    public DateTime? LastTriggeredAt { get; private set; }
    public DateTime? NextRunAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string? WebhookToken { get; private set; }

    public WorkflowDefinition? Workflow { get; private set; }

    private AutomationRule() { }

    public static AutomationRule Create(
        string name,
        string description,
        TriggerType triggerType,
        string triggerConfig,
        Guid workflowId,
        string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        return new AutomationRule
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            TriggerType = triggerType,
            TriggerConfig = triggerConfig ?? "{}",
            WorkflowId = workflowId,
            IsEnabled = true,
            TenantId = tenantId,
            ExecutionCount = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            WebhookToken = triggerType == TriggerType.Webhook ? GenerateToken() : null
        };
    }

    public void RegenerateWebhookToken()
    {
        WebhookToken = GenerateToken();
        UpdatedAt = DateTime.UtcNow;
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

    public void Update(string name, string description, string triggerConfig)
    {
        Name = name;
        Description = description;
        TriggerConfig = triggerConfig;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Enable()
    {
        IsEnabled = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Disable()
    {
        IsEnabled = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordTrigger()
    {
        ExecutionCount++;
        LastTriggeredAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetNextRunAt(DateTime? nextRunAt)
    {
        NextRunAt = nextRunAt;
        UpdatedAt = DateTime.UtcNow;
    }
}
