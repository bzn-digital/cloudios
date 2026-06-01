using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Events;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class InMemoryEventBus : IEventBus
{
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct = default) where TEvent : notnull
    {
        switch (evt)
        {
            case ContainerStartedEvent e:
                _logger.LogInformation("Event: ContainerStarted {ContainerId} in Realm {RealmId}", e.ContainerId, e.RealmId);
                break;
            case ContainerStoppedEvent e:
                _logger.LogInformation("Event: ContainerStopped {ContainerId} in Realm {RealmId}", e.ContainerId, e.RealmId);
                break;
            case ContainerDeletedEvent e:
                _logger.LogInformation("Event: ContainerDeleted {ContainerId} in Realm {RealmId}", e.ContainerId, e.RealmId);
                break;
            case ContainerFailedEvent e:
                _logger.LogError("Event: ContainerFailed {ContainerId} in Realm {RealmId}: {Error}", e.ContainerId, e.RealmId, e.ErrorMessage);
                break;
            default:
                _logger.LogInformation("Event: {EventType}", typeof(TEvent).Name);
                break;
        }

        return Task.CompletedTask;
    }
}
