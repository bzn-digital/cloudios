using Bzn.Cloudios.Domain.Enums;

namespace Bzn.Cloudios.Domain.Entities;

public sealed class ManagedAppTemplate
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string DockerImage { get; set; } = string.Empty;
    public int InternalPort { get; set; } = 80;
    public Dictionary<string, string> DefaultEnvVars { get; set; } = new();
    public InstanceSize DefaultInstanceSize { get; set; } = InstanceSize.Micro1s;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
