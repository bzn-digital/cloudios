using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Events;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class BillingEventHandler
{
    private readonly IBillingService _billingService;
    private readonly ILogger<BillingEventHandler> _logger;

    public BillingEventHandler(IBillingService billingService, ILogger<BillingEventHandler> logger)
    {
        _billingService = billingService;
        _logger = logger;
    }

    public async Task RegisterStartAsync(ContainerStartedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("Billing: Registering start for container {Name} in realm {RealmId}", evt.ContainerName, evt.RealmId);
        await _billingService.RegisterStartAsync(evt.ContainerId, evt.OccurredAt, ct);
    }

    public async Task RegisterStopAsync(ContainerStoppedEvent evt, CancellationToken ct)
    {
        _logger.LogInformation("Billing: Registering stop for container {Name} in realm {RealmId}", evt.ContainerName, evt.RealmId);
        await _billingService.RegisterStopAsync(evt.ContainerId, evt.OccurredAt, ct);
    }
}
