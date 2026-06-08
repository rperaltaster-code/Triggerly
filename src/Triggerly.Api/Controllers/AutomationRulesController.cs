using MediatR;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Application.Commands.AutomationRules;
using Triggerly.Application.Queries.AutomationRules;

namespace Triggerly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AutomationRulesController : ControllerBase
{
    private readonly IMediator _mediator;
    private const string DemoTenantId = "tenant-demo";

    public AutomationRulesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isEnabled = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListAutomationRulesQuery(DemoTenantId, page, pageSize, isEnabled), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAutomationRuleByIdQuery(id, DemoTenantId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAutomationRuleCommand command, CancellationToken cancellationToken = default)
    {
        var commandWithTenant = command with { TenantId = DemoTenantId };
        var result = await _mediator.Send(commandWithTenant, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateAutomationRuleCommand command, CancellationToken cancellationToken = default)
    {
        var commandWithId = command with { Id = id, TenantId = DemoTenantId };
        var result = await _mediator.Send(commandWithId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteAutomationRuleCommand(id, DemoTenantId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new ToggleAutomationRuleCommand(id, true, DemoTenantId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new ToggleAutomationRuleCommand(id, false, DemoTenantId), cancellationToken);
        return NoContent();
    }
}
