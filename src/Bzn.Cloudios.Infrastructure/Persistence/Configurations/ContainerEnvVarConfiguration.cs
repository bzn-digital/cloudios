using Bzn.Cloudios.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bzn.Cloudios.Infrastructure.Persistence.Configurations;

public sealed class ContainerEnvVarConfiguration : IEntityTypeConfiguration<ContainerEnvVar>
{
    public void Configure(EntityTypeBuilder<ContainerEnvVar> builder)
    {
        builder.ToTable("ContainerEnvVars");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnType("TEXT");

        builder.Property(e => e.ContainerId)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(e => e.Key)
            .IsRequired()
            .HasMaxLength(256)
            .HasColumnType("TEXT");

        builder.Property(e => e.Value)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.HasIndex(e => e.ContainerId);
        builder.HasIndex(e => new { e.ContainerId, e.Key }).IsUnique();
    }
}
