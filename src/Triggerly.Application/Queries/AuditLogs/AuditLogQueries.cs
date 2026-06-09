using MediatR;
using Triggerly.Application.Common;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Queries.AuditLogs;

public record ListAuditLogsQuery(
    string TenantId,
    int Page = 1,
    int PageSize = 50,
    string? EntityType = null,
    string? Search = null
) : IRequest<PagedResult<AuditLogDto>>;
