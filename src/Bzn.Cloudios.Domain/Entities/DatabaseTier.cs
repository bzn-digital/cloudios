namespace Bzn.Cloudios.Domain.Entities;

public sealed class DatabaseTier
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }

    public List<ManagedDatabaseInstance> Instances { get; set; } = [];
}
