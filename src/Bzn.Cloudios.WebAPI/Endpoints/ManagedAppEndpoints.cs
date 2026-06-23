using Bzn.Cloudios.Application.Abstractions;
using Bzn.Cloudios.Domain.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class ManagedAppEndpoints
{
    public static void MapManagedAppEndpoints(this WebApplication app)
    {
        // Admin endpoint: all managed apps across realms
        app.MapGet("/api/managed-apps/all", async (
            IManagedAppService service,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] Guid? realmId = null,
            [FromQuery] string? status = null,
            CancellationToken ct = default) =>
        {
            var result = await service.ListAllAsync(page, pageSize, realmId, status, ct);
            return Results.Ok(result);
        }).RequireAuthorization("PlatformAdmin");

        // List templates (authenticated endpoint - may contain sensitive info)
        app.MapGet("/api/managed-apps/templates", async (
            IManagedAppService service,
            [FromQuery] string? category = null,
            [FromQuery] string? search = null,
            CancellationToken ct = default) =>
        {
            var result = await service.ListTemplatesAsync(category, search, ct);
            return Results.Ok(result);
        }).RequireAuthorization();

        var group = app.MapGroup("/api/managed-apps");

        // List managed apps for the caller's realm.
        group.MapGet("/", async (
            IManagedAppService service,
            ITenantProvider tenant,
            CancellationToken ct,
            [FromQuery] string? search = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20) =>
        {
            var result = await service.ListAsync(tenant.RealmId, search, status, page, pageSize, ct);
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

        // Create a new managed app instance (RealmOwner+ only).
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
        }).RequireAuthorization("RealmOwner");

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

        // Restart a managed app instance.
        group.MapPost("/{id:guid}/restart", async (
            Guid id,
            IManagedAppService service,
            ITenantProvider tenant,
            CancellationToken ct) =>
        {
            try
            {
                var result = await service.RestartInstanceAsync(tenant.RealmId, id, ct);
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
