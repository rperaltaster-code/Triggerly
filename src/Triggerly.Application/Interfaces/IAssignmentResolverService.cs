namespace Triggerly.Application.Interfaces;

public record AssignedUser(Guid UserId, string UserName, string Email);

public interface IAssignmentResolverService
{
    Task<AssignedUser?> ResolveAsync(
        Dictionary<string, object> config, string tenantId,
        CancellationToken cancellationToken = default);
}
