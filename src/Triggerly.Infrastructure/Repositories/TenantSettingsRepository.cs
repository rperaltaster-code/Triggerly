using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;

namespace Triggerly.Infrastructure.Repositories;

public class TenantSettingsRepository : ITenantSettingsRepository
{
    private readonly AppDbContext _context;
    public TenantSettingsRepository(AppDbContext context) => _context = context;

    public Task<TenantSettings?> GetByTenantAsync(string tenantId, CancellationToken ct = default) =>
        _context.TenantSettings.FirstOrDefaultAsync(ts => ts.TenantId == tenantId, ct);

    public async Task AddAsync(TenantSettings settings, CancellationToken ct = default) =>
        await _context.TenantSettings.AddAsync(settings, ct);
}
