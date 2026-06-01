using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.Application.Abstractions;

public sealed record ContainerStats
{
    public string ContainerId { get; init; } = string.Empty;
    public string ContainerName { get; init; } = string.Empty;
    public double CpuPercent { get; init; }
    public long MemoryUsedBytes { get; init; }
    public long NetworkRxBytes { get; init; }
    public long NetworkTxBytes { get; init; }
    public long BlockReadBytes { get; init; }
    public long BlockWriteBytes { get; init; }
}

public interface IDockerNetworkService
{
    Task EnsureNetworkAsync(CancellationToken ct = default);
    Task<List<ContainerStats>> GetContainerStatsAsync(CancellationToken ct = default);
    Task<T?> SendRequestAsync<T>(string method, string path, string? body = null, CancellationToken ct = default);
    Task<List<ContainerLogEntry>> GetContainerLogsAsync(string dockerContainerId, int tail = 100, CancellationToken ct = default);
}
