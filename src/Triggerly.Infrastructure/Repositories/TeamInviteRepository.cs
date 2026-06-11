using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;

namespace Triggerly.Infrastructure.Repositories;

public class TeamInviteRepository : ITeamInviteRepository
{
    private readonly AppDbContext _context;

    public TeamInviteRepository(AppDbContext context) => _context = context;

    public async Task AddAsync(TeamInvite invite, CancellationToken cancellationToken = default) =>
        await _context.TeamInvites.AddAsync(invite, cancellationToken);

    public Task<TeamInvite?> GetByTokenAsync(string token, CancellationToken cancellationToken = default) =>
        _context.TeamInvites.FirstOrDefaultAsync(i => i.Token == token, cancellationToken);

    public Task<TeamInvite?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _context.TeamInvites.FindAsync([id], cancellationToken).AsTask();

    public async Task<IReadOnlyList<TeamInvite>> GetPendingByTenantAsync(string tenantId, CancellationToken cancellationToken = default) =>
        await _context.TeamInvites
            .Where(i => i.TenantId == tenantId && i.AcceptedAt == null)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> HasPendingInviteAsync(string tenantId, string email, CancellationToken cancellationToken = default) =>
        _context.TeamInvites.AnyAsync(
            i => i.TenantId == tenantId && i.Email == email.ToLowerInvariant() && i.AcceptedAt == null,
            cancellationToken);

    public void Remove(TeamInvite invite) => _context.TeamInvites.Remove(invite);
}
