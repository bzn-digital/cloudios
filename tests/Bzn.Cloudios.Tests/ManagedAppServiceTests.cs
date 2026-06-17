using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Bzn.Cloudios.Tests;

public class ManagedAppServiceTests
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

    private static ManagedAppService CreateService(CloudiosDbContext db)
    {
        var mockDockerClient = new Mock<Docker.DotNet.DockerClient>();
        var mockNetworkService = new Mock<IDockerNetworkService>();
        var mockPortAllocator = new Mock<IManagedAppPortAllocator>();
        var mockConfiguration = new Mock<IConfiguration>();
        mockConfiguration.Setup(c => c["Volumes:BasePath"]).Returns("/var/lib/cloudios");
        mockConfiguration.Setup(c => c.GetValue<bool>("Volumes:SkipDirectoryCreation")).Returns(true);
        mockPortAllocator.Setup(p => p.AllocateNextPortAsync(It.IsAny<CancellationToken>())).ReturnsAsync(2000);

        return new ManagedAppService(
            db,
            mockDockerClient.Object,
            mockNetworkService.Object,
            mockPortAllocator.Object,
            mockConfiguration.Object,
            NullLogger<ManagedAppService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_ValidName_CreatesInstance()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "nginx",
            DisplayName = "Nginx",
            Description = "Web server",
            Category = "Web",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var request = new CreateManagedAppRequest
        {
            Name = "my-app",
            TemplateId = templateId,
            Size = InstanceSize.Micro1s
        };

        var result = await service.CreateAsync(realmId, request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("my-app", result.Name);
        Assert.Equal(realmId, result.RealmId);
        Assert.Equal(templateId, result.TemplateId);
        Assert.Equal(ManagedAppStatus.Provisioning.ToString(), result.Status);
        Assert.Equal(2000, result.HostPort);
    }

    [Fact]
    public async Task CreateAsync_InvalidNameWithSpaces_ThrowsArgumentException()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "nginx",
            DisplayName = "Nginx",
            Description = "Web server",
            Category = "Web",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var request = new CreateManagedAppRequest
        {
            Name = "my app",
            TemplateId = templateId
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(realmId, request, CancellationToken.None));

        Assert.Contains("lowercase letters, numbers, and hyphens", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_InvalidNameWithUppercase_ThrowsArgumentException()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "nginx",
            DisplayName = "Nginx",
            Description = "Web server",
            Category = "Web",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var request = new CreateManagedAppRequest
        {
            Name = "MyApp",
            TemplateId = templateId
        };

        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => service.CreateAsync(realmId, request, CancellationToken.None));

        Assert.Contains("lowercase letters, numbers, and hyphens", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNameInSameRealm_ThrowsInvalidOperationException()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "nginx",
            DisplayName = "Nginx",
            Description = "Web server",
            Category = "Web",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.ManagedAppInstances.Add(new ManagedAppInstance
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            TemplateId = templateId,
            Name = "my-app",
            HostPort = 2000,
            Status = ManagedAppStatus.Running,
            Size = InstanceSize.Micro1s,
            CpuLimitCores = 0.5,
            MemoryLimitBytes = 512 * 1024 * 1024,
            CostPerHourBRL = 0.02m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var request = new CreateManagedAppRequest
        {
            Name = "my-app",
            TemplateId = templateId
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(realmId, request, CancellationToken.None));

        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_InvalidTemplateId_ThrowsInvalidOperationException()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var request = new CreateManagedAppRequest
        {
            Name = "my-app",
            TemplateId = templateId
        };

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.CreateAsync(realmId, request, CancellationToken.None));

        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public async Task ListAsync_ReturnsOnlyRealmInstances()
    {
        var db = CreateInMemoryDb();
        var realmA = Guid.NewGuid();
        var realmB = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.AddRange(
            new Realm { Id = realmA, Name = "RealmA", Slug = "realm-a", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Realm { Id = realmB, Name = "RealmB", Slug = "realm-b", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "nginx",
            DisplayName = "Nginx",
            Description = "Web server",
            Category = "Web",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.ManagedAppInstances.AddRange(
            new ManagedAppInstance
            {
                Id = Guid.NewGuid(),
                RealmId = realmA,
                TemplateId = templateId,
                Name = "app-a",
                HostPort = 2000,
                Status = ManagedAppStatus.Running,
                Size = InstanceSize.Micro1s,
                CpuLimitCores = 0.5,
                MemoryLimitBytes = 512 * 1024 * 1024,
                CostPerHourBRL = 0.02m,
                CreatedAt = DateTime.UtcNow
            },
            new ManagedAppInstance
            {
                Id = Guid.NewGuid(),
                RealmId = realmB,
                TemplateId = templateId,
                Name = "app-b",
                HostPort = 2001,
                Status = ManagedAppStatus.Running,
                Size = InstanceSize.Micro1s,
                CpuLimitCores = 0.5,
                MemoryLimitBytes = 512 * 1024 * 1024,
                CostPerHourBRL = 0.02m,
                CreatedAt = DateTime.UtcNow
            }
        );
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.ListAsync(realmA, null, 1, 20, CancellationToken.None);

        Assert.Single(result.Items);
        Assert.Equal("app-a", result.Items[0].Name);
        Assert.Equal(1, result.TotalCount);
    }

    [Fact]
    public async Task GetByIdAsync_InstanceInRealm_ReturnsInstance()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "nginx",
            DisplayName = "Nginx",
            Description = "Web server",
            Category = "Web",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.ManagedAppInstances.Add(new ManagedAppInstance
        {
            Id = instanceId,
            RealmId = realmId,
            TemplateId = templateId,
            Name = "my-app",
            HostPort = 2000,
            Status = ManagedAppStatus.Running,
            Size = InstanceSize.Micro1s,
            CpuLimitCores = 0.5,
            MemoryLimitBytes = 512 * 1024 * 1024,
            CostPerHourBRL = 0.02m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetByIdAsync(realmId, instanceId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(instanceId, result.Id);
        Assert.Equal("my-app", result.Name);
    }

    [Fact]
    public async Task GetByIdAsync_InstanceInDifferentRealm_ReturnsNull()
    {
        var db = CreateInMemoryDb();
        var realmA = Guid.NewGuid();
        var realmB = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.AddRange(
            new Realm { Id = realmA, Name = "RealmA", Slug = "realm-a", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Realm { Id = realmB, Name = "RealmB", Slug = "realm-b", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "nginx",
            DisplayName = "Nginx",
            Description = "Web server",
            Category = "Web",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.ManagedAppInstances.Add(new ManagedAppInstance
        {
            Id = instanceId,
            RealmId = realmA,
            TemplateId = templateId,
            Name = "my-app",
            HostPort = 2000,
            Status = ManagedAppStatus.Running,
            Size = InstanceSize.Micro1s,
            CpuLimitCores = 0.5,
            MemoryLimitBytes = 512 * 1024 * 1024,
            CostPerHourBRL = 0.02m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        var result = await service.GetByIdAsync(realmB, instanceId, CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task StopInstanceAsync_InstanceInDifferentRealm_ThrowsInvalidOperationException()
    {
        var db = CreateInMemoryDb();
        var realmA = Guid.NewGuid();
        var realmB = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.AddRange(
            new Realm { Id = realmA, Name = "RealmA", Slug = "realm-a", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Realm { Id = realmB, Name = "RealmB", Slug = "realm-b", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "nginx",
            DisplayName = "Nginx",
            Description = "Web server",
            Category = "Web",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.ManagedAppInstances.Add(new ManagedAppInstance
        {
            Id = instanceId,
            RealmId = realmA,
            TemplateId = templateId,
            Name = "my-app",
            HostPort = 2000,
            Status = ManagedAppStatus.Running,
            Size = InstanceSize.Micro1s,
            CpuLimitCores = 0.5,
            MemoryLimitBytes = 512 * 1024 * 1024,
            CostPerHourBRL = 0.02m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.StopInstanceAsync(realmB, instanceId, CancellationToken.None));

        Assert.Contains("not found in realm", exception.Message);
    }

    [Fact]
    public async Task DeleteInstanceAsync_RemovesFromDatabase()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "nginx",
            DisplayName = "Nginx",
            Description = "Web server",
            Category = "Web",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.ManagedAppInstances.Add(new ManagedAppInstance
        {
            Id = instanceId,
            RealmId = realmId,
            TemplateId = templateId,
            Name = "my-app",
            HostPort = 2000,
            Status = ManagedAppStatus.Stopped,
            Size = InstanceSize.Micro1s,
            CpuLimitCores = 0.5,
            MemoryLimitBytes = 512 * 1024 * 1024,
            CostPerHourBRL = 0.02m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.DeleteInstanceAsync(realmId, instanceId, CancellationToken.None);

        var instance = await db.ManagedAppInstances.FindAsync(instanceId);
        Assert.Null(instance);
    }

    [Fact]
    public async Task DeleteInstanceAsync_InstanceInDifferentRealm_ThrowsInvalidOperationException()
    {
        var db = CreateInMemoryDb();
        var realmA = Guid.NewGuid();
        var realmB = Guid.NewGuid();
        var instanceId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.AddRange(
            new Realm { Id = realmA, Name = "RealmA", Slug = "realm-a", IsActive = true, CreatedAt = DateTime.UtcNow },
            new Realm { Id = realmB, Name = "RealmB", Slug = "realm-b", IsActive = true, CreatedAt = DateTime.UtcNow }
        );
        db.ManagedAppTemplates.Add(new ManagedAppTemplate
        {
            Id = templateId,
            Slug = "nginx",
            DisplayName = "Nginx",
            Description = "Web server",
            Category = "Web",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        db.ManagedAppInstances.Add(new ManagedAppInstance
        {
            Id = instanceId,
            RealmId = realmA,
            TemplateId = templateId,
            Name = "my-app",
            HostPort = 2000,
            Status = ManagedAppStatus.Stopped,
            Size = InstanceSize.Micro1s,
            CpuLimitCores = 0.5,
            MemoryLimitBytes = 512 * 1024 * 1024,
            CostPerHourBRL = 0.02m,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DeleteInstanceAsync(realmB, instanceId, CancellationToken.None));

        Assert.Contains("not found in realm", exception.Message);
    }
}
