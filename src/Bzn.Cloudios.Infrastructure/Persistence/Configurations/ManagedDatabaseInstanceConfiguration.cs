using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bzn.Cloudios.Infrastructure.Persistence.Configurations;

public sealed class ManagedDatabaseInstanceConfiguration : IEntityTypeConfiguration<ManagedDatabaseInstance>
{
    public void Configure(EntityTypeBuilder<ManagedDatabaseInstance> builder)
    {
        builder.ToTable("ManagedDatabaseInstances");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .HasColumnType("TEXT");

        builder.Property(d => d.RealmId)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(d => d.TierId)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("TEXT");

        builder.Property(d => d.Type)
            .IsRequired()
            .HasColumnType("TEXT")
            .HasConversion<string>();

        builder.ToTable(t => t.HasCheckConstraint("CK_ManagedDatabaseInstances_Type",
            "Type IN ('MySQL','MongoDB')"));

        builder.Property(d => d.NetworkId)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(d => d.CpuLimit)
            .IsRequired()
            .HasColumnType("REAL");

        builder.Property(d => d.MemoryLimit)
            .IsRequired()
            .HasColumnType("INTEGER");

        builder.Property(d => d.DockerContainerId)
            .HasMaxLength(64)
            .HasColumnType("TEXT");

        builder.Property(d => d.Status)
            .IsRequired()
            .HasColumnType("TEXT")
            .HasConversion<string>()
            .HasDefaultValue(ManagedDatabaseStatus.Provisioning);

        builder.ToTable(t => t.HasCheckConstraint("CK_ManagedDatabaseInstances_Status",
            "Status IN ('Provisioning','Running','Stopped','Failed')"));

        builder.Property(d => d.CreatedAt)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.HasIndex(d => d.RealmId);
        builder.HasIndex(d => new { d.RealmId, d.Status });
        builder.HasIndex(d => new { d.RealmId, d.Name }).IsUnique();
        builder.HasIndex(d => d.TierId);

        builder.HasOne(d => d.Realm)
            .WithMany(r => r.ManagedDatabases)
            .HasForeignKey(d => d.RealmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Tier)
            .WithMany(t => t.Instances)
            .HasForeignKey(d => d.TierId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
