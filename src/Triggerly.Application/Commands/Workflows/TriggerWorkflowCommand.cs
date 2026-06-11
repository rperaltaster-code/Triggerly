using MediatR;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Workflows;

public record TriggerWorkflowCommand(
    Guid WorkflowId,
    string TenantId,
    string? TriggeredBy,
    string? TriggeredByName,
    Dictionary<string, object>? InputData,
    Guid? ClientId = null,
    Guid? ClientServiceId = null
) : IRequest<WorkflowExecutionDto>;
