using MediatR;
using Triggerly.Domain.Interfaces;
using Triggerly.Shared.DTOs;

namespace Triggerly.Application.Commands.Executions;

public record AddCommentCommand(
    Guid ExecutionId,
    string TenantId,
    string AuthorId,
    string AuthorName,
    string Content
) : IRequest<ExecutionCommentDto>;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, ExecutionCommentDto>
{
    private readonly IWorkflowExecutionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AddCommentCommandHandler(IWorkflowExecutionRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ExecutionCommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var execution = await _repository.GetByIdAsync(request.ExecutionId, cancellationToken)
            ?? throw new KeyNotFoundException($"Execution {request.ExecutionId} not found.");

        if (execution.TenantId != request.TenantId)
            throw new UnauthorizedAccessException("Access denied.");

        var comment = execution.AddComment(request.AuthorId, request.AuthorName, request.Content);
        await _repository.UpdateAsync(execution, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExecutionCommentDto(
            comment.Id, comment.ExecutionId, comment.AuthorId,
            comment.AuthorName, comment.Content, comment.CreatedAt);
    }
}
