namespace Triggerly.Shared.DTOs;

public record SsoConfigDto(
    Guid Id,
    string Provider,
    string ClientId,
    string DirectoryTenantId,
    string GroupClaimName,
    string GroupRoleMappings,
    bool IsEnabled
);

// Returned to the login page — no secrets
public record SsoPublicDto(string Provider, bool IsEnabled);
