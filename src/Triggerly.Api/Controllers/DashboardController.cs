using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Application.Queries.Executions;

namespace Triggerly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private string TenantId => User.FindFirstValue("tenantId") ?? throw new UnauthorizedAccessException("Missing tenantId claim.");

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetDashboardStatsQuery(TenantId), cancellationToken);
        return Ok(result);
    }
}
