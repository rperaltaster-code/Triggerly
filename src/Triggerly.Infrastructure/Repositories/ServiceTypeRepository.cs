using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;

namespace Triggerly.Infrastructure.Repositories;

public class ServiceTypeRepository : IServiceTypeRepository
{
    private readonly AppDbContext _context;
    public ServiceTypeRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<ServiceType>> GetByTenantAsync(string tenantId, CancellationToken ct = default) =>
        await _context.ServiceTypes.Where(st => st.TenantId == tenantId)
            .OrderBy(st => st.Name).ToListAsync(ct);

    public Task<ServiceType?> GetByIdAsync(string tenantId, Guid id, CancellationToken ct = default) =>
        _context.ServiceTypes.FirstOrDefaultAsync(st => st.TenantId == tenantId && st.Id == id, ct);

    public async Task AddAsync(ServiceType serviceType, CancellationToken ct = default) =>
        await _context.ServiceTypes.AddAsync(serviceType, ct);

    public void Remove(ServiceType serviceType) => _context.ServiceTypes.Remove(serviceType);
}
