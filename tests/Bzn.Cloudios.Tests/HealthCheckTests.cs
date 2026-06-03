using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Infrastructure.Persistence;
using Docker.DotNet;
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
        var dockerClient = new MockDockerClient(healthy: true);
        var lifetime = new MockHostApplicationLifetime();
        var config = new ConfigurationBuilder().Build();

        var service = new HealthCheckService(mainDb, metricsDb, dockerClient, lifetime, config);
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
        var dockerClient = new MockDockerClient(healthy: false);
        var lifetime = new MockHostApplicationLifetime();
        var config = new ConfigurationBuilder().Build();

        var service = new HealthCheckService(mainDb, metricsDb, dockerClient, lifetime, config);
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
        var dockerClient = new MockDockerClient(healthy: true);
        var lifetime = new MockHostApplicationLifetime();
        var config = new ConfigurationBuilder().Build();

        var service = new HealthCheckService(mainDb, metricsDb, dockerClient, lifetime, config);
        var result = await service.CheckHealthAsync(CancellationToken.None);

        Assert.Equal("Unhealthy", result.Status);
    }

    [Fact]
    public void GetVersion_UsesEnvVar_WhenSet()
    {
        var mainDb = CreateInMemoryDb();
        var metricsDb = CreateInMemoryMetricsDb();
        var dockerClient = new MockDockerClient(healthy: true);
        var lifetime = new MockHostApplicationLifetime();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new[] { new KeyValuePair<string, string?>("CLOUDIOS_VERSION", "2.0.0") })
            .Build();

        var service = new HealthCheckService(mainDb, metricsDb, dockerClient, lifetime, config);
        var result = service.CheckHealthAsync(CancellationToken.None).Result;

        Assert.Equal("2.0.0", result.Version);
    }

    [Fact]
    public async Task GetHostMetricsAsync_ReturnsMetrics()
    {
        var mainDb = CreateInMemoryDb();
        var metricsDb = CreateInMemoryMetricsDb();
        var dockerClient = new MockDockerClient(healthy: true, returnSystemInfo: true);
        var lifetime = new MockHostApplicationLifetime();
        var config = new ConfigurationBuilder().Build();

        var service = new HealthCheckService(mainDb, metricsDb, dockerClient, lifetime, config);
        var metrics = await service.GetHostMetricsAsync(CancellationToken.None);

        Assert.NotNull(metrics);
        Assert.True(metrics.TotalCpuPercent >= 0);
        Assert.True(metrics.TotalMemoryTotalBytes > 0);
        Assert.True(metrics.ActiveContainers >= 0);
    }
}

public class MockDockerClient : DockerClient
{
    private readonly bool _healthy;
    private readonly bool _returnSystemInfo;

    public MockDockerClient(bool healthy = true, bool returnSystemInfo = false) : base(new Uri("unix:///var/run/docker.sock"))
    {
        _healthy = healthy;
        _returnSystemInfo = returnSystemInfo;
    }

    public new Task<SystemInfoResponseBody> GetSystemInfoAsync(CancellationToken ct = default)
    {
        if (!_healthy)
            throw new InvalidOperationException("Docker connection failed");

        if (_returnSystemInfo)
        {
            return Task.FromResult(new SystemInfoResponseBody
            {
                NCPU = 4,
                MemTotal = 17179869184
            });
        }

        return Task.FromResult(new SystemInfoResponseBody());
    }

    public new Task<IList<ContainerListResponse>> ListContainersAsync(ContainersListParameters parameters, CancellationToken ct = default)
    {
        if (!_healthy)
            throw new InvalidOperationException("Docker connection failed");

        if (_returnSystemInfo)
        {
            return Task.FromResult<IList<ContainerListResponse>>(new List<ContainerListResponse>
            {
                new ContainerListResponse { ID = "abc123" }
            });
        }

        return Task.FromResult<IList<ContainerListResponse>>(new List<ContainerListResponse>());
    }

    public new Task<SystemPingResponse> PingAsync(CancellationToken ct = default)
    {
        if (!_healthy)
            throw new InvalidOperationException("Docker connection failed");

        return Task.FromResult(new SystemPingResponse());
    }
}

public class MockHostApplicationLifetime : IHostApplicationLifetime
{
    public CancellationToken ApplicationStarted => CancellationToken.None;
    public CancellationToken ApplicationStopping => CancellationToken.None;
    public CancellationToken ApplicationStopped => CancellationToken.None;
    public void StopApplication() { }
}
