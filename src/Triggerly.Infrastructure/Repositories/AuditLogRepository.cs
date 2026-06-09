using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;

namespace Triggerly.Infrastructure.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _context;
    public AuditLogRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(AuditLog log, CancellationToken ct = default) =>
        await _context.AuditLogs.AddAsync(log, ct);

    public async Task<(IReadOnlyList<AuditLog> Items, int TotalCount)> GetPagedAsync(
        string tenantId, int page, int pageSize,
        string? entityType = null, string? search = null,
        CancellationToken ct = default)
    {
        var query = _context.AuditLogs.Where(l => l.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(entityType))
            query = query.Where(l => l.EntityType == entityType);

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(l =>
                l.Action.Contains(search) ||
                l.EntityName.Contains(search) ||
                l.UserName.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return (items, total);
    }
}
