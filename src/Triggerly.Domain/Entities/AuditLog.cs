namespace Triggerly.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string UserId { get; private set; } = string.Empty;
    public string UserName { get; private set; } = string.Empty;
    public string Action { get; private set; } = string.Empty;
    public string EntityType { get; private set; } = string.Empty;
    public string EntityId { get; private set; } = string.Empty;
    public string EntityName { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public DateTime Timestamp { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string tenantId, string userId, string userName,
        string action, string entityType, string entityId, string entityName,
        string? details = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            UserId = userId,
            UserName = userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            EntityName = entityName,
            Details = details,
            Timestamp = DateTime.UtcNow,
        };
    }
}
