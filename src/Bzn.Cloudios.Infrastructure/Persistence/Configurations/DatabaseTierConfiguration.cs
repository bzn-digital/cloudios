using Bzn.Cloudios.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Bzn.Cloudios.Infrastructure.Persistence.Configurations;

public sealed class DatabaseTierConfiguration : IEntityTypeConfiguration<DatabaseTier>
{
    private const long Mb = 1024L * 1024L;
    private const long Gb = 1024L * Mb;

    public void Configure(EntityTypeBuilder<DatabaseTier> builder)
    {
        builder.ToTable("DatabaseTiers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnType("TEXT");

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(50)
            .HasColumnType("TEXT");

        builder.Property(t => t.CpuLimitCores)
            .IsRequired()
            .HasColumnType("REAL");

        builder.Property(t => t.MemoryLimitBytes)
            .IsRequired()
            .HasColumnType("INTEGER");

        builder.HasIndex(t => t.Name).IsUnique();

        builder.HasData(Seed());
    }

    private static DatabaseTier[] Seed() =>
    [
        Tier("00000000-0000-0000-0000-000000000101", "dbl-micro-1s", 0.5, 500 * Mb),
        Tier("00000000-0000-0000-0000-000000000102", "dbl-micro-2s", 0.5, 1 * Gb),
        Tier("00000000-0000-0000-0000-000000000103", "dbl-mini-1s", 1, 1 * Gb),
        Tier("00000000-0000-0000-0000-000000000104", "dbl-mini-2s", 2, 1 * Gb),
        Tier("00000000-0000-0000-0000-000000000105", "dbl-standard-1s", 2, 2 * Gb),
        Tier("00000000-0000-0000-0000-000000000106", "dbl-standard-2s", 2, 4 * Gb),
        Tier("00000000-0000-0000-0000-000000000107", "dbl-standard-3s", 4, 4 * Gb),
        Tier("00000000-0000-0000-0000-000000000108", "dbl-large-1s", 4, 8 * Gb),
        Tier("00000000-0000-0000-0000-000000000109", "dbl-large-2s", 8, 10 * Gb),
        Tier("00000000-0000-0000-0000-000000000110", "dbl-large-3s", 10, 12 * Gb),
    ];

    private static DatabaseTier Tier(string id, string name, double cpuCores, long memoryBytes) => new()
    {
        Id = Guid.Parse(id),
        Name = name,
        CpuLimitCores = cpuCores,
        MemoryLimitBytes = memoryBytes
    };
}
