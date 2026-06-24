namespace Bzn.Cloudios.Domain.Dto;

public sealed class RealmListResponse
{
    public List<RealmItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}

public sealed class RealmItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int UserCount { get; set; }
    public int ContainerCount { get; set; }
}

public sealed class RealmDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<RealmUserItem> Users { get; set; } = [];
}

public sealed class RealmUserItem
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
}

public sealed class CreateRealmRequest
{
    public string Name { get; set; } = string.Empty;
}

public sealed class UpdateRealmRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class SuspendRealmResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int ContainersStopped { get; set; }
    public int BillingPeriodsClosed { get; set; }
}

public sealed class ReactivateRealmResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class UpdateQuotasRequest
{
    public int? MaxContainers { get; init; }
    public int? MaxDatabases { get; init; }
    public int? MaxManagedApps { get; init; }
    public long? MaxRamBytes { get; init; }
    public double? MaxCpuCores { get; init; }
}

public sealed class RealmQuotas
{
    public int? MaxContainers { get; set; }
    public int? MaxDatabases { get; set; }
    public int? MaxManagedApps { get; set; }
    public long? MaxRamBytes { get; set; }
    public double? MaxCpuCores { get; set; }
}

public sealed class RealmUsage
{
    public int ContainersCount { get; set; }
    public int DatabasesCount { get; set; }
    public int ManagedAppsCount { get; set; }
    public long RamBytesUsed { get; set; }
    public double CpuCoresUsed { get; set; }
}

public sealed class RealmStatsResponse
{
    public int UsersCount { get; set; }
    public int ContainersCount { get; set; }
    public int DatabasesCount { get; set; }
    public int ManagedAppsCount { get; set; }
    public decimal MonthlyCostBRL { get; set; }
    public RealmQuotas Quotas { get; set; } = new();
    public RealmUsage Usage { get; set; } = new();
}
