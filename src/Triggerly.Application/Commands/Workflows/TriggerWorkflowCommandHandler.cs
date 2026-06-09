using MediatR;
using Triggerly.Application.Interfaces;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;
using Triggerly.Shared.Models;

namespace Triggerly.Application.Commands.Workflows;

public class TriggerWorkflowCommandHandler : IRequestHandler<TriggerWorkflowCommand, WorkflowExecutionDto>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowExecutionRepository _executionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITemporalService _temporalService;
    private readonly IAuditService _audit;

    public TriggerWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IWorkflowExecutionRepository executionRepository,
        IUnitOfWork unitOfWork,
        ITemporalService temporalService,
        IAuditService audit)
    {
        _workflowRepository = workflowRepository;
        _executionRepository = executionRepository;
        _unitOfWork = unitOfWork;
        _temporalService = temporalService;
        _audit = audit;
    }

    public async Task<WorkflowExecutionDto> Handle(TriggerWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdWithStepsAsync(request.WorkflowId, cancellationToken)
            ?? throw new KeyNotFoundException($"Workflow {request.WorkflowId} not found.");

        if (workflow.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        if (workflow.Status != WorkflowStatus.Active)
            throw new InvalidOperationException("Only active workflows can be triggered.");

        var temporalWorkflowId = $"triggerly-{workflow.Id}-{Guid.NewGuid():N}";

        var execution = WorkflowExecution.Create(
            workflow.Id,
            temporalWorkflowId,
            request.TenantId,
            request.TriggeredBy,
            request.InputData);

        await _executionRepository.AddAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var runId = await _temporalService.StartWorkflowAsync(
            workflow.Id,
            execution.Id,
            request.TenantId,
            request.InputData,
            cancellationToken);

        execution.Start(runId);
        await _executionRepository.UpdateAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _audit.LogAsync(request.TenantId,
            request.TriggeredBy ?? "system",
            request.TriggeredByName ?? request.TriggeredBy ?? "System",
            "ExecutionTriggered", "Execution", execution.Id.ToString(), workflow.Name,
            ct: cancellationToken);

        return MapToDto(execution, workflow.Name);
    }

    private static WorkflowExecutionDto MapToDto(WorkflowExecution execution, string workflowName) =>
        new(
            execution.Id,
            execution.WorkflowId,
            workflowName,
            execution.TemporalWorkflowId,
            execution.TemporalRunId,
            execution.Status,
            execution.TenantId,
            execution.TriggeredBy,
            execution.InputData,
            execution.OutputData,
            execution.ErrorMessage,
            execution.CurrentStepOrder,
            execution.CurrentStepName,
            execution.StartedAt,
            execution.CompletedAt,
            execution.SlaBreachedAt,
            [],
            []);
}
