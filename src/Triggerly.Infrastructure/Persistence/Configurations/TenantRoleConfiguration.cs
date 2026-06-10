using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triggerly.Domain.Entities;

namespace Triggerly.Infrastructure.Persistence.Configurations;

public class TenantRoleConfiguration : IEntityTypeConfiguration<TenantRole>
{
    public void Configure(EntityTypeBuilder<TenantRole> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(r => r.Role).HasConversion<int>();
        builder.HasIndex(r => new { r.UserId, r.TenantId }).IsUnique();
        builder.HasIndex(r => r.TenantId);
    }
}
