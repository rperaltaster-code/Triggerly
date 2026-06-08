using MediatR;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Application.Commands.Workflows;

public class ApproveExecutionCommandHandler : IRequestHandler<ApproveExecutionCommand>
{
    private readonly IWorkflowExecutionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITemporalService _temporalService;

    public ApproveExecutionCommandHandler(
        IWorkflowExecutionRepository repository,
        IUnitOfWork unitOfWork,
        ITemporalService temporalService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _temporalService = temporalService;
    }

    public async Task Handle(ApproveExecutionCommand request, CancellationToken cancellationToken)
    {
        var execution = await _repository.GetByIdAsync(request.ExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Execution {request.ExecutionId} not found.");

        if (execution.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        execution.Approve();
        await _temporalService.SendApprovalSignalAsync(
            execution.TemporalWorkflowId, true, request.ActorId, cancellationToken: cancellationToken);

        await _repository.UpdateAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class RejectExecutionCommandHandler : IRequestHandler<RejectExecutionCommand>
{
    private readonly IWorkflowExecutionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITemporalService _temporalService;

    public RejectExecutionCommandHandler(
        IWorkflowExecutionRepository repository,
        IUnitOfWork unitOfWork,
        ITemporalService temporalService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _temporalService = temporalService;
    }

    public async Task Handle(RejectExecutionCommand request, CancellationToken cancellationToken)
    {
        var execution = await _repository.GetByIdAsync(request.ExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Execution {request.ExecutionId} not found.");

        if (execution.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        execution.Reject(request.Reason);
        await _temporalService.SendApprovalSignalAsync(
            execution.TemporalWorkflowId, false, request.ActorId, request.Reason, cancellationToken);

        await _repository.UpdateAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}

public class CancelExecutionCommandHandler : IRequestHandler<CancelExecutionCommand>
{
    private readonly IWorkflowExecutionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITemporalService _temporalService;

    public CancelExecutionCommandHandler(
        IWorkflowExecutionRepository repository,
        IUnitOfWork unitOfWork,
        ITemporalService temporalService)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _temporalService = temporalService;
    }

    public async Task Handle(CancelExecutionCommand request, CancellationToken cancellationToken)
    {
        var execution = await _repository.GetByIdAsync(request.ExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Execution {request.ExecutionId} not found.");

        if (execution.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        execution.Cancel();
        await _temporalService.CancelWorkflowAsync(execution.TemporalWorkflowId, cancellationToken);
        await _repository.UpdateAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
