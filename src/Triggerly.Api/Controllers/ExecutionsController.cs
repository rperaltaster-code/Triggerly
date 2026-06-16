using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Application.Commands.Executions;
using Triggerly.Application.Commands.Workflows;
using Triggerly.Application.Queries.Executions;
using Triggerly.Shared.Models;
using Triggerly.Shared.DTOs;

namespace Triggerly.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ExecutionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private string TenantId => User.FindFirstValue("tenantId") ?? throw new UnauthorizedAccessException("Missing tenantId claim.");
    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new UnauthorizedAccessException("Missing user claim.");
    private string UserName => User.FindFirstValue(ClaimTypes.Name) ?? "Unknown";

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
            new ListExecutionsQuery(TenantId, page, pageSize, workflowId, status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetExecutionByIdQuery(id, TenantId), cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = "Manager,Reviewer")]
    [HttpPost("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new ApproveExecutionCommand(id, UserId, UserName, TenantId), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Manager,Reviewer")]
    [HttpPost("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectRequest request, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new RejectExecutionCommand(id, UserId, UserName, request.Reason, TenantId), cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Manager,Reviewer")]
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(new CancelExecutionCommand(id, TenantId, UserId, UserName), cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/comments")]
    public async Task<IActionResult> GetComments(Guid id, CancellationToken cancellationToken = default)
    {
        var execution = await _mediator.Send(new GetExecutionByIdQuery(id, TenantId), cancellationToken);
        if (execution is null) return NotFound();
        return Ok(execution.Comments);
    }

    [HttpPost("{id:guid}/comments")]
    public async Task<IActionResult> AddComment(
        Guid id, [FromBody] AddCommentRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(
            new AddCommentCommand(id, TenantId, UserId,
                User.FindFirstValue(ClaimTypes.Name) ?? "Unknown", request.Content),
            cancellationToken);
        return CreatedAtAction(nameof(GetComments), new { id }, result);
    }

    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks(CancellationToken cancellationToken = default)
    {
        var result = await _mediator.Send(new GetMyTasksQuery(UserId, TenantId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id:guid}/steps/{stepId:guid}/complete")]
    public async Task<IActionResult> CompleteStep(
        Guid id, Guid stepId, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new CompleteActionStepCommand(id, stepId, UserId, UserName, TenantId),
            cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Manager")]
    [HttpPost("{id:guid}/steps/{stepId:guid}/reassign")]
    public async Task<IActionResult> ReassignStep(
        Guid id, Guid stepId, [FromBody] ReassignRequest request, CancellationToken cancellationToken = default)
    {
        await _mediator.Send(
            new ReassignTaskCommand(id, stepId, request.NewUserId, UserId, UserName, TenantId),
            cancellationToken);
        return NoContent();
    }
}

public record RejectRequest(string Reason);
public record AddCommentRequest(string Content);
public record ReassignRequest(Guid NewUserId);
