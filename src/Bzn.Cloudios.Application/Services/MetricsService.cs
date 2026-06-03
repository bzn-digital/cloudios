using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bzn.Cloudios.Application.Services;

public sealed class MetricsService
{
    private readonly MetricsDbContext _metricsDb;
    private readonly CloudiosDbContext _mainDb;

    public MetricsService(MetricsDbContext metricsDb, CloudiosDbContext mainDb)
    {
        _metricsDb = metricsDb;
        _mainDb = mainDb;
    }

    public async Task<ContainerMetricsResponse?> GetContainerMetricsAsync(Guid containerId, DateTime? from = null, DateTime? to = null, CancellationToken ct = default)
    {
        var container = await _mainDb.Containers.FindAsync([containerId], ct);
        if (container is null) return null;

        var query = _metricsDb.ContainerMetricsHistory
            .Where(m => m.ContainerId == containerId);

        if (from.HasValue)
            query = query.Where(m => m.Timestamp >= from.Value);
        if (to.HasValue)
            query = query.Where(m => m.Timestamp <= to.Value);

        var metrics = await query
            .OrderBy(m => m.Timestamp)
            .Select(m => new MetricDataPoint
            {
                Timestamp = m.Timestamp,
                CpuPercent = m.CpuPercent,
                MemoryUsedBytes = m.MemoryUsedBytes,
                NetworkRxBytes = m.NetworkRxBytes,
                NetworkTxBytes = m.NetworkTxBytes
            })
            .ToListAsync(ct);

        return new ContainerMetricsResponse
        {
            ContainerId = containerId,
            From = from ?? DateTime.MinValue,
            To = to ?? DateTime.UtcNow,
            DataPoints = metrics
        };
    }

    public async Task<HostMetricsResponse> GetHostMetricsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var oneHourAgo = now.AddHours(-1);

        var recentMetrics = await _metricsDb.ContainerMetricsHistory
            .Where(m => m.Timestamp >= oneHourAgo)
            .GroupBy(m => m.ContainerId)
            .Select(g => new
            {
                AvgCpu = g.Average(m => m.CpuPercent),
                AvgMemory = g.Average(m => m.MemoryUsedBytes),
                TotalRx = g.Sum(m => m.NetworkRxBytes),
                TotalTx = g.Sum(m => m.NetworkTxBytes)
            })
            .ToListAsync(ct);

        var totalCpu = recentMetrics.Sum(m => m.AvgCpu);
        var totalMemory = recentMetrics.Sum(m => m.AvgMemory);
        var totalRx = recentMetrics.Sum(m => m.TotalRx);
        var totalTx = recentMetrics.Sum(m => m.TotalTx);
        var activeContainers = recentMetrics.Count;

        return new HostMetricsResponse
        {
            TotalCpuPercent = totalCpu,
            TotalMemoryUsedBytes = (long)totalMemory,
            TotalMemoryTotalBytes = 16L * 1024 * 1024 * 1024, // 16GB stub
            ActiveContainers = activeContainers,
            DiskUsedBytes = 100L * 1024 * 1024 * 1024, // 100GB stub
            DiskTotalBytes = 500L * 1024 * 1024 * 1024 // 500GB stub
        };
    }

    public async Task<RealmMetricsResponse> GetRealmMetricsAsync(Guid realmId, DateTime from, DateTime to, CancellationToken ct = default)
    {
        var containers = await _mainDb.Containers
            .Where(c => c.RealmId == realmId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var metrics = await _metricsDb.ContainerMetricsHistory
            .Where(m => containers.Contains(m.ContainerId) && m.Timestamp >= from && m.Timestamp <= to)
            .OrderBy(m => m.Timestamp)
            .Select(m => new MetricDataPoint
            {
                Timestamp = m.Timestamp,
                CpuPercent = m.CpuPercent,
                MemoryUsedBytes = m.MemoryUsedBytes,
                NetworkRxBytes = m.NetworkRxBytes,
                NetworkTxBytes = m.NetworkTxBytes
            })
            .ToListAsync(ct);

        return new RealmMetricsResponse
        {
            RealmId = realmId,
            From = from,
            To = to,
            DataPoints = metrics
        };
    }
}
