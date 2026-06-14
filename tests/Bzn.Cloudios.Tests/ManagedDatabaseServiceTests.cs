using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bzn.Cloudios.Tests;

public class ManagedDatabaseServiceTests
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
    public void GetImage_MapsToOfficialImages()
    {
        Assert.Equal("mysql:latest", ManagedDatabaseService.GetImage(ManagedDatabaseType.MySQL));
        Assert.Equal("mongo:latest", ManagedDatabaseService.GetImage(ManagedDatabaseType.MongoDB));
    }

    [Fact]
    public void GetDataPath_MapsToEnginePersistencePaths()
    {
        Assert.Equal("/var/lib/mysql", ManagedDatabaseService.GetDataPath(ManagedDatabaseType.MySQL));
        Assert.Equal("/data/db", ManagedDatabaseService.GetDataPath(ManagedDatabaseType.MongoDB));
    }

    [Fact]
    public void BuildEnvironment_MySQL_SetsRootPassword()
    {
        var env = ManagedDatabaseService.BuildEnvironment(ManagedDatabaseType.MySQL, "secret");
        Assert.Contains("MYSQL_ROOT_PASSWORD=secret", env);
    }

    [Fact]
    public void BuildEnvironment_MongoDB_SetsRootCredentials()
    {
        var env = ManagedDatabaseService.BuildEnvironment(ManagedDatabaseType.MongoDB, "secret");
        Assert.Contains("MONGO_INITDB_ROOT_USERNAME=root", env);
        Assert.Contains("MONGO_INITDB_ROOT_PASSWORD=secret", env);
    }

    [Fact]
    public void BuildCreateContainerParameters_AppliesTierResourceLimits()
    {
        var instance = new ManagedDatabaseInstance
        {
            Id = Guid.NewGuid(),
            RealmId = Guid.NewGuid(),
            Name = "orders db",
            Type = ManagedDatabaseType.MySQL
        };
        var tier = new DatabaseTier { Id = Guid.NewGuid(), Name = "dbl-mini-2s", CpuLimitCores = 2, MemoryLimitBytes = 1024L * 1024L * 1024L };

        var p = ManagedDatabaseService.BuildCreateContainerParameters(
            instance, tier, "mysql:latest", "cloudios_test", "/host/data", "pw");

        Assert.Equal(1024L * 1024L * 1024L, p.HostConfig.Memory);
        Assert.Equal(2_000_000_000L, p.HostConfig.NanoCPUs);
    }

    [Fact]
    public void BuildCreateContainerParameters_LabelsWithRealmId()
    {
        var realmId = Guid.NewGuid();
        var instance = new ManagedDatabaseInstance
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            Name = "db",
            Type = ManagedDatabaseType.MongoDB
        };
        var tier = new DatabaseTier { Id = Guid.NewGuid(), Name = "t", CpuLimitCores = 1, MemoryLimitBytes = 1 };

        var p = ManagedDatabaseService.BuildCreateContainerParameters(
            instance, tier, "mongo:latest", "cloudios_test", "/host/data", "pw");

        Assert.Equal(realmId.ToString(), p.Labels["cloudios.realm"]);
        Assert.Equal(instance.Id.ToString(), p.Labels["cloudios.database"]);
        Assert.Equal("true", p.Labels["cloudios.managed"]);
    }

    [Fact]
    public void BuildCreateContainerParameters_BindsVolumeAndAttachesNetwork()
    {
        var instance = new ManagedDatabaseInstance
        {
            Id = Guid.NewGuid(),
            RealmId = Guid.NewGuid(),
            Name = "db",
            Type = ManagedDatabaseType.MySQL
        };
        var tier = new DatabaseTier { Id = Guid.NewGuid(), Name = "t", CpuLimitCores = 1, MemoryLimitBytes = 1 };

        var p = ManagedDatabaseService.BuildCreateContainerParameters(
            instance, tier, "mysql:latest", "cloudios_realm_net", "/host/data", "pw");

        Assert.Contains("/host/data:/var/lib/mysql", p.HostConfig.Binds);
        Assert.True(p.NetworkingConfig.EndpointsConfig.ContainsKey("cloudios_realm_net"));
    }

    [Fact]
    public async Task ProvisionAsync_Throws_WhenInstanceNotFound()
    {
        var db = CreateInMemoryDb();
        var config = new ConfigurationBuilder().Build();
        var service = new ManagedDatabaseService(db, null!, new MockDockerNetworkService(), config,
            NullLogger<ManagedDatabaseService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ProvisionAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
