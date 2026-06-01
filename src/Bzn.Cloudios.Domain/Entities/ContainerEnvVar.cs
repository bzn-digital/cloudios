namespace Bzn.Cloudios.Domain.Entities;

public sealed class ContainerEnvVar
{
    public Guid Id { get; set; }
    public Guid ContainerId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public Container Container { get; set; } = null!;
}
