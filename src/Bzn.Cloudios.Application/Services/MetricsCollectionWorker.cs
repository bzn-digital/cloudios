using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class MetricsCollectionWorker : BackgroundService
{
    private readonly IDockerNetworkService _dockerNetwork;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MetricsCollectionWorker> _logger;

    public MetricsCollectionWorker(
        IDockerNetworkService dockerNetwork,
        IServiceScopeFactory scopeFactory,
        ILogger<MetricsCollectionWorker> logger)
    {
        _dockerNetwork = dockerNetwork;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MetricsCollectionWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CollectAndStoreMetricsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting metrics");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }

        _logger.LogInformation("MetricsCollectionWorker stopped");
    }

    public async Task CollectAndStoreMetricsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var mainDb = scope.ServiceProvider.GetRequiredService<CloudiosDbContext>();
        var metricsDb = scope.ServiceProvider.GetRequiredService<MetricsDbContext>();
        
        var stats = await _dockerNetwork.GetContainerStatsAsync(ct);
        if (stats.Count == 0)
        {
            _logger.LogDebug("No managed containers with stats");
            return;
        }

        // Map Docker container IDs to Cloudios container IDs
        var dockerToCloudiosMap = await mainDb.Containers
            .Where(c => c.DockerContainerId != null)
            .Select(c => new { c.DockerContainerId, c.Id })
            .ToDictionaryAsync(x => x.DockerContainerId!, x => x.Id, ct);

        var metricsToInsert = new List<ContainerMetricHistory>();
        var now = DateTime.UtcNow;

        foreach (var stat in stats)
        {
            if (!dockerToCloudiosMap.TryGetValue(stat.ContainerId, out var containerId))
            {
                _logger.LogDebug("Container {DockerId} not found in DB", stat.ContainerId);
                continue;
            }

            metricsToInsert.Add(new ContainerMetricHistory
            {
                ContainerId = containerId,
                Timestamp = now,
                CpuPercent = stat.CpuPercent,
                MemoryUsedBytes = stat.MemoryUsedBytes,
                NetworkRxBytes = stat.NetworkRxBytes,
                NetworkTxBytes = stat.NetworkTxBytes,
                BlockReadBytes = stat.BlockReadBytes,
                BlockWriteBytes = stat.BlockWriteBytes
            });
        }

        if (metricsToInsert.Count == 0) return;

        // Batch insert in single transaction
        await metricsDb.Database.BeginTransactionAsync(ct);
        try
        {
            await metricsDb.ContainerMetricsHistory.AddRangeAsync(metricsToInsert, ct);
            await metricsDb.SaveChangesAsync(ct);
            await metricsDb.Database.CommitTransactionAsync(ct);
            _logger.LogInformation("Collected and stored {Count} metrics", metricsToInsert.Count);
        }
        catch
        {
            await metricsDb.Database.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
