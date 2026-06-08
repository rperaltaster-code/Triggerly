using Triggerly.Shared.Models;

namespace Triggerly.Shared.Events;

public record WorkflowStartedEvent(
    Guid ExecutionId,
    Guid WorkflowId,
    string TenantId,
    string? TriggeredBy,
    DateTime StartedAt
);

public record WorkflowCompletedEvent(
    Guid ExecutionId,
    Guid WorkflowId,
    string TenantId,
    DateTime CompletedAt
);

public record WorkflowFailedEvent(
    Guid ExecutionId,
    Guid WorkflowId,
    string TenantId,
    string ErrorMessage,
    DateTime FailedAt
);

public record WorkflowApprovalRequestedEvent(
    Guid ExecutionId,
    Guid WorkflowId,
    Guid StepId,
    string StepName,
    string TenantId,
    string? ApproverEmail
);

public record WorkflowStepApprovedEvent(
    Guid ExecutionId,
    Guid StepId,
    string ApprovedBy,
    DateTime ApprovedAt
);

public record WorkflowStepRejectedEvent(
    Guid ExecutionId,
    Guid StepId,
    string RejectedBy,
    string Reason,
    DateTime RejectedAt
);
