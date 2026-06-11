using Triggerly.Shared.Models;

namespace Triggerly.Shared.DTOs;

public record ClientDto(
    Guid Id,
    string TenantId,
    string Name,
    string Email,
    string? Phone,
    string? BalanceDate,
    string? IrdNumber,
    string? Notes,
    string? ExternalId,
    ClientSource Source,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ClientSummaryDto(
    Guid Id,
    string Name,
    string Email,
    string? Phone,
    int ServiceCount,
    DateTime UpdatedAt
);

public record ServiceTypeDto(
    Guid Id,
    string TenantId,
    string Name,
    string? Description,
    Guid? DefaultWorkflowId,
    FilingPeriod? DefaultFilingPeriod,
    string? Color,
    DateTime CreatedAt
);

public record ClientServiceDto(
    Guid Id,
    Guid ClientId,
    Guid ServiceTypeId,
    string ServiceTypeName,
    Guid WorkflowId,
    string? WorkflowName,
    FilingPeriod FilingPeriod,
    DateTime? LastFiledAt,
    DateTime? NextDueAt,
    bool IsActive,
    string? Notes
);

public record SaveClientRequest(
    string Name,
    string Email,
    string? Phone,
    string? BalanceDate,
    string? IrdNumber,
    string? Notes
);

public record SaveServiceTypeRequest(
    string Name,
    string? Description,
    Guid? DefaultWorkflowId,
    FilingPeriod? DefaultFilingPeriod,
    string? Color
);

public record SaveClientServiceRequest(
    Guid ServiceTypeId,
    Guid WorkflowId,
    FilingPeriod FilingPeriod,
    bool IsActive,
    string? Notes
);
