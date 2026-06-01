using Bzn.Cloudios.Application.Events;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class BillingEventHandler
{
    private readonly ILogger<BillingEventHandler> _logger;

    public BillingEventHandler(ILogger<BillingEventHandler> logger)
    {
        _logger = logger;
    }

    public Task RegisterStartAsync(ContainerStartedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("Billing: Registering start for container {Name} in realm {RealmId}", evt.ContainerName, evt.RealmId);
        // TODO: Calculate billing start time
        return Task.CompletedTask;
    }

    public Task RegisterStopAsync(ContainerStoppedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("Billing: Registering stop for container {Name} in realm {RealmId}", evt.ContainerName, evt.RealmId);
        // TODO: Calculate billing end time and cost
        return Task.CompletedTask;
    }
}
