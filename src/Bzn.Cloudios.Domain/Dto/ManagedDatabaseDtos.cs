namespace Bzn.Cloudios.Domain.Dto;

public sealed class DatabaseTierListResponse
{
    public List<DatabaseTierItem> Tiers { get; set; } = [];
}

public sealed class DatabaseTierItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }
    public List<DatabaseTierPricing> Pricing { get; set; } = [];
}

public sealed class DatabaseTierPricing
{
    public string Engine { get; set; } = string.Empty;
    public decimal HourlyRateBRL { get; set; }
    public decimal MonthlyForecastBRL { get; set; }
}

public sealed class CreateManagedDatabaseRequest
{
    public string Name { get; set; } = string.Empty;
    public Guid TierId { get; set; }
    public string Type { get; set; } = string.Empty;
}

public sealed class ManagedDatabaseResponse
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public Guid TierId { get; set; }
    public string TierName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }
    public decimal HourlyRateBRL { get; set; }
    public decimal MonthlyForecastBRL { get; set; }
    public DateTime CreatedAt { get; set; }
}
