using Bzn.Cloudios.Domain.Enums;

namespace Bzn.Cloudios.Domain.Entities;

public sealed class ManagedAppTemplate
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DockerImage { get; set; } = string.Empty;
    public Dictionary<string, string> DefaultEnvVars { get; set; } = new();
    public InstanceSize DefaultInstanceSize { get; set; } = InstanceSize.Micro1s;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
