using Microsoft.EntityFrameworkCore;
using Triggerly.Domain.Entities;
using Triggerly.Domain.Interfaces;
using Triggerly.Infrastructure.Persistence;

namespace Triggerly.Infrastructure.Repositories;

public class EmailTemplateRepository : IEmailTemplateRepository
{
    private readonly AppDbContext _context;

    public EmailTemplateRepository(AppDbContext context) => _context = context;

    public Task<EmailTemplate?> GetAsync(string tenantId, string templateKey, CancellationToken cancellationToken = default) =>
        _context.EmailTemplates.FirstOrDefaultAsync(
            t => t.TenantId == tenantId && t.TemplateKey == templateKey, cancellationToken);

    public async Task<IReadOnlyList<EmailTemplate>> GetAllForTenantAsync(string tenantId, CancellationToken cancellationToken = default) =>
        await _context.EmailTemplates
            .Where(t => t.TenantId == tenantId)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(EmailTemplate template, CancellationToken cancellationToken = default) =>
        await _context.EmailTemplates.AddAsync(template, cancellationToken);

    public void Remove(EmailTemplate template) => _context.EmailTemplates.Remove(template);
}
