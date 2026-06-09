using MediatR;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Application.Commands.Workflows;

public class ActivateWorkflowCommandHandler : IRequestHandler<ActivateWorkflowCommand>
{
    private readonly IWorkflowRepository _repository;
    private readonly IAuditService _audit;

    public ActivateWorkflowCommandHandler(IWorkflowRepository repository, IAuditService audit)
    {
        _repository = repository;
        _audit = audit;
    }

    public async Task Handle(ActivateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.Id} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        await _repository.ActivateAsync(request.Id, cancellationToken);

        await _audit.LogAsync(request.TenantId, request.UserId, request.UserName,
            "WorkflowActivated", "Workflow", workflow.Id.ToString(), workflow.Name,
            ct: cancellationToken);
    }
}

public class DeactivateWorkflowCommandHandler : IRequestHandler<DeactivateWorkflowCommand>
{
    private readonly IWorkflowRepository _repository;
    private readonly IAuditService _audit;

    public DeactivateWorkflowCommandHandler(IWorkflowRepository repository, IAuditService audit)
    {
        _repository = repository;
        _audit = audit;
    }

    public async Task Handle(DeactivateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.Id} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        await _repository.DeactivateAsync(request.Id, cancellationToken);

        await _audit.LogAsync(request.TenantId, request.UserId, request.UserName,
            "WorkflowDeactivated", "Workflow", workflow.Id.ToString(), workflow.Name,
            ct: cancellationToken);
    }
}

public class DeleteWorkflowCommandHandler : IRequestHandler<DeleteWorkflowCommand>
{
    private readonly IWorkflowRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditService _audit;

    public DeleteWorkflowCommandHandler(IWorkflowRepository repository, IUnitOfWork unitOfWork, IAuditService audit)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _audit = audit;
    }

    public async Task Handle(DeleteWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.Id} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        await _repository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(request.TenantId, request.UserId, request.UserName,
            "WorkflowDeleted", "Workflow", workflow.Id.ToString(), workflow.Name,
            ct: cancellationToken);
    }
}
