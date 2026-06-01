using Bzn.Cloudios.Domain.Enums;

namespace Bzn.Cloudios.Domain.Entities;

public sealed class Container
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DockerContainerId { get; set; }
    public string ImageName { get; set; } = string.Empty;
    public int InternalPort { get; set; } = 8080;
    public ContainerStatus Status { get; set; } = ContainerStatus.Stopped;
    public double CpuLimitCores { get; set; } = 0.5;
    public long MemoryLimitBytes { get; set; } = 536870912;
    public decimal CostPerHourBRL { get; set; } = 0.02m;
    public DateTime? StartedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }

    public Realm Realm { get; set; } = null!;
    public List<ContainerVolume> Volumes { get; set; } = [];
    public List<ContainerEnvVar> EnvironmentVariables { get; set; } = [];
}
