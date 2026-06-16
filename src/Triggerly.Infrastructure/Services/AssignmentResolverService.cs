using Triggerly.Application.Interfaces;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.Models;
using Triggerly.Shared.Utils;

namespace Triggerly.Infrastructure.Services;

public class AssignmentResolverService : IAssignmentResolverService
{
    private readonly IUserRepository _users;
    private readonly ITenantRoleRepository _roles;
    private readonly IWorkflowExecutionRepository _executions;

    public AssignmentResolverService(
        IUserRepository users,
        ITenantRoleRepository roles,
        IWorkflowExecutionRepository executions)
    {
        _users = users;
        _roles = roles;
        _executions = executions;
    }

    public async Task<AssignedUser?> ResolveAsync(
        Dictionary<string, object> config, string tenantId,
        CancellationToken cancellationToken = default)
    {
        var mode = GetString(config, "assignmentMode");
        if (string.IsNullOrEmpty(mode)) return null;

        if (mode == "specific")
        {
            var idStr = GetString(config, "assignedUserId");
            if (!Guid.TryParse(idStr, out var userId)) return null;
            var user = await _users.GetByIdAsync(userId, cancellationToken);
            if (user is null) return null;
            var role = await _roles.GetAsync(userId, tenantId, cancellationToken);
            if (role is null) return null; // user is not a member of this tenant
            return new AssignedUser(user.Id, user.Name, user.Email);
        }

        var roleStr = GetString(config, "assignedRole") ?? "Preparer";
        if (!Enum.TryParse<UserRole>(roleStr, true, out var targetRole)) return null;

        var allUsers = await _users.GetByTenantAsync(tenantId, cancellationToken);
        var allRoles = await _roles.GetByTenantAsync(tenantId, cancellationToken);
        var roleMap = allRoles.ToDictionary(r => r.UserId, r => r.Role);

        var eligible = allUsers
            .Where(u => roleMap.TryGetValue(u.Id, out var r) && r == targetRole)
            .ToList();

        if (eligible.Count == 0) return null;

        if (mode == "role-any")
        {
            var picked = eligible[Random.Shared.Next(eligible.Count)];
            return new AssignedUser(picked.Id, picked.Name, picked.Email);
        }

        // Both least-loaded and round-robin pick the user with fewest open tasks.
        // Stable Id ordering as tiebreaker gives a consistent rotation when loads are equal.
        // True stateful round-robin would require persistent last-assigned tracking.
        var taskCounts = await _executions.GetOpenTaskCountsByUserAsync(tenantId, cancellationToken);
        var best = eligible
            .OrderBy(u => taskCounts.GetValueOrDefault(u.Id, 0))
            .ThenBy(u => u.Id)
            .First();

        return new AssignedUser(best.Id, best.Name, best.Email);
    }

    private static string? GetString(Dictionary<string, object> dict, string key) =>
        dict.TryGetValue(key, out var v) ? JsonHelpers.GetString(v) : null;
}
