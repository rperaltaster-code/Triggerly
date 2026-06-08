using MediatR;
using Triggerly.Application.Common;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;
using Triggerly.Shared.Models;

namespace Triggerly.Application.Queries.Executions;

public class GetExecutionByIdQueryHandler : IRequestHandler<GetExecutionByIdQuery, WorkflowExecutionDto?>
{
    private readonly IWorkflowExecutionRepository _repository;
    private readonly IWorkflowRepository _workflowRepository;

    public GetExecutionByIdQueryHandler(IWorkflowExecutionRepository repository, IWorkflowRepository workflowRepository)
    {
        _repository = repository;
        _workflowRepository = workflowRepository;
    }

    public async Task<WorkflowExecutionDto?> Handle(GetExecutionByIdQuery request, CancellationToken cancellationToken)
    {
        var execution = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (execution is null || execution.TenantId != request.TenantId) return null;

        var workflow = await _workflowRepository.GetByIdAsync(execution.WorkflowId, cancellationToken);

        return new WorkflowExecutionDto(
            execution.Id,
            execution.WorkflowId,
            workflow?.Name ?? string.Empty,
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
            execution.Steps.Select(s => new ExecutionStepDto(
                s.Id, s.StepId, s.StepName, s.Status, s.Order,
                s.Output, s.ErrorMessage, s.StartedAt, s.CompletedAt)).ToList(),
            execution.Comments.Select(c => new ExecutionCommentDto(
                c.Id, c.ExecutionId, c.AuthorId, c.AuthorName, c.Content, c.CreatedAt)).ToList());
    }
}

public class ListExecutionsQueryHandler : IRequestHandler<ListExecutionsQuery, PagedResult<WorkflowExecutionDto>>
{
    private readonly IWorkflowExecutionRepository _repository;
    private readonly IWorkflowRepository _workflowRepository;

    public ListExecutionsQueryHandler(IWorkflowExecutionRepository repository, IWorkflowRepository workflowRepository)
    {
        _repository = repository;
        _workflowRepository = workflowRepository;
    }

    public async Task<PagedResult<WorkflowExecutionDto>> Handle(ListExecutionsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.TenantId, request.Page, request.PageSize,
            request.WorkflowId, request.Status, cancellationToken);

        var dtos = new List<WorkflowExecutionDto>();
        foreach (var execution in items)
        {
            var workflow = await _workflowRepository.GetByIdAsync(execution.WorkflowId, cancellationToken);
            dtos.Add(new WorkflowExecutionDto(
                execution.Id, execution.WorkflowId, workflow?.Name ?? string.Empty,
                execution.TemporalWorkflowId, execution.TemporalRunId, execution.Status,
                execution.TenantId, execution.TriggeredBy, execution.InputData, execution.OutputData,
                execution.ErrorMessage, execution.CurrentStepOrder, execution.CurrentStepName,
                execution.StartedAt, execution.CompletedAt, execution.SlaBreachedAt, [], []));
        }

        return new PagedResult<WorkflowExecutionDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}

public class GetDashboardStatsQueryHandler : IRequestHandler<GetDashboardStatsQuery, DashboardStatsDto>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IWorkflowExecutionRepository _executionRepository;

    public GetDashboardStatsQueryHandler(
        IWorkflowRepository workflowRepository,
        IWorkflowExecutionRepository executionRepository)
    {
        _workflowRepository = workflowRepository;
        _executionRepository = executionRepository;
    }

    public async Task<DashboardStatsDto> Handle(GetDashboardStatsQuery request, CancellationToken cancellationToken)
    {
        var (allWorkflows, totalWorkflows) = await _workflowRepository.GetPagedAsync(
            request.TenantId, 1, int.MaxValue, cancellationToken: cancellationToken);

        var activeWorkflows = allWorkflows.Count(w => w.Status == WorkflowStatus.Active);
        var running = await _executionRepository.CountByStatusAsync(request.TenantId, ExecutionStatus.Running, cancellationToken);
        var pending = await _executionRepository.CountByStatusAsync(request.TenantId, ExecutionStatus.WaitingApproval, cancellationToken);
        var failed = await _executionRepository.CountByStatusAsync(request.TenantId, ExecutionStatus.Failed, cancellationToken);
        var completedToday = await _executionRepository.CountCompletedTodayAsync(request.TenantId, cancellationToken);
        var (_, totalExecutions) = await _executionRepository.GetPagedAsync(request.TenantId, 1, 1, cancellationToken: cancellationToken);
        var trend = await _executionRepository.GetRecentTrendAsync(request.TenantId, 7, cancellationToken);

        return new DashboardStatsDto(
            totalWorkflows,
            activeWorkflows,
            totalExecutions,
            running,
            pending,
            failed,
            completedToday,
            trend.Select(t => new ExecutionTrendDto(t.Date, t.Completed, t.Failed)).ToList());
    }
}
