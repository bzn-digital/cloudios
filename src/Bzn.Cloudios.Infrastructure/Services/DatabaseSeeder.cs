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

            _context.Realms.Add(systemRealm);
            await _context.SaveChangesAsync(ct);
        }

        // Ensure admin user exists and is up to date
        var adminUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == adminEmail && u.RealmId == systemRealmId, ct);

        if (adminUser is null)
        {
            _logger.LogInformation("Creating admin user...");

            adminUser = new User
            {
                Id = Guid.NewGuid(),
                RealmId = systemRealmId,
                Email = adminEmail,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword),
                Role = UserRole.PlatformAdmin,
                IsBlocked = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(adminUser);
        }
        else
        {
            _logger.LogInformation("Updating admin user password...");

            adminUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
            adminUser.Role = UserRole.PlatformAdmin;
            adminUser.IsBlocked = false;
            _context.Users.Update(adminUser);
        }

        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Seeding complete: admin user ensured");

        await SeedManagedAppTemplatesAsync(ct);
    }

    public async Task SeedManagedAppTemplatesAsync(CancellationToken ct = default)
    {
        var templates = new[]
        {
            new ManagedAppTemplate
            {
                Id = Guid.NewGuid(),
                Slug = "redisinsight",
                DisplayName = "RedisInsight",
                Description = "Redis visualization and management tool",
                Category = "DevOps",
                DockerImage = "redis/redisinsight:latest",
                DefaultEnvVars = new Dictionary<string, string>(),
                DefaultInstanceSize = InstanceSize.Micro1s,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ManagedAppTemplate
            {
                Id = Guid.NewGuid(),
                Slug = "n8n",
                DisplayName = "N8N",
                Description = "Workflow automation tool",
                Category = "Automation",
                DockerImage = "n8nio/n8n:latest",
                DefaultEnvVars = new Dictionary<string, string>(),
                DefaultInstanceSize = InstanceSize.Small1s,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ManagedAppTemplate
            {
                Id = Guid.NewGuid(),
                Slug = "gitlab",
                DisplayName = "GitLab CE",
                Description = "Git repository management and CI/CD",
                Category = "DevOps",
                DockerImage = "gitlab/gitlab-ce:latest",
                DefaultEnvVars = new Dictionary<string, string>(),
                DefaultInstanceSize = InstanceSize.Medium1s,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ManagedAppTemplate
            {
                Id = Guid.NewGuid(),
                Slug = "grafana",
                DisplayName = "Grafana",
                Description = "Metrics visualization and analytics",
                Category = "Monitoring",
                DockerImage = "grafana/grafana:latest",
                DefaultEnvVars = new Dictionary<string, string>(),
                DefaultInstanceSize = InstanceSize.Micro1s,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ManagedAppTemplate
            {
                Id = Guid.NewGuid(),
                Slug = "uptime-kuma",
                DisplayName = "Uptime Kuma",
                Description = "Self-hosted monitoring tool",
                Category = "Monitoring",
                DockerImage = "louislam/uptime-kuma:latest",
                DefaultEnvVars = new Dictionary<string, string>(),
                DefaultInstanceSize = InstanceSize.Micro1s,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ManagedAppTemplate
            {
                Id = Guid.NewGuid(),
                Slug = "portainer",
                DisplayName = "Portainer CE",
                Description = "Container management platform",
                Category = "DevOps",
                DockerImage = "portainer/portainer-ce:latest",
                DefaultEnvVars = new Dictionary<string, string>(),
                DefaultInstanceSize = InstanceSize.Micro1s,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ManagedAppTemplate
            {
                Id = Guid.NewGuid(),
                Slug = "metabase",
                DisplayName = "Metabase",
                Description = "Business intelligence and analytics",
                Category = "Analytics",
                DockerImage = "metabase/metabase:latest",
                DefaultEnvVars = new Dictionary<string, string>(),
                DefaultInstanceSize = InstanceSize.Small1s,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            },
            new ManagedAppTemplate
            {
                Id = Guid.NewGuid(),
                Slug = "minio",
                DisplayName = "MinIO",
                Description = "High-performance object storage",
                Category = "Storage",
                DockerImage = "minio/minio:latest",
                DefaultEnvVars = new Dictionary<string, string>(),
                DefaultInstanceSize = InstanceSize.Small1s,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            }
        };

        foreach (var template in templates)
        {
            var existing = await _context.ManagedAppTemplates
                .FirstOrDefaultAsync(t => t.Slug == template.Slug, ct);

            if (existing is null)
            {
                _logger.LogInformation("Seeding managed app template: {Slug}", template.Slug);
                _context.ManagedAppTemplates.Add(template);
            }
        }

        await _context.SaveChangesAsync(ct);
    }
}
