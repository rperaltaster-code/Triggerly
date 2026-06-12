using MediatR;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.Models;

namespace Triggerly.Application.Commands.Executions;

public record CompleteActionStepCommand(
    Guid ExecutionId,
    Guid StepId,
    string UserId,
    string UserName,
    string TenantId
) : IRequest;

public class CompleteActionStepCommandHandler : IRequestHandler<CompleteActionStepCommand>
{
    private readonly IWorkflowExecutionRepository _repository;
    private readonly ITemporalService _temporalService;
    private readonly ITenantRoleRepository _roles;
    private readonly IAuditService _audit;

    public CompleteActionStepCommandHandler(
        IWorkflowExecutionRepository repository,
        ITemporalService temporalService,
        ITenantRoleRepository roles,
        IAuditService audit)
    {
        _repository = repository;
        _temporalService = temporalService;
        _roles = roles;
        _audit = audit;
    }

    public async Task Handle(CompleteActionStepCommand request, CancellationToken cancellationToken)
    {
        var execution = await _repository.GetByIdAsync(request.ExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Execution {request.ExecutionId} not found.");

        if (execution.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        if (execution.Status != ExecutionStatus.Running)
            throw new InvalidOperationException("Execution is not running.");

        var step = execution.Steps.FirstOrDefault(s => s.StepId == request.StepId)
            ?? throw new KeyNotFoundException($"Step {request.StepId} not found.");

        if (!Guid.TryParse(request.UserId, out var userId))
            throw new UnauthorizedAccessException("Invalid user ID.");

        // Allow if assigned to this user or user is a Manager
        var tenantRole = await _roles.GetAsync(userId, request.TenantId, cancellationToken);
        var isManager = tenantRole?.Role == Triggerly.Shared.Models.UserRole.Manager;

        if (step.AssignedUserId != userId && !isManager)
            throw new UnauthorizedAccessException("You are not assigned to this step.");

        await _temporalService.SendActionCompleteSignalAsync(
            execution.TemporalWorkflowId, request.UserId, request.UserName, cancellationToken);

        await _audit.LogAsync(request.TenantId, request.UserId, request.UserName,
            "ActionStepCompleted", "ExecutionStep", step.Id.ToString(), step.StepName,
            ct: cancellationToken);
    }
}
