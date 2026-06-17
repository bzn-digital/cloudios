using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Dto;
using Microsoft.AspNetCore.Mvc;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class ManagedAppEndpoints
{
    public static void MapManagedAppEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/managed-apps");

        // List managed apps for the caller's realm.
        group.MapGet("/", async (
            IManagedAppService service,
            ITenantProvider tenant,
            CancellationToken ct,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await service.ListAsync(tenant.RealmId, search, page, pageSize, ct);
            return Results.Ok(result);
        });

        // Get a specific managed app by ID.
        group.MapGet("/{id:guid}", async (
            Guid id,
            IManagedAppService service,
            ITenantProvider tenant,
            CancellationToken ct) =>
        {
            var result = await service.GetByIdAsync(tenant.RealmId, id, ct);
            if (result is null)
                return Results.NotFound(new { error = "Managed app instance not found" });
            return Results.Ok(result);
        });

        // Create a new managed app instance.
        group.MapPost("/", async (
            CreateManagedAppRequest request,
            IManagedAppService service,
            ITenantProvider tenant,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.CreateAsync(tenant.RealmId, request, ct);
                return Results.Created($"/api/managed-apps/{result.Id}", result);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Results.Conflict(new { error = ex.Message });
            }
        });

        // Start a managed app instance.
        group.MapPost("/{id:guid}/start", async (
            Guid id,
            IManagedAppService service,
            ITenantProvider tenant,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.StartInstanceAsync(tenant.RealmId, id, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        // Stop a managed app instance.
        group.MapPost("/{id:guid}/stop", async (
            Guid id,
            IManagedAppService service,
            ITenantProvider tenant,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.StopInstanceAsync(tenant.RealmId, id, ct);
                return Results.Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        // Delete a managed app instance.
        group.MapDelete("/{id:guid}", async (
            Guid id,
            IManagedAppService service,
            ITenantProvider tenant,
            CancellationToken ct) =>
        {
            try
            {
                await service.DeleteInstanceAsync(tenant.RealmId, id, ct);
                return Results.NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });
    }
}
