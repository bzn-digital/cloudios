using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bzn.Cloudios.Application.Services;

public sealed class ManagedAppService : IManagedAppService
{
    private const int MaxPortRetries = 5;
    private readonly CloudiosDbContext _context;
    private readonly ITenantProvider _tenantProvider;
    private readonly IManagedAppPortAllocator _portAllocator;

    public ManagedAppService(
        CloudiosDbContext context,
        ITenantProvider tenantProvider,
        IManagedAppPortAllocator portAllocator)
    {
        _context = context;
        _tenantProvider = tenantProvider;
        _portAllocator = portAllocator;
    }

    public async Task<ManagedAppInstance> ProvisionAsync(Guid templateId, string name, CancellationToken ct = default)
    {
        var realmId = _tenantProvider.RealmId;
        var template = await _context.ManagedAppTemplates
            .FirstOrDefaultAsync(t => t.Id == templateId, ct)
            ?? throw new InvalidOperationException($"Template {templateId} not found.");

        var specs = InstanceSizeCatalog.GetSpecs(template.DefaultInstanceSize);

        for (int attempt = 0; attempt < MaxPortRetries; attempt++)
        {
            var port = await _portAllocator.AllocateNextPortAsync(ct);

            var instance = new ManagedAppInstance
            {
                Id = Guid.NewGuid(),
                RealmId = realmId,
                TemplateId = templateId,
                Name = name,
                HostPort = port,
                Status = ManagedAppStatus.Provisioning,
                Size = template.DefaultInstanceSize,
                CpuLimitCores = specs.CpuLimitCores,
                MemoryLimitBytes = specs.MemoryLimitBytes,
                CostPerHourBRL = specs.CostPerHourBRL,
                CreatedAt = DateTime.UtcNow
            };

            _context.ManagedAppInstances.Add(instance);

            try
            {
                await _context.SaveChangesAsync(ct);
                return instance;
            }
            catch (DbUpdateException) when (attempt < MaxPortRetries - 1)
            {
                _context.Entry(instance).State = EntityState.Detached;
            }
        }

        throw new InvalidOperationException("Failed to allocate a unique port after multiple retries.");
    }
}
