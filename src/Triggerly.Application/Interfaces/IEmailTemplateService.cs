namespace Triggerly.Application.Interfaces;

public interface IEmailTemplateService
{
    Task<(string Subject, string Body)> GetRenderedAsync(
        string tenantId,
        string templateKey,
        Dictionary<string, string> tokens,
        CancellationToken cancellationToken = default);
}
