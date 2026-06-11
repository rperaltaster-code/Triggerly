using Triggerly.Domain.Entities;

namespace Triggerly.Domain.Interfaces;

public interface IClientRepository
{
    Task<(IReadOnlyList<Client> Items, int TotalCount)> GetPagedAsync(
        string tenantId, int page, int pageSize, string? search, CancellationToken ct = default);
    Task<Client?> GetByIdAsync(string tenantId, Guid id, CancellationToken ct = default);
    Task AddAsync(Client client, CancellationToken ct = default);
    void Remove(Client client);
}
