using MediatR;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Application.Commands.Executions;

public record ReassignTaskCommand(
    Guid ExecutionId,
    Guid StepId,
    Guid NewUserId,
    string RequestingUserId,
    string RequestingUserName,
    string TenantId
) : IRequest;

public class ReassignTaskCommandHandler : IRequestHandler<ReassignTaskCommand>
{
    private readonly IWorkflowExecutionRepository _repository;
    private readonly IUserRepository _users;
    private readonly IEmailService _email;
    private readonly IEmailTemplateService _templates;
    private readonly IAuditService _audit;

    public ReassignTaskCommandHandler(
        IWorkflowExecutionRepository repository,
        IUserRepository users,
        IEmailService email,
        IEmailTemplateService templates,
        IAuditService audit)
    {
        _repository = repository;
        _users = users;
        _email = email;
        _templates = templates;
        _audit = audit;
    }

    public async Task Handle(ReassignTaskCommand request, CancellationToken cancellationToken)
    {
        var execution = await _repository.GetByIdAsync(request.ExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Execution {request.ExecutionId} not found.");

        if (execution.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        var step = execution.Steps.FirstOrDefault(s => s.StepId == request.StepId)
            ?? throw new KeyNotFoundException($"Step {request.StepId} not found.");

        var newUser = await _users.GetByIdAsync(request.NewUserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.NewUserId} not found.");

        await _repository.ReassignStepAsync(
            request.ExecutionId, request.StepId,
            request.NewUserId, newUser.Name, cancellationToken);

        var tokens = new Dictionary<string, string>
        {
            ["workflowName"] = execution.InputData.TryGetValue("workflowName", out var wn) ? wn?.ToString() ?? string.Empty : string.Empty,
            ["stepName"] = step.StepName,
            ["executionId"] = execution.Id.ToString(),
            ["approvalsUrl"] = "/my-tasks",
            ["slaHours"] = "—",
        };

        try
        {
            var (subject, body) = await _templates.GetRenderedAsync(
                request.TenantId, "approval_request", tokens, cancellationToken);
            await _email.SendAsync(newUser.Email, subject, body, cancellationToken);
        }
        catch
        {
            // notification failure must not fail the reassignment
        }

        await _audit.LogAsync(request.TenantId, request.RequestingUserId, request.RequestingUserName,
            "TaskReassigned", "ExecutionStep", step.Id.ToString(), step.StepName,
            $"Reassigned to {newUser.Name}", cancellationToken);
    }
}
