using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Web;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Entities;

namespace Triggerly.Infrastructure.Services;

public class OidcService : IOidcService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;

    public OidcService(IHttpClientFactory httpClientFactory, IMemoryCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
    }

    public string BuildAuthorizationUrl(TenantSsoConfig config, string state, string redirectUri)
    {
        var scopes = HttpUtility.UrlEncode("openid profile email offline_access");
        var clientId = HttpUtility.UrlEncode(config.ClientId);
        var redirect = HttpUtility.UrlEncode(redirectUri);
        var encodedState = HttpUtility.UrlEncode(state);

        return $"{config.Authority}/oauth2/v2.0/authorize" +
               $"?client_id={clientId}" +
               $"&response_type=code" +
               $"&redirect_uri={redirect}" +
               $"&response_mode=query" +
               $"&scope={scopes}" +
               $"&state={encodedState}";
    }

    public async Task<string> ExchangeCodeAsync(TenantSsoConfig config, string code, string redirectUri, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient();
        var tokenEndpoint = $"{config.Authority}/oauth2/v2.0/token";

        var body = new Dictionary<string, string>
        {
            ["client_id"] = config.ClientId,
            ["client_secret"] = config.ClientSecret,
            ["code"] = code,
            ["redirect_uri"] = redirectUri,
            ["grant_type"] = "authorization_code",
            ["scope"] = "openid profile email",
        };

        var response = await client.PostAsync(tokenEndpoint, new FormUrlEncodedContent(body), ct);
        var content = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Token exchange failed: {content}");

        var doc = JsonDocument.Parse(content);
        return doc.RootElement.GetProperty("id_token").GetString()
            ?? throw new InvalidOperationException("id_token missing from response.");
    }

    public async Task<OidcUserInfo> ValidateAndParseIdTokenAsync(TenantSsoConfig config, string idToken, CancellationToken ct = default)
    {
        var keys = await GetSigningKeysAsync(config, ct);

        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers =
            [
                $"https://login.microsoftonline.com/{config.DirectoryTenantId}/v2.0",
                $"https://sts.windows.net/{config.DirectoryTenantId}/"
            ],
            ValidateAudience = true,
            ValidAudience = config.ClientId,
            ValidateLifetime = true,
            IssuerSigningKeys = keys,
            ValidateIssuerSigningKey = true,
        };

        ClaimsPrincipal principal;
        try
        {
            principal = handler.ValidateToken(idToken, validationParams, out _);
        }
        catch (Exception ex)
        {
            throw new UnauthorizedAccessException($"ID token validation failed: {ex.Message}");
        }

        var sub = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? throw new UnauthorizedAccessException("sub claim missing.");
        var email = principal.FindFirst(ClaimTypes.Email)?.Value
                    ?? principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                    ?? throw new UnauthorizedAccessException("email claim missing.");
        var name = principal.FindFirst(ClaimTypes.Name)?.Value
                   ?? principal.FindFirst(JwtRegisteredClaimNames.Name)?.Value
                   ?? email;

        var groups = principal.Claims
            .Where(c => c.Type == config.GroupClaimName || c.Type == "groups")
            .Select(c => c.Value)
            .ToList();

        return new OidcUserInfo(sub, email.ToLowerInvariant(), name, groups);
    }

    private async Task<IEnumerable<SecurityKey>> GetSigningKeysAsync(TenantSsoConfig config, CancellationToken ct)
    {
        var cacheKey = $"oidc-jwks-{config.DirectoryTenantId}";
        if (_cache.TryGetValue(cacheKey, out IEnumerable<SecurityKey>? cached) && cached is not null)
            return cached;

        var client = _httpClientFactory.CreateClient();

        // Fetch OIDC discovery document to get JWKS URI
        var discoveryUrl = $"{config.Authority}/.well-known/openid-configuration";
        var discovery = await client.GetFromJsonAsync<JsonDocument>(discoveryUrl, ct)
            ?? throw new InvalidOperationException("Failed to fetch OIDC discovery document.");

        var jwksUri = discovery.RootElement.GetProperty("jwks_uri").GetString()
            ?? throw new InvalidOperationException("jwks_uri missing from discovery document.");

        var jwksJson = await client.GetStringAsync(jwksUri, ct);
        var keySet = new JsonWebKeySet(jwksJson);
        var keys = keySet.GetSigningKeys();

        _cache.Set(cacheKey, keys, TimeSpan.FromHours(12));
        return keys;
    }
}
