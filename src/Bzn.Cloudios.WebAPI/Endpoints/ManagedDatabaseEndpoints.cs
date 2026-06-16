using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class ManagedDatabaseEndpoints
{
    public static void MapManagedDatabaseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/managed-databases");

        // List available tiers with real-time billing forecasts (BRL).
        group.MapGet("/tiers", async (ManagedDatabaseCrudService service, CancellationToken ct) =>
        {
            var result = await service.GetTiersAsync(ct);
            return Results.Ok(result);
        }).AllowAnonymous();

        // List managed databases for the caller's realm.
        group.MapGet("/", async (ManagedDatabaseCrudService service, CancellationToken ct) =>
        {
            var result = await service.ListAsync(ct);
            return Results.Ok(result);
        });

        // Create a managed database for the caller's realm.
        group.MapPost("/", async (CreateManagedDatabaseRequest request, ManagedDatabaseCrudService service, CancellationToken ct) =>
        {
            Console.WriteLine($"Creating managed database: {request.Name}, Tier: {request.TierId}, Type: {request.Type}, Disk: {request.DiskSizeGB}GB");
            var (instance, error, statusCode) = await service.CreateAsync(request, ct);
            if (error is not null)
            {
                Console.WriteLine($"Failed to create database: {error} (Status: {statusCode})");
                return Results.Json(new ErrorResponse { Detail = error }, statusCode: statusCode);
            }

            Console.WriteLine($"Successfully created database: {instance!.Id}");
            return Results.Created($"/api/managed-databases/{instance!.Id}", instance);
        });
    }
}
