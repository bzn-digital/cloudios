using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.Application.Abstractions;

public interface IManagedAppService
{
    Task<ManagedAppResponse> CreateAsync(Guid realmId, CreateManagedAppRequest request, CancellationToken ct = default);
    Task<ManagedAppListResponse> ListAsync(Guid realmId, string? search, int page, int pageSize, CancellationToken ct = default);
    Task<ManagedAppResponse?> GetByIdAsync(Guid realmId, Guid instanceId, CancellationToken ct = default);
    Task<ManagedAppActionResponse> StartInstanceAsync(Guid realmId, Guid instanceId, CancellationToken ct = default);
    Task<ManagedAppActionResponse> StopInstanceAsync(Guid realmId, Guid instanceId, CancellationToken ct = default);
    Task DeleteInstanceAsync(Guid realmId, Guid instanceId, CancellationToken ct = default);
}
