namespace Bzn.Cloudios.Application.Abstractions;

public interface IBillingService
{
    Task RegisterStartAsync(Guid containerId, DateTime startedAtUtc, CancellationToken ct = default);
    Task RegisterStopAsync(Guid containerId, DateTime stoppedAtUtc, CancellationToken ct = default);
    Task<decimal> GetRealmBillingAsync(Guid realmId, int year, int month, CancellationToken ct = default);
    Task<decimal> GetGlobalBillingAsync(int year, int month, CancellationToken ct = default);
    Task<decimal> GetContainerMonthCostAsync(Guid containerId, int year, int month, CancellationToken ct = default);
}
