using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Extensions;
using Bzn.Cloudios.Domain;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class ManagedAppService : IManagedAppService
{
    private const int MinPort = 2000;
    private const int MaxPort = 4500;
    private const int MaxPortRetries = 10;
    private const long NanoCpusPerCore = 1_000_000_000L;

    private readonly CloudiosDbContext _context;
    private readonly DockerClient _dockerClient;
    private readonly IDockerNetworkService _dockerNetworkService;
    private readonly IManagedAppDeployQueue _deployQueue;
    private readonly ILogger<ManagedAppService> _logger;
    private readonly string _volumesBasePath;
    private readonly bool _skipDirectoryCreation;

    public ManagedAppService(
        CloudiosDbContext context,
        DockerClient dockerClient,
        IDockerNetworkService dockerNetworkService,
        IManagedAppDeployQueue deployQueue,
        IConfiguration configuration,
        ILogger<ManagedAppService> logger)
    {
        _context = context;
        _dockerClient = dockerClient;
        _dockerNetworkService = dockerNetworkService;
        _deployQueue = deployQueue;
        _logger = logger;
        _volumesBasePath = configuration["Volumes:BasePath"] ?? "/var/lib/cloudios";
        _skipDirectoryCreation = configuration.GetValue<bool>("Volumes:SkipDirectoryCreation");
    }

    public async Task<ManagedAppResponse> CreateAsync(Guid realmId, CreateManagedAppRequest request, CancellationToken ct = default)
    {
        // Validate name format (lowercase, no spaces, alphanumeric and hyphens only)
        var sanitizedName = request.Name.ToLowerInvariant().Replace(" ", "-");
        if (!System.Text.RegularExpressions.Regex.IsMatch(sanitizedName, "^[a-z0-9-]+$"))
        {
            throw new ArgumentException("Name must contain only lowercase letters, numbers, and hyphens", nameof(request.Name));
        }

        var realm = await _context.Realms.FirstOrDefaultAsync(r => r.Id == realmId, ct);
        if (realm is null)
            throw new InvalidOperationException($"Realm {realmId} not found");
        if (!realm.IsActive)
            throw new InvalidOperationException("Realm is not allowed to provision resources");

        // Check for duplicate name in the same realm
        var existing = await _context.ManagedAppInstances
            .ForRealm(realmId)
            .FirstOrDefaultAsync(i => i.Name == sanitizedName, ct);
        
        if (existing is not null)
        {
            throw new InvalidOperationException($"An instance with name '{sanitizedName}' already exists in this realm");
        }

        // Verify template exists
        var template = await _context.ManagedAppTemplates
            .FirstOrDefaultAsync(t => t.Id == request.TemplateId, ct);
        
        if (template is null)
        {
            throw new InvalidOperationException($"Template {request.TemplateId} not found");
        }

        // Get instance size specs
        var (cpuLimit, memoryLimit, costPerHour) = InstanceSizeCatalog.GetSpecs(request.Size);

        // Allocate port and create instance atomically to prevent race conditions
        for (int attempt = 0; attempt < MaxPortRetries; attempt++)
        {
            try
            {
                var usedPorts = await _context.ManagedAppInstances
                    .Select(i => i.HostPort)
                    .ToHashSetAsync(ct);

                var hostPort = -1;
                for (int port = MinPort; port <= MaxPort; port++)
                {
                    if (!usedPorts.Contains(port))
                    {
                        hostPort = port;
                        break;
                    }
                }

                if (hostPort == -1)
                    throw new InvalidOperationException($"No available ports in the managed app range ({MinPort}-{MaxPort}).");

                var instance = new ManagedAppInstance
                {
                    Id = Guid.NewGuid(),
                    RealmId = realmId,
                    TemplateId = request.TemplateId,
                    Name = sanitizedName,
                    HostPort = hostPort,
                    Status = ManagedAppStatus.Imaging,
                    Size = request.Size,
                    CpuLimitCores = cpuLimit,
                    MemoryLimitBytes = memoryLimit,
                    CostPerHourBRL = costPerHour,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ManagedAppInstances.Add(instance);
                await _context.SaveChangesAsync(ct);

                _deployQueue.Enqueue(instance.Id);

                _logger.LogInformation("Created managed app instance {InstanceId} with name {Name} for realm {RealmId}",
                    instance.Id, instance.Name, realmId);

                return MapToResponse(instance, template.DisplayName, template.InternalPort);
            }
            catch (DbUpdateException) when (attempt < MaxPortRetries - 1)
            {
                _context.ChangeTracker.Clear();
                await Task.Delay(50, ct);
            }
        }

        throw new InvalidOperationException("Failed to allocate port after maximum retries due to concurrent conflicts.");
    }

    public async Task<ManagedAppListResponse> ListAsync(Guid realmId, string? search, string? status, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _context.ManagedAppInstances
            .Include(i => i.Template)
            .ForRealm(realmId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i => i.Name.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ManagedAppStatus>(status, true, out var statusEnum))
        {
            query = query.Where(i => i.Status == statusEnum);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new ManagedAppListResponse
        {
            Items = items.Select(i => MapToResponse(i, i.Template.DisplayName, i.Template.InternalPort)).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ManagedAppResponse?> GetByIdAsync(Guid realmId, Guid instanceId, CancellationToken ct = default)
    {
        var instance = await _context.ManagedAppInstances
            .Include(i => i.Template)
            .ForRealm(realmId)
            .FirstOrDefaultAsync(i => i.Id == instanceId, ct);

        if (instance is null)
            return null;

        return MapToResponse(instance, instance.Template.DisplayName, instance.Template.InternalPort);
    }

    public async Task<ManagedAppActionResponse> StartInstanceAsync(Guid realmId, Guid instanceId, CancellationToken ct = default)
    {
        var instance = await _context.ManagedAppInstances
            .ForRealm(realmId)
            .FirstOrDefaultAsync(i => i.Id == instanceId, ct);

        if (instance is null)
            throw new InvalidOperationException($"Managed app instance {instanceId} not found in realm {realmId}");

        if (instance.Status is ManagedAppStatus.Imaging or ManagedAppStatus.Initializing)
            throw new InvalidOperationException($"Instance {instanceId} is currently being deployed (status: {instance.Status})");

        try
        {
            if (instance.DockerContainerId is null)
            {
                await DeployContainerAsync(instance, ct);
            }
            else
            {
                await _dockerClient.Containers.StartContainerAsync(instance.DockerContainerId, new ContainerStartParameters(), ct);
            }

            instance.Status = ManagedAppStatus.Running;
            instance.StartedAtUtc = DateTime.UtcNow;
            instance.StoppedAtUtc = null;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Started managed app instance {InstanceId} ({Name})", instance.Id, instance.Name);
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            _logger.LogError(ex, "Docker/Podman socket not available. Please ensure Podman socket is running: 'systemctl --user start podman.socket' or 'sudo systemctl start podman.socket'");
            instance.Status = ManagedAppStatus.Failed;
            await _context.SaveChangesAsync(CancellationToken.None);
            throw new InvalidOperationException("Docker/Podman socket not available. Please ensure the container runtime socket is running.", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start managed app instance {InstanceId} ({Name})", instance.Id, instance.Name);
            instance.Status = ManagedAppStatus.Failed;
            try
            {
                await _context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to persist Failed status for managed app {InstanceId}", instance.Id);
            }
            throw;
        }

        return new ManagedAppActionResponse
        {
            Id = instance.Id,
            Status = instance.Status.ToString(),
            DockerContainerId = instance.DockerContainerId,
            StartedAtUtc = instance.StartedAtUtc
        };
    }

    public async Task<ManagedAppActionResponse> StopInstanceAsync(Guid realmId, Guid instanceId, CancellationToken ct = default)
    {
        var instance = await _context.ManagedAppInstances
            .ForRealm(realmId)
            .FirstOrDefaultAsync(i => i.Id == instanceId, ct);

        if (instance is null)
            throw new InvalidOperationException($"Managed app instance {instanceId} not found in realm {realmId}");

        if (instance.DockerContainerId is not null)
        {
            await _dockerClient.Containers.StopContainerAsync(instance.DockerContainerId, new ContainerStopParameters(), ct);
        }

        instance.Status = ManagedAppStatus.Stopped;
        instance.StoppedAtUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Stopped managed app instance {InstanceId} ({Name})", instance.Id, instance.Name);

        return new ManagedAppActionResponse
        {
            Id = instance.Id,
            Status = instance.Status.ToString(),
            DockerContainerId = instance.DockerContainerId,
            StartedAtUtc = instance.StartedAtUtc
        };
    }

    public async Task<ManagedAppActionResponse> RestartInstanceAsync(Guid realmId, Guid instanceId, CancellationToken ct = default)
    {
        var instance = await _context.ManagedAppInstances
            .ForRealm(realmId)
            .FirstOrDefaultAsync(i => i.Id == instanceId, ct);

        if (instance is null)
            throw new InvalidOperationException($"Managed app instance {instanceId} not found in realm {realmId}");

        if (instance.DockerContainerId is null)
            throw new InvalidOperationException($"Cannot restart instance {instanceId}: no Docker container associated");

        try
        {
            await _dockerClient.Containers.RestartContainerAsync(instance.DockerContainerId, new ContainerRestartParameters(), ct);
            instance.Status = ManagedAppStatus.Running;
            instance.StartedAtUtc = DateTime.UtcNow;
            instance.StoppedAtUtc = null;
            await _context.SaveChangesAsync(ct);

            _logger.LogInformation("Restarted managed app instance {InstanceId} ({Name})", instance.Id, instance.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to restart managed app instance {InstanceId} ({Name})", instance.Id, instance.Name);
            instance.Status = ManagedAppStatus.Failed;
            try
            {
                await _context.SaveChangesAsync(CancellationToken.None);
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to persist Failed status for managed app {InstanceId}", instance.Id);
            }
            throw;
        }

        return new ManagedAppActionResponse
        {
            Id = instance.Id,
            Status = instance.Status.ToString(),
            DockerContainerId = instance.DockerContainerId,
            StartedAtUtc = instance.StartedAtUtc
        };
    }

    public async Task DeleteInstanceAsync(Guid realmId, Guid instanceId, CancellationToken ct = default)
    {
        var instance = await _context.ManagedAppInstances
            .ForRealm(realmId)
            .FirstOrDefaultAsync(i => i.Id == instanceId, ct);

        if (instance is null)
            throw new InvalidOperationException($"Managed app instance {instanceId} not found in realm {realmId}");

        // Remove Docker container if it exists
        if (instance.DockerContainerId is not null)
        {
            try
            {
                await _dockerClient.Containers.RemoveContainerAsync(instance.DockerContainerId,
                    new ContainerRemoveParameters { Force = true, RemoveVolumes = true }, ct);
                _logger.LogInformation("Removed Docker container {DockerId}", instance.DockerContainerId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove Docker container {DockerId}", instance.DockerContainerId);
            }
        }

        // Remove volume directory if it exists
        var volumePath = Path.Combine(_volumesBasePath, "managed-apps", instance.Id.ToString("N"));
        if (Directory.Exists(volumePath))
        {
            try
            {
                Directory.Delete(volumePath, recursive: true);
                _logger.LogInformation("Removed volume directory {Path}", volumePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to remove volume directory {Path}", volumePath);
            }
        }

        instance.Status = ManagedAppStatus.Terminated;
        _context.ManagedAppInstances.Remove(instance);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Deleted managed app instance {InstanceId} ({Name})", instance.Id, instance.Name);
    }

    public async Task<ManagedAppTemplateListResponse> ListTemplatesAsync(string? category, string? search, CancellationToken ct = default)
    {
        var query = _context.ManagedAppTemplates.AsQueryable();

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(t => t.Category == category);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Name.Contains(search) || t.DisplayName.Contains(search) || t.Description.Contains(search));
        }

        var templates = await query.OrderBy(t => t.Category).ThenBy(t => t.DisplayName).ToListAsync(ct);

        return new ManagedAppTemplateListResponse
        {
            Items = templates.Select(t => new ManagedAppTemplateResponse
            {
                Id = t.Id,
                Slug = t.Slug,
                DisplayName = t.DisplayName,
                Name = t.Name,
                Description = t.Description,
                Category = t.Category,
                InternalPort = t.InternalPort,
                DefaultInstanceSize = t.DefaultInstanceSize.ToString()
            }).ToList()
        };
    }

    public async Task<AdminManagedAppListResponse> ListAllAsync(int page, int pageSize, Guid? realmId, string? status, CancellationToken ct = default)
    {
        var query = _context.ManagedAppInstances
            .Include(i => i.Template)
            .Include(i => i.Realm)
            .AsQueryable();

        if (realmId.HasValue)
        {
            query = query.Where(i => i.RealmId == realmId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ManagedAppStatus>(status, true, out var statusEnum))
        {
            query = query.Where(i => i.Status == statusEnum);
        }

        var totalCount = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new AdminManagedAppListResponse
        {
            Items = items.Select(i => new AdminManagedAppResponse
            {
                Id = i.Id,
                RealmId = i.RealmId,
                RealmName = i.Realm.Name,
                TemplateId = i.TemplateId,
                TemplateName = i.Template.DisplayName,
                Name = i.Name,
                HostPort = i.HostPort,
                InternalPort = i.Template.InternalPort,
                InternalAccess = $"{i.Name}:{i.Template.InternalPort}",
                Status = i.Status.ToString(),
                Size = i.Size.ToString(),
                DockerContainerId = i.DockerContainerId,
                CpuLimitCores = i.CpuLimitCores,
                MemoryLimitBytes = i.MemoryLimitBytes,
                CostPerHourBRL = i.CostPerHourBRL,
                CurrentMonthCostBRL = i.CostPerHourBRL * 720,
                CreatedAt = i.CreatedAt,
                StartedAtUtc = i.StartedAtUtc,
                StoppedAtUtc = i.StoppedAtUtc
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    private async Task DeployContainerAsync(ManagedAppInstance instance, CancellationToken ct)
    {
        var template = await _context.ManagedAppTemplates
            .FirstOrDefaultAsync(t => t.Id == instance.TemplateId, ct);

        if (template is null)
            throw new InvalidOperationException($"Template {instance.TemplateId} not found");

        var networkName = $"cloudios_{instance.RealmId:N}";
        await _dockerNetworkService.EnsureRealmNetworkAsync(instance.RealmId, ct);

        // Pull the image
        _logger.LogInformation("Pulling image {Image} for managed app {Name}...", template.DockerImage, instance.Name);
        try
        {
            await _dockerClient.Images.CreateImageAsync(
                new ImagesCreateParameters { FromImage = template.DockerImage },
                null,
                new Progress<JSONMessage>(msg => _logger.LogDebug("Pull progress: {Status}", msg.Status)),
                ct);
        }
        catch (System.Net.Http.HttpRequestException ex) when (ex.InnerException is System.Net.Sockets.SocketException)
        {
            _logger.LogError(ex, "Docker/Podman socket not available. Please ensure Podman socket is running: 'systemctl --user start podman.socket' or 'sudo systemctl start podman.socket'");
            throw new InvalidOperationException("Docker/Podman socket not available. Please ensure the container runtime socket is running.", ex);
        }
        catch (Docker.DotNet.DockerApiException ex) when (ex.ResponseBody?.Contains("permission denied") == true)
        {
            _logger.LogError(ex, "Docker/Podman permission denied. Check volumes path and ensure the application has write permissions to the volumes directory.");
            throw new InvalidOperationException("Docker/Podman permission denied. Ensure the volumes directory is writable by the application user.", ex);
        }
        _logger.LogInformation("Image {Image} pulled successfully", template.DockerImage);

        // Create volume directory
        var volumePath = Path.Combine(_volumesBasePath, "managed-apps", instance.Id.ToString("N"));
        if (!_skipDirectoryCreation)
        {
            Directory.CreateDirectory(volumePath);
        }

        // Build container name
        var containerName = $"cloudios-app-{instance.Name}-{instance.Id:N}";

        // Check for existing container with same name
        var existingContainers = await _dockerClient.Containers.ListContainersAsync(
            new ContainersListParameters { All = true }, ct);
        var existingContainer = existingContainers.FirstOrDefault(c => c.Names.Contains($"/{containerName}"));
        if (existingContainer is not null)
        {
            _logger.LogWarning("Container with name {Name} already exists (ID: {Id}), removing it", containerName, existingContainer.ID);
            await _dockerClient.Containers.RemoveContainerAsync(existingContainer.ID,
                new ContainerRemoveParameters { Force = true, RemoveVolumes = true }, ct);
        }

        // Create container
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
                    [$"{template.InternalPort}/tcp"] = new List<PortBinding> { new PortBinding { HostPort = instance.HostPort.ToString() } }
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

        await _dockerClient.Containers.StartContainerAsync(createResponse.ID, new ContainerStartParameters(), ct);

        _logger.LogInformation("Created and started Docker container {DockerId} for managed app {Name}", createResponse.ID, instance.Name);
    }

    private static ManagedAppResponse MapToResponse(ManagedAppInstance instance, string templateName, int internalPort)
    {
        // Calculate current month cost (720 hours = 30 days * 24 hours)
        var currentMonthCostBRL = instance.CostPerHourBRL * 720;

        return new ManagedAppResponse
        {
            Id = instance.Id,
            RealmId = instance.RealmId,
            TemplateId = instance.TemplateId,
            TemplateName = templateName,
            Name = instance.Name,
            HostPort = instance.HostPort,
            InternalPort = internalPort,
            InternalAccess = $"{instance.Name}:{internalPort}",
            Status = instance.Status.ToString(),
            Size = instance.Size.ToString(),
            DockerContainerId = instance.DockerContainerId,
            CpuLimitCores = instance.CpuLimitCores,
            MemoryLimitBytes = instance.MemoryLimitBytes,
            CostPerHourBRL = instance.CostPerHourBRL,
            CurrentMonthCostBRL = currentMonthCostBRL,
            CreatedAt = instance.CreatedAt,
            StartedAtUtc = instance.StartedAtUtc,
            StoppedAtUtc = instance.StoppedAtUtc
        };
    }
}
