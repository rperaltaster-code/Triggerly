using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Application.Commands.AutomationRules;
using Triggerly.Application.Queries.AutomationRules;
using Triggerly.Shared.Models;

namespace Triggerly.Api.Controllers;

public record CreateAutomationRuleRequest(string Name, string? Description, TriggerType TriggerType, string? TriggerConfig, Guid WorkflowId);

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AutomationRulesController : ControllerBase
{
    private readonly IMediator _mediator;
    private string TenantId => User.FindFirstValue("tenantId") ?? throw new UnauthorizedAccessException("Missing tenantId claim.");

    public AutomationRulesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? isEnabled = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListAutomationRulesQuery(TenantId, page, pageSize, isEnabled), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetAutomationRuleByIdQuery(id, TenantId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAutomationRuleRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateAutomationRuleCommand(request.Name, request.Description ?? string.Empty, request.TriggerType, request.TriggerConfig ?? "{}", request.WorkflowId, TenantId);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateAutomationRuleCommand command, CancellationToken cancellationToken = default)
    {
        var commandWithId = command with { Id = id, TenantId = TenantId };
        var result = await _mediator.Send(commandWithId, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteAutomationRuleCommand(id, TenantId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/enable")]
    public async Task<IActionResult> Enable(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new ToggleAutomationRuleCommand(id, true, TenantId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/disable")]
    public async Task<IActionResult> Disable(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new ToggleAutomationRuleCommand(id, false, TenantId), cancellationToken);
        return NoContent();
    }
}
