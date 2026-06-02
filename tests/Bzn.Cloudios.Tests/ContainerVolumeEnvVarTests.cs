using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Configuration;
using System.Security.Claims;

namespace Bzn.Cloudios.Tests;

public class ContainerVolumeEnvVarTests
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
    public async Task UpdateEnvVarsAsync_ReplacesExistingEnvVars()
    {
        var db = CreateInMemoryDb();
        var containerId = Guid.NewGuid();
        var realmId = Guid.NewGuid();

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.Containers.Add(new Container
        {
            Id = containerId,
            RealmId = realmId,
            Name = "test",
            ImageName = "nginx",
            InternalPort = 80,
            CostPerHourBRL = 0.02m,
            Status = ContainerStatus.Stopped,
            CreatedAt = DateTime.UtcNow
        });
        db.ContainerEnvVars.AddRange(
            new ContainerEnvVar { Id = Guid.NewGuid(), ContainerId = containerId, Key = "OLD_KEY", Value = "old_value" }
        );
        await db.SaveChangesAsync();

        var logger = NullLogger<ContainerService>.Instance;
        var docker = new MockDockerNetworkService();
        var config = new ConfigurationBuilder().Build();
        var service = new ContainerService(db, docker, config, logger);

        var newEnvVars = new Dictionary<string, string>
        {
            { "NEW_KEY1", "value1" },
            { "NEW_KEY2", "value2" }
        };

        await service.UpdateEnvVarsAsync(containerId, newEnvVars, CancellationToken.None);

        var envVars = await db.ContainerEnvVars.Where(e => e.ContainerId == containerId).ToListAsync();
        Assert.Equal(2, envVars.Count);
        Assert.DoesNotContain(envVars, e => e.Key == "OLD_KEY");
        Assert.Contains(envVars, e => e.Key == "NEW_KEY1" && e.Value == "value1");
        Assert.Contains(envVars, e => e.Key == "NEW_KEY2" && e.Value == "value2");
    }

    [Fact]
    public async Task UpdateVolumesAsync_ReplacesExistingVolumes()
    {
        var db = CreateInMemoryDb();
        var containerId = Guid.NewGuid();
        var realmId = Guid.NewGuid();
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-volumes-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.Containers.Add(new Container
        {
            Id = containerId,
            RealmId = realmId,
            Name = "test",
            ImageName = "nginx",
            InternalPort = 80,
            CostPerHourBRL = 0.02m,
            Status = ContainerStatus.Stopped,
            CreatedAt = DateTime.UtcNow
        });
        db.ContainerVolumes.AddRange(
            new ContainerVolume { Id = Guid.NewGuid(), ContainerId = containerId, HostPath = Path.Combine(tempDir, "old"), ContainerPath = "/container/old", IsReadOnly = false }
        );
        await db.SaveChangesAsync();

        var logger = NullLogger<ContainerService>.Instance;
        var docker = new MockDockerNetworkService();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "Volumes:BasePath", tempDir }
            })
            .Build();
        var service = new ContainerService(db, docker, config, logger);

        var newVolumes = new List<ContainerVolumeRequest>
        {
            new ContainerVolumeRequest { HostPath = "data", ContainerPath = "/app/data", IsReadOnly = false },
            new ContainerVolumeRequest { HostPath = "config", ContainerPath = "/app/config", IsReadOnly = true }
        };

        await service.UpdateVolumesAsync(containerId, newVolumes, CancellationToken.None);

        var volumes = await db.ContainerVolumes.Where(v => v.ContainerId == containerId).ToListAsync();
        Assert.Equal(2, volumes.Count);
        Assert.DoesNotContain(volumes, v => v.HostPath.Contains("old"));
        Assert.Contains(volumes, v => v.HostPath.Contains("data") && v.ContainerPath == "/app/data");
        Assert.Contains(volumes, v => v.HostPath.Contains("config") && v.IsReadOnly);

        // Cleanup
        Directory.Delete(tempDir, recursive: true);
    }

    [Fact]
    public async Task DeleteAsync_WithRemoveVolumes_RemovesVolumeDirectories()
    {
        var db = CreateInMemoryDb();
        var containerId = Guid.NewGuid();
        var realmId = Guid.NewGuid();
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-volumes-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.Containers.Add(new Container
        {
            Id = containerId,
            RealmId = realmId,
            Name = "test",
            ImageName = "nginx",
            InternalPort = 80,
            CostPerHourBRL = 0.02m,
            Status = ContainerStatus.Stopped,
            CreatedAt = DateTime.UtcNow
        });
        db.ContainerVolumes.Add(new ContainerVolume
        {
            Id = Guid.NewGuid(),
            ContainerId = containerId,
            HostPath = tempDir,
            ContainerPath = "/app/data",
            IsReadOnly = false
        });
        await db.SaveChangesAsync();

        var logger = NullLogger<ContainerService>.Instance;
        var docker = new MockDockerNetworkService();
        var config = new ConfigurationBuilder().Build();
        var service = new ContainerService(db, docker, config, logger);

        await service.DeleteAsync(containerId, removeVolumes: true, CancellationToken.None);

        Assert.False(Directory.Exists(tempDir));
        Assert.Empty(await db.ContainerVolumes.ToListAsync());
        Assert.Empty(await db.ContainerEnvVars.ToListAsync());
    }

    [Fact]
    public async Task DeleteAsync_WithoutRemoveVolumes_KeepsVolumeDirectories()
    {
        var db = CreateInMemoryDb();
        var containerId = Guid.NewGuid();
        var realmId = Guid.NewGuid();
        var tempDir = Path.Combine(Path.GetTempPath(), $"test-volumes-{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        db.Realms.Add(new Realm { Id = realmId, Name = "Test", Slug = "test", IsActive = true, CreatedAt = DateTime.UtcNow });
        db.Containers.Add(new Container
        {
            Id = containerId,
            RealmId = realmId,
            Name = "test",
            ImageName = "nginx",
            InternalPort = 80,
            CostPerHourBRL = 0.02m,
            Status = ContainerStatus.Stopped,
            CreatedAt = DateTime.UtcNow
        });
        db.ContainerVolumes.Add(new ContainerVolume
        {
            Id = Guid.NewGuid(),
            ContainerId = containerId,
            HostPath = tempDir,
            ContainerPath = "/app/data",
            IsReadOnly = false
        });
        await db.SaveChangesAsync();

        var logger = NullLogger<ContainerService>.Instance;
        var docker = new MockDockerNetworkService();
        var config = new ConfigurationBuilder().Build();
        var service = new ContainerService(db, docker, config, logger);

        await service.DeleteAsync(containerId, removeVolumes: false, CancellationToken.None);

        Assert.True(Directory.Exists(tempDir));
        Directory.Delete(tempDir, recursive: true);
    }

    [Fact]
    public void MapToDetail_RealmViewer_MasksEnvVarValues()
    {
        var container = new Container
        {
            Id = Guid.NewGuid(),
            Name = "test",
            ImageName = "nginx",
            InternalPort = 80,
            Status = ContainerStatus.Running,
            CreatedAt = DateTime.UtcNow
        };
        container.Volumes.Add(new ContainerVolume
        {
            Id = Guid.NewGuid(),
            ContainerId = container.Id,
            HostPath = "/host/path",
            ContainerPath = "/container/path",
            IsReadOnly = false
        });
        container.EnvironmentVariables.Add(new ContainerEnvVar
        {
            Id = Guid.NewGuid(),
            ContainerId = container.Id,
            Key = "SECRET_KEY",
            Value = "super_secret_value"
        });

        var claims = new List<Claim> { new Claim(ClaimTypes.Role, "RealmViewer") };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        var response = ContainerCrudServiceTestsAccessor.MapToDetail(container, user) as ContainerDetailResponse;

        Assert.NotNull(response);
        Assert.Single(response.EnvironmentVariables);
        var envVar = response.EnvironmentVariables[0] as ContainerEnvVarSecureDto;
        Assert.NotNull(envVar);
        Assert.Equal("SECRET_KEY", envVar.Key);
        Assert.Equal("***", envVar.Value);
    }

    [Fact]
    public void MapToDetail_RealmOwner_ShowsEnvVarValues()
    {
        var container = new Container
        {
            Id = Guid.NewGuid(),
            Name = "test",
            ImageName = "nginx",
            InternalPort = 80,
            Status = ContainerStatus.Running,
            CreatedAt = DateTime.UtcNow
        };
        container.EnvironmentVariables.Add(new ContainerEnvVar
        {
            Id = Guid.NewGuid(),
            ContainerId = container.Id,
            Key = "SECRET_KEY",
            Value = "super_secret_value"
        });

        var claims = new List<Claim> { new Claim(ClaimTypes.Role, "RealmOwner") };
        var identity = new ClaimsIdentity(claims);
        var user = new ClaimsPrincipal(identity);

        var response = ContainerCrudServiceTestsAccessor.MapToDetail(container, user) as ContainerDetailResponse;

        Assert.NotNull(response);
        Assert.Single(response.EnvironmentVariables);
        var envVar = response.EnvironmentVariables[0] as ContainerEnvVarDto;
        Assert.NotNull(envVar);
        Assert.Equal("SECRET_KEY", envVar.Key);
        Assert.Equal("super_secret_value", envVar.Value);
    }
}

// Helper class to access private static method
public static class ContainerCrudServiceTestsAccessor
{
    public static object MapToDetail(Bzn.Cloudios.Domain.Entities.Container c, System.Security.Claims.ClaimsPrincipal? user)
    {
        var method = typeof(Bzn.Cloudios.Application.Services.ContainerCrudService)
            .GetMethod("MapToDetail", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return method!.Invoke(null, new object[] { c, user })!;
    }
}

public class MockDockerNetworkService : IDockerNetworkService
{
    public Task EnsureNetworkAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<ContainerStats>> GetContainerStatsAsync(CancellationToken ct = default) => Task.FromResult(new List<ContainerStats>());
    public Task<T?> SendRequestAsync<T>(string method, string path, string? body = null, CancellationToken ct = default) => Task.FromResult(default(T));
    public Task<List<ContainerLogEntry>> GetContainerLogsAsync(string dockerContainerId, int tail = 100, CancellationToken ct = default) => Task.FromResult(new List<ContainerLogEntry>());
}
