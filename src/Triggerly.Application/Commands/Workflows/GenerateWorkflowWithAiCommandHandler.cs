using MediatR;
using Triggerly.Application.Interfaces;

namespace Triggerly.Application.Commands.Workflows;

public class GenerateWorkflowWithAiCommandHandler : IRequestHandler<GenerateWorkflowWithAiCommand, List<AiGeneratedStep>>
{
    private readonly IAiWorkflowService _ai;

    public GenerateWorkflowWithAiCommandHandler(IAiWorkflowService ai) => _ai = ai;

    public Task<List<AiGeneratedStep>> Handle(GenerateWorkflowWithAiCommand request, CancellationToken cancellationToken)
        => _ai.GenerateStepsAsync(request.Prompt, cancellationToken);
}
