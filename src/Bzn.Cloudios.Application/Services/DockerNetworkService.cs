using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Bzn.Cloudios.Application.Services;

public sealed class DockerNetworkService : IDockerNetworkService
{
    private readonly ILogger<DockerNetworkService> _logger;
    private readonly DockerClient _dockerClient;

    public DockerNetworkService(DockerClient dockerClient, ILogger<DockerNetworkService> logger)
    {
        _dockerClient = dockerClient;
        _logger = logger;
        _logger.LogInformation("Docker client initialized via dependency injection");
    }

    public async Task EnsureNetworkAsync(CancellationToken ct = default)
    {
        try
        {
            var networks = await _dockerClient.Networks.ListNetworksAsync();
            var exists = networks.Any(n => n.Name == "cloudios_internal");

            if (exists)
            {
                _logger.LogInformation("Docker network cloudios_internal already exists");
                return;
            }

            var createParams = new NetworksCreateParameters
            {
                Name = "cloudios_internal",
                Driver = "bridge",
                IPAM = new IPAM
                {
                    Config = new List<IPAMConfig>
                    {
                        new IPAMConfig { Subnet = "172.20.0.0/16" }
                    }
                }
            };

            await _dockerClient.Networks.CreateNetworkAsync(createParams);
            _logger.LogInformation("Docker network cloudios_internal created");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Docker network cloudios_internal");
        }
    }

    public async Task EnsureRealmNetworkAsync(Guid realmId, CancellationToken ct = default)
    {
        try
        {
            var networkName = $"cloudios_{realmId:N}";
            var networks = await _dockerClient.Networks.ListNetworksAsync();
            var exists = networks.Any(n => n.Name == networkName);

            if (exists)
            {
                _logger.LogInformation("Docker network {NetworkName} already exists", networkName);
                return;
            }

            // Generate a unique subnet for each realm based on realm ID
            var subnet = GenerateSubnetForRealm(realmId);

            var createParams = new NetworksCreateParameters
            {
                Name = networkName,
                Driver = "bridge",
                IPAM = new IPAM
                {
                    Config = new List<IPAMConfig>
                    {
                        new IPAMConfig { Subnet = subnet }
                    }
                }
            };

            await _dockerClient.Networks.CreateNetworkAsync(createParams);
            _logger.LogInformation("Docker network {NetworkName} created with subnet {Subnet}", networkName, subnet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Docker network for realm {RealmId}", realmId);
        }
    }

    private static string GenerateSubnetForRealm(Guid realmId)
    {
        // Use the first byte of the GUID to generate a unique subnet in the 172.21.x.0/24 range
        var bytes = realmId.ToByteArray();
        var thirdOctet = bytes[0] % 254 + 1; // Ensure it's between 1 and 254
        return $"172.21.{thirdOctet}.0/24";
    }

    public async Task<List<string>> ListNetworksAsync(CancellationToken ct = default)
    {
        try
        {
            var networks = await _dockerClient.Networks.ListNetworksAsync(new NetworksListParameters(), ct);
            var networkNames = networks
                .Where(n => n.Name != null && n.Name.StartsWith("cloudios_"))
                .Select(n => n.Name)
                .ToList();

            _logger.LogInformation("Found {Count} Cloudios networks", networkNames.Count);
            return networkNames;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to list Docker networks");
            return [];
        }
    }

    public async Task<List<ContainerStats>> GetContainerStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var containers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }, ct);

            var managedContainers = containers.Where(c =>
                c.Labels != null &&
                c.Labels.TryGetValue("cloudios.managed", out var managed) &&
                managed == "true").ToList();

            var stats = new List<ContainerStats>();
            foreach (var container in managedContainers)
            {
                var statsStream = await _dockerClient.Containers.GetContainerStatsAsync(
                    container.ID, new ContainerStatsParameters { Stream = false }, ct);

                if (statsStream == null) continue;

                var statsResponse = await System.Text.Json.JsonSerializer.DeserializeAsync<ContainerStatsResponse>(statsStream);
                if (statsResponse == null) continue;

                var cpuStats = statsResponse.CPUStats;
                var memoryStats = statsResponse.MemoryStats;
                var preCpuStats = statsResponse.PreCPUStats;

                var cpuPercent = CalculateCpuPercent(cpuStats, preCpuStats);
                var memoryUsed = memoryStats != null ? (long)memoryStats.Usage : 0;
                var networkRx = 0L;
                var networkTx = 0L;
                var blockRead = 0L;
                var blockWrite = 0L;

                if (statsResponse.Networks != null)
                {
                    foreach (var net in statsResponse.Networks.Values)
                    {
                        networkRx += (long)net.RxBytes;
                        networkTx += (long)net.TxBytes;
                    }
                }

                if (statsResponse.BlockIO != null && statsResponse.BlockIO.IoServiceBytesRecursive != null)
                {
                    foreach (var entry in statsResponse.BlockIO.IoServiceBytesRecursive)
                    {
                        if (entry.Op == "Read") blockRead += (long)entry.Value;
                        if (entry.Op == "Write") blockWrite += (long)entry.Value;
                    }
                }

                var name = container.Names.FirstOrDefault()?.TrimStart('/') ?? "";

                stats.Add(new ContainerStats
                {
                    ContainerId = container.ID,
                    ContainerName = name,
                    CpuPercent = cpuPercent,
                    MemoryUsedBytes = memoryUsed,
                    NetworkRxBytes = networkRx,
                    NetworkTxBytes = networkTx,
                    BlockReadBytes = blockRead,
                    BlockWriteBytes = blockWrite
                });
            }

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get container stats");
            return [];
        }
    }

    private class ContainerStatsResponse
    {
        public CPUStats? CPUStats { get; set; }
        public MemoryStats? MemoryStats { get; set; }
        public CPUStats? PreCPUStats { get; set; }
        public Dictionary<string, NetworkStats>? Networks { get; set; }
        public BlockIOStatsResponse? BlockIO { get; set; }
    }

    private class BlockIOStatsResponse
    {
        public List<BlockIOEntry>? IoServiceBytesRecursive { get; set; }
    }

    private class BlockIOEntry
    {
        public string? Op { get; set; }
        public ulong Value { get; set; }
    }

    private static double CalculateCpuPercent(CPUStats cpuStats, CPUStats? preCpuStats)
    {
        if (cpuStats == null || cpuStats.CPUUsage == null) return 0;
        if (cpuStats.SystemUsage == 0) return 0;

        var totalUsage = (long)cpuStats.CPUUsage.TotalUsage;
        var systemUsage = (long)cpuStats.SystemUsage;

        long preTotalUsage = 0;
        long preSystemUsage = 0;

        if (preCpuStats != null && preCpuStats.CPUUsage != null)
        {
            preTotalUsage = (long)preCpuStats.CPUUsage.TotalUsage;
            preSystemUsage = (long)preCpuStats.SystemUsage;
        }

        var cpuDelta = totalUsage - preTotalUsage;
        var systemDelta = systemUsage - preSystemUsage;

        if (systemDelta <= 0) return 0;
        return (cpuDelta / (double)systemDelta) * 100.0;
    }

    public async Task<List<ContainerLogEntry>> GetContainerLogsAsync(string dockerContainerId, int tail = 100, CancellationToken ct = default)
    {
        try
        {
            _logger.LogInformation("Getting logs for container {DockerId}", dockerContainerId);

            var logsParams = new ContainerLogsParameters
            {
                ShowStdout = true,
                ShowStderr = true,
                Timestamps = true,
                Tail = tail.ToString()
            };

            // Get the log stream (using deprecated method that works with StreamReader)
            var logStream = await _dockerClient.Containers.GetContainerLogsAsync(dockerContainerId, logsParams, ct);

            using var reader = new System.IO.StreamReader(logStream);
            var logs = await reader.ReadToEndAsync(ct);

            _logger.LogInformation("Retrieved {LogLength} characters of logs for container {DockerId}", logs?.Length ?? 0, dockerContainerId);

            if (string.IsNullOrEmpty(logs))
                return [];

            var entries = new List<ContainerLogEntry>();
            var lines = logs.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            _logger.LogInformation("Parsed {LineCount} log lines for container {DockerId}", lines.Length, dockerContainerId);

            foreach (var line in lines)
            {
                var timestamp = DateTime.UtcNow;
                var message = line;
                var stream = "stdout";

                // Parse timestamp from payload (format: "2024-01-01T00:00:00.000000000Z message")
                var spaceIndex = line.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    var timestampStr = line[..spaceIndex];
                    if (DateTime.TryParse(timestampStr, out var parsedTs))
                        timestamp = parsedTs;
                    message = line[(spaceIndex + 1)..];
                }

                entries.Add(new ContainerLogEntry
                {
                    Timestamp = timestamp,
                    Stream = stream,
                    Line = message
                });
            }

            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get logs for container {DockerId}", dockerContainerId);
            return [];
        }
    }
}
