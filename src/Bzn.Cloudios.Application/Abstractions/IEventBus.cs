namespace Bzn.Cloudios.Application.Abstractions;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent evt, CancellationToken ct = default) where TEvent : notnull;
}
