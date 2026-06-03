using Bzn.Cloudios.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bzn.Cloudios.Infrastructure.Persistence.Configurations;

public sealed class BillingPeriodConfiguration : IEntityTypeConfiguration<BillingPeriod>
{
    public void Configure(EntityTypeBuilder<BillingPeriod> builder)
    {
        builder.ToTable("BillingPeriods");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedOnAdd()
            .HasColumnType("INTEGER");

        builder.Property(b => b.ContainerId)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(b => b.StartedAtUtc)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(b => b.StoppedAtUtc)
            .HasColumnType("TEXT");

        builder.Property(b => b.Hours)
            .IsRequired()
            .HasColumnType("REAL")
            .HasDefaultValue(0.0);

        builder.Property(b => b.CostBRL)
            .IsRequired()
            .HasColumnType("REAL")
            .HasDefaultValue(0.0m);

        builder.HasIndex(b => new { b.ContainerId, b.StartedAtUtc });
        builder.HasIndex(b => b.StartedAtUtc);
    }
}
