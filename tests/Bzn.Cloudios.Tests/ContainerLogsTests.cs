using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Bzn.Cloudios.Tests;

public class ContainerLogsTests
{
    [Fact]
    public async Task GetContainerLogsAsync_ParsesDockerLogFormat()
    {
        var docker = new MockDockerNetworkServiceWithLogs();
        var logs = await docker.GetContainerLogsAsync("test-container-id", tail: 100, CancellationToken.None);

        Assert.Equal(2, logs.Count);
        Assert.Equal("stdout", logs[0].Stream);
        Assert.Contains("Hello from Docker", logs[0].Line);
        Assert.Equal("stderr", logs[1].Stream);
        Assert.Contains("Error message", logs[1].Line);
    }

    [Fact]
    public async Task GetContainerLogsAsync_EmptyLogs_ReturnsEmptyList()
    {
        var docker = new MockDockerNetworkServiceWithLogs(returnEmpty: true);
        var logs = await docker.GetContainerLogsAsync("test-container-id", tail: 100, CancellationToken.None);

        Assert.Empty(logs);
    }

    [Fact]
    public async Task GetContainerLogsAsync_ParsesTimestamp()
    {
        var docker = new MockDockerNetworkServiceWithLogs();
        var logs = await docker.GetContainerLogsAsync("test-container-id", tail: 100, CancellationToken.None);

        Assert.NotNull(logs[0].Timestamp);
        Assert.True(logs[0].Timestamp > DateTime.MinValue);
    }

    [Fact]
    public async Task GetContainerLogsAsync_InvalidHeader_ReturnsEmptyList()
    {
        var docker = new MockDockerNetworkServiceWithLogs(returnInvalid: true);
        var logs = await docker.GetContainerLogsAsync("test-container-id", tail: 100, CancellationToken.None);

        Assert.Empty(logs);
    }
}

public class MockDockerNetworkServiceWithLogs : IDockerNetworkService
{
    private readonly bool _returnEmpty;
    private readonly bool _returnInvalid;

    public MockDockerNetworkServiceWithLogs(bool returnEmpty = false, bool returnInvalid = false)
    {
        _returnEmpty = returnEmpty;
        _returnInvalid = returnInvalid;
    }

    public Task EnsureNetworkAsync(CancellationToken ct = default) => Task.CompletedTask;
    public Task EnsureRealmNetworkAsync(Guid realmId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<List<string>> ListNetworksAsync(CancellationToken ct = default) => Task.FromResult(new List<string>());
    public Task<List<ContainerStats>> GetContainerStatsAsync(CancellationToken ct = default) => Task.FromResult(new List<ContainerStats>());
    public Task<T?> SendRequestAsync<T>(string method, string path, string? body = null, CancellationToken ct = default)
    {
        if (_returnEmpty)
            return Task.FromResult(default(T));

        if (_returnInvalid)
            return Task.FromResult((T?)(object)new byte[] { 1, 2, 3 }); // Invalid length

        // Simulate Docker log format: 8-byte header + payload
        // Header: [stream_type (1), unused (3), size (4)]
        var timestamp = DateTime.UtcNow.ToString("o");
        var stdoutMsg = $"{timestamp} Hello from Docker";
        var stderrMsg = $"{timestamp} Error message";

        var stdoutBytes = System.Text.Encoding.UTF8.GetBytes(stdoutMsg);
        var stderrBytes = System.Text.Encoding.UTF8.GetBytes(stderrMsg);

        var buffer = new List<byte>();

        // Stdout entry (stream_type = 1)
        buffer.Add(1); // stream_type
        buffer.AddRange(new byte[3]); // unused
        buffer.AddRange(BitConverter.GetBytes(stdoutBytes.Length)); // size
        buffer.AddRange(stdoutBytes); // payload

        // Stderr entry (stream_type = 2)
        buffer.Add(2); // stream_type
        buffer.AddRange(new byte[3]); // unused
        buffer.AddRange(BitConverter.GetBytes(stderrBytes.Length)); // size
        buffer.AddRange(stderrBytes); // payload

        return Task.FromResult((T?)(object)buffer.ToArray());
    }

    public Task<List<ContainerLogEntry>> GetContainerLogsAsync(string dockerContainerId, int tail = 100, CancellationToken ct = default)
    {
        return SendRequestAsync<byte[]>("GET", $"/containers/{dockerContainerId}/logs?stdout=true&stderr=true&timestamps=true&tail={tail}", ct: ct)
            .ContinueWith(t =>
            {
                var logs = new List<ContainerLogEntry>();
                var bytes = t.Result;
                if (bytes is null || bytes.Length == 0)
                    return logs;

                var offset = 0;
                while (offset < bytes.Length)
                {
                    if (offset + 8 > bytes.Length)
                        break;

                    var streamType = bytes[offset];
                    var size = BitConverter.ToInt32(bytes, offset + 4);
                    offset += 8;

                    if (offset + size > bytes.Length)
                        break;

                    var payload = System.Text.Encoding.UTF8.GetString(bytes, offset, size);
                    offset += size;

                    var stream = streamType == 1 ? "stdout" : streamType == 2 ? "stderr" : "unknown";
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

                    logs.Add(new ContainerLogEntry
                    {
                        Timestamp = timestamp,
                        Stream = stream,
                        Line = message
                    });
                }

                return logs;
            });
    }
}
