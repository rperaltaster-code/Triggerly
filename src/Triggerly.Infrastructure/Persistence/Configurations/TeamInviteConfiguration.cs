using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triggerly.Domain.Entities;

namespace Triggerly.Infrastructure.Persistence.Configurations;

public class TeamInviteConfiguration : IEntityTypeConfiguration<TeamInvite>
{
    public void Configure(EntityTypeBuilder<TeamInvite> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(i => i.Email).IsRequired().HasMaxLength(256);
        builder.Property(i => i.Token).IsRequired().HasMaxLength(64);
        builder.Property(i => i.Role).HasConversion<int>();
        builder.HasIndex(i => i.Token).IsUnique();
        builder.HasIndex(i => i.TenantId);
    }
}
