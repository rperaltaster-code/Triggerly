using Triggerly.Shared.Models;

namespace Triggerly.Domain.Entities;

public class TenantRole
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public DateTime AssignedAt { get; private set; }

    private TenantRole() { }

    public static TenantRole Create(Guid userId, string tenantId, UserRole role) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TenantId = tenantId,
            Role = role,
            AssignedAt = DateTime.UtcNow,
        };

    public void UpdateRole(UserRole role) => Role = role;
}
