using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triggerly.Domain.Entities;

namespace Triggerly.Infrastructure.Persistence.Configurations;

public class TenantSsoConfigConfiguration : IEntityTypeConfiguration<TenantSsoConfig>
{
    public void Configure(EntityTypeBuilder<TenantSsoConfig> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Provider).IsRequired().HasMaxLength(50);
        builder.Property(c => c.ClientId).IsRequired().HasMaxLength(500);
        builder.Property(c => c.ClientSecret).IsRequired().HasMaxLength(1000);
        builder.Property(c => c.DirectoryTenantId).IsRequired().HasMaxLength(200);
        builder.Property(c => c.GroupClaimName).HasMaxLength(100);
        builder.Property(c => c.GroupRoleMappings).HasMaxLength(4000);
        builder.HasIndex(c => c.TenantId).IsUnique();
        // Authority is computed — not persisted
        builder.Ignore(c => c.Authority);
    }
}
