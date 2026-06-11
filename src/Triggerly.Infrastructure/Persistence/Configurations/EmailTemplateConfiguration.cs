using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triggerly.Domain.Entities;

namespace Triggerly.Infrastructure.Persistence.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.HasKey(t => t.Id);
        builder.Property(t => t.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(t => t.TemplateKey).IsRequired().HasMaxLength(50);
        builder.Property(t => t.Subject).IsRequired().HasMaxLength(500);
        builder.Property(t => t.Body).IsRequired();
        builder.HasIndex(t => new { t.TenantId, t.TemplateKey }).IsUnique();
    }
}
