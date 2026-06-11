using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;

namespace Triggerly.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly AppDbContext _context;
    public ClientRepository(AppDbContext context) => _context = context;

    public async Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(
        string tenantId, int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = _context.Clients.Where(c => c.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search) || c.Email.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public Task<Client?> GetByIdAsync(string tenantId, Guid id, CancellationToken ct = default) =>
        _context.Clients.Include(c => c.Services)
            .FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id, ct);

    public async Task AddAsync(Client client, CancellationToken ct = default) =>
        await _context.Clients.AddAsync(client, ct);

    public void Remove(Client client) => _context.Clients.Remove(client);
}
