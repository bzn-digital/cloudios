using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Microsoft.AspNetCore.Authorization;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/login", async (LoginRequest request, AuthService authService, CancellationToken ct) =>
        {
            var response = await authService.LoginAsync(request, ct);

            return response is null
                ? Results.Unauthorized()
                : Results.Ok(response);
        }).AllowAnonymous();
    }
}
