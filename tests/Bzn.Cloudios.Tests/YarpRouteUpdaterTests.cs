using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Events;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Bzn.Cloudios.WebAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Yarp.ReverseProxy.Configuration;

namespace Bzn.Cloudios.Tests;

public class YarpRouteUpdaterTests
{
    private static (YarpRouteUpdater updater, InMemoryConfigProvider provider) CreateUpdater(CloudiosDbContext? context = null, string? baseDomain = null)
    {
        var provider = new InMemoryConfigProvider([], []);
        var db = context ?? CreateInMemoryDb();
        var logger = NullLogger<YarpRouteUpdater>.Instance;
        
        var scopeFactory = new MockScopeFactory(db);
        var updater = new YarpRouteUpdater(provider, scopeFactory, logger, baseDomain);
        return (updater, provider);
    }

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
    public void BuildHostname_CorrectFormat()
    {
        var (updater, _) = CreateUpdater();
        var hostname = updater.BuildHostname("web-api", "acme");
        Assert.Equal("web-api.acme.cloudios.bzn.dev", hostname);
    }

    [Fact]
    public void BuildHostname_CustomBaseDomain()
    {
        var (updater, _) = CreateUpdater(baseDomain: "custom.example.com");
        var hostname = updater.BuildHostname("app", "myrealm");
        Assert.Equal("app.myrealm.custom.example.com", hostname);
    }

    [Fact]
    public async Task AddRouteAsync_CreatesRouteAndCluster()
    {
        var (updater, provider) = CreateUpdater();
        var containerId = Guid.NewGuid();

        await updater.AddRouteAsync(containerId, "192.168.1.10", 8080, "app.acme.cloudios.bzn.dev");

        var config = provider.GetConfig();
        Assert.Single(config.Routes);
        Assert.Single(config.Clusters);

        var route = config.Routes[0];
        Assert.Equal($"route-{containerId}", route.RouteId);
        Assert.Equal($"cluster-{containerId}", route.ClusterId);
        Assert.Contains("app.acme.cloudios.bzn.dev", route.Match.Hosts!);

        var cluster = config.Clusters[0];
        Assert.Equal($"cluster-{containerId}", cluster.ClusterId);
        Assert.Single(cluster.Destinations!);
        Assert.Equal("http://192.168.1.10:8080", cluster.Destinations!["destination"].Address);
    }

    [Fact]
    public async Task RemoveRouteAsync_RemovesRouteAndCluster()
    {
        var (updater, provider) = CreateUpdater();
        var containerId = Guid.NewGuid();

        await updater.AddRouteAsync(containerId, "192.168.1.10", 8080, "app.acme.cloudios.bzn.dev");
        await updater.RemoveRouteAsync(containerId);

        var config = provider.GetConfig();
        Assert.Empty(config.Routes);
        Assert.Empty(config.Clusters);
    }

    [Fact]
    public async Task AddRouteAsync_SameContainerId_ReplacesExisting()
    {
        var (updater, provider) = CreateUpdater();
        var containerId = Guid.NewGuid();

        await updater.AddRouteAsync(containerId, "192.168.1.10", 8080, "old.cloudios.bzn.dev");
        await updater.AddRouteAsync(containerId, "192.168.1.20", 9090, "new.cloudios.bzn.dev");

        var config = provider.GetConfig();
        Assert.Single(config.Routes);
        Assert.Single(config.Clusters);

        Assert.Equal("http://192.168.1.20:9090", config.Clusters[0].Destinations!["destination"].Address);
        Assert.Contains("new.cloudios.bzn.dev", config.Routes[0].Match.Hosts!);
    }

    [Fact]
    public async Task AddRouteAsync_MultipleContainers_CreatesMultipleRoutes()
    {
        var (updater, provider) = CreateUpdater();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await updater.AddRouteAsync(id1, "10.0.0.1", 80, "app1.acme.cloudios.bzn.dev");
        await updater.AddRouteAsync(id2, "10.0.0.2", 80, "app2.acme.cloudios.bzn.dev");

        var config = provider.GetConfig();
        Assert.Equal(2, config.Routes.Count);
        Assert.Equal(2, config.Clusters.Count);
    }

    [Fact]
    public async Task RemoveRouteAsync_OnlyRemovesTargetContainer()
    {
        var (updater, provider) = CreateUpdater();
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        await updater.AddRouteAsync(id1, "10.0.0.1", 80, "app1.acme.cloudios.bzn.dev");
        await updater.AddRouteAsync(id2, "10.0.0.2", 80, "app2.acme.cloudios.bzn.dev");
        await updater.RemoveRouteAsync(id1);

        var config = provider.GetConfig();
        Assert.Single(config.Routes);
        Assert.Single(config.Clusters);
        Assert.Equal($"route-{id2}", config.Routes[0].RouteId);
    }

    [Fact]
    public async Task HandleContainerStoppedAsync_RemovesRoute()
    {
        var (updater, provider) = CreateUpdater();
        var containerId = Guid.NewGuid();

        await updater.AddRouteAsync(containerId, "10.0.0.1", 80, "app.acme.cloudios.bzn.dev");

        var evt = new ContainerStoppedEvent(containerId, Guid.NewGuid(), "app", DateTime.UtcNow);
        await updater.HandleContainerStoppedAsync(evt, CancellationToken.None);

        var config = provider.GetConfig();
        Assert.Empty(config.Routes);
    }

    [Fact]
    public async Task HandleContainerDeletedAsync_RemovesRoute()
    {
        var (updater, provider) = CreateUpdater();
        var containerId = Guid.NewGuid();

        await updater.AddRouteAsync(containerId, "10.0.0.1", 80, "app.acme.cloudios.bzn.dev");

        var evt = new ContainerDeletedEvent(containerId, Guid.NewGuid(), "app", DateTime.UtcNow);
        await updater.HandleContainerDeletedAsync(evt, CancellationToken.None);

        var config = provider.GetConfig();
        Assert.Empty(config.Routes);
    }

    [Fact]
    public async Task HandleContainerStartedAsync_AddsRouteFromDbData()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var containerId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Acme Corp", Slug = "acme", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.Containers.Add(new Container
        {
            Id = containerId,
            RealmId = realmId,
            Name = "web-api",
            ImageName = "nginx",
            InternalPort = 80,
            DockerContainerId = "abc123",
            Status = ContainerStatus.Running,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var (updater, provider) = CreateUpdater(db);
        var evt = new ContainerStartedEvent(containerId, realmId, "web-api", DateTime.UtcNow);

        await updater.HandleContainerStartedAsync(evt, CancellationToken.None);

        var config = provider.GetConfig();
        Assert.Single(config.Routes);
        Assert.Contains("web-api.acme.cloudios.bzn.dev", config.Routes[0].Match.Hosts!);
    }

    [Fact]
    public async Task HandleContainerStartedAsync_ContainerNotFound_NoRouteAdded()
    {
        var (updater, provider) = CreateUpdater();
        var evt = new ContainerStartedEvent(Guid.NewGuid(), Guid.NewGuid(), "ghost", DateTime.UtcNow);

        await updater.HandleContainerStartedAsync(evt, CancellationToken.None);

        var config = provider.GetConfig();
        Assert.Empty(config.Routes);
    }
}

public class IYarpRouteUpdaterInterfaceTests
{
    [Fact]
    public void IYarpRouteUpdater_HasRequiredMethods()
    {
        var interfaceType = typeof(IYarpRouteUpdater);
        var addRoute = interfaceType.GetMethod("AddRouteAsync");
        var removeRoute = interfaceType.GetMethod("RemoveRouteAsync");
        var buildHostname = interfaceType.GetMethod("BuildHostname");

        Assert.NotNull(addRoute);
        Assert.NotNull(removeRoute);
        Assert.NotNull(buildHostname);

        Assert.Equal(typeof(Task), addRoute!.ReturnType);
        Assert.Equal(typeof(Task), removeRoute!.ReturnType);
        Assert.Equal(typeof(string), buildHostname!.ReturnType);
    }
}

public class MockScopeFactory : IServiceScopeFactory
{
    private readonly CloudiosDbContext _db;

    public MockScopeFactory(CloudiosDbContext db)
    {
        _db = db;
    }

    public IServiceScope CreateScope()
    {
        return new MockScope(_db);
    }
}

public class MockScope : IServiceScope
{
    private readonly CloudiosDbContext _db;

    public MockScope(CloudiosDbContext db)
    {
        _db = db;
        ServiceProvider = new MockServiceProvider(db);
    }

    public IServiceProvider ServiceProvider { get; }

    public void Dispose()
    {
    }
}

public class MockServiceProvider : IServiceProvider
{
    private readonly CloudiosDbContext _db;

    public MockServiceProvider(CloudiosDbContext db)
    {
        _db = db;
    }

    public object? GetService(Type serviceType)
    {
        if (serviceType == typeof(CloudiosDbContext))
            return _db;
        throw new InvalidOperationException($"Service {serviceType.Name} not supported in mock");
    }
}
