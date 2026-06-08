namespace Triggerly.Domain.Entities;

public class ExecutionComment
{
    public Guid Id { get; private set; }
    public Guid ExecutionId { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string AuthorId { get; private set; } = string.Empty;
    public string AuthorName { get; private set; } = string.Empty;
    public string Content { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }

    private ExecutionComment() { }

    public static ExecutionComment Create(
        Guid executionId, string tenantId, string authorId, string authorName, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException("Comment content cannot be empty.", nameof(content));

        return new ExecutionComment
        {
            Id = Guid.NewGuid(),
            ExecutionId = executionId,
            TenantId = tenantId,
            AuthorId = authorId,
            AuthorName = authorName,
            Content = content.Trim(),
            CreatedAt = DateTime.UtcNow
        };
    }
}
