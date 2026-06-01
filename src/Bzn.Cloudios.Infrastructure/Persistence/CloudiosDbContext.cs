using Bzn.Cloudios.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Bzn.Cloudios.Infrastructure.Persistence;

public sealed class CloudiosDbContext : DbContext
{
    public DbSet<Realm> Realms => Set<Realm>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Container> Containers => Set<Container>();
    public DbSet<ContainerVolume> ContainerVolumes => Set<ContainerVolume>();
    public DbSet<ContainerEnvVar> ContainerEnvVars => Set<ContainerEnvVar>();
    public DbSet<BillingPeriod> BillingPeriods => Set<BillingPeriod>();

    public CloudiosDbContext(DbContextOptions<CloudiosDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CloudiosDbContext).Assembly, t =>
            t.Namespace?.StartsWith("Bzn.Cloudios.Infrastructure.Persistence.Configurations") == true
            && !t.Name.StartsWith("Metrics", StringComparison.Ordinal));
    }
}
