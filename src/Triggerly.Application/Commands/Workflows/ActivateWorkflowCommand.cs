using MediatR;

namespace Triggerly.Application.Commands.Workflows;

public record ActivateWorkflowCommand(Guid Id, string TenantId, string UserId, string UserName) : IRequest;

public record DeactivateWorkflowCommand(Guid Id, string TenantId, string UserId, string UserName) : IRequest;

public record DeleteWorkflowCommand(Guid Id, string TenantId, string UserId, string UserName) : IRequest;
