using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Domain.Enums;

namespace Bzn.Cloudios.Tests;

public class DomainDtoTests
{
    [Fact]
    public void ContainerStatus_Enum_HasExpectedValues()
    {
        Assert.Equal(4, Enum.GetNames<ContainerStatus>().Length);
        Assert.True(Enum.IsDefined(ContainerStatus.Deploying));
        Assert.True(Enum.IsDefined(ContainerStatus.Running));
        Assert.True(Enum.IsDefined(ContainerStatus.Stopped));
        Assert.True(Enum.IsDefined(ContainerStatus.Failed));
    }

    [Fact]
    public void UserRole_Enum_HasExpectedValues()
    {
        Assert.Equal(7, Enum.GetNames<UserRole>().Length);
        Assert.True(Enum.IsDefined(UserRole.PlatformAdmin));
        Assert.True(Enum.IsDefined(UserRole.PlatformUser));
        Assert.True(Enum.IsDefined(UserRole.PlatformSre));
        Assert.True(Enum.IsDefined(UserRole.RealmOwner));
        Assert.True(Enum.IsDefined(UserRole.RealmAdmin));
        Assert.True(Enum.IsDefined(UserRole.RealmUser));
        Assert.True(Enum.IsDefined(UserRole.RealmSre));
    }

    [Fact]
    public void LoginRequest_Defaults_AreEmptyString()
    {
        var req = new LoginRequest();
        Assert.Equal(string.Empty, req.Email);
        Assert.Equal(string.Empty, req.Password);
    }

    [Fact]
    public void HealthResponse_Defaults_AreEmptyString()
    {
        var resp = new HealthResponse();
        Assert.Equal(string.Empty, resp.Status);
        Assert.Equal(string.Empty, resp.Version);
        Assert.Equal(string.Empty, resp.Uptime);
    }

    [Fact]
    public void CreateContainerRequest_EnvVars_Default_ToEmpty()
    {
        var req = new CreateContainerRequest();
        Assert.Empty(req.EnvironmentVariables);
    }

    [Fact]
    public void ContainerListResponse_Pagination_Defaults()
    {
        var resp = new ContainerListResponse();
        Assert.Empty(resp.Items);
        Assert.Equal(0, resp.TotalCount);
        Assert.Equal(0, resp.Page);
        Assert.Equal(0, resp.PageSize);
        Assert.False(resp.HasNextPage);
    }
}
