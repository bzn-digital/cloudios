using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class MetricsCleanupWorker : BackgroundService
{
    private readonly MetricsDbContext _metricsDb;
    private readonly ILogger<MetricsCleanupWorker> _logger;

    public MetricsCleanupWorker(MetricsDbContext metricsDb, ILogger<MetricsCleanupWorker> logger)
    {
        _metricsDb = metricsDb;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("MetricsCleanupWorker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldMetricsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up old metrics");
            }

            // Calculate delay until next 03:00 UTC
            var now = DateTime.UtcNow;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, 3, 0, 0, DateTimeKind.Utc);
            if (now > nextRun)
                nextRun = nextRun.AddDays(1);

            var delay = nextRun - now;
            _logger.LogInformation("Next cleanup scheduled for {NextRun} UTC (in {Delay})", nextRun, delay);
            await Task.Delay(delay, stoppingToken);
        }

        _logger.LogInformation("MetricsCleanupWorker stopped");
    }

    public async Task CleanupOldMetricsAsync(CancellationToken ct)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-90);

        var deletedCount = await _metricsDb.ContainerMetricsHistory
            .Where(m => m.Timestamp < cutoffDate)
            .ExecuteDeleteAsync(ct);

        if (deletedCount > 0)
        {
            _logger.LogInformation("Deleted {Count} old metrics records (older than 90 days)", deletedCount);
        }

        // Run PRAGMA optimize to rebuild SQLite database
        await _metricsDb.Database.ExecuteSqlRawAsync("PRAGMA optimize;", ct);
        _logger.LogInformation("SQLite database optimized");
    }
}
