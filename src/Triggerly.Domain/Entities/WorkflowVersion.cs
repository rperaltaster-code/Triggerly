namespace Triggerly.Domain.Entities;

public class WorkflowVersion
{
    public Guid Id { get; private set; }
    public Guid WorkflowId { get; private set; }
    public int VersionNumber { get; private set; }
    public string StepsJson { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = string.Empty;

    private WorkflowVersion() { }

    public static WorkflowVersion Create(Guid workflowId, int versionNumber, string stepsJson, string createdBy) =>
        new()
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            VersionNumber = versionNumber,
            StepsJson = stepsJson,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy,
        };
}
