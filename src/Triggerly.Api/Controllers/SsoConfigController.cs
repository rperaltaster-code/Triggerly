using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Application.Commands.Sso;
using Triggerly.Application.Queries.Sso;

namespace Triggerly.Api.Controllers;

public record SaveSsoConfigRequest(
    string ClientId,
    string ClientSecret,
    string DirectoryTenantId,
    string GroupClaimName,
    string GroupRoleMappings
);

[Authorize]
[ApiController]
[Route("api/sso-config")]
public class SsoConfigController : ControllerBase
{
    private readonly IMediator _mediator;
    private string TenantId => User.FindFirstValue("tenantId") ?? throw new UnauthorizedAccessException();
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException();
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

    public SsoConfigController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSsoConfigQuery(TenantId), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Save([FromBody] SaveSsoConfigRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new SaveSsoConfigCommand(
            TenantId, request.ClientId, request.ClientSecret,
            request.DirectoryTenantId, request.GroupClaimName, request.GroupRoleMappings,
            UserId, UserName), ct);
        return Ok(result);
    }

    [HttpDelete]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Delete(CancellationToken ct)
    {
        await _mediator.Send(new DeleteSsoConfigCommand(TenantId, UserId, UserName), ct);
        return NoContent();
    }

    [HttpPost("enable")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Enable(CancellationToken ct)
    {
        await _mediator.Send(new ToggleSsoConfigCommand(TenantId, true, UserId, UserName), ct);
        return NoContent();
    }

    [HttpPost("disable")]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> Disable(CancellationToken ct)
    {
        await _mediator.Send(new ToggleSsoConfigCommand(TenantId, false, UserId, UserName), ct);
        return NoContent();
    }
}
