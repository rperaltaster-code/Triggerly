namespace Triggerly.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string tenantId, string userId, string userName,
        string action, string entityType, string entityId, string entityName,
        string? details = null,
        CancellationToken ct = default);
}
