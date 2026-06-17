using MediatR;
using Triggerly.Application.Interfaces;

namespace Triggerly.Application.Commands.Workflows;

public record GenerateWorkflowWithAiCommand(string Prompt) : IRequest<List<AiGeneratedStep>>;
