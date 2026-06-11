using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Triggerly.Domain.Entities;

namespace Triggerly.Infrastructure.Persistence.Configurations;

public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Email).IsRequired().HasMaxLength(256);
        builder.Property(c => c.Phone).HasMaxLength(50);
        builder.Property(c => c.BalanceDate).HasMaxLength(50);
        builder.Property(c => c.IrdNumber).HasMaxLength(50);
        builder.Property(c => c.Notes).HasMaxLength(2000);
        builder.Property(c => c.ExternalId).HasMaxLength(200);
        builder.Property(c => c.Source).HasConversion<int>();
        builder.HasIndex(c => c.TenantId);
        builder.HasIndex(c => new { c.TenantId, c.Email });

        builder.HasMany(c => c.Services)
            .WithOne()
            .HasForeignKey(s => s.ClientId)
            .OnDelete(DeleteBehavior.Cascade)
            .Metadata.PrincipalToDependent!.SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}

public class ServiceTypeConfiguration : IEntityTypeConfiguration<ServiceType>
{
    public void Configure(EntityTypeBuilder<ServiceType> builder)
    {
        builder.HasKey(st => st.Id);
        builder.Property(st => st.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(st => st.Name).IsRequired().HasMaxLength(200);
        builder.Property(st => st.Description).HasMaxLength(500);
        builder.Property(st => st.Color).HasMaxLength(20);
        builder.Property(st => st.DefaultFilingPeriod).HasConversion<int?>();
        builder.HasIndex(st => st.TenantId);
    }
}

public class ClientServiceConfiguration : IEntityTypeConfiguration<ClientService>
{
    public void Configure(EntityTypeBuilder<ClientService> builder)
    {
        builder.HasKey(cs => cs.Id);
        builder.Property(cs => cs.FilingPeriod).HasConversion<int>();
        builder.Property(cs => cs.Notes).HasMaxLength(500);
        builder.HasIndex(cs => cs.ClientId);
        builder.HasIndex(cs => cs.ServiceTypeId);
    }
}

public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
{
    public void Configure(EntityTypeBuilder<TenantSettings> builder)
    {
        builder.HasKey(ts => ts.Id);
        builder.Property(ts => ts.TenantId).IsRequired().HasMaxLength(100);
        builder.Property(ts => ts.ClientDataSource).HasConversion<int>();
        builder.HasIndex(ts => ts.TenantId).IsUnique();
    }
}
