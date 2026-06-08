using MediatR;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Application.Commands.Executions;
using Triggerly.Application.Commands.Workflows;
using Triggerly.Application.Queries.Executions;
using Triggerly.Shared.Models;

namespace Triggerly.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExecutionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private const string DemoTenantId = "tenant-demo";
    private const string DemoUserId = "user-demo";

    public ExecutionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] Guid? workflowId = null,
        [FromQuery] ExecutionStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new ListExecutionsQuery(DemoTenantId, page, pageSize, workflowId, status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetExecutionByIdQuery(id, DemoTenantId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new ApproveExecutionCommand(id, DemoUserId, DemoTenantId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequest request, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new RejectExecutionCommand(id, DemoUserId, request.Reason, DemoTenantId), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new CancelExecutionCommand(id, DemoTenantId), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid id, CancellationToken cancellationToken = default)
    {
        var execution = await _mediator.Send(new GetExecutionByIdQuery(id, DemoTenantId), cancellationToken);
        if (execution is null) return NotFound();
        return Ok(execution.Comments);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id, [FromBody] AddCommentRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new AddCommentCommand(id, DemoTenantId, DemoUserId, "Demo User", request.Content),
            cancellationToken);
        return CreatedAtAction(nameof(GetComments), new { id }, result);
    }
}

public record RejectRequest(string Reason);
public record AddCommentRequest(string Content);
