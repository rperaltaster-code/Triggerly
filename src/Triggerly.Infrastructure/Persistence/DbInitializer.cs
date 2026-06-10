using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Infrastructure.Persistence;

public static class DbInitializer
{
    public static void EnsureCreated(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();

        // Backfill: any user without a TenantRole gets Admin (handles existing DBs)
        var roles = scope.ServiceProvider.GetRequiredService<ITenantRoleRepository>();
        roles.SeedAdminForExistingUsersAsync().GetAwaiter().GetResult();
    }
}
