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

        var systemRealmId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var systemRealm = await _context.Realms.FindAsync([systemRealmId], ct);

        if (systemRealm is null)
        {
            _logger.LogInformation("Seeding initial data...");

            systemRealm = new Realm
            {
                Id = systemRealmId,
                Name = "system",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            var adminUser = new User
            {
                Id = Guid.NewGuid(),
                RealmId = systemRealmId,
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
}
