using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Events;
using Bzn.Cloudios.Application.Extensions;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Bzn.Cloudios.Application.Services;

public sealed class ContainerCrudService
{
    private readonly CloudiosDbContext _context;
    private readonly IContainerService _containerService;
    private readonly ITenantProvider _tenantProvider;
    private readonly IEventBus _eventBus;
    private readonly ILogger<ContainerCrudService> _logger;

    public ContainerCrudService(
        CloudiosDbContext context,
        IContainerService containerService,
        ITenantProvider tenantProvider,
        IEventBus eventBus,
        ILogger<ContainerCrudService> logger)
    {
        _context = context;
        _containerService = containerService;
        _tenantProvider = tenantProvider;
        _eventBus = eventBus;
        _logger = logger;
    }

    public async Task<ContainerListResponse> ListAsync(int page = 1, int pageSize = 20, string? search = null, string? status = null, CancellationToken ct = default)
    {
        var realmId = _tenantProvider.RealmId;
        var query = _context.Containers.ForRealm(realmId).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(c => c.Name.Contains(search));

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ContainerStatus>(status, out var statusFilter))
            query = query.Where(c => c.Status == statusFilter);

        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new ContainerListItem
            {
                Id = c.Id,
                Name = c.Name,
                ImageName = c.ImageName,
                InternalPort = c.InternalPort,
                Status = c.Status.ToString(),
                CpuLimitCores = c.CpuLimitCores,
                MemoryLimitBytes = c.MemoryLimitBytes,
                CostPerHourBRL = c.CostPerHourBRL,
                CurrentMonthCostBRL = 0,
                StartedAtUtc = c.StartedAtUtc,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(ct);

        return new ContainerListResponse
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            HasNextPage = total > page * pageSize
        };
    }

    public async Task<AdminContainerListResponse> ListAllAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = _context.Containers.Include(c => c.Realm).AsQueryable();
        var total = await query.CountAsync(ct);

        var items = await query
            .OrderBy(c => c.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new AdminContainerListItem
            {
                Id = c.Id,
                RealmId = c.RealmId,
                RealmName = c.Realm.Name,
                Name = c.Name,
                ImageName = c.ImageName,
                Status = c.Status.ToString(),
                CpuLimitCores = c.CpuLimitCores,
                MemoryLimitBytes = c.MemoryLimitBytes,
                CostPerHourBRL = c.CostPerHourBRL,
                CurrentMonthCostBRL = 0
            })
            .ToListAsync(ct);

        return new AdminContainerListResponse
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize,
            HasNextPage = total > page * pageSize
        };
    }

    public async Task<ContainerDetailResponse?> GetByIdAsync(Guid containerId, CancellationToken ct = default)
    {
        var container = await _context.Containers
            .Include(c => c.Volumes)
            .Include(c => c.EnvironmentVariables)
            .FirstOrDefaultAsync(c => c.Id == containerId, ct);

        if (container is null) return null;

        return MapToDetail(container);
    }

    public async Task<(ContainerDetailResponse? Container, string? Error)> CreateAsync(CreateContainerRequest request, CancellationToken ct = default)
    {
        var realmId = _tenantProvider.RealmId;

        var error = ValidateCreate(request);
        if (error is not null) return (null, error);

        if (await _context.Containers.ForRealm(realmId).AnyAsync(c => c.Name == request.Name, ct))
            return (null, "Container name already exists in this realm");

        var container = new Container
        {
            Id = Guid.NewGuid(),
            RealmId = realmId,
            Name = request.Name,
            ImageName = request.ImageName,
            InternalPort = request.InternalPort,
            CpuLimitCores = request.CpuLimitCores,
            MemoryLimitBytes = request.MemoryLimitBytes,
            CostPerHourBRL = request.CostPerHourBRL,
            Status = ContainerStatus.Stopped,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var vol in request.Volumes)
        {
            container.Volumes.Add(new ContainerVolume
            {
                Id = Guid.NewGuid(),
                ContainerId = container.Id,
                HostPath = vol.HostPath,
                ContainerPath = vol.ContainerPath,
                IsReadOnly = vol.IsReadOnly
            });
        }

        foreach (var (key, value) in request.EnvironmentVariables)
        {
            container.EnvironmentVariables.Add(new ContainerEnvVar
            {
                Id = Guid.NewGuid(),
                ContainerId = container.Id,
                Key = key,
                Value = value
            });
        }

        _context.Containers.Add(container);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Container {Name} created in realm {RealmId}", container.Name, realmId);
        return (MapToDetail(container), null);
    }

    public async Task<ContainerActionResponse> DeployAsync(Guid containerId, CancellationToken ct = default)
    {
        try
        {
            var result = await _containerService.DeployAsync(containerId, ct);
            var container = await _context.Containers.FindAsync([containerId], ct);
            if (container is not null)
                await _eventBus.PublishAsync(new ContainerStartedEvent(containerId, container.RealmId, container.Name, DateTime.UtcNow), ct);
            return result;
        }
        catch (Exception ex)
        {
            var container = await _context.Containers.FindAsync([containerId], ct);
            if (container is not null)
                await _eventBus.PublishAsync(new ContainerFailedEvent(containerId, container.RealmId, container.Name, ex.Message, DateTime.UtcNow), ct);
            throw;
        }
    }

    public async Task<ContainerActionResponse> StartAsync(Guid containerId, CancellationToken ct = default)
    {
        var result = await _containerService.StartAsync(containerId, ct);
        var container = await _context.Containers.FindAsync([containerId], ct);
        if (container is not null)
            await _eventBus.PublishAsync(new ContainerStartedEvent(containerId, container.RealmId, container.Name, DateTime.UtcNow), ct);
        return result;
    }

    public async Task<ContainerActionResponse> StopAsync(Guid containerId, CancellationToken ct = default)
    {
        var result = await _containerService.StopAsync(containerId, ct);
        var container = await _context.Containers.FindAsync([containerId], ct);
        if (container is not null)
            await _eventBus.PublishAsync(new ContainerStoppedEvent(containerId, container.RealmId, container.Name, DateTime.UtcNow), ct);
        return result;
    }

    public async Task<ContainerActionResponse> RestartAsync(Guid containerId, CancellationToken ct = default)
    {
        var result = await _containerService.RestartAsync(containerId, ct);
        var container = await _context.Containers.FindAsync([containerId], ct);
        if (container is not null)
            await _eventBus.PublishAsync(new ContainerStartedEvent(containerId, container.RealmId, container.Name, DateTime.UtcNow), ct);
        return result;
    }

    public async Task DeleteAsync(Guid containerId, CancellationToken ct = default)
    {
        var container = await _context.Containers.FindAsync([containerId], ct);
        var realmId = container?.RealmId ?? Guid.Empty;
        var name = container?.Name ?? "unknown";

        await _containerService.DeleteAsync(containerId, ct);
        await _eventBus.PublishAsync(new ContainerDeletedEvent(containerId, realmId, name, DateTime.UtcNow), ct);
    }

    private static string? ValidateCreate(CreateContainerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ImageName))
            return "Image name is required";

        if (request.CpuLimitCores < 0.1 || request.CpuLimitCores > 4.0)
            return "CPU limit must be between 0.1 and 4.0 cores";

        if (request.MemoryLimitBytes < 128 * 1024 * 1024 || request.MemoryLimitBytes > 8L * 1024 * 1024 * 1024)
            return "Memory limit must be between 128MB and 8GB";

        return null;
    }

    private static ContainerDetailResponse MapToDetail(Container c)
    {
        return new ContainerDetailResponse
        {
            Id = c.Id,
            Name = c.Name,
            ImageName = c.ImageName,
            InternalPort = c.InternalPort,
            Status = c.Status.ToString(),
            CpuLimitCores = c.CpuLimitCores,
            MemoryLimitBytes = c.MemoryLimitBytes,
            CostPerHourBRL = c.CostPerHourBRL,
            CurrentMonthCostBRL = 0,
            DockerContainerId = c.DockerContainerId,
            StartedAtUtc = c.StartedAtUtc,
            CreatedAt = c.CreatedAt,
            Volumes = c.Volumes.Select(v => new ContainerVolumeDto
            {
                Id = v.Id,
                HostPath = v.HostPath,
                ContainerPath = v.ContainerPath,
                IsReadOnly = v.IsReadOnly
            }).ToList(),
            EnvironmentVariables = c.EnvironmentVariables.Select(e => new ContainerEnvVarDto
            {
                Id = e.Id,
                Key = e.Key,
                Value = e.Value
            }).ToList()
        };
    }
}
