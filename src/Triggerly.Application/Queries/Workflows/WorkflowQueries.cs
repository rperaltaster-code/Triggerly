using MediatR;
using Triggerly.Application.Common;
using Triggerly.Shared.DTOs;
using Triggerly.Shared.Models;

namespace Triggerly.Application.Queries.Workflows;

public record GetWorkflowByIdQuery(Guid Id, string TenantId) : IRequest<WorkflowDto?>;

public record ListWorkflowsQuery(
    string TenantId,
    int Page = 1,
    int PageSize = 20,
    WorkflowStatus? Status = null,
    string? Search = null
) : IRequest<PagedResult<WorkflowSummaryDto>>;
