using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class MetricsCollectionWorker : BackgroundService
{
    private readonly IDockerNetworkService _dockerNetwork;
    private readonly CloudiosDbContext _mainDb;
    private readonly MetricsDbContext _metricsDb;
    private readonly ILogger<MetricsCollectionWorker> _logger;

    public MetricsCollectionWorker(
        IDockerNetworkService dockerNetwork,
        CloudiosDbContext mainDb,
        MetricsDbContext metricsDb,
        ILogger<MetricsCollectionWorker> logger)
    {
        _dockerNetwork = dockerNetwork;
        _mainDb = mainDb;
        _metricsDb = metricsDb;
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
        var stats = await _dockerNetwork.GetContainerStatsAsync(ct);
        if (stats.Count == 0)
        {
            _logger.LogDebug("No managed containers with stats");
            return;
        }

        // Map Docker container IDs to Cloudios container IDs
        var dockerToCloudiosMap = await _mainDb.Containers
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
        await _metricsDb.Database.BeginTransactionAsync(ct);
        try
        {
            await _metricsDb.ContainerMetricsHistory.AddRangeAsync(metricsToInsert, ct);
            await _metricsDb.SaveChangesAsync(ct);
            await _metricsDb.Database.CommitTransactionAsync(ct);
            _logger.LogInformation("Collected and stored {Count} metrics", metricsToInsert.Count);
        }
        catch
        {
            await _metricsDb.Database.RollbackTransactionAsync(ct);
            throw;
        }
    }
}
