using MediatR;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Workflows;

public record UpdateWorkflowCommand(
    Guid Id,
    string Name,
    string Description,
    string TenantId
) : IRequest<WorkflowDto>;
