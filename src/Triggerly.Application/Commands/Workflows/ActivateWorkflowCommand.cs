using MediatR;

namespace Triggerly.Application.Commands.Workflows;

public record ActivateWorkflowCommand(Guid Id, string TenantId) : IRequest;

public record DeactivateWorkflowCommand(Guid Id, string TenantId) : IRequest;

public record DeleteWorkflowCommand(Guid Id, string TenantId) : IRequest;
