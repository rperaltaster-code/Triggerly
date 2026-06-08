using Triggerly.Shared.Models;

namespace Triggerly.Shared.DTOs;

public record WorkflowDto(
    Guid Id,
    string Name,
    string Description,
    WorkflowStatus Status,
    string TenantId,
    int Version,
    List<WorkflowStepDto> Steps,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record WorkflowStepDto(
    Guid Id,
    string Name,
    StepType Type,
    int Order,
    Dictionary<string, object> Config,
    Guid? NextStepId
);

public record WorkflowSummaryDto(
    Guid Id,
    string Name,
    WorkflowStatus Status,
    int Version,
    int StepCount,
    int ExecutionCount,
    DateTime UpdatedAt
);
