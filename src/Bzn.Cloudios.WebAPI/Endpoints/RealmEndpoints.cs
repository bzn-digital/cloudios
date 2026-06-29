using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class RealmEndpoints
{
    public static void MapRealmEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/realms");

        group.MapGet("/", async (RealmService service, int page = 1, int pageSize = 20, string? search = null, string? status = null, string? sortBy = null, CancellationToken ct = default) =>
        {
            var result = await service.ListAsync(page, pageSize, search, status, sortBy, ct);
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

        group.MapPost("/{id:guid}/suspend", async (Guid id, RealmService service, CancellationToken ct) =>
        {
            var (response, error) = await service.SuspendAsync(id, ct);
            if (error is not null) return error == "Realm not found" ? Results.NotFound() : Results.Conflict(new { error });
            return Results.Ok(response);
        });

        group.MapPost("/{id:guid}/reactivate", async (Guid id, RealmService service, CancellationToken ct) =>
        {
            var (response, error) = await service.ReactivateAsync(id, ct);
            if (error is not null) return error == "Realm not found" ? Results.NotFound() : Results.Conflict(new { error });
            return Results.Ok(response);
        });

        group.MapPut("/{id:guid}/quotas", async (Guid id, UpdateQuotasRequest request, RealmService service, CancellationToken ct) =>
        {
            var (realm, error) = await service.UpdateQuotasAsync(id, request, ct);
            if (error is not null) return error == "Realm not found" ? Results.NotFound() : Results.Conflict(new { error });
            return Results.Ok(realm);
        });

        group.MapGet("/{id:guid}/stats", async (Guid id, RealmService service, CancellationToken ct) =>
        {
            var result = await service.GetStatsAsync(id, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }
}
