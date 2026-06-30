using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Extensions;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class UserService
{
    private readonly CloudiosDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly ILogger<UserService> _logger;

    public UserService(CloudiosDbContext context, ITenantProvider tenantProvider, ILogger<UserService> logger)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _logger = logger;
    }

    public async Task<UserListResponse> ListByRealmAsync(Guid realmId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = _context.Users.ForRealm(realmId);
        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserItem
            {
                Id = u.Id,
                Email = u.Email,
                Role = u.Role.ToString(),
                IsBlocked = u.IsBlocked,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync(ct);

        return new UserListResponse
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            HasNextPage = total > page * pageSize
        };
    }

    public async Task<(UserItem? User, string? Error)> CreateAsync(Guid realmId, CreateUserRequest request, CancellationToken ct = default)
    {
        if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.RealmId == realmId, ct))
            return (null, "Email already exists in this realm");

        if (!await _context.Realms.AnyAsync(r => r.Id == realmId, ct))
            return (null, "Realm not found");

        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            return (null, "Invalid role");

        var user = new User
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {Email} created in realm {RealmId}", user.Email, realmId);

        return (new UserItem
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsBlocked = user.IsBlocked,
            CreatedAt = user.CreatedAt
        }, null);
    }

    public async Task<(UserItem? User, string? Error)> UpdateAsync(Guid realmId, Guid userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _context.Users.ForRealm(realmId).FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return (null, "User not found");

        if (request.Role is not null)
        {
            if (!Enum.TryParse<UserRole>(request.Role, out var newRole))
                return (null, "Invalid role");

            // Prohibit removing the last RealmOwner
            if (user.Role == UserRole.RealmOwner && newRole != UserRole.RealmOwner)
            {
                var ownerCount = await _context.Users
                    .ForRealm(realmId)
                    .CountAsync(u => u.Role == UserRole.RealmOwner && u.Id != userId, ct);

                if (ownerCount == 0)
                    return (null, "Cannot remove the last RealmOwner");
            }

            user.Role = newRole;
        }

        if (request.IsBlocked.HasValue)
            user.IsBlocked = request.IsBlocked.Value;

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} updated", userId);

        return (new UserItem
        {
            Id = user.Id,
            Email = user.Email,
            Role = user.Role.ToString(),
            IsBlocked = user.IsBlocked,
            CreatedAt = user.CreatedAt
        }, null);
    }

    public async Task<(bool Success, string? Error)> DeleteAsync(Guid realmId, Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users.ForRealm(realmId).FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null) return (false, "User not found");

        // Prohibit self-deletion
        if (user.Id == _tenantProvider.UserId)
            return (false, "Cannot delete yourself");

        // Prohibit removing the last RealmOwner
        if (user.Role == UserRole.RealmOwner)
        {
            var ownerCount = await _context.Users
                .ForRealm(realmId)
                .CountAsync(u => u.Role == UserRole.RealmOwner && u.Id != userId, ct);

            if (ownerCount == 0)
                return (false, "Cannot remove the last RealmOwner");
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} deleted", userId);
        return (true, null);
    }
}
