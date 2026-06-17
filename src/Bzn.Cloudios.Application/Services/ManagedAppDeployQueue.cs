using System.Threading.Channels;
using Bzn.Cloudios.Application.Abstractions;

namespace Bzn.Cloudios.Application.Services;

public sealed class ManagedAppDeployQueue : IManagedAppDeployQueue
{
    private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>(
        new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

    public void Enqueue(Guid instanceId)
    {
        _channel.Writer.TryWrite(instanceId);
    }

    public IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct)
    {
        return _channel.Reader.ReadAllAsync(ct);
    }
}
