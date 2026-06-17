using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bzn.Cloudios.Tests;

public class ManagedAppPortAllocatorTests
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
    public async Task AllocateNextPortAsync_EmptyDatabase_ReturnsPort2000()
    {
        var db = CreateInMemoryDb();
        var allocator = new ManagedAppPortAllocator(db);

        var port = await allocator.AllocateNextPortAsync(CancellationToken.None);

        Assert.Equal(2000, port);
    }

    [Fact]
    public async Task AllocateNextPortAsync_Ports2000To2005Occupied_ReturnsPort2006()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate { Id = templateId, Slug = "test", DisplayName = "Test", Description = "Test", Category = "Test", DockerImage = "nginx", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        for (int p = 2000; p <= 2005; p++)
        {
            db.ManagedAppInstances.Add(new ManagedAppInstance
            {
                Id = Guid.NewGuid(),
                RealmId = realmId,
                TemplateId = templateId,
                Name = $"app-{p}",
                HostPort = p,
                Status = ManagedAppStatus.Running,
                Size = InstanceSize.Micro1s,
                CpuLimitCores = 0.5,
                MemoryLimitBytes = 512 * 1024 * 1024,
                CostPerHourBRL = 0.01m,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var allocator = new ManagedAppPortAllocator(db);
        var port = await allocator.AllocateNextPortAsync(CancellationToken.None);

        Assert.Equal(2006, port);
    }

    [Fact]
    public async Task AllocateNextPortAsync_AllPortsOccupied_ThrowsInvalidOperationException()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate { Id = templateId, Slug = "test", DisplayName = "Test", Description = "Test", Category = "Test", DockerImage = "nginx", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });

        for (int p = 2000; p <= 4500; p++)
        {
            db.ManagedAppInstances.Add(new ManagedAppInstance
            {
                Id = Guid.NewGuid(),
                RealmId = realmId,
                TemplateId = templateId,
                Name = $"app-{p}",
                HostPort = p,
                Status = ManagedAppStatus.Running,
                Size = InstanceSize.Micro1s,
                CpuLimitCores = 0.5,
                MemoryLimitBytes = 512 * 1024 * 1024,
                CostPerHourBRL = 0.01m,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var allocator = new ManagedAppPortAllocator(db);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => allocator.AllocateNextPortAsync(CancellationToken.None));

        Assert.Equal("No available ports in the managed app range (2000-4500).", exception.Message);
    }

    [Fact]
    public async Task AllocateNextPortAsync_SequentialCalls_AllocatesDifferentPorts()
    {
        var db = CreateInMemoryDb();
        var realmId = Guid.NewGuid();
        var templateId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.ManagedAppTemplates.Add(new ManagedAppTemplate { Id = templateId, Slug = "test", DisplayName = "Test", Description = "Test", Category = "Test", DockerImage = "nginx", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var allocator = new ManagedAppPortAllocator(db);
        var ports = new List<int>();

        for (int i = 0; i < 10; i++)
        {
            var port = await allocator.AllocateNextPortAsync(CancellationToken.None);
            ports.Add(port);

            // Simulate persisting the allocated port
            db.ManagedAppInstances.Add(new ManagedAppInstance
            {
                Id = Guid.NewGuid(),
                RealmId = realmId,
                TemplateId = templateId,
                Name = $"app-{i}",
                HostPort = port,
                Status = ManagedAppStatus.Running,
                Size = InstanceSize.Micro1s,
                CpuLimitCores = 0.5,
                MemoryLimitBytes = 512 * 1024 * 1024,
                CostPerHourBRL = 0.01m,
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var uniquePorts = ports.Distinct().ToList();
        Assert.Equal(10, uniquePorts.Count);
        Assert.Equal(2000, ports[0]);
        Assert.Equal(2001, ports[1]);
        Assert.All(uniquePorts, port => Assert.InRange(port, 2000, 4500));
    }
}
