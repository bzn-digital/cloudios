using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Microsoft.AspNetCore.Authorization;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class RegistrationEndpoints
{
    public static void MapRegistrationEndpoints(this WebApplication app)
    {
        app.MapPost("/api/register", async (RegisterRequest request, RealmService realmService, UserService userService, CancellationToken ct) =>
        {
            // Create realm
            var (realm, realmError) = await realmService.CreateAsync(new CreateRealmRequest
            {
                Name = request.RealmName
            }, ct);

            if (realmError is not null)
            {
                return Results.Conflict(new { error = realmError });
            }

            // Create user as RealmOwner
            var (user, userError) = await userService.CreateAsync(realm!.Id, new CreateUserRequest
            {
                Email = request.Email,
                Password = request.Password,
                Role = "RealmOwner"
            }, ct);

            if (userError is not null)
            {
                return Results.Conflict(new { error = userError });
            }

            return Results.Ok(new RegistrationResponse
            {
                Message = "Registration successful",
                RealmId = realm.Id,
                UserId = user!.Id
            });
        }).AllowAnonymous();
    }
}

public record RegisterRequest
{
    public string RealmName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record RegistrationResponse
{
    public string Message { get; init; } = string.Empty;
    public Guid RealmId { get; init; }
    public Guid UserId { get; init; }
}
