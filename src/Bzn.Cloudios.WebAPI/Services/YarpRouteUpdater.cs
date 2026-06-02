using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Events;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Yarp.ReverseProxy.Configuration;

namespace Bzn.Cloudios.WebAPI.Services;

public sealed class YarpRouteUpdater : IYarpRouteUpdater
{
    private readonly InMemoryConfigProvider _configProvider;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<YarpRouteUpdater> _logger;
    private readonly string _baseDomain;

    private readonly object _lock = new();
    private List<RouteConfig> _routes = [];
    private List<ClusterConfig> _clusters = [];

    public YarpRouteUpdater(
        InMemoryConfigProvider configProvider,
        IServiceScopeFactory scopeFactory,
        ILogger<YarpRouteUpdater> logger,
        string? baseDomain = null)
    {
        _configProvider = configProvider;
        _scopeFactory = scopeFactory;
        _logger = logger;
        _baseDomain = baseDomain ?? "cloudios.bzn.dev";
    }

    public Task AddRouteAsync(Guid containerId, string internalIp, int internalPort, string hostname, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var clusterId = $"cluster-{containerId}";
            var routeId = $"route-{containerId}";

            // Remove existing if present
            _clusters = _clusters.Where(c => c.ClusterId != clusterId).ToList();
            _routes = _routes.Where(r => r.RouteId != routeId).ToList();

            // Add cluster
            _clusters.Add(new ClusterConfig
            {
                ClusterId = clusterId,
                Destinations = new Dictionary<string, DestinationConfig>
                {
                    ["destination"] = new() { Address = $"http://{internalIp}:{internalPort}" }
                }
            });

            // Add route matching hostname
            _routes.Add(new RouteConfig
            {
                RouteId = routeId,
                ClusterId = clusterId,
                Match = new RouteMatch
                {
                    Hosts = [hostname]
                }
            });

            ApplyConfig();
        }

        _logger.LogInformation("YARP route added: {Hostname} -> {Ip}:{Port}", hostname, internalIp, internalPort);
        return Task.CompletedTask;
    }

    public Task RemoveRouteAsync(Guid containerId, CancellationToken ct = default)
    {
        lock (_lock)
        {
            var clusterId = $"cluster-{containerId}";
            var routeId = $"route-{containerId}";

            _clusters = _clusters.Where(c => c.ClusterId != clusterId).ToList();
            _routes = _routes.Where(r => r.RouteId != routeId).ToList();

            ApplyConfig();
        }

        _logger.LogInformation("YARP route removed for container {Id}", containerId);
        return Task.CompletedTask;
    }

    public string BuildHostname(string containerName, string realmSlug)
    {
        return $"{containerName}.{realmSlug}.{_baseDomain}";
    }

    // Event handlers for IEventBus subscription
    public async Task HandleContainerStartedAsync(ContainerStartedEvent evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CloudiosDbContext>();
        
        var container = await context.Containers.FindAsync([evt.ContainerId], ct);
        if (container is null)
        {
            _logger.LogWarning("Container {Id} not found in DB for route addition", evt.ContainerId);
            return;
        }

        var realm = await context.Realms.FindAsync([container.RealmId], ct);
        if (realm is null) return;

        var hostname = BuildHostname(container.Name, realm.Slug);

        if (container.DockerContainerId is null)
        {
            _logger.LogWarning("Container {Id} has no Docker container ID", evt.ContainerId);
            return;
        }

        // Use Docker network DNS name for container resolution
        var dnsName = $"cloudios-{container.Id:N}".Substring(0, Math.Min(32, $"cloudios-{container.Id:N}".Length));

        await AddRouteAsync(evt.ContainerId, dnsName, container.InternalPort, hostname, ct);
    }

    public Task HandleContainerStoppedAsync(ContainerStoppedEvent evt, CancellationToken ct)
    {
        return RemoveRouteAsync(evt.ContainerId, ct);
    }

    public Task HandleContainerDeletedAsync(ContainerDeletedEvent evt, CancellationToken ct)
    {
        return RemoveRouteAsync(evt.ContainerId, ct);
    }

    private void ApplyConfig()
    {
        _configProvider.Update(_routes, _clusters);
    }
}
