using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Bzn.Cloudios.Tests;

public class RealmQuotaTests
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
    public void Realm_QuotaProperties_AreNullable()
    {
        var realm = new Realm
        {
            Id = Guid.NewGuid(),
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        Assert.Null(realm.MaxContainers);
        Assert.Null(realm.MaxDatabases);
        Assert.Null(realm.MaxManagedApps);
        Assert.Null(realm.MaxRamBytes);
        Assert.Null(realm.MaxCpuCores);
    }

    [Fact]
    public void Realm_QuotaProperties_CanBeSet()
    {
        var realm = new Realm
        {
            Id = Guid.NewGuid(),
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MaxContainers = 10,
            MaxDatabases = 5,
            MaxManagedApps = 20,
            MaxRamBytes = 8L * 1024 * 1024 * 1024, // 8 GB
            MaxCpuCores = 4.0
        };

        Assert.Equal(10, realm.MaxContainers);
        Assert.Equal(5, realm.MaxDatabases);
        Assert.Equal(20, realm.MaxManagedApps);
        Assert.Equal(8L * 1024 * 1024 * 1024, realm.MaxRamBytes);
        Assert.Equal(4.0, realm.MaxCpuCores);
    }

    [Fact]
    public async Task Realm_QuotaProperties_PersistToDatabase()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();

        var realm = new Realm
        {
            Id = realmId,
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MaxContainers = 10,
            MaxDatabases = 5,
            MaxManagedApps = 20,
            MaxRamBytes = 8L * 1024 * 1024 * 1024,
            MaxCpuCores = 4.0
        };

        db.Realms.Add(realm);
        await db.SaveChangesAsync();

        var retrieved = await db.Realms.FindAsync(realmId);
        Assert.NotNull(retrieved);
        Assert.Equal(10, retrieved!.MaxContainers);
        Assert.Equal(5, retrieved.MaxDatabases);
        Assert.Equal(20, retrieved.MaxManagedApps);
        Assert.Equal(8L * 1024 * 1024 * 1024, retrieved.MaxRamBytes);
        Assert.Equal(4.0, retrieved.MaxCpuCores);
    }

    [Fact]
    public async Task Realm_QuotaProperties_NullValuesPersistToDatabase()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();

        var realm = new Realm
        {
            Id = realmId,
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
            // All quota properties left as null
        };

        db.Realms.Add(realm);
        await db.SaveChangesAsync();

        var retrieved = await db.Realms.FindAsync(realmId);
        Assert.NotNull(retrieved);
        Assert.Null(retrieved!.MaxContainers);
        Assert.Null(retrieved.MaxDatabases);
        Assert.Null(retrieved.MaxManagedApps);
        Assert.Null(retrieved.MaxRamBytes);
        Assert.Null(retrieved.MaxCpuCores);
    }

    [Fact]
    public async Task Realm_QuotaProperties_CanBeUpdated()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();

        var realm = new Realm
        {
            Id = realmId,
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MaxContainers = 10,
            MaxDatabases = 5
        };

        db.Realms.Add(realm);
        await db.SaveChangesAsync();

        // Update quota properties
        realm.MaxContainers = 20;
        realm.MaxDatabases = 10;
        realm.MaxManagedApps = 30;
        realm.MaxRamBytes = 16L * 1024 * 1024 * 1024;
        realm.MaxCpuCores = 8.0;

        await db.SaveChangesAsync();

        var retrieved = await db.Realms.FindAsync(realmId);
        Assert.NotNull(retrieved);
        Assert.Equal(20, retrieved!.MaxContainers);
        Assert.Equal(10, retrieved.MaxDatabases);
        Assert.Equal(30, retrieved.MaxManagedApps);
        Assert.Equal(16L * 1024 * 1024 * 1024, retrieved.MaxRamBytes);
        Assert.Equal(8.0, retrieved.MaxCpuCores);
    }

    [Fact]
    public async Task RealmService_Suspend_SetsIsActiveToFalse()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();

        var realm = new Realm
        {
            Id = realmId,
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Realms.Add(realm);
        await db.SaveChangesAsync();

        var logger = new Mock<ILogger<RealmService>>();
        var dockerNetworkService = new Mock<IDockerNetworkService>();
        var containerService = new Mock<IContainerService>();
        var billingService = new Mock<IBillingService>();
        var managedAppService = new Mock<IManagedAppService>();

        var service = new RealmService(db, logger.Object, dockerNetworkService.Object, containerService.Object, billingService.Object, managedAppService.Object);

        var (response, error) = await service.SuspendAsync(realmId);

        Assert.Null(error);
        Assert.NotNull(response);
        Assert.False(response.IsActive);
        Assert.Equal(realmId, response.Id);
    }

    [Fact]
    public async Task RealmService_Suspend_AlreadySuspended_ReturnsError()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();

        var realm = new Realm
        {
            Id = realmId,
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Realms.Add(realm);
        await db.SaveChangesAsync();

        var logger = new Mock<ILogger<RealmService>>();
        var dockerNetworkService = new Mock<IDockerNetworkService>();
        var containerService = new Mock<IContainerService>();
        var billingService = new Mock<IBillingService>();
        var managedAppService = new Mock<IManagedAppService>();

        var service = new RealmService(db, logger.Object, dockerNetworkService.Object, containerService.Object, billingService.Object, managedAppService.Object);

        var (response, error) = await service.SuspendAsync(realmId);

        Assert.NotNull(error);
        Assert.Equal("Realm is already suspended", error);
        Assert.Null(response);
    }

    [Fact]
    public async Task RealmService_Reactivate_SetsIsActiveToTrue()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();

        var realm = new Realm
        {
            Id = realmId,
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = false,
            CreatedAt = DateTime.UtcNow
        };

        db.Realms.Add(realm);
        await db.SaveChangesAsync();

        var logger = new Mock<ILogger<RealmService>>();
        var dockerNetworkService = new Mock<IDockerNetworkService>();
        var containerService = new Mock<IContainerService>();
        var billingService = new Mock<IBillingService>();
        var managedAppService = new Mock<IManagedAppService>();

        var service = new RealmService(db, logger.Object, dockerNetworkService.Object, containerService.Object, billingService.Object, managedAppService.Object);

        var (response, error) = await service.ReactivateAsync(realmId);

        Assert.Null(error);
        Assert.NotNull(response);
        Assert.True(response.IsActive);
        Assert.Equal(realmId, response.Id);
    }

    [Fact]
    public async Task RealmService_UpdateQuotas_UpdatesQuotaProperties()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();

        var realm = new Realm
        {
            Id = realmId,
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Realms.Add(realm);
        await db.SaveChangesAsync();

        var logger = new Mock<ILogger<RealmService>>();
        var dockerNetworkService = new Mock<IDockerNetworkService>();
        var containerService = new Mock<IContainerService>();
        var billingService = new Mock<IBillingService>();
        var managedAppService = new Mock<IManagedAppService>();

        var service = new RealmService(db, logger.Object, dockerNetworkService.Object, containerService.Object, billingService.Object, managedAppService.Object);

        var request = new UpdateQuotasRequest
        {
            MaxContainers = 15,
            MaxDatabases = 8,
            MaxManagedApps = 25,
            MaxRamBytes = 16L * 1024 * 1024 * 1024,
            MaxCpuCores = 6.0
        };

        var (response, error) = await service.UpdateQuotasAsync(realmId, request);

        Assert.Null(error);
        Assert.NotNull(response);

        var updatedRealm = await db.Realms.FindAsync(realmId);
        Assert.Equal(15, updatedRealm!.MaxContainers);
        Assert.Equal(8, updatedRealm.MaxDatabases);
        Assert.Equal(25, updatedRealm.MaxManagedApps);
        Assert.Equal(16L * 1024 * 1024 * 1024, updatedRealm.MaxRamBytes);
        Assert.Equal(6.0, updatedRealm.MaxCpuCores);
    }

    [Fact]
    public async Task RealmService_GetStats_ReturnsCorrectStats()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();

        var realm = new Realm
        {
            Id = realmId,
            Name = "Test Realm",
            Slug = "test-realm",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            MaxContainers = 10,
            MaxDatabases = 5,
            MaxManagedApps = 20
        };

        db.Realms.Add(realm);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = UserRole.RealmOwner,
            RealmId = realmId,
            IsBlocked = false
        };
        db.Users.Add(user);

        var container = new Container
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            Name = "test-container",
            ImageName = "nginx",
            Status = Domain.Enums.ContainerStatus.Running,
            CpuLimitCores = 1.0,
            MemoryLimitBytes = 1024 * 1024 * 1024,
            CostPerHourBRL = 0.5m,
            CreatedAt = DateTime.UtcNow
        };
        db.Containers.Add(container);

        var database = new ManagedDatabaseInstance
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            Name = "test-db",
            Type = Domain.Enums.ManagedDatabaseType.MySQL,
            Status = Domain.Enums.ManagedDatabaseStatus.Running,
            CpuLimit = 0.5,
            MemoryLimit = 512 * 1024 * 1024,
            CreatedAt = DateTime.UtcNow
        };
        db.ManagedDatabaseInstances.Add(database);

        await db.SaveChangesAsync();

        var logger = new Mock<ILogger<RealmService>>();
        var dockerNetworkService = new Mock<IDockerNetworkService>();
        var containerService = new Mock<IContainerService>();
        var billingService = new Mock<IBillingService>();
        var managedAppService = new Mock<IManagedAppService>();
        billingService.Setup(b => b.GetRealmBillingAsync(realmId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(100.0m);

        var service = new RealmService(db, logger.Object, dockerNetworkService.Object, containerService.Object, billingService.Object, managedAppService.Object);

        var stats = await service.GetStatsAsync(realmId);

        Assert.NotNull(stats);
        Assert.Equal(1, stats.UsersCount);
        Assert.Equal(1, stats.ContainersCount);
        Assert.Equal(1, stats.DatabasesCount);
        Assert.Equal(100.0m, stats.MonthlyCostBRL);
        Assert.Equal(10, stats.Quotas.MaxContainers);
        Assert.Equal(5, stats.Quotas.MaxDatabases);
        Assert.Equal(20, stats.Quotas.MaxManagedApps);
        Assert.Equal(1, stats.Usage.ContainersCount);
        Assert.Equal(1, stats.Usage.DatabasesCount);
    }
}
