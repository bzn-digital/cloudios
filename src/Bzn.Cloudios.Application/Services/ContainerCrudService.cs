using Bzn.Cloudios.Application.Abstractions;
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
    private readonly ILogger<ContainerCrudService> _logger;

    public ContainerCrudService(
        CloudiosDbContext context,
        IContainerService containerService,
        ILogger<ContainerCrudService> logger)
    {
        _context = context;
        _containerService = containerService;
        _logger = logger;
    }

    public async Task<ContainerListResponse> ListByRealmAsync(Guid realmId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var query = _context.Containers.ForRealm(realmId);
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

    public async Task<ContainerDetailResponse?> GetByIdAsync(Guid containerId, CancellationToken ct = default)
    {
        var container = await _context.Containers
            .Include(c => c.Volumes)
            .Include(c => c.EnvironmentVariables)
            .FirstOrDefaultAsync(c => c.Id == containerId, ct);

        if (container is null) return null;

        return MapToDetail(container);
    }

    public async Task<(ContainerDetailResponse? Container, string? Error)> CreateAsync(Guid realmId, CreateContainerRequest request, CancellationToken ct = default)
    {
        if (!await _context.Realms.AnyAsync(r => r.Id == realmId, ct))
            return (null, "Realm not found");

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
        return await _containerService.DeployAsync(containerId, ct);
    }

    public async Task<ContainerActionResponse> StartAsync(Guid containerId, CancellationToken ct = default)
    {
        return await _containerService.StartAsync(containerId, ct);
    }

    public async Task<ContainerActionResponse> StopAsync(Guid containerId, CancellationToken ct = default)
    {
        return await _containerService.StopAsync(containerId, ct);
    }

    public async Task<ContainerActionResponse> RestartAsync(Guid containerId, CancellationToken ct = default)
    {
        return await _containerService.RestartAsync(containerId, ct);
    }

    public async Task DeleteAsync(Guid containerId, CancellationToken ct = default)
    {
        await _containerService.DeleteAsync(containerId, ct);
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
