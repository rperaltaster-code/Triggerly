using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triggerly.Domain.Entities;

namespace Triggerly.Infrastructure.Persistence.Configurations;

public class AutomationRuleConfiguration : IEntityTypeConfiguration<AutomationRule>
{
    public void Configure(EntityTypeBuilder<AutomationRule> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Name).IsRequired().HasMaxLength(200);
        builder.Property(r => r.Description).HasMaxLength(1000);
        builder.Property(r => r.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(r => r.TriggerConfig).HasMaxLength(4000);
        builder.Property(r => r.TriggerType).HasConversion<int>();

        builder.HasIndex(r => r.TenantId);
        builder.HasIndex(r => r.WorkflowId);
        builder.HasIndex(r => r.WebhookToken).IsUnique();

        builder.Ignore(r => r.Workflow);
    }
}
