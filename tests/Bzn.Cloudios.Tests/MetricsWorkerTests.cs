using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bzn.Cloudios.Tests;

public class MetricsCollectionWorkerTests
{
    private static (CloudiosDbContext mainDb, MetricsDbContext metricsDb) CreateInMemoryDbs()
    {
        var mainOptions = new DbContextOptionsBuilder<CloudiosDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var metricsOptions = new DbContextOptionsBuilder<MetricsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        var mainDb = new CloudiosDbContext(mainOptions);
        var metricsDb = new MetricsDbContext(metricsOptions);

        mainDb.Database.OpenConnection();
        metricsDb.Database.OpenConnection();
        mainDb.Database.EnsureCreated();
        metricsDb.Database.EnsureCreated();

        return (mainDb, metricsDb);
    }

    [Fact]
    public async Task CollectAndStoreMetricsAsync_NoContainers_DoesNotInsert()
    {
        var (mainDb, metricsDb) = CreateInMemoryDbs();
        var dockerNetwork = new DockerNetworkServiceStub();
        var logger = NullLogger<MetricsCollectionWorker>.Instance;
        var worker = new MetricsCollectionWorker(dockerNetwork, new MockDbsScopeFactory(mainDb, metricsDb), logger, null);

        await worker.CollectAndStoreMetricsAsync(CancellationToken.None);

        var count = await metricsDb.ContainerMetricsHistory.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task CollectAndStoreMetricsAsync_WithContainer_InsertsMetrics()
    {
        var (mainDb, metricsDb) = CreateInMemoryDbs();
        var containerId = Guid.NewGuid();
        var realmId = Guid.NewGuid();
        var dockerContainerId = "abc123";

        mainDb.Realms.Add(new Realm { Id = realmId, Name = "Test Realm", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        mainDb.Containers.Add(new Container
        {
            Id = containerId,
            RealmId = realmId,
            Name = "test-container",
            ImageName = "nginx",
            InternalPort = 80,
            DockerContainerId = dockerContainerId,
            Status = ContainerStatus.Running,
            CreatedAt = DateTime.UtcNow
        });
        await mainDb.SaveChangesAsync();

        var dockerNetwork = new DockerNetworkServiceStub(new List<ContainerStats>
        {
            new ContainerStats
            {
                ContainerId = dockerContainerId,
                ContainerName = "test-container",
                CpuPercent = 25.5,
                MemoryUsedBytes = 512 * 1024 * 1024,
                NetworkRxBytes = 1024,
                NetworkTxBytes = 2048,
                BlockReadBytes = 4096,
                BlockWriteBytes = 8192
            }
        });

        var logger = NullLogger<MetricsCollectionWorker>.Instance;
        var worker = new MetricsCollectionWorker(dockerNetwork, new MockDbsScopeFactory(mainDb, metricsDb), logger, null);

        await worker.CollectAndStoreMetricsAsync(CancellationToken.None);

        var metrics = await metricsDb.ContainerMetricsHistory.ToListAsync();
        Assert.Single(metrics);
        Assert.Equal(containerId, metrics[0].ContainerId);
        Assert.Equal(25.5, metrics[0].CpuPercent);
        Assert.Equal(512 * 1024 * 1024, metrics[0].MemoryUsedBytes);
    }

    [Fact]
    public async Task CollectAndStoreMetricsAsync_MultipleContainers_BatchInsert()
    {
        var (mainDb, metricsDb) = CreateInMemoryDbs();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var realmId = Guid.NewGuid();

        mainDb.Realms.Add(new Realm { Id = realmId, Name = "Test Realm", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        mainDb.Containers.AddRange(
            new Container { Id = id1, RealmId = realmId, Name = "c1", ImageName = "nginx", InternalPort = 80, DockerContainerId = "d1", Status = ContainerStatus.Running, CreatedAt = DateTime.UtcNow },
            new Container { Id = id2, RealmId = realmId, Name = "c2", ImageName = "redis", InternalPort = 6379, DockerContainerId = "d2", Status = ContainerStatus.Running, CreatedAt = DateTime.UtcNow }
        );
        await mainDb.SaveChangesAsync();

        var dockerNetwork = new DockerNetworkServiceStub(new List<ContainerStats>
        {
            new ContainerStats { ContainerId = "d1", ContainerName = "c1", CpuPercent = 10, MemoryUsedBytes = 100, NetworkRxBytes = 1, NetworkTxBytes = 2, BlockReadBytes = 3, BlockWriteBytes = 4 },
            new ContainerStats { ContainerId = "d2", ContainerName = "c2", CpuPercent = 20, MemoryUsedBytes = 200, NetworkRxBytes = 5, NetworkTxBytes = 6, BlockReadBytes = 7, BlockWriteBytes = 8 }
        });

        var logger = NullLogger<MetricsCollectionWorker>.Instance;
        var worker = new MetricsCollectionWorker(dockerNetwork, new MockDbsScopeFactory(mainDb, metricsDb), logger, null);

        await worker.CollectAndStoreMetricsAsync(CancellationToken.None);

        var metrics = await metricsDb.ContainerMetricsHistory.ToListAsync();
        Assert.Equal(2, metrics.Count);
    }

    [Fact]
    public async Task CollectAndStoreMetricsAsync_ContainerNotInDb_Skips()
    {
        var (mainDb, metricsDb) = CreateInMemoryDbs();

        var dockerNetwork = new DockerNetworkServiceStub(new List<ContainerStats>
        {
            new ContainerStats { ContainerId = "unknown", ContainerName = "ghost", CpuPercent = 10, MemoryUsedBytes = 100, NetworkRxBytes = 1, NetworkTxBytes = 2, BlockReadBytes = 3, BlockWriteBytes = 4 }
        });

        var logger = NullLogger<MetricsCollectionWorker>.Instance;
        var worker = new MetricsCollectionWorker(dockerNetwork, new MockDbsScopeFactory(mainDb, metricsDb), logger, null);

        await worker.CollectAndStoreMetricsAsync(CancellationToken.None);

        var metrics = await metricsDb.ContainerMetricsHistory.ToListAsync();
        Assert.Empty(metrics);
    }
}

public class MetricsCleanupWorkerTests
{
    private static MetricsDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<MetricsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var db = new MetricsDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task CleanupOldMetricsAsync_DeletesOlderThan90Days()
    {
        var db = CreateInMemoryDb();
        var containerId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.ContainerMetricsHistory.AddRange(
            new ContainerMetricHistory { ContainerId = containerId, Timestamp = now.AddDays(-100), CpuPercent = 10, MemoryUsedBytes = 100, NetworkRxBytes = 1, NetworkTxBytes = 2, BlockReadBytes = 3, BlockWriteBytes = 4 },
            new ContainerMetricHistory { ContainerId = containerId, Timestamp = now.AddDays(-80), CpuPercent = 20, MemoryUsedBytes = 200, NetworkRxBytes = 5, NetworkTxBytes = 6, BlockReadBytes = 7, BlockWriteBytes = 8 }
        );
        await db.SaveChangesAsync();

        var logger = NullLogger<MetricsCleanupWorker>.Instance;
        var scopeFactory = new MockMetricsScopeFactory(db);
        var worker = new MetricsCleanupWorker(scopeFactory, logger);

        await worker.CleanupOldMetricsAsync(CancellationToken.None);

        var remaining = await db.ContainerMetricsHistory.ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(now.AddDays(-80).Date, remaining[0].Timestamp.Date);
    }

    [Fact]
    public async Task CleanupOldMetricsAsync_NoOldMetrics_DeletesNone()
    {
        var db = CreateInMemoryDb();
        var containerId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        db.ContainerMetricsHistory.Add(new ContainerMetricHistory
        {
            ContainerId = containerId,
            Timestamp = now.AddDays(-10),
            CpuPercent = 10,
            MemoryUsedBytes = 100,
            NetworkRxBytes = 1,
            NetworkTxBytes = 2,
            BlockReadBytes = 3,
            BlockWriteBytes = 4
        });
        await db.SaveChangesAsync();

        var logger = NullLogger<MetricsCleanupWorker>.Instance;
        var scopeFactory = new MockMetricsScopeFactory(db);
        var worker = new MetricsCleanupWorker(scopeFactory, logger);

        await worker.CleanupOldMetricsAsync(CancellationToken.None);

        var remaining = await db.ContainerMetricsHistory.ToListAsync();
        Assert.Single(remaining);
    }

    [Fact]
    public async Task CleanupOldMetricsAsync_EmptyDb_NoError()
    {
        var db = CreateInMemoryDb();
        var logger = NullLogger<MetricsCleanupWorker>.Instance;
        var scopeFactory = new MockMetricsScopeFactory(db);
        var worker = new MetricsCleanupWorker(scopeFactory, logger);

        await worker.CleanupOldMetricsAsync(CancellationToken.None);

        var count = await db.ContainerMetricsHistory.CountAsync();
        Assert.Equal(0, count);
    }
}

// Stub for IDockerNetworkService
public sealed class DockerNetworkServiceStub : IDockerNetworkService
{
    private readonly List<ContainerStats> _stats;

    public DockerNetworkServiceStub(List<ContainerStats>? stats = null)
    {
        _stats = stats ?? [];
    }

    public Task EnsureNetworkAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task EnsureRealmNetworkAsync(Guid realmId, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }

    public Task<List<string>> ListNetworksAsync(CancellationToken ct = default)
    {
        return Task.FromResult(new List<string>());
    }

    public Task<List<ContainerStats>> GetContainerStatsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_stats);
    }

    public Task<T?> SendRequestAsync<T>(string method, string path, string? body = null, CancellationToken ct = default)
    {
        return Task.FromResult(default(T));
    }

    public Task<List<ContainerLogEntry>> GetContainerLogsAsync(string dockerContainerId, int tail = 100, CancellationToken ct = default)
    {
        return Task.FromResult(new List<ContainerLogEntry>());
    }
}

public class MockMetricsScopeFactory : IServiceScopeFactory
{
    private readonly MetricsDbContext _db;

    public MockMetricsScopeFactory(MetricsDbContext db)
    {
        _db = db;
    }

    public IServiceScope CreateScope()
    {
        return new MockMetricsScope(_db);
    }
}

public class MockMetricsScope : IServiceScope
{
    private readonly MetricsDbContext _db;

    public MockMetricsScope(MetricsDbContext db)
    {
        _db = db;
        ServiceProvider = new MockMetricsServiceProvider(db);
    }

    public IServiceProvider ServiceProvider { get; }

    public void Dispose()
    {
    }
}

public class MockMetricsServiceProvider : IServiceProvider
{
    private readonly MetricsDbContext _db;

    public MockMetricsServiceProvider(MetricsDbContext db)
    {
        _db = db;
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(MetricsDbContext))
            return _db;
        throw new InvalidOperationException($"Service {serviceType.Name} not supported in mock");
    }
}

public class MockDbsScopeFactory : IServiceScopeFactory
{
    private readonly CloudiosDbContext _mainDb;
    private readonly MetricsDbContext _metricsDb;

    public MockDbsScopeFactory(CloudiosDbContext mainDb, MetricsDbContext metricsDb)
    {
        _mainDb = mainDb;
        _metricsDb = metricsDb;
    }

    public IServiceScope CreateScope()
    {
        return new MockDbsScope(_mainDb, _metricsDb);
    }
}

public class MockDbsScope : IServiceScope
{
    private readonly CloudiosDbContext _mainDb;
    private readonly MetricsDbContext _metricsDb;

    public MockDbsScope(CloudiosDbContext mainDb, MetricsDbContext metricsDb)
    {
        _mainDb = mainDb;
        _metricsDb = metricsDb;
        ServiceProvider = new MockDbsServiceProvider(mainDb, metricsDb);
    }

    public IServiceProvider ServiceProvider { get; }

    public void Dispose()
    {
    }
}

public class MockDbsServiceProvider : IServiceProvider
{
    private readonly CloudiosDbContext _mainDb;
    private readonly MetricsDbContext _metricsDb;

    public MockDbsServiceProvider(CloudiosDbContext mainDb, MetricsDbContext metricsDb)
    {
        _mainDb = mainDb;
        _metricsDb = metricsDb;
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(CloudiosDbContext))
            return _mainDb;
        if (serviceType == typeof(MetricsDbContext))
            return _metricsDb;
        throw new InvalidOperationException($"Service {serviceType.Name} not supported in mock");
    }
}
