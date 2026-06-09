using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Triggerly.Domain.Entities;

namespace Triggerly.Infrastructure.Persistence.Configurations;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.HasKey(w => w.Id);
        builder.Property(w => w.Name).IsRequired().HasMaxLength(200);
        builder.Property(w => w.Description).HasMaxLength(1000);
        builder.Property(w => w.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(w => w.CreatedBy).HasMaxLength(200);
        builder.Property(w => w.Status).HasConversion<int>();

        builder.HasMany(w => w.Steps)
            .WithOne()
            .HasForeignKey(s => s.WorkflowId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(w => w.TenantId);
        builder.HasIndex(w => new { w.TenantId, w.Status });

        builder.Ignore(w => w.DomainEvents);
    }
}

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    private static readonly JsonSerializerOptions _jsonOptions = new();

    private static readonly ValueComparer<Dictionary<string, object>> _dictComparer = new(
        (a, b) => JsonSerializer.Serialize(a, _jsonOptions) == JsonSerializer.Serialize(b, _jsonOptions),
        c => JsonSerializer.Serialize(c, _jsonOptions).GetHashCode(),
        c => JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(c, _jsonOptions), _jsonOptions) ?? new());

    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Type).HasConversion<int>();
        builder.Property(s => s.ApproverEmail).HasMaxLength(200);

        builder.Property(s => s.Config)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, _jsonOptions) ?? new(),
                _dictComparer);
    }
}
