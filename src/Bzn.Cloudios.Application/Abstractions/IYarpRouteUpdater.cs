namespace Bzn.Cloudios.Application.Abstractions;

public interface IYarpRouteUpdater
{
    Task AddRouteAsync(Guid containerId, string internalIp, int internalPort, string hostname, CancellationToken ct = default);
    Task RemoveRouteAsync(Guid containerId, CancellationToken ct = default);
    string BuildHostname(string containerName, string realmSlug);
}
