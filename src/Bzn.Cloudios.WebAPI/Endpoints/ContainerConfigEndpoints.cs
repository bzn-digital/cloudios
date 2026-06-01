using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Microsoft.AspNetCore.Authorization;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class ContainerConfigEndpoints
{
    public static void MapContainerConfigEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/containers/{id}");

        group.MapPut("/env-vars", async (Guid id, Dictionary<string, string> envVars, IContainerService containerService, CancellationToken ct) =>
        {
            await containerService.UpdateEnvVarsAsync(id, envVars, ct);
            return Results.Ok(new { message = "Environment variables updated" });
        }).RequireAuthorization("RequireRealmOwner");

        group.MapPut("/volumes", async (Guid id, List<ContainerVolumeRequest> volumes, IContainerService containerService, CancellationToken ct) =>
        {
            await containerService.UpdateVolumesAsync(id, volumes, ct);
            return Results.Ok(new { message = "Volumes updated" });
        }).RequireAuthorization("RequireRealmOwner");
    }
}
