namespace Triggerly.Domain.Events;

public record WorkflowCreatedDomainEvent(Guid WorkflowId, string TenantId);
public record WorkflowActivatedDomainEvent(Guid WorkflowId, string TenantId);
public record WorkflowDeactivatedDomainEvent(Guid WorkflowId, string TenantId);
