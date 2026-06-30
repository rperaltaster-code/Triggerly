namespace Triggerly.Domain.Entities;

public class TenantSsoConfig
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string Provider { get; private set; } = "Microsoft";
    public string ClientId { get; private set; } = string.Empty;
    public string ClientSecret { get; private set; } = string.Empty;
    // Azure AD / Entra directory (tenant) ID
    public string DirectoryTenantId { get; private set; } = string.Empty;
    // Claim that carries group membership (default: "groups")
    public string GroupClaimName { get; private set; } = "groups";
    // JSON: { "group-guid-or-name": "Manager|Reviewer|Preparer" }
    public string GroupRoleMappings { get; private set; } = "{}";
    public bool IsEnabled { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private TenantSsoConfig() { }

    public static TenantSsoConfig Create(
        string tenantId,
        string clientId,
        string clientSecret,
        string directoryTenantId,
        string groupClaimName,
        string groupRoleMappings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientId);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientSecret);
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryTenantId);

        return new TenantSsoConfig
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Provider = "Microsoft",
            ClientId = clientId,
            ClientSecret = clientSecret,
            DirectoryTenantId = directoryTenantId,
            GroupClaimName = string.IsNullOrWhiteSpace(groupClaimName) ? "groups" : groupClaimName,
            GroupRoleMappings = string.IsNullOrWhiteSpace(groupRoleMappings) ? "{}" : groupRoleMappings,
            IsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public void Update(string clientId, string clientSecret, string directoryTenantId, string groupClaimName, string groupRoleMappings)
    {
        ClientId = clientId;
        if (!string.IsNullOrWhiteSpace(clientSecret))
            ClientSecret = clientSecret;
        DirectoryTenantId = directoryTenantId;
        GroupClaimName = string.IsNullOrWhiteSpace(groupClaimName) ? "groups" : groupClaimName;
        GroupRoleMappings = string.IsNullOrWhiteSpace(groupRoleMappings) ? "{}" : groupRoleMappings;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public string Authority => $"https://login.microsoftonline.com/{DirectoryTenantId}/v2.0";
}
