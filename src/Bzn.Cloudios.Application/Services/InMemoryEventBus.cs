using Bzn.Cloudios.Application.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace Bzn.Cloudios.Application.Services;

public sealed class EventEnvelope
{
    public string EventType { get; init; } = string.Empty;
    public object Payload { get; init; } = null!;
    public DateTime EnqueuedAt { get; init; } = DateTime.UtcNow;
}

public sealed class InProcessEventBus : IEventBus
{
    private readonly Channel<EventEnvelope> _channel;
    private readonly Dictionary<Type, List<Delegate>> _handlers = new();
    private readonly ILogger<InProcessEventBus> _logger;

    public InProcessEventBus(ILogger<InProcessEventBus> logger)
    {
        _logger = logger;
        _channel = Channel.CreateBounded<EventEnvelope>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public async Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct = default) where TEvent : notnull
    {
        var envelope = new EventEnvelope
        {
            EventType = typeof(TEvent).Name,
            Payload = evt
        };

        await _channel.Writer.WriteAsync(envelope, ct);
        _logger.LogDebug("Event published: {EventType}", envelope.EventType);
    }

    public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : notnull
    {
        var type = typeof(TEvent);
        if (!_handlers.TryGetValue(type, out var list))
        {
            list = new List<Delegate>();
            _handlers[type] = list;
        }
        list.Add(handler);
        _logger.LogInformation("Handler subscribed for {EventType}", type.Name);
    }

    public IAsyncEnumerable<EventEnvelope> ReadAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }

    public async Task DispatchAsync(EventEnvelope envelope, CancellationToken ct)
    {
        var payloadType = envelope.Payload.GetType();
        if (!_handlers.TryGetValue(payloadType, out var handlers))
        {
            _logger.LogDebug("No handlers registered for {EventType}", envelope.EventType);
            return;
        }

        var tasks = handlers.Select(async handler =>
        {
            try
            {
                var typedHandler = (Func<object, CancellationToken, Task>)handler;
                await typedHandler(envelope.Payload, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handler failed for {EventType}", envelope.EventType);
            }
        });

        await Task.WhenAll(tasks);
    }
}
