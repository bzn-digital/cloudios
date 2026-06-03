using Bzn.Cloudios.Application.Services;
using Bzn.Cloudios.Domain.Dto;

namespace Bzn.Cloudios.WebAPI.Endpoints;

public static class MetricsEndpoints
{
    public static void MapMetricsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/containers/{id:guid}/metrics");

        group.MapGet("/", async (Guid id, MetricsService service, DateTime? from, DateTime? to, CancellationToken ct) =>
        {
            var result = await service.GetContainerMetricsAsync(id, from, to, ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        app.MapGet("/api/metrics/host", async (MetricsService service, CancellationToken ct) =>
        {
            var result = await service.GetHostMetricsAsync(ct);
            return Results.Ok(result);
        });
    }
}
