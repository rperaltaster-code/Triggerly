using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;

namespace Triggerly.Infrastructure.Repositories;

public class TenantSsoConfigRepository : ITenantSsoConfigRepository
{
    private readonly AppDbContext _context;
    public TenantSsoConfigRepository(AppDbContext context) => _context = context;

    public Task<TenantSsoConfig?> GetByTenantAsync(string tenantId, CancellationToken ct = default) =>
        _context.TenantSsoConfigs.FirstOrDefaultAsync(c => c.TenantId == tenantId, ct);

    public async Task AddAsync(TenantSsoConfig config, CancellationToken ct = default) =>
        await _context.TenantSsoConfigs.AddAsync(config, ct);

    public Task DeleteAsync(string tenantId, CancellationToken ct = default) =>
        _context.TenantSsoConfigs
            .Where(c => c.TenantId == tenantId)
            .ExecuteDeleteAsync(ct);
}
