using MediatR;
using Triggerly.Shared.DTOs;
using Triggerly.Shared.Models;

namespace Triggerly.Application.Commands.Workflows;

public record CreateWorkflowCommand(
    string Name,
    string Description,
    string TenantId,
    string CreatedBy,
    string? CreatedByName,
    List<CreateWorkflowStepRequest> Steps
) : IRequest<WorkflowDto>;

public record CreateWorkflowStepRequest(
    string Name,
    StepType Type,
    int Order,
    Dictionary<string, object>? Config,
    string? ApproverEmail
);
