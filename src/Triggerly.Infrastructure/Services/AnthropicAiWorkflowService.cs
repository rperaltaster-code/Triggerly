using System.Text.Json;
using Anthropic;
using Anthropic.Models.Messages;
using Triggerly.Application.Interfaces;

namespace Triggerly.Infrastructure.Services;

public class AnthropicAiWorkflowService : IAiWorkflowService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    private static readonly Dictionary<string, JsonElement> Schema = BuildSchema();

    private const string SystemPrompt = """
        You are a workflow designer assistant for an accounting firm automation platform called Triggerly.
        Generate structured workflow step definitions based on the user's plain-English description.

        Available step types and their typical config fields:
        - Action: { "description": "what to do" }
        - Approval: { "message": "approval request message", "slaDays": 3 }
        - Condition: { "field": "fieldName", "operator": "equals|notEquals|greaterThan|lessThan", "value": "expected" }
        - Delay: { "days": 1 }
        - Notification: { "to": "recipient@example.com", "subject": "Subject", "body": "Message body" }
        - DataTransform: { "field": "fieldName", "transform": "transformation description" }
        - Webhook: { "url": "https://example.com/hook", "method": "POST", "body": "{}" }

        Use token placeholders like {{input.fieldName}}, {{client.name}}, {{client.email}} in config values where appropriate.
        Order steps starting from 1. Suggest a concise workflow name.
        Return JSON that exactly matches the required schema.
        """;

    private readonly AnthropicClient _client;

    public AnthropicAiWorkflowService(AnthropicClient client) => _client = client;

    public async Task<AiWorkflowSuggestion> GenerateWorkflowAsync(string prompt, CancellationToken ct = default)
    {
        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = Model.ClaudeSonnet4_6,
            MaxTokens = 2048,
            System = SystemPrompt,
            Messages = [new() { Role = Role.User, Content = prompt }],
            OutputConfig = new OutputConfig
            {
                Format = new JsonOutputFormat { Schema = Schema },
            },
        });

        var text = response.Content.Select(b => b.Value).OfType<TextBlock>().FirstOrDefault()?.Text ?? "{}";
        var result = JsonSerializer.Deserialize<AiWorkflowResult>(text, JsonOpts);

        var steps = result?.Steps?.Select((s, i) => new AiGeneratedStep(
            s.Name ?? $"Step {i + 1}",
            s.Type ?? "Action",
            s.Order > 0 ? s.Order : i + 1,
            ConvertConfig(s.Config),
            s.ApproverEmail
        )).ToList() ?? [];

        return new AiWorkflowSuggestion(result?.SuggestedName, steps);
    }

    private static Dictionary<string, object> ConvertConfig(Dictionary<string, JsonElement>? raw)
    {
        if (raw is null) return [];
        var result = new Dictionary<string, object>(raw.Count);
        foreach (var (k, v) in raw)
            result[k] = v.ValueKind == JsonValueKind.String ? v.GetString()! : (object)v;
        return result;
    }

    private static Dictionary<string, JsonElement> BuildSchema() => new()
    {
        ["type"] = JsonSerializer.SerializeToElement("object"),
        ["properties"] = JsonSerializer.SerializeToElement(new
        {
            suggestedName = new { type = "string", description = "A short, descriptive workflow name" },
            steps = new
            {
                type = "array",
                items = new
                {
                    type = "object",
                    properties = new
                    {
                        name = new { type = "string" },
                        type = new
                        {
                            type = "string",
                            @enum = new[] { "Action", "Approval", "Condition", "Delay", "Notification", "DataTransform", "Webhook" }
                        },
                        order = new { type = "integer", minimum = 1 },
                        config = new { type = "object", additionalProperties = true },
                        approverEmail = new { type = "string" },
                    },
                    required = new[] { "name", "type", "order", "config" },
                },
            },
        }),
        ["required"] = JsonSerializer.SerializeToElement(new[] { "suggestedName", "steps" }),
    };

    private record AiWorkflowResult(string? SuggestedName, List<AiStepResult>? Steps);

    private record AiStepResult(
        string? Name,
        string? Type,
        int Order,
        Dictionary<string, JsonElement>? Config,
        string? ApproverEmail);
}
