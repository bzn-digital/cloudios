using Bzn.Cloudios.Domain.Enums;

namespace Bzn.Cloudios.Domain;

public static class InstanceSizeCatalog
{
    public static (double CpuLimitCores, long MemoryLimitBytes, decimal CostPerHourBRL) GetSpecs(InstanceSize size) => size switch
    {
        InstanceSize.Nano1s => (0.25, 268435456, 0.01m),   // 0.25 vCPU, 256MB RAM
        InstanceSize.Micro1s => (0.5, 536870912, 0.02m),  // 0.5 vCPU, 512MB RAM
        InstanceSize.Small1s => (1.0, 1073741824, 0.04m), // 1.0 vCPU, 1GB RAM
        InstanceSize.Medium1s => (2.0, 2147483648, 0.08m), // 2.0 vCPU, 2GB RAM
        InstanceSize.Large1s => (4.0, 4294967296, 0.16m),   // 4.0 vCPU, 4GB RAM
        _ => throw new ArgumentOutOfRangeException(nameof(size), size, null)
    };
}
