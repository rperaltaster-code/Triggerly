using MediatR;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Workflows;

public record TriggerWorkflowCommand(
    Guid WorkflowId,
    string TenantId,
    string? TriggeredBy,
    Dictionary<string, object>? InputData
) : IRequest<WorkflowExecutionDto>;
