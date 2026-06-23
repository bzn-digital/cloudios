using Bzn.Cloudios.Domain.Enums;

namespace Bzn.Cloudios.Domain.Dto;

public sealed class CreateManagedAppRequest
{
    public required string Name { get; init; }
    public required Guid TemplateId { get; init; }
    public InstanceSize Size { get; init; } = InstanceSize.Micro1s;
}

public sealed class ManagedAppResponse
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int HostPort { get; set; }
    public int InternalPort { get; set; }
    public string InternalAccess { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string? DockerContainerId { get; set; }
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }
    public decimal CostPerHourBRL { get; set; }
    public decimal CurrentMonthCostBRL { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? StoppedAtUtc { get; set; }
}

public sealed class ManagedAppListResponse
{
    public List<ManagedAppResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => Page * PageSize < TotalCount;
}

public sealed class ManagedAppActionResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DockerContainerId { get; set; }
    public DateTime? StartedAtUtc { get; set; }
}

public sealed class ManagedAppTemplateResponse
{
    public Guid Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int InternalPort { get; set; }
    public string DefaultInstanceSize { get; set; } = string.Empty;
}

public sealed class ManagedAppTemplateListResponse
{
    public List<ManagedAppTemplateResponse> Items { get; set; } = [];
}

public sealed class AdminManagedAppResponse
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public string RealmName { get; set; } = string.Empty;
    public Guid TemplateId { get; set; }
    public string TemplateName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int HostPort { get; set; }
    public int InternalPort { get; set; }
    public string InternalAccess { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string? DockerContainerId { get; set; }
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }
    public decimal CostPerHourBRL { get; set; }
    public decimal CurrentMonthCostBRL { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime? StoppedAtUtc { get; set; }
}

public sealed class AdminManagedAppListResponse
{
    public List<AdminManagedAppResponse> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage => Page * PageSize < TotalCount;
}
