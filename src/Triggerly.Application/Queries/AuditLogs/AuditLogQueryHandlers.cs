using MediatR;
using Triggerly.Application.Common;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Queries.AuditLogs;

public class ListAuditLogsQueryHandler : IRequestHandler<ListAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private readonly IAuditLogRepository _repository;

    public ListAuditLogsQueryHandler(IAuditLogRepository repository) => _repository = repository;

    public async Task<PagedResult<AuditLogDto>> Handle(ListAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var (items, total) = await _repository.GetPagedAsync(
            request.TenantId, request.Page, request.PageSize,
            request.EntityType, request.Search, cancellationToken);

        var dtos = items.Select(l => new AuditLogDto(
            l.Id, l.TenantId, l.UserId, l.UserName,
            l.Action, l.EntityType, l.EntityId, l.EntityName,
            l.Details, l.Timestamp)).ToList();

        return new PagedResult<AuditLogDto>(dtos, total, request.Page, request.PageSize);
    }
}
