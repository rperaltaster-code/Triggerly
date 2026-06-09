using Triggerly.Application.Interfaces;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;

namespace Triggerly.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AuditService(IAuditLogRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task LogAsync(
        string tenantId, string userId, string userName,
        string action, string entityType, string entityId, string entityName,
        string? details = null,
        CancellationToken ct = default)
    {
        var log = AuditLog.Create(tenantId, userId, userName, action, entityType, entityId, entityName, details);
        await _repository.AddAsync(log, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }
}
