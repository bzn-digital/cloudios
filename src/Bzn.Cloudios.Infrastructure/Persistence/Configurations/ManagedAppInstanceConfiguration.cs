using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bzn.Cloudios.Infrastructure.Persistence.Configurations;

public sealed class ManagedAppInstanceConfiguration : IEntityTypeConfiguration<ManagedAppInstance>
{
    public void Configure(EntityTypeBuilder<ManagedAppInstance> builder)
    {
        builder.ToTable("ManagedAppInstances");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnType("TEXT");

        builder.Property(i => i.RealmId)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(i => i.TemplateId)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(i => i.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("TEXT");

        builder.Property(i => i.Status)
            .IsRequired()
            .HasColumnType("TEXT")
            .HasConversion<string>()
            .HasDefaultValue(ManagedAppStatus.Provisioning);

        builder.ToTable(t => t.HasCheckConstraint("CK_ManagedAppInstances_Status",
            "Status IN ('Provisioning','Running','Stopped','Failed','Terminated')"));

        builder.Property(i => i.Size)
            .IsRequired()
            .HasColumnType("TEXT")
            .HasConversion<string>();

        builder.Property(i => i.HostPort)
            .IsRequired()
            .HasColumnType("INTEGER");

        builder.Property(i => i.DockerContainerId)
            .HasColumnType("TEXT");

        builder.Property(i => i.CpuLimitCores)
            .IsRequired()
            .HasColumnType("REAL")
            .HasDefaultValue(0.5);

        builder.Property(i => i.MemoryLimitBytes)
            .IsRequired()
            .HasColumnType("INTEGER")
            .HasDefaultValue(536870912);

        builder.Property(i => i.CostPerHourBRL)
            .IsRequired()
            .HasColumnType("REAL")
            .HasDefaultValue(0.02m);

        builder.Property(i => i.CreatedAt)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(i => i.StartedAtUtc)
            .HasColumnType("TEXT");

        builder.Property(i => i.StoppedAtUtc)
            .HasColumnType("TEXT");

        builder.HasIndex(i => i.RealmId);
        builder.HasIndex(i => new { i.RealmId, i.Status });
        builder.HasIndex(i => i.HostPort).IsUnique();

        builder.HasOne(i => i.Realm)
            .WithMany(r => r.ManagedAppInstances)
            .HasForeignKey(i => i.RealmId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Template)
            .WithMany()
            .HasForeignKey(i => i.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
