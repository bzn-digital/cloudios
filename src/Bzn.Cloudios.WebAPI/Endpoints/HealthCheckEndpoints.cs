using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Microsoft.AspNetCore.Authorization;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class HealthCheckEndpoints
{
    public static void MapHealthCheckEndpoints(this WebApplication app)
    {
        // Public health check for Cloudflare Tunnel
        app.MapGet("/health", async (HealthCheckService healthCheckService, CancellationToken ct) =>
        {
            var result = await healthCheckService.CheckHealthAsync(ct);
            return result.Status == "Healthy" 
                ? Results.Ok(result) 
                : Results.StatusCode(503);
        });

        // Host metrics (GlobalAdmin only)
        app.MapGet("/api/metrics/host", async (HealthCheckService healthCheckService, CancellationToken ct) =>
        {
            var metrics = await healthCheckService.GetHostMetricsAsync(ct);
            return Results.Ok(metrics);
        }).RequireAuthorization("RequirePlatformAdmin");
    }
}
