using Triggerly.Shared.Models;

namespace Triggerly.Domain.Entities;

public class WorkflowStep
{
    public Guid Id { get; private set; }
    public Guid WorkflowId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public StepType Type { get; private set; }
    public int Order { get; private set; }
    public Dictionary<string, object> Config { get; private set; } = [];
    public Guid? NextStepId { get; private set; }
    public string? ApproverEmail { get; private set; }

    private WorkflowStep() { }

    public static WorkflowStep Create(Guid workflowId, string name, StepType type, int order, Dictionary<string, object> config)
    {
        return new WorkflowStep
        {
            Id = Guid.NewGuid(),
            WorkflowId = workflowId,
            Name = name,
            Type = type,
            Order = order,
            Config = config ?? []
        };
    }

    public void SetNextStep(Guid? nextStepId) => NextStepId = nextStepId;

    public void SetApprover(string? email) => ApproverEmail = email;

    public void UpdateConfig(Dictionary<string, object> config)
    {
        Config = config ?? [];
    }
}
