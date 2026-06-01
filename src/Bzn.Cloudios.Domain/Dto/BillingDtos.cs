namespace Bzn.Cloudios.Domain.Dto;

public sealed class RealmBillingResponse
{
    public Guid RealmId { get; set; }
    public string RealmName { get; set; } = string.Empty;
    public string Month { get; set; } = string.Empty;
    public decimal TotalCostBRL { get; set; }
    public List<BillingServiceItem> Services { get; set; } = [];
}

public sealed class BillingServiceItem
{
    public Guid ContainerId { get; set; }
    public string ContainerName { get; set; } = string.Empty;
    public decimal CostPerHourBRL { get; set; }
    public double RunningHours { get; set; }
    public decimal TotalCostBRL { get; set; }
}

public sealed class GlobalBillingResponse
{
    public string Month { get; set; } = string.Empty;
    public decimal TotalRevenueBRL { get; set; }
    public List<RealmBillingItem> Realms { get; set; } = [];
}

public sealed class RealmBillingItem
{
    public Guid RealmId { get; set; }
    public string RealmName { get; set; } = string.Empty;
    public decimal TotalCostBRL { get; set; }
    public int ContainerCount { get; set; }
    public int ActiveContainerCount { get; set; }
}
