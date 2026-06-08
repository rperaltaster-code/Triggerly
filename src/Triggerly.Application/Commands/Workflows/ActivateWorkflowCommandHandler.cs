using MediatR;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Application.Commands.Workflows;

public class ActivateWorkflowCommandHandler : IRequestHandler<ActivateWorkflowCommand>
{
    private readonly IWorkflowRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ActivateWorkflowCommandHandler(IWorkflowRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(ActivateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _repository.GetByIdWithStepsAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.Id} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        workflow.Activate();
        await _repository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class DeactivateWorkflowCommandHandler : IRequestHandler<DeactivateWorkflowCommand>
{
    private readonly IWorkflowRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeactivateWorkflowCommandHandler(IWorkflowRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeactivateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.Id} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        workflow.Deactivate();
        await _repository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class DeleteWorkflowCommandHandler : IRequestHandler<DeleteWorkflowCommand>
{
    private readonly IWorkflowRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteWorkflowCommandHandler(IWorkflowRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task Handle(DeleteWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.Id} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        await _repository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
