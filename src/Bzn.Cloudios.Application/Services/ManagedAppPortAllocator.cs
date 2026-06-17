using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bzn.Cloudios.Application.Services;

/// <summary>
/// Finds the next available host port in the managed app range.
/// Concurrency safety is provided by the unique index on
/// <c>ManagedAppInstance.HostPort</c>; callers should handle
/// <see cref="DbUpdateException"/> and retry when a concurrent
/// insert claims the same port.
/// </summary>
public sealed class ManagedAppPortAllocator : IManagedAppPortAllocator
{
    private const int MinPort = 2000;
    private const int MaxPort = 4500;
    private readonly CloudiosDbContext _context;

    public ManagedAppPortAllocator(CloudiosDbContext context)
    {
        _context = context;
    }

    public async Task<int> AllocateNextPortAsync(CancellationToken ct = default)
    {
        var usedPorts = await _context.ManagedAppInstances
            .Select(i => i.HostPort)
            .ToHashSetAsync(ct);

        for (int port = MinPort; port <= MaxPort; port++)
        {
            if (!usedPorts.Contains(port))
                return port;
        }

        throw new InvalidOperationException("No available ports in the managed app range (2000-4500).");
    }
}
