namespace Triggerly.Domain.Entities;

public class EmailTemplate
{
    public Guid Id { get; private set; }
    public string TenantId { get; private set; } = string.Empty;
    public string TemplateKey { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public DateTime UpdatedAt { get; private set; }

    private EmailTemplate() { }

    public static EmailTemplate Create(string tenantId, string templateKey, string subject, string body) =>
        new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            TemplateKey = templateKey,
            Subject = subject,
            Body = body,
            UpdatedAt = DateTime.UtcNow,
        };

    public void Update(string subject, string body)
    {
        Subject = subject;
        Body = body;
        UpdatedAt = DateTime.UtcNow;
    }
}
