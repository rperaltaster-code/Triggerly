using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Application.Commands.Workflows;
using Triggerly.Application.Queries.Workflows;
using Triggerly.Shared.Models;

namespace Triggerly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkflowsController : ControllerBase
{
    private readonly IMediator _mediator;

    private string TenantId => User.FindFirstValue("tenantId") ?? throw new UnauthorizedAccessException("Missing tenantId claim.");
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Missing user claim.");
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

    public WorkflowsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] WorkflowStatus? status = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListWorkflowsQuery(TenantId, page, pageSize, status, search), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetWorkflowByIdQuery(id, TenantId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("{id:guid}/versions")]
    public async Task<IActionResult> GetVersions(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetWorkflowVersionsQuery(id, TenantId), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkflowRequest request, CancellationToken cancellationToken = default)
    {
        var command = new CreateWorkflowCommand(request.Name, request.Description ?? string.Empty, TenantId, UserId, UserName, []);
        var result = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkflowCommand command, CancellationToken cancellationToken = default)
    {
        var commandWithId = command with { Id = id, TenantId = TenantId };
        var result = await _mediator.Send(commandWithId, cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeleteWorkflowCommand(id, TenantId, UserId, UserName), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("{id:guid}/activate")]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new ActivateWorkflowCommand(id, TenantId, UserId, UserName), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new DeactivateWorkflowCommand(id, TenantId, UserId, UserName), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPost("{id:guid}/trigger")]
    public async Task<IActionResult> Trigger(
        Guid id,
        [FromBody] TriggerWorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new TriggerWorkflowCommand(id, TenantId, UserId, UserName, request.InputData), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("{id:guid}/form")]
    public async Task<IActionResult> SaveForm(
        Guid id,
        [FromBody] SaveFormRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new SaveWorkflowFormCommand(id, TenantId, request.Fields ?? []), cancellationToken);
        return Ok(result);
    }

    [Authorize(Roles = "Admin,Editor")]
    [HttpPut("{id:guid}/steps")]
    public async Task<IActionResult> SaveSteps(
        Guid id,
        [FromBody] SaveStepsRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new SaveWorkflowStepsCommand(
            id, TenantId,
            request.Steps.Select(s => new StepDefinition(
                s.Name, s.Type, s.Order, s.Config ?? [], s.ApproverEmail)).ToList());

        var result = await _mediator.Send(command, cancellationToken);
        return Ok(result);
    }
}

public record CreateWorkflowRequest(string Name, string? Description);
public record SaveFormRequest(List<FormField>? Fields);
public record TriggerWorkflowRequest(Dictionary<string, object>? InputData);

public record SaveStepsRequest(List<StepRequest> Steps);
public record StepRequest(
    string Name,
    string Type,
    int Order,
    Dictionary<string, object>? Config,
    string? ApproverEmail);
