using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bzn.Cloudios.Infrastructure.Persistence.Configurations;

public sealed class ContainerConfiguration : IEntityTypeConfiguration<Container>
{
    public void Configure(EntityTypeBuilder<Container> builder)
    {
        builder.ToTable("Containers");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnType("TEXT");

        builder.Property(c => c.RealmId)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100)
            .HasColumnType("TEXT");

        builder.Property(c => c.DockerContainerId)
            .HasColumnType("TEXT");

        builder.Property(c => c.ImageName)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(c => c.InternalPort)
            .IsRequired()
            .HasColumnType("INTEGER")
            .HasDefaultValue(8080);

        builder.Property(c => c.Status)
            .IsRequired()
            .HasColumnType("TEXT")
            .HasConversion<string>()
            .HasDefaultValue(ContainerStatus.Stopped);

        builder.HasCheckConstraint("CK_Containers_Status",
            "Status IN ('Deploying','Running','Stopped','Failed')");

        builder.Property(c => c.CpuLimitCores)
            .IsRequired()
            .HasColumnType("REAL")
            .HasDefaultValue(0.5);

        builder.Property(c => c.MemoryLimitBytes)
            .IsRequired()
            .HasColumnType("INTEGER")
            .HasDefaultValue(536870912);

        builder.Property(c => c.CostPerHourBRL)
            .IsRequired()
            .HasColumnType("REAL")
            .HasDefaultValue(0.02m);

        builder.Property(c => c.StartedAtUtc)
            .HasColumnType("TEXT");

        builder.Property(c => c.CreatedAt)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.HasIndex(c => c.RealmId);
        builder.HasIndex(c => new { c.RealmId, c.Status });
        builder.HasIndex(c => c.DockerContainerId);
        builder.HasIndex(c => c.Name);

        builder.HasMany(c => c.Volumes)
            .WithOne(v => v.Container)
            .HasForeignKey(v => v.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.EnvironmentVariables)
            .WithOne(e => e.Container)
            .HasForeignKey(e => e.ContainerId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
