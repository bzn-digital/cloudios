using Bzn.Cloudios.Domain.Enums;

namespace Bzn.Cloudios.Domain.Entities;

public sealed class ManagedDatabaseInstance
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public Guid TierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public ManagedDatabaseType Type { get; set; }
    public string NetworkId { get; set; } = string.Empty;
    public double CpuLimit { get; set; }
    public long MemoryLimit { get; set; }
    public ManagedDatabaseStatus Status { get; set; } = ManagedDatabaseStatus.Provisioning;
    public DateTime CreatedAt { get; set; }

    public Realm Realm { get; set; } = null!;
    public DatabaseTier Tier { get; set; } = null!;
}
