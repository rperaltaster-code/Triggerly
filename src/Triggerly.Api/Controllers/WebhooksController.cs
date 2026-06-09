using MediatR;
using Microsoft.AspNetCore.Mvc;
using Triggerly.Application.Commands.Workflows;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Api.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IAutomationRuleRepository _ruleRepository;
    private readonly IMediator _mediator;

    public WebhooksController(IAutomationRuleRepository ruleRepository, IMediator mediator)
    {
        _ruleRepository = ruleRepository;
        _mediator = mediator;
    }

    [HttpPost("{token}")]
    public async Task<IActionResult> Receive(string token, [FromBody] Dictionary<string, object>? payload, CancellationToken cancellationToken)
    {
        var rule = await _ruleRepository.GetByWebhookTokenAsync(token, cancellationToken);
        if (rule is null || !rule.IsEnabled)
            return NotFound(new { error = "Webhook not found or disabled." });

        var result = await _mediator.Send(new TriggerWorkflowCommand(
            rule.WorkflowId,
            rule.TenantId,
            "webhook",
            $"Webhook: {rule.Name}",
            payload), cancellationToken);

        return Ok(new { executionId = result.Id, status = result.Status.ToString() });
    }
}
