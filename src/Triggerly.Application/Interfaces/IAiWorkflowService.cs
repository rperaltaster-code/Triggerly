namespace Triggerly.Application.Interfaces;

public interface IAiWorkflowService
{
    Task<List<AiGeneratedStep>> GenerateStepsAsync(string prompt, CancellationToken ct = default);
}

public class AiGeneratedStep
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Order { get; set; }
    public Dictionary<string, object> Config { get; set; } = [];
    public string? ApproverEmail { get; set; }
}
