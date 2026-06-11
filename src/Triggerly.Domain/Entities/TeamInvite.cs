using System.Security.Cryptography;
using Triggerly.Shared.Models;

namespace Triggerly.Domain.Entities;

public class TeamInvite
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string Token { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }

    private TeamInvite() { }

    public static TeamInvite Create(string tenantId, string email, UserRole role)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-").Replace("/", "_").TrimEnd('=');

        return new TeamInvite
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Email = email.Trim().ToLowerInvariant(),
            Role = role,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
        };
    }

    public void Accept() => AcceptedAt = DateTime.UtcNow;

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsAccepted => AcceptedAt.HasValue;
}
