using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class BillingEventHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BillingEventHandler> _logger;

    public BillingEventHandler(IServiceScopeFactory scopeFactory, ILogger<BillingEventHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RegisterStartAsync(ContainerStartedEvent evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var billingService = scope.ServiceProvider.GetRequiredService<IBillingService>();
        
        _logger.LogInformation("Billing: Registering start for container {Name} in realm {RealmId}", evt.ContainerName, evt.RealmId);
        await billingService.RegisterStartAsync(evt.ContainerId, evt.OccurredAt, ct);
    }

    public async Task RegisterStopAsync(ContainerStoppedEvent evt, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var billingService = scope.ServiceProvider.GetRequiredService<IBillingService>();
        
        _logger.LogInformation("Billing: Registering stop for container {Name} in realm {RealmId}", evt.ContainerName, evt.RealmId);
        await billingService.RegisterStopAsync(evt.ContainerId, evt.OccurredAt, ct);
    }
}
