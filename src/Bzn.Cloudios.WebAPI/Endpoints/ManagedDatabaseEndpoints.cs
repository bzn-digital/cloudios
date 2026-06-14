using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class ManagedDatabaseEndpoints
{
    public static void MapManagedDatabaseEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/managed-databases");

        // List available tiers with real-time billing forecasts (BRL).
        group.MapGet("/tiers", async (IManagedDatabaseService service, CancellationToken ct) =>
        {
            var result = await service.GetTiersAsync(ct);
            return Results.Ok(result);
        });

        // Create a managed database for the caller's realm.
        group.MapPost("/", async (CreateManagedDatabaseRequest request, IManagedDatabaseService service, CancellationToken ct) =>
        {
            var (instance, error, statusCode) = await service.CreateAsync(request, ct);
            if (error is not null)
                return Results.Json(new ErrorResponse { Detail = error }, statusCode: statusCode);

            return Results.Created($"/api/managed-databases/{instance!.Id}", instance);
        });
    }
}
