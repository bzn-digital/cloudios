using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Infrastructure.Services;

public sealed class DatabaseSeeder
{
    private readonly CloudiosDbContext _context;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(CloudiosDbContext context, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(string adminEmail, string adminPassword, CancellationToken ct = default)
    {
        await _context.Database.MigrateAsync(ct);

        if (await _context.Realms.AnyAsync(ct))
            return;

        _logger.LogInformation("Seeding initial data...");

        var systemRealm = new Realm
        {
            Id = Guid.NewGuid(),
            Name = "system",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            RealmId = systemRealm.Id,
            Email = adminEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role = UserRole.PlatformAdmin,
            IsBlocked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Realms.Add(systemRealm);
        _context.Users.Add(adminUser);

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Seeding complete: system realm + admin user created");
    }
}
