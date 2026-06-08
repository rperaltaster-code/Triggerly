using MediatR;
using Triggerly.Application.Common;
using Triggerly.Shared.DTOs;
using Triggerly.Shared.Models;

namespace Triggerly.Application.Queries.Executions;

public record GetExecutionByIdQuery(Guid Id, string TenantId) : IRequest<WorkflowExecutionDto?>;

public record ListExecutionsQuery(
    string TenantId,
    int Page = 1,
    int PageSize = 20,
    Guid? WorkflowId = null,
    ExecutionStatus? Status = null
) : IRequest<PagedResult<WorkflowExecutionDto>>;

public record GetDashboardStatsQuery(string TenantId) : IRequest<DashboardStatsDto>;
