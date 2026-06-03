using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using System.Text.Json;

namespace Bzn.Cloudios.Application.Services;

public sealed class HealthCheckService
{
    private readonly CloudiosDbContext _mainDb;
    private readonly MetricsDbContext _metricsDb;
    private readonly IDockerNetworkService _dockerNetwork;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly IConfiguration _configuration;

    public HealthCheckService(
        CloudiosDbContext mainDb,
        MetricsDbContext metricsDb,
        IDockerNetworkService dockerNetwork,
        IHostApplicationLifetime lifetime,
        IConfiguration configuration)
    {
        _mainDb = mainDb;
        _metricsDb = metricsDb;
        _dockerNetwork = dockerNetwork;
        _lifetime = lifetime;
        _configuration = configuration;
    }

    public async Task<HealthCheckResponse> CheckHealthAsync(CancellationToken ct = default)
    {
        var response = new HealthCheckResponse
        {
            Status = "Healthy",
            Version = GetVersion(),
            Uptime = GetUptime(),
            Details = new Dictionary<string, string>()
        };

        var issues = new List<string>();

        // Check main database
        try
        {
            await _mainDb.Database.CanConnectAsync(ct);
            response.Details["main_db"] = "connected";
        }
        catch (Exception ex)
        {
            issues.Add($"main_db: {ex.Message}");
            response.Details["main_db"] = "failed";
        }

        // Check metrics database
        try
        {
            await _metricsDb.Database.CanConnectAsync(ct);
            response.Details["metrics_db"] = "connected";
        }
        catch (Exception ex)
        {
            issues.Add($"metrics_db: {ex.Message}");
            response.Details["metrics_db"] = "failed";
        }

        // Check Docker socket
        try
        {
            await _dockerNetwork.SendRequestAsync<object>("GET", "/_ping", ct: ct);
            response.Details["docker"] = "connected";
        }
        catch (Exception ex)
        {
            issues.Add($"docker: {ex.Message}");
            response.Details["docker"] = "failed";
        }

        if (issues.Count > 0)
        {
            response.Status = issues.Count == 1 && issues[0].StartsWith("docker") ? "Degraded" : "Unhealthy";
        }

        return response;
    }

    public async Task<HostMetricsResponse> GetHostMetricsAsync(CancellationToken ct = default)
    {
        // Get CPU and memory from Docker SystemInfo
        var cpuPercent = 0.0;
        var memoryUsed = 0L;
        var memoryTotal = 0L;

        try
        {
            var systemInfo = await _dockerNetwork.SendRequestAsync<JsonElement>("GET", "/system/info", ct: ct);
            if (systemInfo.ValueKind != JsonValueKind.Undefined)
            {
                cpuPercent = systemInfo.TryGetProperty("NCPU", out var ncpu) ? ncpu.GetInt32() * 1.0 : 0.0;
                if (systemInfo.TryGetProperty("MemTotal", out var memTotal))
                {
                    memoryTotal = memTotal.GetInt64();
                }
                if (systemInfo.TryGetProperty("MemUsed", out var memUsed))
                {
                    memoryUsed = memUsed.GetInt64();
                }
            }
        }
        catch
        {
            // Fallback to system metrics if Docker fails
        }

        // Disk info
        var diskInfo = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "/");
        var diskUsed = diskInfo.TotalSize - diskInfo.AvailableFreeSpace;
        var diskTotal = diskInfo.TotalSize;

        // Active containers count
        var activeContainers = 0;
        try
        {
            var containers = await _dockerNetwork.SendRequestAsync<JsonElement>("GET", "/containers/json?all=false&filters={\"label\":[\"cloudios.managed=true\"]}", ct: ct);
            if (containers.ValueKind != JsonValueKind.Undefined && containers.ValueKind == JsonValueKind.Array)
            {
                activeContainers = containers.GetArrayLength();
            }
        }
        catch
        {
            // Ignore if container list fails
        }

        return new HostMetricsResponse
        {
            TotalCpuPercent = cpuPercent,
            TotalMemoryUsedBytes = memoryUsed,
            TotalMemoryTotalBytes = memoryTotal,
            DiskUsedBytes = diskUsed,
            DiskTotalBytes = diskTotal,
            ActiveContainers = activeContainers
        };
    }

    private string GetVersion()
    {
        return _configuration["CLOUDIOS_VERSION"] 
            ?? System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() 
            ?? "1.0.0";
    }

    private string GetUptime()
    {
        var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
        return $"{uptime.Hours:D2}:{uptime.Minutes:D2}:{uptime.Seconds:D2}";
    }
}
