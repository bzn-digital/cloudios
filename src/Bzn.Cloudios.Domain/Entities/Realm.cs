namespace Bzn.Cloudios.Domain.Entities;

public sealed class Realm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public List<User> Users { get; set; } = [];
    public List<Container> Containers { get; set; } = [];
    public List<ManagedDatabaseInstance> ManagedDatabases { get; set; } = [];

    public int? MaxContainers { get; set; }
    public int? MaxDatabases { get; set; }
    public int? MaxManagedApps { get; set; }
    public long? MaxRamBytes { get; set; }
    public double? MaxCpuCores { get; set; }
}
