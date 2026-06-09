using MediatR;
using Triggerly.Domain.Entities;
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
        var exists = await _repository.ExistsAsync(request.ExecutionId, request.TenantId, cancellationToken);
        if (!exists)
            throw new KeyNotFoundException($"Execution {request.ExecutionId} not found.");

        var comment = ExecutionComment.Create(
            request.ExecutionId, request.TenantId,
            request.AuthorId, request.AuthorName, request.Content);

        await _repository.AddCommentAsync(comment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ExecutionCommentDto(
            comment.Id, comment.ExecutionId, comment.AuthorId,
            comment.AuthorName, comment.Content, comment.CreatedAt);
    }
}
