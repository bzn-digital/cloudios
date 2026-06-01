using Bzn.Cloudios.Application.Events;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class YarpRouteHandler
{
    private readonly ILogger<YarpRouteHandler> _logger;

    public YarpRouteHandler(ILogger<YarpRouteHandler> logger)
    {
        _logger = logger;
    }

    public Task AddRouteAsync(ContainerStartedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("YARP: Adding route for container {Name} (Id={Id})", evt.ContainerName, evt.ContainerId);
        // TODO: Update YARP config/proxy routes dynamically
        return Task.CompletedTask;
    }

    public Task RemoveRouteAsync(ContainerStoppedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("YARP: Removing route for container {Name} (Id={Id})", evt.ContainerName, evt.ContainerId);
        return Task.CompletedTask;
    }

    public Task RemoveRouteAsync(ContainerDeletedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("YARP: Removing route for deleted container {Name} (Id={Id})", evt.ContainerName, evt.ContainerId);
        return Task.CompletedTask;
    }
}
