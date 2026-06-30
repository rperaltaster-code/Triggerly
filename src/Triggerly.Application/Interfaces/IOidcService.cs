using Triggerly.Domain.Entities;

namespace Triggerly.Application.Interfaces;

public record OidcUserInfo(string Sub, string Email, string Name, IReadOnlyList<string> Groups);

public interface IOidcService
{
    string BuildAuthorizationUrl(TenantSsoConfig config, string state, string redirectUri);
    Task<string> ExchangeCodeAsync(TenantSsoConfig config, string code, string redirectUri, CancellationToken ct = default);
    Task<OidcUserInfo> ValidateAndParseIdTokenAsync(TenantSsoConfig config, string idToken, CancellationToken ct = default);
}
