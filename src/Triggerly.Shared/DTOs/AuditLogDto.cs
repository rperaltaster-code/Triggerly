namespace Triggerly.Shared.DTOs;

public record AuditLogDto(
    Guid Id,
    string TenantId,
    string UserId,
    string UserName,
    string Action,
    string EntityType,
    string EntityId,
    string EntityName,
    string? Details,
    DateTime Timestamp
);
