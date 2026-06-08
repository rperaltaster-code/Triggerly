using MediatR;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Application.Queries.Executions;

namespace Triggerly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;
    private const string DemoTenantId = "tenant-demo";

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetDashboardStatsQuery(DemoTenantId), cancellationToken);
        return Ok(result);
    }
}
