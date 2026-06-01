using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class RealmService
{
    private readonly CloudiosDbContext _context;
    private readonly ILogger<RealmService> _logger;

    public RealmService(CloudiosDbContext context, ILogger<RealmService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<RealmListResponse> ListAsync(int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default)
    {
        var query = _context.Realms.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.Name.Contains(search));

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderBy(r => r.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RealmItem
            {
                Id = r.Id,
                Name = r.Name,
                IsActive = r.IsActive,
                CreatedAt = r.CreatedAt,
                UserCount = r.Users.Count,
                ContainerCount = r.Containers.Count
            })
            .ToListAsync(ct);

        return new RealmListResponse
        {
            Items = items,
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

        return new RealmDetailResponse
        {
            Id = realm.Id,
            Name = realm.Name,
            IsActive = realm.IsActive,
            CreatedAt = realm.CreatedAt,
            Users = realm.Users.Select(u => new RealmUserItem
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsBlocked = u.IsBlocked
            }).ToList()
        };
    }

    public async Task<(RealmDetailResponse? Realm, string? Error)> CreateAsync(CreateRealmRequest request, CancellationToken ct = default)
    {
        if (await _context.Realms.AnyAsync(r => r.Name == request.Name, ct))
            return (null, "Realm name already exists");

        var realm = new Realm
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Realms.Add(realm);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Realm {Name} created", realm.Name);

        return (new RealmDetailResponse
        {
            Id = realm.Id,
            Name = realm.Name,
            IsActive = realm.IsActive,
            CreatedAt = realm.CreatedAt,
            Users = []
        }, null);
    }

    public async Task<(RealmDetailResponse? Realm, string? Error)> UpdateAsync(Guid id, UpdateRealmRequest request, CancellationToken ct = default)
    {
        var realm = await _context.Realms.FindAsync([id], ct);
        if (realm is null) return (null, "Realm not found");

        if (realm.Name != request.Name && await _context.Realms.AnyAsync(r => r.Name == request.Name, ct))
            return (null, "Realm name already exists");

        realm.Name = request.Name;
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
}
