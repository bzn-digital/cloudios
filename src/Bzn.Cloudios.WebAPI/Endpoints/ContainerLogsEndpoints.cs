using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Microsoft.AspNetCore.Authorization;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class ContainerLogsEndpoints
{
    public static void MapContainerLogsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/containers/{id}");

        group.MapGet("/logs", async (Guid id, int? tail, ContainerCrudService crudService, CancellationToken ct) =>
        {
            var logs = await crudService.GetContainerLogsAsync(id, tail ?? 100, ct);
            return Results.Ok(new ContainerLogsResponse { ContainerId = id, Logs = logs });
        });
    }
}
