using Triggerly.Domain.Entities;

namespace Triggerly.Domain.Interfaces;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLog log, CancellationToken ct = default);
    Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        string tenantId, int page, int pageSize,
        string? entityType = null, string? search = null,
        CancellationToken ct = default);
}
