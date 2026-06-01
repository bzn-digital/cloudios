using Bzn.Cloudios.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bzn.Cloudios.Infrastructure.Persistence.Configurations;

public sealed class MetricsContainerMetricHistoryConfiguration : IEntityTypeConfiguration<ContainerMetricHistory>
{
    public void Configure(EntityTypeBuilder<ContainerMetricHistory> builder)
    {
        builder.ToTable("ContainerMetrics_History");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd()
            .HasColumnType("INTEGER");

        builder.Property(m => m.ContainerId)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(m => m.Timestamp)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(m => m.CpuPercent)
            .IsRequired()
            .HasColumnType("REAL")
            .HasDefaultValue(0.0);

        builder.Property(m => m.MemoryUsedBytes)
            .IsRequired()
            .HasColumnType("INTEGER")
            .HasDefaultValue(0L);

        builder.Property(m => m.NetworkRxBytes)
            .IsRequired()
            .HasColumnType("INTEGER")
            .HasDefaultValue(0L);

        builder.Property(m => m.NetworkTxBytes)
            .IsRequired()
            .HasColumnType("INTEGER")
            .HasDefaultValue(0L);

        builder.Property(m => m.BlockReadBytes)
            .IsRequired()
            .HasColumnType("INTEGER")
            .HasDefaultValue(0L);

        builder.Property(m => m.BlockWriteBytes)
            .IsRequired()
            .HasColumnType("INTEGER")
            .HasDefaultValue(0L);

        builder.HasIndex(m => new { m.ContainerId, m.Timestamp });
        builder.HasIndex(m => m.Timestamp);
    }
}
