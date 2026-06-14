namespace Bzn.Cloudios.Application.Abstractions;

public sealed record ManagedDatabaseConnection
{
    public Guid InstanceId { get; init; }
    public string DockerContainerId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public int Port { get; init; }
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public interface IManagedDatabaseService
{
    Task<ManagedDatabaseConnection> ProvisionAsync(Guid instanceId, CancellationToken ct = default);
}
