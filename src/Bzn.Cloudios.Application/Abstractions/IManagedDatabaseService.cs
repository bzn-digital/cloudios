using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.Application.Abstractions;

public interface IManagedDatabaseService
{
    Task<DatabaseTierListResponse> GetTiersAsync(CancellationToken ct = default);
    Task<(ManagedDatabaseResponse? Instance, string? Error, int StatusCode)> CreateAsync(CreateManagedDatabaseRequest request, CancellationToken ct = default);
}
