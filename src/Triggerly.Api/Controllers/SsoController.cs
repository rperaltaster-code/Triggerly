using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Application.Interfaces;
using Triggerly.Application.Queries.Sso;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using UserEntity = Triggerly.Domain.Entities.User;

namespace Triggerly.Api.Controllers;

[ApiController]
[Route("api/sso")]
public class SsoController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IOidcService _oidcService;
    private readonly IUserRepository _users;
    private readonly ITenantRoleRepository _roles;
    private readonly ITenantSsoConfigRepository _ssoConfigs;
    private readonly ITokenService _tokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _config;

    public SsoController(
        IMediator mediator,
        IOidcService oidcService,
        IUserRepository users,
        ITenantRoleRepository roles,
        ITenantSsoConfigRepository ssoConfigs,
        ITokenService tokenService,
        IUnitOfWork unitOfWork,
        IConfiguration config)
    {
        _mediator = mediator;
        _oidcService = oidcService;
        _users = users;
        _roles = roles;
        _ssoConfigs = ssoConfigs;
        _tokenService = tokenService;
        _unitOfWork = unitOfWork;
        _config = config;
    }

    // Returns the public SSO info (provider name, enabled) for the login page
    [HttpGet("{tenantId}/public")]
    public async Task<IActionResult> GetPublicInfo(string tenantId, CancellationToken ct)
    {
        var info = await _mediator.Send(new GetSsoPublicInfoQuery(tenantId), ct);
        if (info is null || !info.IsEnabled) return NotFound();
        return Ok(info);
    }

    // Builds and returns the authorization URL to redirect the user to
    [HttpGet("{tenantId}/init")]
    public async Task<IActionResult> Initiate(string tenantId, CancellationToken ct)
    {
        var ssoConfig = await _ssoConfigs.GetByTenantAsync(tenantId, ct);
        if (ssoConfig is null || !ssoConfig.IsEnabled)
            return NotFound("SSO is not configured or disabled for this tenant.");

        var state = BuildState(tenantId);
        var redirectUri = GetCallbackUrl();
        var authUrl = _oidcService.BuildAuthorizationUrl(ssoConfig, state, redirectUri);

        return Ok(new { url = authUrl });
    }

    // Microsoft redirects here after authentication
    [HttpGet("callback")]
    public async Task<IActionResult> Callback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken ct = default)
    {
        var frontendBase = _config["App:BaseUrl"] ?? "http://localhost:5173";

        if (!string.IsNullOrEmpty(error))
            return Redirect($"{frontendBase}/login?sso_error={Uri.EscapeDataString(errorDescription ?? error)}");

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect($"{frontendBase}/login?sso_error=missing_code");

        string tenantId;
        try { tenantId = ParseTenantFromState(state); }
        catch { return Redirect($"{frontendBase}/login?sso_error=invalid_state"); }

        try
        {
            var ssoConfig = await _ssoConfigs.GetByTenantAsync(tenantId, ct)
                ?? throw new InvalidOperationException("SSO not configured.");

            var redirectUri = GetCallbackUrl();
            var idToken = await _oidcService.ExchangeCodeAsync(ssoConfig, code, redirectUri, ct);
            var userInfo = await _oidcService.ValidateAndParseIdTokenAsync(ssoConfig, idToken, ct);

            var role = ResolveRole(ssoConfig.GroupRoleMappings, userInfo.Groups);
            var user = await JitProvisionAsync(userInfo, ssoConfig.TenantId, ct);
            await EnsureRoleAsync(user.Id, ssoConfig.TenantId, role, ct);

            var jwt = _tokenService.GenerateToken(user, role);
            return Redirect($"{frontendBase}/sso/complete?token={Uri.EscapeDataString(jwt)}");
        }
        catch (Exception ex)
        {
            return Redirect($"{frontendBase}/login?sso_error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    // --- helpers ---

    private string GetCallbackUrl() =>
        _config["App:SsoCallbackUrl"] ?? "http://localhost:5000/api/sso/callback";

    private static string BuildState(string tenantId)
    {
        var nonce = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
        var raw = $"{tenantId}|{nonce}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static string ParseTenantFromState(string state)
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(state));
        return raw.Split('|')[0];
    }

    private static string ResolveRole(string groupRoleMappingsJson, IReadOnlyList<string> groups)
    {
        if (groups.Count == 0 || string.IsNullOrWhiteSpace(groupRoleMappingsJson))
            return "Preparer";

        try
        {
            var mappings = JsonSerializer.Deserialize<Dictionary<string, string>>(groupRoleMappingsJson)
                           ?? [];

            // Priority: Manager > Reviewer > Preparer
            foreach (var priority in new[] { "Manager", "Reviewer", "Preparer" })
            {
                if (mappings.Any(m => m.Value == priority && groups.Contains(m.Key)))
                    return priority;
            }
        }
        catch { /* fall through to default */ }

        return "Preparer";
    }

    private async Task<UserEntity> JitProvisionAsync(OidcUserInfo info, string tenantId, CancellationToken ct)
    {
        var existing = await _users.GetByEmailAsync(info.Email, ct);
        if (existing is not null && existing.TenantId == tenantId)
            return existing;

        var user = UserEntity.CreateForSso(info.Name, info.Email, tenantId);
        await _users.AddAsync(user, ct);
        await _unitOfWork.SaveChangesAsync(ct);
        return user;
    }

    private async Task EnsureRoleAsync(Guid userId, string tenantId, string role, CancellationToken ct)
    {
        var parsed = Enum.Parse<Triggerly.Shared.Models.UserRole>(role);
        var existing = await _roles.GetAsync(userId, tenantId, ct);

        if (existing is null)
            await _roles.AddAsync(TenantRole.Create(userId, tenantId, parsed), ct);
        else if (existing.Role != parsed)
            existing.UpdateRole(parsed);
        else
            return;

        await _unitOfWork.SaveChangesAsync(ct);
    }
}
