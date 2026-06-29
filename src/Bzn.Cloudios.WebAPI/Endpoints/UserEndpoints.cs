using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/realms/{realmId:guid}/users");

        group.MapGet("/", async (Guid realmId, UserService service, int page = 1, int pageSize = 20, CancellationToken ct = default) =>
        {
            var result = await service.ListByRealmAsync(realmId, page, pageSize, ct);
            return Results.Ok(result);
        });

        group.MapPost("/", async (Guid realmId, CreateUserRequest request, UserService service, CancellationToken ct) =>
        {
            var (user, error) = await service.CreateAsync(realmId, request, ct);
            if (error is not null) return Results.Conflict(new { error });
            return Results.Created($"/api/realms/{realmId}/users/{user!.Id}", user);
        }).RequireAuthorization();

        group.MapPut("/{id:guid}", async (Guid realmId, Guid id, UpdateUserRequest request, UserService service, CancellationToken ct) =>
        {
            var (user, error) = await service.UpdateAsync(realmId, id, request, ct);
            if (error is not null) return error == "User not found" ? Results.NotFound() : Results.Conflict(new { error });
            return Results.Ok(user);
        });

        group.MapDelete("/{id:guid}", async (Guid realmId, Guid id, UserService service, CancellationToken ct) =>
        {
            var (success, error) = await service.DeleteAsync(realmId, id, ct);
            if (!success) return error == "User not found" ? Results.NotFound() : Results.Conflict(new { error });
            return Results.NoContent();
        });
    }
}
