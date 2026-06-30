using Triggerly.Domain.Entities;

namespace Triggerly.Domain.Interfaces;

public interface ITenantSsoConfigRepository
{
    Task<TenantSsoConfig?> GetByTenantAsync(string tenantId, CancellationToken ct = default);
    Task AddAsync(TenantSsoConfig config, CancellationToken ct = default);
    Task DeleteAsync(string tenantId, CancellationToken ct = default);
}
