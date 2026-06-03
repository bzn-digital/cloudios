namespace Bzn.Cloudios.Domain.Dto;

public sealed class ContainerMetricsResponse
{
    public Guid ContainerId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<MetricDataPoint> DataPoints { get; set; } = [];
}

public sealed class MetricDataPoint
{
    public DateTime Timestamp { get; set; }
    public double CpuPercent { get; set; }
    public long MemoryUsedBytes { get; set; }
    public long NetworkRxBytes { get; set; }
    public long NetworkTxBytes { get; set; }
}

public sealed class HostMetricsResponse
{
    public double TotalCpuPercent { get; set; }
    public long TotalMemoryUsedBytes { get; set; }
    public long TotalMemoryTotalBytes { get; set; }
    public int ActiveContainers { get; set; }
    public long DiskUsedBytes { get; set; }
    public long DiskTotalBytes { get; set; }
}

public sealed class RealmMetricsResponse
{
    public Guid RealmId { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<MetricDataPoint> DataPoints { get; set; } = [];
}
