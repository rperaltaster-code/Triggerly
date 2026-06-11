using Triggerly.Domain.Entities;

namespace Triggerly.Domain.Interfaces;

public interface ITenantSettingsRepository
{
    Task<TenantSettings?> GetByTenantAsync(string tenantId, CancellationToken ct = default);
    Task AddAsync(TenantSettings settings, CancellationToken ct = default);
}
