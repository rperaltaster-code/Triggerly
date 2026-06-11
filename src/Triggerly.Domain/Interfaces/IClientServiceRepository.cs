using Triggerly.Domain.Entities;

namespace Triggerly.Domain.Interfaces;

public interface IClientServiceRepository
{
    Task<IReadOnlyList<ClientService>> GetByClientAsync(Guid clientId, CancellationToken ct = default);
    Task<ClientService?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(ClientService clientService, CancellationToken ct = default);
    void Remove(ClientService clientService);
}
