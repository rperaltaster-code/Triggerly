using Triggerly.Shared.Models;

namespace Triggerly.Domain.Entities;

public class ServiceType
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public Guid? DefaultWorkflowId { get; private set; }
    public FilingPeriod? DefaultFilingPeriod { get; private set; }
    public string? Color { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private ServiceType() { }

    public static ServiceType Create(string tenantId, string name, string? description = null,
        Guid? defaultWorkflowId = null, FilingPeriod? defaultFilingPeriod = null, string? color = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new ServiceType
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Name = name.Trim(),
            Description = description?.Trim(),
            DefaultWorkflowId = defaultWorkflowId,
            DefaultFilingPeriod = defaultFilingPeriod,
            Color = color,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string name, string? description, Guid? defaultWorkflowId,
        FilingPeriod? defaultFilingPeriod, string? color)
    {
        Name = name.Trim();
        Description = description?.Trim();
        DefaultWorkflowId = defaultWorkflowId;
        DefaultFilingPeriod = defaultFilingPeriod;
        Color = color;
        UpdatedAt = DateTime.UtcNow;
    }
}
