using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;
using Bzn.Cloudios.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class HealthCheckEndpoints
{
    public static void MapHealthCheckEndpoints(this WebApplication app)
    {
        // Public health check for Cloudflare Tunnel
        app.MapGet("/health", () =>
        {
            return Results.Ok(new { status = "Healthy", version = "0.1.0" });
        });

        // Host metrics (GlobalAdmin only)
        app.MapGet("/api/metrics/host", async (HealthCheckService healthCheckService, CancellationToken ct) =>
        {
            var metrics = await healthCheckService.GetHostMetricsAsync(ct);
            return Results.Ok(metrics);
        });
    }
}
