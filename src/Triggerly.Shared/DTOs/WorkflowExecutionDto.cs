using Triggerly.Shared.Models;

namespace Triggerly.Shared.DTOs;

public record WorkflowExecutionDto(
    Guid Id,
    Guid WorkflowId,
    string WorkflowName,
    string TemporalWorkflowId,
    string TemporalRunId,
    ExecutionStatus Status,
    string TenantId,
    string? TriggeredBy,
    Dictionary<string, object> InputData,
    Dictionary<string, object> OutputData,
    string? ErrorMessage,
    int CurrentStepOrder,
    string? CurrentStepName,
    DateTime StartedAt,
    DateTime? CompletedAt,
    DateTime? SlaBreachedAt,
    List<ExecutionStepDto> Steps,
    List<ExecutionCommentDto> Comments,
    int WorkflowVersionNumber,
    Guid? ClientId = null,
    Guid? ClientServiceId = null,
    string? ClientName = null,
    string? ServiceTypeName = null
);

public record WorkflowVersionDto(
    Guid Id,
    int VersionNumber,
    DateTime CreatedAt,
    string CreatedBy
);

public record ExecutionStepDto(
    Guid Id,
    Guid StepId,
    string StepName,
    ExecutionStatus Status,
    int Order,
    string? Output,
    string? ErrorMessage,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

public record ExecutionCommentDto(
    Guid Id,
    Guid ExecutionId,
    string AuthorId,
    string AuthorName,
    string Content,
    DateTime CreatedAt
);

public record DashboardStatsDto(
    int TotalWorkflows,
    int ActiveWorkflows,
    int TotalExecutions,
    int RunningExecutions,
    int PendingApprovals,
    int FailedExecutions,
    int CompletedToday,
    List<ExecutionTrendDto> RecentTrend
);

public record ExecutionTrendDto(
    string Date,
    int Completed,
    int Failed
);
