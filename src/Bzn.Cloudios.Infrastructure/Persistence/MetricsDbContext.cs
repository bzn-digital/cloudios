using Bzn.Cloudios.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bzn.Cloudios.Infrastructure.Persistence;

public sealed class MetricsDbContext : DbContext
{
    public DbSet<ContainerMetricHistory> ContainerMetricsHistory => Set<ContainerMetricHistory>();

    public MetricsDbContext(DbContextOptions<MetricsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MetricsDbContext).Assembly, t =>
            t.Namespace?.StartsWith("Bzn.Cloudios.Infrastructure.Persistence.Configurations") == true
            && t.Name.StartsWith("Metrics", StringComparison.Ordinal));
    }
}
