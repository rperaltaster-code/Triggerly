using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;
using Triggerly.Shared.Models;

namespace Triggerly.Infrastructure.Repositories;

public class TenantRoleRepository : ITenantRoleRepository
{
    private readonly AppDbContext _context;

    public TenantRoleRepository(AppDbContext context) => _context = context;

    public Task<TenantRole?> GetAsync(Guid userId, string tenantId, CancellationToken ct = default) =>
        _context.TenantRoles.FirstOrDefaultAsync(r => r.UserId == userId && r.TenantId == tenantId, ct);

    public async Task<IReadOnlyList<TenantRole>> GetByTenantAsync(string tenantId, CancellationToken ct = default) =>
        await _context.TenantRoles.Where(r => r.TenantId == tenantId).ToListAsync(ct);

    public async Task AddAsync(TenantRole role, CancellationToken ct = default) =>
        await _context.TenantRoles.AddAsync(role, ct);

    public async Task SeedAdminForExistingUsersAsync(CancellationToken ct = default)
    {
        var usersWithoutRole = await _context.Users
            .Where(u => !_context.TenantRoles.Any(r => r.UserId == u.Id))
            .ToListAsync(ct);

        foreach (var user in usersWithoutRole)
            await _context.TenantRoles.AddAsync(TenantRole.Create(user.Id, user.TenantId, UserRole.Admin), ct);

        if (usersWithoutRole.Count > 0)
            await _context.SaveChangesAsync(ct);
    }
}
