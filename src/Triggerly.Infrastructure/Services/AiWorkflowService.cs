using System.Text.Json;
using System.Text.Json.Serialization;
using Anthropic;
using Anthropic.Models.Messages;
using Microsoft.Extensions.Configuration;
using Triggerly.Application.Interfaces;

namespace Triggerly.Infrastructure.Services;

public class AiWorkflowService : IAiWorkflowService
{
    private readonly AnthropicClient _client;

    private const string SystemPrompt = """
        You are a workflow automation designer for accounting firms.
        Given a plain-English description, generate an ordered sequence of workflow steps.
        Respond ONLY with valid JSON (no markdown fences, no explanation) in this exact format:
        {
          "steps": [
            {
              "name": "string (short, descriptive step name)",
              "type": "Action|Approval|Condition|Delay|Notification|DataTransform|Webhook",
              "order": 1,
              "config": {},
              "approverEmail": null
            }
          ]
        }
        Step types and their config shapes:
        - Action: { "description": "what to do" }
        - Approval: { "message": "instructions to approver", "slaHours": 48 }  (set approverEmail to reviewer email if known)
        - Condition: { "expression": "{{input.field}} == 'value'", "trueBranchLabel": "Yes", "falseBranchLabel": "No" }
        - Delay: { "delayHours": 24 }
        - Notification: { "to": "{{client.email}}", "subject": "subject line", "body": "email body text" }
        - DataTransform: { "expression": "transform expression" }
        - Webhook: { "url": "https://example.com/hook", "method": "POST", "headers": {}, "body": "" }
        Use {{input.fieldId}}, {{client.name}}, {{client.email}}, {{service.name}} tokens where relevant.
        Order starts at 1 and increments by 1. Generate between 2 and 8 steps.
        """;

    public AiWorkflowService(IConfiguration configuration)
    {
        var apiKey = configuration["Anthropic:ApiKey"]
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY");
        _client = new AnthropicClient { ApiKey = apiKey };
    }

    public async Task<List<AiGeneratedStep>> GenerateStepsAsync(string prompt, CancellationToken ct = default)
    {
        var response = await _client.Messages.Create(new MessageCreateParams
        {
            Model = "claude-sonnet-4-6",
            MaxTokens = 4096,
            System = SystemPrompt,
            Messages = [new() { Role = Role.User, Content = prompt }],
        }, ct);

        var text = response.Content
            .Select(b => b.Value)
            .OfType<TextBlock>()
            .FirstOrDefault()?.Text
            ?? throw new InvalidOperationException("No text content in AI response.");

        var result = JsonSerializer.Deserialize<GeneratedWorkflowResponse>(text,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("Failed to parse AI response as workflow steps.");

        return result.Steps;
    }
}

internal class GeneratedWorkflowResponse
{
    [JsonPropertyName("steps")]
    public List<AiGeneratedStep> Steps { get; set; } = [];
}
