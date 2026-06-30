using MediatR;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Sso;

public record SaveSsoConfigCommand(
    string TenantId,
    string ClientId,
    string ClientSecret,
    string DirectoryTenantId,
    string GroupClaimName,
    string GroupRoleMappings,
    string UserId,
    string UserName
) : IRequest<SsoConfigDto>;

public record DeleteSsoConfigCommand(string TenantId, string UserId, string UserName) : IRequest;

public record ToggleSsoConfigCommand(string TenantId, bool Enable, string UserId, string UserName) : IRequest;
