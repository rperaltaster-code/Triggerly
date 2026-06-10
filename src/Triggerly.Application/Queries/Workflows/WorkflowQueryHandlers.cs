using MediatR;
using Triggerly.Application.Common;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Queries.Workflows;

public class GetWorkflowByIdQueryHandler : IRequestHandler<GetWorkflowByIdQuery, WorkflowDto?>
{
    private readonly IWorkflowRepository _repository;
    private readonly IWorkflowExecutionRepository _executionRepository;

    public GetWorkflowByIdQueryHandler(IWorkflowRepository repository, IWorkflowExecutionRepository executionRepository)
    {
        _repository = repository;
        _executionRepository = executionRepository;
    }

    public async Task<WorkflowDto?> Handle(GetWorkflowByIdQuery request, CancellationToken cancellationToken)
    {
        var workflow = await _repository.GetByIdWithStepsAsync(request.Id, cancellationToken);
        if (workflow is null || workflow.TenantId != request.TenantId) return null;

        return new WorkflowDto(
            workflow.Id, workflow.Name, workflow.Description, workflow.Status,
            workflow.TenantId, workflow.Version,
            workflow.Steps.OrderBy(s => s.Order)
                .Select(s => new WorkflowStepDto(s.Id, s.Name, s.Type, s.Order, s.Config, s.NextStepId))
                .ToList(),
            workflow.CreatedAt, workflow.UpdatedAt, workflow.FormSchema);
    }
}

public class ListWorkflowsQueryHandler : IRequestHandler<ListWorkflowsQuery, PagedResult<WorkflowSummaryDto>>
{
    private readonly IWorkflowRepository _repository;
    private readonly IWorkflowExecutionRepository _executionRepository;

    public ListWorkflowsQueryHandler(IWorkflowRepository repository, IWorkflowExecutionRepository executionRepository)
    {
        _repository = repository;
        _executionRepository = executionRepository;
    }

    public async Task<PagedResult<WorkflowSummaryDto>> Handle(ListWorkflowsQuery request, CancellationToken cancellationToken)
    {
        var (items, totalCount) = await _repository.GetPagedAsync(
            request.TenantId, request.Page, request.PageSize,
            request.Status, request.Search, cancellationToken);

        var summaries = new List<WorkflowSummaryDto>();
        foreach (var w in items)
        {
            var count = await _executionRepository.CountByStatusAsync(
                request.TenantId, Shared.Models.ExecutionStatus.Completed, cancellationToken);
            summaries.Add(new WorkflowSummaryDto(
                w.Id, w.Name, w.Status, w.Version, w.Steps.Count, count, w.UpdatedAt, w.FormSchema.Count > 0));
        }

        return new PagedResult<WorkflowSummaryDto>(summaries, totalCount, request.Page, request.PageSize);
    }
}
