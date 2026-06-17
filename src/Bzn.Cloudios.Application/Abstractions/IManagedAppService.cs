using Bzn.Cloudios.Domain.Entities;

namespace Bzn.Cloudios.Application.Abstractions;

public interface IManagedAppService
{
    Task<ManagedAppInstance> ProvisionAsync(Guid templateId, string name, CancellationToken ct = default);
}
