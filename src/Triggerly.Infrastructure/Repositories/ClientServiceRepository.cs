using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;

namespace Triggerly.Infrastructure.Repositories;

public class ClientServiceRepository : IClientServiceRepository
{
    private readonly AppDbContext _context;
    public ClientServiceRepository(AppDbContext context) => _context = context;

    public async Task<IReadOnlyList<ClientService>> GetByClientAsync(Guid clientId, CancellationToken ct = default) =>
        await _context.ClientServices.Where(cs => cs.ClientId == clientId).ToListAsync(ct);

    public Task<ClientService?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.ClientServices.FindAsync([id], ct).AsTask();

    public async Task AddAsync(ClientService clientService, CancellationToken ct = default) =>
        await _context.ClientServices.AddAsync(clientService, ct);

    public void Remove(ClientService clientService) => _context.ClientServices.Remove(clientService);
}
