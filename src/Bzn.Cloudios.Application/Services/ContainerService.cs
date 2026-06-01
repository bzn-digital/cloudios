using System.Text.Json;
using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Extensions;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class ContainerService : IContainerService
{
    private readonly CloudiosDbContext _context;
    private readonly IDockerNetworkService _docker;
    private readonly ILogger<ContainerService> _logger;
    private readonly string _socketPath;

    public ContainerService(
        CloudiosDbContext context,
        IDockerNetworkService docker,
        IConfiguration configuration,
        ILogger<ContainerService> logger)
    {
        _context = context;
        _docker = docker;
        _logger = logger;
        _socketPath = configuration["Docker:SocketPath"] ?? "/var/run/docker.sock";
    }

    public async Task<ContainerActionResponse> DeployAsync(Guid containerId, CancellationToken ct = default)
    {
        var container = await _context.Containers
            .Include(c => c.Volumes)
            .Include(c => c.EnvironmentVariables)
            .FirstOrDefaultAsync(c => c.Id == containerId, ct);

        if (container is null)
            throw new InvalidOperationException($"Container {containerId} not found");

        container.Status = ContainerStatus.Deploying;
        await _context.SaveChangesAsync(ct);

        try
        {
            var createBody = BuildCreateContainerBody(container);
            var createJson = JsonSerializer.Serialize(createBody);

            var createResponse = await _docker.SendRequestAsync<JsonElement>(
                "POST", "/containers/create", createJson, ct);

            var dockerId = createResponse.GetProperty("Id").GetString()!;

            container.DockerContainerId = dockerId;

            // Start the container
            await _docker.SendRequestAsync<JsonElement>(
                "POST", $"/containers/{dockerId}/start", ct: ct);

            // Connect to cloudios_internal network
            await _docker.SendRequestAsync<JsonElement>(
                "POST", $"/networks/cloudios_internal/connect",
                JsonSerializer.Serialize(new { Container = dockerId }), ct);

            container.Status = ContainerStatus.Running;
            container.StartedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Container {Name} deployed as Docker {DockerId}", container.Name, dockerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy container {Name}", container.Name);
            container.Status = ContainerStatus.Failed;
            await _context.SaveChangesAsync(ct);
            throw;
        }

        return MapToActionResponse(container);
    }

    public async Task<ContainerActionResponse> StartAsync(Guid containerId, CancellationToken ct = default)
    {
        var container = await _context.Containers.FindAsync([containerId], ct);
        if (container is null) throw new InvalidOperationException($"Container {containerId} not found");
        if (container.DockerContainerId is null) throw new InvalidOperationException("Container not deployed");

        await _docker.SendRequestAsync<JsonElement>(
            "POST", $"/containers/{container.DockerContainerId}/start", ct: ct);

        container.Status = ContainerStatus.Running;
        container.StartedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Container {Name} started", container.Name);
        return MapToActionResponse(container);
    }

    public async Task<ContainerActionResponse> StopAsync(Guid containerId, CancellationToken ct = default)
    {
        var container = await _context.Containers.FindAsync([containerId], ct);
        if (container is null) throw new InvalidOperationException($"Container {containerId} not found");
        if (container.DockerContainerId is null) throw new InvalidOperationException("Container not deployed");

        await _docker.SendRequestAsync<JsonElement>(
            "POST", $"/containers/{container.DockerContainerId}/stop", ct: ct);

        container.Status = ContainerStatus.Stopped;
        container.StartedAtUtc = null;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Container {Name} stopped", container.Name);
        return MapToActionResponse(container);
    }

    public async Task<ContainerActionResponse> RestartAsync(Guid containerId, CancellationToken ct = default)
    {
        var container = await _context.Containers.FindAsync([containerId], ct);
        if (container is null) throw new InvalidOperationException($"Container {containerId} not found");
        if (container.DockerContainerId is null) throw new InvalidOperationException("Container not deployed");

        await _docker.SendRequestAsync<JsonElement>(
            "POST", $"/containers/{container.DockerContainerId}/restart", ct: ct);

        container.Status = ContainerStatus.Running;
        container.StartedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Container {Name} restarted", container.Name);
        return MapToActionResponse(container);
    }

    public async Task DeleteAsync(Guid containerId, bool removeVolumes = true, CancellationToken ct = default)
    {
        var container = await _context.Containers
            .Include(c => c.Volumes)
            .Include(c => c.EnvironmentVariables)
            .FirstOrDefaultAsync(c => c.Id == containerId, ct);
        if (container is null) throw new InvalidOperationException($"Container {containerId} not found");

        if (container.DockerContainerId is not null)
        {
            try
            {
                // Force stop + remove with volumes
                await _docker.SendRequestAsync<JsonElement>(
                    "DELETE", $"/containers/{container.DockerContainerId}?force=true&v=true", ct: ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove Docker container {DockerId}", container.DockerContainerId);
            }
        }

        // Remove volume directories from host if requested
        if (removeVolumes)
        {
            foreach (var volume in container.Volumes)
            {
                try
                {
                    if (Directory.Exists(volume.HostPath))
                    {
                        Directory.Delete(volume.HostPath, recursive: true);
                        _logger.LogInformation("Removed volume directory {Path}", volume.HostPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to remove volume directory {Path}", volume.HostPath);
                }
            }
        }

        _context.Containers.Remove(container);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Container {Name} deleted", container.Name);
    }

    public async Task UpdateEnvVarsAsync(Guid containerId, Dictionary<string, string> envVars, CancellationToken ct = default)
    {
        var container = await _context.Containers
            .Include(c => c.EnvironmentVariables)
            .FirstOrDefaultAsync(c => c.Id == containerId, ct);
        if (container is null) throw new InvalidOperationException($"Container {containerId} not found");

        // Remove existing env vars
        _context.ContainerEnvVars.RemoveRange(container.EnvironmentVariables);

        // Add new env vars
        foreach (var (key, value) in envVars)
        {
            container.EnvironmentVariables.Add(new ContainerEnvVar
            {
                ContainerId = containerId,
                Key = key,
                Value = value
            });
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated environment variables for container {Name}", container.Name);
    }

    public async Task UpdateVolumesAsync(Guid containerId, List<ContainerVolumeRequest> volumes, CancellationToken ct = default)
    {
        var container = await _context.Containers
            .Include(c => c.Volumes)
            .FirstOrDefaultAsync(c => c.Id == containerId, ct);
        if (container is null) throw new InvalidOperationException($"Container {containerId} not found");

        // Remove existing volumes
        _context.ContainerVolumes.RemoveRange(container.Volumes);

        // Add new volumes
        foreach (var vol in volumes)
        {
            var hostPath = $"/var/lib/cloudios/volumes/realm-{container.RealmId}/container-{containerId}/{vol.HostPath}";
            Directory.CreateDirectory(hostPath);

            container.Volumes.Add(new ContainerVolume
            {
                ContainerId = containerId,
                HostPath = hostPath,
                ContainerPath = vol.ContainerPath,
                IsReadOnly = vol.IsReadOnly
            });
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Updated volumes for container {Name}", container.Name);
    }

    public async Task<string?> GetContainerIpAsync(string dockerContainerId, CancellationToken ct = default)
    {
        var inspect = await _docker.SendRequestAsync<JsonElement>(
            "GET", $"/containers/{dockerContainerId}/json", ct: ct);

        if (inspect.ValueKind == JsonValueKind.Undefined) return null;

        var networks = inspect
            .GetProperty("NetworkSettings")
            .GetProperty("Networks");

        if (networks.TryGetProperty("cloudios_internal", out var net))
        {
            return net.GetProperty("IPAddress").GetString();
        }

        return null;
    }

    public async Task SynchronizeStateAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting container state synchronization...");

        // Get all managed containers from Docker
        var dockerContainers = await _docker.SendRequestAsync<List<JsonElement>>(
            "GET", "/containers/json?all=true&filters={\"label\":[\"cloudios.managed=true\"]}", ct: ct);

        var dockerIds = new HashSet<string>();
        if (dockerContainers is not null)
        {
            foreach (var dc in dockerContainers)
            {
                var dockerId = dc.GetProperty("Id").GetString()!;
                dockerIds.Add(dockerId);

                var state = dc.GetProperty("State").GetString()!;
                var isRunning = state == "running";

                var dbContainer = await _context.Containers
                    .FirstOrDefaultAsync(c => c.DockerContainerId == dockerId, ct);

                if (dbContainer is not null)
                {
                    // DB says Running but Docker says stopped
                    if (dbContainer.Status == ContainerStatus.Running && !isRunning)
                    {
                        _logger.LogWarning("Container {Name} marked as Running in DB but {State} in Docker — fixing", dbContainer.Name, state);
                        dbContainer.Status = ContainerStatus.Stopped;
                        dbContainer.StartedAtUtc = null;
                    }
                    // DB says Stopped but Docker says running
                    else if (dbContainer.Status == ContainerStatus.Stopped && isRunning)
                    {
                        _logger.LogWarning("Container {Name} marked as Stopped in DB but Running in Docker — fixing", dbContainer.Name);
                        dbContainer.Status = ContainerStatus.Running;
                        dbContainer.StartedAtUtc = DateTime.UtcNow;
                    }
                }
                else
                {
                    // Orphan container in Docker — remove it
                    _logger.LogWarning("Orphan Docker container {DockerId} found — removing", dockerId);
                    try
                    {
                        await _docker.SendRequestAsync<JsonElement>(
                            "DELETE", $"/containers/{dockerId}?force=true&v=true", ct: ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to remove orphan container {DockerId}", dockerId);
                    }
                }
            }
        }

        // Containers in DB with Docker ID but not in Docker — mark as Stopped
        var dbContainersWithDocker = await _context.Containers
            .Where(c => c.DockerContainerId != null)
            .ToListAsync(ct);

        foreach (var dbContainer in dbContainersWithDocker)
        {
            if (!dockerIds.Contains(dbContainer.DockerContainerId!) && dbContainer.Status == ContainerStatus.Running)
            {
                _logger.LogWarning("Container {Name} has Docker ID but not found in Docker — marking Stopped", dbContainer.Name);
                dbContainer.Status = ContainerStatus.Stopped;
                dbContainer.StartedAtUtc = null;
            }
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Container state synchronization complete");
    }

    private static object BuildCreateContainerBody(Container container)
    {
        var cpuQuota = (long)(container.CpuLimitCores * 100000);

        return new
        {
            Image = container.ImageName,
            Hostname = container.Name.Replace(" ", "-").ToLowerInvariant(),
            Labels = new Dictionary<string, string>
            {
                ["cloudios.realm"] = container.RealmId.ToString(),
                ["cloudios.container"] = container.Id.ToString(),
                ["cloudios.managed"] = "true"
            },
            HostConfig = new
            {
                CpuQuota = cpuQuota,
                Memory = container.MemoryLimitBytes,
                PortBindings = new Dictionary<string, object>
                {
                    [$"{container.InternalPort}/tcp"] = Array.Empty<object>()
                },
                Binds = container.Volumes.Select(v =>
                    $"{v.HostPath}:{v.ContainerPath}{(v.IsReadOnly ? ":ro" : "")}").ToList(),
                RestartPolicy = new { Name = "unless-stopped" }
            },
            Env = container.EnvironmentVariables.Select(e => $"{e.Key}={e.Value}").ToList(),
            ExposedPorts = new Dictionary<string, object>
            {
                [$"{container.InternalPort}/tcp"] = new { }
            },
            NetworkingConfig = new
            {
                EndpointsConfig = new Dictionary<string, object>
                {
                    ["cloudios_internal"] = new { }
                }
            }
        };
    }

    private static ContainerActionResponse MapToActionResponse(Container container)
    {
        return new ContainerActionResponse
        {
            Id = container.Id,
            Status = container.Status.ToString(),
            DockerContainerId = container.DockerContainerId,
            StartedAtUtc = container.StartedAtUtc
        };
    }
}
