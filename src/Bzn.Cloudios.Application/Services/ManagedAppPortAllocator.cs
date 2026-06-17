using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bzn.Cloudios.Application.Services;

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
        const int maxRetries = 10;
        const int retryDelayMs = 50;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(ct);

            try
            {
                var usedPorts = await _context.ManagedAppInstances
                    .Select(i => i.HostPort)
                    .ToHashSetAsync(ct);

                for (int port = MinPort; port <= MaxPort; port++)
                {
                    if (!usedPorts.Contains(port))
                    {
                        await transaction.CommitAsync(ct);
                        return port;
                    }
                }

                await transaction.RollbackAsync(ct);
                throw new InvalidOperationException("No available ports in the managed app range (2000-4500).");
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(ct);
                if (attempt < maxRetries - 1)
                {
                    await Task.Delay(retryDelayMs, ct);
                    continue;
                }
                throw;
            }
        }

        throw new InvalidOperationException("Failed to allocate port after maximum retries due to concurrent conflicts.");
    }
}
