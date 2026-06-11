using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;
using Triggerly.Domain.Entities;

namespace Triggerly.Infrastructure.Persistence.Configurations;

public class WorkflowExecutionConfiguration : IEntityTypeConfiguration<WorkflowExecution>
{
    private static readonly JsonSerializerOptions _jsonOptions = new();

    private static readonly ValueComparer<Dictionary<string, object>> _dictComparer = new(
        (a, b) => JsonSerializer.Serialize(a, _jsonOptions) == JsonSerializer.Serialize(b, _jsonOptions),
        c => JsonSerializer.Serialize(c, _jsonOptions).GetHashCode(),
        c => JsonSerializer.Deserialize<Dictionary<string, object>>(JsonSerializer.Serialize(c, _jsonOptions), _jsonOptions) ?? new());

    public void Configure(EntityTypeBuilder<WorkflowExecution> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TemporalWorkflowId).IsRequired().HasMaxLength(500);
        builder.Property(e => e.TemporalRunId).HasMaxLength(500);
        builder.Property(e => e.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(e => e.TriggeredBy).HasMaxLength(200);
        builder.Property(e => e.ErrorMessage).HasMaxLength(4000);
        builder.Property(e => e.CurrentStepName).HasMaxLength(200);
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.WorkflowVersionId);
        builder.Property(e => e.WorkflowVersionNumber);

        builder.Property(e => e.InputData)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, _jsonOptions) ?? new(),
                _dictComparer);

        builder.Property(e => e.OutputData)
            .HasConversion(
                v => JsonSerializer.Serialize(v, _jsonOptions),
                v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, _jsonOptions) ?? new(),
                _dictComparer);

        builder.HasMany(e => e.Steps)
            .WithOne()
            .HasForeignKey(s => s.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Comments)
            .WithOne()
            .HasForeignKey(c => c.ExecutionId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!.SetPropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(e => e.ClientId);
        builder.Property(e => e.ClientServiceId);

        builder.HasIndex(e => e.TenantId);
        builder.HasIndex(e => new { e.TenantId, e.Status });
        builder.HasIndex(e => e.TemporalWorkflowId).IsUnique();
        builder.HasIndex(e => e.ClientId);

        builder.Ignore(e => e.Workflow);
    }
}

public class ExecutionCommentConfiguration : IEntityTypeConfiguration<ExecutionComment>
{
    public void Configure(EntityTypeBuilder<ExecutionComment> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(c => c.AuthorId).IsRequired().HasMaxLength(200);
        builder.Property(c => c.AuthorName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Content).IsRequired().HasMaxLength(4000);
        builder.HasIndex(c => c.ExecutionId);
    }
}

public class ExecutionStepConfiguration : IEntityTypeConfiguration<ExecutionStep>
{
    public void Configure(EntityTypeBuilder<ExecutionStep> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.StepName).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Output).HasMaxLength(4000);
        builder.Property(s => s.ErrorMessage).HasMaxLength(4000);
        builder.Property(s => s.Status).HasConversion<int>();
    }
}
