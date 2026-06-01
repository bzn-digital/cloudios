using Bzn.Cloudios.Application.Abstractions;
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

public sealed class DockerNetworkService
{
    private readonly ILogger<DockerNetworkService> _logger;
    private readonly string _socketPath;

    public DockerNetworkService(IConfiguration configuration, ILogger<DockerNetworkService> logger)
    {
        _socketPath = configuration["Docker:SocketPath"] ?? "/var/run/docker.sock";
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

    internal async Task<T?> SendRequestAsync<T>(string method, string path, string? body = null, CancellationToken ct = default)
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
}
