using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using IContainerService = Bzn.Cloudios.Application.Abstractions.IContainerService;

namespace Bzn.Cloudios.Application.Services;

public sealed class RealmService
{
    private readonly CloudiosDbContext _context;
    private readonly ILogger<RealmService> _logger;
    private readonly IDockerNetworkService _dockerNetworkService;
    private readonly IContainerService _containerService;
    private readonly IBillingService _billingService;
    private readonly IManagedAppService _managedAppService;

    public RealmService(
        CloudiosDbContext context,
        ILogger<RealmService> logger,
        IDockerNetworkService dockerNetworkService,
        IContainerService containerService,
        IBillingService billingService,
        IManagedAppService managedAppService)
    {
        _context = context;
        _logger = logger;
        _dockerNetworkService = dockerNetworkService;
        _containerService = containerService;
        _billingService = billingService;
        _managedAppService = managedAppService;
    }

    public async Task<RealmListResponse> ListAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null, string? sortBy = null, CancellationToken ct = default)
    {
        var query = _context.Realms.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.Contains(search) || r.Slug.Contains(search));

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (status.ToLower() == "active")
                query = query.Where(r => r.IsActive);
            else if (status.ToLower() == "suspended")
                query = query.Where(r => !r.IsActive);
        }

        var total = await query.CountAsync(ct);
        var realms = await query
            .Include(r => r.Users)
            .Include(r => r.Containers)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        var now = DateTime.UtcNow;
        var items = new List<RealmItem>();

        foreach (var realm in realms)
        {
            var monthlyCost = await _billingService.GetRealmBillingAsync(realm.Id, now.Year, now.Month, ct);
            items.Add(new RealmItem
            {
                Id = realm.Id,
                Name = realm.Name,
                Slug = realm.Slug,
                IsActive = realm.IsActive,
                CreatedAt = realm.CreatedAt,
                UserCount = realm.Users.Count,
                ContainerCount = realm.Containers.Count,
                MonthlyCostBRL = monthlyCost
            });
        }

        var sortedItems = sortBy?.ToLower() switch
        {
            "createdat" => items.OrderBy(r => r.CreatedAt).ToList(),
            "monthlycost" => items.OrderBy(r => r.MonthlyCostBRL).ToList(),
            _ => items.OrderBy(r => r.Name).ToList()
        };

        return new RealmListResponse
        {
            Items = sortedItems,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            HasNextPage = total > page * pageSize
        };
    }

    public async Task<RealmDetailResponse?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var realm = await _context.Realms
            .Include(r => r.Users)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (realm is null) return null;

        _logger.LogInformation("Realm {RealmName} has {UserCount} users", realm.Name, realm.Users.Count);
        foreach (var user in realm.Users)
        {
            _logger.LogInformation("User: {Email}, Role: {Role}", user.Email, user.Role);
        }

        var owner = realm.Users.FirstOrDefault(u => u.Role == Domain.Enums.UserRole.RealmOwner);
        _logger.LogInformation("Owner found: {OwnerEmail}", owner?.Email ?? "null");

        // Fetch containers for this realm
        var containers = await _context.Containers
            .Where(c => c.RealmId == id)
            .Select(c => new RealmResourceItem
            {
                Id = c.Id,
                Name = c.Name,
                Type = "container",
                Status = c.Status.ToString(),
                CostBRL = 0 // TODO: Calculate actual cost
            })
            .ToListAsync(ct);

        // Fetch managed databases for this realm
        var databases = await _context.ManagedDatabaseInstances
            .Where(d => d.RealmId == id)
            .Select(d => new RealmResourceItem
            {
                Id = d.Id,
                Name = d.Name,
                Type = "database",
                Status = d.Status.ToString(),
                CostBRL = 0 // TODO: Calculate actual cost from tier
            })
            .ToListAsync(ct);

        // Fetch managed apps for this realm
        var managedApps = await _context.ManagedAppInstances
            .Where(m => m.RealmId == id)
            .Select(m => new RealmResourceItem
            {
                Id = m.Id,
                Name = m.Name,
                Type = "managedapp",
                Status = m.Status.ToString(),
                CostBRL = 0 // TODO: Calculate actual cost from template
            })
            .ToListAsync(ct);

        var resources = new List<RealmResourceItem>();
        resources.AddRange(containers);
        resources.AddRange(databases);
        resources.AddRange(managedApps);

        return new RealmDetailResponse
        {
            Id = realm.Id,
            Name = realm.Name,
            Slug = realm.Slug,
            IsActive = realm.IsActive,
            CreatedAt = realm.CreatedAt,
            OwnerEmail = owner?.Email,
            Users = realm.Users.Select(u => new RealmUserItem
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsBlocked = u.IsBlocked,
                CreatedAt = u.CreatedAt
            }).ToList(),
            Resources = resources
        };
    }

    public async Task<(RealmDetailResponse? Realm, string? Error)> CreateAsync(CreateRealmRequest request, CancellationToken ct = default)
    {
        if (await _context.Realms.AnyAsync(r => r.Name == request.Name, ct))
            return (null, "Realm name already exists");

        var slug = GenerateSlug(request.Name);

        if (string.IsNullOrEmpty(slug))
            return (null, "Realm name must contain at least one alphanumeric character");

        if (await _context.Realms.AnyAsync(r => r.Slug == slug, ct))
            return (null, "Realm slug already exists (name may produce duplicate slug)");

        var realm = new Realm
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = slug,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Realms.Add(realm);
        await _context.SaveChangesAsync(ct);

        // Create the default network for this realm
        await _dockerNetworkService.EnsureRealmNetworkAsync(realm.Id, ct);

        _logger.LogInformation("Realm {Name} created with default network", realm.Name);

        return (new RealmDetailResponse
        {
            Id = realm.Id,
            Name = realm.Name,
            Slug = realm.Slug,
            IsActive = realm.IsActive,
            CreatedAt = realm.CreatedAt,
            Users = []
        }, null);
    }

    public async Task<(RealmDetailResponse? Realm, string? Error)> UpdateAsync(Guid id, UpdateRealmRequest request, CancellationToken ct = default)
    {
        var realm = await _context.Realms.FindAsync([id], ct);
        if (realm is null) return (null, "Realm not found");

        var originalName = realm.Name;
        
        if (originalName != request.Name && await _context.Realms.AnyAsync(r => r.Name == request.Name, ct))
            return (null, "Realm name already exists");

        realm.Name = request.Name;
        
        if (originalName != request.Name)
        {
            var newSlug = GenerateSlug(request.Name);

            if (string.IsNullOrEmpty(newSlug))
                return (null, "Realm name must contain at least one alphanumeric character");
            
            if (await _context.Realms.AnyAsync(r => r.Slug == newSlug && r.Id != id, ct))
                return (null, "Realm slug already exists (name may produce duplicate slug)");
            
            realm.Slug = newSlug;
        }
        
        realm.IsActive = request.IsActive;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Realm {Id} updated", id);

        return (new RealmDetailResponse
        {
            Id = realm.Id,
            Name = realm.Name,
            IsActive = realm.IsActive,
            CreatedAt = realm.CreatedAt,
            Users = []
        }, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var realm = await _context.Realms.Include(r => r.Users).FirstOrDefaultAsync(r => r.Id == id, ct);
        if (realm is null) return (false, "Realm not found");

        if (realm.Name == "system")
            return (false, "Cannot delete the system realm");

        _context.Realms.Remove(realm);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Realm {Id} deleted", id);
        return (true, null);
    }

    public async Task<(SuspendRealmResponse? Response, string? Error)> SuspendAsync(Guid id, CancellationToken ct = default)
    {
        var realm = await _context.Realms
            .Include(r => r.Containers)
            .Include(r => r.ManagedDatabases)
            .Include(r => r.ManagedApps)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (realm is null)
            return (null, "Realm not found");

        if (!realm.IsActive)
            return (null, "Realm is already suspended");

        realm.IsActive = false;

        var containersStopped = 0;
        foreach (var container in realm.Containers)
        {
            if (container.DockerContainerId is not null)
            {
                try
                {
                    await _containerService.StopAsync(container.Id, ct);
                    containersStopped++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to stop container {ContainerId} during realm suspension", container.Id);
                }
            }
        }

        var managedAppsStopped = 0;
        foreach (var app in realm.ManagedApps)
        {
            if (app.DockerContainerId is not null)
            {
                try
                {
                    await _managedAppService.StopInstanceAsync(realm.Id, app.Id, ct);
                    managedAppsStopped++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to stop managed app {AppId} during realm suspension", app.Id);
                }
            }
        }

        var billingPeriodsClosed = await CloseOpenBillingPeriodsAsync(id, ct);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Realm {Name} suspended. Containers stopped: {Count}, Managed apps stopped: {AppsCount}, Billing periods closed: {BillingCount}",
            realm.Name, containersStopped, managedAppsStopped, billingPeriodsClosed);

        return (new SuspendRealmResponse
        {
            Id = realm.Id,
            Name = realm.Name,
            IsActive = realm.IsActive,
            ContainersStopped = containersStopped,
            BillingPeriodsClosed = billingPeriodsClosed
        }, null);
    }

    public async Task<(ReactivateRealmResponse? Response, string? Error)> ReactivateAsync(Guid id, CancellationToken ct = default)
    {
        var realm = await _context.Realms.FirstOrDefaultAsync(r => r.Id == id, ct);

        if (realm is null)
            return (null, "Realm not found");

        if (realm.IsActive)
            return (null, "Realm is already active");

        realm.IsActive = true;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Realm {Name} reactivated", realm.Name);

        return (new ReactivateRealmResponse
        {
            Id = realm.Id,
            Name = realm.Name,
            IsActive = realm.IsActive
        }, null);
    }

    public async Task<(RealmDetailResponse? Realm, string? Error)> UpdateQuotasAsync(Guid id, UpdateQuotasRequest request, CancellationToken ct = default)
    {
        var realm = await _context.Realms.FindAsync([id], ct);
        if (realm is null) return (null, "Realm not found");

        if (request.MaxContainers.HasValue)
            realm.MaxContainers = request.MaxContainers.Value;
        if (request.MaxDatabases.HasValue)
            realm.MaxDatabases = request.MaxDatabases.Value;
        if (request.MaxManagedApps.HasValue)
            realm.MaxManagedApps = request.MaxManagedApps.Value;
        if (request.MaxRamBytes.HasValue)
            realm.MaxRamBytes = request.MaxRamBytes.Value;
        if (request.MaxCpuCores.HasValue)
            realm.MaxCpuCores = request.MaxCpuCores.Value;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Realm {Id} quotas updated", id);

        return (new RealmDetailResponse
        {
            Id = realm.Id,
            Name = realm.Name,
            IsActive = realm.IsActive,
            CreatedAt = realm.CreatedAt,
            Users = []
        }, null);
    }

    public async Task<RealmStatsResponse?> GetStatsAsync(Guid id, CancellationToken ct = default)
    {
        var realm = await _context.Realms
            .Include(r => r.Users)
            .Include(r => r.Containers)
            .Include(r => r.ManagedDatabases)
            .FirstOrDefaultAsync(r => r.Id == id, ct);

        if (realm is null) return null;

        var now = DateTime.UtcNow;
        var currentMonthCost = await _billingService.GetRealmBillingAsync(id, now.Year, now.Month, ct);

        var managedAppsCount = await _context.ManagedAppInstances
            .Where(i => i.RealmId == id)
            .CountAsync(ct);

        var ramBytesUsed = realm.Containers.Sum(c => c.MemoryLimitBytes) +
                          realm.ManagedDatabases.Sum(d => d.MemoryLimit);

        var cpuCoresUsed = realm.Containers.Sum(c => c.CpuLimitCores) +
                          realm.ManagedDatabases.Sum(d => d.CpuLimit);

        return new RealmStatsResponse
        {
            UsersCount = realm.Users.Count,
            ContainersCount = realm.Containers.Count,
            DatabasesCount = realm.ManagedDatabases.Count,
            ManagedAppsCount = managedAppsCount,
            MonthlyCostBRL = currentMonthCost,
            Quotas = new RealmQuotas
            {
                MaxContainers = realm.MaxContainers,
                MaxDatabases = realm.MaxDatabases,
                MaxManagedApps = realm.MaxManagedApps,
                MaxRamBytes = realm.MaxRamBytes,
                MaxCpuCores = realm.MaxCpuCores
            },
            Usage = new RealmUsage
            {
                ContainersCount = realm.Containers.Count,
                DatabasesCount = realm.ManagedDatabases.Count,
                ManagedAppsCount = managedAppsCount,
                RamBytesUsed = ramBytesUsed,
                CpuCoresUsed = cpuCoresUsed
            }
        };
    }

    private async Task<int> CloseOpenBillingPeriodsAsync(Guid realmId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;

        var containerIds = await _context.Containers
            .Where(c => c.RealmId == realmId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        var databaseIds = await _context.ManagedDatabaseInstances
            .Where(d => d.RealmId == realmId)
            .Select(d => d.Id)
            .ToListAsync(ct);

        var closedCount = 0;

        foreach (var containerId in containerIds)
        {
            var activePeriod = await _context.BillingPeriods
                .Where(b => b.ContainerId == containerId && b.StoppedAtUtc == null)
                .FirstOrDefaultAsync(ct);

            if (activePeriod is not null)
            {
                await _billingService.RegisterStopAsync(containerId, now, ct);
                closedCount++;
            }
        }

        foreach (var databaseId in databaseIds)
        {
            var activePeriod = await _context.BillingPeriods
                .Where(b => b.ManagedDatabaseId == databaseId && b.StoppedAtUtc == null)
                .FirstOrDefaultAsync(ct);

            if (activePeriod is not null)
            {
                await _billingService.RegisterDatabaseStopAsync(databaseId, now, ct);
                closedCount++;
            }
        }

        return closedCount;
    }

    private static string GenerateSlug(string name)
    {
        // First attempt: generate slug from name
        var slug = Regex.Replace(Regex.Replace(name.ToLower(), "[^a-z0-9-]", "-"), "-{2,}", "-").Trim('-');

        // Fallback: if slug is empty (non-Latin characters), use GUID-based slug
        if (string.IsNullOrEmpty(slug))
        {
            slug = $"realm-{Guid.NewGuid():N}";
        }

        return slug;
    }
}
