using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class RealmEndpoints
{
    public static void MapRealmEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/realms");

        group.MapGet("/", async (RealmService service, int page = 1, int pageSize = 20, string? search = null, CancellationToken ct = default) =>
        {
            var result = await service.ListAsync(page, pageSize, search, ct);
            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, RealmService service, CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        group.MapPost("/", async (CreateRealmRequest request, RealmService service, CancellationToken ct) =>
        {
            var (realm, error) = await service.CreateAsync(request, ct);
            if (error is not null) return Results.Conflict(new { error });
            return Results.Created($"/api/realms/{realm!.Id}", realm);
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateRealmRequest request, RealmService service, CancellationToken ct) =>
        {
            var (realm, error) = await service.UpdateAsync(id, request, ct);
            if (error is not null) return error == "Realm not found" ? Results.NotFound() : Results.Conflict(new { error });
            return Results.Ok(realm);
        });

        group.MapDelete("/{id:guid}", async (Guid id, RealmService service, CancellationToken ct) =>
        {
            var (success, error) = await service.DeleteAsync(id, ct);
            if (!success) return error == "Realm not found" ? Results.NotFound() : Results.Conflict(new { error });
            return Results.NoContent();
        });
    }
}
