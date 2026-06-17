using Bzn.Cloudios.Domain.Enums;

namespace Bzn.Cloudios.Domain.Entities;

public sealed class ManagedAppInstance
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public Guid TemplateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int HostPort { get; set; }
    public ManagedAppStatus Status { get; set; } = ManagedAppStatus.Provisioning;
    public InstanceSize Size { get; set; } = InstanceSize.Micro1s;
    public string? DockerContainerId { get; set; }
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }
    public decimal CostPerHourBRL { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? StoppedAtUtc { get; set; }

    public Realm Realm { get; set; } = null!;
    public ManagedAppTemplate Template { get; set; } = null!;
}
