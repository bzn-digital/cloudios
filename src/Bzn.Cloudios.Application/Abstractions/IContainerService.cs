using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.Application.Abstractions;

public interface IContainerService
{
    Task<ContainerActionResponse> DeployAsync(Guid containerId, CancellationToken ct = default);
    Task<ContainerActionResponse> StartAsync(Guid containerId, CancellationToken ct = default);
    Task<ContainerActionResponse> StopAsync(Guid containerId, CancellationToken ct = default);
    Task<ContainerActionResponse> RestartAsync(Guid containerId, CancellationToken ct = default);
    Task DeleteAsync(Guid containerId, bool removeVolumes = true, CancellationToken ct = default);
    Task<string?> GetContainerIpAsync(string dockerContainerId, CancellationToken ct = default);
    Task SynchronizeStateAsync(CancellationToken ct = default);
    Task UpdateEnvVarsAsync(Guid containerId, Dictionary<string, string> envVars, CancellationToken ct = default);
    Task UpdateVolumesAsync(Guid containerId, List<ContainerVolumeRequest> volumes, CancellationToken ct = default);
}
