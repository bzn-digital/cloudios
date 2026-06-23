namespace Bzn.Cloudios.Application.Abstractions;

public interface IManagedAppPortAllocator
{
    Task<int> AllocateNextPortAsync(CancellationToken ct = default);
}
