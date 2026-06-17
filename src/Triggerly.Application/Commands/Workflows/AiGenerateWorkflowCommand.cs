using MediatR;
using Triggerly.Application.Interfaces;

namespace Triggerly.Application.Commands.Workflows;

public record AiGenerateWorkflowCommand(string Prompt, string TenantId) : IRequest<AiWorkflowSuggestion>;

public class AiGenerateWorkflowCommandHandler : IRequestHandler<AiGenerateWorkflowCommand, AiWorkflowSuggestion>
{
    private readonly IAiWorkflowService _aiService;

    public AiGenerateWorkflowCommandHandler(IAiWorkflowService aiService) => _aiService = aiService;

    public Task<AiWorkflowSuggestion> Handle(AiGenerateWorkflowCommand request, CancellationToken ct)
        => _aiService.GenerateWorkflowAsync(request.Prompt, ct);
}
