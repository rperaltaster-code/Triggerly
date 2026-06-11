using Triggerly.Domain.Entities;

namespace Triggerly.Domain.Interfaces;

public interface ITeamInviteRepository
{
    Task AddAsync(TeamInvite invite, CancellationToken cancellationToken = default);
    Task<TeamInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<TeamInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TeamInvite>> GetPendingByTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task<bool> HasPendingInviteAsync(string tenantId, string email, CancellationToken cancellationToken = default);
    void Remove(TeamInvite invite);
}
