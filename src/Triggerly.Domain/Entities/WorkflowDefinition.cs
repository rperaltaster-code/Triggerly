using Triggerly.Shared.Models;
using Triggerly.Domain.Events;

namespace Triggerly.Domain.Entities;

public class WorkflowDefinition
{
    private readonly List<WorkflowStep> _steps = [];
    private readonly List<object> _domainEvents = [];

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public WorkflowStatus Status { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public int Version { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    public IReadOnlyList<WorkflowStep> Steps => _steps.AsReadOnly();
    public IReadOnlyList<object> DomainEvents => _domainEvents.AsReadOnly();

    private WorkflowDefinition() { }

    public static WorkflowDefinition Create(string name, string description, string tenantId, string createdBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);

        var workflow = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            TenantId = tenantId,
            Status = WorkflowStatus.Draft,
            Version = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        workflow._domainEvents.Add(new WorkflowCreatedDomainEvent(workflow.Id, tenantId));
        return workflow;
    }

    public void Update(string name, string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (Status == WorkflowStatus.Archived)
            throw new InvalidOperationException("Cannot update an archived workflow.");

        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        if (_steps.Count == 0)
            throw new InvalidOperationException("Cannot activate a workflow with no steps.");

        if (Status == WorkflowStatus.Archived)
            throw new InvalidOperationException("Cannot activate an archived workflow.");

        Status = WorkflowStatus.Active;
        Version++;
        UpdatedAt = DateTime.UtcNow;
        _domainEvents.Add(new WorkflowActivatedDomainEvent(Id, TenantId));
    }

    public void Deactivate()
    {
        if (Status != WorkflowStatus.Active)
            throw new InvalidOperationException("Only active workflows can be deactivated.");

        Status = WorkflowStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        Status = WorkflowStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
    }

    public WorkflowStep AddStep(string name, StepType type, int order, Dictionary<string, object> config)
    {
        if (Status == WorkflowStatus.Archived)
            throw new InvalidOperationException("Cannot add steps to an archived workflow.");

        var step = WorkflowStep.Create(Id, name, type, order, config);
        _steps.Add(step);
        UpdatedAt = DateTime.UtcNow;
        return step;
    }

    public void RemoveStep(Guid stepId)
    {
        var step = _steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new InvalidOperationException($"Step {stepId} not found.");

        _steps.Remove(step);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearSteps()
    {
        _steps.Clear();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ClearDomainEvents() => _domainEvents.Clear();
}
