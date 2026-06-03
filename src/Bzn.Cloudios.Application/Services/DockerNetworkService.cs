using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Bzn.Cloudios.Application.Services;

public sealed class DockerNetworkService : IDockerNetworkService
{
    private readonly ILogger<DockerNetworkService> _logger;
    private readonly string _socketPath;

    public DockerNetworkService(IConfiguration configuration, ILogger<DockerNetworkService> logger)
    {
        // Use Windows named pipe for Docker on Windows, Unix socket on Linux/Mac
        if (OperatingSystem.IsWindows())
        {
            _socketPath = configuration["Docker:SocketPath"] ?? @"\\.\pipe\docker_engine";
        }
        else
        {
            _socketPath = configuration["Docker:SocketPath"] ?? "/var/run/docker.sock";
        }
        _logger = logger;
    }

    public async Task EnsureNetworkAsync(CancellationToken ct = default)
    {
        try
        {
            var networks = await SendRequestAsync<List<JsonElement>>("GET", "/networks", ct: ct);
            if (networks is null) return;

            var exists = networks.Any(n =>
                n.TryGetProperty("Name", out var name) && name.GetString() == "cloudios_internal");

            if (exists)
            {
                _logger.LogInformation("Docker network cloudios_internal already exists");
                return;
            }

            var body = JsonSerializer.Serialize(new
            {
                Name = "cloudios_internal",
                Driver = "bridge",
                IPAM = new
                {
                    Config = new[]
                    {
                        new { Subnet = "172.20.0.0/16" }
                    }
                }
            });

            await SendRequestAsync<JsonElement>("POST", "/networks/create", body, ct);
            _logger.LogInformation("Docker network cloudios_internal created");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure Docker network cloudios_internal");
        }
    }

    public async Task<List<ContainerStats>> GetContainerStatsAsync(CancellationToken ct = default)
    {
        try
        {
            var containers = await SendRequestAsync<List<JsonElement>>("GET", "/containers/json", ct: ct);
            if (containers is null) return [];

            var managedContainers = containers.Where(c =>
            {
                if (!c.TryGetProperty("Labels", out var labels)) return false;
                if (!labels.TryGetProperty("cloudios.managed", out var managed)) return false;
                return managed.GetString() == "true";
            }).ToList();

            var stats = new List<ContainerStats>();
            foreach (var container in managedContainers)
            {
                var id = container.GetProperty("Id").GetString() ?? "";
                var name = container.GetProperty("Names")[0].GetString() ?? "";
                var statsResponse = await SendRequestAsync<JsonElement?>("GET", $"/containers/{id}/stats?stream=false", ct: ct);
                if (statsResponse is null) continue;

                if (!statsResponse.Value.TryGetProperty("cpu_stats", out var cpuStatsObj)) continue;
                if (cpuStatsObj.ValueKind == JsonValueKind.Null) continue;
                if (!statsResponse.Value.TryGetProperty("memory_stats", out var memoryStatsObj)) continue;
                if (memoryStatsObj.ValueKind == JsonValueKind.Null) continue;

                var cpuStats = cpuStatsObj;
                var memoryStats = memoryStatsObj;
                var preCpuStats = statsResponse.Value.TryGetProperty("precpu_stats", out var pcs) && pcs.ValueKind != JsonValueKind.Null ? pcs : (JsonElement?)null;
                var networkStats = statsResponse.Value.TryGetProperty("networks", out var nets) && nets.ValueKind != JsonValueKind.Null ? nets : (JsonElement?)null;
                var blockStats = statsResponse.Value.TryGetProperty("blkio_stats", out var blk) && blk.ValueKind != JsonValueKind.Null ? blk : (JsonElement?)null;

                var cpuPercent = CalculateCpuPercent(cpuStats, preCpuStats);
                var memoryUsed = memoryStats.GetProperty("usage").GetInt64();
                var networkRx = 0L;
                var networkTx = 0L;
                var blockRead = 0L;
                var blockWrite = 0L;

                if (networkStats.HasValue)
                {
                    foreach (var net in networkStats.Value.EnumerateObject())
                    {
                        var rx = net.Value.TryGetProperty("rx_bytes", out var r) ? r.GetInt64() : 0;
                        var tx = net.Value.TryGetProperty("tx_bytes", out var t) ? t.GetInt64() : 0;
                        networkRx += rx;
                        networkTx += tx;
                    }
                }

                if (blockStats.HasValue)
                {
                    var ioService = blockStats.Value.TryGetProperty("io_service_bytes_recursive", out var io) && io.ValueKind != JsonValueKind.Null ? io : (JsonElement?)null;
                    if (ioService.HasValue)
                    {
                        foreach (var entry in ioService.Value.EnumerateArray())
                        {
                            var op = entry.TryGetProperty("op", out var o) ? o.GetString() : "";
                            var value = entry.TryGetProperty("value", out var v) ? v.GetInt64() : 0;
                            if (op == "Read") blockRead += value;
                            if (op == "Write") blockWrite += value;
                        }
                    }
                }

                stats.Add(new ContainerStats
                {
                    ContainerId = id,
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

    private static double CalculateCpuPercent(JsonElement cpuStats, JsonElement? preCpuStats)
    {
        if (!cpuStats.TryGetProperty("cpu_usage", out var cpuUsage)) return 0;
        if (!cpuUsage.TryGetProperty("total_usage", out var totalUsage)) return 0;
        if (!cpuStats.TryGetProperty("system_cpu_usage", out var systemUsage)) return 0;

        long preTotalUsage = 0;
        long preSystemUsage = 0;

        if (preCpuStats.HasValue)
        {
            if (preCpuStats.Value.TryGetProperty("cpu_usage", out var preCpuUsage) &&
                preCpuUsage.TryGetProperty("total_usage", out var preTotal))
            {
                preTotalUsage = preTotal.GetInt64();
            }
            if (preCpuStats.Value.TryGetProperty("system_cpu_usage", out var preSystem))
            {
                preSystemUsage = preSystem.GetInt64();
            }
        }

        var cpuDelta = totalUsage.GetInt64() - preTotalUsage;
        var systemDelta = systemUsage.GetInt64() - preSystemUsage;

        if (systemDelta <= 0) return 0;
        return (cpuDelta / (double)systemDelta) * 100.0;
    }

    public async Task<T?> SendRequestAsync<T>(string method, string path, string? body = null, CancellationToken ct = default)
    {
        using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
        var endpoint = new UnixDomainSocketEndPoint(_socketPath);
        await socket.ConnectAsync(endpoint, ct);

        using var stream = new NetworkStream(socket, ownsSocket: true);
        using var writer = new StreamWriter(stream, leaveOpen: true) { AutoFlush = true };

        var request = new StringBuilder();
        request.Append($"{method} {path} HTTP/1.1\r\n");
        request.Append("Host: localhost\r\n");

        if (body is not null)
        {
            var bodyBytes = Encoding.UTF8.GetBytes(body);
            request.Append($"Content-Length: {bodyBytes.Length}\r\n");
            request.Append("Content-Type: application/json\r\n");
            request.Append("\r\n");

            await writer.WriteAsync(request.ToString().AsMemory(), ct);
            await stream.WriteAsync(bodyBytes, ct);
        }
        else
        {
            request.Append("\r\n");
            await writer.WriteAsync(request.ToString().AsMemory(), ct);
        }

        // Read response
        var buffer = new byte[8192];
        var totalBytes = new List<byte>();
        var read = await stream.ReadAsync(buffer, ct);
        while (read > 0)
        {
            totalBytes.AddRange(buffer.AsSpan(0, read).ToArray());
            if (!stream.DataAvailable) break;
            read = await stream.ReadAsync(buffer, ct);
        }

        var responseStr = Encoding.UTF8.GetString(totalBytes.ToArray());
        var bodyStart = responseStr.IndexOf("\r\n\r\n");
        if (bodyStart < 0) return default;

        var responseBody = responseStr[(bodyStart + 4)..];

        if (string.IsNullOrWhiteSpace(responseBody)) return default;

        return JsonSerializer.Deserialize<T>(responseBody);
    }

    public async Task<List<ContainerLogEntry>> GetContainerLogsAsync(string dockerContainerId, int tail = 100, CancellationToken ct = default)
    {
        try
        {
            var logs = await SendRequestAsync<byte[]>(
                "GET", $"/containers/{dockerContainerId}/logs?stdout=true&stderr=true&timestamps=true&tail={tail}", ct: ct);

            if (logs is null || logs.Length == 0)
                return [];

            var entries = new List<ContainerLogEntry>();
            var offset = 0;

            while (offset < logs.Length)
            {
                if (offset + 8 > logs.Length)
                    break;

                // Docker log format: 8-byte header [stream_type (1), unused (3), size (4)]
                var streamType = logs[offset];
                var size = BitConverter.ToInt32(logs, offset + 4);

                offset += 8;

                if (offset + size > logs.Length)
                    break;

                var payload = Encoding.UTF8.GetString(logs, offset, size);
                offset += size;

                var stream = streamType == 1 ? "stdout" : streamType == 2 ? "stderr" : "unknown";

                // Parse timestamp from payload (format: "2024-01-01T00:00:00.000000000Z message")
                var timestamp = DateTime.UtcNow;
                var message = payload;
                var spaceIndex = payload.IndexOf(' ');
                if (spaceIndex > 0)
                {
                    var timestampStr = payload[..spaceIndex];
                    if (DateTime.TryParse(timestampStr, out var parsedTs))
                        timestamp = parsedTs;
                    message = payload[(spaceIndex + 1)..];
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
