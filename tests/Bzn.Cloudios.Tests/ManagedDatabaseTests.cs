using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bzn.Cloudios.Tests;

public class ManagedDatabaseTests
{
    private static readonly Guid MiniTierId = Guid.Parse("00000000-0000-0000-0000-000000000103"); // dbl-mini-1s: 1 vCPU, 1 GiB

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

    private static Guid SeedActiveRealm(CloudiosDbContext db)
    {
        var realmId = Guid.NewGuid();
        db.Realms.Add(new Realm { Id = realmId, Name = "Acme", Slug = "acme", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.SaveChanges();
        return realmId;
    }

    private static ManagedDatabaseService CreateService(CloudiosDbContext db, Guid realmId)
    {
        var billing = new BillingService(db, NullLogger<BillingService>.Instance);
        var tenant = new StubTenantProvider(realmId);
        return new ManagedDatabaseService(db, tenant, billing, NullLogger<ManagedDatabaseService>.Instance);
    }

    [Theory]
    [InlineData(ManagedDatabaseType.MySQL, 0.17)]   // 1*0.05 + 1*0.02 + 0.10
    [InlineData(ManagedDatabaseType.MongoDB, 0.19)] // 1*0.05 + 1*0.02 + 0.12
    public void HourlyRate_SumsCpuMemoryAndEngineFixedCost(ManagedDatabaseType type, double expected)
    {
        var rate = ManagedDatabasePricing.HourlyRateBRL(1.0, 1024L * 1024 * 1024, type);
        Assert.Equal((decimal)expected, rate);
    }

    [Fact]
    public void MonthlyForecast_IsHourlyRateTimes730()
    {
        var hourly = ManagedDatabasePricing.HourlyRateBRL(1.0, 1024L * 1024 * 1024, ManagedDatabaseType.MySQL);
        Assert.Equal(0.17m * 730m, ManagedDatabasePricing.MonthlyForecastBRL(hourly));
    }

    [Fact]
    public async Task GetTiersAsync_ReturnsAllTiersWithBothEnginePrices()
    {
        var db = CreateInMemoryDb();
        var service = CreateService(db, SeedActiveRealm(db));

        var result = await service.GetTiersAsync(CancellationToken.None);

        Assert.Equal(10, result.Tiers.Count);
        foreach (var tier in result.Tiers)
        {
            Assert.Equal(2, tier.Pricing.Count);
            Assert.All(tier.Pricing, p =>
            {
                Assert.True(p.HourlyRateBRL > 0);
                Assert.Equal(ManagedDatabasePricing.MonthlyForecastBRL(p.HourlyRateBRL), p.MonthlyForecastBRL);
            });
        }
    }

    [Fact]
    public async Task CreateAsync_ActivatesInstanceAndStartsBilling()
    {
        var db = CreateInMemoryDb();
        var realmId = SeedActiveRealm(db);
        var service = CreateService(db, realmId);

        var (instance, error, status) = await service.CreateAsync(
            new CreateManagedDatabaseRequest { Name = "orders-db", TierId = MiniTierId, Type = "MySQL" },
            CancellationToken.None);

        Assert.Null(error);
        Assert.Equal(StatusCodes.Status201Created, status);
        Assert.NotNull(instance);
        Assert.Equal("Running", instance!.Status);
        Assert.Equal(0.17m, instance.HourlyRateBRL);
        Assert.Equal(0.17m * 730m, instance.MonthlyForecastBRL);

        var persisted = await db.ManagedDatabaseInstances.SingleAsync();
        Assert.Equal(ManagedDatabaseStatus.Running, persisted.Status);

        var period = await db.BillingPeriods.SingleAsync();
        Assert.Equal(instance.Id, period.ManagedDatabaseId);
        Assert.Null(period.ContainerId);
        Assert.Null(period.StoppedAtUtc);
    }

    [Fact]
    public async Task CreateAsync_InvalidType_Returns400()
    {
        var db = CreateInMemoryDb();
        var service = CreateService(db, SeedActiveRealm(db));

        var (_, error, status) = await service.CreateAsync(
            new CreateManagedDatabaseRequest { Name = "x", TierId = MiniTierId, Type = "Postgres" },
            CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(StatusCodes.Status400BadRequest, status);
    }

    [Fact]
    public async Task CreateAsync_TierNotFound_Returns400()
    {
        var db = CreateInMemoryDb();
        var service = CreateService(db, SeedActiveRealm(db));

        var (_, error, status) = await service.CreateAsync(
            new CreateManagedDatabaseRequest { Name = "x", TierId = Guid.NewGuid(), Type = "MySQL" },
            CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(StatusCodes.Status400BadRequest, status);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameInRealm_Returns409()
    {
        var db = CreateInMemoryDb();
        var realmId = SeedActiveRealm(db);
        var service = CreateService(db, realmId);

        var request = new CreateManagedDatabaseRequest { Name = "dup", TierId = MiniTierId, Type = "MySQL" };
        await service.CreateAsync(request, CancellationToken.None);
        var (_, error, status) = await service.CreateAsync(request, CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(StatusCodes.Status409Conflict, status);
    }

    [Fact]
    public async Task CreateAsync_InactiveRealm_Returns403()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        db.Realms.Add(new Realm { Id = realmId, Name = "Blocked", Slug = "blocked", IsActive = false, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var service = CreateService(db, realmId);

        var (_, error, status) = await service.CreateAsync(
            new CreateManagedDatabaseRequest { Name = "x", TierId = MiniTierId, Type = "MySQL" },
            CancellationToken.None);

        Assert.NotNull(error);
        Assert.Equal(StatusCodes.Status403Forbidden, status);
    }

    [Fact]
    public async Task CreateAsync_QuotaExceeded_Returns403()
    {
        var db = CreateInMemoryDb();
        var realmId = SeedActiveRealm(db);
        var service = CreateService(db, realmId);

        for (var i = 0; i < ManagedDatabaseService.MaxDatabasesPerRealm; i++)
        {
            var (_, error, _) = await service.CreateAsync(
                new CreateManagedDatabaseRequest { Name = $"db-{i}", TierId = MiniTierId, Type = "MySQL" },
                CancellationToken.None);
            Assert.Null(error);
        }

        var (_, quotaError, status) = await service.CreateAsync(
            new CreateManagedDatabaseRequest { Name = "over", TierId = MiniTierId, Type = "MySQL" },
            CancellationToken.None);

        Assert.NotNull(quotaError);
        Assert.Equal(StatusCodes.Status403Forbidden, status);
    }

    [Fact]
    public async Task RegisterDatabaseStopAsync_CalculatesCostFromTierAndEngine()
    {
        var db = CreateInMemoryDb();
        var realmId = SeedActiveRealm(db);
        var instanceId = Guid.NewGuid();
        var startedAt = DateTime.UtcNow.AddHours(-10);

        db.ManagedDatabaseInstances.Add(new ManagedDatabaseInstance
        {
            Id = instanceId,
            RealmId = realmId,
            TierId = MiniTierId,
            Name = "metrics-db",
            Type = ManagedDatabaseType.MySQL,
            CpuLimit = 1.0,
            MemoryLimit = 1024L * 1024 * 1024,
            Status = ManagedDatabaseStatus.Running,
            CreatedAt = DateTime.UtcNow
        });
        db.BillingPeriods.Add(new BillingPeriod
        {
            ManagedDatabaseId = instanceId,
            StartedAtUtc = startedAt,
            StoppedAtUtc = null,
            Hours = 0,
            CostBRL = 0
        });
        await db.SaveChangesAsync();

        var billing = new BillingService(db, NullLogger<BillingService>.Instance);
        var stoppedAt = startedAt.AddHours(10);
        await billing.RegisterDatabaseStopAsync(instanceId, stoppedAt, CancellationToken.None);

        var period = await db.BillingPeriods.SingleAsync();
        Assert.Equal(10, period.Hours, 1);
        Assert.Equal(1.70m, period.CostBRL, 2); // 10h * 0.17

        var realmTotal = await billing.GetRealmBillingAsync(realmId, stoppedAt.Year, stoppedAt.Month, CancellationToken.None);
        Assert.Equal(1.70m, realmTotal, 2);
    }
}

internal sealed class StubTenantProvider : ITenantProvider
{
    public StubTenantProvider(Guid realmId)
    {
        RealmId = realmId;
    }

    public Guid RealmId { get; }
    public string Role { get; } = "RealmOwner";
    public Guid UserId { get; } = Guid.NewGuid();
}
