namespace Bzn.Cloudios.Application.Abstractions;

public interface IManagedAppDeployQueue
{
    void Enqueue(Guid instanceId);
    IAsyncEnumerable<Guid> DequeueAllAsync(CancellationToken ct);
}
