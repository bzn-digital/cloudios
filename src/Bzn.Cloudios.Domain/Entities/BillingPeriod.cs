namespace Bzn.Cloudios.Domain.Entities;

public sealed class BillingPeriod
{
    public long Id { get; set; }
    public Guid? ContainerId { get; set; }
    public Guid? ManagedDatabaseId { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? StoppedAtUtc { get; set; }
    public double Hours { get; set; }
    public decimal CostBRL { get; set; }
}
