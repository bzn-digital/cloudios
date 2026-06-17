using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class ContainerEndpoints
{
    public static void MapContainerEndpoints(this WebApplication app)
    {
        // Admin endpoint: all containers across realms
        app.MapGet("/api/containers/all", async (ContainerCrudService service, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        {
            var result = await service.ListAllAsync(page, pageSize, ct);
            return Results.Ok(result);
        });

        // List networks
        app.MapGet("/api/networks", async (IDockerNetworkService networkService, CancellationToken ct) =>
        {
            var networks = await networkService.ListNetworksAsync(ct);
            return Results.Ok(networks);
        });

        var group = app.MapGroup("/api/containers");

        // List containers (realm from JWT)
        group.MapGet("/", async (ContainerCrudService service, int page = 1, int pageSize = 20, string? search = null, string? status = null, CancellationToken ct = default) =>
        {
            var result = await service.ListAsync(page, pageSize, search, status, ct);
            return Results.Ok(result);
        });

        // Get container detail
        group.MapGet("/{id:guid}", async (Guid id, ContainerCrudService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        // Create container (RealmOwner+ only)
        group.MapPost("/", async (CreateContainerRequest request, ContainerCrudService service, CancellationToken ct) =>
        {
            var (container, error) = await service.CreateAsync(request, ct);
            if (error is not null) return Results.Conflict(new ErrorResponse { Detail = error });
            return Results.Created($"/api/containers/{container!.Id}", container);
        });

        // Deploy container (create + start in Docker)
        group.MapPost("/{id:guid}/deploy", async (Guid id, ContainerCrudService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.DeployAsync(id, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new ErrorResponse { Detail = ex.Message });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new ErrorResponse { Detail = ex.Message });
            }
        });

        // Start container
        group.MapPost("/{id:guid}/start", async (Guid id, ContainerCrudService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.StartAsync(id, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse { Detail = ex.Message });
            }
        });

        // Stop container
        group.MapPost("/{id:guid}/stop", async (Guid id, ContainerCrudService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.StopAsync(id, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse { Detail = ex.Message });
            }
        });

        // Restart container
        group.MapPost("/{id:guid}/restart", async (Guid id, ContainerCrudService service, CancellationToken ct) =>
        {
            try
            {
                var result = await service.RestartAsync(id, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ErrorResponse { Detail = ex.Message });
            }
        });

        // Delete container (RealmOwner only)
        group.MapDelete("/{id:guid}", async (Guid id, ContainerCrudService service, CancellationToken ct) =>
        {
            try
            {
                await service.DeleteAsync(id, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new ErrorResponse { Detail = ex.Message });
            }
        });
    }
}
