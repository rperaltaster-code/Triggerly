using MediatR;

namespace Triggerly.Application.Commands.Workflows;

public record ApproveExecutionCommand(
    Guid ExecutionId,
    string ActorId,
    string TenantId
) : IRequest;

public record RejectExecutionCommand(
    Guid ExecutionId,
    string ActorId,
    string Reason,
    string TenantId
) : IRequest;

public record CancelExecutionCommand(
    Guid ExecutionId,
    string TenantId
) : IRequest;
