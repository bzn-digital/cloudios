using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Extensions;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ContainerStatusEnum = Bzn.Cloudios.Domain.Enums.ContainerStatus;

namespace Bzn.Cloudios.Application.Services;

public sealed class ContainerService : IContainerService
{
    private readonly CloudiosDbContext _context;
    private readonly DockerClient _dockerClient;
    private readonly IDockerNetworkService _dockerNetworkService;
    private readonly ILogger<ContainerService> _logger;
    private readonly string _volumesBasePath;
    private readonly bool _skipDirectoryCreation;

    public ContainerService(
        CloudiosDbContext context,
        DockerClient dockerClient,
        IDockerNetworkService dockerNetworkService,
        IConfiguration configuration,
        ILogger<ContainerService> logger)
    {
        _context = context;
        _dockerClient = dockerClient;
        _dockerNetworkService = dockerNetworkService;
        _logger = logger;
        _volumesBasePath = configuration["Volumes:BasePath"] ?? "/var/lib/cloudios";
        _skipDirectoryCreation = configuration.GetValue<bool>("Volumes:SkipDirectoryCreation");
    }

    public async Task<ContainerActionResponse> DeployAsync(Guid containerId, CancellationToken ct = default)
    {
        var container = await _context.Containers
            .Include(c => c.Volumes)
            .Include(c => c.EnvironmentVariables)
            .FirstOrDefaultAsync(c => c.Id == containerId, ct);

        if (container is null)
            throw new InvalidOperationException($"Container {containerId} not found");

        container.Status = ContainerStatusEnum.Deploying;
        await _context.SaveChangesAsync(ct);

        try
        {
            // Use the specified network or default to realm network
            var networkName = string.IsNullOrEmpty(container.NetworkName)
                ? $"cloudios_{container.RealmId:N}"
                : container.NetworkName;

            // Ensure the network exists
            if (networkName.StartsWith($"cloudios_{container.RealmId:N}"))
            {
                await _dockerNetworkService.EnsureRealmNetworkAsync(container.RealmId, ct);
            }

            // Pull the image if it doesn't exist locally
            _logger.LogInformation("Pulling image {Image}...", container.ImageName);
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = container.ImageName },
                null,
                new Progress<JSONMessage>(msg => _logger.LogDebug("Pull progress: {Status}", msg.Status)),
                ct);
            _logger.LogInformation("Image {Image} pulled successfully", container.ImageName);

            // Check if a container with the same name already exists and remove it
            var containerName = container.Name.Replace(" ", "-").ToLowerInvariant();
            var existingContainers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }, ct);
            var existingContainer = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}"));

            if (existingContainer != null)
            {
                _logger.LogWarning("Container with name {Name} already exists (ID: {Id}), removing it", containerName, existingContainer.ID);
                await _dockerClient.Containers.RemoveContainerAsync(existingContainer.ID,
                    new ContainerRemoveParameters { Force = true, RemoveVolumes = true }, ct);
            }

            var createParams = BuildCreateContainerParameters(container);
            var createResponse = await _dockerClient.Containers.CreateContainerAsync(createParams, ct);

            var dockerId = createResponse.ID;
            container.DockerContainerId = dockerId;

            // Start the container
            await _dockerClient.Containers.StartContainerAsync(dockerId, new ContainerStartParameters(), ct);

            container.Status = ContainerStatusEnum.Running;
            container.StartedAtUtc = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Container {Name} deployed as Docker {DockerId}", container.Name, dockerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy container {Name}", container.Name);
            container.Status = ContainerStatusEnum.Failed;
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

        await _dockerClient.Containers.StartContainerAsync(container.DockerContainerId, new ContainerStartParameters(), ct);

        container.Status = ContainerStatusEnum.Running;
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

        await _dockerClient.Containers.StopContainerAsync(container.DockerContainerId, new ContainerStopParameters(), ct);

        container.Status = ContainerStatusEnum.Stopped;
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

        await _dockerClient.Containers.RestartContainerAsync(container.DockerContainerId, new ContainerRestartParameters(), ct);

        container.Status = ContainerStatusEnum.Running;
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
                await _dockerClient.Containers.RemoveContainerAsync(container.DockerContainerId,
                    new ContainerRemoveParameters { Force = true, RemoveVolumes = true }, ct);
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
            var hostPath = Path.Combine(_volumesBasePath, "volumes", $"realm-{container.RealmId}", $"container-{containerId}", vol.HostPath);
            if (!_skipDirectoryCreation)
            {
                Directory.CreateDirectory(hostPath);
            }

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
        var inspect = await _dockerClient.Containers.InspectContainerAsync(dockerContainerId, ct);

        if (inspect?.NetworkSettings?.Networks == null) return null;

        if (inspect.NetworkSettings.Networks.TryGetValue("cloudios_internal", out var network))
        {
            return network.IPAddress;
        }

        return null;
    }

    public async Task SynchronizeStateAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Starting container state synchronization...");

        // Get all managed containers from Docker
        var dockerContainers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters
            {
                All = true,
                Filters = new Dictionary<string, IDictionary<string, bool>>
                {
                    ["label"] = new Dictionary<string, bool> { ["cloudios.managed"] = true }
                }
            }, ct);

        var dockerIds = new HashSet<string>();
        foreach (var dc in dockerContainers)
        {
            dockerIds.Add(dc.ID);
            var isRunning = dc.State == "running";

            var dbContainer = await _context.Containers
                .FirstOrDefaultAsync(c => c.DockerContainerId == dc.ID, ct);

            if (dbContainer is not null)
            {
                // DB says Running but Docker says stopped
                if (dbContainer.Status == ContainerStatusEnum.Running && !isRunning)
                {
                    _logger.LogWarning("Container {Name} marked as Running in DB but {State} in Docker — fixing", dbContainer.Name, dc.State);
                    dbContainer.Status = ContainerStatusEnum.Stopped;
                    dbContainer.StartedAtUtc = null;
                }
                // DB says Stopped but Docker says running
                else if (dbContainer.Status == ContainerStatusEnum.Stopped && isRunning)
                {
                    _logger.LogWarning("Container {Name} marked as Stopped in DB but Running in Docker — fixing", dbContainer.Name);
                    dbContainer.Status = ContainerStatusEnum.Running;
                    dbContainer.StartedAtUtc = DateTime.UtcNow;
                }
            }
            else
            {
                // Orphan container in Docker — remove it
                _logger.LogWarning("Orphan Docker container {DockerId} found — removing", dc.ID);
                try
                {
                    await _dockerClient.Containers.RemoveContainerAsync(dc.ID,
                        new ContainerRemoveParameters { Force = true, RemoveVolumes = true }, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to remove orphan container {DockerId}", dc.ID);
                }
            }
        }

        // Containers in DB with Docker ID but not in Docker — mark as Stopped
        var dbContainersWithDocker = await _context.Containers
            .Where(c => c.DockerContainerId != null)
            .ToListAsync(ct);

        foreach (var dbContainer in dbContainersWithDocker)
        {
            if (!dockerIds.Contains(dbContainer.DockerContainerId!) && dbContainer.Status == ContainerStatusEnum.Running)
            {
                _logger.LogWarning("Container {Name} has Docker ID but not found in Docker — marking Stopped", dbContainer.Name);
                dbContainer.Status = ContainerStatusEnum.Stopped;
                dbContainer.StartedAtUtc = null;
            }
        }

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Container state synchronization complete");
    }

    private static CreateContainerParameters BuildCreateContainerParameters(Container container)
    {
        var portBindings = new Dictionary<string, IList<PortBinding>>();
        var exposedPorts = new Dictionary<string, EmptyStruct>();

        // Add internal port
        exposedPorts[$"{container.InternalPort}/tcp"] = new EmptyStruct();

        // Add host port binding if specified
        if (container.HostPort.HasValue)
        {
            portBindings[$"{container.InternalPort}/tcp"] = new List<PortBinding>
            {
                new PortBinding { HostPort = container.HostPort.Value.ToString() }
            };
        }
        else
        {
            // Bind to random host port if no specific port specified
            portBindings[$"{container.InternalPort}/tcp"] = new List<PortBinding> { new PortBinding() };
        }

        // Use the specified network or default to realm network
        var networkName = string.IsNullOrEmpty(container.NetworkName)
            ? $"cloudios_{container.RealmId:N}"
            : container.NetworkName;

        return new CreateContainerParameters
        {
            Name = container.Name.Replace(" ", "-").ToLowerInvariant(),
            Image = container.ImageName,
            Hostname = container.Name.Replace(" ", "-").ToLowerInvariant(),
            Labels = new Dictionary<string, string>
            {
                ["cloudios.realm"] = container.RealmId.ToString(),
                ["cloudios.container"] = container.Id.ToString(),
                ["cloudios.managed"] = "true"
            },
            HostConfig = new HostConfig
            {
                Memory = container.MemoryLimitBytes,
                PortBindings = portBindings,
                Binds = container.Volumes.Select(v =>
                    $"{v.HostPath}:{v.ContainerPath}{(v.IsReadOnly ? ":ro" : "")}").ToList(),
                RestartPolicy = new RestartPolicy
                {
                    Name = RestartPolicyKind.UnlessStopped
                }
            },
            Env = container.EnvironmentVariables.Select(e => $"{e.Key}={e.Value}").ToList(),
            ExposedPorts = exposedPorts,
            NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    [networkName] = new EndpointSettings()
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
