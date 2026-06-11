using Triggerly.Domain.Entities;

namespace Triggerly.Domain.Interfaces;

public interface IEmailTemplateRepository
{
    Task<EmailTemplate?> GetAsync(string tenantId, string templateKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EmailTemplate>> GetAllForTenantAsync(string tenantId, CancellationToken cancellationToken = default);
    Task AddAsync(EmailTemplate template, CancellationToken cancellationToken = default);
    void Remove(EmailTemplate template);
}
