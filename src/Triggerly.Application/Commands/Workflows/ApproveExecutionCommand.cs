using MediatR;

namespace Triggerly.Application.Commands.Workflows;

public record ApproveExecutionCommand(
    Guid ExecutionId,
    string ActorId,
    string ActorName,
    string TenantId
) : IRequest;

public record RejectExecutionCommand(
    Guid ExecutionId,
    string ActorId,
    string ActorName,
    string Reason,
    string TenantId
) : IRequest;

public record CancelExecutionCommand(
    Guid ExecutionId,
    string TenantId,
    string UserId,
    string UserName
) : IRequest;
