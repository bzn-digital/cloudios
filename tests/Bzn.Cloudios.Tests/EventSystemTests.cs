using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Events;
using Bzn.Cloudios.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bzn.Cloudios.Tests;

public class InProcessEventBusTests
{
    private static InProcessEventBus CreateBus()
        => new(NullLogger<InProcessEventBus>.Instance);

    [Fact]
    public async Task PublishAsync_WithNoHandlers_CompletesWithoutError()
    {
        var bus = CreateBus();
        var evt = new ContainerStartedEvent(Guid.NewGuid(), Guid.NewGuid(), "test", DateTime.UtcNow);
        await bus.PublishAsync(evt);
        // No exception = pass
    }

    [Fact]
    public async Task PublishAsync_DispatchesToSubscribedHandler()
    {
        var bus = CreateBus();
        var received = new List<ContainerStartedEvent>();

        bus.Subscribe<ContainerStartedEvent>((e, ct) =>
        {
            received.Add(e);
            return Task.CompletedTask;
        });

        var evt = new ContainerStartedEvent(Guid.NewGuid(), Guid.NewGuid(), "test", DateTime.UtcNow);
        await bus.PublishAsync(evt);

        // Read from channel and dispatch
        await foreach (var envelope in bus.ReadAllAsync(CancellationToken.None))
        {
            await bus.DispatchAsync(envelope, CancellationToken.None);
            break;
        }

        Assert.Single(received);
        Assert.Equal(evt.ContainerId, received[0].ContainerId);
        Assert.Equal(evt.ContainerName, received[0].ContainerName);
    }

    [Fact]
    public async Task PublishAsync_MultipleHandlersForSameEvent_AllExecutedInParallel()
    {
        var bus = CreateBus();
        var handler1Called = false;
        var handler2Called = false;

        bus.Subscribe<ContainerStoppedEvent>((e, ct) =>
        {
            handler1Called = true;
            return Task.CompletedTask;
        });

        bus.Subscribe<ContainerStoppedEvent>((e, ct) =>
        {
            handler2Called = true;
            return Task.CompletedTask;
        });

        var evt = new ContainerStoppedEvent(Guid.NewGuid(), Guid.NewGuid(), "test", DateTime.UtcNow);
        await bus.PublishAsync(evt);

        await foreach (var envelope in bus.ReadAllAsync(CancellationToken.None))
        {
            await bus.DispatchAsync(envelope, CancellationToken.None);
            break;
        }

        Assert.True(handler1Called);
        Assert.True(handler2Called);
    }

    [Fact]
    public async Task DispatchAsync_HandlerThrows_OtherHandlersContinue()
    {
        var bus = CreateBus();
        var handler2Called = false;

        bus.Subscribe<ContainerDeletedEvent>((e, ct) => throw new InvalidOperationException("boom"));
        bus.Subscribe<ContainerDeletedEvent>((e, ct) =>
        {
            handler2Called = true;
            return Task.CompletedTask;
        });

        var evt = new ContainerDeletedEvent(Guid.NewGuid(), Guid.NewGuid(), "test", DateTime.UtcNow);
        await bus.PublishAsync(evt);

        await foreach (var envelope in bus.ReadAllAsync(CancellationToken.None))
        {
            await bus.DispatchAsync(envelope, CancellationToken.None);
            break;
        }

        Assert.True(handler2Called, "Second handler should still execute even if first throws");
    }

    [Fact]
    public async Task Subscribe_DifferentEventTypes_OnlyMatchingHandlersCalled()
    {
        var bus = CreateBus();
        var startedReceived = false;
        var stoppedReceived = false;

        bus.Subscribe<ContainerStartedEvent>((e, ct) =>
        {
            startedReceived = true;
            return Task.CompletedTask;
        });

        bus.Subscribe<ContainerStoppedEvent>((e, ct) =>
        {
            stoppedReceived = true;
            return Task.CompletedTask;
        });

        var evt = new ContainerStartedEvent(Guid.NewGuid(), Guid.NewGuid(), "test", DateTime.UtcNow);
        await bus.PublishAsync(evt);

        await foreach (var envelope in bus.ReadAllAsync(CancellationToken.None))
        {
            await bus.DispatchAsync(envelope, CancellationToken.None);
            break;
        }

        Assert.True(startedReceived);
        Assert.False(stoppedReceived);
    }
}

public class DomainEventTests
{
    [Fact]
    public void ContainerStartedEvent_RecordEquality()
    {
        var id = Guid.NewGuid();
        var realmId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt1 = new ContainerStartedEvent(id, realmId, "app", now);
        var evt2 = new ContainerStartedEvent(id, realmId, "app", now);

        Assert.Equal(evt1, evt2);
    }

    [Fact]
    public void ContainerFailedEvent_ContainsErrorMessage()
    {
        var evt = new ContainerFailedEvent(Guid.NewGuid(), Guid.NewGuid(), "app", "docker timeout", DateTime.UtcNow);

        Assert.Equal("docker timeout", evt.ErrorMessage);
        Assert.Equal("app", evt.ContainerName);
    }

    [Fact]
    public void RealmBlockedEvent_RecordProperties()
    {
        var realmId = Guid.NewGuid();
        var evt = new RealmBlockedEvent(realmId, "acme", true, DateTime.UtcNow);

        Assert.Equal(realmId, evt.RealmId);
        Assert.Equal("acme", evt.RealmName);
        Assert.True(evt.IsBlocked);
    }

    [Fact]
    public void ContainerDeletedEvent_RecordEquality()
    {
        var id = Guid.NewGuid();
        var realmId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var evt1 = new ContainerDeletedEvent(id, realmId, "app", now);
        var evt2 = new ContainerDeletedEvent(id, realmId, "app", now);

        Assert.Equal(evt1, evt2);
    }
}

public class EventEnvelopeTests
{
    [Fact]
    public void EventEnvelope_Defaults()
    {
        var envelope = new EventEnvelope();
        Assert.Equal(string.Empty, envelope.EventType);
        Assert.Null(envelope.Payload);
        Assert.NotEqual(default, envelope.EnqueuedAt);
    }

    [Fact]
    public void EventEnvelope_WithPayload_SetsProperties()
    {
        var evt = new ContainerStartedEvent(Guid.NewGuid(), Guid.NewGuid(), "app", DateTime.UtcNow);
        var envelope = new EventEnvelope
        {
            EventType = nameof(ContainerStartedEvent),
            Payload = evt,
            EnqueuedAt = DateTime.UtcNow
        };

        Assert.Equal(nameof(ContainerStartedEvent), envelope.EventType);
        Assert.Same(evt, envelope.Payload);
    }
}

public class YarpRouteHandlerTests
{
    [Fact]
    public async Task AddRouteAsync_CompletesWithoutError()
    {
        var handler = new YarpRouteHandler(NullLogger<YarpRouteHandler>.Instance);
        var evt = new ContainerStartedEvent(Guid.NewGuid(), Guid.NewGuid(), "app", DateTime.UtcNow);
        await handler.AddRouteAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task RemoveRouteAsync_StoppedEvent_CompletesWithoutError()
    {
        var handler = new YarpRouteHandler(NullLogger<YarpRouteHandler>.Instance);
        var evt = new ContainerStoppedEvent(Guid.NewGuid(), Guid.NewGuid(), "app", DateTime.UtcNow);
        await handler.RemoveRouteAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task RemoveRouteAsync_DeletedEvent_CompletesWithoutError()
    {
        var handler = new YarpRouteHandler(NullLogger<YarpRouteHandler>.Instance);
        var evt = new ContainerDeletedEvent(Guid.NewGuid(), Guid.NewGuid(), "app", DateTime.UtcNow);
        await handler.RemoveRouteAsync(evt, CancellationToken.None);
    }
}

public class BillingEventHandlerTests
{
    [Fact]
    public async Task RegisterStartAsync_CompletesWithoutError()
    {
        var handler = new BillingEventHandler(NullLogger<BillingEventHandler>.Instance);
        var evt = new ContainerStartedEvent(Guid.NewGuid(), Guid.NewGuid(), "app", DateTime.UtcNow);
        await handler.RegisterStartAsync(evt, CancellationToken.None);
    }

    [Fact]
    public async Task RegisterStopAsync_CompletesWithoutError()
    {
        var handler = new BillingEventHandler(NullLogger<BillingEventHandler>.Instance);
        var evt = new ContainerStoppedEvent(Guid.NewGuid(), Guid.NewGuid(), "app", DateTime.UtcNow);
        await handler.RegisterStopAsync(evt, CancellationToken.None);
    }
}
