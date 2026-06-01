using Bzn.Cloudios.Application.Abstractions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class EventProcessorWorker : BackgroundService
{
    private readonly InProcessEventBus _eventBus;
    private readonly ILogger<EventProcessorWorker> _logger;

    public EventProcessorWorker(IEventBus eventBus, ILogger<EventProcessorWorker> logger)
    {
        _eventBus = (InProcessEventBus)eventBus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EventProcessorWorker started");

        await foreach (var envelope in _eventBus.ReadAllAsync(stoppingToken))
        {
            try
            {
                await _eventBus.DispatchAsync(envelope, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error dispatching event {EventType}", envelope.EventType);
            }
        }

        _logger.LogInformation("EventProcessorWorker stopped");
    }
}
