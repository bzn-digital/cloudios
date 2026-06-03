using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace Bzn.Cloudios.Tests;

public class HealthCheckTests
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

    private static MetricsDbContext CreateInMemoryMetricsDb()
    {
        var options = new DbContextOptionsBuilder<MetricsDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var db = new MetricsDbContext(options);
        db.Database.OpenConnection();
        db.Database.EnsureCreated();
        return db;
    }

    [Fact]
    public async Task CheckHealthAsync_AllHealthy_ReturnsHealthy()
    {
        var mainDb = CreateInMemoryDb();
        var metricsDb = CreateInMemoryMetricsDb();
        var dockerNetwork = new MockDockerNetworkServiceForHealth(healthy: true);
        var lifetime = new MockHostApplicationLifetime();
        var config = new ConfigurationBuilder().Build();

        var service = new HealthCheckService(mainDb, metricsDb, dockerNetwork, lifetime, config);
        var result = await service.CheckHealthAsync(CancellationToken.None);

        Assert.Equal("Healthy", result.Status);
        Assert.Contains("connected", result.Details["main_db"]);
        Assert.Contains("connected", result.Details["metrics_db"]);
        Assert.Contains("connected", result.Details["docker"]);
    }

    [Fact]
    public async Task CheckHealthAsync_DockerFails_ReturnsDegraded()
    {
        var mainDb = CreateInMemoryDb();
        var metricsDb = CreateInMemoryMetricsDb();
        var dockerNetwork = new MockDockerNetworkServiceForHealth(healthy: false);
        var lifetime = new MockHostApplicationLifetime();
        var config = new ConfigurationBuilder().Build();

        var service = new HealthCheckService(mainDb, metricsDb, dockerNetwork, lifetime, config);
        var result = await service.CheckHealthAsync(CancellationToken.None);

        Assert.Equal("Degraded", result.Status);
        Assert.Contains("failed", result.Details["docker"]);
    }

    [Fact]
    public async Task CheckHealthAsync_DatabaseFails_ReturnsUnhealthy()
    {
        var mainDb = CreateInMemoryDb();
        var metricsDb = CreateInMemoryMetricsDb();
        // Simulate database failure by disposing the context
        await mainDb.DisposeAsync();
        var dockerNetwork = new MockDockerNetworkServiceForHealth(healthy: true);
        var lifetime = new MockHostApplicationLifetime();
        var config = new ConfigurationBuilder().Build();

        var service = new HealthCheckService(mainDb, metricsDb, dockerNetwork, lifetime, config);
        var result = await service.CheckHealthAsync(CancellationToken.None);

        Assert.Equal("Unhealthy", result.Status);
    }

    [Fact]
    public void GetVersion_UsesEnvVar_WhenSet()
    {
        var mainDb = CreateInMemoryDb();
        var metricsDb = CreateInMemoryMetricsDb();
        var dockerNetwork = new MockDockerNetworkServiceForHealth(healthy: true);
        var lifetime = new MockHostApplicationLifetime();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("CLOUDIOS_VERSION", "2.0.0") })
            .Build();

        var service = new HealthCheckService(mainDb, metricsDb, dockerNetwork, lifetime, config);
        var result = service.CheckHealthAsync(CancellationToken.None).Result;

        Assert.Equal("2.0.0", result.Version);
    }

    [Fact]
    public async Task GetHostMetricsAsync_ReturnsMetrics()
    {
        var mainDb = CreateInMemoryDb();
        var metricsDb = CreateInMemoryMetricsDb();
        var dockerNetwork = new MockDockerNetworkServiceForHealth(healthy: true, returnSystemInfo: true);
        var lifetime = new MockHostApplicationLifetime();
        var config = new ConfigurationBuilder().Build();

        var service = new HealthCheckService(mainDb, metricsDb, dockerNetwork, lifetime, config);
        var metrics = await service.GetHostMetricsAsync(CancellationToken.None);

        Assert.NotNull(metrics);
        Assert.True(metrics.TotalCpuPercent >= 0);
        Assert.True(metrics.TotalMemoryTotalBytes > 0);
        Assert.True(metrics.ActiveContainers >= 0);
    }
}

public class MockDockerNetworkServiceForHealth : IDockerNetworkService
{
    private readonly bool _healthy;
    private readonly bool _returnSystemInfo;

    public MockDockerNetworkServiceForHealth(bool healthy = true, bool returnSystemInfo = false)
    {
        _healthy = healthy;
        _returnSystemInfo = returnSystemInfo;
    }

    public Task EnsureNetworkAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task EnsureRealmNetworkAsync(Guid realmId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<string>> ListNetworksAsync(CancellationToken ct = default) => Task.FromResult(new List<string>());
    public Task<List<ContainerStats>> GetContainerStatsAsync(CancellationToken ct = default) => Task.FromResult(new List<ContainerStats>());
    public Task<List<ContainerLogEntry>> GetContainerLogsAsync(string dockerContainerId, int tail = 100, CancellationToken ct = default) => Task.FromResult(new List<ContainerLogEntry>());

    public Task<T?> SendRequestAsync<T>(string method, string path, string? body = null, CancellationToken ct = default)
    {
        if (!_healthy)
            throw new InvalidOperationException("Docker connection failed");

        if (_returnSystemInfo && path.Contains("system/info"))
        {
            var systemInfoJson = @"{
                ""NCPU"": 4,
                ""MemTotal"": 17179869184,
                ""MemUsed"": 8589934592
            }";
            var element = JsonSerializer.Deserialize<JsonElement>(systemInfoJson);
            return Task.FromResult((T?)(object)element);
        }

        if (_returnSystemInfo && path.Contains("containers/json"))
        {
            var containersJson = @"[{""Id"": ""abc123""}]";
            var element = JsonSerializer.Deserialize<JsonElement>(containersJson);
            return Task.FromResult((T?)(object)element);
        }

        return Task.FromResult(default(T));
    }
}

public class MockHostApplicationLifetime : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;
    public void StopApplication() { }
}
