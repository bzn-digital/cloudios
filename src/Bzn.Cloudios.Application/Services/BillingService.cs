using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class BillingService : IBillingService
{
    private readonly CloudiosDbContext _db;
    private readonly ILogger<BillingService> _logger;

    public BillingService(CloudiosDbContext db, ILogger<BillingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task RegisterStartAsync(Guid containerId, DateTime startedAtUtc, CancellationToken ct = default)
    {
        var container = await _db.Containers.FindAsync([containerId], ct);
        if (container is null)
        {
            _logger.LogWarning("Container {ContainerId} not found for billing start", containerId);
            return;
        }

        var period = new BillingPeriod
        {
            ContainerId = containerId,
            StartedAtUtc = startedAtUtc,
            StoppedAtUtc = null,
            Hours = 0,
            CostBRL = 0
        };

        _db.BillingPeriods.Add(period);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Registered billing start for container {ContainerId} at {StartedAt}", containerId, startedAtUtc);
    }

    public async Task RegisterStopAsync(Guid containerId, DateTime stoppedAtUtc, CancellationToken ct = default)
    {
        var container = await _db.Containers.FindAsync([containerId], ct);
        if (container is null)
        {
            _logger.LogWarning("Container {ContainerId} not found for billing stop", containerId);
            return;
        }

        var activePeriod = await _db.BillingPeriods
            .Where(b => b.ContainerId == containerId && b.StoppedAtUtc == null)
            .OrderByDescending(b => b.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (activePeriod is null)
        {
            _logger.LogWarning("No active billing period found for container {ContainerId}", containerId);
            return;
        }

        var hours = (stoppedAtUtc - activePeriod.StartedAtUtc).TotalHours;
        var cost = (decimal)hours * container.CostPerHourBRL;

        activePeriod.StoppedAtUtc = stoppedAtUtc;
        activePeriod.Hours = hours;
        activePeriod.CostBRL = cost;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Registered billing stop for container {ContainerId}: {Hours:F2}h = R${Cost:F2}", containerId, hours, cost);
    }

    public async Task RegisterDatabaseStartAsync(Guid managedDatabaseId, DateTime startedAtUtc, CancellationToken ct = default)
    {
        var instance = await _db.ManagedDatabaseInstances.FindAsync([managedDatabaseId], ct);
        if (instance is null)
        {
            _logger.LogWarning("Managed database {DatabaseId} not found for billing start", managedDatabaseId);
            return;
        }

        var period = new BillingPeriod
        {
            ManagedDatabaseId = managedDatabaseId,
            StartedAtUtc = startedAtUtc,
            StoppedAtUtc = null,
            Hours = 0,
            CostBRL = 0
        };

        _db.BillingPeriods.Add(period);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Registered billing start for managed database {DatabaseId} at {StartedAt}", managedDatabaseId, startedAtUtc);
    }

    public async Task RegisterDatabaseStopAsync(Guid managedDatabaseId, DateTime stoppedAtUtc, CancellationToken ct = default)
    {
        var instance = await _db.ManagedDatabaseInstances.FindAsync([managedDatabaseId], ct);
        if (instance is null)
        {
            _logger.LogWarning("Managed database {DatabaseId} not found for billing stop", managedDatabaseId);
            return;
        }

        var activePeriod = await _db.BillingPeriods
            .Where(b => b.ManagedDatabaseId == managedDatabaseId && b.StoppedAtUtc == null)
            .OrderByDescending(b => b.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (activePeriod is null)
        {
            _logger.LogWarning("No active billing period found for managed database {DatabaseId}", managedDatabaseId);
            return;
        }

        var hours = (stoppedAtUtc - activePeriod.StartedAtUtc).TotalHours;
        var hourlyRate = ManagedDatabasePricing.HourlyRateBRL(instance.CpuLimit, instance.MemoryLimit, instance.Type);
        var cost = (decimal)hours * hourlyRate;

        activePeriod.StoppedAtUtc = stoppedAtUtc;
        activePeriod.Hours = hours;
        activePeriod.CostBRL = cost;

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Registered billing stop for managed database {DatabaseId}: {Hours:F2}h = R${Cost:F2}", managedDatabaseId, hours, cost);
    }

    public async Task<decimal> GetRealmBillingAsync(Guid realmId, int year, int month, CancellationToken ct = default)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);
        var now = DateTime.UtcNow;

        var containerTotal = await _db.BillingPeriods
            .Join(_db.Containers, b => b.ContainerId, c => (Guid?)c.Id, (b, c) => new { b, c })
            .Where(x => x.c.RealmId == realmId)
            .Where(x => x.b.StartedAtUtc >= startDate && x.b.StartedAtUtc < endDate)
            .SumAsync(x => x.b.CostBRL, ct);
        containerTotal += await SumActiveContainerCostAsync(realmId, startDate, endDate, now, ct);

        var databaseTotal = await _db.BillingPeriods
            .Join(_db.ManagedDatabaseInstances, b => b.ManagedDatabaseId, d => (Guid?)d.Id, (b, d) => new { b, d })
            .Where(x => x.d.RealmId == realmId)
            .Where(x => x.b.StartedAtUtc >= startDate && x.b.StartedAtUtc < endDate)
            .SumAsync(x => x.b.CostBRL, ct);
        databaseTotal += await SumActiveDatabaseCostAsync(realmId, startDate, endDate, now, ct);

        return containerTotal + databaseTotal;
    }

    public async Task<decimal> GetGlobalBillingAsync(int year, int month, CancellationToken ct = default)
    {
        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);
        var now = DateTime.UtcNow;

        var total = await _db.BillingPeriods
            .Where(b => b.StartedAtUtc >= startDate && b.StartedAtUtc < endDate)
            .SumAsync(b => b.CostBRL, ct);

        total += await SumActiveContainerCostAsync(null, startDate, endDate, now, ct);
        total += await SumActiveDatabaseCostAsync(null, startDate, endDate, now, ct);

        return total;
    }

    // Estimates accrued cost of containers still running, since their billing periods
    // only persist CostBRL on stop. Pass realmId = null to span all realms.
    private async Task<decimal> SumActiveContainerCostAsync(Guid? realmId, DateTime startDate, DateTime endDate, DateTime now, CancellationToken ct)
    {
        var active = await _db.BillingPeriods
            .Join(_db.Containers, b => b.ContainerId, c => (Guid?)c.Id, (b, c) => new { b, c })
            .Where(x => realmId == null || x.c.RealmId == realmId)
            .Where(x => x.b.StoppedAtUtc == null)
            .Where(x => x.b.StartedAtUtc >= startDate && x.b.StartedAtUtc < endDate)
            .Select(x => new { x.b.StartedAtUtc, x.c.CostPerHourBRL })
            .ToListAsync(ct);

        return active.Sum(p => (decimal)(now - p.StartedAtUtc).TotalHours * p.CostPerHourBRL);
    }

    // Estimates accrued cost of managed databases still running, deriving the hourly
    // rate from the instance tier + engine. Pass realmId = null to span all realms.
    private async Task<decimal> SumActiveDatabaseCostAsync(Guid? realmId, DateTime startDate, DateTime endDate, DateTime now, CancellationToken ct)
    {
        var active = await _db.BillingPeriods
            .Join(_db.ManagedDatabaseInstances, b => b.ManagedDatabaseId, d => (Guid?)d.Id, (b, d) => new { b, d })
            .Where(x => realmId == null || x.d.RealmId == realmId)
            .Where(x => x.b.StoppedAtUtc == null)
            .Where(x => x.b.StartedAtUtc >= startDate && x.b.StartedAtUtc < endDate)
            .Select(x => new { x.b.StartedAtUtc, x.d.CpuLimit, x.d.MemoryLimit, x.d.Type })
            .ToListAsync(ct);

        return active.Sum(p => (decimal)(now - p.StartedAtUtc).TotalHours
            * ManagedDatabasePricing.HourlyRateBRL(p.CpuLimit, p.MemoryLimit, p.Type));
    }

    public async Task<decimal> GetContainerMonthCostAsync(Guid containerId, int year, int month, CancellationToken ct = default)
    {
        var container = await _db.Containers.FindAsync([containerId], ct);
        if (container is null) return 0;

        var startDate = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var endDate = startDate.AddMonths(1);

        var total = await _db.BillingPeriods
            .Where(b => b.ContainerId == containerId)
            .Where(b => b.StartedAtUtc >= startDate && b.StartedAtUtc < endDate)
            .SumAsync(b => b.CostBRL, ct);

        // Add cost for currently running period if any
        var activePeriod = await _db.BillingPeriods
            .Where(b => b.ContainerId == containerId && b.StoppedAtUtc == null)
            .OrderByDescending(b => b.StartedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (activePeriod is not null && activePeriod.StartedAtUtc >= startDate && activePeriod.StartedAtUtc < endDate)
        {
            var currentHours = (DateTime.UtcNow - activePeriod.StartedAtUtc).TotalHours;
            total += (decimal)currentHours * container.CostPerHourBRL;
        }

        return total;
    }
}
