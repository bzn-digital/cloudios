using Bzn.Cloudios.Domain;
using Bzn.Cloudios.Domain.Entities;
using Bzn.Cloudios.Domain.Enums;

namespace Bzn.Cloudios.Tests;

public class ManagedAppTests
{
    [Fact]
    public void ManagedAppStatus_HasFiveStates()
    {
        var values = Enum.GetValues<ManagedAppStatus>();
        Assert.Equal(5, values.Length);
        Assert.Contains(ManagedAppStatus.Imaging, values);
        Assert.Contains(ManagedAppStatus.Running, values);
        Assert.Contains(ManagedAppStatus.Stopped, values);
        Assert.Contains(ManagedAppStatus.Failed, values);
        Assert.Contains(ManagedAppStatus.Terminated, values);
    }

    [Fact]
    public void ManagedAppInstance_CanInstantiateWithEachStatus()
    {
        foreach (var status in Enum.GetValues<ManagedAppStatus>())
        {
            var instance = new ManagedAppInstance
            {
                Id = Guid.NewGuid(),
                RealmId = Guid.NewGuid(),
                TemplateId = Guid.NewGuid(),
                Name = "test-app",
                Status = status,
                CreatedAt = DateTime.UtcNow
            };

            Assert.Equal(status, instance.Status);
        }
    }

    [Fact]
    public void InstanceSizeCatalog_Nano1s_ReturnsCorrectSpecs()
    {
        var specs = InstanceSizeCatalog.GetSpecs(InstanceSize.Nano1s);
        Assert.Equal(0.25, specs.CpuLimitCores);
        Assert.Equal(268435456, specs.MemoryLimitBytes); // 256MB
        Assert.Equal(0.01m, specs.CostPerHourBRL);
    }

    [Fact]
    public void InstanceSizeCatalog_Micro1s_ReturnsCorrectSpecs()
    {
        var specs = InstanceSizeCatalog.GetSpecs(InstanceSize.Micro1s);
        Assert.Equal(0.5, specs.CpuLimitCores);
        Assert.Equal(536870912, specs.MemoryLimitBytes); // 512MB
        Assert.Equal(0.02m, specs.CostPerHourBRL);
    }

    [Fact]
    public void InstanceSizeCatalog_Small1s_ReturnsCorrectSpecs()
    {
        var specs = InstanceSizeCatalog.GetSpecs(InstanceSize.Small1s);
        Assert.Equal(1.0, specs.CpuLimitCores);
        Assert.Equal(1073741824, specs.MemoryLimitBytes); // 1GB
        Assert.Equal(0.04m, specs.CostPerHourBRL);
    }

    [Fact]
    public void InstanceSizeCatalog_Medium1s_ReturnsCorrectSpecs()
    {
        var specs = InstanceSizeCatalog.GetSpecs(InstanceSize.Medium1s);
        Assert.Equal(2.0, specs.CpuLimitCores);
        Assert.Equal(2147483648, specs.MemoryLimitBytes); // 2GB
        Assert.Equal(0.08m, specs.CostPerHourBRL);
    }

    [Fact]
    public void InstanceSizeCatalog_Large1s_ReturnsCorrectSpecs()
    {
        var specs = InstanceSizeCatalog.GetSpecs(InstanceSize.Large1s);
        Assert.Equal(4.0, specs.CpuLimitCores);
        Assert.Equal(4294967296, specs.MemoryLimitBytes); // 4GB
        Assert.Equal(0.16m, specs.CostPerHourBRL);
    }

    [Fact]
    public void InstanceSizeCatalog_InvalidSize_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            InstanceSizeCatalog.GetSpecs((InstanceSize)999);
        });
    }

    [Fact]
    public void ManagedAppTemplate_DefaultEnvVars_IsEmptyDictionary()
    {
        var template = new ManagedAppTemplate
        {
            Id = Guid.NewGuid(),
            Name = "test-template",
            Description = "Test template",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Assert.NotNull(template.DefaultEnvVars);
        Assert.Empty(template.DefaultEnvVars);
    }

    [Fact]
    public void ManagedAppTemplate_CanSetDefaultEnvVars()
    {
        var template = new ManagedAppTemplate
        {
            Id = Guid.NewGuid(),
            Name = "test-template",
            Description = "Test template",
            DockerImage = "nginx:latest",
            DefaultEnvVars = new Dictionary<string, string>
            {
                { "PORT", "8080" },
                { "ENV", "production" }
            },
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Assert.Equal(2, template.DefaultEnvVars.Count);
        Assert.Equal("8080", template.DefaultEnvVars["PORT"]);
        Assert.Equal("production", template.DefaultEnvVars["ENV"]);
    }

    [Fact]
    public void ManagedAppTemplate_DefaultInstanceSize_IsMicro1s()
    {
        var template = new ManagedAppTemplate
        {
            Id = Guid.NewGuid(),
            Name = "test-template",
            Description = "Test template",
            DockerImage = "nginx:latest",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        Assert.Equal(InstanceSize.Micro1s, template.DefaultInstanceSize);
    }

    [Fact]
    public void ManagedAppInstance_DefaultStatus_IsImaging()
    {
        var instance = new ManagedAppInstance
        {
            Id = Guid.NewGuid(),
            RealmId = Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            Name = "test-app",
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal(ManagedAppStatus.Imaging, instance.Status);
    }

    [Fact]
    public void ManagedAppInstance_DefaultSize_IsMicro1s()
    {
        var instance = new ManagedAppInstance
        {
            Id = Guid.NewGuid(),
            RealmId = Guid.NewGuid(),
            TemplateId = Guid.NewGuid(),
            Name = "test-app",
            CreatedAt = DateTime.UtcNow
        };

        Assert.Equal(InstanceSize.Micro1s, instance.Size);
    }
}
