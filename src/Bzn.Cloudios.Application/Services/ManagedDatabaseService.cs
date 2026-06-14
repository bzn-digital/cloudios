using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class ManagedDatabaseService : IManagedDatabaseService
{
    /// <summary>Maximum number of managed databases a single realm may provision.</summary>
    public const int MaxDatabasesPerRealm = 10;

    private static readonly ManagedDatabaseType[] Engines = [ManagedDatabaseType.MySQL, ManagedDatabaseType.MongoDB];

    private readonly CloudiosDbContext _db;
    private readonly ITenantProvider _tenant;
    private readonly IBillingService _billing;
    private readonly ILogger<ManagedDatabaseService> _logger;

    public ManagedDatabaseService(
        CloudiosDbContext db,
        ITenantProvider tenant,
        IBillingService billing,
        ILogger<ManagedDatabaseService> logger)
    {
        _db = db;
        _tenant = tenant;
        _billing = billing;
        _logger = logger;
    }

    public async Task<DatabaseTierListResponse> GetTiersAsync(CancellationToken ct = default)
    {
        var tiers = await _db.DatabaseTiers
            .OrderBy(t => t.MemoryLimitBytes)
            .ThenBy(t => t.CpuLimitCores)
            .ToListAsync(ct);

        return new DatabaseTierListResponse
        {
            Tiers = tiers.Select(t => new DatabaseTierItem
            {
                Id = t.Id,
                Name = t.Name,
                CpuLimitCores = t.CpuLimitCores,
                MemoryLimitBytes = t.MemoryLimitBytes,
                Pricing = Engines.Select(engine =>
                {
                    var hourly = ManagedDatabasePricing.HourlyRateBRL(t.CpuLimitCores, t.MemoryLimitBytes, engine);
                    return new DatabaseTierPricing
                    {
                        Engine = engine.ToString(),
                        HourlyRateBRL = hourly,
                        MonthlyForecastBRL = ManagedDatabasePricing.MonthlyForecastBRL(hourly)
                    };
                }).ToList()
            }).ToList()
        };
    }

    public async Task<(ManagedDatabaseResponse? Instance, string? Error, int StatusCode)> CreateAsync(CreateManagedDatabaseRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return (null, "Database name is required", StatusCodes.Status400BadRequest);

        if (!Enum.TryParse<ManagedDatabaseType>(request.Type, ignoreCase: true, out var type))
            return (null, $"Invalid database type '{request.Type}'. Allowed: MySQL, MongoDB", StatusCodes.Status400BadRequest);

        var realmId = _tenant.RealmId;

        // Realm permission: the realm must exist and be active to provision resources.
        var realm = await _db.Realms.FirstOrDefaultAsync(r => r.Id == realmId, ct);
        if (realm is null)
            return (null, "Realm not found", StatusCodes.Status404NotFound);
        if (!realm.IsActive)
            return (null, "Realm is not allowed to provision resources", StatusCodes.Status403Forbidden);

        var tier = await _db.DatabaseTiers.FirstOrDefaultAsync(t => t.Id == request.TierId, ct);
        if (tier is null)
            return (null, "Database tier not found", StatusCodes.Status400BadRequest);

        // Realm quota: cap the number of databases the realm may hold.
        var realmCount = await _db.ManagedDatabaseInstances.CountAsync(d => d.RealmId == realmId, ct);
        if (realmCount >= MaxDatabasesPerRealm)
            return (null, $"Realm has reached the limit of {MaxDatabasesPerRealm} managed databases", StatusCodes.Status403Forbidden);

        if (await _db.ManagedDatabaseInstances.AnyAsync(d => d.RealmId == realmId && d.Name == request.Name, ct))
            return (null, "A managed database with this name already exists in this realm", StatusCodes.Status409Conflict);

        var instance = new ManagedDatabaseInstance
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            TierId = tier.Id,
            Name = request.Name,
            Type = type,
            NetworkId = string.Empty,
            CpuLimit = tier.CpuLimitCores,
            MemoryLimit = tier.MemoryLimitBytes,
            Status = ManagedDatabaseStatus.Provisioning,
            CreatedAt = DateTime.UtcNow
        };

        _db.ManagedDatabaseInstances.Add(instance);
        await _db.SaveChangesAsync(ct);

        // Provisioning completes immediately at this layer: transition to the active
        // (Running) state and start billing consumption tracking.
        instance.Status = ManagedDatabaseStatus.Running;
        await _db.SaveChangesAsync(ct);
        await _billing.RegisterDatabaseStartAsync(instance.Id, DateTime.UtcNow, ct);

        _logger.LogInformation(
            "Managed database {Name} ({Type}) created and activated in realm {RealmId}",
            instance.Name, instance.Type, realmId);

        var hourlyRate = ManagedDatabasePricing.HourlyRateBRL(instance.CpuLimit, instance.MemoryLimit, instance.Type);

        return (new ManagedDatabaseResponse
        {
            Id = instance.Id,
            RealmId = instance.RealmId,
            TierId = instance.TierId,
            TierName = tier.Name,
            Name = instance.Name,
            Type = instance.Type.ToString(),
            Status = instance.Status.ToString(),
            CpuLimitCores = instance.CpuLimit,
            MemoryLimitBytes = instance.MemoryLimit,
            HourlyRateBRL = hourlyRate,
            MonthlyForecastBRL = ManagedDatabasePricing.MonthlyForecastBRL(hourlyRate),
            CreatedAt = instance.CreatedAt
        }, null, StatusCodes.Status201Created);
    }
}
