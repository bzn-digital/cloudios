using System.Security.Cryptography;
using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class ManagedDatabaseService : IManagedDatabaseService
{
    private const long NanoCpusPerCore = 1_000_000_000L;

    private readonly CloudiosDbContext _context;
    private readonly DockerClient _dockerClient;
    private readonly IDockerNetworkService _dockerNetworkService;
    private readonly ILogger<ManagedDatabaseService> _logger;
    private readonly string _volumesBasePath;
    private readonly bool _skipDirectoryCreation;

    public ManagedDatabaseService(
        CloudiosDbContext context,
        DockerClient dockerClient,
        IDockerNetworkService dockerNetworkService,
        IConfiguration configuration,
        ILogger<ManagedDatabaseService> logger)
    {
        _context = context;
        _dockerClient = dockerClient;
        _dockerNetworkService = dockerNetworkService;
        _logger = logger;
        _volumesBasePath = configuration["Volumes:BasePath"] ?? "/var/lib/cloudios";
        _skipDirectoryCreation = configuration.GetValue<bool>("Volumes:SkipDirectoryCreation");
    }

    public async Task<ManagedDatabaseConnection> ProvisionAsync(Guid instanceId, CancellationToken ct = default)
    {
        var instance = await _context.ManagedDatabaseInstances
            .Include(d => d.Tier)
            .FirstOrDefaultAsync(d => d.Id == instanceId, ct);

        if (instance is null)
            throw new InvalidOperationException($"Managed database instance {instanceId} not found");

        instance.Status = ManagedDatabaseStatus.Provisioning;
        await _context.SaveChangesAsync(ct);

        try
        {
            var image = GetImage(instance.Type);

            // Resolve the network the container should join.
            var networkName = string.IsNullOrEmpty(instance.NetworkId)
                ? $"cloudios_{instance.RealmId:N}"
                : instance.NetworkId;

            if (networkName.StartsWith($"cloudios_{instance.RealmId:N}"))
            {
                await _dockerNetworkService.EnsureRealmNetworkAsync(instance.RealmId, ct);
            }

            // Pull the official image.
            _logger.LogInformation("Pulling image {Image} for managed database {Name}...", image, instance.Name);
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = image },
                null,
                new Progress<JSONMessage>(msg => _logger.LogDebug("Pull progress: {Status}", msg.Status)),
                ct);
            _logger.LogInformation("Image {Image} pulled successfully", image);

            // Ensure the persistent volume directory exists on the host.
            var hostPath = Path.Combine(_volumesBasePath, "databases", instance.Id.ToString("N"));
            if (!_skipDirectoryCreation)
            {
                Directory.CreateDirectory(hostPath);
            }

            // Remove any pre-existing container with the same name.
            var containerName = BuildContainerName(instance);
            var existingContainers = await _dockerClient.Containers.ListContainersAsync(
                new ContainersListParameters { All = true }, ct);
            var existingContainer = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}"));
            if (existingContainer is not null)
            {
                _logger.LogWarning("Database container {Name} already exists (ID: {Id}), removing it", containerName, existingContainer.ID);
                await _dockerClient.Containers.RemoveContainerAsync(existingContainer.ID,
                    new ContainerRemoveParameters { Force = true, RemoveVolumes = false }, ct);
            }

            var password = GeneratePassword();
            var createParams = BuildCreateContainerParameters(instance, instance.Tier, image, networkName, hostPath, password);

            var createResponse = await _dockerClient.Containers.CreateContainerAsync(createParams, ct);
            var dockerId = createResponse.ID;

            await _dockerClient.Containers.StartContainerAsync(dockerId, new ContainerStartParameters(), ct);

            instance.DockerContainerId = dockerId;
            instance.Status = ManagedDatabaseStatus.Running;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Managed database {Name} provisioned as Docker {DockerId}", instance.Name, dockerId);

            return new ManagedDatabaseConnection
            {
                InstanceId = instance.Id,
                DockerContainerId = dockerId,
                Status = instance.Status.ToString(),
                Type = instance.Type.ToString(),
                Host = containerName,
                Port = GetPort(instance.Type),
                Username = GetRootUsername(instance.Type),
                Password = password
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to provision managed database {Name}", instance.Name);
            instance.Status = ManagedDatabaseStatus.Failed;
            await _context.SaveChangesAsync(ct);
            throw;
        }
    }

    internal static CreateContainerParameters BuildCreateContainerParameters(
        ManagedDatabaseInstance instance,
        DatabaseTier tier,
        string image,
        string networkName,
        string hostPath,
        string password)
    {
        var containerName = BuildContainerName(instance);
        var dataPath = GetDataPath(instance.Type);

        return new CreateContainerParameters
        {
            Name = containerName,
            Image = image,
            Hostname = containerName,
            Labels = new Dictionary<string, string>
            {
                ["cloudios.realm"] = instance.RealmId.ToString(),
                ["cloudios.database"] = instance.Id.ToString(),
                ["cloudios.type"] = "managed-database",
                ["cloudios.managed"] = "true"
            },
            Env = BuildEnvironment(instance.Type, password),
            HostConfig = new HostConfig
            {
                // Tier-driven hardware limits (HostConfig.Resources).
                Memory = tier.MemoryLimitBytes,
                NanoCPUs = (long)(tier.CpuLimitCores * NanoCpusPerCore),
                // Persist database data on the host so it survives restarts.
                Binds = new List<string> { $"{hostPath}:{dataPath}" },
                RestartPolicy = new RestartPolicy { Name = RestartPolicyKind.UnlessStopped }
            },
            NetworkingConfig = new NetworkingConfig
            {
                EndpointsConfig = new Dictionary<string, EndpointSettings>
                {
                    [networkName] = new EndpointSettings()
                }
            }
        };
    }

    internal static string BuildContainerName(ManagedDatabaseInstance instance)
    {
        var sanitized = instance.Name.Replace(" ", "-").ToLowerInvariant();
        return $"cloudios-db-{sanitized}-{instance.Id:N}";
    }

    internal static string GetImage(ManagedDatabaseType type) => type switch
    {
        ManagedDatabaseType.MySQL => "mysql:latest",
        ManagedDatabaseType.MongoDB => "mongo:latest",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported managed database type")
    };

    internal static string GetDataPath(ManagedDatabaseType type) => type switch
    {
        ManagedDatabaseType.MySQL => "/var/lib/mysql",
        ManagedDatabaseType.MongoDB => "/data/db",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported managed database type")
    };

    internal static int GetPort(ManagedDatabaseType type) => type switch
    {
        ManagedDatabaseType.MySQL => 3306,
        ManagedDatabaseType.MongoDB => 27017,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported managed database type")
    };

    internal static string GetRootUsername(ManagedDatabaseType type) => type switch
    {
        ManagedDatabaseType.MySQL => "root",
        ManagedDatabaseType.MongoDB => "root",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported managed database type")
    };

    internal static List<string> BuildEnvironment(ManagedDatabaseType type, string password) => type switch
    {
        ManagedDatabaseType.MySQL =>
        [
            $"MYSQL_ROOT_PASSWORD={password}"
        ],
        ManagedDatabaseType.MongoDB =>
        [
            "MONGO_INITDB_ROOT_USERNAME=root",
            $"MONGO_INITDB_ROOT_PASSWORD={password}"
        ],
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported managed database type")
    };

    private static string GeneratePassword()
    {
        // URL-safe base64 without padding so it is shell/connection-string friendly.
        var bytes = RandomNumberGenerator.GetBytes(24);
        return Convert.ToBase64String(bytes)
            .Replace("+", "")
            .Replace("/", "")
            .Replace("=", "");
    }
}
