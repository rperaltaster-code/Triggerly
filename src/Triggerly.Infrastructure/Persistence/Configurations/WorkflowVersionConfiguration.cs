using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triggerly.Domain.Entities;

namespace Triggerly.Infrastructure.Persistence.Configurations;

public class WorkflowVersionConfiguration : IEntityTypeConfiguration<WorkflowVersion>
{
    public void Configure(EntityTypeBuilder<WorkflowVersion> builder)
    {
        builder.HasKey(v => v.Id);
        builder.Property(v => v.CreatedBy).HasMaxLength(200);
        builder.HasIndex(v => v.WorkflowId);
        builder.HasIndex(v => new { v.WorkflowId, v.VersionNumber }).IsUnique();
    }
}
