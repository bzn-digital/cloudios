using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Workers;

public sealed class ManagedAppDeployWorker : BackgroundService
{
    private const long NanoCpusPerCore = 1_000_000_000L;
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(30);

    private readonly IManagedAppDeployQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DockerClient _dockerClient;
    private readonly IDockerNetworkService _dockerNetworkService;
    private readonly ILogger<ManagedAppDeployWorker> _logger;
    private readonly string _volumesBasePath;
    private readonly bool _skipDirectoryCreation;

    public ManagedAppDeployWorker(
        IManagedAppDeployQueue queue,
        IServiceScopeFactory scopeFactory,
        DockerClient dockerClient,
        IDockerNetworkService dockerNetworkService,
        IConfiguration configuration,
        ILogger<ManagedAppDeployWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _dockerClient = dockerClient;
        _dockerNetworkService = dockerNetworkService;
        _logger = logger;
        _volumesBasePath = configuration["Volumes:BasePath"] ?? "/var/lib/cloudios";
        _skipDirectoryCreation = configuration.GetValue<bool>("Volumes:SkipDirectoryCreation");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ManagedAppDeployWorker started");

        await foreach (var instanceId in _queue.DequeueAllAsync(stoppingToken))
        {
            try
            {
                await DeployInstanceAsync(instanceId, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error deploying managed app {InstanceId}", instanceId);
            }
        }

        _logger.LogInformation("ManagedAppDeployWorker stopped");
    }

    internal async Task DeployInstanceAsync(Guid instanceId, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CloudiosDbContext>();

        var instance = await db.ManagedAppInstances
            .Include(i => i.Template)
            .FirstOrDefaultAsync(i => i.Id == instanceId, ct);

        if (instance is null)
        {
            _logger.LogWarning("Instance {InstanceId} not found, skipping deploy", instanceId);
            return;
        }

        if (instance.Status != ManagedAppStatus.Imaging)
        {
            _logger.LogWarning("Instance {InstanceId} is in status {Status}, expected Imaging — skipping", instanceId, instance.Status);
            return;
        }

        var template = instance.Template;

        try
        {
            // --- Phase 1: Pull image (status stays Imaging) ---
            _logger.LogInformation("Pulling image {Image} for managed app {InstanceId}", template.DockerImage, instanceId);
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = template.DockerImage },
                null,
                new Progress<JSONMessage>(msg => _logger.LogDebug("Pull progress: {Status}", msg.Status)),
                ct);
            _logger.LogInformation("Image {Image} pulled successfully for {InstanceId}", template.DockerImage, instanceId);

            // --- Phase 2: Create container → transition to Initializing ---
            var volumePath = Path.Combine(
                _volumesBasePath,
                "managed-apps",
                instance.RealmId.ToString("N"),
                instance.Id.ToString("N"));

            if (!_skipDirectoryCreation)
            {
                Directory.CreateDirectory(volumePath);
            }

            var containerName = $"{instance.Name}-{instance.RealmId.ToString("N")[..8]}";

            // Remove pre-existing container with the same name
            var existingContainers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }, ct);
            var existing = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}"));
            if (existing is not null)
            {
                _logger.LogWarning("Container {Name} already exists (ID: {Id}), removing it", containerName, existing.ID);
                await _dockerClient.Containers.RemoveContainerAsync(existing.ID,
                    new ContainerRemoveParameters { Force = true, RemoveVolumes = true }, ct);
            }

            var networkName = $"cloudios_{instance.RealmId:N}";
            await _dockerNetworkService.EnsureRealmNetworkAsync(instance.RealmId, ct);

            var createParams = new CreateContainerParameters
            {
                Name = containerName,
                Image = template.DockerImage,
                Hostname = containerName,
                Labels = new Dictionary<string, string>
                {
                    ["cloudios.realm"] = instance.RealmId.ToString(),
                    ["cloudios.managed-app"] = instance.Id.ToString(),
                    ["cloudios.managed"] = "true"
                },
                HostConfig = new HostConfig
                {
                    Memory = instance.MemoryLimitBytes,
                    NanoCPUs = (long)(instance.CpuLimitCores * NanoCpusPerCore),
                    PortBindings = new Dictionary<string, IList<PortBinding>>
                    {
                        [$"{template.InternalPort}/tcp"] = new List<PortBinding>
                        {
                            new PortBinding { HostPort = instance.HostPort.ToString() }
                        }
                    },
                    Binds = new List<string> { $"{volumePath}:/app/data" },
                    RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
                },
                Env = template.DefaultEnvVars.Select(kvp => $"{kvp.Key}={kvp.Value}").ToList(),
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    [$"{template.InternalPort}/tcp"] = new EmptyStruct()
                },
                NetworkingConfig = new NetworkingConfig
                {
                    EndpointsConfig = new Dictionary<string, EndpointSettings>
                    {
                        [networkName] = new EndpointSettings()
                    }
                }
            };

            var createResponse = await _dockerClient.Containers.CreateContainerAsync(createParams, ct);
            instance.DockerContainerId = createResponse.ID;
            instance.Status = ManagedAppStatus.Initializing;
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Container created for {InstanceId}, status → Initializing", instanceId);

            // --- Phase 3: Start container ---
            await _dockerClient.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters(), ct);

            // --- Phase 4: Health check — wait for State.Running ---
            var deadline = DateTime.UtcNow + HealthCheckTimeout;
            var isRunning = false;

            while (DateTime.UtcNow < deadline)
            {
                var inspect = await _dockerClient.Containers.InspectContainerAsync(createResponse.ID, ct);
                if (inspect.State.Running)
                {
                    isRunning = true;
                    break;
                }
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }

            if (isRunning)
            {
                instance.Status = ManagedAppStatus.Running;
                instance.StartedAtUtc = DateTime.UtcNow;
                await db.SaveChangesAsync(ct);
                _logger.LogInformation("Managed app {InstanceId} is Running", instanceId);
            }
            else
            {
                instance.Status = ManagedAppStatus.Failed;
                await db.SaveChangesAsync(ct);
                _logger.LogWarning("Managed app {InstanceId} failed to start within {Timeout}s", instanceId, HealthCheckTimeout.TotalSeconds);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deploy failed for managed app {InstanceId}", instanceId);
            instance.Status = ManagedAppStatus.Failed;
            try
            {
                await db.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to persist Failed status for {InstanceId}", instanceId);
            }
        }
    }
}
