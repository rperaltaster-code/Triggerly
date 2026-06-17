namespace Triggerly.Application.Interfaces;

public interface IAiWorkflowService
{
    Task<AiWorkflowSuggestion> GenerateWorkflowAsync(string prompt, CancellationToken ct = default);
}

public record AiWorkflowSuggestion(string? SuggestedName, List<AiGeneratedStep> Steps);

public record AiGeneratedStep(
    string Name,
    string Type,
    int Order,
    Dictionary<string, object> Config,
    string? ApproverEmail);
