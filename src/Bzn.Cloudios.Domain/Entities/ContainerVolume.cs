namespace Bzn.Cloudios.Domain.Entities;

public sealed class ContainerVolume
{
    public Guid Id { get; set; }
    public Guid ContainerId { get; set; }
    public string HostPath { get; set; } = string.Empty;
    public string ContainerPath { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }

    public Container Container { get; set; } = null!;
}
