using Bzn.Cloudios.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bzn.Cloudios.Infrastructure.Persistence.Configurations;

public sealed class ContainerVolumeConfiguration : IEntityTypeConfiguration<ContainerVolume>
{
    public void Configure(EntityTypeBuilder<ContainerVolume> builder)
    {
        builder.ToTable("ContainerVolumes");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnType("TEXT");

        builder.Property(v => v.ContainerId)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(v => v.HostPath)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(v => v.ContainerPath)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(v => v.IsReadOnly)
            .IsRequired()
            .HasColumnType("INTEGER")
            .HasDefaultValue(false);

        builder.HasIndex(v => v.ContainerId);
    }
}
