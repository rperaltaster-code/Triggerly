using MediatR;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Queries.Workflows;

public record GetWorkflowVersionsQuery(Guid WorkflowId, string TenantId) : IRequest<List<WorkflowVersionDto>>;

public class GetWorkflowVersionsQueryHandler : IRequestHandler<GetWorkflowVersionsQuery, List<WorkflowVersionDto>>
{
    private readonly IWorkflowVersionRepository _versionRepository;
    private readonly IWorkflowRepository _workflowRepository;

    public GetWorkflowVersionsQueryHandler(
        IWorkflowVersionRepository versionRepository,
        IWorkflowRepository workflowRepository)
    {
        _versionRepository = versionRepository;
        _workflowRepository = workflowRepository;
    }

    public async Task<List<WorkflowVersionDto>> Handle(GetWorkflowVersionsQuery request, CancellationToken cancellationToken)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        if (workflow is null || workflow.TenantId != request.TenantId)
            return [];

        var versions = await _versionRepository.GetByWorkflowAsync(request.WorkflowId, cancellationToken);
        return versions.Select(v => new WorkflowVersionDto(v.Id, v.VersionNumber, v.CreatedAt, v.CreatedBy)).ToList();
    }
}
