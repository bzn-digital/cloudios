namespace Bzn.Cloudios.Domain.Dto;

public sealed class ContainerListResponse
{
    public List<ContainerListItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}

public sealed class ContainerListItem
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public int InternalPort { get; set; }
    public string Status { get; set; } = string.Empty;
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }
    public decimal CostPerHourBRL { get; set; }
    public decimal CurrentMonthCostBRL { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class ContainerDetailResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public int InternalPort { get; set; }
    public string Status { get; set; } = string.Empty;
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }
    public decimal CostPerHourBRL { get; set; }
    public decimal CurrentMonthCostBRL { get; set; }
    public string? DockerContainerId { get; set; }
    public DateTime? StartedAtUtc { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<ContainerVolumeDto> Volumes { get; set; } = [];
    public List<object> EnvironmentVariables { get; set; } = [];
}

public sealed class CreateContainerRequest
{
    public string Name { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public int InternalPort { get; set; }
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }
    public decimal CostPerHourBRL { get; set; }
    public List<ContainerVolumeDto> Volumes { get; set; } = [];
    public Dictionary<string, string> EnvironmentVariables { get; set; } = [];
}

public sealed class ContainerActionResponse
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? DockerContainerId { get; set; }
    public DateTime? StartedAtUtc { get; set; }
}

public sealed class ContainerVolumeDto
{
    public Guid Id { get; set; }
    public string HostPath { get; set; } = string.Empty;
    public string ContainerPath { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
}

public sealed class ContainerVolumeRequest
{
    public string HostPath { get; set; } = string.Empty;
    public string ContainerPath { get; set; } = string.Empty;
    public bool IsReadOnly { get; set; }
}

public sealed class ContainerEnvVarDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public sealed class ContainerEnvVarSecureDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = "***"; // Hidden for RealmViewer
}

public sealed class ContainerLogsResponse
{
    public Guid ContainerId { get; set; }
    public List<ContainerLogEntry> Logs { get; set; } = [];
}

public sealed class ContainerLogEntry
{
    public DateTime Timestamp { get; set; }
    public string Stream { get; set; } = string.Empty;
    public string Line { get; set; } = string.Empty;
}

public sealed class AdminContainerListResponse
{
    public List<AdminContainerListItem> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool HasNextPage { get; set; }
}

public sealed class AdminContainerListItem
{
    public Guid Id { get; set; }
    public Guid RealmId { get; set; }
    public string RealmName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ImageName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public double CpuLimitCores { get; set; }
    public long MemoryLimitBytes { get; set; }
    public decimal CostPerHourBRL { get; set; }
    public decimal CurrentMonthCostBRL { get; set; }
}
