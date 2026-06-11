using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Triggerly.Application.Commands.Team;
using Triggerly.Application.Queries.Team;
using Triggerly.Shared.Models;

namespace Triggerly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TeamController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _configuration;
    private string TenantId => User.FindFirstValue("tenantId") ?? throw new UnauthorizedAccessException();
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? "A team member";

    public TeamController(IMediator mediator, IConfiguration configuration)
    {
        _mediator = mediator;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> GetTeam(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetTeamQuery(TenantId), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Manager")]
    [HttpPut("{userId:guid}/role")]
    public async Task<IActionResult> UpdateRole(Guid userId, [FromBody] UpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            return BadRequest($"Invalid role '{request.Role}'. Valid values: Preparer, Reviewer, Manager.");

        await _mediator.Send(new UpdateUserRoleCommand(userId, TenantId, role), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("invite")]
    public async Task<IActionResult> InviteMember([FromBody] InviteRequest request, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            return BadRequest($"Invalid role '{request.Role}'. Valid values: Preparer, Reviewer, Manager.");

        var baseUrl = _configuration["App:BaseUrl"] ?? "http://localhost:5173";
        await _mediator.Send(new InviteTeamMemberCommand(TenantId, request.Email, role, UserName, baseUrl), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Manager")]
    [HttpGet("invites")]
    public async Task<IActionResult> GetInvites(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetPendingInvitesQuery(TenantId), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Manager")]
    [HttpDelete("invites/{id:guid}")]
    public async Task<IActionResult> RevokeInvite(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new RevokeInviteCommand(id, TenantId), cancellationToken);
        return NoContent();
    }
}

public record UpdateRoleRequest(string Role);
public record InviteRequest(string Email, string Role);
