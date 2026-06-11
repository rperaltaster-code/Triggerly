using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private string TenantId => User.FindFirstValue("tenantId") ?? throw new UnauthorizedAccessException();

    public TeamController(IMediator mediator) => _mediator = mediator;

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
}

public record UpdateRoleRequest(string Role);
