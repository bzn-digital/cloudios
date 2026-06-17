using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Bzn.Cloudios.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bzn.Cloudios.Tests;

public class ManagedAppMigrationAndSeederTests
{
    private static CloudiosDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<CloudiosDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var db = new CloudiosDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task Migration_CreatesManagedAppTemplatesTable()
    {
        var db = CreateInMemoryDb();
        
        var template = new ManagedAppTemplate
        {
            Id = Guid.NewGuid(),
            Slug = "test-app",
            DisplayName = "Test App",
            Description = "Test description",
            Category = "Test",
            DockerImage = "test/app:latest",
            DefaultEnvVars = new Dictionary<string, string> { { "KEY", "VALUE" } },
            DefaultInstanceSize = InstanceSize.Micro1s,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.ManagedAppTemplates.Add(template);
        await db.SaveChangesAsync();

        var saved = await db.ManagedAppTemplates.FirstOrDefaultAsync(t => t.Slug == "test-app");
        Assert.NotNull(saved);
        Assert.Equal("test-app", saved.Slug);
        Assert.Equal("Test App", saved.DisplayName);
        Assert.Equal("Test", saved.Category);
        Assert.Equal("test/app:latest", saved.DockerImage);
        Assert.Single(saved.DefaultEnvVars);
        Assert.Equal("VALUE", saved.DefaultEnvVars["KEY"]);
    }

    [Fact]
    public async Task Migration_CreatesManagedAppInstancesTable()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "test",
            DisplayName = "Test",
            Description = "Test",
            Category = "Test",
            DockerImage = "test:latest",
            DefaultEnvVars = new(),
            DefaultInstanceSize = InstanceSize.Micro1s,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var instance = new ManagedAppInstance
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            TemplateId = templateId,
            Name = "test-instance",
            HostPort = 8080,
            Status = ManagedAppStatus.Running,
            Size = InstanceSize.Micro1s,
            CpuLimitCores = 0.5,
            MemoryLimitBytes = 512 * 1024 * 1024,
            CostPerHourBRL = 0.02m,
            CreatedAt = DateTime.UtcNow
        };

        db.ManagedAppInstances.Add(instance);
        await db.SaveChangesAsync();

        var saved = await db.ManagedAppInstances.FirstOrDefaultAsync(i => i.Name == "test-instance");
        Assert.NotNull(saved);
        Assert.Equal("test-instance", saved.Name);
        Assert.Equal(8080, saved.HostPort);
        Assert.Equal(ManagedAppStatus.Running, saved.Status);
    }

    [Fact]
    public async Task Migration_HostPortUniqueIndex_PreventsDuplicates()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "test",
            DisplayName = "Test",
            Description = "Test",
            Category = "Test",
            DockerImage = "test:latest",
            DefaultEnvVars = new(),
            DefaultInstanceSize = InstanceSize.Micro1s,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        db.ManagedAppInstances.Add(new ManagedAppInstance
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            TemplateId = templateId,
            Name = "instance1",
            HostPort = 8080,
            Status = ManagedAppStatus.Running,
            Size = InstanceSize.Micro1s,
            CpuLimitCores = 0.5,
            MemoryLimitBytes = 512 * 1024 * 1024,
            CostPerHourBRL = 0.02m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var duplicate = new ManagedAppInstance
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            TemplateId = templateId,
            Name = "instance2",
            HostPort = 8080,
            Status = ManagedAppStatus.Running,
            Size = InstanceSize.Micro1s,
            CpuLimitCores = 0.5,
            MemoryLimitBytes = 512 * 1024 * 1024,
            CostPerHourBRL = 0.02m,
            CreatedAt = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<DbUpdateException>(() =>
        {
            db.ManagedAppInstances.Add(duplicate);
            return db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Migration_RealmIdNameUniqueIndex_PreventsDuplicates()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "test",
            DisplayName = "Test",
            Description = "Test",
            Category = "Test",
            DockerImage = "test:latest",
            DefaultEnvVars = new(),
            DefaultInstanceSize = InstanceSize.Micro1s,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        db.ManagedAppInstances.Add(new ManagedAppInstance
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            TemplateId = templateId,
            Name = "myapp",
            HostPort = 8080,
            Status = ManagedAppStatus.Running,
            Size = InstanceSize.Micro1s,
            CpuLimitCores = 0.5,
            MemoryLimitBytes = 512 * 1024 * 1024,
            CostPerHourBRL = 0.02m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var duplicate = new ManagedAppInstance
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            TemplateId = templateId,
            Name = "myapp",
            HostPort = 8081,
            Status = ManagedAppStatus.Running,
            Size = InstanceSize.Micro1s,
            CpuLimitCores = 0.5,
            MemoryLimitBytes = 512 * 1024 * 1024,
            CostPerHourBRL = 0.02m,
            CreatedAt = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<DbUpdateException>(() =>
        {
            db.ManagedAppInstances.Add(duplicate);
            return db.SaveChangesAsync();
        });
    }

    [Fact]
    public async Task Seeder_Populates8Templates()
    {
        var db = CreateInMemoryDb();
        var logger = NullLogger<DatabaseSeeder>.Instance;
        var seeder = new DatabaseSeeder(db, logger);

        await seeder.SeedManagedAppTemplatesAsync(CancellationToken.None);

        var templates = await db.ManagedAppTemplates.ToListAsync();
        Assert.Equal(8, templates.Count);
        Assert.Contains(templates, t => t.Slug == "redisinsight");
        Assert.Contains(templates, t => t.Slug == "n8n");
        Assert.Contains(templates, t => t.Slug == "gitlab");
        Assert.Contains(templates, t => t.Slug == "grafana");
        Assert.Contains(templates, t => t.Slug == "uptime-kuma");
        Assert.Contains(templates, t => t.Slug == "portainer");
        Assert.Contains(templates, t => t.Slug == "metabase");
        Assert.Contains(templates, t => t.Slug == "minio");
    }

    [Fact]
    public async Task Seeder_IsIdempotent_DoesNotDuplicateOnSecondRun()
    {
        var db = CreateInMemoryDb();
        var logger = NullLogger<DatabaseSeeder>.Instance;
        var seeder = new DatabaseSeeder(db, logger);

        await seeder.SeedManagedAppTemplatesAsync(CancellationToken.None);
        var firstRunCount = await db.ManagedAppTemplates.CountAsync();

        await seeder.SeedManagedAppTemplatesAsync(CancellationToken.None);
        var secondRunCount = await db.ManagedAppTemplates.CountAsync();

        Assert.Equal(firstRunCount, secondRunCount);
        Assert.Equal(8, secondRunCount);
    }

    [Fact]
    public async Task Seeder_TemplatesHaveCorrectProperties()
    {
        var db = CreateInMemoryDb();
        var logger = NullLogger<DatabaseSeeder>.Instance;
        var seeder = new DatabaseSeeder(db, logger);

        await seeder.SeedManagedAppTemplatesAsync(CancellationToken.None);

        var redisinsight = await db.ManagedAppTemplates.FirstAsync(t => t.Slug == "redisinsight");
        Assert.Equal("RedisInsight", redisinsight.DisplayName);
        Assert.Equal("DevOps", redisinsight.Category);
        Assert.Equal("redis/redisinsight:latest", redisinsight.DockerImage);
        Assert.Equal(InstanceSize.Micro1s, redisinsight.DefaultInstanceSize);

        var n8n = await db.ManagedAppTemplates.FirstAsync(t => t.Slug == "n8n");
        Assert.Equal("N8N", n8n.DisplayName);
        Assert.Equal("Automation", n8n.Category);
        Assert.Equal("n8nio/n8n:latest", n8n.DockerImage);
        Assert.Equal(InstanceSize.Small1s, n8n.DefaultInstanceSize);

        var gitlab = await db.ManagedAppTemplates.FirstAsync(t => t.Slug == "gitlab");
        Assert.Equal("GitLab CE", gitlab.DisplayName);
        Assert.Equal("DevOps", gitlab.Category);
        Assert.Equal("gitlab/gitlab-ce:latest", gitlab.DockerImage);
        Assert.Equal(InstanceSize.Medium1s, gitlab.DefaultInstanceSize);
    }

    [Fact]
    public async Task Seeder_DoesNotIncludeDatabaseApps()
    {
        var db = CreateInMemoryDb();
        var logger = NullLogger<DatabaseSeeder>.Instance;
        var seeder = new DatabaseSeeder(db, logger);

        await seeder.SeedManagedAppTemplatesAsync(CancellationToken.None);

        var templates = await db.ManagedAppTemplates.Select(t => t.Slug).ToListAsync();
        Assert.DoesNotContain("mysql", templates);
        Assert.DoesNotContain("mongodb", templates);
        Assert.DoesNotContain("redis", templates);
        Assert.DoesNotContain("rabbitmq", templates);
        Assert.DoesNotContain("kafka", templates);
    }
}
