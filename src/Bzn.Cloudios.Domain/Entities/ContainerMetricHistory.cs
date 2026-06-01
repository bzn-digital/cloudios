namespace Bzn.Cloudios.Domain.Entities;

public sealed class ContainerMetricHistory
{
    public long Id { get; set; }
    public Guid ContainerId { get; set; }
    public DateTime Timestamp { get; set; }
    public double CpuPercent { get; set; }
    public long MemoryUsedBytes { get; set; }
    public long NetworkRxBytes { get; set; }
    public long NetworkTxBytes { get; set; }
    public long BlockReadBytes { get; set; }
    public long BlockWriteBytes { get; set; }
}
