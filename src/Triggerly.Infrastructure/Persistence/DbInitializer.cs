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

        // Migrate legacy role int values to new Preparer/Reviewer/Manager scheme:
        //   Admin(3) → Manager(2), Editor(2) → Preparer(0), Viewer(0) → Preparer(0), Approver(1) → Reviewer(1)
        db.Database.ExecuteSqlRaw("""
            UPDATE TenantRoles SET Role = CASE
                WHEN Role = 3 THEN 2
                WHEN Role = 2 THEN 0
                ELSE Role
            END
            WHERE Role IN (2, 3)
            """);

        // Backfill: any user without a TenantRole gets Manager
        var roles = scope.ServiceProvider.GetRequiredService<ITenantRoleRepository>();
        roles.SeedAdminForExistingUsersAsync().GetAwaiter().GetResult();
    }
}
