using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bzn.Cloudios.Tests;

public class BillingServiceTests
{
    private static CloudiosDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<CloudiosDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var db = new CloudiosDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task RegisterStartAsync_CreatesBillingPeriod()
    {
        var db = CreateInMemoryDb();
        var containerId = Guid.NewGuid();
        var realmId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.Containers.Add(new Container
        {
            Id = containerId,
            RealmId = realmId,
            Name = "test",
            ImageName = "nginx",
            InternalPort = 80,
            CostPerHourBRL = 0.02m,
            Status = ContainerStatus.Running,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var logger = NullLogger<BillingService>.Instance;
        var service = new BillingService(db, logger);

        var startedAt = DateTime.UtcNow;
        await service.RegisterStartAsync(containerId, startedAt, CancellationToken.None);

        var periods = await db.BillingPeriods.ToListAsync();
        Assert.Single(periods);
        Assert.Equal(containerId, periods[0].ContainerId);
        Assert.Equal(startedAt, periods[0].StartedAtUtc);
        Assert.Null(periods[0].StoppedAtUtc);
    }

    [Fact]
    public async Task RegisterStopAsync_CalculatesHoursAndCost()
    {
        var db = CreateInMemoryDb();
        var containerId = Guid.NewGuid();
        var realmId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddHours(-10);
        var stoppedAt = DateTime.UtcNow;

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.Containers.Add(new Container
        {
            Id = containerId,
            RealmId = realmId,
            Name = "test",
            ImageName = "nginx",
            InternalPort = 80,
            CostPerHourBRL = 0.02m,
            Status = ContainerStatus.Running,
            CreatedAt = DateTime.UtcNow
        });
        db.BillingPeriods.Add(new BillingPeriod
        {
            ContainerId = containerId,
            StartedAtUtc = startedAt,
            StoppedAtUtc = null,
            Hours = 0,
            CostBRL = 0
        });
        await db.SaveChangesAsync();

        var logger = NullLogger<BillingService>.Instance;
        var service = new BillingService(db, logger);

        await service.RegisterStopAsync(containerId, stoppedAt, CancellationToken.None);

        var period = await db.BillingPeriods.FirstAsync();
        Assert.Equal(stoppedAt, period.StoppedAtUtc);
        Assert.Equal(10, period.Hours, 1);
        Assert.Equal(0.20m, period.CostBRL, 2);
    }

    [Fact]
    public async Task GetRealmBillingAsync_SumsContainerCosts()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var container1 = Guid.NewGuid();
        var container2 = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.Containers.AddRange(
            new Container { Id = container1, RealmId = realmId, Name = "c1", ImageName = "nginx", InternalPort = 80, CostPerHourBRL = 0.02m, Status = ContainerStatus.Running, CreatedAt = DateTime.UtcNow },
            new Container { Id = container2, RealmId = realmId, Name = "c2", ImageName = "redis", InternalPort = 6379, CostPerHourBRL = 0.03m, Status = ContainerStatus.Running, CreatedAt = DateTime.UtcNow }
        );
        var now = DateTime.UtcNow;
        db.BillingPeriods.AddRange(
            new BillingPeriod { ContainerId = container1, StartedAtUtc = now, StoppedAtUtc = now.AddHours(10), Hours = 10, CostBRL = 0.20m },
            new BillingPeriod { ContainerId = container2, StartedAtUtc = now, StoppedAtUtc = now.AddHours(5), Hours = 5, CostBRL = 0.15m }
        );
        await db.SaveChangesAsync();

        var logger = NullLogger<BillingService>.Instance;
        var service = new BillingService(db, logger);

        var total = await service.GetRealmBillingAsync(realmId, now.Year, now.Month, CancellationToken.None);
        Assert.Equal(0.35m, total, 2);
    }

    [Fact]
    public async Task GetGlobalBillingAsync_SumsAllRealms()
    {
        var db = CreateInMemoryDb();
        var realm1 = Guid.NewGuid();
        var realm2 = Guid.NewGuid();
        var container1 = Guid.NewGuid();
        var container2 = Guid.NewGuid();

        db.Realms.AddRange(
            new Realm { Id = realm1, Name = "R1", Slug = "r1", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Realm { Id = realm2, Name = "R2", Slug = "r2", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        db.Containers.AddRange(
            new Container { Id = container1, RealmId = realm1, Name = "c1", ImageName = "nginx", InternalPort = 80, CostPerHourBRL = 0.02m, Status = ContainerStatus.Running, CreatedAt = DateTime.UtcNow },
            new Container { Id = container2, RealmId = realm2, Name = "c2", ImageName = "redis", InternalPort = 6379, CostPerHourBRL = 0.03m, Status = ContainerStatus.Running, CreatedAt = DateTime.UtcNow }
        );
        var now = DateTime.UtcNow;
        db.BillingPeriods.AddRange(
            new BillingPeriod { ContainerId = container1, StartedAtUtc = now, StoppedAtUtc = now.AddHours(10), Hours = 10, CostBRL = 0.20m },
            new BillingPeriod { ContainerId = container2, StartedAtUtc = now, StoppedAtUtc = now.AddHours(5), Hours = 5, CostBRL = 0.15m }
        );
        await db.SaveChangesAsync();

        var logger = NullLogger<BillingService>.Instance;
        var service = new BillingService(db, logger);

        var total = await service.GetGlobalBillingAsync(now.Year, now.Month, CancellationToken.None);
        Assert.Equal(0.35m, total, 2);
    }

    [Fact]
    public async Task GetContainerMonthCostAsync_IncludesActivePeriod()
    {
        var db = CreateInMemoryDb();
        var containerId = Guid.NewGuid();
        var realmId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.Containers.Add(new Container
        {
            Id = containerId,
            RealmId = realmId,
            Name = "test",
            ImageName = "nginx",
            InternalPort = 80,
            CostPerHourBRL = 0.02m,
            Status = ContainerStatus.Running,
            CreatedAt = DateTime.UtcNow
        });
        db.BillingPeriods.Add(new BillingPeriod
        {
            ContainerId = containerId,
            StartedAtUtc = now.AddHours(-5),
            StoppedAtUtc = null,
            Hours = 0,
            CostBRL = 0
        });
        await db.SaveChangesAsync();

        var logger = NullLogger<BillingService>.Instance;
        var service = new BillingService(db, logger);

        var cost = await service.GetContainerMonthCostAsync(containerId, now.Year, now.Month, CancellationToken.None);
        Assert.Equal(0.10m, cost, 2);
    }

    [Fact]
    public async Task RegisterStopAsync_NoActivePeriod_DoesNothing()
    {
        var db = CreateInMemoryDb();
        var containerId = Guid.NewGuid();
        var realmId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.Containers.Add(new Container
        {
            Id = containerId,
            RealmId = realmId,
            Name = "test",
            ImageName = "nginx",
            InternalPort = 80,
            CostPerHourBRL = 0.02m,
            Status = ContainerStatus.Stopped,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var logger = NullLogger<BillingService>.Instance;
        var service = new BillingService(db, logger);

        await service.RegisterStopAsync(containerId, DateTime.UtcNow, CancellationToken.None);

        var count = await db.BillingPeriods.CountAsync();
        Assert.Equal(0, count);
    }
}
