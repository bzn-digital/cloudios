using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Events;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

/// <summary>
/// Stub handler for YARP route events. The real implementation is in Bzn.Cloudios.WebAPI.
/// This exists so the Application layer can subscribe to events without depending on YARP.
/// </summary>
public sealed class YarpRouteHandler
{
    private readonly ILogger<YarpRouteHandler> _logger;

    public YarpRouteHandler(ILogger<YarpRouteHandler> logger)
    {
        _logger = logger;
    }

    public Task AddRouteAsync(ContainerStartedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("YARP stub: AddRoute for container {Name} (Id={Id})", evt.ContainerName, evt.ContainerId);
        return Task.CompletedTask;
    }

    public Task RemoveRouteAsync(ContainerStoppedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("YARP stub: RemoveRoute for container {Name} (Id={Id})", evt.ContainerName, evt.ContainerId);
        return Task.CompletedTask;
    }

    public Task RemoveRouteAsync(ContainerDeletedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("YARP stub: RemoveRoute for deleted container {Name} (Id={Id})", evt.ContainerName, evt.ContainerId);
        return Task.CompletedTask;
    }
}
