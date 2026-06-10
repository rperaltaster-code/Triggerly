using Triggerly.Domain.Entities;

namespace Triggerly.Domain.Interfaces;

public interface ITenantRoleRepository
{
    Task<TenantRole?> GetAsync(Guid userId, string tenantId, CancellationToken ct = default);
    Task<IReadOnlyList<TenantRole>> GetByTenantAsync(string tenantId, CancellationToken ct = default);
    Task AddAsync(TenantRole role, CancellationToken ct = default);
    Task SeedAdminForExistingUsersAsync(CancellationToken ct = default);
}
