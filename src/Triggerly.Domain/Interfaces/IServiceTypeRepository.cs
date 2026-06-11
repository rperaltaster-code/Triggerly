using Triggerly.Domain.Entities;

namespace Triggerly.Domain.Interfaces;

public interface IServiceTypeRepository
{
    Task<IReadOnlyList<ServiceType>> GetByTenantAsync(string tenantId, CancellationToken ct = default);
    Task<ServiceType?> GetByIdAsync(string tenantId, Guid id, CancellationToken ct = default);
    Task AddAsync(ServiceType serviceType, CancellationToken ct = default);
    void Remove(ServiceType serviceType);
}
